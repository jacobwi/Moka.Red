namespace Moka.Red.Core.Keys;

/// <summary>What a key press belongs to before any page-level shortcut. Read from <see cref="MokaKeyEventArgs.Owner" />.</summary>
public enum MokaKeyOwner
{
	/// <summary>Nothing else uses the key, so it is free for the page.</summary>
	None = 0,

	/// <summary>It was pressed in something that takes typing: a text field, a select, a text area or editable content.</summary>
	Field = 1,

	/// <summary>
	///     The focused control uses the key itself: Space and Enter on a button, a link, a check box or a menu item; the
	///     arrows, Home, End and Page Up/Down in a slider, a list, a menu, tabs, a tree or a grid.
	/// </summary>
	Control = 2,

	/// <summary>It was pressed inside an open modal (a dialog, a bottom sheet, an overlay drawer), which the page sits behind.</summary>
	Modal = 3
}
