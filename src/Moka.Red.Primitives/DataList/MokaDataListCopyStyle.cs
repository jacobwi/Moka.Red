namespace Moka.Red.Primitives.DataList;

/// <summary>How a <see cref="MokaDataList" /> shows that a <see cref="MokaDataListItem.Copyable" /> value can be copied.</summary>
public enum MokaDataListCopyStyle
{
	/// <summary>A small copy button after the value that stays faint until its row is hovered or it has focus.</summary>
	Faint,

	/// <summary>A small outlined copy button that's always there, so nobody has to find it. Better on touch screens.</summary>
	Always,

	/// <summary>
	///     The value itself is the button: hovering it raises it and shows a copy icon, and a click copies. Its name says
	///     what it copies and the value. A value holding a link or another control can't be a button, so leave those on
	///     another style.
	/// </summary>
	ClickValue
}
