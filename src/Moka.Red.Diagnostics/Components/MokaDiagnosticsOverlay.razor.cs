using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Diagnostics.Services;
using Moka.Red.Icons;
using Moka.Red.Primitives.Tooltip;

namespace Moka.Red.Diagnostics.Components;

/// <summary>
///     Floating diagnostics overlay with badge toggle, tabbed panel, drag support,
///     and open-in-new-window capability. It renders nothing outside the Development environment unless
///     <see cref="DiagnosticsOptions.EnableOutsideDevelopment" /> is set.
/// </summary>
public sealed partial class MokaDiagnosticsOverlay : ComponentBase, IAsyncDisposable
{
	private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

	// The tabs and their panel point at each other by id, and an id must be unique in the page.
	private readonly string _idPrefix = $"moka-diag-{Guid.NewGuid():N}";
	private readonly string _panelId;
	private string _activeTab = DiagnosticsTabs.Console;
	private ElementReference _badgeRef;
	private IMokaDiagnosticsService? _diagnosticsService;
	private bool _disposed;
	private DotNetObjectReference<MokaDiagnosticsOverlay>? _dotNetRef;
	private bool _dragInitialized;
	private IJSObjectReference? _dragModule;
	private FocusTarget _focusAfterRender;
	private ElementReference _headerRef;
	private bool _isExpanded;
	private IJSObjectReference? _jsModule;
	private ElementReference _panelRef;
	private MokaDiagnosticsSession? _session;
	private MokaDiagnosticsSessions? _sessions;
	private IJSObjectReference? _shortcut;
	private IJSObjectReference? _tabRow;
	private string? _tabRowBoundTo;
	private ElementReference _tabsRowRef;

	/// <summary>Creates the overlay.</summary>
	public MokaDiagnosticsOverlay() => _panelId = $"{_idPrefix}-panel";

	private enum FocusTarget
	{
		None,
		Panel,
		Badge
	}

	/// <summary>
	///     The current theme to inspect. Pass this explicitly so the inspector
	///     updates when the theme changes (CascadingValue IsFixed="true" won't re-cascade).
	/// </summary>
	[Parameter]
	public MokaTheme? Theme { get; set; }

	// The diagnostics services are looked up rather than injected: [Inject] throws for a service that
	// is not registered, even on a nullable property, and the overlay should render nothing then.
	[Inject] private IServiceProvider Services { get; set; } = default!;

	[Inject] private IJSRuntime? _jsRuntime { get; set; }

	private string PositionStyle
	{
		get
		{
			OverlayPosition position = _diagnosticsService?.Settings.Position ?? OverlayPosition.BottomRight;
			return position switch
			{
				OverlayPosition.BottomRight => "bottom: 16px; right: 16px;",
				OverlayPosition.BottomLeft => "bottom: 16px; left: 16px;",
				OverlayPosition.TopRight => "top: 16px; right: 16px;",
				OverlayPosition.TopLeft => "top: 16px; left: 16px;",
				_ => "bottom: 16px; right: 16px;"
			};
		}
	}

	private string PanelStyle => $"{PositionStyle} width: {_diagnosticsService?.Settings.PanelWidth ?? 480}px;";

	// The badge's name opens away from the corner it sits in.
	private MokaTooltipPosition BadgeTooltipPosition =>
		(_diagnosticsService?.Settings.Position ?? OverlayPosition.BottomRight) switch
		{
			OverlayPosition.BottomLeft => MokaTooltipPosition.TopStart,
			OverlayPosition.TopRight => MokaTooltipPosition.BottomEnd,
			OverlayPosition.TopLeft => MokaTooltipPosition.BottomStart,
			_ => MokaTooltipPosition.TopEnd
		};

	private MokaIconDefinition PauseIcon => _diagnosticsService?.IsPaused == true ? MokaIcons.Action.Play : MokaIcons.Action.Pause;

	private string PauseClass => new CssBuilder("moka-diag-action-btn")
		.AddClass("moka-diag-action-btn--active", _diagnosticsService?.IsPaused == true)
		.Build();

	/// <summary>
	///     Stops listening to the diagnostics service, ends the overlay's popup session and releases its JavaScript
	///     modules.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		_disposed = true;

		if (_diagnosticsService is not null)
		{
			_diagnosticsService.OnOverlayVisibilityChanged -= HandleVisibilityChanged;
			_diagnosticsService.OnPauseStateChanged -= HandleShownStateChanged;
			_diagnosticsService.Settings.Changed -= HandleShownStateChanged;
		}

		if (_session is not null)
		{
			_sessions?.Remove(_session.Key);
			_session = null;
		}

		await _dragModule.SafeInvokeVoidAsync("removeDraggable", _headerRef);
		await _dragModule.SafeDisposeAsync();
		_dragModule = null;

		await _shortcut.SafeInvokeVoidAsync("dispose");
		await _shortcut.SafeDisposeAsync();
		_shortcut = null;

		await ReleaseTabRowAsync();

		await _jsModule.SafeDisposeAsync();
		_jsModule = null;

		_dotNetRef?.Dispose();
	}

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		_diagnosticsService = DiagnosticsServiceResolver.Resolve(null, Services);
		_sessions = Services.GetService<MokaDiagnosticsSessions>();

		if (_diagnosticsService is not null)
		{
			// The service belongs to this window, so a layout that mounts the overlay again finds the panel as it was left.
			_isExpanded = _diagnosticsService.IsOverlayVisible;
			_diagnosticsService.OnOverlayVisibilityChanged += HandleVisibilityChanged;

			// The popup page can pause this window or change its settings from its own circuit.
			_diagnosticsService.OnPauseStateChanged += HandleShownStateChanged;
			_diagnosticsService.Settings.Changed += HandleShownStateChanged;
		}
	}

	/// <inheritdoc />
	protected override void OnParametersSet() => _session?.ShowTheme(Theme);

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && _diagnosticsService is not null && _jsRuntime is not null)
		{
			try
			{
				_dotNetRef = DotNetObjectReference.Create(this);
				_jsModule = await _jsRuntime.InvokeAsync<IJSObjectReference>(
					"import",
					"./_content/Moka.Red.Diagnostics/moka-diagnostics.js");

				_shortcut = await _jsModule.InvokeAsync<IJSObjectReference>(
					"registerKeyboardShortcut",
					_dotNetRef,
					_diagnosticsService.Options.KeyboardShortcut);
			}
			catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
			{
				_dotNetRef?.Dispose();
				_dotNetRef = null;
			}
		}

		// The panel is a new element each time it opens, so its header is made draggable each time.
		if (_isExpanded && !_dragInitialized && _jsRuntime is not null)
		{
			try
			{
				_dragModule ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
					"import", "./_content/Moka.Red.Core/moka-drag.js");
				await _dragModule.InvokeVoidAsync("makeDraggable", null, _panelRef, _headerRef,
					new Dictionary<string, object?> { ["bounds"] = true });
				_dragInitialized = true;
			}
			catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
			{
			}
		}

		await BindTabRowAsync();
		await MoveFocusAsync();
	}

	// The tab row is a new element each time the panel opens, so it's watched each time and let go once it's gone.
	private async Task BindTabRowAsync()
	{
		string? row = _isExpanded ? _tabsRowRef.Id : null;
		if (row == _tabRowBoundTo)
		{
			return;
		}

		await ReleaseTabRowAsync();
		if (row is null || _jsModule is null)
		{
			return;
		}

		try
		{
			_tabRow = await _jsModule.InvokeAsync<IJSObjectReference>("bindTabRow", _tabsRowRef);
			_tabRowBoundTo = row;
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
	}

	private async Task ReleaseTabRowAsync()
	{
		IJSObjectReference? tabRow = _tabRow;
		_tabRow = null;
		_tabRowBoundTo = null;
		await tabRow.SafeInvokeVoidAsync("dispose");
		await tabRow.SafeDisposeAsync();
	}

	/// <summary>
	///     Called from JavaScript when the keyboard shortcut is pressed outside the panel. It opens or closes the
	///     panel and leaves focus where it is.
	/// </summary>
	[JSInvokable]
	public void ToggleFromJs()
	{
		SetExpanded(!_isExpanded, FocusTarget.None);
		StateHasChanged();
	}

	/// <summary>
	///     Called from JavaScript when Escape or the keyboard shortcut is pressed inside the panel. It closes the panel
	///     and gives focus to the badge that reopens it, since the focused control goes with the panel.
	/// </summary>
	[JSInvokable]
	public void CloseFromJs()
	{
		if (!_isExpanded)
		{
			return;
		}

		SetExpanded(false, FocusTarget.Badge);
		StateHasChanged();
	}

	private void Toggle() => SetExpanded(!_isExpanded, _isExpanded ? FocusTarget.Badge : FocusTarget.Panel);

	// The badge and the panel replace each other, so the control that was pressed is gone after the render. Focus goes
	// to what took its place, as it would after the browser's own disclosure.
	private void SetExpanded(bool expanded, FocusTarget focus)
	{
		_isExpanded = expanded;
		_dragInitialized = false;
		_focusAfterRender = focus;

		if (_diagnosticsService is not null)
		{
			_diagnosticsService.IsOverlayVisible = expanded;
		}
	}

	private async Task MoveFocusAsync()
	{
		FocusTarget target = _focusAfterRender;
		_focusAfterRender = FocusTarget.None;
		ElementReference? element = target switch
		{
			FocusTarget.Panel when _isExpanded => _panelRef,
			FocusTarget.Badge when !_isExpanded => _badgeRef,
			_ => null
		};

		if (element is null)
		{
			return;
		}

		try
		{
			await element.Value.FocusAsync();
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
	}

	private void SetActiveTab(string tab) => _activeTab = tab;

	private string TabId(string tab) => $"{_idPrefix}-{tab}";

	private string TabClass(string tab) => _activeTab == tab ? "moka-diag-tab moka-diag-tab--active" : "moka-diag-tab";

	private string Selected(string tab) => _activeTab == tab ? "true" : "false";

	// One tab stop for the row, on the tab showing; the arrow keys move between the others.
	private string TabIndex(string tab) => _activeTab == tab ? "0" : "-1";

	private async Task OpenInNewWindow()
	{
		if (_jsModule is null)
		{
			return;
		}

		// The popup gets a scope of its own. The key lets its page find this window's service and theme, so it
		// shows this window's renders, events and tokens rather than its own empty ones.
		if (_session is null && _sessions is not null && _diagnosticsService is not null)
		{
			_session = _sessions.Add(_diagnosticsService, Theme);
		}

		await _jsModule.SafeInvokeVoidAsync("openDiagnosticsPage", _session?.Key);
	}

	private void TogglePause()
	{
		if (_diagnosticsService is not null)
		{
			_diagnosticsService.IsPaused = !_diagnosticsService.IsPaused;
		}
	}

	// Streamed as UTF-8 bytes, as the Blazor file downloads guidance does, rather than as base64 text that the browser
	// decoded into one character per byte and the Blob encoded again.
	private async Task ExportData()
	{
		if (_diagnosticsService is null || _jsModule is null)
		{
			return;
		}

		var data = new
		{
			Timestamp = DateTime.UtcNow,
			RenderEntries = _diagnosticsService.GetRenderEntries(),
			JsInteropEntries = _diagnosticsService.GetJsInteropEntries(),
			ActiveComponents = _diagnosticsService.ActiveComponentCount,
			DisposedComponents = _diagnosticsService.DisposedComponentCount,
			Events = _diagnosticsService.GetRecentEvents(int.MaxValue)
		};

		string filename = $"moka-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.json";
		using MemoryStream json = new(JsonSerializer.SerializeToUtf8Bytes(data, _jsonOptions), writable: false);
		using DotNetStreamReference content = new(json, leaveOpen: true);
		await _jsModule.SafeInvokeVoidAsync("downloadJson", content, filename);
	}

	private void HandleVisibilityChanged(object? sender, EventArgs e) => _ = DispatchAsync(ApplyVisibility);

	private void HandleShownStateChanged(object? sender, EventArgs e) => _ = DispatchAsync(StateHasChanged);

	private void ApplyVisibility()
	{
		if (_diagnosticsService is null || _diagnosticsService.IsOverlayVisible == _isExpanded)
		{
			return;
		}

		SetExpanded(_diagnosticsService.IsOverlayVisible, FocusTarget.None);
		StateHasChanged();
	}

	// The service's state can change from anywhere that holds it, on any thread; the render goes to the renderer's
	// thread, and a failure reaches Blazor's error handling rather than vanishing with a discarded task.
	private async Task DispatchAsync(Action change)
	{
		if (_disposed)
		{
			return;
		}

		try
		{
			await InvokeAsync(() =>
			{
				if (!_disposed)
				{
					change();
				}
			});
		}
		catch (ObjectDisposedException)
		{
			// The renderer went away between the change and the dispatch.
		}
		catch (Exception ex) when (!_disposed)
		{
			await DispatchExceptionAsync(ex);
		}
	}
}
