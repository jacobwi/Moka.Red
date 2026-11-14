namespace Moka.Red.Data.Table;

/// <summary>
///     One group of rows a <see cref="MokaTable{TItem}" /> has gathered together, and what its heading row shows.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
/// <param name="Key">The group's own key: the text, or the column's <c>GroupKey</c>, every row in the group shares.</param>
/// <param name="Count">
///     How many rows the whole group holds: across every page for rows in memory, as the server's
///     <see cref="MokaTableResult{TItem}.GroupTotals" /> say under a server-side source, else the rows on this page.
/// </param>
/// <param name="Collapsed">Whether the group's rows are hidden.</param>
/// <param name="Items">The group's rows the table has, in order: every row of the group for rows in memory.</param>
public sealed record MokaTableGroup<TItem>(
	string Key,
	int Count,
	bool Collapsed,
	IReadOnlyList<TItem> Items)
{
	/// <summary>
	///     What joins a nested group's keys into its <see cref="Id" />: U+001F, which no key holds. The state's
	///     <see cref="MokaTableState.CollapsedGroups" /> and a table's <c>InitiallyCollapsedGroups</c> name groups this way;
	///     a group of the first level is named by its key alone.
	/// </summary>
	public const char IdSeparator = '\u001F';

	/// <summary>How deep the group sits: 0 for the outermost groups.</summary>
	public int Level { get; init; }

	/// <summary>The group's keys and its parents', outermost first.</summary>
	public IReadOnlyList<string> Path { get; init; } = [];

	/// <summary>The group's keys and its parents' joined by <see cref="IdSeparator" />: how the state names it.</summary>
	public string Id => string.Join(IdSeparator, Path);

	/// <summary>The group's totals by column title, for the columns that total anything, as the footer would write them.</summary>
	public IReadOnlyDictionary<string, string> Totals { get; init; } = new Dictionary<string, string>();
}
