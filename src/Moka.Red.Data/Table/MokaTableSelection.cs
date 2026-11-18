namespace Moka.Red.Data.Table;

/// <summary>
///     What a <see cref="MokaTable{TItem}" /> has picked: the rows picked one by one, or, under
///     <c>SelectAllMatching</c>, every row that matches the search and the filters, the rows the table never loaded
///     included, but for the rows unticked since.
/// </summary>
/// <typeparam name="TItem">The row data type.</typeparam>
public sealed record MokaTableSelection<TItem>
{
	/// <summary>
	///     Whether every row that matches <see cref="Matching" /> is picked, but for <see cref="Except" />. False when the
	///     rows were picked one by one and <see cref="Items" /> holds them.
	/// </summary>
	public bool AllMatching { get; init; }

	/// <summary>The rows picked one by one, the ones on screen first. Empty while <see cref="AllMatching" />.</summary>
	public IReadOnlyList<TItem> Items { get; init; } = [];

	/// <summary>
	///     Under <see cref="AllMatching" />, the rows unticked since, as the table last loaded them. Identify them by their
	///     key, since the table matches them by its <c>ItemKey</c>.
	/// </summary>
	public IReadOnlyList<TItem> Except { get; init; } = [];

	/// <summary>
	///     How many rows are picked: the source's count less <see cref="Except" /> under <see cref="AllMatching" />, else
	///     <see cref="Items" />' count. Null when every matching row is picked from a source that doesn't count its rows.
	/// </summary>
	public int? Count { get; init; }

	/// <summary>
	///     Under <see cref="AllMatching" />, the request every picked row matches: its search, filters, quick filter and
	///     sorts, from the first row, sized to the count. Run it the way the <c>ServerData</c> source does, without the
	///     paging, to reach every picked row, and leave out <see cref="Except" />. Null otherwise.
	/// </summary>
	public MokaTableRequest? Matching { get; init; }
}
