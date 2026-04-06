using System.Collections.Concurrent;
using Moka.Red.Core.Theming;

namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Default implementation of <see cref="IMokaDiagnosticsService" />.
///     Extracts theme tokens mirroring <see cref="MokaTheme.ToCssVariables" />.
/// </summary>
internal sealed class MokaDiagnosticsService : IMokaDiagnosticsService
{
	private const int MaxEventLogSize = 500;
	private readonly ConcurrentQueue<DiagnosticsEvent> _eventLog = new();
	private readonly ConcurrentDictionary<string, JsInteropEntry> _jsInteropEntries = new();
	private readonly ConcurrentDictionary<string, ComponentRenderEntry> _renderEntries = new();
	private int _disposedCount;
	private int _eventLogCount;

	private bool _isOverlayVisible;
	private bool _isPaused;

	public MokaDiagnosticsService(DiagnosticsOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		Options = options;
		_isOverlayVisible = options.StartExpanded;
	}

	public DiagnosticsOptions Options { get; }

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

		return
		[
			BuildPaletteGroup(theme.Palette),
			BuildTypographyGroup(theme.Typography),
			BuildSpacingGroup(theme.Spacing),
			BuildBorderRadiusGroup(theme.Spacing)
		];
	}

	// ── Render Tracking ────────────────────────────────────────────

	public event EventHandler? OnRenderDataChanged;

	public void RecordRender(string componentType, string componentId, TimeSpan duration)
	{
		if (_isPaused || !Options.RenderTrackingEnabled)
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
		if (_isPaused || !Options.RenderTrackingEnabled)
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
		OnRenderDataChanged?.Invoke(this, EventArgs.Empty);
	}

	// ── JS Interop Tracking ────────────────────────────────────────

	public event EventHandler? OnPerformanceDataChanged;

	public void RecordJsInteropCall(string identifier, TimeSpan duration)
	{
		if (_isPaused || !Options.JsInteropTrackingEnabled)
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
		if (_isPaused)
		{
			return;
		}

		if (_renderEntries.TryGetValue(componentId, out ComponentRenderEntry? entry))
		{
			entry.IsDisposed = true;
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
		_disposedCount = 0;

		// Reset disposal flags on render entries
		foreach (ComponentRenderEntry entry in _renderEntries.Values)
		{
			entry.IsDisposed = false;
		}

		OnPerformanceDataChanged?.Invoke(this, EventArgs.Empty);
	}

	// ── Event Log ─────────────────────────────────────────────────

	public event EventHandler? OnEventLogged;

	public IReadOnlyList<DiagnosticsEvent> GetRecentEvents(int maxCount = 100)
	{
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
		int count = Interlocked.Increment(ref _eventLogCount);

		// Trim oldest events when over capacity
		while (count > MaxEventLogSize && _eventLog.TryDequeue(out _))
		{
			count = Interlocked.Decrement(ref _eventLogCount);
		}

		OnEventLogged?.Invoke(this, EventArgs.Empty);
	}

	// ── Theme Token Builders ───────────────────────────────────────

	private static ThemeTokenGroup BuildPaletteGroup(MokaPalette palette)
	{
		List<ThemeToken> tokens =
		[
			new("--moka-color-primary", palette.Primary, ThemeTokenKind.Color),
			new("--moka-color-primary-light", palette.PrimaryLight, ThemeTokenKind.Color),
			new("--moka-color-primary-dark", palette.PrimaryDark, ThemeTokenKind.Color),
			new("--moka-color-on-primary", palette.OnPrimary, ThemeTokenKind.Color),

			new("--moka-color-secondary", palette.Secondary, ThemeTokenKind.Color),
			new("--moka-color-secondary-light", palette.SecondaryLight, ThemeTokenKind.Color),
			new("--moka-color-secondary-dark", palette.SecondaryDark, ThemeTokenKind.Color),
			new("--moka-color-on-secondary", palette.OnSecondary, ThemeTokenKind.Color),

			new("--moka-color-surface", palette.Surface, ThemeTokenKind.Color),
			new("--moka-color-surface-variant", palette.SurfaceVariant, ThemeTokenKind.Color),
			new("--moka-color-on-surface", palette.OnSurface, ThemeTokenKind.Color),
			new("--moka-color-background", palette.Background, ThemeTokenKind.Color),
			new("--moka-color-on-background", palette.OnBackground, ThemeTokenKind.Color),

			new("--moka-color-error", palette.Error, ThemeTokenKind.Color),
			new("--moka-color-on-error", palette.OnError, ThemeTokenKind.Color),
			new("--moka-color-warning", palette.Warning, ThemeTokenKind.Color),
			new("--moka-color-on-warning", palette.OnWarning, ThemeTokenKind.Color),
			new("--moka-color-success", palette.Success, ThemeTokenKind.Color),
			new("--moka-color-on-success", palette.OnSuccess, ThemeTokenKind.Color),
			new("--moka-color-info", palette.Info, ThemeTokenKind.Color),
			new("--moka-color-on-info", palette.OnInfo, ThemeTokenKind.Color),

			new("--moka-color-outline", palette.Outline, ThemeTokenKind.Color),
			new("--moka-color-outline-variant", palette.OutlineVariant, ThemeTokenKind.Color)
		];

		return new ThemeTokenGroup("Palette", tokens);
	}

	private static ThemeTokenGroup BuildTypographyGroup(MokaTypography typography)
	{
		List<ThemeToken> tokens =
		[
			new("--moka-font-family", typography.FontFamily, ThemeTokenKind.FontFamily),
			new("--moka-font-family-mono", typography.FontFamilyMono, ThemeTokenKind.FontFamily),

			new("--moka-font-size-xs", typography.FontSizeXs, ThemeTokenKind.FontSize),
			new("--moka-font-size-sm", typography.FontSizeSm, ThemeTokenKind.FontSize),
			new("--moka-font-size-base", typography.FontSizeBase, ThemeTokenKind.FontSize),
			new("--moka-font-size-md", typography.FontSizeMd, ThemeTokenKind.FontSize),
			new("--moka-font-size-lg", typography.FontSizeLg, ThemeTokenKind.FontSize),
			new("--moka-font-size-xl", typography.FontSizeXl, ThemeTokenKind.FontSize),
			new("--moka-font-size-xxl", typography.FontSizeXxl, ThemeTokenKind.FontSize),

			new("--moka-line-height-tight", typography.LineHeightTight, ThemeTokenKind.LineHeight),
			new("--moka-line-height-base", typography.LineHeightBase, ThemeTokenKind.LineHeight),
			new("--moka-line-height-relaxed", typography.LineHeightRelaxed, ThemeTokenKind.LineHeight),

			new("--moka-font-weight-light", typography.FontWeightLight, ThemeTokenKind.FontWeight),
			new("--moka-font-weight-normal", typography.FontWeightNormal, ThemeTokenKind.FontWeight),
			new("--moka-font-weight-medium", typography.FontWeightMedium, ThemeTokenKind.FontWeight),
			new("--moka-font-weight-semibold", typography.FontWeightSemibold, ThemeTokenKind.FontWeight),
			new("--moka-font-weight-bold", typography.FontWeightBold, ThemeTokenKind.FontWeight)
		];

		return new ThemeTokenGroup("Typography", tokens);
	}

	private static ThemeTokenGroup BuildSpacingGroup(MokaSpacing spacing)
	{
		List<ThemeToken> tokens =
		[
			new("--moka-spacing-xxs", spacing.Xxs, ThemeTokenKind.Spacing),
			new("--moka-spacing-xs", spacing.Xs, ThemeTokenKind.Spacing),
			new("--moka-spacing-sm", spacing.Sm, ThemeTokenKind.Spacing),
			new("--moka-spacing-md", spacing.Md, ThemeTokenKind.Spacing),
			new("--moka-spacing-lg", spacing.Lg, ThemeTokenKind.Spacing),
			new("--moka-spacing-xl", spacing.Xl, ThemeTokenKind.Spacing),
			new("--moka-spacing-xxl", spacing.Xxl, ThemeTokenKind.Spacing)
		];

		return new ThemeTokenGroup("Spacing", tokens);
	}

	private static ThemeTokenGroup BuildBorderRadiusGroup(MokaSpacing spacing)
	{
		List<ThemeToken> tokens =
		[
			new("--moka-radius-none", spacing.RadiusNone, ThemeTokenKind.BorderRadius),
			new("--moka-radius-sm", spacing.RadiusSm, ThemeTokenKind.BorderRadius),
			new("--moka-radius-md", spacing.RadiusMd, ThemeTokenKind.BorderRadius),
			new("--moka-radius-lg", spacing.RadiusLg, ThemeTokenKind.BorderRadius),
			new("--moka-radius-xl", spacing.RadiusXl, ThemeTokenKind.BorderRadius),
			new("--moka-radius-full", spacing.RadiusFull, ThemeTokenKind.BorderRadius)
		];

		return new ThemeTokenGroup("Border Radius", tokens);
	}
}
