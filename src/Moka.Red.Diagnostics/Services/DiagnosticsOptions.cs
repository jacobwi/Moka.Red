using Microsoft.Extensions.Logging;

namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Configuration options for the Moka.Red diagnostics overlay. <see cref="Position" />, <see cref="PanelWidth" />,
///     <see cref="MaxEventLogEntries" />, <see cref="RenderTrackingEnabled" /> and <see cref="JsInteropTrackingEnabled" />
///     are where each window's <see cref="DiagnosticsSettings" /> start; the Settings tab changes those, never these.
/// </summary>
public sealed class DiagnosticsOptions
{
	/// <summary>
	///     Keyboard shortcut to toggle the overlay panel.
	///     Format: modifier keys joined by '+', e.g. "Ctrl+Shift+D".
	/// </summary>
	public string KeyboardShortcut { get; set; } = "Ctrl+Shift+D";

	/// <summary>
	///     Screen corner where the overlay badge and panel appear, until a window picks another.
	/// </summary>
	public OverlayPosition Position { get; set; } = OverlayPosition.BottomRight;

	/// <summary>
	///     Whether the overlay panel starts in expanded state.
	/// </summary>
	public bool StartExpanded { get; set; }

	/// <summary>
	///     Width of the diagnostics panel in pixels, until a window picks another. The Settings tab offers 300 to 600.
	/// </summary>
	public int PanelWidth { get; set; } = 420;

	/// <summary>
	///     Minimum log level to capture in the console buffer. The app has one buffer, so this setting is shared too: a
	///     change from the Settings tab applies to every window from the next message on.
	/// </summary>
	public LogLevel MinConsoleLogLevel { get; set; } = LogLevel.Debug;

	/// <summary>
	///     Maximum number of events each window's event log retains, until the window picks another. The Settings tab
	///     offers 100 to 1000.
	/// </summary>
	public int MaxEventLogEntries { get; set; } = 500;

	/// <summary>
	///     Whether component render tracking starts enabled in each window.
	/// </summary>
	public bool RenderTrackingEnabled { get; set; } = true;

	/// <summary>
	///     Whether JS interop call tracking starts enabled in each window.
	/// </summary>
	public bool JsInteropTrackingEnabled { get; set; } = true;

	/// <summary>
	///     Turns the diagnostics on in environments other than <c>Development</c>. Off by default: the Console tab shows
	///     the app's logs, which on Blazor Server are every user's, so outside Development the overlay, its panels and
	///     the <c>/moka-diagnostics</c> page render nothing, and nothing is recorded or captured. A host that registers
	///     no environment (Blazor Hybrid, tests) runs them whenever <c>AddMokaDiagnostics()</c> was called.
	/// </summary>
	public bool EnableOutsideDevelopment { get; set; }
}

/// <summary>
///     Screen corner position for the diagnostics overlay.
/// </summary>
public enum OverlayPosition
{
	/// <summary>The bottom right corner.</summary>
	BottomRight,

	/// <summary>The bottom left corner.</summary>
	BottomLeft,

	/// <summary>The top right corner.</summary>
	TopRight,

	/// <summary>The top left corner.</summary>
	TopLeft
}
