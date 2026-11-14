namespace Moka.Red.Data.Table;

/// <summary>What a <c>ServerData</c> source hands back to a <see cref="MokaTable{TItem}" />.</summary>
/// <typeparam name="TItem">The row data type.</typeparam>
public sealed record MokaTableResult<TItem>
{
	/// <summary>The rows of the page asked for.</summary>
	public required IReadOnlyList<TItem> Items { get; init; }

	/// <summary>
	///     How many rows match across every page, for the pager. Null when the source doesn't count them, as a cursor-paged
	///     one often doesn't; the table then counts what it was given.
	/// </summary>
	public int? TotalItems { get; init; }

	/// <summary>Under the <see cref="MokaPagerLook.Cursor" /> pager, the cursor for older rows, or null at the oldest.</summary>
	public string? NextCursor { get; init; }

	/// <summary>Under the <see cref="MokaPagerLook.Cursor" /> pager, the cursor for newer rows, or null at the newest.</summary>
	public string? PreviousCursor { get; init; }

	/// <summary>
	///     Totals over the whole result set, by column title, for the footer in place of totals over the rows on this
	///     page. A column missing here totals the rows the table has. See <see cref="MokaTableRequest.Aggregates" />.
	/// </summary>
	public IReadOnlyDictionary<string, double>? Totals { get; init; }

	/// <summary>
	///     The row count and totals of each whole group, for the group headings, which otherwise count only the rows on
	///     this page. See <see cref="MokaTableRequest.Groups" />.
	/// </summary>
	public IReadOnlyList<MokaTableGroupTotals>? GroupTotals { get; init; }

	/// <summary>
	///     Every value of each column asked for in <see cref="MokaTableRequest.Facets" />, with counts, by column title.
	/// </summary>
	public IReadOnlyDictionary<string, IReadOnlyList<MokaFacetValue>>? Facets { get; init; }

	/// <summary>How many rows each quick filter asked for in <see cref="MokaTableRequest.QuickFilterCounts" /> keeps, by name.</summary>
	public IReadOnlyDictionary<string, int>? QuickFilterCounts { get; init; }
}

/// <summary>A total a <see cref="MokaTable{TItem}" /> shows for a column.</summary>
/// <param name="Column">The column's title.</param>
/// <param name="Type">What to total.</param>
public sealed record MokaTableAggregate(string Column, MokaAggregateType Type);

/// <summary>The row count and totals of one whole group, for its heading.</summary>
/// <param name="Path">The group's keys, outermost first: one key for a group of the first level, two for one inside it.</param>
/// <param name="Count">How many rows the whole group holds.</param>
public sealed record MokaTableGroupTotals(IReadOnlyList<string> Path, int Count)
{
	/// <summary>The group's totals by column title. A column missing here totals the group's rows on this page.</summary>
	public IReadOnlyDictionary<string, double>? Totals { get; init; }
}
