using Moka.Red.Core.Icons;

namespace Moka.Red.Feedback.Toast;

/// <summary>
///     Configuration options for an individual toast notification.
/// </summary>
public sealed class MokaToastOptions
{
	/// <summary>Optional title displayed in bold above the message.</summary>
	public string? Title { get; set; }

	/// <summary>
	///     Duration in milliseconds before auto-dismissal. Set to 0 for persistent toasts.
	///     Defaults to 5000ms.
	/// </summary>
	public int DurationMs { get; set; } = 5000;

	/// <summary>Whether to show the close (X) button. Defaults to true.</summary>
	public bool ShowCloseButton { get; set; } = true;

	/// <summary>Whether to show the severity icon. Defaults to true.</summary>
	public bool ShowIcon { get; set; } = true;

	/// <summary>Custom icon override. When null, the severity-mapped icon is used.</summary>
	public MokaIconDefinition? CustomIcon { get; set; }

	/// <summary>Callback invoked when the toast body is clicked.</summary>
	public Action? OnClick { get; set; }

	/// <summary>Text for an optional action button displayed in the toast.</summary>
	public string? ActionText { get; set; }

	/// <summary>Callback invoked when the action button is clicked.</summary>
	public Action? OnAction { get; set; }
}
