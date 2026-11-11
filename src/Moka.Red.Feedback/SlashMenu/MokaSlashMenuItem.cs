using Moka.Red.Core.Icons;

namespace Moka.Red.Feedback.SlashMenu;

/// <summary>
///     A single entry in a <see cref="MokaSlashMenu" />.
///     The filter query matches case-insensitively against <see cref="Title" />,
///     <see cref="Keywords" />, and <see cref="Category" />.
/// </summary>
public sealed record MokaSlashMenuItem
{
	/// <summary>Display title shown on the row. Matched against the filter query.</summary>
	public required string Title { get; init; }

	/// <summary>Optional one-line description shown under the title (ellipsized).</summary>
	public string? Description { get; init; }

	/// <summary>
	///     Optional group name. Items that share a category are listed together under it as a heading, groups in
	///     the order their first item appears, and items without one come first with no heading. Matched against
	///     the filter query.
	/// </summary>
	public string? Category { get; init; }

	/// <summary>Optional icon, drawn in a small tile before the title.</summary>
	public MokaIconDefinition? Icon { get; init; }

	/// <summary>
	///     Optional emoji (or a character or two) drawn where the icon goes, in the emoji font, so a row can show an emoji
	///     in place of an icon. Wins over <see cref="Icon" />. Hidden from screen readers, so say what it is in
	///     <see cref="Title" /> or <see cref="Description" />. Not matched against the filter query.
	/// </summary>
	public string? Glyph { get; init; }

	/// <summary>
	///     Optional text the user could type instead of picking the item, such as <c>"##"</c> for a second-level
	///     heading, drawn on the right of the row as key caps. A key combination such as <c>"Ctrl+Alt+2"</c> is
	///     drawn as one cap per key. Not matched against the filter query.
	/// </summary>
	public string? Shortcut { get; init; }

	/// <summary>Optional extra search terms matched against the filter query but never displayed.</summary>
	public string? Keywords { get; init; }

	/// <summary>Optional consumer payload (e.g. the snippet to insert) carried through to the selection callback.</summary>
	public object? Value { get; init; }
}
