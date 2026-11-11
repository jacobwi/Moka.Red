namespace Moka.Red.Primitives.DataList;

/// <summary>
///     How a <see cref="MokaDataList" /> draws its pairs. Every look keeps the <c>dl</c>, <c>dt</c> and <c>dd</c>
///     elements, so a screen reader reads each term with its value whichever is picked.
/// </summary>
public enum MokaDataListLook
{
	/// <summary>
	///     The list as it has always been: <see cref="MokaDataList.Orientation" /> decides whether a value sits under its
	///     term or beside it.
	/// </summary>
	Standard,

	/// <summary>
	///     The new design's own sizes: a muted term in a 9.5rem column, the value beside it at body size, room above and
	///     below, hairlines between. A value moves under its term when there isn't room beside it.
	/// </summary>
	SettingsRows,

	/// <summary>
	///     The list in a panel of its own, terms on the left and values on the right, like a phone's settings. Reads as one
	///     block on a page with several lists.
	/// </summary>
	InsetPanel,

	/// <summary>
	///     Each pair a small tile, a micro label over a bigger value, as many tiles to a row as fit. For a summary at the
	///     top of a page. An item marked <see cref="MokaDataListItem.Wide" /> takes a whole row.
	/// </summary>
	FactTiles,

	/// <summary>
	///     Dotted leaders run from each term to its value on the right, with even-width digits, like a printed spec sheet.
	/// </summary>
	SpecSheet,

	/// <summary>
	///     Everything in mono: lowercase keys with a colon in a fixed column, an accent edge, square corners. An empty value
	///     reads "- not set", and an item's icon and help are left out.
	/// </summary>
	Terminal
}
