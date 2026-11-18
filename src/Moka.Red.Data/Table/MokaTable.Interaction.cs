using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Moka.Red.Data.Table;

// The grid's keys and tab stop, dragging columns and rows, and resizing headings. Moving between cells stays in
// moka-table.js; .NET hears only what acts on a row.
public partial class MokaTable<TItem>
{

	// ── Keyboard navigation ──

	private string CellTabIndex(int rowIndex, int colIndex)
	{
		bool isRoving = _focusedCell.Row < 0
			? rowIndex == 0 && colIndex == _firstTabColumn
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

		int rows = RemoteRows ? _totalItems : _displayRows.Count;
		int columns = VisibleColumnCount;
		_focusedCell = rows == 0 || columns == 0
			? (-1, -1)
			: (Math.Min(_focusedCell.Row, rows - 1), Math.Min(_focusedCell.Col, columns - 1));
	}

	/// <summary>
	///     Called from JS when a data cell takes focus. The script has already moved the tab stop, so this
	///     only remembers the cell for the next render, and renders nothing itself.
	/// </summary>
	/// <param name="rowIndex">The cell's <c>data-row-index</c>.</param>
	/// <param name="colIndex">The cell's <c>data-col-index</c>.</param>
	[JSInvokable]
	public void OnCellFocused(int rowIndex, int colIndex)
	{
		_focusedCell = (rowIndex, colIndex);
		_tabStopMoved = true;
	}

	/// <summary>
	///     Called from JS for a key that acts on a focused cell: Enter or F2 on an editable one, Space on a selectable row,
	///     Ctrl+Z for undo. Moving between cells stays in the script. The answer says whether the key opened an editor in
	///     that cell: until it is drawn and focused, the script holds what is typed on the cell and hands it to the editor.
	/// </summary>
	/// <param name="rowIndex">The cell's <c>data-row-index</c>.</param>
	/// <param name="colIndex">The cell's <c>data-col-index</c>.</param>
	/// <param name="key">The key, as <c>KeyboardEvent.key</c> names it, or <c>Undo</c>.</param>
	/// <returns>Whether an editor opened in the cell.</returns>
	[JSInvokable]
	public async Task<bool> OnCellKey(int rowIndex, int colIndex, string key)
	{
		try
		{
			return await HandleCellKeyAsync(rowIndex, colIndex, key);
		}
#pragma warning disable CA1031 // A failure goes to the error boundary, as one from an event handler would, not to the script.
		catch (Exception ex)
#pragma warning restore CA1031
		{
			await DispatchExceptionAsync(ex);
			return false;
		}
	}

	private async Task<bool> HandleCellKeyAsync(int rowIndex, int colIndex, string key)
	{
		List<(MokaColumn<TItem> Col, int ColIdx)> columns = VisibleColumnsInOrder();

		if (key == "Undo")
		{
			await UndoLastSaveAsync();
			StateHasChanged();
			return false;
		}

		// The rows can have changed while the key was on its way.
		if (_edit is { Saving: true } || !TryGetRow(rowIndex, out TItem item) || colIndex < 0 || colIndex >= columns.Count)
		{
			return false;
		}

		bool editing = await HandleCellActionKey(key, item, rowIndex, columns[colIndex].Col);
		StateHasChanged();
		return editing;
	}

	private async Task<bool> HandleCellActionKey(string key, TItem item, int rowIndex, MokaColumn<TItem> col)
	{
		switch (key)
		{
			case "Enter" or "F2" when col.Editable:
				return await BeginEditAsync(item, col);
			case " " when Selectable:
				await HandleRowSelectAsync(item, rowIndex);
				return false;
			case "Shift+ " when Selectable:
				_runPick = true;
				await HandleRowSelectAsync(item, rowIndex);
				return false;
			default:
				return false;
		}
	}

	// Enter on a focused cell raises OnRowClick, through moka-table.js, which clicks the cell so
	// the keyboard takes the mouse's path. An editable cell keeps Enter for starting the edit.
	private bool ActivatesRow(MokaColumn<TItem> col) => RowsHearClicks && !col.Editable;

	// ── Column reorder ──

	// The drag handlers are attached only while columns can be reordered. The highlight of the
	// header under the pointer lives in moka-table.js, because dragover fires many times a second.
	private EventCallback<DragEventArgs> ColumnDragStartHandler(MokaColumn<TItem> column) =>
		ColumnReorderable && !column.Fixed ? HandlersFor(column).DragStart : default;

	private EventCallback<DragEventArgs> ColumnDropHandler(MokaColumn<TItem> target) =>
		ColumnReorderable && !target.Fixed ? HandlersFor(target).Drop : default;

	private EventCallback<DragEventArgs> _endColumnDrag;

	// A drag cancelled with Escape or dropped outside the headers ends without a drop.
	private EventCallback<DragEventArgs> ColumnDragEndHandler
	{
		get
		{
			if (!ColumnReorderable)
			{
				return default;
			}

			if (!_endColumnDrag.HasDelegate)
			{
				_endColumnDrag = EventCallback.Factory.Create<DragEventArgs>(this, () => _draggingColumn = null);
			}

			return _endColumnDrag;
		}
	}

	// Works on the columns themselves, not on their positions: positions change with every move
	// and skip hidden columns, so an index taken from the screen addresses the wrong column.
	private async Task HandleColumnDropAsync(MokaColumn<TItem> target)
	{
		MokaColumn<TItem>? source = _draggingColumn;
		_draggingColumn = null;
		if (!ColumnReorderable || source is null)
		{
			return;
		}

		await MoveColumnAsync(source, target);
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

	// ── Column resize ──

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
		RunDetached(NoteArrangementChangedAsync);
	}

	/// <summary>Called from JS when Home or a double click on a heading's edge gives the column back its own width.</summary>
	/// <param name="colIndex">Index of the column among the visible columns.</param>
	[JSInvokable]
	public void OnColumnWidthReset(int colIndex)
	{
		List<(MokaColumn<TItem> Col, int ColIdx)> visible = VisibleColumnsInOrder();
		if (colIndex < 0 || colIndex >= visible.Count || !_columnWidths.Remove(visible[colIndex].Col))
		{
			return;
		}

		StateHasChanged();
		RunDetached(NoteArrangementChangedAsync);
	}

	private string? GetColumnWidth(MokaColumn<TItem> col) =>
		_columnWidths.TryGetValue(col, out double w)
			? FormattableString.Invariant($"{w}px")
			: null;

	// Columns register after the first render pass builds the header, so the handles may not be in
	// the DOM yet. Only record the layout version once every expected handle is actually wired.
	private async Task InitColumnResizeAsync()
	{
		int expected = DrawHeader ? _columns.Count(c => c.Resizable && IsColumnVisible(c)) : 0;
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
}
