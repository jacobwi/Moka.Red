using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Interactions;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Table;

/// <summary>
///     Feature-complete data table with sorting, pagination, search, selection,
///     row expansion, export, column visibility toggle, skeleton loading,
///     and both client-side and server-side data modes.
/// </summary>
/// <typeparam name="TItem">The row data type.</typeparam>
[CascadingTypeParameter(nameof(TItem))]
public partial class MokaTable<TItem> : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Data/moka-table.js";
	private const int DebounceMs = 300;

	// Column filters: stores filter values keyed by column title
	private readonly Dictionary<string, string> _columnFilters = new(StringComparer.Ordinal);

	// Registered columns in display order: the order they registered in, until the user drags a
	// header. The column instance is the column's identity; the title is only its name in sort and
	// filter state.
	private readonly List<MokaColumn<TItem>> _columns = [];

	// What the column toggle set, per column. A column's Visible parameter is the starting state,
	// and a new Visible value from the parent removes the entry.
	private readonly Dictionary<MokaColumn<TItem>, bool> _columnVisibility = [];

	// Widths the user dragged, per column.
	private readonly Dictionary<MokaColumn<TItem>, double> _columnWidths = [];

	// Feature 1: Row expand tracking. Keyed on TItem when no ItemKey is supplied so struct items
	// compare by value instead of by a freshly boxed object on every call.
	private readonly HashSet<TItem> _expandedItems = new(EqualityComparer<TItem>.Default);
	private readonly HashSet<object> _expandedKeys = [];

	// Every active sort, single-column sorts included, in priority order.
	private readonly List<MokaTableSortDescriptor> _sortDescriptors = [];

	// A column was hidden or removed during a render and took its filter with it, so the rows have
	// to be loaded again once that render is done.
	private bool _columnReloadPending;
	private int _currentPage = 1;
	private bool _dense = true;
	private IReadOnlyList<TItem> _displayItems = [];
	private List<MokaTableRow<TItem>> _displayRows = [];
	private bool _disposed;
	private DotNetObjectReference<MokaTable<TItem>>? _dotNetRef;
	private MokaColumn<TItem>? _draggingColumn;
	private MokaTableRow<TItem>? _draggingRow;

	// Inline editing: currently editing cell
	private (TItem Item, MokaColumn<TItem> Column)? _editingCell;
	private bool _editFocusPending;

	// The text the editor opened with. A save compares against it, not against the raw value,
	// because a column with Format opens the editor on the formatted text.
	private string _editOriginalText = "";
	private string? _editValue;

	// Enter or Escape in the edit input removes it, and focus with it; the cell takes focus back.
	private bool _cellFocusPending;

	private Timer? _filterDebounceTimer;

	// Full client-side result set after search, column filters and sort. Empty in server mode.
	private List<TItem> _filteredItems = [];

	// Position of the first displayed row in the whole result set, so row reordering can report
	// absolute indexes for the rows that are actually on screen.
	private int _firstRowIndex;

	// Keyboard navigation
	private (int Row, int Col) _focusedCell = (-1, -1);
	private bool _gridKeysInitialized;
	private bool _hasLoaded;
	private bool? _indeterminateState;
	private bool _isLoading;
	private int _layoutVersion;
	private int _pageSize = 10;
	private bool _parameterReloadPending;

	// The page size the parent set. The size the user picked, which @bind-PageSize passes back,
	// does not count. The pager stays while the rows need more than one page at this size, so a
	// user who picked a size that fits every row can still pick a smaller one.
	private int _parentPageSize = 10;

	// Bug 1: Track data-relevant parameters to avoid redundant reloads and to keep internal
	// state from being reverted by the next parent render.
	private bool _previousDense = true;
	private IEnumerable<TItem>? _previousItems;
	private int _previousPageSize;
	private HashSet<TItem>? _previousSelectedItems;
	private bool _previousShowPagination = true;
	private string? _previousSortColumn;
	private MokaSortDirection _previousSortDirection;
	private int _resizeInitVersion = -1;

	// Whether moka-table.js is marking drop targets, which it does while columns or rows can be
	// dragged.
	private bool _reorderDragBound;
	private Timer? _searchDebounceTimer;
	private string? _searchTerm;
	private ElementReference _selectAllRef;
	private HashSet<TItem> _selectedItems = [];
	private bool _showColumnMenu;

	// Whether moka-table.js keeps a stand-in tab stop, which it does while Virtualize is on.
	private bool _tabStopWatchBound;
	private string? _sortColumn;
	private MokaSortDirection _sortDirection = MokaSortDirection.None;
	private int _totalItems;

	// The page size the user picked last, until the parent sets a different one.
	private int? _userPageSize;
	private ElementReference _wrapperRef;

	// ── Data ──

	/// <summary>Client-side data. Mutually exclusive with ServerData.</summary>
	[Parameter]
	public IEnumerable<TItem>? Items { get; set; }

	/// <summary>
	///     Server-side data callback. Called whenever sort/page/filter changes. While
	///     <see cref="ShowPagination" /> is false it is asked for page 1 with a page size that covers
	///     the whole result set. That size comes from the last
	///     <see cref="MokaTableResult{TItem}.TotalItems" />, so a load that finds more rows than that
	///     (the first load, for example) makes a second call sized to the new total.
	/// </summary>
	[Parameter]
	public Func<MokaTableState, Task<MokaTableResult<TItem>>>? ServerData { get; set; }

	/// <summary>Column definitions (MokaColumn components).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Identifies a row across renders. It becomes the row's <c>@key</c>, so a row keeps its DOM
	///     and component state when sorting, filtering or a reload moves it, and it records which rows
	///     are expanded. Without it the item itself is the key. A key that more than one rendered row
	///     returns identifies none of them: those rows render unkeyed instead of making Blazor throw
	///     on the repeated key, and expanding one of them expands them all.
	/// </summary>
	[Parameter]
	public Func<TItem, object>? ItemKey { get; set; }

	// ── Toolbar ──

	/// <summary>Table title shown in toolbar.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Shows search input in toolbar. Default false.</summary>
	[Parameter]
	public bool Searchable { get; set; }

	/// <summary>Placeholder text for the search input. Default "Search...".</summary>
	[Parameter]
	public string SearchPlaceholder { get; set; } = "Search...";

	/// <summary>Custom content in the toolbar (action buttons, filters).</summary>
	[Parameter]
	public RenderFragment? ToolbarContent { get; set; }

	/// <summary>Whether to show the toolbar. Default true.</summary>
	[Parameter]
	public bool ShowToolbar { get; set; } = true;

	// ── Pagination ──

	/// <summary>
	///     Items per page. Default 10. Does not limit the rows while <see cref="ShowPagination" /> is
	///     false: the table then shows every row.
	/// </summary>
	[Parameter]
	public int PageSize { get; set; } = 10;

	/// <summary>Callback when page size changes.</summary>
	[Parameter]
	public EventCallback<int> PageSizeChanged { get; set; }

	/// <summary>Dropdown options for page size. Default [10, 25, 50, 100].</summary>
	[Parameter]
	public IReadOnlyList<int> PageSizeOptions { get; set; } = [10, 25, 50, 100];

	/// <summary>
	///     Whether the table pages its rows and shows the pager. Default true. When false every row
	///     renders: <see cref="Items" /> is not cut to <see cref="PageSize" />, and
	///     <see cref="ServerData" /> is asked for page 1 with a page size that covers the whole result
	///     set. Pair it with <see cref="Virtualize" /> for long lists.
	/// </summary>
	[Parameter]
	public bool ShowPagination { get; set; } = true;

	/// <summary>Where to render pagination. Default Bottom.</summary>
	[Parameter]
	public MokaTablePaginationPosition PaginationPosition { get; set; } = MokaTablePaginationPosition.Bottom;

	/// <summary>
	///     Pins the pagination bar to its edge (position: sticky) so it stays visible while the
	///     page or the table body scrolls. Pairs well with <see cref="Height" /> for an in-card
	///     fixed footer; for page-level sticky the wrapper stops clipping its corners. Default false.
	/// </summary>
	[Parameter]
	public bool StickyPagination { get; set; }

	// ── Selection ──

	/// <summary>Shows checkboxes for row selection. Default false.</summary>
	[Parameter]
	public bool Selectable { get; set; }

	/// <summary>
	///     Currently selected items. Two-way bindable. The table works on its own copy and hands a
	///     new set to <see cref="SelectedItemsChanged" />, so the bound set is never mutated in place.
	/// </summary>
	[Parameter]
	[SuppressMessage("Usage", "CA2227:Collection properties should be read only",
		Justification = "Blazor two-way binding requires a setter.")]
	public HashSet<TItem>? SelectedItems { get; set; }

	/// <summary>Callback when selected items change. Always receives a new set instance.</summary>
	[Parameter]
	public EventCallback<HashSet<TItem>> SelectedItemsChanged { get; set; }

	/// <summary>
	///     Only one row selectable at a time. Default false. Selecting a row replaces the selection,
	///     and unticking the selected row clears it.
	/// </summary>
	[Parameter]
	public bool SingleSelect { get; set; }

	/// <summary>
	///     Fires when a row is clicked, or when Enter is pressed on one of its cells. On an
	///     editable cell Enter starts editing instead.
	/// </summary>
	[Parameter]
	public EventCallback<TItem> OnRowClick { get; set; }

	/// <summary>
	///     Fires on right-click of a row. When a handler is attached, the browser's default
	///     context menu is suppressed. Pair with a context-menu service:
	///     <c>OnRowContextMenu="a =&gt; Menu.Show(a.MouseEvent, ItemsFor(a.Item))"</c>.
	/// </summary>
	[Parameter]
	public EventCallback<MokaItemContextMenuArgs<TItem>> OnRowContextMenu { get; set; }

	// ── Appearance ──

	/// <summary>Alternating row colors. Default false.</summary>
	[Parameter]
	public bool Striped { get; set; }

	/// <summary>Row hover highlight. Default true.</summary>
	[Parameter]
	public bool Hoverable { get; set; } = true;

	/// <summary>Cell borders. Default false.</summary>
	[Parameter]
	public bool Bordered { get; set; }

	/// <summary>
	///     Dense row spacing. Default true (Moka is dense by default). Seeds the internal density
	///     state; the built-in density toggle changes that state without writing back to this parameter.
	/// </summary>
	[Parameter]
	public bool Dense { get; set; } = true;

	/// <summary>Sticky header on scroll. Default false.</summary>
	[Parameter]
	public bool FixedHeader { get; set; }

	/// <summary>Max height with vertical scroll. Null = auto.</summary>
	[Parameter]
	public string? Height { get; set; }

	/// <summary>Custom empty state content.</summary>
	[Parameter]
	public RenderFragment? EmptyContent { get; set; }

	/// <summary>Custom loading state content.</summary>
	[Parameter]
	public RenderFragment? LoadingContent { get; set; }

	/// <summary>Dynamic CSS class per row.</summary>
	[Parameter]
	public Func<TItem, string?>? RowClass { get; set; }

	/// <summary>Dynamic inline style per row.</summary>
	[Parameter]
	public Func<TItem, string?>? RowStyle { get; set; }

	// ── Sorting ──

	/// <summary>
	///     Currently sorted column (by Title). Two-way bindable. A new value from the parent replaces
	///     the current sort, a multi-column one included, and goes back to page 1.
	/// </summary>
	[Parameter]
	public string? SortColumn { get; set; }

	/// <summary>Callback when sort column changes.</summary>
	[Parameter]
	public EventCallback<string?> SortColumnChanged { get; set; }

	/// <summary>
	///     Current sort direction. Two-way bindable. A new value from the parent replaces the current
	///     sort like a new <see cref="SortColumn" /> does.
	/// </summary>
	[Parameter]
	public MokaSortDirection SortDirection { get; set; } = MokaSortDirection.None;

	/// <summary>Callback when sort direction changes.</summary>
	[Parameter]
	public EventCallback<MokaSortDirection> SortDirectionChanged { get; set; }

	/// <summary>Allow sorting by multiple columns. Default false.</summary>
	[Parameter]
	public bool MultiSort { get; set; }

	// ── Responsive ──

	/// <summary>Horizontal scroll on small screens. Default true.</summary>
	[Parameter]
	public bool Responsive { get; set; } = true;

	// ── Feature 1: Row Expand/Detail ──

	/// <summary>Template for expanded row detail content.</summary>
	[Parameter]
	public RenderFragment<TItem>? DetailTemplate { get; set; }

	/// <summary>Whether row detail expansion is enabled. Default false.</summary>
	[Parameter]
	public bool Expandable { get; set; }

	// ── Feature 2: Footer/Summary Row ──

	/// <summary>Footer row content. Rendered in a tfoot element.</summary>
	[Parameter]
	public RenderFragment? FooterContent { get; set; }

	// ── Feature 3: Export ──

	/// <summary>Whether to show an export button in the toolbar. Default false.</summary>
	[Parameter]
	public bool Exportable { get; set; }

	/// <summary>
	///     Callback to generate export data. Receives every row matching the current search and
	///     column filters (not just the current page). Under server-side data the table issues a
	///     dedicated ServerData request sized to cover the whole result set; check
	///     <see cref="MokaTableExportContext{TItem}.IsCompleteSet" /> to see whether the server
	///     returned all of it.
	/// </summary>
	[Parameter]
	public EventCallback<MokaTableExportContext<TItem>> OnExport { get; set; }

	// ── Feature 4: Column Visibility Toggle ──

	/// <summary>Whether to show a column visibility toggle in the toolbar. Default false.</summary>
	[Parameter]
	public bool ShowColumnToggle { get; set; }

	// ── Feature 5: Loading Skeleton ──

	/// <summary>Number of skeleton rows to show while loading. Default 5. Set to 0 for spinner mode.</summary>
	[Parameter]
	public int SkeletonRows { get; set; } = 5;

	// ── Feature 6: Selection Actions ──

	/// <summary>
	///     Template for bulk actions when items are selected. Receives a copy of the selection, so
	///     changing that set does not change what the table has selected: assign a new set to
	///     <see cref="SelectedItems" /> for that.
	/// </summary>
	[Parameter]
	public RenderFragment<HashSet<TItem>>? SelectionActions { get; set; }

	// ── Feature 7: Per-Row Actions ──

	/// <summary>Template for per-row action buttons. Rendered in a dedicated action column at the end.</summary>
	[Parameter]
	public RenderFragment<TItem>? RowActions { get; set; }

	/// <summary>Header text for the action column. Default "Actions".</summary>
	[Parameter]
	public string ActionsColumnTitle { get; set; } = "Actions";

	/// <summary>Width of the action column. Default "auto".</summary>
	[Parameter]
	public string ActionsColumnWidth { get; set; } = "auto";

	// ── Feature 8: Refresh Button ──

	/// <summary>Whether to show a refresh button in the toolbar. Default false. Useful for server-side data.</summary>
	[Parameter]
	public bool ShowRefresh { get; set; }

	/// <summary>Callback when refresh is clicked. If not set, reloads data automatically.</summary>
	[Parameter]
	public EventCallback OnRefresh { get; set; }

	// ── Feature 9: Density Toggle ──

	/// <summary>Whether to show a density toggle button in the toolbar. Default false.</summary>
	[Parameter]
	public bool ShowDensityToggle { get; set; }

	// ── Feature 10: Column Filters ──

	/// <summary>
	///     Whether to show a filter row below the header. Default false. Columns opt in individually
	///     via <see cref="MokaColumn{TItem}.Filterable" />.
	/// </summary>
	[Parameter]
	public bool ShowFilters { get; set; }

	// ── Feature 11: Inline Editing ──

	/// <summary>
	///     Fires when any cell is edited. Cells opt in via <see cref="MokaColumn{TItem}.Editable" />;
	///     double-click or press Enter on a focused cell to start editing.
	/// </summary>
	[Parameter]
	public EventCallback<MokaTableCellEditResult<TItem>> OnCellEdit { get; set; }

	// ── Feature 12: Virtualized Rows ──

	/// <summary>When true, uses virtualization to render only visible rows. Requires a fixed Height. Default false.</summary>
	[Parameter]
	public bool Virtualize { get; set; }

	/// <summary>Estimated row height for virtualization. Default 36.</summary>
	[Parameter]
	public float VirtualRowHeight { get; set; } = 36;

	// ── Feature 13: Column Reorder ──

	/// <summary>Allow reordering columns by dragging headers. Default false.</summary>
	[Parameter]
	public bool ColumnReorderable { get; set; }

	/// <summary>
	///     Callback after a header is dropped on another one. The table has already moved the column.
	///     OldIndex and NewIndex are its positions among the visible columns before and after the move.
	/// </summary>
	[Parameter]
	public EventCallback<(int OldIndex, int NewIndex)> OnColumnReordered { get; set; }

	// ── Feature 14: Row Reorder ──

	/// <summary>
	///     Allow reordering rows by dragging the grip handle. Default false. The handles are off while
	///     a sort, a search or a column filter is active, because the rows are not in the order of the
	///     collection then.
	/// </summary>
	[Parameter]
	public bool RowReorderable { get; set; }

	/// <summary>
	///     Callback when a row is dropped on another row. OldIndex and NewIndex are positions in
	///     <see cref="Items" /> (under <see cref="ServerData" />, in the unsorted, unfiltered result
	///     set), page offset included, so <c>RemoveAt(OldIndex)</c> followed by
	///     <c>Insert(NewIndex, Item)</c> applies the move. The table does not reorder anything itself:
	///     apply the move to your data, then call <see cref="ReloadAsync" /> if you mutated the
	///     collection in place.
	/// </summary>
	[Parameter]
	public EventCallback<(TItem Item, int OldIndex, int NewIndex)> OnRowReordered { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-table-wrapper";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-table-wrapper--sticky-pagination", StickyPagination)
		.AddClass(Class)
		.Build();

	// ── CSS helpers ──

	private string TableClasses => new CssBuilder("moka-table")
		.AddClass("moka-table--dense", _dense)
		.AddClass("moka-table--striped", Striped)
		.AddClass("moka-table--hoverable", Hoverable)
		.AddClass("moka-table--bordered", Bordered)
		.Build();

	private string ContainerClass => new CssBuilder("moka-table-container")
		.AddClass("moka-table-container--responsive", Responsive)
		.Build();

	private string? ContainerStyle => new StyleBuilder()
		.AddStyle("max-height", Height)
		.AddStyle("overflow-y", "auto", Height is not null)
		.Build();

	private string HeadClass => new CssBuilder()
		.AddClass("moka-table-head--fixed", FixedHeader)
		.Build();

	private string SearchInputClass => new CssBuilder("moka-table-search-input")
		.AddClass("moka-table-search-input--has-clear", !string.IsNullOrEmpty(_searchTerm))
		.Build();

	private string? ActionsHeaderStyle => new StyleBuilder()
		.AddStyle("width", ActionsColumnWidth)
		.Build();

	private string ExpandButtonClass(TItem item) => new CssBuilder("moka-table-expand-btn")
		.AddClass("moka-table-expand-btn--open", IsExpanded(item))
		.Build();

	private static string SortIconClass(MokaTableSortDescriptor? descriptor) => new CssBuilder("moka-table-sort-icon")
		.AddClass("moka-table-sort-icon--active", descriptor is not null)
		.Build();

	private static string? SkeletonBarStyle(int rowIndex, int colIndex) => new StyleBuilder()
		.AddStyle("width", FormattableString.Invariant($"{SkeletonWidth(rowIndex, colIndex)}%"))
		.Build();

	private string ReorderCellClass => new CssBuilder("moka-table-cell--reorder")
		.AddClass("moka-table-cell--reorder-off", !CanReorderRows)
		.Build();

	private string RowDraggable => CanReorderRows ? "true" : "false";

	private string? ReorderCellTitle => CanReorderRows ? null : "Clear the sort, search and filters to reorder rows";

	private string? ColumnDraggable => ColumnReorderable ? "true" : null;

	private int VisibleColumnCount => _columns.Count(IsColumnVisible);

	private int ColSpan => VisibleColumnCount
	                       + (Selectable ? 1 : 0)
	                       + (Expandable ? 1 : 0)
	                       + (RowReorderable ? 1 : 0)
	                       + (RowActions is not null ? 1 : 0);

	// ── Aggregation ──

	private bool HasAggregates => _columns.Any(c => c.Aggregate != MokaAggregateType.None);

	// ── Pagination visibility (Top and Bottom share one rule) ──

	private int PageCount => _pageSize > 0
		? Math.Max(1, (int)Math.Ceiling(_totalItems / (double)_pageSize))
		: 1;

	private int PageStartIndex => (_currentPage - 1) * Math.Max(_pageSize, 0);

	// A page size that holds the whole result set, as far as the last load reported it.
	private int FullSetPageSize => Math.Max(_totalItems, Math.Max(_pageSize, 1));

	// One rule for both positions. Hidden while everything fits on one page, but kept while the user
	// is past page 1, and while the rows would need more than one page at the parent's page size:
	// a user who picked a size that fits every row needs the size menu to pick a smaller one again.
	// Measuring against PageSize itself fails under @bind-PageSize, where it follows the user's pick.
	private bool ShowPaginationBar => ShowPagination
	                                  && (_totalItems > _pageSize || _currentPage > 1 || _totalItems > _parentPageSize);

	private bool ShowTopPagination => ShowPaginationBar
	                                  && PaginationPosition is MokaTablePaginationPosition.Top
		                                  or MokaTablePaginationPosition.Both;

	private bool ShowBottomPagination => ShowPaginationBar
	                                     && PaginationPosition is MokaTablePaginationPosition.Bottom
		                                     or MokaTablePaginationPosition.Both;

	// ── Selection state (scoped to the displayed rows, matching HandleSelectAll) ──

	private bool AllDisplayedRowsSelected => _displayItems.Count > 0 && _displayItems.All(_selectedItems.Contains);

	private bool SomeDisplayedRowsSelected =>
		!AllDisplayedRowsSelected && _displayItems.Any(_selectedItems.Contains);

	private string SelectAllLabel => ShowPagination ? "Select all rows on this page" : "Select all rows";

	// Aggregates and distinct filter values can only see loaded rows. In server mode that is the
	// current page, or every row while the pager is hidden; in client mode it is the full filtered set.
	private IReadOnlyList<TItem> LoadedItems => ServerData is not null ? _displayItems : _filteredItems;

	// OnRowReordered reports positions in Items (or in the unsorted server result), and the rows
	// only stand in that order while no sort reorders them and no search or filter leaves rows out.
	private bool CanReorderRows => RowReorderable
	                               && _sortDescriptors.Count == 0
	                               && string.IsNullOrWhiteSpace(_searchTerm)
	                               && _columnFilters.Count == 0;

	// The selection bar's template gets a copy, so it cannot change the selection behind the table's back.
	private HashSet<TItem> SelectionCopy => new(_selectedItems, _selectedItems.Comparer);

	/// <summary>
	///     Tables always re-render - they have complex internal state (search, selection, expand, sort)
	///     that changes independently of parameters. The base class ShouldRender optimization
	///     is too aggressive for stateful components.
	/// </summary>
	protected override bool ShouldRender() => true;

	/// <summary>
	///     Forces a data refresh: re-runs the ServerData callback, or re-applies search, filters,
	///     sorting and paging to <see cref="Items" />. Call this after mutating the bound collection
	///     in place, which the table cannot detect on its own.
	/// </summary>
	public async Task ReloadAsync()
	{
		await LoadDataAsync();
		StateHasChanged();
	}

	/// <summary>Registers a column definition from a child MokaColumn component.</summary>
	internal void AddColumn(MokaColumn<TItem> column)
	{
		if (_columns.Contains(column))
		{
			return;
		}

		_columns.Add(column);
		_layoutVersion++;

		// Client-side rows first load before any column has registered, so a sort the parent set
		// from the start had no column to order by. The render queued below shows the sorted rows.
		if (ServerData is null && _hasLoaded && _sortDescriptors.Exists(d => d.Column == column.Title))
		{
			LoadClientData();
		}

		StateHasChanged();
	}

	/// <summary>Removes a column definition when a MokaColumn is disposed.</summary>
	internal void RemoveColumn(MokaColumn<TItem> column)
	{
		if (!_columns.Remove(column))
		{
			return;
		}

		_columnVisibility.Remove(column);
		_columnWidths.Remove(column);
		if (DropFiltersWithoutControl())
		{
			_columnReloadPending = true;
		}

		_layoutVersion++;
		ClampFocusedCell();
		StateHasChanged();
	}

	/// <summary>
	///     Called by a column when the parent passes it a new value for a setting the table renders.
	///     The table has rendered before its columns got their parameters, so it renders again, in the
	///     same batch.
	/// </summary>
	/// <param name="column">The column whose settings changed.</param>
	/// <param name="visibleChanged">Whether <c>Visible</c> is one of them.</param>
	/// <param name="previousTitle">The title the column had until now.</param>
	internal void OnColumnChanged(MokaColumn<TItem> column, bool visibleChanged, string? previousTitle)
	{
		// The parent's new Visible value replaces what the column toggle chose.
		if (visibleChanged)
		{
			_columnVisibility.Remove(column);
		}

		if (previousTitle != column.Title && RenameColumnState(previousTitle, column.Title))
		{
			_columnReloadPending = true;
		}

		if (DropFiltersWithoutControl())
		{
			_columnReloadPending = true;
		}

		_layoutVersion++;
		ClampFocusedCell();
		StateHasChanged();
	}

	// Sorts and filters name a column by its title, so a renamed column takes its sort and filter
	// along instead of leaving them under a title no column has. A column whose title goes away can
	// no longer be sorted or filtered, so its sort and filter go with it, and the method returns
	// true because the rows have to be loaded again.
	private bool RenameColumnState(string? previousTitle, string? title)
	{
		if (previousTitle is null)
		{
			return false;
		}

		if (title is null)
		{
			bool filtered = _columnFilters.Remove(previousTitle);
			bool sorted = _sortDescriptors.RemoveAll(d => d.Column == previousTitle) > 0;
			if (_sortColumn == previousTitle)
			{
				_sortColumn = null;
				_sortDirection = MokaSortDirection.None;
				sorted = true;
			}

			return filtered || sorted;
		}

		if (_columnFilters.Remove(previousTitle, out string? filter))
		{
			_columnFilters[title] = filter;
		}

		foreach (MokaTableSortDescriptor descriptor in _sortDescriptors.Where(d => d.Column == previousTitle))
		{
			descriptor.Column = title;
		}

		if (_sortColumn == previousTitle)
		{
			_sortColumn = title;
		}

		return false;
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (firstRender && !_gridKeysInitialized)
		{
			_gridKeysInitialized = true;
			await InvokeTableJsAsync("initGridKeys", _wrapperRef);
		}

		bool reorderWanted = ColumnReorderable || RowReorderable;
		if (reorderWanted != _reorderDragBound)
		{
			_reorderDragBound = reorderWanted;
			await InvokeTableJsAsync(reorderWanted ? "initReorderDrag" : "disposeReorderDrag", _wrapperRef);
		}

		// Virtualize renders only some rows, and the remembered cell's row can scroll out of them.
		if (Virtualize != _tabStopWatchBound)
		{
			_tabStopWatchBound = Virtualize;
			await InvokeTableJsAsync(Virtualize ? "watchTabStop" : "unwatchTabStop", _wrapperRef);
		}

		if (_resizeInitVersion != _layoutVersion)
		{
			await InitColumnResizeAsync();
		}

		// A column change during the render dropped a filter, one that lost its control. The rows
		// it filtered have to come back, and a render is no place to load them.
		if (_columnReloadPending)
		{
			_columnReloadPending = false;
			_currentPage = 1;
			await LoadDataAsync();
			StateHasChanged();
		}

		if (_editFocusPending)
		{
			_editFocusPending = false;
			await InvokeTableJsAsync("focusEditInput", _wrapperRef);
		}
		else if (_cellFocusPending)
		{
			_cellFocusPending = false;
			if (_focusedCell.Row >= 0)
			{
				await InvokeTableJsAsync("focusCell", _wrapperRef, _focusedCell.Row, _focusedCell.Col);
			}
		}

		await SyncSelectAllIndeterminateAsync();
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		SyncSelectionFromParameters();
		SyncStateFromParameters();
	}

	// Bug 1: Only reload when data-relevant parameters change
	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		bool itemsChanged = !ReferenceEquals(Items, _previousItems);
		if (itemsChanged)
		{
			_previousItems = Items;
		}

		if (_hasLoaded && !itemsChanged && !_parameterReloadPending)
		{
			return;
		}

		_hasLoaded = true;
		_parameterReloadPending = false;
		await LoadDataAsync();
	}

	// PageSize, SortColumn, SortDirection and Dense are parameters the table also changes itself.
	// Internal state lives in backing fields and the parameter only seeds them when the parent
	// actually passes a new value, so an unbound parent re-render cannot revert a user action.
	// A new value that matches the state already in place is a two-way binding handing back what
	// the table just reported: nothing to apply, and nothing to load again.
	private void SyncStateFromParameters()
	{
		SyncPageSizeFromParameters();
		SyncSortFromParameters();

		if (Dense != _previousDense)
		{
			_previousDense = Dense;
			_dense = Dense;
		}

		// Hiding or showing the pager changes which rows are loaded, not just the chrome.
		if (ShowPagination != _previousShowPagination)
		{
			_previousShowPagination = ShowPagination;
			_parameterReloadPending = true;
		}

		// The filter row is the only control for the column filters, so hiding it drops them and
		// brings back the rows they hid.
		if (!ShowFilters && _columnFilters.Count > 0)
		{
			_columnFilters.Clear();
			_currentPage = 1;
			_parameterReloadPending = true;
		}
	}

	private void SyncPageSizeFromParameters()
	{
		if (PageSize == _previousPageSize)
		{
			return;
		}

		_previousPageSize = PageSize;

		// @bind-PageSize handing back the size the user picked. It is in place already, and it is
		// the user's choice, not the parent's, so the pager keeps measuring against the parent's.
		if (PageSize == _userPageSize)
		{
			return;
		}

		_userPageSize = null;
		_parentPageSize = PageSize;
		if (PageSize != _pageSize)
		{
			_pageSize = PageSize;
			_parameterReloadPending = true;
		}
	}

	private void SyncSortFromParameters()
	{
		bool changed = false;
		if (SortColumn != _previousSortColumn)
		{
			_previousSortColumn = SortColumn;
			if (SortColumn != _sortColumn)
			{
				_sortColumn = SortColumn;
				changed = true;
			}
		}

		if (SortDirection != _previousSortDirection)
		{
			_previousSortDirection = SortDirection;
			if (SortDirection != _sortDirection)
			{
				_sortDirection = SortDirection;
				changed = true;
			}
		}

		if (!changed)
		{
			return;
		}

		// The parent's sort replaces the one the header clicks built, a multi-column one included.
		// The rows and the header icons both read the descriptors, so the sort has to land there.
		ResetSortDescriptors();
		_currentPage = 1;
		_parameterReloadPending = true;
	}

	// Bug 2: take a copy so selection changes never mutate the parent's set in place.
	private void SyncSelectionFromParameters()
	{
		if (ReferenceEquals(SelectedItems, _previousSelectedItems))
		{
			return;
		}

		_previousSelectedItems = SelectedItems;
		_selectedItems = SelectedItems is null ? [] : new HashSet<TItem>(SelectedItems);
		_indeterminateState = null;
	}

	// ── Data loading ──

	private async Task LoadDataAsync()
	{
		_isLoading = true;
		try
		{
			if (ServerData is not null)
			{
				await LoadServerDataAsync(ServerData);
			}
			else if (Items is not null)
			{
				LoadClientData();
			}
			else
			{
				_filteredItems = [];
				_totalItems = 0;
				_currentPage = 1;
				SetDisplayItems([], 0);
			}
		}
		finally
		{
			_isLoading = false;
		}
	}

	private async Task LoadServerDataAsync(Func<MokaTableState, Task<MokaTableResult<TItem>>> source)
	{
		_filteredItems = [];

		// With the pager hidden no other page can be reached, so the table asks for every row.
		if (!ShowPagination)
		{
			_currentPage = 1;
			MokaTableResult<TItem> all = await FetchAllServerRowsAsync(source);
			_totalItems = all.TotalItems;
			SetDisplayItems(all.Items, 0);
			return;
		}

		MokaTableResult<TItem> result = await source(BuildState(_currentPage, _pageSize));
		_totalItems = result.TotalItems;

		// The requested page can be past the end after a filter narrows the result set.
		if (ClampCurrentPage())
		{
			result = await source(BuildState(_currentPage, _pageSize));
			_totalItems = result.TotalItems;
		}

		SetDisplayItems(result.Items, PageStartIndex);
	}

	// Page 1, sized from the last TotalItems the source reported. There is none before the first
	// load and it is out of date once the result set grows, so an answer reporting more rows than
	// were asked for gets one follow-up request sized to the new total. A source that caps its page
	// size below that still comes back short, and the table shows what it got.
	private async Task<MokaTableResult<TItem>> FetchAllServerRowsAsync(
		Func<MokaTableState, Task<MokaTableResult<TItem>>> source)
	{
		int pageSize = FullSetPageSize;
		MokaTableResult<TItem> result = await source(BuildState(1, pageSize));
		if (result.TotalItems > pageSize && result.Items.Count < result.TotalItems)
		{
			result = await source(BuildState(1, result.TotalItems));
		}

		return result;
	}

	private void LoadClientData()
	{
		_filteredItems = BuildFilteredItems();
		_totalItems = _filteredItems.Count;

		// With the pager hidden no other page can be reached, so every row renders.
		if (!ShowPagination)
		{
			_currentPage = 1;
			SetDisplayItems(_filteredItems, 0);
			return;
		}

		ClampCurrentPage();
		SetDisplayItems(_filteredItems
			.Skip(PageStartIndex)
			.Take(_pageSize)
			.ToList(), PageStartIndex);
	}

	private List<TItem> BuildFilteredItems()
	{
		IEnumerable<TItem> items = Items ?? [];

		if (!string.IsNullOrWhiteSpace(_searchTerm))
		{
			items = FilterItems(items, _searchTerm);
		}

		if (_columnFilters.Count > 0)
		{
			items = ApplyColumnFilters(items);
		}

		if (_sortDescriptors.Count > 0)
		{
			items = SortItems(items);
		}

		return items.ToList();
	}

	// The descriptors are copied because the table changes its own in place on the next header
	// click, which would rewrite a state the source kept.
	private MokaTableState BuildState(int page, int pageSize) => new()
	{
		Page = page,
		PageSize = pageSize,
		SearchTerm = _searchTerm,
		SortColumn = _sortColumn,
		SortDirection = _sortDirection,
		SortDescriptors = _sortDescriptors
			.Select(d => new MokaTableSortDescriptor { Column = d.Column, Direction = d.Direction, Priority = d.Priority })
			.ToList(),
		ColumnFilters = new Dictionary<string, string>(_columnFilters, StringComparer.Ordinal)
	};

	private void SetDisplayItems(IReadOnlyList<TItem> items, int firstRowIndex)
	{
		_displayItems = items;
		_firstRowIndex = firstRowIndex;
		_displayRows = BuildRows(items);
		_indeterminateState = null;
		ClampFocusedCell();
	}

	// Blazor throws when two sibling rows carry the same @key, and a key that several rows share
	// cannot tell them apart anyway. Those rows lose the key and Blazor matches them by position, as
	// it would without ItemKey; mixing keyed and unkeyed siblings is supported. Unique keys stay, so
	// those rows are still moved rather than rebuilt. The set uses the default equality, which is
	// what Blazor compares keys with.
	private List<MokaTableRow<TItem>> BuildRows(IReadOnlyList<TItem> items)
	{
		var keys = new object?[items.Count];
		var seen = new HashSet<object>();
		HashSet<object>? repeated = null;
		for (int i = 0; i < items.Count; i++)
		{
			object? key = RowKey(items[i]);
			keys[i] = key;
			if (key is not null && !seen.Add(key))
			{
				repeated ??= [];
				repeated.Add(key);
			}
		}

		var rows = new List<MokaTableRow<TItem>>(items.Count);
		for (int i = 0; i < items.Count; i++)
		{
			object? key = keys[i];
			if (key is not null && repeated is not null && repeated.Contains(key))
			{
				key = null;
			}

			rows.Add(new MokaTableRow<TItem>(items[i], i, key));
		}

		return rows;
	}

	private bool ClampCurrentPage()
	{
		int clamped = Math.Clamp(_currentPage, 1, PageCount);
		if (clamped == _currentPage)
		{
			return false;
		}

		_currentPage = clamped;
		return true;
	}

	private IEnumerable<TItem> FilterItems(IEnumerable<TItem> items, string search)
	{
		return items.Where(item =>
			_columns.Any(col =>
				col.Field is not null &&
				col.Field(item)?.ToString()?.Contains(search, StringComparison.OrdinalIgnoreCase) == true));
	}

	// One path for every sort: each descriptor, in priority order, orders by the column's
	// SortComparer when it has one and by its Field otherwise. A column with neither is skipped.
	private IEnumerable<TItem> SortItems(IEnumerable<TItem> items)
	{
		IOrderedEnumerable<TItem>? ordered = null;
		foreach (MokaTableSortDescriptor desc in _sortDescriptors.OrderBy(d => d.Priority))
		{
			MokaColumn<TItem>? col = _columns.FirstOrDefault(c => c.Title == desc.Column);
			if (col is null || desc.Direction == MokaSortDirection.None)
			{
				continue;
			}

			bool descending = desc.Direction == MokaSortDirection.Descending;
			if (col.SortComparer is { } compare)
			{
				var comparer = Comparer<TItem>.Create((a, b) => compare(a, b));
				ordered = (ordered, descending) switch
				{
					(null, false) => items.OrderBy(static x => x, comparer),
					(null, true) => items.OrderByDescending(static x => x, comparer),
					(_, false) => ordered.ThenBy(static x => x, comparer),
					_ => ordered.ThenByDescending(static x => x, comparer)
				};
			}
			else if (col.Field is { } field)
			{
				ordered = (ordered, descending) switch
				{
					(null, false) => items.OrderBy(field),
					(null, true) => items.OrderByDescending(field),
					(_, false) => ordered.ThenBy(field),
					_ => ordered.ThenByDescending(field)
				};
			}
		}

		return ordered ?? items;
	}

	private MokaTableSortDescriptor? GetSortDescriptor(string? columnTitle) =>
		_sortDescriptors.FirstOrDefault(d => d.Column == columnTitle);

	// With client-side data a click has to have something to order the rows by. A server-side
	// source sorts by the title it receives.
	private bool CanSort(MokaColumn<TItem> col) =>
		col.Sortable
		&& col.Title is not null
		&& (ServerData is not null || col.Field is not null || col.SortComparer is not null);

	// On the header cell, so a click anywhere in it sorts, and so does the sort button's Enter or
	// Space, whose click bubbles up with Shift for a multi-column sort. Headers that cannot sort
	// get no handler.
	private EventCallback<MouseEventArgs> SortClickHandler(MokaColumn<TItem> col) =>
		CanSort(col)
			? EventCallback.Factory.Create<MouseEventArgs>(this, e => HandleSort(col, e.ShiftKey))
			: default;

	// ARIA wants aria-sort on one header at a time, so a multi-column sort names only its first
	// column.
	private string? AriaSort(MokaColumn<TItem> col)
	{
		if (!CanSort(col) || _sortDescriptors.Count == 0)
		{
			return null;
		}

		MokaTableSortDescriptor primary = _sortDescriptors.MinBy(d => d.Priority)!;
		if (primary.Column != col.Title)
		{
			return null;
		}

		return primary.Direction switch
		{
			MokaSortDirection.Ascending => "ascending",
			MokaSortDirection.Descending => "descending",
			_ => null
		};
	}

	// Without a HeaderTemplate the button holds the title, which names it.
	private static string? SortButtonLabel(MokaColumn<TItem> col) =>
		col.HeaderTemplate is null ? null : $"Sort by {col.Title}";

	// ── Column reorder ──

	// The visible columns with their positions, which the cells carry as data-col-index.
	private List<(MokaColumn<TItem> Col, int ColIdx)> VisibleColumnsInOrder() =>
		_columns.Where(IsColumnVisible).Select((c, i) => (c, i)).ToList();

	// The drag handlers are attached only while columns can be reordered. The highlight of the
	// header under the pointer lives in moka-table.js, because dragover fires many times a second.
	private EventCallback<DragEventArgs> ColumnDragStartHandler(MokaColumn<TItem> column) =>
		ColumnReorderable
			? EventCallback.Factory.Create<DragEventArgs>(this, () => _draggingColumn = column)
			: default;

	private EventCallback<DragEventArgs> ColumnDropHandler(MokaColumn<TItem> target) =>
		ColumnReorderable
			? EventCallback.Factory.Create<DragEventArgs>(this, () => HandleColumnDropAsync(target))
			: default;

	// A drag cancelled with Escape or dropped outside the headers ends without a drop.
	private EventCallback<DragEventArgs> ColumnDragEndHandler =>
		ColumnReorderable
			? EventCallback.Factory.Create<DragEventArgs>(this, () => _draggingColumn = null)
			: default;

	// Works on the columns themselves, not on their positions: positions change with every move
	// and skip hidden columns, so an index taken from the screen addresses the wrong column.
	private async Task HandleColumnDropAsync(MokaColumn<TItem> target)
	{
		MokaColumn<TItem>? source = _draggingColumn;
		_draggingColumn = null;
		if (!ColumnReorderable || source is null || ReferenceEquals(source, target))
		{
			return;
		}

		var visible = _columns.Where(IsColumnVisible).ToList();
		int oldIndex = visible.IndexOf(source);
		int newIndex = visible.IndexOf(target);
		int from = _columns.IndexOf(source);
		int to = _columns.IndexOf(target);
		if (oldIndex < 0 || newIndex < 0 || from < 0 || to < 0)
		{
			return;
		}

		// Taking the target's place pushes the columns in between over by one: the moved column
		// ends up after the target when it moves right and before it when it moves left.
		_columns.RemoveAt(from);
		_columns.Insert(to, source);
		_layoutVersion++;

		await OnColumnReordered.InvokeAsync((oldIndex, newIndex));
	}

	// ── Row reorder ──

	private void HandleRowDragStart(MokaTableRow<TItem> row)
	{
		if (CanReorderRows)
		{
			_draggingRow = row;
		}
	}

	private void HandleRowDragEnd() => _draggingRow = null;

	private EventCallback<DragEventArgs> RowDropHandler(MokaTableRow<TItem> row) =>
		RowReorderable
			? EventCallback.Factory.Create<DragEventArgs>(this, () => HandleRowDrop(row.Index))
			: default;

	private async Task HandleRowDrop(int targetIndex)
	{
		MokaTableRow<TItem>? source = _draggingRow;
		_draggingRow = null;

		if (!CanReorderRows || source is null || source.Value.Index == targetIndex)
		{
			return;
		}

		await OnRowReordered.InvokeAsync(
			(source.Value.Item, _firstRowIndex + source.Value.Index, _firstRowIndex + targetIndex));
	}

	// ── Keyboard navigation ──

	private string CellTabIndex(int rowIndex, int colIndex)
	{
		bool isRoving = _focusedCell.Row < 0
			? rowIndex == 0 && colIndex == 0
			: _focusedCell == (rowIndex, colIndex);
		return isRoving ? "0" : "-1";
	}

	// The grid's one tab stop is the remembered cell. A page change, a filter or a hidden column
	// can leave that cell outside the grid, and then no cell has tabindex 0 and Tab skips the table.
	private void ClampFocusedCell()
	{
		if (_focusedCell.Row < 0)
		{
			return;
		}

		int rows = _displayRows.Count;
		int columns = VisibleColumnCount;
		_focusedCell = rows == 0 || columns == 0
			? (-1, -1)
			: (Math.Min(_focusedCell.Row, rows - 1), Math.Min(_focusedCell.Col, columns - 1));
	}

	private async Task HandleCellKeyDown(KeyboardEventArgs e, int rowIndex, int colIndex, TItem item,
		MokaColumn<TItem> col)
	{
		if (_editingCell is not null)
		{
			return;
		}

		(int Row, int Col) target = NextFocusedCell(e.Key, rowIndex, colIndex);
		if (target.Row >= 0)
		{
			_focusedCell = target;
			await InvokeTableJsAsync("focusCell", _wrapperRef, target.Row, target.Col);
			return;
		}

		await HandleCellActionKey(e.Key, item, col);
	}

	private (int Row, int Col) NextFocusedCell(string key, int rowIndex, int colIndex)
	{
		int lastRow = _displayItems.Count - 1;
		int lastCol = VisibleColumnCount - 1;
		return key switch
		{
			"ArrowDown" => (Math.Min(rowIndex + 1, lastRow), colIndex),
			"ArrowUp" => (Math.Max(rowIndex - 1, 0), colIndex),
			"ArrowRight" => (rowIndex, Math.Min(colIndex + 1, lastCol)),
			"ArrowLeft" => (rowIndex, Math.Max(colIndex - 1, 0)),
			"Home" => (rowIndex, 0),
			"End" => (rowIndex, Math.Max(lastCol, 0)),
			_ => (-1, -1)
		};
	}

	private async Task HandleCellActionKey(string key, TItem item, MokaColumn<TItem> col)
	{
		switch (key)
		{
			case "Enter" when col.Editable:
				StartEdit(item, col);
				break;
			case " " when Selectable:
				await HandleRowSelect(item);
				break;
			default:
				break;
		}
	}

	private void HandleCellFocus(int rowIndex, int colIndex) => _focusedCell = (rowIndex, colIndex);

	// Enter on a focused cell raises OnRowClick, through moka-table.js, which clicks the cell so
	// the keyboard takes the mouse's path. An editable cell keeps Enter for starting the edit.
	private bool ActivatesRow(MokaColumn<TItem> col) => OnRowClick.HasDelegate && !col.Editable;

	// ── Sorting ──

	private async Task HandleSort(MokaColumn<TItem> column, bool addToExisting = false)
	{
		if (!CanSort(column) || column.Title is not { } title)
		{
			return;
		}

		if (MultiSort && addToExisting)
		{
			ApplyMultiSort(title);
		}
		else
		{
			ApplySingleSort(title);
		}

		await SortColumnChanged.InvokeAsync(_sortColumn);
		await SortDirectionChanged.InvokeAsync(_sortDirection);
		_currentPage = 1;
		await LoadDataAsync();
	}

	private void ApplyMultiSort(string columnTitle)
	{
		MokaTableSortDescriptor? existing = _sortDescriptors.FirstOrDefault(d => d.Column == columnTitle);
		if (existing is null)
		{
			_sortDescriptors.Add(new MokaTableSortDescriptor
			{
				Column = columnTitle,
				Direction = MokaSortDirection.Ascending,
				Priority = _sortDescriptors.Count + 1
			});
		}
		else
		{
			existing.Direction = existing.Direction == MokaSortDirection.Ascending
				? MokaSortDirection.Descending
				: MokaSortDirection.None;
			if (existing.Direction == MokaSortDirection.None)
			{
				_sortDescriptors.Remove(existing);
			}
		}

		for (int i = 0; i < _sortDescriptors.Count; i++)
		{
			_sortDescriptors[i].Priority = i + 1;
		}

		// Keep the single-sort state in step with the highest-priority descriptor.
		_sortColumn = _sortDescriptors.Count > 0 ? _sortDescriptors[0].Column : null;
		_sortDirection = _sortDescriptors.Count > 0 ? _sortDescriptors[0].Direction : MokaSortDirection.None;
	}

	private void ApplySingleSort(string columnTitle)
	{
		if (_sortColumn == columnTitle)
		{
			_sortDirection = _sortDirection switch
			{
				MokaSortDirection.Ascending => MokaSortDirection.Descending,
				MokaSortDirection.Descending => MokaSortDirection.None,
				_ => MokaSortDirection.Ascending
			};
			if (_sortDirection == MokaSortDirection.None)
			{
				_sortColumn = null;
			}
		}
		else
		{
			_sortColumn = columnTitle;
			_sortDirection = MokaSortDirection.Ascending;
		}

		ResetSortDescriptors();
	}

	// Makes the descriptors hold just the single-column sort in _sortColumn and _sortDirection.
	private void ResetSortDescriptors()
	{
		_sortDescriptors.Clear();
		if (_sortColumn is not null && _sortDirection != MokaSortDirection.None)
		{
			_sortDescriptors.Add(new MokaTableSortDescriptor
			{
				Column = _sortColumn,
				Direction = _sortDirection,
				Priority = 1
			});
		}
	}

	// ── Paging ──

	private async Task HandlePageChange(int page)
	{
		// The pager reports page 1 right after a new page size, which has loaded page 1 already.
		if (page == _currentPage)
		{
			return;
		}

		_currentPage = page;
		await LoadDataAsync();
	}

	private async Task HandlePageSizeChange(int pageSize)
	{
		_pageSize = pageSize;
		_userPageSize = pageSize;
		_currentPage = 1;
		await PageSizeChanged.InvokeAsync(pageSize);
		await LoadDataAsync();
	}

	// ── Search ──

	// Bug 5: _disposed guards keep the debounce callback from touching a torn-down component.
	private Timer CreateDebouncedReloadTimer(Action? beforeLoad = null)
	{
		return new Timer(async _ =>
		{
			if (_disposed)
			{
				return;
			}

			try
			{
				await InvokeAsync(async () =>
				{
					if (_disposed)
					{
						return;
					}

					beforeLoad?.Invoke();
					_currentPage = 1;
					await LoadDataAsync();
					StateHasChanged();
				});
			}
			catch (ObjectDisposedException)
			{
			}
		}, null, DebounceMs, Timeout.Infinite);
	}

	private void HandleSearchInput(ChangeEventArgs e)
	{
		string? term = e.Value?.ToString();
		_searchDebounceTimer?.Dispose();
		_searchDebounceTimer = CreateDebouncedReloadTimer(() => _searchTerm = term);
	}

	private async Task ClearSearch()
	{
		_searchTerm = null;
		_currentPage = 1;
		if (_searchDebounceTimer is not null)
		{
			await _searchDebounceTimer.DisposeAsync();
			_searchDebounceTimer = null;
		}

		await LoadDataAsync();
	}

	// ── Selection ──

	private async Task NotifySelectionChangedAsync()
	{
		// Hand out a new set: mutating (and returning) the parent's instance makes a
		// reference-comparing parent see no change at all. _previousSelectedItems stays the set
		// the parent passed: a parent that only listens passes none, and the render that follows
		// its handler must not read that as a new, empty selection.
		var snapshot = new HashSet<TItem>(_selectedItems);
		_indeterminateState = null;
		await SelectedItemsChanged.InvokeAsync(snapshot);
	}

	private async Task HandleSelectAll(bool selectAll)
	{
		foreach (TItem item in _displayItems)
		{
			if (selectAll)
			{
				_selectedItems.Add(item);
			}
			else
			{
				_selectedItems.Remove(item);
			}
		}

		await NotifySelectionChangedAsync();
	}

	private async Task HandleRowSelect(TItem item)
	{
		if (SingleSelect)
		{
			// Unticking the selected row clears the selection. Keeping the row selected would leave
			// the browser's unticked box on screen: the markup still says checked, as it did before
			// the click, so Blazor finds nothing to update.
			bool wasSelected = _selectedItems.Contains(item);
			_selectedItems.Clear();
			if (!wasSelected)
			{
				_selectedItems.Add(item);
			}
		}
		else if (!_selectedItems.Remove(item))
		{
			_selectedItems.Add(item);
		}

		await NotifySelectionChangedAsync();
	}

	private async Task ClearSelection()
	{
		_selectedItems.Clear();
		await NotifySelectionChangedAsync();
	}

	private async Task SyncSelectAllIndeterminateAsync()
	{
		if (!Selectable || SingleSelect)
		{
			return;
		}

		bool indeterminate = SomeDisplayedRowsSelected;
		if (_indeterminateState == indeterminate)
		{
			return;
		}

		_indeterminateState = indeterminate;
		await InvokeTableJsAsync("setIndeterminate", _selectAllRef, indeterminate);
	}

	// ── Rows ──

	// Enter on a cell reaches OnRowClick through this handler too: moka-table.js clicks cells marked
	// data-activates-row, which only happens while OnRowClick is set.
	private EventCallback<MouseEventArgs> RowClickHandler(TItem item) =>
		OnRowClick.HasDelegate
			? EventCallback.Factory.Create<MouseEventArgs>(this, () => OnRowClick.InvokeAsync(item))
			: default;

	private EventCallback<MouseEventArgs> RowContextMenuHandler(TItem item) =>
		OnRowContextMenu.HasDelegate
			? EventCallback.Factory.Create<MouseEventArgs>(this,
				e => OnRowContextMenu.InvokeAsync(new MokaItemContextMenuArgs<TItem>(item, e)))
			: default;

	private EventCallback<MouseEventArgs> CellDoubleClickHandler(TItem item, MokaColumn<TItem> col) =>
		col.Editable
			? EventCallback.Factory.Create<MouseEventArgs>(this, () => StartEdit(item, col))
			: default;

	private static string GetCellValue(MokaColumn<TItem> column, TItem item)
	{
		if (column.Field is null)
		{
			return "";
		}

		object? value = column.Field(item);
		if (value is null)
		{
			return "";
		}

		if (column.Format is not null && value is IFormattable formattable)
		{
			return formattable.ToString(column.Format, CultureInfo.CurrentCulture);
		}

		return value.ToString() ?? "";
	}

	// ── Feature 1: Row expand helpers ──

	private object? RowKey(TItem item) => ItemKey?.Invoke(item) ?? item;

	// Detail rows are siblings of the data rows, so their key is wrapped in a type that no ItemKey
	// can return. An unkeyed row's detail row is unkeyed too: its key would be just as ambiguous.
	private static DetailRowIdentity? DetailRowKey(MokaTableRow<TItem> row) =>
		row.Key is null ? null : new DetailRowIdentity(row.Key);

	private bool IsExpanded(TItem item) =>
		ItemKey is not null ? _expandedKeys.Contains(ItemKey(item)) : _expandedItems.Contains(item);

	private void ToggleExpand(TItem item)
	{
		if (ItemKey is not null)
		{
			object key = ItemKey(item);
			if (!_expandedKeys.Remove(key))
			{
				_expandedKeys.Add(key);
			}

			return;
		}

		if (!_expandedItems.Remove(item))
		{
			_expandedItems.Add(item);
		}
	}

	// ── Feature 3: Export helpers ──

	private async Task HandleExport()
	{
		(IReadOnlyList<TItem> items, bool complete) = await GetExportItemsAsync();

		if (OnExport.HasDelegate)
		{
			await OnExport.InvokeAsync(new MokaTableExportContext<TItem>
			{
				Items = items,
				Columns = _columns.Where(IsColumnVisible).ToList(),
				IsCompleteSet = complete
			});
			return;
		}

		await ExportToCsvAsync(items);
	}

	// Server mode pages, so "all rows" means asking the source for one page big enough to hold
	// the whole result set instead of quietly exporting whatever is on screen.
	private async Task<(IReadOnlyList<TItem> Items, bool Complete)> GetExportItemsAsync()
	{
		if (ServerData is null)
		{
			return (_filteredItems, true);
		}

		MokaTableResult<TItem> result = await ServerData(BuildState(1, FullSetPageSize));
		return (result.Items, result.Items.Count >= result.TotalItems);
	}

	private async Task ExportToCsvAsync(IReadOnlyList<TItem> items)
	{
		var visibleCols = _columns.Where(c => IsColumnVisible(c) && c.Field is not null).ToList();
		var sb = new StringBuilder();

		sb.AppendLine(string.Join(",", visibleCols.Select(c => EscapeCsv(c.Title ?? ""))));

		foreach (TItem item in items)
		{
			IEnumerable<string> values = visibleCols.Select(c => EscapeCsv(GetCellValue(c, item)));
			sb.AppendLine(string.Join(",", values));
		}

		await DownloadCsvAsync(sb.ToString(), $"export-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
	}

	private static string EscapeCsv(string value)
	{
		// A spreadsheet runs a cell that starts with one of these as a formula, so data such as
		// =HYPERLINK(...) would execute when the export is opened (CSV injection, per OWASP). The
		// leading quote makes it text. A number such as -5 holds no formula, so it stays a number.
		if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' && !IsNumber(value))
		{
			value = "'" + value;
		}

		if (value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal) ||
		    value.Contains('\n', StringComparison.Ordinal) || value.Contains('\r', StringComparison.Ordinal))
		{
			return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
		}

		return value;
	}

	// The cell text went through the column's Format in the current culture, so that culture is
	// tried first. No surrounding white space: a leading tab is one of the formula triggers.
	private static bool IsNumber(string value)
	{
		const NumberStyles Styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint
		                            | NumberStyles.AllowThousands | NumberStyles.AllowExponent;

		return double.TryParse(value, Styles, CultureInfo.CurrentCulture, out _)
		       || double.TryParse(value, Styles, CultureInfo.InvariantCulture, out _);
	}

	private async Task DownloadCsvAsync(string csv, string filename)
	{
		string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv));
		await InvokeTableJsAsync("downloadCsv", base64, filename);
	}

	// ── Feature 4: Column visibility helpers ──

	private bool IsColumnVisible(MokaColumn<TItem> col) =>
		_columnVisibility.TryGetValue(col, out bool visible) ? visible : col.Visible;

	private async Task ToggleColumnVisibilityAsync(MokaColumn<TItem> col)
	{
		bool show = !IsColumnVisible(col);
		_columnVisibility[col] = show;
		_layoutVersion++;
		ClampFocusedCell();

		if (!show && DropFiltersWithoutControl())
		{
			_currentPage = 1;
			await LoadDataAsync();
		}
	}

	// A filter's control is the input of a visible, filterable column with that title. A filter
	// whose column was hidden, removed, renamed away or made unfilterable would go on narrowing the
	// rows with nothing on screen to show it or clear it.
	private bool DropFiltersWithoutControl()
	{
		if (_columnFilters.Count == 0)
		{
			return false;
		}

		var orphans = _columnFilters.Keys
			.Where(title => !_columns.Exists(c => c.Title == title && c.Filterable && IsColumnVisible(c)))
			.ToList();
		foreach (string title in orphans)
		{
			_columnFilters.Remove(title);
		}

		return orphans.Count > 0;
	}

	// ── Feature 5: Skeleton row helpers ──

	private static int SkeletonWidth(int rowIndex, int colIndex) =>
		40 + (rowIndex * 7 + colIndex * 13) % 50;

	// ── Feature 8: Refresh ──

	private async Task HandleRefresh()
	{
		if (OnRefresh.HasDelegate)
		{
			await OnRefresh.InvokeAsync();
		}
		else
		{
			await LoadDataAsync();
		}
	}

	// ── Feature 9: Density toggle ──

	private void ToggleDensity() => _dense = !_dense;

	// ── Column Resize ──

	/// <summary>Called from JS when a column resize drag completes.</summary>
	/// <param name="colIndex">Index of the resized column among the visible columns.</param>
	/// <param name="newWidth">The new width in pixels.</param>
	[JSInvokable]
	public void OnColumnResized(int colIndex, double newWidth)
	{
		List<(MokaColumn<TItem> Col, int ColIdx)> visible = VisibleColumnsInOrder();
		if (colIndex < 0 || colIndex >= visible.Count)
		{
			return;
		}

		// Stored against the column itself, so hiding or moving columns cannot hand the width to
		// another one.
		_columnWidths[visible[colIndex].Col] = newWidth;
		StateHasChanged();
	}

	private string? GetColumnWidth(MokaColumn<TItem> col) =>
		_columnWidths.TryGetValue(col, out double w)
			? FormattableString.Invariant($"{w}px")
			: null;

	// Columns register after the first render pass builds the header, so the handles may not be in
	// the DOM yet. Only record the layout version once every expected handle is actually wired.
	private async Task InitColumnResizeAsync()
	{
		int expected = _columns.Count(c => c.Resizable && IsColumnVisible(c));
		if (expected == 0)
		{
			_resizeInitVersion = _layoutVersion;
			return;
		}

		_dotNetRef ??= DotNetObjectReference.Create(this);
		int wired = await SafeModuleInvokeAsync<int>(ModulePath, "initAllColumnResize", _dotNetRef, _wrapperRef);
		if (wired >= expected)
		{
			_resizeInitVersion = _layoutVersion;
		}
	}

	// ── JS interop ──

	private ValueTask InvokeTableJsAsync(string identifier, params object?[] args) =>
		SafeModuleInvokeVoidAsync(ModulePath, identifier, args);

	// ── Column Filters ──

	private string GetColumnFilter(MokaColumn<TItem> col) =>
		col.Title is not null && _columnFilters.TryGetValue(col.Title, out string? val) ? val : "";

	private void HandleColumnFilter(MokaColumn<TItem> col, ChangeEventArgs e)
	{
		if (col.Title is null)
		{
			return;
		}

		string? value = e.Value?.ToString();
		if (string.IsNullOrEmpty(value))
		{
			_columnFilters.Remove(col.Title);
		}
		else
		{
			_columnFilters[col.Title] = value;
		}

		_filterDebounceTimer?.Dispose();
		_filterDebounceTimer = CreateDebouncedReloadTimer();
	}

	// Distinct values come from the rows the table has loaded. Under ServerData that is the
	// current page (every row while the pager is hidden), so a paged Select filter there only
	// lists values visible on that page.
	private IEnumerable<string> GetDistinctValues(MokaColumn<TItem> col)
	{
		if (col.Field is null)
		{
			return [];
		}

		Func<TItem, object?> field = col.Field;
		IEnumerable<TItem> source = Items ?? LoadedItems;

		return source
			.Select(item => field(item)?.ToString())
			.Where(v => !string.IsNullOrEmpty(v))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(v => v, StringComparer.OrdinalIgnoreCase)!;
	}

	private IEnumerable<TItem> ApplyColumnFilters(IEnumerable<TItem> items)
	{
		foreach (MokaColumn<TItem> col in _columns)
		{
			if (col.Title is null || col.Field is null)
			{
				continue;
			}

			if (!_columnFilters.TryGetValue(col.Title, out string? filterValue) || string.IsNullOrEmpty(filterValue))
			{
				continue;
			}

			Func<TItem, object?> field = col.Field;
			items = col.FilterType == MokaColumnFilterType.Select
				? items.Where(item => string.Equals(field(item)?.ToString(), filterValue, StringComparison.OrdinalIgnoreCase))
				: items.Where(item =>
					field(item)?.ToString()?.Contains(filterValue, StringComparison.OrdinalIgnoreCase) == true);
		}

		return items;
	}

	// ── Inline Editing ──

	private bool IsEditing(TItem item, MokaColumn<TItem> col) =>
		_editingCell is not null &&
		EqualityComparer<TItem>.Default.Equals(_editingCell.Value.Item, item) &&
		_editingCell.Value.Column == col;

	private void StartEdit(TItem item, MokaColumn<TItem> col)
	{
		if (!col.Editable)
		{
			return;
		}

		_editingCell = (item, col);
		_editOriginalText = GetCellValue(col, item);
		_editValue = _editOriginalText;
		_editFocusPending = true;
	}

	private async Task CommitEdit(TItem item, MokaColumn<TItem> col, ChangeEventArgs? e = null)
	{
		if (!IsEditing(item, col))
		{
			return;
		}

		string newValue = e?.Value?.ToString() ?? _editValue ?? "";
		object? oldValue = col.Field?.Invoke(item);
		_editingCell = null;

		// Text against text: the editor opened on the cell's text, which Format may have changed
		// from the raw value's ToString().
		if (newValue == _editOriginalText)
		{
			return;
		}

		if (col.OnCellEdited.HasDelegate)
		{
			await col.OnCellEdited.InvokeAsync((item, newValue));
		}

		if (OnCellEdit.HasDelegate)
		{
			await OnCellEdit.InvokeAsync(
				new MokaTableCellEditResult<TItem>(item, col.Title ?? "", oldValue, newValue));
		}
	}

	private async Task HandleEditKeyDown(TItem item, MokaColumn<TItem> col, KeyboardEventArgs e)
	{
		if (e.Key == "Enter")
		{
			_cellFocusPending = true;
			await CommitEdit(item, col);
		}
		else if (e.Key == "Escape")
		{
			_cellFocusPending = true;
			_editingCell = null;
		}
	}

	private void HandleEditInput(ChangeEventArgs e) => _editValue = e.Value?.ToString();

	// ── Aggregates ──

	private static string AggregateLabel(MokaColumn<TItem> col) => col.Aggregate switch
	{
		MokaAggregateType.Sum => "Sum",
		MokaAggregateType.Average => "Avg",
		MokaAggregateType.Count => "Count",
		MokaAggregateType.Min => "Min",
		MokaAggregateType.Max => "Max",
		_ => ""
	};

	private string ComputeAggregate(MokaColumn<TItem> col)
	{
		if (col.Aggregate == MokaAggregateType.None || col.Field is null)
		{
			return "";
		}

		Func<TItem, object?> field = col.Field;
		var values = LoadedItems.Select(item => field(item)).Where(v => v is not null).ToList();

		if (col.Aggregate == MokaAggregateType.Count)
		{
			return values.Count.ToString(col.AggregateFormat ?? "", CultureInfo.CurrentCulture).TrimEnd();
		}

		var numericValues = ToNumericValues(values);
		if (numericValues.Count == 0)
		{
			return "";
		}

		double result = col.Aggregate switch
		{
			MokaAggregateType.Sum => numericValues.Sum(),
			MokaAggregateType.Average => numericValues.Average(),
			MokaAggregateType.Min => numericValues.Min(),
			MokaAggregateType.Max => numericValues.Max(),
			_ => 0.0
		};

		return result.ToString(col.AggregateFormat ?? "N2", CultureInfo.CurrentCulture);
	}

	private static List<double> ToNumericValues(List<object?> values)
	{
		var numericValues = new List<double>(values.Count);
		foreach (object? v in values)
		{
			try
			{
				numericValues.Add(Convert.ToDouble(v, CultureInfo.InvariantCulture));
			}
			catch (FormatException)
			{
				// Skip non-numeric values
			}
			catch (InvalidCastException)
			{
				// Skip non-convertible values
			}
		}

		return numericValues;
	}

	// ── CSS helpers ──

	// Sticky and HideOnMobile have to reach the column's cell in every row the table renders
	// (header, filter row, skeleton, data, aggregate), or those rows stop lining up.
	private static CssBuilder ColumnCellClasses(MokaColumn<TItem> col, string? rootClass = null) =>
		new CssBuilder(rootClass)
			.AddClass("moka-table-cell--sticky", col.Sticky)
			.AddClass("moka-table-cell--hide-mobile", col.HideOnMobile);

	private string HeaderCellClass(MokaColumn<TItem> col) => ColumnCellClasses(col)
		.AddClass("moka-table-header--sortable", CanSort(col))
		.AddClass($"moka-table-cell--{MokaEnumHelpers.ToCssValue(col.Align)}")
		.Build();

	private string? HeaderCellStyle(MokaColumn<TItem> col) => new StyleBuilder()
		.AddStyle("width", GetColumnWidth(col) ?? col.Width)
		.AddStyle("min-width", col.MinWidth)
		.Build();

	private static string FilterCellClass(MokaColumn<TItem> col) =>
		ColumnCellClasses(col, "moka-table-filter-cell").Build();

	private static string SkeletonCellClass(MokaColumn<TItem> col) => ColumnCellClasses(col).Build();

	private static string AggregateCellClass(MokaColumn<TItem> col) => ColumnCellClasses(col, "moka-table-aggregate-cell")
		.AddClass($"moka-table-cell--{MokaEnumHelpers.ToCssValue(col.Align)}")
		.Build();

	private static string DataCellClass(MokaColumn<TItem> col) => ColumnCellClasses(col)
		.AddClass($"moka-table-cell--{MokaEnumHelpers.ToCssValue(col.Align)}")
		.AddClass("moka-table-cell--editable", col.Editable)
		.AddClass(col.CellClass)
		.Build();

	private string? DataCellStyle(MokaColumn<TItem> col) => new StyleBuilder()
		.AddStyle("width", GetColumnWidth(col) ?? col.Width)
		.AddStyle("min-width", col.MinWidth)
		.Build();

	private string RowCssClass(MokaTableRow<TItem> row) => new CssBuilder()
		.AddClass("moka-table-row--selected", _selectedItems.Contains(row.Item))
		.AddClass("moka-table-row--clickable", OnRowClick.HasDelegate)
		.AddClass("moka-table-row--dragging", _draggingRow?.Index == row.Index)
		.AddClass(RowClass?.Invoke(row.Item))
		.Build();

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_disposed = true;

		if (_searchDebounceTimer is not null)
		{
			await _searchDebounceTimer.DisposeAsync();
		}

		if (_filterDebounceTimer is not null)
		{
			await _filterDebounceTimer.DisposeAsync();
		}

		if (_gridKeysInitialized || _resizeInitVersion >= 0 || _reorderDragBound || _tabStopWatchBound)
		{
			await InvokeTableJsAsync("dispose", _wrapperRef);
		}

		_dotNetRef?.Dispose();
		_dotNetRef = null;

		await base.DisposeAsyncCore();
	}

	/// <summary>The <c>@key</c> of a detail row: the key of the data row it belongs to.</summary>
	/// <param name="RowKey">The data row's key.</param>
	private sealed record DetailRowIdentity(object RowKey);
}
