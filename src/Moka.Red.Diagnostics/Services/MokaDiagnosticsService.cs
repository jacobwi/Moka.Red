using System.Collections.Concurrent;
using Moka.Red.Core.Colors;
using Moka.Red.Core.Theming;

namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Default implementation of <see cref="IMokaDiagnosticsService" />.
///     Lists theme tokens from <see cref="MokaTokens.All" /> with the values <see cref="MokaTheme.ToTokenMap" /> gives.
/// </summary>
internal sealed class MokaDiagnosticsService : IMokaDiagnosticsService
{
	// Disposed components stay listed so the Tree tab can show them, but only this many: a page that keeps making and
	// dropping components would otherwise grow the list for as long as the circuit lives.
	internal const int MaxDisposedEntries = 500;

	private readonly ConcurrentQueue<string> _disposedIds = new();
	private readonly ConcurrentQueue<DiagnosticsEvent> _eventLog = new();
	private readonly ConcurrentDictionary<string, JsInteropEntry> _jsInteropEntries = new();
	private readonly ConcurrentDictionary<string, ComponentRenderEntry> _renderEntries = new();
	private int _disposedCount;
	private int _eventLogCount;

	private bool _isOverlayVisible;
	private bool _isPaused;

	// Off outside the Development environment unless the app opted in (MokaDiagnosticsAvailability): nothing is shown
	// then, so nothing is kept either.
	private readonly bool _recording;

	public MokaDiagnosticsService(DiagnosticsOptions options)
		: this(options, recording: true)
	{
	}

	internal MokaDiagnosticsService(DiagnosticsOptions options, bool recording)
	{
		ArgumentNullException.ThrowIfNull(options);
		Options = options;
		Settings = new DiagnosticsSettings(options);
		_recording = recording;
		_isOverlayVisible = options.StartExpanded;
	}

	public DiagnosticsOptions Options { get; }

	public DiagnosticsSettings Settings { get; }

	public bool IsOverlayVisible
	{
		get => _isOverlayVisible;
		set
		{
			if (_isOverlayVisible == value)
			{
				return;
			}

			_isOverlayVisible = value;
			OnOverlayVisibilityChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public event EventHandler? OnOverlayVisibilityChanged;

	public IReadOnlyList<ThemeTokenGroup> GetThemeTokens(MokaTheme theme)
	{
		ArgumentNullException.ThrowIfNull(theme);

		IReadOnlyDictionary<string, string> values = theme.ToTokenMap();
		return MokaTokens.All
			.Where(token => values.ContainsKey(token.Name))
			.GroupBy(token => token.Group)
			.Select(group => new ThemeTokenGroup(
				group.Key.ToString(),
				group.Select(token => ToThemeToken(theme, token, values[token.Name])).ToList()))
			.ToList();
	}

	// ── Render Tracking ────────────────────────────────────────────

	public event EventHandler? OnRenderDataChanged;

	public void RecordRender(string componentType, string componentId, TimeSpan duration)
	{
		if (!RecordsRenders)
		{
			return;
		}

		ComponentRenderEntry entry = _renderEntries.GetOrAdd(componentId, static (id, type) => new ComponentRenderEntry
		{
			ComponentType = type,
			ComponentId = id
		}, componentType);

		entry.RenderCount++;
		entry.LastRenderTime = DateTime.UtcNow;
		entry.LastRenderDuration = duration;

		LogEvent(new DiagnosticsEvent
		{
			Timestamp = DateTime.UtcNow,
			Type = DiagnosticsEventType.Render,
			Message = $"{componentType} rendered in {duration.TotalMilliseconds:F1}ms",
			ComponentType = componentType,
			ComponentId = componentId
		});

		OnRenderDataChanged?.Invoke(this, EventArgs.Empty);
	}

	public void RecordShouldRenderSkip(string componentType, string componentId)
	{
		if (!RecordsRenders)
		{
			return;
		}

		ComponentRenderEntry entry = _renderEntries.GetOrAdd(componentId, static (id, type) => new ComponentRenderEntry
		{
			ComponentType = type,
			ComponentId = id
		}, componentType);

		entry.ShouldRenderSkipCount++;

		LogEvent(new DiagnosticsEvent
		{
			Timestamp = DateTime.UtcNow,
			Type = DiagnosticsEventType.RenderSkip,
			Message = $"{componentType} render skipped",
			ComponentType = componentType,
			ComponentId = componentId
		});

		OnRenderDataChanged?.Invoke(this, EventArgs.Empty);
	}

	public IReadOnlyList<ComponentRenderEntry> GetRenderEntries()
	{
		return _renderEntries.Values
			.OrderByDescending(e => e.RenderCount)
			.ToList();
	}

	public void ClearRenderData()
	{
		_renderEntries.Clear();
		_disposedIds.Clear();
		Interlocked.Exchange(ref _disposedCount, 0);
		OnRenderDataChanged?.Invoke(this, EventArgs.Empty);
		OnPerformanceDataChanged?.Invoke(this, EventArgs.Empty);
	}

	private bool RecordsRenders => _recording && !_isPaused && Settings.RenderTrackingEnabled;

	// ── JS Interop Tracking ────────────────────────────────────────

	public event EventHandler? OnPerformanceDataChanged;

	public void RecordJsInteropCall(string identifier, TimeSpan duration)
	{
		if (!_recording || _isPaused || !Settings.JsInteropTrackingEnabled)
		{
			return;
		}

		JsInteropEntry entry = _jsInteropEntries.GetOrAdd(identifier, static id => new JsInteropEntry
		{
			Identifier = id
		});

		entry.CallCount++;
		entry.TotalDuration += duration;

		if (duration > entry.MaxDuration)
		{
			entry.MaxDuration = duration;
		}

		LogEvent(new DiagnosticsEvent
		{
			Timestamp = DateTime.UtcNow,
			Type = DiagnosticsEventType.JsInterop,
			Message = $"JS call '{identifier}' took {duration.TotalMilliseconds:F1}ms"
		});

		OnPerformanceDataChanged?.Invoke(this, EventArgs.Empty);
	}

	public IReadOnlyList<JsInteropEntry> GetJsInteropEntries()
	{
		return _jsInteropEntries.Values
			.OrderByDescending(e => e.CallCount)
			.ToList();
	}

	// ── Disposal Tracking ──────────────────────────────────────────

	public void RecordDisposal(string componentType, string componentId)
	{
		if (!_recording)
		{
			return;
		}

		// Marked even while paused: a component that went away is gone whether or not anything is recorded, and the
		// Active count would otherwise keep it for good.
		if (_renderEntries.TryGetValue(componentId, out ComponentRenderEntry? entry) && !entry.IsDisposed)
		{
			entry.IsDisposed = true;
			_disposedIds.Enqueue(componentId);
			ForgetOldestDisposed();
		}

		if (_isPaused)
		{
			return;
		}

		Interlocked.Increment(ref _disposedCount);

		LogEvent(new DiagnosticsEvent
		{
			Timestamp = DateTime.UtcNow,
			Type = DiagnosticsEventType.Disposal,
			Message = $"{componentType} disposed",
			ComponentType = componentType,
			ComponentId = componentId
		});

		OnPerformanceDataChanged?.Invoke(this, EventArgs.Empty);
	}

	public int ActiveComponentCount => _renderEntries.Values.Count(e => !e.IsDisposed);

	public int DisposedComponentCount => _disposedCount;

	public void ClearPerformanceData()
	{
		_jsInteropEntries.Clear();
		OnPerformanceDataChanged?.Invoke(this, EventArgs.Empty);
	}

	// ── Event Log ─────────────────────────────────────────────────

	public event EventHandler? OnEventLogged;

	public IReadOnlyList<DiagnosticsEvent> GetRecentEvents(int maxCount = 100)
	{
		// Trimming here too means a lower limit set in the Settings tab shows at once, not only
		// after the next event arrives.
		TrimEventLog();

		return _eventLog
			.Reverse()
			.Take(maxCount)
			.ToList();
	}

	public void ClearEvents()
	{
		while (_eventLog.TryDequeue(out _))
		{
			Interlocked.Decrement(ref _eventLogCount);
		}

		_eventLogCount = 0;
	}

	// ── Pause / Resume ────────────────────────────────────────────

	public event EventHandler? OnPauseStateChanged;

	public bool IsPaused
	{
		get => _isPaused;
		set
		{
			if (_isPaused == value)
			{
				return;
			}

			_isPaused = value;
			OnPauseStateChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private void LogEvent(DiagnosticsEvent evt)
	{
		_eventLog.Enqueue(evt);
		Interlocked.Increment(ref _eventLogCount);
		TrimEventLog();

		OnEventLogged?.Invoke(this, EventArgs.Empty);
	}

	private void ForgetOldestDisposed()
	{
		while (_disposedIds.Count > MaxDisposedEntries && _disposedIds.TryDequeue(out string? oldest))
		{
			if (_renderEntries.TryGetValue(oldest, out ComponentRenderEntry? entry) && entry.IsDisposed)
			{
				_renderEntries.TryRemove(oldest, out _);
			}
		}
	}

	// Reads the limit on every call because the Settings tab changes it at runtime.
	private void TrimEventLog()
	{
		int limit = Math.Max(0, Settings.MaxEventLogEntries);
		while (Volatile.Read(ref _eventLogCount) > limit && _eventLog.TryDequeue(out _))
		{
			Interlocked.Decrement(ref _eventLogCount);
		}
	}

	private static ThemeToken ToThemeToken(MokaTheme theme, MokaToken token, string value)
	{
		ThemeTokenKind kind = KindOf(token);
		string? color = kind == ThemeTokenKind.Color && theme.TryResolveColor(value, out MokaSrgb resolved)
			? resolved.ToString()
			: null;
		return new ThemeToken(token.Name, value, kind) { ResolvedColor = color };
	}

	private static ThemeTokenKind KindOf(MokaToken token) => token switch
	{
		{ Kind: MokaTokenKind.Color } => ThemeTokenKind.Color,
		{ Group: MokaTokenGroup.Spacing, Kind: MokaTokenKind.Length } => ThemeTokenKind.Spacing,
		{ Group: MokaTokenGroup.Radius, Kind: MokaTokenKind.Length } => ThemeTokenKind.BorderRadius,
		{ Group: MokaTokenGroup.Typography } => TypographyKindOf(token.Name),
		_ => ThemeTokenKind.Other
	};

	private static ThemeTokenKind TypographyKindOf(string name) =>
		name.StartsWith("--moka-font-family", StringComparison.Ordinal) ? ThemeTokenKind.FontFamily
		: name.StartsWith("--moka-font-size", StringComparison.Ordinal) ? ThemeTokenKind.FontSize
		: name.StartsWith("--moka-font-weight", StringComparison.Ordinal) ? ThemeTokenKind.FontWeight
		: name.StartsWith("--moka-line-height", StringComparison.Ordinal) ? ThemeTokenKind.LineHeight
		: ThemeTokenKind.Other;
}
