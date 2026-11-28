using Moka.Red.Core.Icons;

namespace Moka.Red.Feedback.CommandPalette;

/// <summary>A single command/action in the palette.</summary>
public sealed class MokaCommand
{
	/// <summary>Unique identifier.</summary>
	public required string Id { get; init; }

	/// <summary>Display text (searchable).</summary>
	public required string Title { get; init; }

	/// <summary>Optional description (searchable).</summary>
	public string? Description { get; init; }

	/// <summary>Icon shown before title.</summary>
	public MokaIconDefinition? Icon { get; init; }

	/// <summary>Keyboard shortcut hint (e.g., "Ctrl+S").</summary>
	public string? Shortcut { get; init; }

	/// <summary>Category/group name for grouping in the palette.</summary>
	public string? Group { get; init; }

	/// <summary>Action to execute when selected.</summary>
	public Func<Task>? OnExecute { get; init; }

	/// <summary>Sync action alternative.</summary>
	public Action? OnExecuteSync { get; init; }

	/// <summary>Whether this command is currently disabled.</summary>
	public bool Disabled { get; init; }

	/// <summary>Keywords for search matching (not displayed).</summary>
	public string? Keywords { get; init; }

	/// <summary>
	///     URL to navigate to when the command is selected. Navigation happens after
	///     <see cref="OnExecute" /> or <see cref="OnExecuteSync" /> if either is also set,
	///     so a command can do work and then route. A <c>javascript:</c>, <c>vbscript:</c> or <c>data:</c>
	///     URL is never navigated to. With a <see cref="Target" /> that opens another tab, the page opens there instead.
	/// </summary>
	public string? Href { get; init; }

	/// <summary>
	///     Where <see cref="Href" /> opens: <c>_blank</c> for a new tab, or a window's name. Such a command is drawn as a
	///     link with <c>rel="noopener noreferrer"</c> and an external-link mark, so a click, Ctrl+click and a middle click
	///     do what they do on any link. Enter opens the tab as soon as the palette hears the key, while the browser still
	///     counts it as the person's press (five seconds in Chromium, Firefox and WebKit). <see cref="OnExecute" /> or
	///     <see cref="OnExecuteSync" /> still runs. Null, <c>_self</c>, <c>_parent</c> and <c>_top</c> navigate in the page as
	///     <see cref="Href" /> always has.
	/// </summary>
	public string? Target { get; init; }

	/// <summary>
	///     Commands one level down. Picking a command that has them opens them in the palette in its place, the way "Change
	///     theme" can open a list of themes: the search box clears, the command's title shows before it, and what is typed
	///     searches only these. Escape, or Backspace in an empty search box, goes back a level. Such a command runs nothing
	///     and goes nowhere itself, so <see cref="OnExecute" />, <see cref="OnExecuteSync" /> and <see cref="Href" /> are
	///     ignored. An empty list still opens, showing the palette's no-results text.
	/// </summary>
	public IReadOnlyList<MokaCommand>? Children { get; init; }
}
