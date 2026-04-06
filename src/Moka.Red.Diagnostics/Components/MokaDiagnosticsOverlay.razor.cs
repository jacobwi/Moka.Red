using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Theming;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components;

/// <summary>
///     Floating diagnostics overlay with badge toggle, tabbed panel, drag support,
///     and open-in-new-window capability.
/// </summary>
public sealed partial class MokaDiagnosticsOverlay : ComponentBase, IAsyncDisposable
{
	private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
	private string _activeTab = "Console";
	private DotNetObjectReference<MokaDiagnosticsOverlay>? _dotNetRef;
	private bool _dragInitialized;
	private IJSObjectReference? _dragModule;
	private ElementReference _headerRef;
	private bool _isExpanded;
	private IJSObjectReference? _jsModule;
	private ElementReference _panelRef;

	/// <summary>
	///     The current theme to inspect. Pass this explicitly so the inspector
	///     updates when the theme changes (CascadingValue IsFixed="true" won't re-cascade).
	/// </summary>
	[Parameter]
	public MokaTheme? Theme { get; set; }

	[Inject] private IMokaDiagnosticsService? _diagnosticsService { get; set; }

	[Inject] private IJSRuntime? _jsRuntime { get; set; }

	private string PositionStyle
	{
		get
		{
			OverlayPosition position = _diagnosticsService?.Options.Position ?? OverlayPosition.BottomRight;
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

	private string PanelStyle => $"{PositionStyle} width: {_diagnosticsService?.Options.PanelWidth ?? 480}px;";

	public async ValueTask DisposeAsync()
	{
		if (_diagnosticsService is not null)
		{
			_diagnosticsService.OnOverlayVisibilityChanged -= OnVisibilityChanged;
		}

		if (_dragModule is not null)
		{
			try
			{
				await _dragModule.InvokeVoidAsync("removeDraggable", _headerRef);
				await _dragModule.DisposeAsync();
			}
			catch (JSDisconnectedException)
			{
				// Circuit disconnected, safe to ignore.
			}
		}

		if (_jsModule is not null)
		{
			try
			{
				await _jsModule.InvokeVoidAsync("dispose");
				await _jsModule.DisposeAsync();
			}
			catch (JSDisconnectedException)
			{
				// Circuit disconnected, safe to ignore.
			}
		}

		_dotNetRef?.Dispose();
	}

	protected override void OnInitialized()
	{
		if (_diagnosticsService is not null)
		{
			_isExpanded = _diagnosticsService.Options.StartExpanded;
			_diagnosticsService.OnOverlayVisibilityChanged += OnVisibilityChanged;
		}
	}

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

				await _jsModule.InvokeVoidAsync(
					"registerKeyboardShortcut",
					_dotNetRef,
					_diagnosticsService.Options.KeyboardShortcut);
			}
			catch (JSDisconnectedException)
			{
				// Circuit disconnected during init
				_dotNetRef?.Dispose();
				_dotNetRef = null;
			}
		}

		// Enable drag whenever the panel is expanded and drag hasn't been initialized yet
		if (_isExpanded && !_dragInitialized && _jsRuntime is not null)
		{
			try
			{
				_dragModule ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
					"import", "./_content/Moka.Red.Core/moka-drag.js");
				await _dragModule.InvokeVoidAsync("makeDraggable", null, _panelRef, _headerRef, new
				{
					bounds = true
				});
				_dragInitialized = true;
			}
			catch (JSException)
			{
				// Element references may not be ready yet; will retry on next render.
			}
		}
	}

	/// <summary>
	///     Called from JavaScript when the keyboard shortcut is pressed.
	/// </summary>
	[JSInvokable]
	public void ToggleFromJs()
	{
		_isExpanded = !_isExpanded;

		if (_diagnosticsService is not null)
		{
			_diagnosticsService.IsOverlayVisible = _isExpanded;
		}

		StateHasChanged();
	}

	private void Toggle()
	{
		_isExpanded = !_isExpanded;
		_dragInitialized = false;

		if (_diagnosticsService is not null)
		{
			_diagnosticsService.IsOverlayVisible = _isExpanded;
		}
	}

	private void SetActiveTab(string tab) => _activeTab = tab;

	private string TabClass(string tab) => _activeTab == tab ? "moka-diag-tab moka-diag-tab--active" : "moka-diag-tab";

	private async Task OpenInNewWindow()
	{
		if (_jsModule is not null)
		{
			try
			{
				await _jsModule.InvokeVoidAsync("openDiagnosticsPage");
			}
			catch (JSException)
			{
				// Pop-up may be blocked; safe to ignore.
			}
		}
	}

	private void TogglePause()
	{
		if (_diagnosticsService is not null)
		{
			_diagnosticsService.IsPaused = !_diagnosticsService.IsPaused;
		}
	}

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
			Events = _diagnosticsService.GetRecentEvents()
		};

		string json = JsonSerializer.Serialize(data, _jsonOptions);

		string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
		string filename = $"moka-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.json";

		try
		{
			await _jsModule.InvokeVoidAsync("downloadJson", base64, filename);
		}
		catch (JSException)
		{
			// Download may fail in certain contexts; safe to ignore.
		}
	}

	private void OnVisibilityChanged(object? sender, EventArgs e)
	{
		if (_diagnosticsService is not null)
		{
			_isExpanded = _diagnosticsService.IsOverlayVisible;
		}

		_dragInitialized = false;
		InvokeAsync(StateHasChanged);
	}
}
