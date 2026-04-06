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
	[Inject] private IMokaDiagnosticsService? _diagnosticsService { get; set; }

	/// <summary>Callback when any setting changes, so parent can re-render.</summary>
	[Parameter]
	public EventCallback OnSettingsChanged { get; set; }

	private DiagnosticsOptions? Options => _diagnosticsService?.Options;

	private async Task NotifyChanged()
	{
		if (OnSettingsChanged.HasDelegate)
		{
			await OnSettingsChanged.InvokeAsync();
		}
	}

	private async Task HandlePositionChange(ChangeEventArgs e)
	{
		if (Options is null || e.Value is not string value)
		{
			return;
		}

		if (Enum.TryParse(value, out OverlayPosition position))
		{
			Options.Position = position;
			await NotifyChanged();
		}
	}

	private async Task HandlePanelWidthChange(ChangeEventArgs e)
	{
		if (Options is null || e.Value is not string value)
		{
			return;
		}

		if (int.TryParse(value, out int width))
		{
			Options.PanelWidth = Math.Clamp(width, 300, 600);
			await NotifyChanged();
		}
	}

	private async Task HandleMinLogLevelChange(ChangeEventArgs e)
	{
		if (Options is null || e.Value is not string value)
		{
			return;
		}

		if (Enum.TryParse(value, out LogLevel level))
		{
			Options.MinConsoleLogLevel = level;
			await NotifyChanged();
		}
	}

	private async Task HandleMaxEventLogChange(ChangeEventArgs e)
	{
		if (Options is null || e.Value is not string value)
		{
			return;
		}

		if (int.TryParse(value, out int max))
		{
			Options.MaxEventLogEntries = Math.Clamp(max, 100, 1000);
			await NotifyChanged();
		}
	}

	private async Task TogglePause()
	{
		if (_diagnosticsService is not null)
		{
			_diagnosticsService.IsPaused = !_diagnosticsService.IsPaused;
			await NotifyChanged();
		}
	}

	private async Task ToggleRenderTracking(ChangeEventArgs e)
	{
		if (Options is not null && e.Value is bool val)
		{
			Options.RenderTrackingEnabled = val;
			await NotifyChanged();
		}
	}

	private async Task ToggleJsInteropTracking(ChangeEventArgs e)
	{
		if (Options is not null && e.Value is bool val)
		{
			Options.JsInteropTrackingEnabled = val;
			await NotifyChanged();
		}
	}

	private async Task ToggleAutoExpand(ChangeEventArgs e)
	{
		if (Options is not null && e.Value is bool val)
		{
			Options.StartExpanded = val;
			await NotifyChanged();
		}
	}
}
