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
	private readonly List<MokaColumn<TItem>> _columns = [];

	// Column resize: stores resized widths keyed by column (title, or visible index when untitled)
	private readonly Dictionary<string, double> _columnWidths = new(StringComparer.Ordinal);

	// Feature 1: Row expand tracking. Keyed on TItem when no ItemKey is supplied so struct items
	// compare by value instead of by a freshly boxed object on every call.
	private readonly HashSet<TItem> _expandedItems = new(EqualityComparer<TItem>.Default);
	private readonly HashSet<object> _expandedKeys = [];

	// Feature 4: Column visibility internal override
	private readonly HashSet<string> _hiddenColumns = new(StringComparer.Ordinal);
	private readonly List<MokaTableSortDescriptor> _sortDescriptors = [];

	// Column reorder
	private int[] _columnOrder = [];

	private int _currentPage = 1;
	private bool _dense = true;
	private IReadOnlyList<TItem> _displayItems = [];
	private List<MokaTableRow<TItem>> _displayRows = [];
	private bool _disposed;
	private DotNetObjectReference<MokaTable<TItem>>? _dotNetRef;
	private int? _draggingColIndex;
	private int? _dragOverColIndex;
	private int? _dragOverRowIndex;
	private MokaTableRow<TItem>? _draggingRow;

	// Inline editing: currently editing cell
	private (TItem Item, MokaColumn<TItem> Column)? _editingCell;
	private bool _editFocusPending;
	private string? _editValue;

	private Timer? _filterDebounceTimer;

	// Full client-side result set after search, column filters and sort. Empty in server mode.
	private List<TItem> _filteredItems = [];

	// Keyboard navigation
	private (int Row, int Col) _focusedCell = (-1, -1);
	private bool _gridKeysInitialized;
	private bool? _indeterminateState;
	private bool _isLoading;
	private int _layoutVersion;
	private int _pageSize = 10;
	private bool _parameterReloadPending;

	// Bug 1: Track data-relevant parameters to avoid redundant reloads and to keep internal
	// state from being reverted by the next parent render.
	private bool _previousDense = true;
	private IEnumerable<TItem>? _previousItems;
	private int _previousPageSize;
	private HashSet<TItem>? _previousSelectedItems;
	private string? _previousSortColumn;
	private MokaSortDirection _previousSortDirection;
	private int _resizeInitVersion = -1;
	private Timer? _searchDebounceTimer;
	private string? _searchTerm;
	private ElementReference _selectAllRef;
	private HashSet<TItem> _selectedItems = [];
	private bool _showColumnMenu;
	private string? _sortColumn;
	private MokaSortDirection _sortDirection = MokaSortDirection.None;
	private int _totalItems;
	private ElementReference _wrapperRef;

	// ── Data ──

	/// <summary>Client-side data. Mutually exclusive with ServerData.</summary>
	[Parameter]
	public IEnumerable<TItem>? Items { get; set; }

	/// <summary>Server-side data callback. Called whenever sort/page/filter changes.</summary>
	[Parameter]
	public Func<MokaTableState, Task<MokaTableResult<TItem>>>? ServerData { get; set; }

	/// <summary>Column definitions (MokaColumn components).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Key selector for the @key directive (performance).</summary>
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

	/// <summary>Items per page. Default 10.</summary>
	[Parameter]
	public int PageSize { get; set; } = 10;

	/// <summary>Callback when page size changes.</summary>
	[Parameter]
	public EventCallback<int> PageSizeChanged { get; set; }

	/// <summary>Dropdown options for page size. Default [10, 25, 50, 100].</summary>
	[Parameter]
	public IReadOnlyList<int> PageSizeOptions { get; set; } = [10, 25, 50, 100];

	/// <summary>Whether to show pagination. Default true.</summary>
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

	/// <summary>Only one row selectable at a time. Default false.</summary>
	[Parameter]
	public bool SingleSelect { get; set; }

	/// <summary>Fires when a row is clicked.</summary>
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

	/// <summary>Currently sorted column (by Title). Two-way bindable.</summary>
	[Parameter]
	public string? SortColumn { get; set; }

	/// <summary>Callback when sort column changes.</summary>
	[Parameter]
	public EventCallback<string?> SortColumnChanged { get; set; }

	/// <summary>Current sort direction. Two-way bindable.</summary>
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

	/// <summary>Template for bulk actions when items are selected. Receives the selected items set.</summary>
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

	/// <summary>Callback when a column is reordered.</summary>
	[Parameter]
	public EventCallback<(int OldIndex, int NewIndex)> OnColumnReordered { get; set; }

	// ── Feature 14: Row Reorder ──

	/// <summary>Allow reordering rows by dragging the grip handle. Default false.</summary>
	[Parameter]
	public bool RowReorderable { get; set; }

	/// <summary>
	///     Callback when a row is dropped on another row. Indexes are absolute across the whole data
	///     set (page offset included), so they address the backing collection directly. The table does
	///     not reorder anything itself: apply the move to your data, then call
	///     <see cref="ReloadAsync" /> if you mutated the collection in place.
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

	private int VisibleColumnCount => _columns.Count(IsColumnVisible);

	private int ColSpan => VisibleColumnCount
	                       + (Selectable ? 1 : 0)
	                       + (Expandable ? 1 : 0)
	                       + (RowReorderable ? 1 : 0)
	                       + (RowActions is not null ? 1 : 0);

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	// ── Aggregation ──

	private bool HasAggregates => _columns.Any(c => c.Aggregate != MokaAggregateType.None);

	// ── Pagination visibility (Top and Bottom share one rule) ──

	private int PageCount => _pageSize > 0
		? Math.Max(1, (int)Math.Ceiling(_totalItems / (double)_pageSize))
		: 1;

	// One rule for both positions. Hidden while everything fits on one page, but kept visible once
	// the user has paged or picked a different page size - otherwise the control that got them
	// there vanishes and they cannot get back.
	private bool ShowPaginationBar => ShowPagination
	                                  && (_totalItems > _pageSize || _currentPage > 1 || _pageSize != PageSize);

	private bool ShowTopPagination => ShowPaginationBar
	                                  && PaginationPosition is MokaTablePaginationPosition.Top
		                                  or MokaTablePaginationPosition.Both;

	private bool ShowBottomPagination => ShowPaginationBar
	                                     && PaginationPosition is MokaTablePaginationPosition.Bottom
		                                     or MokaTablePaginationPosition.Both;

	// ── Selection state (page-scoped, matching HandleSelectAll) ──

	private bool AllPageRowsSelected => _displayItems.Count > 0 && _displayItems.All(_selectedItems.Contains);

	private bool SomePageRowsSelected => !AllPageRowsSelected && _displayItems.Any(_selectedItems.Contains);

	// Aggregates and distinct filter values can only see loaded rows. In server mode that is the
	// current page; in client mode it is the full filtered set.
	private IReadOnlyList<TItem> LoadedItems => ServerData is not null ? _displayItems : _filteredItems;

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
		if (!_columns.Contains(column))
		{
			_columns.Add(column);
			_layoutVersion++;
			StateHasChanged();
		}
	}

	/// <summary>Removes a column definition when a MokaColumn is disposed.</summary>
	internal void RemoveColumn(MokaColumn<TItem> column)
	{
		if (_columns.Remove(column))
		{
			_layoutVersion++;
			StateHasChanged();
		}
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

		if (_resizeInitVersion != _layoutVersion)
		{
			await InitColumnResizeAsync();
		}

		if (_editFocusPending)
		{
			_editFocusPending = false;
			await InvokeTableJsAsync("focusEditInput", _wrapperRef);
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

		if (!itemsChanged && !_parameterReloadPending)
		{
			return;
		}

		_parameterReloadPending = false;
		await LoadDataAsync();
	}

	// PageSize, SortColumn, SortDirection and Dense are parameters the table also changes itself.
	// Internal state lives in backing fields and the parameter only seeds them when the parent
	// actually passes a new value, so an unbound parent re-render cannot revert a user action.
	private void SyncStateFromParameters()
	{
		if (PageSize != _previousPageSize)
		{
			_previousPageSize = PageSize;
			_pageSize = PageSize;
			_parameterReloadPending = true;
		}

		if (SortColumn != _previousSortColumn)
		{
			_previousSortColumn = SortColumn;
			_sortColumn = SortColumn;
			_parameterReloadPending = true;
		}

		if (SortDirection != _previousSortDirection)
		{
			_previousSortDirection = SortDirection;
			_sortDirection = SortDirection;
			_parameterReloadPending = true;
		}

		if (Dense != _previousDense)
		{
			_previousDense = Dense;
			_dense = Dense;
		}
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
				SetDisplayItems([]);
			}
		}
		finally
		{
			_isLoading = false;
		}
	}

	private async Task LoadServerDataAsync(Func<MokaTableState, Task<MokaTableResult<TItem>>> source)
	{
		MokaTableResult<TItem> result = await source(BuildState(_currentPage, _pageSize));
		_totalItems = result.TotalItems;

		// The requested page can be past the end after a filter narrows the result set.
		if (ClampCurrentPage())
		{
			result = await source(BuildState(_currentPage, _pageSize));
			_totalItems = result.TotalItems;
		}

		_filteredItems = [];
		SetDisplayItems(result.Items);
	}

	private void LoadClientData()
	{
		_filteredItems = BuildFilteredItems();
		_totalItems = _filteredItems.Count;
		ClampCurrentPage();
		SetDisplayItems(_filteredItems
			.Skip((_currentPage - 1) * _pageSize)
			.Take(_pageSize)
			.ToList());
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

		if (_sortDescriptors.Count > 0 || (_sortColumn is not null && _sortDirection != MokaSortDirection.None))
		{
			items = SortItems(items);
		}

		return items.ToList();
	}

	private MokaTableState BuildState(int page, int pageSize) => new()
	{
		Page = page,
		PageSize = pageSize,
		SearchTerm = _searchTerm,
		SortColumn = _sortColumn,
		SortDirection = _sortDirection,
		SortDescriptors = _sortDescriptors.ToList(),
		ColumnFilters = new Dictionary<string, string>(_columnFilters, StringComparer.Ordinal)
	};

	private void SetDisplayItems(IReadOnlyList<TItem> items)
	{
		_displayItems = items;
		_displayRows = BuildRows(items);
		_indeterminateState = null;
	}

	private static List<MokaTableRow<TItem>> BuildRows(IReadOnlyList<TItem> items)
	{
		var rows = new List<MokaTableRow<TItem>>(items.Count);
		for (int i = 0; i < items.Count; i++)
		{
			rows.Add(new MokaTableRow<TItem>(items[i], i));
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

	private IEnumerable<TItem> SortItems(IEnumerable<TItem> items)
	{
		if (_sortDescriptors.Count > 0)
		{
			return MultiSortItems(items);
		}

		MokaColumn<TItem>? sortCol = _columns.FirstOrDefault(c => c.Title == _sortColumn);
		if (sortCol?.Field is null)
		{
			return items;
		}

		if (sortCol.SortComparer is not null)
		{
			var comparer = Comparer<TItem>.Create((a, b) => sortCol.SortComparer(a, b));
			return _sortDirection == MokaSortDirection.Descending
				? items.OrderByDescending(x => x, comparer)
				: items.OrderBy(x => x, comparer);
		}

		return _sortDirection == MokaSortDirection.Descending
			? items.OrderByDescending(sortCol.Field)
			: items.OrderBy(sortCol.Field);
	}

	private IEnumerable<TItem> MultiSortItems(IEnumerable<TItem> items)
	{
		IOrderedEnumerable<TItem>? ordered = null;
		foreach (MokaTableSortDescriptor desc in _sortDescriptors.OrderBy(d => d.Priority))
		{
			MokaColumn<TItem>? col = _columns.FirstOrDefault(c => c.Title == desc.Column);
			if (col?.Field is null)
			{
				continue;
			}

			if (ordered is null)
			{
				ordered = desc.Direction == MokaSortDirection.Descending
					? items.OrderByDescending(col.Field)
					: items.OrderBy(col.Field);
			}
			else
			{
				ordered = desc.Direction == MokaSortDirection.Descending
					? ordered.ThenByDescending(col.Field)
					: ordered.ThenBy(col.Field);
			}
		}

		return ordered ?? items;
	}

	private MokaTableSortDescriptor? GetSortDescriptor(string? columnTitle) =>
		_sortDescriptors.FirstOrDefault(d => d.Column == columnTitle);

	// ── Column reorder ──

	private void InitColumnOrder()
	{
		if (_columnOrder.Length != _columns.Count)
		{
			_columnOrder = Enumerable.Range(0, _columns.Count).ToArray();
		}
	}

	private IEnumerable<(MokaColumn<TItem> Col, int ColIdx)> GetOrderedVisibleColumns()
	{
		if (!ColumnReorderable)
		{
			return _columns.Where(IsColumnVisible).Select((c, i) => (c, i));
		}

		InitColumnOrder();
		int visIdx = 0;
		var result = new List<(MokaColumn<TItem>, int)>();
		foreach (int physIdx in _columnOrder)
		{
			if (physIdx < _columns.Count && IsColumnVisible(_columns[physIdx]))
			{
				result.Add((_columns[physIdx], visIdx));
				visIdx++;
			}
		}

		return result;
	}

	private void HandleColumnDragStart(int colIdx) => _draggingColIndex = colIdx;

	private void HandleColumnDragOver(int colIdx) => _dragOverColIndex = colIdx;

	private async Task HandleColumnDrop(int targetIdx)
	{
		if (_draggingColIndex is null || _draggingColIndex == targetIdx)
		{
			_draggingColIndex = null;
			_dragOverColIndex = null;
			return;
		}

		InitColumnOrder();
		var orderList = _columnOrder.ToList();
		int fromPos = orderList.IndexOf(_draggingColIndex.Value);
		int toPos = orderList.IndexOf(targetIdx);
		if (fromPos >= 0 && toPos >= 0)
		{
			int item = orderList[fromPos];
			orderList.RemoveAt(fromPos);
			orderList.Insert(toPos, item);
			_columnOrder = orderList.ToArray();
			_layoutVersion++;
		}

		await OnColumnReordered.InvokeAsync((_draggingColIndex.Value, targetIdx));
		_draggingColIndex = null;
		_dragOverColIndex = null;
	}

	// ── Row reorder ──

	private void HandleRowDragStart(MokaTableRow<TItem> row)
	{
		if (RowReorderable)
		{
			_draggingRow = row;
		}
	}

	private void HandleRowDragOver(int rowIndex)
	{
		if (RowReorderable && _draggingRow is not null)
		{
			_dragOverRowIndex = rowIndex;
		}
	}

	private void HandleRowDragEnd()
	{
		_draggingRow = null;
		_dragOverRowIndex = null;
	}

	private async Task HandleRowDrop(int targetIndex)
	{
		MokaTableRow<TItem>? source = _draggingRow;
		_draggingRow = null;
		_dragOverRowIndex = null;

		if (!RowReorderable || source is null || source.Value.Index == targetIndex)
		{
			return;
		}

		int offset = (_currentPage - 1) * Math.Max(_pageSize, 0);
		await OnRowReordered.InvokeAsync((source.Value.Item, offset + source.Value.Index, offset + targetIndex));
	}

	// ── Keyboard navigation ──

	private string CellTabIndex(int rowIndex, int colIndex)
	{
		bool isRoving = _focusedCell.Row < 0
			? rowIndex == 0 && colIndex == 0
			: _focusedCell == (rowIndex, colIndex);
		return isRoving ? "0" : "-1";
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

	// ── Sorting ──

	private async Task HandleSort(MokaColumn<TItem> column, bool addToExisting = false)
	{
		if (!column.Sortable || column.Title is null)
		{
			return;
		}

		if (MultiSort && addToExisting)
		{
			ApplyMultiSort(column.Title);
		}
		else
		{
			ApplySingleSort(column.Title);
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
		_sortDescriptors.Clear();

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

		if (_sortColumn is not null)
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
		_currentPage = page;
		await LoadDataAsync();
	}

	private async Task HandlePageSizeChange(int pageSize)
	{
		_pageSize = pageSize;
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
		// reference-comparing parent see no change at all.
		var snapshot = new HashSet<TItem>(_selectedItems);
		_previousSelectedItems = snapshot;
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
			_selectedItems.Clear();
			_selectedItems.Add(item);
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

		bool indeterminate = SomePageRowsSelected;
		if (_indeterminateState == indeterminate)
		{
			return;
		}

		_indeterminateState = indeterminate;
		await InvokeTableJsAsync("setIndeterminate", _selectAllRef, indeterminate);
	}

	// ── Rows ──

	private async Task HandleRowClick(TItem item)
	{
		if (OnRowClick.HasDelegate)
		{
			await OnRowClick.InvokeAsync(item);
		}
	}

	private async Task HandleRowContextMenu(TItem item, MouseEventArgs e)
	{
		if (OnRowContextMenu.HasDelegate)
		{
			await OnRowContextMenu.InvokeAsync(new MokaItemContextMenuArgs<TItem>(item, e));
		}
	}

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

	private object RowKey(TItem item) => ItemKey?.Invoke(item) ?? item!;

	private object DetailRowKey(TItem item) => ("moka-table-detail", RowKey(item));

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

		int fetchSize = Math.Max(_totalItems, Math.Max(_pageSize, 1));
		MokaTableResult<TItem> result = await ServerData(BuildState(1, fetchSize));
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
		if (value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal) ||
		    value.Contains('\n', StringComparison.Ordinal))
		{
			return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
		}

		return value;
	}

	private async Task DownloadCsvAsync(string csv, string filename)
	{
		string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv));
		await InvokeTableJsAsync("downloadCsv", base64, filename);
	}

	// ── Feature 4: Column visibility helpers ──

	private bool IsColumnVisible(MokaColumn<TItem> col) =>
		col.Visible && !_hiddenColumns.Contains(col.Title ?? "");

	private void ToggleColumnVisibility(MokaColumn<TItem> col)
	{
		string key = col.Title ?? "";
		if (!_hiddenColumns.Remove(key))
		{
			_hiddenColumns.Add(key);
		}

		_layoutVersion++;
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
		string? key = ResolveColumnKey(colIndex);
		if (key is null)
		{
			return;
		}

		_columnWidths[key] = newWidth;
		StateHasChanged();
	}

	private string? ResolveColumnKey(int visibleIndex)
	{
		foreach ((MokaColumn<TItem> col, int idx) in GetOrderedVisibleColumns())
		{
			if (idx == visibleIndex)
			{
				return ColumnKey(col, idx);
			}
		}

		return null;
	}

	// Widths key on the column title so hiding or reordering a column does not shift them onto
	// a different column. Untitled columns fall back to their visible index.
	private static string ColumnKey(MokaColumn<TItem> col, int colIndex) =>
		col.Title ?? colIndex.ToString(CultureInfo.InvariantCulture);

	private string? GetColumnWidth(MokaColumn<TItem> col, int colIndex) =>
		_columnWidths.TryGetValue(ColumnKey(col, colIndex), out double w)
			? FormattableString.Invariant($"{w}px")
			: null;

	// Columns register after the first render pass builds the header, so the handles may not be in
	// the DOM yet. Only record the layout version once every expected handle is actually wired.
	private async Task InitColumnResizeAsync()
	{
		int expected = GetOrderedVisibleColumns().Count(c => c.Col.Resizable);
		if (expected == 0)
		{
			_resizeInitVersion = _layoutVersion;
			return;
		}

		_dotNetRef ??= DotNetObjectReference.Create(this);
		int wired = await InvokeTableJsAsync<int>("initAllColumnResize", _dotNetRef, _wrapperRef);
		if (wired >= expected)
		{
			_resizeInitVersion = _layoutVersion;
		}
	}

	// ── JS interop ──

	private async Task InvokeTableJsAsync(string identifier, params object?[] args)
	{
		try
		{
			IJSObjectReference module = await GetJsModuleAsync(ModulePath);
			await module.InvokeVoidAsync(identifier, args);
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected - nothing to do
		}
		catch (ObjectDisposedException)
		{
			// Circuit or JS runtime torn down mid-call
		}
		catch (OperationCanceledException)
		{
			// Covers TaskCanceledException too
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
			// JS interop called during prerendering
		}
	}

	private async Task<TResult> InvokeTableJsAsync<TResult>(string identifier, params object?[] args)
	{
		try
		{
			IJSObjectReference module = await GetJsModuleAsync(ModulePath);
			return await module.InvokeAsync<TResult>(identifier, args);
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected - nothing to do
		}
		catch (ObjectDisposedException)
		{
			// Circuit or JS runtime torn down mid-call
		}
		catch (OperationCanceledException)
		{
			// Covers TaskCanceledException too
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
			// JS interop called during prerendering
		}

		return default!;
	}

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
	// current page, so a Select filter there only lists values visible on that page.
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
		_editValue = GetCellValue(col, item);
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

		if (newValue == (oldValue?.ToString() ?? ""))
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
			await CommitEdit(item, col);
		}
		else if (e.Key == "Escape")
		{
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

	private string HeaderCellClass(MokaColumn<TItem> col, int colIndex) => new CssBuilder()
		.AddClass("moka-table-header--sortable", col.Sortable)
		.AddClass($"moka-table-cell--{MokaEnumHelpers.ToCssValue(col.Align)}")
		.AddClass("moka-table-cell--sticky", col.Sticky)
		.AddClass("moka-table-cell--hide-mobile", col.HideOnMobile)
		.AddClass("moka-table-header--drag-over", _dragOverColIndex == colIndex)
		.Build();

	private string? HeaderCellStyle(MokaColumn<TItem> col, int colIndex) => new StyleBuilder()
		.AddStyle("width", GetColumnWidth(col, colIndex) ?? col.Width)
		.AddStyle("min-width", col.MinWidth)
		.Build();

	private static string AggregateCellClass(MokaColumn<TItem> col) => new CssBuilder("moka-table-aggregate-cell")
		.AddClass($"moka-table-cell--{MokaEnumHelpers.ToCssValue(col.Align)}")
		.Build();

	private static string DataCellClass(MokaColumn<TItem> col) => new CssBuilder()
		.AddClass($"moka-table-cell--{MokaEnumHelpers.ToCssValue(col.Align)}")
		.AddClass("moka-table-cell--sticky", col.Sticky)
		.AddClass("moka-table-cell--hide-mobile", col.HideOnMobile)
		.AddClass("moka-table-cell--editable", col.Editable)
		.AddClass(col.CellClass)
		.Build();

	private string? DataCellStyle(MokaColumn<TItem> col, int colIndex) => new StyleBuilder()
		.AddStyle("width", GetColumnWidth(col, colIndex) ?? col.Width)
		.AddStyle("min-width", col.MinWidth)
		.Build();

	private string RowCssClass(MokaTableRow<TItem> row) => new CssBuilder()
		.AddClass("moka-table-row--selected", _selectedItems.Contains(row.Item))
		.AddClass("moka-table-row--clickable", OnRowClick.HasDelegate)
		.AddClass("moka-table-row--dragging", _draggingRow?.Index == row.Index)
		.AddClass("moka-table-row--drag-over", _dragOverRowIndex == row.Index)
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

		if (_gridKeysInitialized || _resizeInitVersion >= 0)
		{
			await InvokeTableJsAsync("dispose", _wrapperRef);
		}

		_dotNetRef?.Dispose();
		_dotNetRef = null;

		await base.DisposeAsyncCore();
	}
}
