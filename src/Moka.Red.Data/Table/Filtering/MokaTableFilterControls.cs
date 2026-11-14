namespace Moka.Red.Data.Table;

/// <summary>
///     Which filter controls a <see cref="MokaTable{TItem}" /> draws. Combine them: every control reads and writes the same
///     filters, so a filter set in a heading's menu shows as a chip and in the builder too. Quick filters are drawn whenever
///     the table has <see cref="MokaTable{TItem}.QuickFilters" />.
/// </summary>
[Flags]
public enum MokaTableFilterControls
{
	/// <summary>No filter controls. Filters still come from the state.</summary>
	None = 0,

	/// <summary>
	///     A row under the headings with an input that fits each filterable column: text, a number range, a date range, a
	///     list, a list of several or yes and no. The default. Drawn only while a shown column is filterable.
	/// </summary>
	Row = 1,

	/// <summary>A filter button in each filterable heading that opens the operator and the value over the table.</summary>
	Menus = 2,

	/// <summary>The filters in force as chips above the table, each with a remove button, and Clear all.</summary>
	Chips = 4,

	/// <summary>A side panel listing the values of each filterable list or yes and no column, with how many rows have each.</summary>
	Facets = 8,

	/// <summary>A toolbar button that opens a panel of conditions, joined by and or by or.</summary>
	Builder = 16
}

/// <summary>How the filters of a <see cref="MokaTable{TItem}" /> combine.</summary>
public enum MokaFilterJoin
{
	/// <summary>A row must pass every filter. The default.</summary>
	All,

	/// <summary>A row must pass at least one filter.</summary>
	Any
}

/// <summary>How a <see cref="MokaTable{TItem}" /> draws its quick filters.</summary>
public enum MokaQuickFilterLook
{
	/// <summary>A segmented control: a group of radio buttons. The default.</summary>
	Segments,

	/// <summary>Tabs along the top of the table.</summary>
	Tabs
}

/// <summary>
///     A preset filter drawn as a segment or a tab above a <see cref="MokaTable{TItem}" />, with how many rows it keeps.
///     Its filters always narrow on top of the others, whatever <see cref="MokaFilterJoin" /> says. Two quick filters are
///     equal when their names and filters match.
/// </summary>
public sealed record MokaTableQuickFilter
{
	/// <summary>A quick filter.</summary>
	/// <param name="name">The name on the segment, and how the state names it. Keep names unique.</param>
	/// <param name="filters">The filters it applies, every one of them.</param>
	public MokaTableQuickFilter(string name, IReadOnlyList<MokaTableFilter> filters)
	{
		Name = name;
		Filters = filters;
	}

	/// <summary>The name on the segment, and how the state names it.</summary>
	public string Name { get; init; }

	/// <summary>The filters it applies, every one of them.</summary>
	public IReadOnlyList<MokaTableFilter> Filters { get; init; }

	/// <inheritdoc />
	public bool Equals(MokaTableQuickFilter? other) =>
		other is not null && string.Equals(Name, other.Name, StringComparison.Ordinal) && Filters.SequenceEqual(other.Filters);

	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(StringComparer.Ordinal.GetHashCode(Name), Filters.Count);
}

/// <summary>One choice in a list filter: the value filters write and the text people read.</summary>
/// <param name="Value">The value, written the way filters write theirs: invariant culture, ISO 8601 dates.</param>
/// <param name="Text">What the choice shows.</param>
public sealed record MokaFilterOption(string Value, string Text);

/// <summary>One value of a column with how many rows have it, for the facet panel and the list filters.</summary>
/// <param name="Value">The value, written the way filters write theirs.</param>
/// <param name="Text">What the value shows.</param>
/// <param name="Count">How many rows have it, counted with every filter but this column's own.</param>
public sealed record MokaFacetValue(string Value, string Text, int Count);
