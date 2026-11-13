namespace Moka.Red.Primitives.List;

/// <summary>How <see cref="MokaList" /> sizes its rows.</summary>
public enum MokaListRowStyle
{
	/// <summary>The list's own rows: medium-weight text, a small icon, at least 32 pixels tall.</summary>
	Standard,

	/// <summary>
	///     Rows sized like <c>MokaTree</c>'s: its text size and weight, its icon size, its row height and its indent, the
	///     icon set in past the tree's toggle column. A list above or below a tree then reads as one sidebar, its icons and
	///     names lined up with the tree's top level.
	/// </summary>
	Tree
}
