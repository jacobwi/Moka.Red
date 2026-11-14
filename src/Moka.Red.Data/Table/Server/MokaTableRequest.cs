namespace Moka.Red.Data.Table;

/// <summary>
///     What a <see cref="MokaTable{TItem}" /> asks its <c>ServerData</c> source for: a page of rows, sorted, filtered and
///     searched as the table stands.
/// </summary>
public sealed record MokaTableRequest
{
	/// <summary>The page asked for, from 1.</summary>
	public int Page { get; init; } = 1;

	/// <summary>
	///     How many rows a page holds. While the table's pager is hidden (<c>ShowPagination</c> is false), <see cref="Page" />
	///     is 1 and this is sized to cover the whole result set.
	/// </summary>
	public int PageSize { get; init; } = 10;

	/// <summary>
	///     The position of the first row asked for, from 0: <c>(Page - 1) * PageSize</c> for a page, or the start of the
	///     window of rows a virtualized table scrolled to (<c>Virtualize</c> with <c>ServerData</c>), which needn't fall on a
	///     page's edge. Read rows by this and <see cref="PageSize" /> and every request is answered right.
	/// </summary>
	public int StartIndex { get; init; }

	/// <summary>The text in the search box, or null for none.</summary>
	public string? Search { get; init; }

	/// <summary>The sorts in force, first sort first, each naming a column by its title.</summary>
	public IReadOnlyList<MokaTableSort> Sorts { get; init; } = [];

	/// <summary>
	///     The column filters in force, each naming a column by its title. The source applies them itself: the table
	///     doesn't filter rows it didn't load.
	/// </summary>
	public IReadOnlyList<MokaTableFilter> Filters { get; init; } = [];

	/// <summary>How the <see cref="Filters" /> combine: every one, or at least one.</summary>
	public MokaFilterJoin FilterJoin { get; init; }

	/// <summary>
	///     The quick filter picked, or null for all rows. Its filters narrow on top of <see cref="Filters" />, whatever
	///     <see cref="FilterJoin" /> says.
	/// </summary>
	public MokaTableQuickFilter? QuickFilter { get; init; }

	/// <summary>
	///     The titles of the columns the rows are grouped by, outermost first. Order the rows by them before the sorts, so
	///     each group's rows come together across pages, and send <see cref="MokaTableResult{TItem}.GroupTotals" /> for
	///     the counts and totals of whole groups.
	/// </summary>
	public IReadOnlyList<string> Groups { get; init; } = [];

	/// <summary>
	///     The totals the footer and the group headings show, by column. Send them for the whole result set in
	///     <see cref="MokaTableResult{TItem}.Totals" /> and per group in <see cref="MokaTableResult{TItem}.GroupTotals" />.
	///     Empty when no column totals anything.
	/// </summary>
	public IReadOnlyList<MokaTableAggregate> Aggregates { get; init; } = [];

	/// <summary>
	///     The columns the table wants every value of, with counts, for its facet panel and list filters. Empty unless the
	///     table's <c>FacetsFromServer</c> is on. Answer in <see cref="MokaTableResult{TItem}.Facets" />, counting each
	///     column with every filter but its own, so its other values still show what they would give.
	/// </summary>
	public IReadOnlyList<string> Facets { get; init; } = [];

	/// <summary>
	///     The quick filters the table wants counts for, under <c>FacetsFromServer</c>. Answer in
	///     <see cref="MokaTableResult{TItem}.QuickFilterCounts" />, counting each with the search and the filters but no
	///     other quick filter.
	/// </summary>
	public IReadOnlyList<MokaTableQuickFilter> QuickFilterCounts { get; init; } = [];

	/// <summary>
	///     Under the <see cref="MokaPagerLook.Cursor" /> pager, the cursor the source handed out for the page wanted:
	///     its <see cref="MokaTableResult{TItem}.NextCursor" /> for older rows or
	///     <see cref="MokaTableResult{TItem}.PreviousCursor" /> for newer ones. Null asks for the newest page.
	/// </summary>
	public string? Cursor { get; init; }

	/// <summary>
	///     Cancelled once this request isn't wanted any more: the table has asked for something newer, or has gone away.
	///     Pass it on to whatever fetches the rows. The table ignores the answer to a replaced request either way, so this
	///     only saves the work.
	/// </summary>
	public CancellationToken CancellationToken { get; init; }
}
