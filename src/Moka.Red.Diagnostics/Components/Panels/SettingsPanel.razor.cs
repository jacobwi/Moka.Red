using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel for configuring diagnostics behavior at runtime.
///     Changes are applied immediately to <see cref="DiagnosticsOptions" />.
/// </summary>
public sealed partial class SettingsPanel : ComponentBase
{
	// The labels point at their controls by id, and more than one panel can be on a page.
	private readonly string _idPrefix = $"moka-diag-settings-{Guid.NewGuid():N}";
	private IMokaDiagnosticsService? _diagnosticsService;

	[Inject] private IServiceProvider Services { get; set; } = default!;

	[CascadingParameter(Name = DiagnosticsServiceResolver.CascadeName)]
	private IMokaDiagnosticsService? SharedService { get; set; }

	/// <summary>Callback when any setting changes, so parent can re-render.</summary>
	[Parameter]
	public EventCallback OnSettingsChanged { get; set; }

	private DiagnosticsOptions? Options => _diagnosticsService?.Options;

	private string FieldId(string setting) => $"{_idPrefix}-{setting}";

	/// <inheritdoc />
	protected override void OnInitialized() => _diagnosticsService = DiagnosticsServiceResolver.Resolve(SharedService, Services);

	private async Task NotifyChanged()
	{
		if (OnSettingsChanged.HasDelegate)
		{
			await OnSettingsChanged.InvokeAsync();
		}
	}

	// The controls bind their setting, so a value the setter clamps (a width past 600) shows as clamped.
	private Task SetPosition(OverlayPosition position) => Apply(options => options.Position = position);

	private Task SetPanelWidth(int width) => Apply(options => options.PanelWidth = Math.Clamp(width, 300, 600));

	private Task SetMinLogLevel(LogLevel level) => Apply(options => options.MinConsoleLogLevel = level);

	private Task SetMaxEventLog(int max) => Apply(options => options.MaxEventLogEntries = Math.Clamp(max, 100, 1000));

	private Task SetRenderTracking(bool on) => Apply(options => options.RenderTrackingEnabled = on);

	private Task SetJsInteropTracking(bool on) => Apply(options => options.JsInteropTrackingEnabled = on);

	private Task SetAutoExpand(bool on) => Apply(options => options.StartExpanded = on);

	private async Task SetPaused(bool paused)
	{
		if (_diagnosticsService is not null)
		{
			_diagnosticsService.IsPaused = paused;
			await NotifyChanged();
		}
	}

	private async Task Apply(Action<DiagnosticsOptions> change)
	{
		if (Options is not null)
		{
			change(Options);
			await NotifyChanged();
		}
	}

}
