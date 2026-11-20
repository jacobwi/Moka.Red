using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Interactions;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Primitives.Tooltip;

namespace Moka.Red.Data.Table;

/// <summary>
///     Data table with sorting, filtering, search, paging, selection, grouping, row detail, export and arranged columns,
///     over rows in memory or a server-side source. Everything someone can change is one <see cref="MokaTableState" />,
///     bindable and restorable.
/// </summary>
/// <remarks>
///     The table is split by concern: <c>MokaTable.Data.cs</c> loads rows through the pure steps of
///     <c>MokaTablePipeline</c>, <c>MokaTable.State.cs</c> keeps the state record, and the sorting, filtering, paging,
///     selection, editing, export and keyboard parts each have a file of their own.
/// </remarks>
/// <typeparam name="TItem">The row data type.</typeparam>
[CascadingTypeParameter(nameof(TItem))]
public partial class MokaTable<TItem> : MokaVisualComponentBase, IMokaTablePartHost
{
	private const string ModulePath = "./_content/Moka.Red.Data/moka-table.js";
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	// Registered columns in display order: the order they registered in, until the user drags a
	// header. The column instance is the column's identity; the title is only its name in sort and
	// filter state.
	private readonly List<MokaColumn<TItem>> _columns = [];

	// What the column toggle set, per column. A column's Visible parameter is the starting state,
	// and a new Visible value from the parent removes the entry.
	private readonly Dictionary<MokaColumn<TItem>, bool> _columnVisibility = [];

	// Widths the user dragged, per column.
	private readonly Dictionary<MokaColumn<TItem>, double> _columnWidths = [];

	// Pins the state named, per column. The table carries them in its state; drawing them is the pinning's job.
	private readonly Dictionary<MokaColumn<TItem>, MokaColumnPin> _columnPins = [];

	// A column was hidden or removed during a render and took its filter with it, so the rows have
	// to be loaded again once that render is done.
	private bool _columnReloadPending;
	private DotNetObjectReference<MokaTable<TItem>>? _dotNetRef;
	private MokaColumn<TItem>? _draggingColumn;
	private MokaTableRow<TItem>? _draggingRow;

	// Keyboard navigation
	private (int Row, int Col) _focusedCell = (-1, -1);
	private bool _gridKeysInitialized;
	private bool _hasLoaded;
	private int _layoutVersion;
	private bool _parameterReloadPending;
	private IEnumerable<TItem>? _previousItems;
	private bool _previousShowPagination = true;
	private int _resizeInitVersion = -1;

	// Whether moka-table.js is marking drop targets, which it does while columns or rows can be
	// dragged.
	private bool _reorderDragBound;
	private bool _showColumnMenu;
	private readonly string _columnMenuKey = $"moka-table-column-menu-{Guid.NewGuid():N}";
	private ElementReference _columnMenuRef;
	private bool _columnMenuShown;
	private ElementReference _columnToggleRef;

	// Whether moka-table.js keeps a stand-in tab stop, which it does while Virtualize is on.
	private bool _tabStopWatchBound;
	private bool _tabStopMoved;
	private ElementReference _wrapperRef;

	/// <summary>Creates the table.</summary>
	public MokaTable()
	{
		_selection = new MokaInputValue<HashSet<TItem>>(ShowParentSelection, DispatchExceptionAsync,
			comparer: ReferenceEqualityComparer.Instance);
		_parts = new MokaChildList<object>(PartsSettled, _ => SyncDeclaredOrder(), IMokaTableOrderedPart.OrderOf);
		_drawCapture = draw => _draw = (MokaTableDraw)draw;
	}

	[Inject] private IServiceProvider Services { get; set; } = default!;

	[CascadingParameter] private MokaTheme? CascadedTheme { get; set; }

	// ── Data ──

	/// <summary>Client-side data. Mutually exclusive with ServerData.</summary>
	[Parameter]
	public IEnumerable<TItem>? Items { get; set; }

	/// <summary>
	///     Server-side data callback. Called whenever the sort, page, search or filters change. While
	///     <see cref="ShowPagination" /> is false it is asked for page 1 with a page size that covers
	///     the whole result set. That size comes from the last
	///     <see cref="MokaTableResult{TItem}.TotalItems" />, so a load that finds more rows than that
	///     (the first load, for example) makes a second call sized to the new total.
	/// </summary>
	[Parameter]
	public Func<MokaTableRequest, Task<MokaTableResult<TItem>>>? ServerData { get; set; }

	/// <summary>Column definitions (MokaColumn components and the column kinds), and a <see cref="MokaTablePager" />.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Identifies a row across renders. It becomes the row's <c>@key</c>, so a row keeps its DOM
	///     and component state when sorting, filtering or a reload moves it, and it records which rows
	///     are expanded, which is how <see cref="MokaTableState.Expanded" /> names them. A picked row whose data changed
	///     stays picked under the same key too, the new item taking the old one's place. Without it the item itself is
	///     the key. A key that more than one rendered row returns identifies none of them: those rows render unkeyed
	///     instead of making Blazor throw on the repeated key, and expanding one of them expands them all.
	/// </summary>
	[Parameter]
	public Func<TItem, object>? ItemKey { get; set; }

	// ── Toolbar ──

	/// <summary>
	///     Table title shown in toolbar. It is only shown: <see cref="Label" /> names the table for screen readers.
	/// </summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>
	///     Your own content at the start of the toolbar, shown in place of <see cref="Title" />, such as a count with a
	///     status and an action. <see cref="Label" /> still names the table for screen readers.
	/// </summary>
	[Parameter]
	public RenderFragment? TitleContent { get; set; }

	/// <summary>Shows search input in toolbar. Default false.</summary>
	[Parameter]
	public bool Searchable { get; set; }

	/// <summary>Custom content in the toolbar (action buttons, filters).</summary>
	[Parameter]
	public RenderFragment? ToolbarContent { get; set; }

	/// <summary>Whether to show the toolbar. Default true.</summary>
	[Parameter]
	public bool ShowToolbar { get; set; } = true;

	/// <summary>
	///     Whether to draw the header row. Default true. Without it the sort headings, the filter row, column resizing and
	///     moving, and the select-all box aren't drawn either, so <see cref="Label" /> is what names the table.
	/// </summary>
	[Parameter]
	public bool ShowHeader { get; set; } = true;

	/// <summary>
	///     Every word the table writes, its pager's and its column kinds' included. Null, the default, is
	///     <see cref="MokaTableText.Default" />, in English.
	/// </summary>
	[Parameter]
	public MokaTableText? Text { get; set; }

	// ── Selection ──

	/// <summary>Shows checkboxes for row selection. Default false.</summary>
	[Parameter]
	public bool Selectable { get; set; }

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

	/// <summary>
	///     How the table draws its rows. Null, the default, takes the theme's <c>Tables</c> (<c>MokaTheme.WithTables</c>),
	///     else <see cref="MokaTableLook.Lined" />, the table as it has always looked. The new design draws every look; the
	///     original design draws <see cref="MokaTableLook.Lined" /> whatever is asked, except
	///     <see cref="MokaTableLook.List" />, which works on both.
	/// </summary>
	[Parameter]
	public MokaTableLook? Look { get; set; }

	/// <summary>Alternating row colors. Default false.</summary>
	[Parameter]
	public bool Striped { get; set; }

	/// <summary>Row hover highlight. Default true.</summary>
	[Parameter]
	public bool Hoverable { get; set; } = true;

	/// <summary>Cell borders. Default false.</summary>
	[Parameter]
	public bool Bordered { get; set; }

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

	// ── Responsive ──

	/// <summary>Horizontal scroll on small screens. Default true.</summary>
	[Parameter]
	public bool Responsive { get; set; } = true;

	// ── Row detail ──

	/// <summary>Template for expanded row detail content.</summary>
	[Parameter]
	public RenderFragment<TItem>? DetailTemplate { get; set; }

	/// <summary>Whether row detail expansion is enabled. Default false.</summary>
	[Parameter]
	public bool Expandable { get; set; }

	// ── Footer ──

	/// <summary>Footer row content. Rendered in a tfoot element.</summary>
	[Parameter]
	public RenderFragment? FooterContent { get; set; }

	// ── Column visibility toggle ──

	/// <summary>Whether to show a column visibility toggle in the toolbar. Default false.</summary>
	[Parameter]
	public bool ShowColumnToggle { get; set; }

	// ── Loading skeleton ──

	/// <summary>Number of skeleton rows to show while loading. Default 5. Set to 0 for spinner mode.</summary>
	[Parameter]
	public int SkeletonRows { get; set; } = 5;

	// ── Row actions ──

	/// <summary>Template for per-row action buttons. Rendered in a dedicated action column at the end.</summary>
	[Parameter]
	public RenderFragment<TItem>? RowActions { get; set; }

	/// <summary>Width of the action column. Default "auto".</summary>
	[Parameter]
	public string ActionsColumnWidth { get; set; } = "auto";

	// ── Refresh ──

	/// <summary>Whether to show a refresh button in the toolbar. Default false. Useful for server-side data.</summary>
	[Parameter]
	public bool ShowRefresh { get; set; }

	/// <summary>Callback when refresh is clicked. If not set, reloads data automatically.</summary>
	[Parameter]
	public EventCallback OnRefresh { get; set; }

	// ── Density ──

	/// <summary>
	///     Whether the toolbar has a button that picks the density, compact, standard or comfortable. Default false. The
	///     pick is part of the <see cref="State" />.
	/// </summary>
	[Parameter]
	public bool ShowDensityToggle { get; set; }

	// ── Virtualized rows ──

	/// <summary>
	///     When true, uses virtualization to render only visible rows. Requires a fixed Height. Default false. With
	///     <see cref="ServerData" /> the rows load window by window as they scroll into view.
	/// </summary>
	[Parameter]
	public bool Virtualize { get; set; }

	/// <summary>Estimated row height for virtualization. Default 36.</summary>
	[Parameter]
	public float VirtualRowHeight { get; set; } = 36;

	// ── Column reorder ──

	/// <summary>Allow reordering columns by dragging headers. Default false.</summary>
	[Parameter]
	public bool ColumnReorderable { get; set; }

	/// <summary>
	///     Callback after a header is dropped on another one. The table has already moved the column.
	///     OldIndex and NewIndex are its positions among the visible columns before and after the move.
	/// </summary>
	[Parameter]
	public EventCallback<(int OldIndex, int NewIndex)> OnColumnReordered { get; set; }

	// ── Row reorder ──

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
		.AddClass("moka-table-wrapper--sticky-pagination", Pager.Sticky)
		.AddClass("moka-table-wrapper--framed", Framed)
		.AddClass("moka-table-wrapper--bleed", Bleed)
		.AddClass("moka-table-wrapper--bulk", BulkActions is not null)
		.AddClass("moka-table-wrapper--phone", IsPhoneList)
		.AddClass("moka-table-wrapper--page-header", HeaderSticksToPage)
		.AddClass(LookClass)
		.AddClass(Class)
		.Build();

	internal MokaTableText Words => Text ?? MokaTableText.Default;

	internal IReadOnlyList<TItem> ShownItems => _displayItems;

	// ── Looks ──

	private MokaTableLook ResolvedLook
	{
		get
		{
			MokaTableLook look = Look ?? (OnNewDesign ? CascadedTheme?.Tables : null) ?? MokaTableLook.Lined;
			return look == MokaTableLook.List || OnNewDesign ? look : MokaTableLook.Lined;
		}
	}

	private bool OnNewDesign => CascadedTheme?.Look != MokaLook.Matrix;

	private bool IsListLook => ResolvedLook == MokaTableLook.List;

	private bool ShowRowNumbers => ResolvedLook == MokaTableLook.Sheet;

	private bool DrawHeader => ShowHeader && !IsListLook;

	private string? LookClass => ResolvedLook switch
	{
		MokaTableLook.Lined => null,
		MokaTableLook.SoftStripes => "moka-table-wrapper--look-soft-stripes",
		MokaTableLook.Cards => "moka-table-wrapper--look-cards",
		MokaTableLook.Sheet => "moka-table-wrapper--look-sheet",
		MokaTableLook.Airy => "moka-table-wrapper--look-airy",
		MokaTableLook.Framed => "moka-table-wrapper--look-framed",
		MokaTableLook.List => "moka-table-wrapper--look-list",
		_ => null
	};

	// ── CSS helpers ──

	private string TableClasses => new CssBuilder("moka-table")
		.AddClass(DensityClass)
		.AddClass("moka-table--striped", Striped)
		.AddClass("moka-table--hoverable", Hoverable)
		.AddClass("moka-table--bordered", Bordered)
		.AddClass("moka-table--phone", IsPhoneList)
		.AddClass("moka-table--list", IsListLook)
		.AddClass("moka-table--column-window", VirtualizeColumns)
		.Build();

	private string DensityClass => _density switch
	{
		MokaTableDensity.Standard => "moka-table--standard",
		MokaTableDensity.Comfortable => "moka-table--comfortable",
		_ => "moka-table--dense"
	};

	private string ContainerClass => new CssBuilder("moka-table-container")
		.AddClass("moka-table-container--responsive", Responsive || MinWidth is not null)
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

	private static string SortIconClass(MokaTableSort? sort) => new CssBuilder("moka-table-sort-icon")
		.AddClass("moka-table-sort-icon--active", sort is not null)
		.Build();

	private static string? SkeletonBarStyle(int rowIndex, int colIndex) => new StyleBuilder()
		.AddStyle("width", FormattableString.Invariant($"{SkeletonWidth(rowIndex, colIndex)}%"))
		.Build();

	private string ReorderCellClass => new CssBuilder("moka-table-cell--reorder")
		.AddClass("moka-table-cell--reorder-off", !CanReorderRows)
		.Build();

	private string RowDraggable => CanReorderRows ? "true" : "false";

	private string? ReorderOffReason => CanReorderRows ? null : Words.ReorderOff;

	// The toolbar's buttons are a row along the top of the table, so their names open below it. The last one
	// lines up with the table's edge, unless the app's own toolbar content comes after it.
	private MokaTooltipPosition ToolbarTooltip(ToolbarButton button) =>
		ToolbarContent is null && button == LastToolbarButton ? MokaTooltipPosition.BottomEnd : MokaTooltipPosition.Bottom;

	private ToolbarButton LastToolbarButton =>
		ShowDensityToggle ? ToolbarButton.Density
		: ShowRefresh ? ToolbarButton.Refresh
		: Exportable ? ToolbarButton.Export
		: ToolbarButton.Columns;

	// The table's own toolbar buttons, in the order they are drawn.
	private enum ToolbarButton
	{
		Columns,
		Export,
		Refresh,
		Density
	}

	private int VisibleColumnCount => _columns.Count(IsColumnVisible);

	private int ColSpan => VisibleColumnCount + LeadingControlCells + TrailingControlCells;

	// OnRowReordered reports positions in Items (or in the unsorted server result), and the rows only stand in that order
	// while no sort or grouping reorders them and no search, filter or quick filter leaves rows out.
	private bool CanReorderRows => RowReorderable
	                               && _sorts.Count == 0
	                               && _groups.Count == 0
	                               && !FiltersActive;

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
	/// <returns>A task that completes once the rows are loaded.</returns>
	public async Task ReloadAsync()
	{
		await LoadDataAsync();
		StateHasChanged();
	}

	/// <summary>
	///     Loads the rows again like <see cref="ReloadAsync" />, but keeps the rows on screen until the new ones arrive
	///     instead of showing the loading state. For a list refreshed on a timer: with an <see cref="ItemKey" /> only the
	///     cells that changed are sent to the browser.
	/// </summary>
	/// <returns>A task that completes once the rows are loaded.</returns>
	public async Task RefreshAsync()
	{
		await LoadDataAsync(keepRows: _displayRows.Count > 0);
		StateHasChanged();
	}

	/// <summary>Registers a column definition from a child MokaColumn component.</summary>
	internal void AddColumn(MokaColumn<TItem> column)
	{
		if (_columns.Contains(column))
		{
			return;
		}

		if (GroupHolding(column) is { } group)
		{
			group.AddColumn(column);
		}
		else
		{
			_parts.Add(column);
		}

		int at = RegistrationIndex(column);
		if (at >= _registeredColumns.Count)
		{
			_registeredColumns.Add(column);
			_columns.Add(column);
		}
		else
		{
			MokaColumn<TItem>? before = at > 0 ? _registeredColumns[at - 1] : null;
			_registeredColumns.Insert(at, column);
			_columns.Insert(before is null ? 0 : _columns.IndexOf(before) + 1, column);
		}
		column.ActionKey = ++_nextActionKey;
		if (_arrangementInForce is { } arrangement)
		{
			ApplyArrangement(arrangement);
		}

		_layoutVersion++;

		bool defaultSortMoved = _sortsFromDefault && column.DefaultSort != MokaSortDirection.None;
		if (defaultSortMoved)
		{
			_sorts.Clear();
			_sorts.AddRange(DefaultSorts());
		}

		// Client-side rows first load before any column has registered, so a sort, a filter or a grouping the state
		// names had no column to work with. The render queued below shows them. A server-side source's first load waits
		// for the columns, so their default sorts reach the first request; a column that arrives later reloads.
		// A filterable column's values are counted from the rows, which first loaded before it was here, and a search
		// reads every column's text.
		bool searched = column.HasValue && !string.IsNullOrWhiteSpace(_searchTerm);
		if (_hasLoaded && !_restorePending && !_firstLoadPending
		    && (defaultSortMoved || StateNames(column.Title) || searched || (column.Filterable && ServerData is null)))
		{
			if (ServerData is null)
			{
				LoadClientData();
			}
			else if (defaultSortMoved)
			{
				_columnReloadPending = true;
			}
			else
			{
				GroupServerRows();
			}
		}

		_parts.Render();
	}

	/// <summary>Reports where a column stands, from its <c>OnParametersSet</c>.</summary>
	internal void SawColumn(MokaColumn<TItem> column)
	{
		if (GroupHolding(column) is { } group)
		{
			group.SawColumn(column);
		}
		else
		{
			_parts.Saw(column);
		}
	}

	/// <summary>Removes a column definition when a MokaColumn is disposed.</summary>
	internal void RemoveColumn(MokaColumn<TItem> column)
	{
		if (!_columns.Remove(column))
		{
			return;
		}

		_registeredColumns.Remove(column);
		column.Group?.RemoveColumn(column);
		_parts.Remove(column);
		_columnVisibility.Remove(column);
		_columnWidths.Remove(column);
		_columnPins.Remove(column);
		_columnHandlers.Remove(column);
		_columnSamples.Remove(column);
		if (column.Filterable && ServerData is null && _hasLoaded && !_restorePending)
		{
			LoadClientData();
		}
		_rowDrafts.Remove(column);
		if (ReferenceEquals(_menuColumn, column))
		{
			_menuColumn = null;
		}

		if (ReferenceEquals(_filterColumn, column))
		{
			_filterColumn = null;
		}
		if (DropFiltersWithoutControl())
		{
			_columnReloadPending = true;
		}

		_layoutVersion++;
		ClampFocusedCell();
		_parts.Render();
	}

	/// <summary>
	///     Called by a column when the parent passes it a new value for a setting the table renders. During a render of
	///     the table its markup draws after the columns have their parameters; a column that gets them later renders the
	///     table again, in the same batch.
	/// </summary>
	/// <param name="column">The column whose settings changed.</param>
	/// <param name="visibleChanged">Whether <c>Visible</c> is one of them.</param>
	/// <param name="pinChanged">Whether <c>Pin</c> is one of them.</param>
	/// <param name="previousTitle">The title the column had until now.</param>
	internal void OnColumnChanged(MokaColumn<TItem> column, bool visibleChanged, bool pinChanged, string? previousTitle)
	{
		// The parent's new Visible or Pin value replaces what the column toggle, the menu or the state chose.
		if (visibleChanged)
		{
			_columnVisibility.Remove(column);
			_arrangementInForce = null;
		}

		if (pinChanged)
		{
			_columnPins.Remove(column);
			_arrangementInForce = null;
		}

		_columnSamples.Remove(column);

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
		_parts.Render();
	}

	/// <summary>Draws the table again for a column whose overlay changed.</summary>
	internal void RedrawForColumn() => StateHasChanged();

	// Sorts, filters and groups name a column by its title, so a renamed column takes them along instead of leaving
	// them under a title no column has. A column whose title goes away can no longer be sorted, filtered or grouped
	// by, so they go with it, and the method returns true because the rows have to be loaded again.
	private bool RenameColumnState(string? previousTitle, string? title)
	{
		if (previousTitle is null)
		{
			return false;
		}

		bool changed = false;
		for (int i = _sorts.Count - 1; i >= 0; i--)
		{
			if (_sorts[i].Column == previousTitle)
			{
				if (title is null)
				{
					_sorts.RemoveAt(i);
					changed = true;
				}
				else
				{
					_sorts[i] = _sorts[i] with { Column = title };
				}
			}
		}

		bool filtersMoved = false;
		for (int i = _filters.Count - 1; i >= 0; i--)
		{
			if (_filters[i].Column == previousTitle)
			{
				if (title is null)
				{
					_filters.RemoveAt(i);
					changed = true;
				}
				else
				{
					_filters[i] = _filters[i] with { Column = title };
					filtersMoved = true;
				}
			}
		}

		if (filtersMoved)
		{
			ResetFilterBoxes();
		}

		int group = _groups.IndexOf(previousTitle);
		if (group >= 0)
		{
			if (title is null)
			{
				_groups.RemoveAt(group);
				changed = true;
			}
			else
			{
				_groups[group] = title;
			}
		}

		return changed;
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		_selection.AfterRender();

		await SyncColumnMenuAsync();
		await SyncDensityMenuAsync();
		await SyncExportMenuAsync();

		if (firstRender && !_gridKeysInitialized)
		{
			_gridKeysInitialized = true;
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await InvokeTableJsAsync("initGridKeys", _wrapperRef, _dotNetRef);
		}

		if (firstRender && _restorePending)
		{
			await RestoreStoredStateAsync();
		}

		// The script moved the tab stop between renders, on elements the render tree does not know have
		// changed, so a render that moved rows can leave two. This puts it where the render says it is.
		if (_tabStopMoved)
		{
			_tabStopMoved = false;
			await InvokeTableJsAsync("setTabStop", _wrapperRef, Math.Max(_focusedCell.Row, 0), Math.Max(_focusedCell.Col, 0));
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

		await SyncPinsAsync();

		await SyncRangesAsync();

		await SyncColumnWatchAsync();

		await SyncEndlessAsync();

		// A column change during the render dropped a filter, one that lost its control. The rows
		// it filtered have to come back, and a render is no place to load them.
		if (_columnReloadPending)
		{
			_columnReloadPending = false;
			_currentPage = 1;
			await LoadDataAsync();
			StateHasChanged();
			await NotifyStateChangedAsync();
		}

		if (_editFocusPending)
		{
			await FocusOpenEditorAsync();
		}
		else if (_cellFocusPending)
		{
			_cellFocusPending = false;
			if (_focusedCell.Row >= 0)
			{
				await InvokeTableJsAsync("focusCell", _wrapperRef, _focusedCell.Row, _focusedCell.Col);
			}
		}

		if (_focusViewName)
		{
			_focusViewName = false;
			await SafeModuleInvokeVoidAsync(KeysModule, "preventKeys", _viewNameRef, ViewNameKeyRules);
			await SafeFocusAsync(_viewNameRef);
		}
		else if (_focusAfterRender is { } selectors)
		{
			_focusAfterRender = null;
			await InvokeTableJsAsync("focusFirst", _wrapperRef, selectors);
		}

		if (_resyncBoxRow is { } boxRow)
		{
			_resyncBoxRow = null;
			if (TryGetRow(boxRow, out TItem item))
			{
				await InvokeTableJsAsync("setRowBox", _wrapperRef, boxRow, IsPicked(item));
			}
		}

		await SyncSelectAllIndeterminateAsync();
	}

	// The wrapper hides its overflow for the rounded frame, which cut the column menu off under a short table.
	// The menu goes to the top layer instead, out of reach of that and of later stacking contexts.
	// The selection bar takes the toolbar's place, menu and all, while rows are selected.
	private async Task SyncColumnMenuAsync()
	{
		bool columnMenuOpen = _showColumnMenu && ShowColumnToggle && ShowToolbar && !SelectionBarShown;
		if (_columnMenuShown == columnMenuOpen)
		{
			return;
		}

		_columnMenuShown = columnMenuOpen;
		if (columnMenuOpen)
		{
			await SafeModuleInvokeVoidAsync(PopupModule, "open", _columnMenuKey, _columnMenuRef, _columnToggleRef, null,
				new Dictionary<string, object?> { ["placement"] = "bottom-end" });
		}
		else
		{
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _columnMenuKey);
		}
	}

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		TakeSelection(parameters);
		_pageGiven = parameters.TryGetValue<int>(nameof(Page), out _);
		_searchGiven = parameters.TryGetValue<string?>(nameof(Search), out _);
		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		SyncStream();
		SyncDefaultsFromParameters();
		SyncStateFromParameters();
		SyncInitiallyCollapsed();
		EndAllMatchingIfTheMatchChanged();
	}

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

		_parameterReloadPending = false;

		// A stored state comes from the browser after the first render, so the first load waits for it rather than
		// loading the default rows and then the stored ones. A render that won't go interactive loads straight away.
		if (!_hasLoaded && StorageKey is not null && RendererInfo.IsInteractive)
		{
			_hasLoaded = true;
			_restorePending = true;
			_isLoading = true;
			return;
		}

		bool firstLoad = !_hasLoaded;
		_hasLoaded = true;
		if (_restorePending)
		{
			return;
		}

		// The columns join the table while its first render is drawn. A server-side source waits for that render, through
		// Task.Yield, so the columns' default sorts reach its first request rather than costing a second one. Rows in
		// memory load at once, so they are in the first render, and a sort that arrives with a column orders them again.
		if (firstLoad && ServerData is not null)
		{
			_firstLoadPending = true;
			_isLoading = true;
			await Task.Yield();
			if (!_firstLoadPending)
			{
				return;
			}
		}

		await LoadDataAsync();
	}

	// Hiding or showing the pager changes which rows are loaded, not just the chrome.
	private void SyncDefaultsFromParameters()
	{
		SyncPageSizeFromParameters();
		SyncDensityFromParameters();
		SyncGroupByFromParameters();
		SyncFilterControls();
		SyncQuickFilters();

		if (ShowPagination != _previousShowPagination)
		{
			_previousShowPagination = ShowPagination;
			_parameterReloadPending = true;
		}
	}

	// ── Column visibility ──

	private bool IsColumnVisible(MokaColumn<TItem> col) =>
		_columnVisibility.TryGetValue(col, out bool visible) ? visible : col.Visible;

	private Task ToggleColumnVisibilityAsync(MokaColumn<TItem> col) => SetColumnVisibleAsync(col, !IsColumnVisible(col));

	private async Task SetColumnVisibleAsync(MokaColumn<TItem> col, bool show)
	{
		if (show == IsColumnVisible(col))
		{
			return;
		}

		_columnVisibility[col] = show;
		_layoutVersion++;
		ClampFocusedCell();

		if (!show && DropFiltersWithoutControl())
		{
			_currentPage = 1;
			await LoadDataAsync();
		}

		await NoteArrangementChangedAsync();
	}

	// ── Skeleton rows ──

	private static int SkeletonWidth(int rowIndex, int colIndex) =>
		40 + (rowIndex * 7 + colIndex * 13) % 50;

	// ── Refresh ──

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

	// ── JS interop ──

	private ValueTask InvokeTableJsAsync(string identifier, params object?[] args) =>
		SafeModuleInvokeVoidAsync(ModulePath, identifier, args);

	// ── CSS helpers ──

	private string HeaderCellClass(MokaColumn<TItem> col, int colIdx) => ColumnCellClasses(col, colIdx)
		.AddClass("moka-table-header--sortable", CanSort(col))
		.AddClass("moka-table-header--tools", ColumnMenu || (HasControl(MokaTableFilterControls.Menus) && col.Filterable))
		.AddClass($"moka-table-cell--{MokaEnumHelpers.ToCssValue(col.Align)}")
		.AddClass(PhoneSlotClass(col))
		.Build();

	private string? HeaderCellStyle(MokaColumn<TItem> col, int colIdx) => PinStyle(col, colIdx)
		.AddStyle("width", GetColumnWidth(col) ?? col.Width ?? (VirtualizeColumns ? WindowColumnWidth : null))
		.AddStyle("min-width", col.MinWidth)
		.Build();

	private string FilterCellClass(MokaColumn<TItem> col, int colIdx) =>
		ColumnCellClasses(col, colIdx, "moka-table-filter-cell").Build();

	private string SkeletonCellClass(MokaColumn<TItem> col, int colIdx) => ColumnCellClasses(col, colIdx)
		.AddClass(PhoneSlotClass(col))
		.AddClass("moka-table-cell--phone-only", colIdx < 0)
		.Build();

	private string AggregateCellClass(MokaColumn<TItem> col, int colIdx) => ColumnCellClasses(col, colIdx, "moka-table-aggregate-cell")
		.AddClass($"moka-table-cell--{MokaEnumHelpers.ToCssValue(col.Align)}")
		.Build();

	private string DataCellClass(MokaColumn<TItem> col, int colIdx, bool editing = false) => ColumnCellClasses(col, colIdx)
		.AddClass($"moka-table-cell--{MokaEnumHelpers.ToCssValue(col.Align)}")
		.AddClass("moka-table-cell--editable", col.Editable)
		.AddClass("moka-table-cell--editing", editing)
		.AddClass("moka-table-cell--mono", col.Mono)
		.AddClass(PhoneSlotClass(col))
		.AddClass("moka-table-cell--phone-only", colIdx < 0)
		.AddClass(col.KindCellClass)
		.AddClass(col.CellClass)
		.Build();

	// An automatic table grows a column to fit a value that can't wrap. A cell's max-width at its width is what
	// makes the browser hold the column there and cut the value off instead.
	private string? DataCellStyle(MokaColumn<TItem> col, int colIdx)
	{
		string? width = GetColumnWidth(col) ?? col.Width;
		return PinStyle(col, colIdx)
			.AddStyle("width", width)
			.AddStyle("min-width", col.MinWidth)
			.AddStyle("max-width", width, col.NoWrap)
			.Build();
	}

	// The default cell's text, with what the search found marked. Elements, not a component per cell, and only while a
	// search is in force on a table that asks for it.
	private RenderFragment CellText(string text) => builder =>
	{
		string? term = HighlightSearch ? _searchTerm : null;
		int at = term is null ? -1 : text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
		if (at < 0)
		{
			builder.AddContent(0, text);
			return;
		}

		int from = 0;
		while (at >= 0)
		{
			builder.AddContent(1, text[from..at]);
			builder.OpenElement(2, "mark");
			builder.AddAttribute(3, "class", "moka-table-mark");
			builder.AddContent(4, text.Substring(at, term!.Length));
			builder.CloseElement();
			from = at + term.Length;
			at = text.IndexOf(term, from, StringComparison.OrdinalIgnoreCase);
		}

		builder.AddContent(5, text[from..]);
	};

	private string RowCssClass(MokaTableRow<TItem> row) => new CssBuilder()
		.AddClass("moka-table-row--selected", IsPicked(row.Item))
		.AddClass("moka-table-row--clickable", RowsHearClicks)
		.AddClass("moka-table-row--dragging", _draggingRow?.Index == row.Index)
		.AddClass("moka-table-row--phone", IsPhoneList)
		.AddClass(RowClass?.Invoke(row.Item))
		.Build();

	private string RowNumber(MokaTableRow<TItem> row) =>
		(_firstRowIndex + row.Index + 1).ToString(CultureInfo.CurrentCulture);

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_ = _loadCancellation?.CancelAsync();
		_loadCancellation = null;
		if (_streamCancellation is { } streaming)
		{
			_streamCancellation = null;
			await streaming.CancelAsync();
		}

		await _storageCancellation.CancelAsync();
		_storageCancellation.Dispose();

		_typingTimer?.Dispose();
		_typingTimer = null;
		_selection.Stop();
		CancelRemoteRun();

		if (_gridKeysInitialized || _resizeInitVersion >= 0 || _reorderDragBound || _tabStopWatchBound || _endlessArmedAt >= 0
		    || _rangesBound || _columnWatchBound)
		{
			await InvokeTableJsAsync("dispose", _wrapperRef);
		}

		_dotNetRef?.Dispose();
		_dotNetRef = null;

		if (_columnMenuShown)
		{
			_columnMenuShown = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _columnMenuKey);
		}

		if (_densityMenuShown)
		{
			_densityMenuShown = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _densityMenuKey);
		}

		if (_exportMenuShown)
		{
			_exportMenuShown = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _exportMenuKey);
		}

		if (_pinsMeasuredFor is not null)
		{
			_pinsMeasuredFor = null;
			await InvokeTableJsAsync("releasePins", _wrapperRef);
		}

		await base.DisposeAsyncCore();
	}

	/// <summary>The <c>@key</c> of a detail row: the key of the data row it belongs to.</summary>
	/// <param name="RowKey">The data row's key.</param>
	private sealed record DetailRowIdentity(object RowKey);
}
