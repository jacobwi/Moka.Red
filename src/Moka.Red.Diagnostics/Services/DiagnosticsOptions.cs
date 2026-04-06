using Microsoft.Extensions.Logging;

namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Configuration options for the Moka.Red diagnostics overlay.
/// </summary>
public sealed class DiagnosticsOptions
{
	/// <summary>
	///     Keyboard shortcut to toggle the overlay panel.
	///     Format: modifier keys joined by '+', e.g. "Ctrl+Shift+D".
	/// </summary>
	public string KeyboardShortcut { get; set; } = "Ctrl+Shift+D";

	/// <summary>
	///     Screen corner where the overlay badge and panel appear.
	/// </summary>
	public OverlayPosition Position { get; set; } = OverlayPosition.BottomRight;

	/// <summary>
	///     Whether the overlay panel starts in expanded state.
	/// </summary>
	public bool StartExpanded { get; set; }

	/// <summary>
	///     Width of the diagnostics panel in pixels (300-600).
	/// </summary>
	public int PanelWidth { get; set; } = 420;

	/// <summary>
	///     Minimum log level to capture in the console buffer.
	/// </summary>
	public LogLevel MinConsoleLogLevel { get; set; } = LogLevel.Debug;

	/// <summary>
	///     Maximum number of events retained in the event log (100-1000).
	/// </summary>
	public int MaxEventLogEntries { get; set; } = 500;

	/// <summary>
	///     Whether component render tracking is enabled.
	/// </summary>
	public bool RenderTrackingEnabled { get; set; } = true;

	/// <summary>
	///     Whether JS interop call tracking is enabled.
	/// </summary>
	public bool JsInteropTrackingEnabled { get; set; } = true;
}

/// <summary>
///     Screen corner position for the diagnostics overlay.
/// </summary>
public enum OverlayPosition
{
	BottomRight,
	BottomLeft,
	TopRight,
	TopLeft
}
