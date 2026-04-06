namespace Moka.Red.Data.Table;

/// <summary>
///     State passed to the ServerData callback. Contains current sort, page, search info.
/// </summary>
public sealed record MokaTableState
{
	/// <summary>Current page number (1-indexed).</summary>
	public int Page { get; init; } = 1;

	/// <summary>Number of items per page.</summary>
	public int PageSize { get; init; } = 10;

	/// <summary>Current search term, if any.</summary>
	public string? SearchTerm { get; init; }

	/// <summary>Currently sorted column title, if any.</summary>
	public string? SortColumn { get; init; }

	/// <summary>Current sort direction.</summary>
	public MokaSortDirection SortDirection { get; init; } = MokaSortDirection.None;

	/// <summary>All active sort descriptors for multi-column sort.</summary>
	public IReadOnlyList<MokaTableSortDescriptor> SortDescriptors { get; init; } = [];
}
