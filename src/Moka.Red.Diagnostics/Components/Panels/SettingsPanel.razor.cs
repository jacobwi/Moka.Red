using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel for configuring diagnostics behavior at runtime. The window's own settings go to
///     <see cref="IMokaDiagnosticsService.Settings" /> and apply to that window only; the console log level goes to
///     <see cref="DiagnosticsOptions.MinConsoleLogLevel" />, which the whole app shares.
/// </summary>
public sealed partial class SettingsPanel : ComponentBase, IDisposable
{
	// The labels point at their controls by id, and more than one panel can be on a page.
	private readonly string _idPrefix = $"moka-diag-settings-{Guid.NewGuid():N}";
	private IMokaDiagnosticsService? _diagnosticsService;
	private bool _disposed;

	[Inject] private IServiceProvider Services { get; set; } = default!;

	[CascadingParameter(Name = DiagnosticsServiceResolver.CascadeName)]
	private IMokaDiagnosticsService? SharedService { get; set; }

	/// <summary>Callback when any setting changes, so parent can re-render.</summary>
	[Parameter]
	public EventCallback OnSettingsChanged { get; set; }

	private DiagnosticsOptions? Options => _diagnosticsService?.Options;

	private DiagnosticsSettings? Settings => _diagnosticsService?.Settings;

	/// <summary>Stops listening to the window's settings.</summary>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		if (_diagnosticsService is not null)
		{
			_diagnosticsService.Settings.Changed -= HandleSettingsChanged;
			_diagnosticsService.OnPauseStateChanged -= HandleSettingsChanged;
		}
	}

	private string FieldId(string setting) => $"{_idPrefix}-{setting}";

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		_diagnosticsService = DiagnosticsServiceResolver.Resolve(SharedService, Services);

		// The overlay's header and the popup page change the same settings, so the controls follow them.
		if (_diagnosticsService is not null)
		{
			_diagnosticsService.Settings.Changed += HandleSettingsChanged;
			_diagnosticsService.OnPauseStateChanged += HandleSettingsChanged;
		}
	}

	private async Task NotifyChanged()
	{
		if (OnSettingsChanged.HasDelegate)
		{
			await OnSettingsChanged.InvokeAsync();
		}
	}

	// The controls bind their setting, so a value the setter clamps (a width past 600) shows as clamped.
	private Task SetPosition(OverlayPosition position) => Apply(settings => settings.Position = position);

	private Task SetPanelWidth(int width) => Apply(settings => settings.PanelWidth = Math.Clamp(width, 300, 600));

	private Task SetMaxEventLog(int max) => Apply(settings => settings.MaxEventLogEntries = Math.Clamp(max, 100, 1000));

	private Task SetRenderTracking(bool on) => Apply(settings => settings.RenderTrackingEnabled = on);

	private Task SetJsInteropTracking(bool on) => Apply(settings => settings.JsInteropTrackingEnabled = on);

	// Shared by every window on purpose: the logger feeds one console buffer for the whole app.
	private async Task SetMinLogLevel(LogLevel level)
	{
		if (Options is not null)
		{
			Options.MinConsoleLogLevel = level;
			await NotifyChanged();
		}
	}

	private async Task SetPaused(bool paused)
	{
		if (_diagnosticsService is not null)
		{
			_diagnosticsService.IsPaused = paused;
			await NotifyChanged();
		}
	}

	private async Task Apply(Action<DiagnosticsSettings> change)
	{
		if (Settings is not null)
		{
			change(Settings);
			await NotifyChanged();
		}
	}

	private void HandleSettingsChanged(object? sender, EventArgs e) => _ = RenderAsync();

	// A change made in another window arrives on that window's thread; the render goes to this panel's renderer, and a
	// failure reaches Blazor's error handling rather than vanishing with a discarded task.
	private async Task RenderAsync()
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
					StateHasChanged();
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
