namespace Moka.Red.Feedback.CommandPalette;

/// <summary>
///     The words a <see cref="MokaCommandPalette" /> writes besides the ones it has parameters for. Set the ones to change,
///     or all of them to translate it. Anything holding a name is a function, so the words can go around it.
/// </summary>
public sealed record MokaCommandPaletteText
{
	/// <summary>The footer hint beside the arrow keys.</summary>
	public string Navigate { get; init; } = "Navigate";

	/// <summary>The footer hint beside Enter.</summary>
	public string Select { get; init; } = "Select";

	/// <summary>The footer hint beside Escape at the top level.</summary>
	public string Close { get; init; } = "Close";

	/// <summary>
	///     The footer hint beside Escape while a command's <see cref="MokaCommand.Children" /> are showing, where Escape goes
	///     back a level.
	/// </summary>
	public string Back { get; init; } = "Back";

	/// <summary>The heading over the commands run most recently, shown with <see cref="MokaCommandPalette.ShowRecent" />.</summary>
	public string Recent { get; init; } = "Recent";

	/// <summary>
	///     What screen readers hear when a command's <see cref="MokaCommand.Children" /> open, given its title. Going back to
	///     the top level says the palette's <see cref="MokaCommandPalette.Label" />.
	/// </summary>
	public Func<string, string> PageOpened { get; init; } = title => title;

	/// <summary>
	///     What a shortcut list such as <c>MokaCheatsheet</c> shows for the palette's
	///     <see cref="MokaCommandPalette.Shortcut" />, when the palette registers it with the app's hotkeys.
	/// </summary>
	public string ShortcutDescription { get; init; } = "Open the command palette";
}
