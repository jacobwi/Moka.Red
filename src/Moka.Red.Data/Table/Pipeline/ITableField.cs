namespace Moka.Red.Data.Table;

/// <summary>What the data pipeline needs from a column: its name, its value, its text and how it sorts and groups.</summary>
/// <typeparam name="TItem">The row data type.</typeparam>
internal interface ITableField<TItem>
{
	/// <summary>The title sorts, filters and groups name the column by.</summary>
	string? Title { get; }

	/// <summary>Whether the column has a value to search, filter, sort and group by.</summary>
	bool HasValue { get; }

	/// <summary>Orders two rows in place of their values, when the column has its own way.</summary>
	Func<TItem, TItem, int>? Comparer { get; }

	/// <summary>The row's value: what sorts, the value filters and the totals read.</summary>
	object? ValueOf(TItem item);

	/// <summary>The row's text as the cell shows it: what the search and the text filters read.</summary>
	string TextOf(TItem item);

	/// <summary>The heading a row is grouped under.</summary>
	string GroupKeyOf(TItem item);
}
