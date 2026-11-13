namespace Moka.Red.ContextMenu;

/// <summary>How a context menu draws the text of its rows on the new design.</summary>
public enum MokaContextMenuItemStyle
{
	/// <summary>Medium weight in full ink, as the menu has always been drawn.</summary>
	Standard,

	/// <summary>
	///     Regular weight in the second, softer ink, at the menu's font size. The lit row, under the mouse or the
	///     keyboard's, takes full ink, so it still stands out.
	/// </summary>
	Quiet
}
