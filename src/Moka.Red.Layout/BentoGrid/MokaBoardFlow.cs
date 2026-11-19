namespace Moka.Red.Layout.BentoGrid;

/// <summary>How a <see cref="MokaBentoGrid" /> board lays out its cards.</summary>
public enum MokaBoardFlow
{
	/// <summary>
	///     Cards sit on a grid of columns, and a later card moves up into a hole a hidden card, or a wide one that
	///     doesn't fit, leaves. The board has no holes, but a card can show ahead of one that comes before it.
	/// </summary>
	Dense,

	/// <summary>
	///     Cards keep their order, row after row. Each starts from its size's share of a row, and the cards of a row
	///     grow into the room it has left, in proportion to their sizes. A hidden card, or one that doesn't fit, leaves
	///     no hole and moves no card ahead of another.
	/// </summary>
	Rows
}
