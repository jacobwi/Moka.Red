using System.Text.Json;

namespace Moka.Red.Data.Table;

/// <summary>
///     Everything someone can change on a <see cref="MokaTable{TItem}" /> and expect to find again: the sort, the filters,
///     the search, the page, the page size, the density, how the columns are arranged, the grouping and the rows they
///     opened. Bind it with <c>@bind-State</c>, keep it with <see cref="MokaTable{TItem}.StorageKey" />, or save it as a
///     <see cref="MokaTableView" />. Columns are named by their <c>Title</c>.
/// </summary>
/// <remarks>
///     A property left null keeps the table's default for it, so a state that says only <see cref="Sorts" /> sorts the
///     table and leaves the rest as its parameters set it. The table always reports a state with every property filled
///     in. Two states are equal when they say the same thing, whichever lists hold it, so a bound parent handing back
///     what the table reported changes nothing. It serializes to JSON as it is; <see cref="ToJson" /> and
///     <see cref="FromJson" /> do it without reflection, so trimmed apps can store it.
/// </remarks>
public sealed record MokaTableState
{
	/// <summary>The sorts in force, first sort first. Null keeps the columns' <c>DefaultSort</c>; empty is unsorted.</summary>
	public IReadOnlyList<MokaTableSort>? Sorts { get; init; }

	/// <summary>The column filters in force. Empty filters nothing.</summary>
	public IReadOnlyList<MokaTableFilter> Filters { get; init; } = [];

	/// <summary>How the <see cref="Filters" /> combine: every one, the default, or at least one.</summary>
	public MokaFilterJoin FilterJoin { get; init; }

	/// <summary>The name of the quick filter picked, or null for all rows.</summary>
	public string? QuickFilter { get; init; }

	/// <summary>The text in the search box, or null for none.</summary>
	public string? Search { get; init; }

	/// <summary>The page shown, from 1.</summary>
	public int Page { get; init; } = 1;

	/// <summary>Rows per page. Null keeps the table's <c>PageSize</c>.</summary>
	public int? PageSize { get; init; }

	/// <summary>How tightly rows are spaced. Null keeps the table's <c>Density</c>.</summary>
	public MokaTableDensity? Density { get; init; }

	/// <summary>
	///     How the columns are arranged, in display order: hidden ones, dragged widths and pins. Empty means the columns
	///     as their parameters set them. A title no column has is kept, so a column that comes and goes keeps its place.
	/// </summary>
	public IReadOnlyList<MokaTableColumnState> Columns { get; init; } = [];

	/// <summary>The titles of the columns the rows are grouped by. Null keeps the table's <c>GroupBy</c>; empty is ungrouped.</summary>
	public IReadOnlyList<string>? Groups { get; init; }

	/// <summary>The groups someone folded away, by key.</summary>
	public IReadOnlyList<string> CollapsedGroups { get; init; } = [];

	/// <summary>
	///     The rows whose detail is open, by <c>ItemKey</c> written in the invariant culture. Only a table with an
	///     <c>ItemKey</c> can say which rows these are.
	/// </summary>
	public IReadOnlyList<string> Expanded { get; init; } = [];

	/// <summary>Writes the state as JSON, with no reflection.</summary>
	/// <returns>The JSON text.</returns>
	public string ToJson() => JsonSerializer.Serialize(this, MokaTableStateJson.Default.MokaTableState);

	/// <summary>Reads a state written by <see cref="ToJson" />.</summary>
	/// <param name="json">The JSON text.</param>
	/// <returns>The state, or null when the text is empty or isn't a table state.</returns>
	public static MokaTableState? FromJson(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}

		try
		{
			return JsonSerializer.Deserialize(json, MokaTableStateJson.Default.MokaTableState);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	/// <summary>
	///     Whether two states show the same view of the rows: the same sorts, filters, quick filter, search, page size,
	///     columns and grouping. The page, the density and what is folded or opened don't count.
	/// </summary>
	/// <param name="other">The state to compare with.</param>
	/// <returns>True when both say the same about the view.</returns>
	public bool SameView(MokaTableState? other) =>
		other is not null
		&& SameSorts(Sorts, other.Sorts)
		&& Filters.SequenceEqual(other.Filters)
		&& FilterJoin == other.FilterJoin
		&& string.Equals(QuickFilter, other.QuickFilter, StringComparison.Ordinal)
		&& string.Equals(Search ?? string.Empty, other.Search ?? string.Empty, StringComparison.Ordinal)
		&& PageSize == other.PageSize
		&& Columns.SequenceEqual(other.Columns)
		&& SameList(Groups, other.Groups);

	/// <inheritdoc />
	public bool Equals(MokaTableState? other) =>
		other is not null
		&& SameView(other)
		&& Page == other.Page
		&& Density == other.Density
		&& CollapsedGroups.ToHashSet(StringComparer.Ordinal).SetEquals(other.CollapsedGroups)
		&& Expanded.ToHashSet(StringComparer.Ordinal).SetEquals(other.Expanded);

	/// <inheritdoc />
	public override int GetHashCode() =>
		HashCode.Combine(Sorts?.Count ?? -1, Filters.Count, Page, PageSize, Density, Columns.Count, Groups?.Count ?? -1);

	private static bool SameSorts(IReadOnlyList<MokaTableSort>? a, IReadOnlyList<MokaTableSort>? b) =>
		a is null ? b is null : b is not null && a.SequenceEqual(b);

	private static bool SameList(IReadOnlyList<string>? a, IReadOnlyList<string>? b) =>
		a is null ? b is null : b is not null && a.SequenceEqual(b, StringComparer.Ordinal);
}
