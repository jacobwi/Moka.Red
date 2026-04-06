namespace Moka.Red.ContextMenu;

/// <summary>Static helper to create divider items.</summary>
public static class MokaContextMenuItems
{
	/// <summary>Creates a divider (horizontal rule) in the menu.</summary>
	public static MokaContextMenuItem Divider() => new() { Text = "", DividerBefore = true };
}
