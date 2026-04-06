using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
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
	// Column filters: stores filter values keyed by column title
	private readonly Dictionary<string, string> _columnFilters = [];
	private readonly List<MokaColumn<TItem>> _columns = [];

	// Column resize: stores resized widths by column index
	private readonly Dictionary<int, double> _columnWidths = [];

	// Feature 1: Row expand tracking
	private readonly HashSet<object> _expandedRows = [];

	// Feature 4: Column visibility internal override
	private readonly HashSet<string> _hiddenColumns = [];
	private readonly List<MokaTableSortDescriptor> _sortDescriptors = [];

	// Column reorder
	private int[] _columnOrder = [];

	private int _currentPage = 1;
	private IReadOnlyList<TItem> _displayItems = [];
	private bool _disposed;
	private DotNetObjectReference<MokaTable<TItem>>? _dotNetRef;
	private int? _draggingColIndex;
	private int? _dragOverColIndex;

	// Inline editing: currently editing cell
	private (TItem Item, MokaColumn<TItem> Column)? _editingCell;
	private string? _editValue;
	private Timer? _filterDebounceTimer;

	// Keyboard navigation
	private (int Row, int Col) _focusedCell = (-1, -1);
	private bool _isLoading;

	// Bug 1: Track data-relevant parameters to avoid redundant reloads
	private IEnumerable<TItem>? _previousItems;
	private int _previousPageSize;
	private string? _previousSortColumn;
	private MokaSortDirection _previousSortDirection;
	private Timer? _searchDebounceTimer;
	private string? _searchTerm;
	private HashSet<TItem> _selectedItems = [];
	private bool _showColumnMenu;
	private IJSObjectReference? _tableJsModule;
	private int _totalItems;

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

	// ── Selection ──

	/// <summary>Shows checkboxes for row selection. Default false.</summary>
	[Parameter]
	public bool Selectable { get; set; }

	/// <summary>Currently selected items. Two-way bindable.</summary>
	[Parameter]
	[SuppressMessage("Usage", "CA2227:Collection properties should be read only",
		Justification = "Blazor two-way binding requires a setter.")]
	public HashSet<TItem>? SelectedItems { get; set; }

	/// <summary>Callback when selected items change.</summary>
	[Parameter]
	public EventCallback<HashSet<TItem>> SelectedItemsChanged { get; set; }

	/// <summary>Only one row selectable at a time. Default false.</summary>
	[Parameter]
	public bool SingleSelect { get; set; }

	/// <summary>Fires when a row is clicked.</summary>
	[Parameter]
	public EventCallback<TItem> OnRowClick { get; set; }

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

	/// <summary>Dense row spacing. Default true (Moka is dense by default).</summary>
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

	/// <summary>Callback to generate export data. Receives all items (not just current page) and column definitions.</summary>
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

	/// <summary>Whether to show a filter row below the header. Default false.</summary>
	[Parameter]
	public bool ShowFilters { get; set; }

	// ── Feature 11: Inline Editing ──

	/// <summary>Fires when any cell is edited.</summary>
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

	/// <summary>Allow reordering rows by dragging. Default false.</summary>
	[Parameter]
	public bool RowReorderable { get; set; }

	/// <summary>Callback when a row is reordered.</summary>
	[Parameter]
	public EventCallback<(TItem Item, int OldIndex, int NewIndex)> OnRowReordered { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-table-wrapper";

	// ── CSS helpers ──

	private string TableClasses => new CssBuilder("moka-table")
		.AddClass("moka-table--dense", Dense)
		.AddClass("moka-table--striped", Striped)
		.AddClass("moka-table--hoverable", Hoverable)
		.AddClass("moka-table--bordered", Bordered)
		.Build();

	private int ColSpan => _columns.Count(IsColumnVisible)
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

	/// <summary>
	///     Tables always re-render — they have complex internal state (search, selection, expand, sort)
	///     that changes independently of parameters. The base class ShouldRender optimization
	///     is too aggressive for stateful components.
	/// </summary>
	protected override bool ShouldRender() => true;

	/// <summary>Registers a column definition from a child MokaColumn component.</summary>
	internal void AddColumn(MokaColumn<TItem> column)
	{
		if (!_columns.Contains(column))
		{
			_columns.Add(column);
			StateHasChanged();
		}
	}

	/// <summary>Removes a column definition when a MokaColumn is disposed.</summary>
	internal void RemoveColumn(MokaColumn<TItem> column)
	{
		_columns.Remove(column);
		StateHasChanged();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender && _columns.Any(c => c.Resizable))
		{
			await InitColumnResizeAsync();
		}
	}

	// Bug 2: Sync selected items from parent parameter
	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		if (SelectedItems is not null)
		{
			_selectedItems = SelectedItems;
		}
	}

	// Bug 1: Only reload when data-relevant parameters change
	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		bool needsReload = !ReferenceEquals(Items, _previousItems)
		                   || PageSize != _previousPageSize
		                   || SortColumn != _previousSortColumn
		                   || SortDirection != _previousSortDirection;

		if (needsReload)
		{
			_previousItems = Items;
			_previousPageSize = PageSize;
			_previousSortColumn = SortColumn;
			_previousSortDirection = SortDirection;
			await LoadDataAsync();
		}
	}

	private async Task LoadDataAsync()
	{
		_isLoading = true;

		if (ServerData is not null)
		{
			var state = new MokaTableState
			{
				Page = _currentPage,
				PageSize = PageSize,
				SearchTerm = _searchTerm,
				SortColumn = SortColumn,
				SortDirection = SortDirection,
				SortDescriptors = _sortDescriptors.ToList()
			};

			MokaTableResult<TItem> result = await ServerData(state);
			_displayItems = result.Items;
			_totalItems = result.TotalItems;
		}
		else if (Items is not null)
		{
			IEnumerable<TItem> items = Items.AsEnumerable();

			// Search filter
			if (!string.IsNullOrWhiteSpace(_searchTerm))
			{
				items = FilterItems(items, _searchTerm);
			}

			// Column filters
			if (_columnFilters.Count > 0)
			{
				items = ApplyColumnFilters(items);
			}

			// Sort
			if (_sortDescriptors.Count > 0 || (SortColumn is not null && SortDirection != MokaSortDirection.None))
			{
				items = SortItems(items);
			}

			var itemList = items.ToList();
			_totalItems = itemList.Count;

			// Paginate
			_displayItems = itemList
				.Skip((_currentPage - 1) * PageSize)
				.Take(PageSize)
				.ToList();
		}
		else
		{
			_displayItems = [];
			_totalItems = 0;
		}

		_isLoading = false;
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

		MokaColumn<TItem>? sortCol = _columns.FirstOrDefault(c => c.Title == SortColumn);
		if (sortCol?.Field is null)
		{
			return items;
		}

		if (sortCol.SortComparer is not null)
		{
			var comparer = Comparer<TItem>.Create((a, b) => sortCol.SortComparer(a, b));
			return SortDirection == MokaSortDirection.Descending
				? items.OrderByDescending(x => x, comparer)
				: items.OrderBy(x => x, comparer);
		}

		return SortDirection == MokaSortDirection.Descending
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
		}

		await OnColumnReordered.InvokeAsync((_draggingColIndex.Value, targetIdx));
		_draggingColIndex = null;
		_dragOverColIndex = null;
	}

	// ── Keyboard navigation ──

	private async Task HandleCellKeyDown(KeyboardEventArgs e, int rowIndex, int colIndex, TItem item,
		MokaColumn<TItem> col)
	{
		switch (e.Key)
		{
			case "ArrowDown":
				_focusedCell = (Math.Min(rowIndex + 1, _displayItems.Count - 1), colIndex);
				break;
			case "ArrowUp":
				_focusedCell = (Math.Max(rowIndex - 1, 0), colIndex);
				break;
			case "ArrowRight":
				_focusedCell = (rowIndex, colIndex + 1);
				break;
			case "ArrowLeft":
				_focusedCell = (rowIndex, Math.Max(colIndex - 1, 0));
				break;
			case "Home":
				_focusedCell = (0, colIndex);
				break;
			case "End":
				_focusedCell = (_displayItems.Count - 1, colIndex);
				break;
			case " " when Selectable:
				await HandleRowSelect(item);
				break;
		}
	}

	private async Task HandleSort(MokaColumn<TItem> column, bool addToExisting = false)
	{
		if (!column.Sortable || column.Title is null)
		{
			return;
		}

		if (MultiSort && addToExisting)
		{
			MokaTableSortDescriptor? existing = _sortDescriptors.FirstOrDefault(d => d.Column == column.Title);
			if (existing is not null)
			{
				existing.Direction = existing.Direction == MokaSortDirection.Ascending
					? MokaSortDirection.Descending
					: MokaSortDirection.None;
				if (existing.Direction == MokaSortDirection.None)
				{
					_sortDescriptors.Remove(existing);
				}
			}
			else
			{
				_sortDescriptors.Add(new MokaTableSortDescriptor
				{
					Column = column.Title,
					Direction = MokaSortDirection.Ascending,
					Priority = _sortDescriptors.Count + 1
				});
			}

			for (int i = 0; i < _sortDescriptors.Count; i++)
			{
				_sortDescriptors[i].Priority = i + 1;
			}

			// Sync single-sort params with first descriptor
			if (_sortDescriptors.Count > 0)
			{
				SortColumn = _sortDescriptors[0].Column;
				SortDirection = _sortDescriptors[0].Direction;
			}
			else
			{
				SortColumn = null;
				SortDirection = MokaSortDirection.None;
			}
		}
		else
		{
			// Single sort (or multi-sort without Shift)
			_sortDescriptors.Clear();

			if (SortColumn == column.Title)
			{
				SortDirection = SortDirection switch
				{
					MokaSortDirection.Ascending => MokaSortDirection.Descending,
					MokaSortDirection.Descending => MokaSortDirection.None,
					_ => MokaSortDirection.Ascending
				};
				if (SortDirection == MokaSortDirection.None)
				{
					SortColumn = null;
				}
			}
			else
			{
				SortColumn = column.Title;
				SortDirection = MokaSortDirection.Ascending;
			}

			if (SortColumn is not null)
			{
				_sortDescriptors.Add(new MokaTableSortDescriptor
				{
					Column = SortColumn,
					Direction = SortDirection,
					Priority = 1
				});
			}
		}

		await SortColumnChanged.InvokeAsync(SortColumn);
		await SortDirectionChanged.InvokeAsync(SortDirection);
		_currentPage = 1;
		await LoadDataAsync();
	}

	private async Task HandlePageChange(int page)
	{
		_currentPage = page;
		await LoadDataAsync();
	}

	private async Task HandlePageSizeChange(int pageSize)
	{
		PageSize = pageSize;
		_currentPage = 1;
		await PageSizeChanged.InvokeAsync(pageSize);
		await LoadDataAsync();
	}

	// Bug 5: Add _disposed guard to debounce timer callback
	private void HandleSearchInput(ChangeEventArgs e)
	{
		string? term = e.Value?.ToString();
		_searchDebounceTimer?.Dispose();
		_searchDebounceTimer = new Timer(async _ =>
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

					_searchTerm = term;
					_currentPage = 1;
					await LoadDataAsync();
					StateHasChanged();
				});
			}
			catch (ObjectDisposedException)
			{
			}
		}, null, 300, Timeout.Infinite);
	}

	private async Task HandleSelectAll(bool selectAll)
	{
		if (selectAll)
		{
			_selectedItems = new HashSet<TItem>(_displayItems);
		}
		else
		{
			_selectedItems.Clear();
		}

		await SelectedItemsChanged.InvokeAsync(_selectedItems);
	}

	private async Task HandleRowSelect(TItem item)
	{
		if (SingleSelect)
		{
			_selectedItems.Clear();
			_selectedItems.Add(item);
		}
		else
		{
			if (!_selectedItems.Remove(item))
			{
				_selectedItems.Add(item);
			}
		}

		await SelectedItemsChanged.InvokeAsync(_selectedItems);
	}

	private async Task HandleRowClick(TItem item)
	{
		if (OnRowClick.HasDelegate)
		{
			await OnRowClick.InvokeAsync(item);
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

	private bool IsExpanded(TItem item)
	{
		object key = ItemKey?.Invoke(item) ?? item!;
		return _expandedRows.Contains(key);
	}

	private void ToggleExpand(TItem item)
	{
		object key = ItemKey?.Invoke(item) ?? item!;
		if (!_expandedRows.Remove(key))
		{
			_expandedRows.Add(key);
		}
	}

	// ── Feature 3: Export helpers ──

	private async Task HandleExport()
	{
		if (OnExport.HasDelegate)
		{
			List<TItem> allItems = Items?.ToList() ?? _displayItems.ToList();
			await OnExport.InvokeAsync(new MokaTableExportContext<TItem>
			{
				Items = allItems,
				Columns = _columns.Where(c => IsColumnVisible(c)).ToList()
			});
		}
		else
		{
			await ExportToCsvAsync();
		}
	}

	private async Task ExportToCsvAsync()
	{
		var visibleCols = _columns.Where(c => IsColumnVisible(c) && c.Field is not null).ToList();
		var sb = new StringBuilder();

		// Header
		sb.AppendLine(string.Join(",", visibleCols.Select(c => EscapeCsv(c.Title ?? ""))));

		// Rows — use all items, not just current page
		List<TItem> allItems = Items?.ToList() ?? _displayItems.ToList();
		foreach (TItem item in allItems)
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
		try
		{
			_tableJsModule ??= await GetJsModuleAsync("./_content/Moka.Red.Data/moka-table.js");
			string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv));
			await _tableJsModule.InvokeVoidAsync("downloadCsv", base64, filename);
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected — nothing to do
		}
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
	}

	// ── Feature 5: Skeleton row helpers ──

	private static int SkeletonWidth(int rowIndex, int colIndex) =>
		40 + (rowIndex * 7 + colIndex * 13) % 50;

	// ── Feature 6: Selection clear ──

	private async Task ClearSelection()
	{
		_selectedItems.Clear();
		await SelectedItemsChanged.InvokeAsync(_selectedItems);
	}

	// ── Feature 7: Search clear ──

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

	private void ToggleDensity() => Dense = !Dense;

	// ── Column Resize ──

	/// <summary>Called from JS when a column resize drag completes.</summary>
	[JSInvokable]
	public void OnColumnResized(int colIndex, double newWidth)
	{
		_columnWidths[colIndex] = newWidth;
		StateHasChanged();
	}

	private string? GetColumnWidth(int colIndex) => _columnWidths.TryGetValue(colIndex, out double w) ? $"{w}px" : null;

	private async Task InitColumnResizeAsync()
	{
		try
		{
			_tableJsModule ??= await GetJsModuleAsync("./_content/Moka.Red.Data/moka-table.js");
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await _tableJsModule.InvokeVoidAsync("initAllColumnResize", _dotNetRef, Id);
		}
		catch (JSDisconnectedException)
		{
		}
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
		_filterDebounceTimer = new Timer(async _ =>
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

					_currentPage = 1;
					await LoadDataAsync();
					StateHasChanged();
				});
			}
			catch (ObjectDisposedException)
			{
			}
		}, null, 300, Timeout.Infinite);
	}

	private IEnumerable<string> GetDistinctValues(MokaColumn<TItem> col)
	{
		if (col.Field is null || Items is null)
		{
			return [];
		}

		return Items
			.Select(item => col.Field(item)?.ToString())
			.Where(v => v is not null)
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

			if (!_columnFilters.TryGetValue(col.Title, out string? filterValue))
			{
				continue;
			}

			if (string.IsNullOrEmpty(filterValue))
			{
				continue;
			}

			if (col.FilterType == MokaColumnFilterType.Select)
			{
				items = items.Where(item =>
					string.Equals(col.Field(item)?.ToString(), filterValue, StringComparison.OrdinalIgnoreCase));
			}
			else
			{
				items = items.Where(item =>
					col.Field(item)?.ToString()?.Contains(filterValue, StringComparison.OrdinalIgnoreCase) == true);
			}
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

		if (newValue != (oldValue?.ToString() ?? ""))
		{
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

		List<TItem> allItems = Items?.ToList() ?? _displayItems.ToList();
		var values = allItems.Select(item => col.Field(item)).Where(v => v is not null).ToList();

		if (col.Aggregate == MokaAggregateType.Count)
		{
			return values.Count.ToString(col.AggregateFormat ?? "", CultureInfo.CurrentCulture).TrimEnd();
		}

		var numericValues = new List<double>();
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

		if (col.AggregateFormat is not null)
		{
			return result.ToString(col.AggregateFormat, CultureInfo.CurrentCulture);
		}

		return result.ToString("N2", CultureInfo.CurrentCulture);
	}

	// ── CSS helpers ──

	private static string HeaderCellClass(MokaColumn<TItem> col) => new CssBuilder()
		.AddClass("moka-table-header--sortable", col.Sortable)
		.AddClass($"moka-table-cell--{MokaEnumHelpers.ToCssValue(col.Align)}")
		.AddClass("moka-table-cell--sticky", col.Sticky)
		.AddClass("moka-table-cell--hide-mobile", col.HideOnMobile)
		.Build();

	private string? HeaderCellStyle(MokaColumn<TItem> col, int colIndex) => new StyleBuilder()
		.AddStyle("width", GetColumnWidth(colIndex) ?? col.Width)
		.AddStyle("min-width", col.MinWidth)
		.Build();

	private static string DataCellClass(MokaColumn<TItem> col) => new CssBuilder()
		.AddClass($"moka-table-cell--{MokaEnumHelpers.ToCssValue(col.Align)}")
		.AddClass("moka-table-cell--sticky", col.Sticky)
		.AddClass("moka-table-cell--hide-mobile", col.HideOnMobile)
		.AddClass("moka-table-cell--editable", col.Editable)
		.AddClass(col.CellClass)
		.Build();

	private string? DataCellStyle(MokaColumn<TItem> col, int colIndex) => new StyleBuilder()
		.AddStyle("width", GetColumnWidth(colIndex) ?? col.Width)
		.AddStyle("min-width", col.MinWidth)
		.Build();

	private string RowCssClass(TItem item) => new CssBuilder()
		.AddClass("moka-table-row--selected", _selectedItems.Contains(item))
		.AddClass("moka-table-row--clickable", OnRowClick.HasDelegate)
		.AddClass(RowClass?.Invoke(item))
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

		_dotNetRef?.Dispose();
		_dotNetRef = null;

		if (_tableJsModule is not null)
		{
			try
			{
				await _tableJsModule.DisposeAsync();
			}
			catch (JSDisconnectedException)
			{
				// Circuit already disconnected
			}

			_tableJsModule = null;
		}

		await base.DisposeAsyncCore();
	}
}
