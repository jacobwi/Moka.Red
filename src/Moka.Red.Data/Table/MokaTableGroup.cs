namespace Moka.Red.Data.Table;

/// <summary>
///     One run of rows a <see cref="MokaTable{TItem}" /> has grouped together, and what its header row shows.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
/// <param name="Key">The value <c>GroupBy</c> returned for every row in the group.</param>
/// <param name="Count">How many rows are in the group on this page.</param>
/// <param name="Collapsed">Whether the group's rows are hidden.</param>
/// <param name="Items">The group's rows, in the order the table had them.</param>
public sealed record MokaTableGroup<TItem>(
	string Key,
	int Count,
	bool Collapsed,
	IReadOnlyList<TItem> Items);
