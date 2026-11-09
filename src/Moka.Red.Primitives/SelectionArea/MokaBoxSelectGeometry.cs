namespace Moka.Red.Primitives.SelectionArea;

/// <summary>
///     Describes items laid out on lines of equal height, with the same number of columns on every line, so a box can
///     cover items that aren't drawn, such as the rows <c>Virtualize</c> leaves out. The box then reports the lines and
///     columns it covers as a <see cref="MokaBoxSelectRange" />. Every drawn item carries its index in
///     <see cref="IndexAttribute" />, and the positions of the drawn ones place the rest.
/// </summary>
/// <param name="Count">How many items there are, drawn or not.</param>
public sealed record MokaBoxSelectGeometry(int Count)
{
	/// <summary>Items on each line. Default 1, a list.</summary>
	public int Columns { get; init; } = 1;

	/// <summary>
	///     The distance from the top of one line to the top of the next, in CSS pixels. Null measures it from the drawn
	///     items.
	/// </summary>
	public double? LineHeight { get; init; }

	/// <summary>The attribute each drawn item carries its index in. Default <c>data-moka-select-index</c>.</summary>
	public string IndexAttribute { get; init; } = "data-moka-select-index";

	/// <summary>
	///     What <see cref="IndexAttribute" /> holds on the first item. Default 0. It is 1 for <c>aria-posinset</c>, and
	///     2 for <c>aria-rowindex</c> under a header row.
	/// </summary>
	public int IndexBase { get; init; }
}
