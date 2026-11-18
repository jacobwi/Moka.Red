using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Interactions;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Table;

// What a row is on a phone, what it listens with, the bar over picked rows, and the states and foot around the rows.
public partial class MokaTable<TItem>
{
	private const int MaxBulkNames = 20;

	private readonly Dictionary<int, RowHandlers> _rowHandlers = [];

	private TItem _anchorItem = default!;
	private int _firstTabColumn;
	private bool _hasAnchor;

	// The clicked box after a shift-click: the browser already flipped it, and when the run leaves it as the markup
	// had it, Blazor finds nothing to update.
	private int? _resyncBoxRow;

	// Whether the next tick of a box is the end of a run, which only its click can tell.
	private bool _runPick;
	private EventCallback<MouseEventArgs> _noteRunClick;

	// Per column while any column names a phone slot, so null is the table as it always was.
	private Dictionary<MokaColumn<TItem>, MokaColumnPhone>? _phoneSlots;
	private int _phoneSlotsVersion = -1;
	private bool _phoneSlotsForList;

	private bool _keepRowsWhileLoading;

	/// <summary>
	///     A card of its own: surface, a 1 px outline and large corners. The toolbar, the view chips and the chooser sit
	///     above the card rather than in it. Default false, which keeps the table's plain border.
	/// </summary>
	[Parameter]
	public bool Framed { get; set; }

	/// <summary>
	///     For a table inside a card: drops the table's own frame and reaches past the card's padding, so the rows run to
	///     the card's edges and bottom corners. Default false.
	/// </summary>
	[Parameter]
	public bool Bleed { get; set; }

	/// <summary>
	///     Buttons for the picked rows, in a bar over the header while any row is picked. The bar says "3 selected" and,
	///     with <see cref="ItemName" />, their names. It receives the picked rows, the ones on screen first. It takes
	///     the place of <see cref="SelectionActions" /> when both are set, and leaves the toolbar where it is.
	/// </summary>
	[Parameter]
	public RenderFragment<IReadOnlyList<TItem>>? BulkActions { get; set; }

	/// <summary>
	///     What a row is called, for the names in the <see cref="BulkActions" /> bar and "Select Printer" on its box.
	/// </summary>
	[Parameter]
	public Func<TItem, string>? ItemName { get; set; }

	/// <summary>
	///     Draws skeleton rows in the real columns in place of the rows, while the app fetches them itself. Default
	///     false. <see cref="LoadingContent" /> takes their place when set. What the table shows during its own
	///     <see cref="ServerData" /> load is <see cref="SkeletonRows" />.
	/// </summary>
	[Parameter]
	public bool Loading { get; set; }

	/// <summary>How many skeleton rows <see cref="Loading" /> draws. Default 4.</summary>
	[Parameter]
	public int LoadingRows { get; set; } = 4;

	/// <summary>
	///     The icon of the empty state. With this, <see cref="EmptyTitle" /> or <see cref="EmptyText" /> an empty table
	///     shows a <c>MokaEmptyState</c>, which announces itself politely, in place of "No data available".
	/// </summary>
	[Parameter]
	public MokaIconDefinition? EmptyIcon { get; set; }

	/// <summary>The title of the empty state. See <see cref="EmptyIcon" />.</summary>
	[Parameter]
	public string? EmptyTitle { get; set; }

	/// <summary>The text under the title of the empty state. See <see cref="EmptyIcon" />.</summary>
	[Parameter]
	public string? EmptyText { get; set; }

	/// <summary>
	///     Your own content for an empty table, filling the row without padding. It wins over
	///     <see cref="EmptyContent" /> and the empty state.
	/// </summary>
	[Parameter]
	public RenderFragment? Empty { get; set; }

	/// <summary>
	///     What the rows are, in the plural ("devices"), for the words the table writes about them: "50 of 312 devices",
	///     "Show 50 more" and "Show all 312" under the load more pager, "Select all devices shown" on the header's box,
	///     "Act on the selected devices" naming the <see cref="BulkActions" /> bar, and "Loading devices…" while
	///     <see cref="Loading" />. Null keeps the words about rows.
	/// </summary>
	[Parameter]
	public string? ItemsName { get; set; }

	/// <summary>
	///     Where the <see cref="Expandable" /> toggle sits. <see cref="MokaTableTogglePosition.Start" />, the default, is a
	///     column before the others with a tooltip on each toggle. <see cref="MokaTableTogglePosition.End" /> is a narrow
	///     column after the others with no tooltip: the chevron turns when the row is open, and the button is named
	///     "Show more about" or "Hide more about" and the row's <see cref="ItemName" />. On the phone list it sits at the
	///     far right.
	/// </summary>
	[Parameter]
	public MokaTableTogglePosition ExpandTogglePosition { get; set; }

	/// <summary>Opening a row's detail closes the one that was open. Default false, where rows open on their own.</summary>
	[Parameter]
	public bool SingleExpand { get; set; }

	/// <summary>
	///     Draws a button over the <see cref="Expandable" /> toggles that opens every row shown, or closes them all when
	///     they are open. Ignored with <see cref="SingleExpand" />. Default false.
	/// </summary>
	[Parameter]
	public bool ShowExpandAll { get; set; }

	/// <summary>
	///     Raised when a row is double-clicked. A cell being edited keeps its double click; the row still hears it.
	/// </summary>
	[Parameter]
	public EventCallback<TItem> OnRowDoubleClick { get; set; }

	/// <summary>
	///     Rows held at the top of the table, above the others, whatever the sort, the filters and the page: a row to
	///     compare with, say. They draw the columns' cells and nothing to select, open or drag. Null, the default, holds none.
	/// </summary>
	[Parameter]
	public IReadOnlyList<TItem>? PinnedTop { get; set; }

	/// <summary>
	///     Rows held at the bottom of the table, under the others, whatever the sort, the filters and the page: a totals
	///     row, say. With a fixed header or a height they stay in view as the rows scroll. Null, the default, holds none.
	/// </summary>
	[Parameter]
	public IReadOnlyList<TItem>? PinnedBottom { get; set; }

	/// <summary>The table's name for screen readers ("Devices", "Latest knocks"), written on the <c>table</c> element.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>
	///     With <see cref="Framed" />, draws the toolbar, the view chips and the column chooser inside the card, as its first
	///     rows with a line under them, rather than above it. Default false.
	/// </summary>
	[Parameter]
	public bool ToolbarInFrame { get; set; }

	/// <summary>
	///     A row under the rows and the load more foot, inside the frame: any markup, laid out as a flex row
	///     with the room between its children, in small muted text over a line ("All 42 devices" at the left, a hint at the
	///     right). <see cref="FooterContent" /> is different: table rows in a <c>tfoot</c>.
	/// </summary>
	[Parameter]
	public RenderFragment? FootContent { get; set; }

	/// <summary>
	///     The narrowest the table draws, as a CSS length ("960px"). In a narrower space the rows scroll sideways inside
	///     the table, which then scrolls on its own whatever <see cref="Responsive" /> says. With
	///     <see cref="Responsive" /> off and neither this nor <see cref="Height" /> set, nothing around the rows scrolls,
	///     so a sticky header (the new design's, or <see cref="FixedHeader" />) sticks to the page, under a fixed app bar.
	/// </summary>
	[Parameter]
	public string? MinWidth { get; set; }

	/// <summary>
	///     Draws the <see cref="BulkActions" /> bar's Clear as an icon button, a cross named "Clear the selection", in place
	///     of the word. Default false.
	/// </summary>
	[Parameter]
	public bool BulkClearIcon { get; set; }

	// ── Phone ──

	private bool IsPhoneList => _phoneSlots is not null;

	// Roles only once rows can stop being table rows: a table drawn with display: grid loses what it means to a
	// screen reader, and an explicit role brings it back. A table with no slots stays as it always was.
	private string? AriaRole(string role) => IsPhoneList ? role : null;

	private Dictionary<MokaColumn<TItem>, MokaColumnPhone>? PhoneSlots()
	{
		bool list = IsListLook;
		if (_phoneSlotsVersion == _layoutVersion && _phoneSlotsForList == list)
		{
			return _phoneSlots;
		}

		_phoneSlotsVersion = _layoutVersion;
		_phoneSlotsForList = list;
		bool named = _columns.Exists(static col => col.Phone is not null);
		if (!named && !list)
		{
			return null;
		}

		// The list look draws every row as a list. With no slot named, the first column is the first line and the
		// others share the second.
		MokaColumnPhone unnamed = named ? MokaColumnPhone.Hidden : MokaColumnPhone.Secondary;
		bool noPrimary = !_columns.Exists(static col => col.Phone == MokaColumnPhone.Primary);
		Dictionary<MokaColumn<TItem>, MokaColumnPhone> slots = new(_columns.Count);
		foreach (MokaColumn<TItem> col in _columns)
		{
			slots[col] = col.Phone
			             ?? (noPrimary && ReferenceEquals(col, _columns[0]) ? MokaColumnPhone.Primary : unnamed);
		}

		return slots;
	}

	// The cells a row writes. A phone lays a row out by its slots whatever the columns above 720 px show, so a column
	// someone hid still draws a cell (position -1) when it has a slot. CSS hides that cell above 720 px, and nothing
	// counts it: it has no position in the grid's keys.
	private List<(MokaColumn<TItem> Col, int ColIdx)> RowColumns(List<(MokaColumn<TItem> Col, int ColIdx)> visible)
	{
		if (_phoneSlots is not { } slots)
		{
			return visible;
		}

		List<(MokaColumn<TItem> Col, int ColIdx)> columns = new(visible);
		foreach (MokaColumn<TItem> col in _columns)
		{
			if (!IsColumnVisible(col) && slots.TryGetValue(col, out MokaColumnPhone slot) && slot != MokaColumnPhone.Hidden)
			{
				columns.Add((col, -1));
			}
		}

		return columns;
	}

	// The cell Tab reaches first has to be one a phone shows, or the table can't be reached from the keyboard there.
	private int FirstTabColumn(List<(MokaColumn<TItem> Col, int ColIdx)> visible)
	{
		if (_phoneSlots is not { } slots)
		{
			return 0;
		}

		foreach ((MokaColumn<TItem> col, int position) in visible)
		{
			if (slots.GetValueOrDefault(col, MokaColumnPhone.Hidden) == MokaColumnPhone.Primary)
			{
				return position;
			}
		}

		return 0;
	}

	private string? PhoneSlotClass(MokaColumn<TItem> col)
	{
		if (_phoneSlots is null || !_phoneSlots.TryGetValue(col, out MokaColumnPhone slot))
		{
			return null;
		}

		return slot switch
		{
			MokaColumnPhone.Primary => "moka-table-cell--phone-primary",
			MokaColumnPhone.Secondary => "moka-table-cell--phone-secondary",
			MokaColumnPhone.Trailing => "moka-table-cell--phone-trailing",
			_ => "moka-table-cell--phone-hidden"
		};
	}

	private string? TableStyle
	{
		get
		{
			if (_phoneSlots is null && MinWidth is null)
			{
				return null;
			}

			// repeat() takes no zero, and an empty track takes no room.
			int? secondary = _phoneSlots is null
				? null
				: Math.Max(1, _phoneSlots.Values.Count(static slot => slot == MokaColumnPhone.Secondary));
			return new StyleBuilder()
				.AddStyle("--_phone-secondary", secondary?.ToString(CultureInfo.InvariantCulture))
				.AddStyle("min-width", MinWidth)
				.Build();
		}
	}

	private string SkeletonRowClass => new CssBuilder("moka-table-skeleton-row")
		.AddClass("moka-table-row--phone", IsPhoneList)
		.Build();

	private string PhoneOnlyHeaderClass(MokaColumn<TItem> col) => new CssBuilder("moka-table-cell--phone-only")
		.AddClass(PhoneSlotClass(col))
		.AddClass(col.Class)
		.Build();

	// ── Rows ──

	// One set of handlers per position, kept from render to render. A lambda written in the row template is a new
	// delegate on every render, which makes Blazor retire the handler and send a new one for every row, even when
	// nothing changed. These stay, so a table that ticks only pays for the cells that moved. They read the row at
	// their position when they run, so a new list needs no new ones.
	private RowHandlers HandlersFor(int index)
	{
		if (!_rowHandlers.TryGetValue(index, out RowHandlers? handlers))
		{
			handlers = new RowHandlers(this, index);
			_rowHandlers[index] = handlers;
		}

		return handlers;
	}

	// A row by the index it carries in the page: its position among the drawn rows, or under remote virtualization its
	// position in the whole result set, of which the table holds the window last loaded.
	private bool TryGetRow(int index, out TItem item)
	{
		int position = index - _rowIndexBase;
		if (position >= 0 && position < _displayRows.Count && _displayRows[position].Index == index)
		{
			item = _displayRows[position].Item;
			return true;
		}

		// Grouped windows skip the rows of folded groups, so the window's rows needn't follow each other.
		if (_lineOfRow is not null)
		{
			int found = _displayRows.BinarySearch(new MokaTableRow<TItem>(default!, index, null), RowPlaceOrder.Instance);
			if (found >= 0)
			{
				item = _displayRows[found].Item;
				return true;
			}
		}

		item = default!;
		return false;
	}

	private bool RowsHearClicks => OnRowClick.HasDelegate || (SelectOnRowClick && Selectable);

	private EventCallback<MouseEventArgs> RowClickHandler(int index) =>
		RowsHearClicks ? HandlersFor(index).Click : default;

	private EventCallback<MouseEventArgs> RowDoubleClickHandler(int index) =>
		OnRowDoubleClick.HasDelegate ? HandlersFor(index).DoubleClick : default;

	private static string PinnedRowRef(bool top, int index) =>
		string.Create(CultureInfo.InvariantCulture, $"{(top ? 't' : 'b')}{index}");

	private EventCallback<MouseEventArgs> RowContextMenuHandler(int index) =>
		OnRowContextMenu.HasDelegate ? HandlersFor(index).ContextMenu : default;

	private EventCallback<DragEventArgs> RowDropHandler(int index) =>
		RowReorderable ? HandlersFor(index).Drop : default;

	private EventCallback<MouseEventArgs> CellDoubleClickHandler(int index, MokaColumn<TItem> col) =>
		col.Editable ? HandlersFor(index).EditCell(col) : default;

	// The change event can't say whether Shift was held, so the box's click, which comes first, notes it.
	private EventCallback<MouseEventArgs> RowBoxClickHandler
	{
		get
		{
			if (SingleSelect)
			{
				return default;
			}

			if (!_noteRunClick.HasDelegate)
			{
				_noteRunClick = NonRendering.Callback<MouseEventArgs>(e => _runPick = e.ShiftKey);
			}

			return _noteRunClick;
		}
	}

	private string? RowBoxLabel(TItem item) => ItemName is null ? null : Words.SelectItem(ItemName(item));

	// ── Detail toggle ──

	private bool ExpandAtStart => Expandable && ExpandTogglePosition == MokaTableTogglePosition.Start;

	private bool ExpandAtEnd => Expandable && ExpandTogglePosition == MokaTableTogglePosition.End;

	private string ExpandLabel(TItem item)
	{
		string? name = ItemName?.Invoke(item);
		return IsExpanded(item) ? Words.HideMoreAbout(name) : Words.ShowMoreAbout(name);
	}

	// ── Words about the rows ──

	private string RowsNoun => ItemsName ?? Words.Rows;

	private static string PinnedRowClass(bool top) => new CssBuilder("moka-table-row--pinned")
		.AddClass(top ? "moka-table-row--pinned-top" : "moka-table-row--pinned-bottom")
		.Build();

	private string LoadingStatusText => Words.LoadingRows(ItemsName);

	private string BulkBarLabel => Words.BulkBar(ItemsName);

	// ── Picking a run ──

	private int RowIndexOf(TItem item) =>
		_displayRows.FindIndex(row => EqualityComparer<TItem>.Default.Equals(row.Item, item));

	// A list that ticks hands over new items for the same things, and a record with a new rate isn't equal to the old
	// one. Under an ItemKey the new item takes the old one's place in the selection, so the box stays ticked.
	private void CarrySelection(IReadOnlyList<TItem> items)
	{
		if (_except.Count > 0)
		{
			foreach (TItem item in items)
			{
				object key = PickKey(item);
				if (_except.ContainsKey(key))
				{
					_except[key] = item;
				}
			}
		}

		if (ItemKey is null || _selectedItems.Count == 0)
		{
			return;
		}

		Dictionary<object, TItem>? pickedByKey = null;
		foreach (TItem item in items)
		{
			if (_selectedItems.Contains(item))
			{
				continue;
			}

			if (pickedByKey is null)
			{
				pickedByKey = new Dictionary<object, TItem>(_selectedItems.Count);
				foreach (TItem picked in _selectedItems)
				{
					pickedByKey.TryAdd(ItemKey(picked), picked);
				}
			}

			if (pickedByKey.Remove(ItemKey(item), out TItem? old))
			{
				_selectedItems.Remove(old);
				_selectedItems.Add(item);
				if (_hasAnchor && EqualityComparer<TItem>.Default.Equals(_anchorItem, old))
				{
					_anchorItem = item;
				}
			}
		}
	}

	// ── Bulk bar ──

	private bool ShowBulkBar => BulkActions is not null && Selectable && AnyPicked;

	private string BulkCountText => PickedCountText;

	private string? BulkStatusText => AnyPicked && Selectable ? BulkCountText : null;

	private List<TItem> PickedInDisplayOrder()
	{
		List<TItem> picked = _displayItems.Where(_selectedItems.Contains).Distinct().ToList();
		if (picked.Count < _selectedItems.Count)
		{
			HashSet<TItem> shown = [.. picked];
			picked.AddRange(_selectedItems.Where(item => !shown.Contains(item)));
		}

		return picked;
	}

	private string? BulkNames(List<TItem> picked)
	{
		if (ItemName is null)
		{
			return null;
		}

		string names = string.Join(", ", picked.Take(MaxBulkNames).Select(ItemName));
		return picked.Count > MaxBulkNames ? Words.BulkNamesAndMore(names, picked.Count - MaxBulkNames) : names;
	}

	// ── Frame and page ──

	private bool ToolbarInsideFrame => Framed && ToolbarInFrame;

	// Nothing around the rows scrolls, so a sticky header sticks to the page and has to clear a fixed app bar.
	private bool HeaderSticksToPage => !Responsive && MinWidth is null && Height is null;

	// ── Loading and empty ──

	private bool HasEmptyState => EmptyIcon is not null || EmptyTitle is not null || EmptyText is not null;

	private string EmptyCellClass => new CssBuilder("moka-table-state-cell")
		.AddClass("moka-table-state-cell--plain", Empty is not null || (EmptyContent is null && HasEmptyState))
		.Build();

	private static string LoadingCellWidth(int rowIndex) => rowIndex % 2 == 0 ? "72%" : "54%";

	/// <summary>The handlers of the row at one position.</summary>
	private sealed class RowPlaceOrder : IComparer<MokaTableRow<TItem>>
	{
		public static readonly RowPlaceOrder Instance = new();

		public int Compare(MokaTableRow<TItem> x, MokaTableRow<TItem> y) => x.Index.CompareTo(y.Index);
	}

	private sealed class RowHandlers
	{
		private readonly MokaTable<TItem> _table;
		private readonly int _index;
		private Dictionary<MokaColumn<TItem>, EventCallback<MouseEventArgs>>? _editCell;

		public RowHandlers(MokaTable<TItem> table, int index)
		{
			_table = table;
			_index = index;
			Click = EventCallback.Factory.Create<MouseEventArgs>(table, RaiseClickAsync);
			DoubleClick = EventCallback.Factory.Create<MouseEventArgs>(table, RaiseDoubleClickAsync);
			ContextMenu = EventCallback.Factory.Create<MouseEventArgs>(table, RaiseContextMenuAsync);
			Select = EventCallback.Factory.Create<ChangeEventArgs>(table, SelectAsync);
			Expand = EventCallback.Factory.Create<MouseEventArgs>(table, ToggleExpand);
			DragStart = EventCallback.Factory.Create<DragEventArgs>(table, StartDrag);
			Drop = EventCallback.Factory.Create<DragEventArgs>(table, DropAsync);
		}

		public EventCallback<MouseEventArgs> Click { get; }

		public EventCallback<MouseEventArgs> DoubleClick { get; }

		public EventCallback<MouseEventArgs> ContextMenu { get; }

		public EventCallback<ChangeEventArgs> Select { get; }

		public EventCallback<MouseEventArgs> Expand { get; }

		public EventCallback<DragEventArgs> DragStart { get; }

		public EventCallback<DragEventArgs> Drop { get; }

		public EventCallback<MouseEventArgs> EditCell(MokaColumn<TItem> col)
		{
			_editCell ??= [];
			if (!_editCell.TryGetValue(col, out EventCallback<MouseEventArgs> handler))
			{
				handler = EventCallback.Factory.Create<MouseEventArgs>(_table, () => StartEditAsync(col));
				_editCell[col] = handler;
			}

			return handler;
		}

		private async Task RaiseClickAsync(MouseEventArgs e)
		{
			if (!_table.TryGetRow(_index, out TItem item))
			{
				return;
			}

			if (_table.SelectOnRowClick && _table.Selectable)
			{
				await _table.SelectFromRowClickAsync(item, _index, e);
			}

			await _table.OnRowClick.InvokeAsync(item);
		}

		private Task RaiseDoubleClickAsync() =>
			_table.TryGetRow(_index, out TItem item) ? _table.OnRowDoubleClick.InvokeAsync(item) : Task.CompletedTask;

		private Task RaiseContextMenuAsync(MouseEventArgs e) =>
			_table.TryGetRow(_index, out TItem item)
				? _table.OnRowContextMenu.InvokeAsync(new MokaItemContextMenuArgs<TItem>(item, e))
				: Task.CompletedTask;

		private Task SelectAsync() =>
			_table.TryGetRow(_index, out TItem item) ? _table.HandleRowSelectAsync(item, _index) : Task.CompletedTask;

		private void ToggleExpand()
		{
			if (_table.TryGetRow(_index, out TItem item))
			{
				_table.ToggleExpand(item);
			}
		}

		private void StartDrag()
		{
			if (_index < _table._displayRows.Count)
			{
				_table.HandleRowDragStart(_table._displayRows[_index]);
			}
		}

		private Task DropAsync() => _table.HandleRowDrop(_index);

		private Task StartEditAsync(MokaColumn<TItem> col) =>
			_table.TryGetRow(_index, out TItem item) ? _table.BeginEditAsync(item, col) : Task.CompletedTask;
	}
}
