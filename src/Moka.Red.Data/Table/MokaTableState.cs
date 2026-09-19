namespace Moka.Red.Data.Table;

/// <summary>
///     State passed to the ServerData callback. Contains current sort, page, search info.
/// </summary>
public sealed record MokaTableState
{
	/// <summary>Current page number (1-indexed).</summary>
	public int Page { get; init; } = 1;

	/// <summary>
	///     Number of items per page. While the table's pager is hidden
	///     (<see cref="MokaTable{TItem}.ShowPagination" /> is false), <see cref="Page" /> is 1 and this
	///     is sized to cover the whole result set.
	/// </summary>
	public int PageSize { get; init; } = 10;

	/// <summary>Current search term, if any.</summary>
	public string? SearchTerm { get; init; }

	/// <summary>Currently sorted column title, if any.</summary>
	public string? SortColumn { get; init; }

	/// <summary>Current sort direction.</summary>
	public MokaSortDirection SortDirection { get; init; } = MokaSortDirection.None;

	/// <summary>All active sort descriptors for multi-column sort.</summary>
	public IReadOnlyList<MokaTableSortDescriptor> SortDescriptors { get; init; } = [];

	/// <summary>
	///     Active column filter values keyed by column title, from the filter row rendered when
	///     <see cref="MokaTable{TItem}.ShowFilters" /> is true. Empty when no filter is set.
	///     A server-side data source has to apply these itself; the table does not filter
	///     rows it did not load.
	/// </summary>
	public IReadOnlyDictionary<string, string> ColumnFilters { get; init; } =
		new Dictionary<string, string>(StringComparer.Ordinal);
}
