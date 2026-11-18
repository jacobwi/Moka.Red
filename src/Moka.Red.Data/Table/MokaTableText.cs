using System.Globalization;
using Moka.Red.Data.Pagination;

namespace Moka.Red.Data.Table;

/// <summary>
///     Every word a <see cref="MokaTable{TItem}" />, its pager and its column kinds write. Replace any of them with
///     <c>with</c>: <c>MokaTableText.Default with { NoData = "Nothing yet" }</c>. A phrase about the rows takes the
///     table's <c>ItemsName</c>, or null when it has none.
/// </summary>
public sealed record MokaTableText
{
	/// <summary>The English words.</summary>
	public static MokaTableText Default { get; } = new();

	/// <summary>The pager's words.</summary>
	public MokaPaginationText Pager { get; init; } = MokaPaginationText.Default;

	/// <summary>What the rows are called when the table has no <c>ItemsName</c>.</summary>
	public string Rows { get; init; } = "rows";

	/// <summary>The search box's placeholder.</summary>
	public string SearchPlaceholder { get; init; } = "Search...";

	/// <summary>The search box's clear button.</summary>
	public string ClearSearch { get; init; } = "Clear search";

	/// <summary>How many rows the search found.</summary>
	public Func<int, string> SearchResults { get; init; } = static count => string.Create(CultureInfo.CurrentCulture, $"{count} results");

	/// <summary>The column toggle button.</summary>
	public string ToggleColumns { get; init; } = "Toggle columns";

	/// <summary>
	///     What a column with no title and no kind of its own is called, in its column menu, the column toggle and its
	///     editor's name: from its place among the columns, from 1.
	/// </summary>
	public Func<int, string> ColumnNumber { get; init; } = static number =>
		string.Create(CultureInfo.CurrentCulture, $"Column {number}");

	/// <summary>The export button.</summary>
	public string Export { get; init; } = "Export";

	/// <summary>The refresh button.</summary>
	public string Refresh { get; init; } = "Refresh";

	/// <summary>The density button and the group of densities it opens.</summary>
	public string Density { get; init; } = "Density";

	/// <summary>The compact density.</summary>
	public string DensityCompact { get; init; } = "Compact";

	/// <summary>The standard density.</summary>
	public string DensityStandard { get; init; } = "Standard";

	/// <summary>The comfortable density.</summary>
	public string DensityComfortable { get; init; } = "Comfortable";

	/// <summary>The column chooser button, from how many columns are hidden.</summary>
	public Func<int, string> ColumnsButton { get; init; } = static hidden =>
		hidden > 0 ? string.Create(CultureInfo.CurrentCulture, $"Columns ({hidden} hidden)") : "Columns";

	/// <summary>The column chooser's name.</summary>
	public string ColumnsGroup { get; init; } = "Columns";

	/// <summary>The note at the top of the column chooser.</summary>
	public string ChooserNote { get; init; } =
		"Show, hide and order the columns. On the table itself, a heading's right edge drags to resize it, and the left and right arrow keys move a focused edge.";

	/// <summary>A column's box in the chooser.</summary>
	public Func<string, string> ShowColumn { get; init; } = static title => $"Show {title}";

	/// <summary>The chooser's button that moves a column earlier.</summary>
	public Func<string, string> MoveEarlier { get; init; } = static title => $"Move {title} earlier";

	/// <summary>The chooser's button that moves a column later.</summary>
	public Func<string, string> MoveLater { get; init; } = static title => $"Move {title} later";

	/// <summary>The chooser's button that gives a dragged column its own width back, from the dragged width.</summary>
	public Func<double, string> WidthReset { get; init; } = static width =>
		string.Create(CultureInfo.CurrentCulture, $"{Math.Round(width)} px, reset");

	/// <summary>The chooser's button that puts every column back.</summary>
	public string PutColumnsBack { get; init; } = "Put the columns back as they were";

	/// <summary>The button that closes the chooser.</summary>
	public string Done { get; init; } = "Done";

	/// <summary>The name of a heading's resizable edge.</summary>
	public Func<string, string> ResizeEdge { get; init; } = static title => $"Width of {title}: left and right arrows, Home for its own";

	/// <summary>The saved views' group.</summary>
	public string SavedViews { get; init; } = "Saved views";

	/// <summary>A view chip's forget button.</summary>
	public Func<string, string> ForgetView { get; init; } = static name => $"Forget the view {name}";

	/// <summary>The view name box's placeholder.</summary>
	public string NameThisView { get; init; } = "Name this view";

	/// <summary>The view name box's name.</summary>
	public string NameForView { get; init; } = "Name for this view";

	/// <summary>The button that saves a view.</summary>
	public string Save { get; init; } = "Save";

	/// <summary>The button that stops naming a view.</summary>
	public string Cancel { get; init; } = "Cancel";

	/// <summary>The button that starts naming a view.</summary>
	public string SaveThisView { get; init; } = "Save this view";

	/// <summary>Why a view name can't be used: another view has it.</summary>
	public Func<string, string> ViewExists { get; init; } = static name => $"There's a view called {name} already.";

	/// <summary>Why there is nothing to save: the table shows what it starts with.</summary>
	public string SaveViewNothing { get; init; } = "Change the table first";

	/// <summary>What saving a view does.</summary>
	public string SaveViewHint { get; init; } = "Keep how the table looks now under a name";

	/// <summary>Why there is nothing to save: a view shows this already.</summary>
	public string SaveViewSame { get; init; } = "A view shows this already";

	/// <summary>A view chip's title, saying what the view does.</summary>
	public Func<MokaTableState, string> ViewSummary { get; init; } = DescribeView;

	/// <summary>How many rows are picked.</summary>
	public Func<int, string> SelectedCount { get; init; } = static count => string.Create(CultureInfo.CurrentCulture, $"{count:N0} selected");

	/// <summary>Every matching row picked, from a source that doesn't count its rows.</summary>
	public string AllSelected { get; init; } = "All selected";

	/// <summary>The button that clears the picked rows, as a word.</summary>
	public string Clear { get; init; } = "Clear";

	/// <summary>The button that clears the picked rows, as an icon's name.</summary>
	public string ClearSelection { get; init; } = "Clear the selection";

	/// <summary>The header's box while more rows can be loaded below: "Select all rows shown".</summary>
	public Func<string, string> SelectAllShown { get; init; } = static rows => $"Select all {rows} shown";

	/// <summary>The header's box on a paged table: "Select all rows on this page".</summary>
	public Func<string, string> SelectAllOnPage { get; init; } = static rows => $"Select all {rows} on this page";

	/// <summary>The header's box on a table that shows every row: "Select all rows".</summary>
	public Func<string, string> SelectAll { get; init; } = static rows => $"Select all {rows}";

	/// <summary>
	///     The header's box under <c>SelectAllMatching</c>, which picks the rows not loaded too: "Select all matching rows".
	/// </summary>
	public Func<string, string> SelectAllMatching { get; init; } = static rows => $"Select all matching {rows}";

	/// <summary>A row's box, from the row's <c>ItemName</c>.</summary>
	public Func<string, string> SelectItem { get; init; } = static name => $"Select {name}";

	/// <summary>The name of the bar over the picked rows, from the table's <c>ItemsName</c>.</summary>
	public Func<string?, string> BulkBar { get; init; } = static rows => rows is null ? "Selected rows" : $"Act on the selected {rows}";

	/// <summary>The picked rows' names when there are too many to list, from the names listed and how many more.</summary>
	public Func<string, int, string> BulkNamesAndMore { get; init; } = static (names, more) =>
		string.Create(CultureInfo.CurrentCulture, $"{names} and {more:N0} more");

	/// <summary>The start toggle that opens a row's detail.</summary>
	public string ToggleDetail { get; init; } = "Toggle detail";

	/// <summary>The end toggle of a closed row, from the row's <c>ItemName</c>.</summary>
	public Func<string?, string> ShowMoreAbout { get; init; } = static name => name is null ? "Show more" : $"Show more about {name}";

	/// <summary>The end toggle of an open row, from the row's <c>ItemName</c>.</summary>
	public Func<string?, string> HideMoreAbout { get; init; } = static name => name is null ? "Hide more" : $"Hide more about {name}";

	/// <summary>The hidden heading over the end toggles.</summary>
	public string More { get; init; } = "More";

	/// <summary>The heading over the row actions.</summary>
	public string Actions { get; init; } = "Actions";

	/// <summary>Why rows can't be dragged.</summary>
	public string ReorderOff { get; init; } = "Clear the sort, search and filters to reorder rows";

	/// <summary>A sort button's name when the heading is a template.</summary>
	public Func<string, string> SortBy { get; init; } = static title => $"Sort by {title}";

	/// <summary>A filter box's placeholder.</summary>
	public string Filter { get; init; } = "Filter";

	/// <summary>A filter box's name.</summary>
	public Func<string, string> FilterBy { get; init; } = static title => $"Filter by {title}";

	/// <summary>The choice in a filter list that filters nothing.</summary>
	public string All { get; init; } = "All";

	/// <summary>An empty table.</summary>
	public string NoData { get; init; } = "No data available";

	/// <summary>The table loading its rows.</summary>
	public string Loading { get; init; } = "Loading...";

	/// <summary>What screen readers hear while the app loads the rows, from the table's <c>ItemsName</c>.</summary>
	public Func<string?, string> LoadingRows { get; init; } = static rows => rows is null ? "Loading rows" : $"Loading {rows}…";

	/// <summary>How many rows are shown of how many, from those, the total and the table's <c>ItemsName</c>.</summary>
	public Func<int, int, string?, string> ShowingCount { get; init; } = static (shown, total, rows) => rows is null
		? string.Create(CultureInfo.CurrentCulture, $"Showing {shown:N0} of {total:N0}")
		: string.Create(CultureInfo.CurrentCulture, $"{shown:N0} of {total:N0} {rows}");

	/// <summary>The button that loads more rows, from how many it loads.</summary>
	public Func<int, string> ShowMoreRows { get; init; } = static count => string.Create(CultureInfo.CurrentCulture, $"Show {count:N0} more");

	/// <summary>The button that loads every row, from the total and the table's <c>ItemsName</c>.</summary>
	public Func<int, string?, string> ShowAll { get; init; } = static (total, rows) =>
		rows is null ? "Show all" : string.Create(CultureInfo.CurrentCulture, $"Show all {total:N0}");

	/// <summary>The label of a sum in the footer.</summary>
	public string Sum { get; init; } = "Sum";

	/// <summary>The label of an average in the footer.</summary>
	public string Average { get; init; } = "Avg";

	/// <summary>The label of a count in the footer.</summary>
	public string Count { get; init; } = "Count";

	/// <summary>The label of a smallest value in the footer.</summary>
	public string Min { get; init; } = "Min";

	/// <summary>The label of a largest value in the footer.</summary>
	public string Max { get; init; } = "Max";

	/// <summary>A yes in a yes or no column.</summary>
	public string Yes { get; init; } = "Yes";

	/// <summary>A no in a yes or no column.</summary>
	public string No { get; init; } = "No";

	/// <summary>The name of a row menu's button, from the row's name, or null when the column names no row.</summary>
	public Func<string?, string> RowMenu { get; init; } = static name => name is null ? "Row actions" : $"Actions for {name}";

	/// <summary>The name of an image cell's button, from the image's name.</summary>
	public Func<string, string> OpenImage { get; init; } = static name => $"Open {name}";

	/// <summary>An image cell's name when it has none of its own.</summary>
	public string Image { get; init; } = "Image";

	/// <summary>A rating in words, from the rating and the most stars: "4 of 5".</summary>
	public Func<double, int, string> RatingOf { get; init; } = static (rating, stars) =>
		string.Create(CultureInfo.CurrentCulture, $"{rating:0.#} of {stars}");

	/// <summary>A copy button's name, from the value it copies.</summary>
	public Func<string, string> CopyValue { get; init; } = static value => $"Copy {value}";

	// ── Filters ──

	/// <summary>An operator's name, and whether the column holds dates, where "more than" reads "after".</summary>
	public Func<MokaFilterOperator, bool, string> OperatorName { get; init; } = DescribeOperator;

	/// <summary>A filter in words, from the column, the operator's name and the values shown: "Age: between 30 and 40".</summary>
	public Func<string, string, IReadOnlyList<string>, string> FilterSummary { get; init; } = DescribeFilter;

	/// <summary>A heading's filter button while its column is filtered, from the title and the filter in words.</summary>
	public Func<string, string, string> FilterOnColumn { get; init; } = static (title, filter) => $"Filter by {title}, now {filter}";

	/// <summary>The name of a column's filter popup, from its title.</summary>
	public Func<string, string> FilterDialog { get; init; } = static title => $"Filter {title}";

	/// <summary>A range's first box in the filter row and the popup.</summary>
	public string RangeFrom { get; init; } = "From";

	/// <summary>A range's second box in the filter row and the popup.</summary>
	public string RangeTo { get; init; } = "To";

	/// <summary>A range's first box's name, from the column's title.</summary>
	public Func<string, string> FilterFrom { get; init; } = static title => $"{title} from";

	/// <summary>A range's second box's name, from the column's title.</summary>
	public Func<string, string> FilterTo { get; init; } = static title => $"{title} to";

	/// <summary>The popup's operator list.</summary>
	public string Operator { get; init; } = "Operator";

	/// <summary>The popup's value box.</summary>
	public string Value { get; init; } = "Value";

	/// <summary>The popup's button that applies the filter.</summary>
	public string Apply { get; init; } = "Apply";

	/// <summary>The popup's button that clears the column's filter.</summary>
	public string ClearFilter { get; init; } = "Clear";

	/// <summary>A list filter or facet with no value to offer.</summary>
	public string NoValues { get; init; } = "No values";

	/// <summary>The name of the chips above the table.</summary>
	public string FiltersInForce { get; init; } = "Filters";

	/// <summary>A chip's remove button, from the filter in words.</summary>
	public Func<string, string> RemoveFilter { get; init; } = static filter => $"Remove the filter {filter}";

	/// <summary>Beside the chips when a row needs to match only one filter.</summary>
	public string MatchingAny { get; init; } = "Matching any";

	/// <summary>The button that clears every filter.</summary>
	public string ClearAll { get; init; } = "Clear all";

	/// <summary>The toolbar button that opens the builder, from how many filters are in force.</summary>
	public Func<int, string> FiltersButton { get; init; } = static count =>
		count > 0 ? string.Create(CultureInfo.CurrentCulture, $"Filters ({count:N0})") : "Filters";

	/// <summary>The builder's name.</summary>
	public string FilterBuilder { get; init; } = "Filter conditions";

	/// <summary>The words before the builder's join choices.</summary>
	public string RowsMatch { get; init; } = "Rows match";

	/// <summary>The join choice where a row matches every condition.</summary>
	public string MatchAll { get; init; } = "all conditions";

	/// <summary>The join choice where a row matches one condition.</summary>
	public string MatchAny { get; init; } = "any condition";

	/// <summary>The builder with no condition yet.</summary>
	public string NoConditions { get; init; } = "No conditions yet.";

	/// <summary>A condition's column list, from its number.</summary>
	public Func<int, string> ConditionColumn { get; init; } = static number =>
		string.Create(CultureInfo.CurrentCulture, $"Column of condition {number}");

	/// <summary>A condition's operator list, from its number.</summary>
	public Func<int, string> ConditionOperator { get; init; } = static number =>
		string.Create(CultureInfo.CurrentCulture, $"Operator of condition {number}");

	/// <summary>A condition's value, from its number.</summary>
	public Func<int, string> ConditionValue { get; init; } = static number =>
		string.Create(CultureInfo.CurrentCulture, $"Value of condition {number}");

	/// <summary>A condition's second value, for a range, from its number.</summary>
	public Func<int, string> ConditionSecondValue { get; init; } = static number =>
		string.Create(CultureInfo.CurrentCulture, $"Second value of condition {number}");

	/// <summary>Between a range's two values.</summary>
	public string And { get; init; } = "and";

	/// <summary>A condition's remove button, from its number.</summary>
	public Func<int, string> RemoveCondition { get; init; } = static number =>
		string.Create(CultureInfo.CurrentCulture, $"Remove condition {number}");

	/// <summary>The builder's button that adds a condition.</summary>
	public string AddCondition { get; init; } = "Add a condition";

	/// <summary>The name of the quick filters.</summary>
	public string QuickFilters { get; init; } = "Show";

	/// <summary>The quick filter that keeps every row.</summary>
	public string AllRows { get; init; } = "All";

	/// <summary>The facet panel's name.</summary>
	public string Facets { get; init; } = "Refine";

	/// <summary>A facet's button that lists every value, from how many there are.</summary>
	public Func<int, string> ShowAllValues { get; init; } = static count => string.Create(CultureInfo.CurrentCulture, $"Show all {count:N0}");

	/// <summary>A facet's button that lists fewer values again.</summary>
	public string ShowFewerValues { get; init; } = "Show fewer";

	// ── States ──

	/// <summary>A search or filters that leave no row, from the search, or null without one.</summary>
	public Func<string?, string> NoResultsFor { get; init; } = static search =>
		search is null ? "No rows match the filters." : $"No results for “{search}”.";

	/// <summary>The button that clears the search and the filters.</summary>
	public string ClearSearchAndFilters { get; init; } = "Clear search and filters";

	/// <summary>A failed load, from the table's <c>ItemsName</c>.</summary>
	public Func<string?, string> LoadFailed { get; init; } = static rows => rows is null ? "Couldn't load the rows" : $"Couldn't load the {rows}";

	/// <summary>The button that loads again after a failure.</summary>
	public string Retry { get; init; } = "Try again";

	// ── Column menu ──

	/// <summary>The menu button of a column with no title and no kind of its own, from its place among the columns.</summary>
	public Func<int, string> ColumnMenuAt { get; init; } = static number =>
		string.Create(CultureInfo.CurrentCulture, $"Options for column {number}");

	/// <summary>A heading's menu button and its menu, from the column's title.</summary>
	public Func<string, string> ColumnMenu { get; init; } = static title => $"{title} column options";

	/// <summary>The menu item that sorts up.</summary>
	public string SortAscending { get; init; } = "Sort ascending";

	/// <summary>The menu item that sorts down.</summary>
	public string SortDescending { get; init; } = "Sort descending";

	/// <summary>The menu item that clears the column's sort.</summary>
	public string ClearSort { get; init; } = "Clear sort";

	/// <summary>The menu item that opens the column's filter.</summary>
	public string FilterColumn { get; init; } = "Filter…";

	/// <summary>The menu item that pins the column to the start.</summary>
	public string PinToStart { get; init; } = "Pin to start";

	/// <summary>The menu item that pins the column to the end.</summary>
	public string PinToEnd { get; init; } = "Pin to end";

	/// <summary>The menu item that unpins the column.</summary>
	public string Unpin { get; init; } = "Unpin";

	/// <summary>The menu item that groups the rows by the column, or stops.</summary>
	public string GroupByColumn { get; init; } = "Group by this column";

	/// <summary>The menu item that hides the column.</summary>
	public string HideColumn { get; init; } = "Hide column";

	/// <summary>The menu item that sizes the column to its content.</summary>
	public string FitToContent { get; init; } = "Fit to content";

	// ── Groups and rows ──

	/// <summary>The bar over a grouped table's name.</summary>
	public string Grouping { get; init; } = "Grouping";

	/// <summary>The words before the grouped columns.</summary>
	public string GroupedBy { get; init; } = "Grouped by";

	/// <summary>A grouped column's remove button, from its title.</summary>
	public Func<string, string> StopGrouping { get; init; } = static title => $"Stop grouping by {title}";

	/// <summary>The button that opens every group.</summary>
	public string ExpandAllGroups { get; init; } = "Expand all";

	/// <summary>The button that folds every group.</summary>
	public string CollapseAllGroups { get; init; } = "Collapse all";

	/// <summary>The button that opens every row's detail.</summary>
	public string ExpandAllRows { get; init; } = "Show detail for every row";

	/// <summary>The button that closes every row's detail.</summary>
	public string CollapseAllRows { get; init; } = "Hide detail for every row";

	// ── Editing ──

	/// <summary>The toolbar button that adds a row.</summary>
	public string AddRow { get; init; } = "Add";

	/// <summary>A row's button that opens it for editing.</summary>
	public string EditRow { get; init; } = "Edit";

	/// <summary>An open row's button that saves it.</summary>
	public string SaveRow { get; init; } = "Save";

	/// <summary>An open row's button that closes it without saving.</summary>
	public string CancelEdit { get; init; } = "Cancel";

	/// <summary>A row's button that deletes it.</summary>
	public string DeleteRow { get; init; } = "Delete";

	/// <summary>The title of the dialog that asks before a row is deleted.</summary>
	public string DeleteRowTitle { get; init; } = "Delete this row?";

	/// <summary>What the dialog that asks before a row is deleted says, from the row's name, or null.</summary>
	public Func<string?, string> DeleteRowMessage { get; init; } = static name =>
		name is null ? "The row will be deleted." : $"{name} will be deleted.";

	/// <summary>The dialog's button that deletes the row.</summary>
	public string Delete { get; init; } = "Delete";

	/// <summary>Why an editor's text can't be saved, from the column's name.</summary>
	public Func<string, string> InvalidValue { get; init; } = static column => $"Enter a valid value for {column}";

	/// <summary>The line under the rows after a save, from the row's name, or null.</summary>
	public Func<string?, string> Saved { get; init; } = static name => name is null ? "Changes saved." : $"Changes to {name} saved.";

	/// <summary>The button that undoes the last save.</summary>
	public string Undo { get; init; } = "Undo";

	/// <summary>The button that closes the line about the last save.</summary>
	public string DismissSaved { get; init; } = "Dismiss";

	/// <summary>Why the last save couldn't be undone.</summary>
	public Func<string, string> UndoFailed { get; init; } = static reason => $"Couldn't undo: {reason}";

	// ── Export and copying ──

	/// <summary>The export menu's item that writes a CSV file.</summary>
	public string ExportCsv { get; init; } = "Export as CSV";

	/// <summary>The export menu's item that writes an Excel workbook.</summary>
	public string ExportExcel { get; init; } = "Export as Excel";

	/// <summary>The name of the sheet in an exported workbook, when the table has no title.</summary>
	public string SheetName { get; init; } = "Rows";

	/// <summary>The button that copies the selected rows, from how many.</summary>
	public Func<int, string> CopyRows { get; init; } = static count =>
		count == 1 ? "Copy row" : string.Create(CultureInfo.CurrentCulture, $"Copy {count:N0} rows");

	/// <summary>What the copy button says once the rows are on the clipboard.</summary>
	public string CopiedRows { get; init; } = "Copied";

	/// <summary>What the copy button says when the clipboard refused.</summary>
	public string CopyFailed { get; init; } = "Couldn't copy";

	/// <summary>What a paste did, from how many cells it changed and how many it couldn't take.</summary>
	public Func<int, int, string> Pasted { get; init; } = static (pasted, refused) => refused == 0
		? string.Create(CultureInfo.CurrentCulture, $"Pasted {pasted:N0} cells.")
		: string.Create(CultureInfo.CurrentCulture, $"Pasted {pasted:N0} cells; {refused:N0} couldn't be pasted.");

	// ── Loading ──

	/// <summary>The row under streamed rows while more arrive, from how many have.</summary>
	public Func<int, string> StreamingRows { get; init; } = static count =>
		string.Create(CultureInfo.CurrentCulture, $"Loading more… {count:N0} so far");

	/// <summary>The button that stops loading streamed rows.</summary>
	public string StopLoading { get; init; } = "Stop";

	/// <summary>What a row still loading in a virtualized server-side table says to screen readers.</summary>
	public string LoadingRow { get; init; } = "Loading";

	private static string DescribeOperator(MokaFilterOperator op, bool dates) => op switch
	{
		MokaFilterOperator.Contains => "contains",
		MokaFilterOperator.DoesNotContain => "does not contain",
		MokaFilterOperator.Is => "is",
		MokaFilterOperator.IsNot => "is not",
		MokaFilterOperator.StartsWith => "starts with",
		MokaFilterOperator.EndsWith => "ends with",
		MokaFilterOperator.IsEmpty => "is empty",
		MokaFilterOperator.IsNotEmpty => "is not empty",
		MokaFilterOperator.GreaterThan => dates ? "is after" : "is more than",
		MokaFilterOperator.AtLeast => dates ? "is on or after" : "is at least",
		MokaFilterOperator.LessThan => dates ? "is before" : "is less than",
		MokaFilterOperator.AtMost => dates ? "is on or before" : "is at most",
		MokaFilterOperator.Between => "is between",
		MokaFilterOperator.IsAnyOf => "is any of",
		MokaFilterOperator.IsNoneOf => "is none of",
		_ => op.ToString()
	};

	private static string DescribeFilter(string column, string op, IReadOnlyList<string> values) => values.Count switch
	{
		0 => $"{column} {op}",
		2 when op == "is between" => $"{column} {op} {values[0]} and {values[1]}",
		_ => $"{column} {op} {string.Join(", ", values)}"
	};

	private static string DescribeView(MokaTableState state)
	{
		List<string> parts = [];
		parts.AddRange(state.Filters.Select(static filter => $"{filter.Column}: {string.Join(", ", filter.Values)}"));
		if (!string.IsNullOrWhiteSpace(state.Search))
		{
			parts.Add($"\"{state.Search}\"");
		}

		if (state.Sorts is { Count: > 0 } sorts)
		{
			parts.Add($"sorted by {string.Join(", ", sorts.Select(static sort => sort.Column))}");
		}

		return parts.Count == 0 ? "Everything" : string.Join(", ", parts);
	}
}
