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
	private bool _previousShowMore;

	// The clicked box after a shift-click: the browser already flipped it, and when the run leaves it as the markup
	// had it, Blazor finds nothing to update.
	private int? _resyncBoxRow;

	// Whether the next tick of a box is the end of a run, which only its click can tell.
	private bool _runPick;
	private EventCallback<MouseEventArgs> _noteRunClick;

	// How many rows ShowMore draws, once someone asked for more. int.MaxValue is every row.
	private int? _shownRows;

	// Per column while any column names a phone slot, so null is the table as it always was.
	private Dictionary<MokaColumn<TItem>, MokaColumnPhone>? _phoneSlots;
	private int _phoneSlotsVersion = -1;

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
	///     Draws the first <see cref="PageSize" /> rows in sort order, then "Show N more" and "Show all" in the foot, in
	///     place of numbered pages. It wins over <see cref="ShowPagination" />. Under <see cref="ServerData" /> the
	///     table asks for page 1, one page size larger each time. Default false.
	/// </summary>
	[Parameter]
	public bool ShowMore { get; set; }

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

	// ── Phone ──

	private bool IsPhoneList => _phoneSlots is not null;

	// Roles only once rows can stop being table rows: a table drawn with display: grid loses what it means to a
	// screen reader, and an explicit role brings it back. A table with no slots stays as it always was.
	private string? AriaRole(string role) => IsPhoneList ? role : null;

	private Dictionary<MokaColumn<TItem>, MokaColumnPhone>? PhoneSlots()
	{
		if (_phoneSlotsVersion == _layoutVersion)
		{
			return _phoneSlots;
		}

		_phoneSlotsVersion = _layoutVersion;
		if (!_columns.Exists(static col => col.Phone is not null))
		{
			return null;
		}

		bool noPrimary = !_columns.Exists(static col => col.Phone == MokaColumnPhone.Primary);
		Dictionary<MokaColumn<TItem>, MokaColumnPhone> slots = new(_columns.Count);
		foreach (MokaColumn<TItem> col in _columns)
		{
			slots[col] = col.Phone
			             ?? (noPrimary && ReferenceEquals(col, _columns[0]) ? MokaColumnPhone.Primary : MokaColumnPhone.Hidden);
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

		List<(MokaColumn<TItem> Col, int ColIdx)> columns = new(_columns.Count);
		int position = 0;
		foreach (MokaColumn<TItem> col in _columns)
		{
			if (IsColumnVisible(col))
			{
				columns.Add((col, position++));
			}
			else if (slots.TryGetValue(col, out MokaColumnPhone slot) && slot != MokaColumnPhone.Hidden)
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
			if (_phoneSlots is null)
			{
				return null;
			}

			// repeat() takes no zero, and an empty track takes no room.
			int secondary = Math.Max(1, _phoneSlots.Values.Count(static slot => slot == MokaColumnPhone.Secondary));
			return new StyleBuilder()
				.AddStyle("--_phone-secondary", secondary.ToString(CultureInfo.InvariantCulture))
				.Build();
		}
	}

	private string SkeletonRowClass => new CssBuilder("moka-table-skeleton-row")
		.AddClass("moka-table-row--phone", IsPhoneList)
		.Build();

	private string PhoneOnlyHeaderClass(MokaColumn<TItem> col) => new CssBuilder("moka-table-cell--phone-only")
		.AddClass(PhoneSlotClass(col))
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

	private bool TryGetRow(int index, out TItem item)
	{
		if (index >= 0 && index < _displayRows.Count)
		{
			item = _displayRows[index].Item;
			return true;
		}

		item = default!;
		return false;
	}

	private EventCallback<MouseEventArgs> RowClickHandler(int index) =>
		OnRowClick.HasDelegate ? HandlersFor(index).Click : default;

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

	private string? RowBoxLabel(TItem item) => ItemName is null ? null : $"Select {ItemName(item)}";

	// ── Picking a run ──

	private int RowIndexOf(TItem item) =>
		_displayRows.FindIndex(row => EqualityComparer<TItem>.Default.Equals(row.Item, item));

	// Every row from the last box touched to this one takes the state of the last box, the way file managers do.
	private bool PickRun(TItem item)
	{
		int to = RowIndexOf(item);
		int from = _hasAnchor ? RowIndexOf(_anchorItem) : -1;
		if (from < 0 || to < 0)
		{
			return false;
		}

		bool pick = _selectedItems.Contains(_anchorItem);
		for (int i = Math.Min(from, to); i <= Math.Max(from, to); i++)
		{
			if (pick)
			{
				_selectedItems.Add(_displayRows[i].Item);
			}
			else
			{
				_selectedItems.Remove(_displayRows[i].Item);
			}
		}

		_resyncBoxRow = to;
		return true;
	}

	// A list that ticks hands over new items for the same things, and a record with a new rate isn't equal to the old
	// one. Under an ItemKey the new item takes the old one's place in the selection, so the box stays ticked.
	private void CarrySelection(IReadOnlyList<TItem> items)
	{
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

	private bool ShowBulkBar => BulkActions is not null && Selectable && _selectedItems.Count > 0;

	private string BulkCountText => $"{_selectedItems.Count.ToString("N0", CultureInfo.CurrentCulture)} selected";

	private string? BulkStatusText => _selectedItems.Count > 0 && Selectable ? BulkCountText : null;

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
		return picked.Count > MaxBulkNames
			? $"{names} and {(picked.Count - MaxBulkNames).ToString("N0", CultureInfo.CurrentCulture)} more"
			: names;
	}

	// ── Show more ──

	private int ShownRowCount => _shownRows ?? Math.Max(_pageSize, 1);

	private bool ShowMoreFoot => ShowMore && !Loading && !_isLoading && _displayRows.Count < _totalItems;

	private string ShowMoreCountText => string.Create(CultureInfo.CurrentCulture, $"Showing {_displayRows.Count:N0} of {_totalItems:N0}");

	private string ShowMoreButtonText =>
		string.Create(CultureInfo.CurrentCulture, $"Show {Math.Min(Math.Max(_pageSize, 1), _totalItems - _displayRows.Count):N0} more");

	private async Task ShowMoreRowsAsync()
	{
		int firstNew = _displayRows.Count;
		_shownRows = firstNew + Math.Max(_pageSize, 1);
		await LoadDataAsync(keepRows: true);
		FocusFirstNewRow(firstNew);
	}

	private async Task ShowAllRowsAsync()
	{
		int firstNew = _displayRows.Count;
		_shownRows = int.MaxValue;
		await LoadDataAsync(keepRows: true);
		FocusFirstNewRow(firstNew);
	}

	// The buttons go away with the last rows, and focus would fall to the page.
	private void FocusFirstNewRow(int firstNew)
	{
		if (_displayRows.Count >= _totalItems && firstNew < _displayRows.Count)
		{
			_focusedCell = (firstNew, _firstTabColumn);
			_cellFocusPending = true;
		}
	}

	// ── Loading and empty ──

	private bool HasEmptyState => EmptyIcon is not null || EmptyTitle is not null || EmptyText is not null;

	private string EmptyCellClass => new CssBuilder("moka-table-state-cell")
		.AddClass("moka-table-state-cell--plain", Empty is not null || (EmptyContent is null && HasEmptyState))
		.Build();

	private static string LoadingCellWidth(int rowIndex) => rowIndex % 2 == 0 ? "72%" : "54%";

	/// <summary>The handlers of the row at one position.</summary>
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
			ContextMenu = EventCallback.Factory.Create<MouseEventArgs>(table, RaiseContextMenuAsync);
			Select = EventCallback.Factory.Create<ChangeEventArgs>(table, SelectAsync);
			Expand = EventCallback.Factory.Create<MouseEventArgs>(table, ToggleExpand);
			DragStart = EventCallback.Factory.Create<DragEventArgs>(table, StartDrag);
			Drop = EventCallback.Factory.Create<DragEventArgs>(table, DropAsync);
		}

		public EventCallback<MouseEventArgs> Click { get; }

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
				handler = EventCallback.Factory.Create<MouseEventArgs>(_table, () => StartEdit(col));
				_editCell[col] = handler;
			}

			return handler;
		}

		private Task RaiseClickAsync() =>
			_table.TryGetRow(_index, out TItem item) ? _table.OnRowClick.InvokeAsync(item) : Task.CompletedTask;

		private Task RaiseContextMenuAsync(MouseEventArgs e) =>
			_table.TryGetRow(_index, out TItem item)
				? _table.OnRowContextMenu.InvokeAsync(new MokaItemContextMenuArgs<TItem>(item, e))
				: Task.CompletedTask;

		private Task SelectAsync() =>
			_table.TryGetRow(_index, out TItem item) ? _table.HandleRowSelect(item) : Task.CompletedTask;

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

		private void StartEdit(MokaColumn<TItem> col)
		{
			if (_table.TryGetRow(_index, out TItem item))
			{
				_table.StartEdit(item, col);
			}
		}
	}
}
