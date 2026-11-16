using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Table;

// The columns as they stand: pinned to an edge, under a heading group, and what their headings' menus and filter buttons
// do. Each column's handlers are made once, so a render that changes nothing retires none.
public partial class MokaTable<TItem>
{
	private const string ColumnMenuKey = "column-menu";
	private const string FilterPopupKey = "filter-popup";

	private readonly Dictionary<MokaColumn<TItem>, ColumnHandlers> _columnHandlers = [];
	private int _nextActionKey;

	// The heading whose column menu is open, and the column whose filter popup is open with what it opened from.
	private MokaColumn<TItem>? _menuColumn;
	private MokaColumn<TItem>? _filterColumn;
	private string? _filterAnchor;

	// What the pinned offsets were last measured for, so the script measures once per layout change.
	private string? _pinsMeasuredFor;

	// The edges of the pinned columns in the render under way: the last pinned to the start and the first to the end.
	private int _lastStartPin = -1;
	private int _firstEndPin = -1;

	/// <summary>
	///     Draws a menu button in each heading, opening a menu to sort, filter, pin to either edge, hide, group by and fit
	///     the column to its content, from the mouse or the keyboard. Default false.
	/// </summary>
	[Parameter]
	public bool ColumnMenu { get; set; }

	// ── Handlers ──

	private ColumnHandlers HandlersFor(MokaColumn<TItem> col)
	{
		if (!_columnHandlers.TryGetValue(col, out ColumnHandlers? handlers))
		{
			handlers = new ColumnHandlers(this, col);
			_columnHandlers[col] = handlers;
		}

		return handlers;
	}

	// ── Pins ──

	internal MokaColumnPin PinOf(MokaColumn<TItem> col) => _columnPins.TryGetValue(col, out MokaColumnPin pin) ? pin : col.Pin;

	private bool HasPins => _columns.Exists(col => PinOf(col) != MokaColumnPin.None && IsColumnVisible(col));

	// The visible columns with their positions, which the cells carry as data-col-index: those pinned to the start first,
	// then the others, then those pinned to the end, each in the order they stand.
	private List<(MokaColumn<TItem> Col, int ColIdx)> VisibleColumnsInOrder()
	{
		List<(MokaColumn<TItem> Col, int ColIdx)> columns = new(_columns.Count);
		AddPinned(MokaColumnPin.Start);
		AddPinned(MokaColumnPin.None);
		AddPinned(MokaColumnPin.End);
		return columns;

		void AddPinned(MokaColumnPin pin)
		{
			foreach (MokaColumn<TItem> col in _columns)
			{
				if (IsColumnVisible(col) && PinOf(col) == pin)
				{
					columns.Add((col, columns.Count));
				}
			}
		}
	}

	private void FindPinEdges(List<(MokaColumn<TItem> Col, int ColIdx)> columns)
	{
		_lastStartPin = -1;
		_firstEndPin = -1;
		foreach ((MokaColumn<TItem> col, int position) in columns)
		{
			MokaColumnPin pin = PinOf(col);
			if (pin == MokaColumnPin.Start)
			{
				_lastStartPin = position;
			}
			else if (pin == MokaColumnPin.End && _firstEndPin < 0)
			{
				_firstEndPin = position;
			}
		}
	}

	private string? PinStyleFor(MokaColumn<TItem> col, int position) => position < 0
		? null
		: PinOf(col) switch
		{
			MokaColumnPin.Start => string.Create(CultureInfo.InvariantCulture, $"var(--_pin-{position}, 0px)"),
			MokaColumnPin.End => string.Create(CultureInfo.InvariantCulture, $"var(--_pin-{position}, 0px)"),
			_ => null
		};

	private string? PinSide(MokaColumn<TItem> col, int position) => position < 0
		? null
		: PinOf(col) switch
		{
			MokaColumnPin.Start => "start",
			MokaColumnPin.End => "end",
			_ => null
		};

	// The classes every cell of a column carries, in every row the table draws (heading, filter, skeleton, data, totals),
	// or those rows stop lining up.
	private CssBuilder ColumnCellClasses(MokaColumn<TItem> col, int position, string? rootClass = null)
	{
		MokaColumnPin pin = position < 0 ? MokaColumnPin.None : PinOf(col);
		return new CssBuilder(rootClass)
			.AddClass("moka-table-cell--pin-start", pin == MokaColumnPin.Start)
			.AddClass("moka-table-cell--pin-end", pin == MokaColumnPin.End)
			.AddClass("moka-table-cell--pin-edge", position >= 0 && (position == _lastStartPin || position == _firstEndPin))
			.AddClass("moka-table-cell--hide-mobile", col.HideOnMobile)
			.AddClass("moka-table-cell--nowrap", col.NoWrap)
			.AddClass(col.Class);
	}

	private StyleBuilder PinStyle(MokaColumn<TItem> col, int position)
	{
		string? offset = PinStyleFor(col, position);
		return new StyleBuilder()
			.AddStyle("inset-inline-start", offset, PinOf(col) == MokaColumnPin.Start)
			.AddStyle("inset-inline-end", offset, PinOf(col) == MokaColumnPin.End);
	}

	// Measured in the browser once the layout changes: the script writes each pinned column's offset as a variable on the
	// table and keeps it right as widths change, and marks the scroll edges for the shadows. A table with no pin pays nothing.
	private async Task SyncPinsAsync()
	{
		string? key = HasPins
			? string.Create(CultureInfo.InvariantCulture,
				$"{_layoutVersion}|{TableStyle}|{Framed}|{ToolbarInsideFrame}|{DrawHeader}|{_displayRows.Count}|{_isLoading}")
			: null;
		if (key == _pinsMeasuredFor)
		{
			return;
		}

		_pinsMeasuredFor = key;
		await InvokeTableJsAsync(key is null ? "releasePins" : "measurePins", _wrapperRef);
	}

	internal async Task PinColumnAsync(MokaColumn<TItem> col, MokaColumnPin pin)
	{
		if (pin == col.Pin)
		{
			_columnPins.Remove(col);
		}
		else
		{
			_columnPins[col] = pin;
		}

		_layoutVersion++;
		ClampFocusedCell();
		await NoteArrangementChangedAsync();
	}

	// ── Heading groups ──

	/// <summary>A run of shown columns under one heading group, or under none.</summary>
	private readonly record struct HeaderGroupRun(MokaColumnGroup? Group, int First, int Last, MokaColumnPin Pin)
	{
		public int Span => Last - First + 1;
	}

	// Null while no column stands under a group, so a table with none draws no extra row.
	private List<HeaderGroupRun>? HeaderGroupRuns(List<(MokaColumn<TItem> Col, int ColIdx)> columns)
	{
		if (!columns.Exists(static entry => entry.Col.Group is not null))
		{
			return null;
		}

		List<HeaderGroupRun> runs = [];
		foreach ((MokaColumn<TItem> col, int position) in columns)
		{
			MokaColumnPin pin = PinOf(col);
			if (runs.Count > 0 && runs[^1] is { } last && ReferenceEquals(last.Group, col.Group) && last.Pin == pin)
			{
				runs[^1] = last with { Last = position };
			}
			else
			{
				runs.Add(new HeaderGroupRun(col.Group, position, position, pin));
			}
		}

		return runs;
	}

	private int LeadingControlCells => (ShowRowNumbers ? 1 : 0) + (ExpandAtStart ? 1 : 0) + (Selectable ? 1 : 0) + (RowReorderable ? 1 : 0);

	private int TrailingControlCells => (RowActions is not null ? 1 : 0) + (ExpandAtEnd ? 1 : 0);

	private static string GroupRunClass(HeaderGroupRun run) => new CssBuilder("moka-table-group-heading")
		.AddClass("moka-table-group-heading--none", run.Group is null)
		.AddClass("moka-table-cell--pin-start", run.Pin == MokaColumnPin.Start)
		.AddClass("moka-table-cell--pin-end", run.Pin == MokaColumnPin.End)
		.AddClass(run.Group?.Class)
		.Build();

	// A run pinned to the start sits at its first column's offset, one pinned to the end at its last column's.
	private static string? GroupRunStyle(HeaderGroupRun run) => run.Pin switch
	{
		MokaColumnPin.Start => new StyleBuilder()
			.AddStyle("inset-inline-start", string.Create(CultureInfo.InvariantCulture, $"var(--_pin-{run.First}, 0px)"))
			.Build(),
		MokaColumnPin.End => new StyleBuilder()
			.AddStyle("inset-inline-end", string.Create(CultureInfo.InvariantCulture, $"var(--_pin-{run.Last}, 0px)"))
			.Build(),
		_ => null
	};

	internal void OnGroupChanged()
	{
		_layoutVersion++;
		_parts.Render();
	}

	void IMokaTablePartHost.OnGroupChanged() => OnGroupChanged();

	// The columns and heading groups in the order they stand in the markup, as QuickGrid collects its columns, so a part
	// that comes back takes its place again. A group keeps the order of its own columns the same way, and they take the
	// place it holds here.
	private readonly MokaChildList<object> _parts;

	void IMokaTablePartHost.AddGroup(MokaColumnGroup group) => _parts.Add(group);

	void IMokaTablePartHost.RemoveGroup(MokaColumnGroup group) => _parts.Remove(group);

	void IMokaTablePartHost.SawGroup(MokaColumnGroup group) => _parts.Saw(group);

	void IMokaTablePartHost.OnGroupColumnsChanged() => SyncDeclaredOrder();

	private MokaColumnGroup? GroupHolding(MokaColumn<TItem> column) =>
		column.Group is { } group && _parts.Contains(group) ? group : null;

	// Where a column stands among the registered ones: after the columns before it in the markup, its group's earlier
	// columns included. Every column held before it has registered.
	private int RegistrationIndex(MokaColumn<TItem> column)
	{
		int index = 0;
		for (int i = 0; i < _parts.Count; i++)
		{
			if (_parts[i] is MokaColumnGroup group)
			{
				IReadOnlyList<object> members = group.Columns;
				for (int j = 0; j < members.Count; j++)
				{
					if (ReferenceEquals(members[j], column))
					{
						return index;
					}

					index++;
				}
			}
			else if (ReferenceEquals(_parts[i], column))
			{
				return index;
			}
			else
			{
				index++;
			}
		}

		return _registeredColumns.Count;
	}

	// The registered columns in the order the markup gives them, from the parts and their groups' columns.
	private List<MokaColumn<TItem>> DeclaredOrder()
	{
		List<MokaColumn<TItem>> order = new(_registeredColumns.Count);
		for (int i = 0; i < _parts.Count; i++)
		{
			if (_parts[i] is MokaColumnGroup group)
			{
				foreach (object member in group.Columns)
				{
					if (member is MokaColumn<TItem> col)
					{
						order.Add(col);
					}
				}
			}
			else if (_parts[i] is MokaColumn<TItem> col)
			{
				order.Add(col);
			}
		}

		return order;
	}

	// A column takes the place the markup gives it once the parts have settled, unless an arrangement in force places it.
	// Columns nobody arranged follow the markup; among arranged ones, a column whose neighbour in the markup changed goes
	// after the column now before it. While a column is still joining or leaving the counts differ, and that column's own
	// registration places it.
	private void SyncDeclaredOrder()
	{
		List<MokaColumn<TItem>> declared = DeclaredOrder();
		if (declared.Count != _registeredColumns.Count || declared.SequenceEqual(_registeredColumns))
		{
			return;
		}

		bool arranged = !_columns.SequenceEqual(_registeredColumns);
		List<MokaColumn<TItem>> previous = [.. _registeredColumns];
		_registeredColumns.Clear();
		_registeredColumns.AddRange(declared);
		if (!arranged)
		{
			_columns.Clear();
			_columns.AddRange(declared);
		}
		else
		{
			for (int i = 0; i < declared.Count; i++)
			{
				MokaColumn<TItem>? before = i > 0 ? declared[i - 1] : null;
				int was = previous.IndexOf(declared[i]);
				if (ReferenceEquals(before, was > 0 ? previous[was - 1] : null))
				{
					continue;
				}

				_columns.Remove(declared[i]);
				_columns.Insert(before is null ? 0 : _columns.IndexOf(before) + 1, declared[i]);
			}
		}

		if (_arrangementInForce is { Count: > 0 } arrangement)
		{
			ApplyOrder(arrangement);
		}

		_layoutVersion++;
		ClampFocusedCell();
		_parts.Render();
	}

	// A group that keeps its columns together takes no column from outside, and gives none away.
	private static bool KeepsApart(MokaColumn<TItem> source, MokaColumn<TItem> target) =>
		!ReferenceEquals(source.Group, target.Group)
		&& (source.Group?.KeepTogether == true || target.Group?.KeepTogether == true);

	// ── Column menu ──

	private static string MenuButtonSelector(MokaColumn<TItem> col) =>
		string.Create(CultureInfo.InvariantCulture, $"[data-moka-menu-for=\"{col.ActionKey}\"]");

	private static string FilterButtonSelector(MokaColumn<TItem> col) =>
		string.Create(CultureInfo.InvariantCulture, $"[data-moka-filter-for=\"{col.ActionKey}\"]");

	private static string RowFilterSelector(MokaColumn<TItem> col) =>
		string.Create(CultureInfo.InvariantCulture, $"[data-moka-filter-row=\"{col.ActionKey}\"]");

	private static string HeadingSelector(MokaColumn<TItem> col) =>
		string.Create(CultureInfo.InvariantCulture, $"[data-moka-head=\"{col.ActionKey}\"]");

	internal ElementReference WrapperRef => _wrapperRef;

	internal string PopupKey(string part) => string.Create(CultureInfo.InvariantCulture, $"{_columnMenuKey}-{part}");

	internal string ColumnMenuPopupKey => PopupKey(ColumnMenuKey);

	internal string FilterPopupPopupKey => PopupKey(FilterPopupKey);

	private void ToggleColumnMenu(MokaColumn<TItem> col)
	{
		_filterColumn = null;
		_menuColumn = ReferenceEquals(_menuColumn, col) ? null : col;
	}

	/// <summary>Closes the column menu, putting focus back on its heading's button when asked.</summary>
	internal void CloseColumnMenu(bool returnFocus)
	{
		if (_menuColumn is not { } col)
		{
			return;
		}

		_menuColumn = null;
		if (returnFocus)
		{
			_focusAfterRender = [MenuButtonSelector(col)];
		}

		StateHasChanged();
	}

	internal async Task SortFromMenuAsync(MokaColumn<TItem> col, MokaSortDirection direction)
	{
		if (!CanSort(col) || col.Title is not { } title)
		{
			return;
		}

		_menuColumn = null;
		_focusAfterRender = [MenuButtonSelector(col)];
		if (!MultiSort)
		{
			_sorts.Clear();
		}

		int index = _sorts.FindIndex(sort => sort.Column == title);
		if (direction == MokaSortDirection.None)
		{
			if (index >= 0)
			{
				_sorts.RemoveAt(index);
			}
		}
		else if (index >= 0)
		{
			_sorts[index] = _sorts[index] with { Direction = direction };
		}
		else
		{
			_sorts.Add(new MokaTableSort(title, direction));
		}

		_sortsFromDefault = false;
		ResetPaging();
		await LoadDataAsync();
		StateHasChanged();
		await NotifyStateChangedAsync();
	}

	internal async Task PinFromMenuAsync(MokaColumn<TItem> col, MokaColumnPin pin)
	{
		_menuColumn = null;
		_focusAfterRender = [MenuButtonSelector(col)];
		await PinColumnAsync(col, pin);
		StateHasChanged();
	}

	internal async Task HideFromMenuAsync(MokaColumn<TItem> col)
	{
		_menuColumn = null;
		_focusAfterRender = [".moka-table-menu-btn", ".moka-table-sort-button"];
		if (IsColumnVisible(col))
		{
			await ToggleColumnVisibilityAsync(col);
		}

		StateHasChanged();
	}

	internal async Task GroupFromMenuAsync(MokaColumn<TItem> col)
	{
		_menuColumn = null;
		_focusAfterRender = [MenuButtonSelector(col)];
		await ToggleGroupByAsync(col);
	}

	internal async Task FitFromMenuAsync(MokaColumn<TItem> col)
	{
		_menuColumn = null;
		_focusAfterRender = [MenuButtonSelector(col)];
		int position = VisibleColumnsInOrder().FindIndex(entry => ReferenceEquals(entry.Col, col));
		if (position >= 0 && await SafeModuleInvokeAsync<double>(ModulePath, "fitColumn", _wrapperRef, position, HeadingSelector(col)) is > 0 and var width)
		{
			_columnWidths[col] = Math.Ceiling(width);
			await NoteArrangementChangedAsync();
		}

		StateHasChanged();
	}

	internal void FilterFromMenu(MokaColumn<TItem> col)
	{
		_menuColumn = null;
		OpenFilterPopup(col, HasControl(MokaTableFilterControls.Menus) ? FilterButtonSelector(col) : HeadingSelector(col));
	}

	internal bool CanFilterFromMenu(MokaColumn<TItem> col) => col is { Filterable: true, Title: not null, HasValue: true } || (ServerData is not null && col is { Filterable: true, Title: not null });

	internal static bool CanGroupBy(MokaColumn<TItem> col) => col.Title is not null && (col.HasValue || col.GroupKey is not null);

	internal bool IsGroupedBy(MokaColumn<TItem> col) => col.Title is not null && _groups.Contains(col.Title);

	internal static bool CanHide(MokaColumn<TItem> col) => !col.Fixed && col.Title is not null;

	internal static bool CanPin(MokaColumn<TItem> col) => !col.Fixed;

	internal MokaSortDirection SortDirectionOf(MokaColumn<TItem> col) => SortOf(col.Title)?.Direction ?? MokaSortDirection.None;

	internal bool CanSortColumn(MokaColumn<TItem> col) => CanSort(col);

	// ── Filter popup ──

	internal MokaTableFilter? FilterOn(MokaColumn<TItem> col) => col.Title is null ? null : _filters.Find(filter => filter.Column == col.Title);

	private void OpenFilterPopup(MokaColumn<TItem> col, string anchor)
	{
		_menuColumn = null;
		if (ReferenceEquals(_filterColumn, col) && _filterAnchor == anchor)
		{
			_filterColumn = null;
			return;
		}

		_filterColumn = col;
		_filterAnchor = anchor;
	}

	internal string? FilterAnchor => _filterAnchor;

	/// <summary>Closes the filter popup, putting focus back on what opened it when asked.</summary>
	internal void CloseFilterPopup(bool returnFocus)
	{
		if (_filterColumn is null)
		{
			return;
		}

		_filterColumn = null;
		if (returnFocus && _filterAnchor is { } anchor)
		{
			_focusAfterRender = [anchor];
		}

		StateHasChanged();
	}

	internal async Task ApplyFilterFromPopupAsync(MokaColumn<TItem> col, MokaTableFilter? filter)
	{
		_filterColumn = null;
		if (_filterAnchor is { } anchor)
		{
			_focusAfterRender = [anchor];
		}

		await SetColumnFilterAsync(col, filter);
	}

	private string FilterButtonClass(MokaColumn<TItem> col) => new CssBuilder("moka-table-filter-btn")
		.AddClass("moka-table-filter-btn--on", FilterOn(col) is not null)
		.Build();

	private string? FilterButtonLabel(MokaColumn<TItem> col) =>
		FilterOn(col) is { } filter ? Words.FilterOnColumn(col.Title!, FilterSummary(filter)) : Words.FilterBy(col.Title!);

	private string RowChoicesSummary(MokaColumn<TItem> col) => FilterOn(col) is { } filter
		? FilterSummary(filter)
		: Words.All;

	// ── Actions in cells ──

	/// <summary>
	///     Called from JS when a control a column kind marked with <c>data-moka-act</c> is clicked or pressed. One listener
	///     on the table hears every row, so a cell needs no handler of its own.
	/// </summary>
	/// <param name="row">The row: its <c>data-row-index</c>, or <c>t</c> or <c>b</c> and a position for a pinned row.</param>
	/// <param name="action">The column's number and the action's name, as <c>MokaColumn.ActionAttribute</c> wrote them.</param>
	/// <param name="clientX">Where the click was across the window, 0 from the keyboard.</param>
	/// <param name="clientY">Where the click was down the window, 0 from the keyboard.</param>
	/// <param name="detail">The click count, 0 from the keyboard.</param>
	[JSInvokable]
	public void OnCellAction(string row, string action, double clientX, double clientY, long detail) => RunDetached(async () =>
	{
		int colon = action.IndexOf(':', StringComparison.Ordinal);
		if (colon <= 0 || !int.TryParse(action.AsSpan(0, colon), NumberStyles.None, CultureInfo.InvariantCulture, out int key))
		{
			return;
		}

		MokaColumn<TItem>? col = _columns.Find(c => c.ActionKey == key);
		if (col is null || !TryGetRowItem(row, out TItem item))
		{
			return;
		}

		await col.HandleCellActionAsync(item, action[(colon + 1)..],
			new MouseEventArgs { ClientX = clientX, ClientY = clientY, Detail = detail });
		StateHasChanged();
	});

	private bool TryGetRowItem(string row, out TItem item)
	{
		if (row == "n" && _edit is { IsNew: true } added)
		{
			item = added.Draft;
			return true;
		}

		if (row.Length > 1 && row[0] is 't' or 'b'
		                   && int.TryParse(row.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out int pinned))
		{
			IReadOnlyList<TItem>? rows = row[0] == 't' ? PinnedTop : PinnedBottom;
			if (rows is not null && pinned < rows.Count)
			{
				item = rows[pinned];
				return true;
			}
		}
		else if (int.TryParse(row, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
		{
			return TryGetRow(index, out item);
		}

		item = default!;
		return false;
	}

	/// <summary>The handlers of one column's heading and filter controls, made once.</summary>
	private sealed class ColumnHandlers
	{
		public ColumnHandlers(MokaTable<TItem> table, MokaColumn<TItem> col)
		{
			Sort = EventCallback.Factory.Create<MouseEventArgs>(table, e => table.HandleSort(col, e.ShiftKey));
			Menu = EventCallback.Factory.Create<MouseEventArgs>(table, () => table.ToggleColumnMenu(col));
			Filter = EventCallback.Factory.Create<MouseEventArgs>(table, () => table.OpenFilterPopup(col, FilterButtonSelector(col)));
			RowFilter = EventCallback.Factory.Create<MouseEventArgs>(table, () => table.OpenFilterPopup(col, RowFilterSelector(col)));
			DragStart = EventCallback.Factory.Create<DragEventArgs>(table, () => table._draggingColumn = col);
			Drop = EventCallback.Factory.Create<DragEventArgs>(table, () => table.HandleColumnDropAsync(col));
			RowText = NonRendering.Callback<ChangeEventArgs>(e => table.HandleRowText(col, e));
			RowRangeEnd = NonRendering.Callback<ChangeEventArgs>(e => table.HandleRowRangeEnd(col, e));
		}

		public EventCallback<MouseEventArgs> Sort { get; }

		public EventCallback<MouseEventArgs> Menu { get; }

		public EventCallback<MouseEventArgs> Filter { get; }

		public EventCallback<MouseEventArgs> RowFilter { get; }

		public EventCallback<DragEventArgs> DragStart { get; }

		public EventCallback<DragEventArgs> Drop { get; }

		public EventCallback<ChangeEventArgs> RowText { get; }

		public EventCallback<ChangeEventArgs> RowRangeEnd { get; }
	}
}
