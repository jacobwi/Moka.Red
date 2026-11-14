using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Data.Common;

namespace Moka.Red.Data.Table;

// Spreadsheet blocks of cells: marked with the mouse or Shift and the arrows, copied as tab-separated text, and a pasted
// block written into the editable cells through the editing path, each row checked and saved through OnRowSave. The block
// lives in moka-table.js, which marks the cells itself; .NET hears it only when it is copied or pasted into.
public partial class MokaTable<TItem>
{
	private bool _rangesBound;
	private string? _pasteStatus;

	/// <summary>
	///     Lets people mark a block of cells, by dragging over them or with Shift and the arrows, copy it with Ctrl+C as
	///     tab-separated text a spreadsheet pastes as cells, and paste such a block with Ctrl+V into the editable cells
	///     from the focused one, each row checked like an edit and saved through <see cref="OnRowSave" />. A single pasted
	///     value fills the whole block. Default false.
	/// </summary>
	[Parameter]
	public bool CellRanges { get; set; }

	/// <summary>
	///     Called from JS for Ctrl+C on a block of cells, or on one cell: the block as tab-separated text, without headings.
	/// </summary>
	/// <param name="firstRow">The <c>data-row-index</c> of the block's first row.</param>
	/// <param name="firstColumn">The <c>data-col-index</c> of its first column.</param>
	/// <param name="lastRow">The <c>data-row-index</c> of its last row.</param>
	/// <param name="lastColumn">The <c>data-col-index</c> of its last column.</param>
	/// <returns>The text.</returns>
	[JSInvokable]
	public string CopyCellsText(int firstRow, int firstColumn, int lastRow, int lastColumn)
	{
		List<(MokaColumn<TItem> Col, int ColIdx)> columns = VisibleColumnsInOrder();
		int fromColumn = Math.Clamp(Math.Min(firstColumn, lastColumn), 0, Math.Max(columns.Count - 1, 0));
		int toColumn = Math.Clamp(Math.Max(firstColumn, lastColumn), 0, Math.Max(columns.Count - 1, 0));
		List<IReadOnlyList<string>> rows = [];
		for (int row = Math.Min(firstRow, lastRow); row <= Math.Max(firstRow, lastRow); row++)
		{
			if (!TryGetRow(row, out TItem item))
			{
				continue;
			}

			List<string> cells = [];
			for (int c = fromColumn; c <= toColumn && c < columns.Count; c++)
			{
				cells.Add(columns[c].Col.ExportTextOf(item));
			}

			rows.Add(cells);
		}

		return MokaTsv.Write(rows);
	}

	/// <summary>
	///     Called from JS for Ctrl+V on a cell: writes the pasted block into the editable cells from the block's first cell,
	///     or fills the marked block with a single value. Each row's values are read in the column's type and checked, and
	///     each row with a change is saved through <see cref="OnRowSave" />. A cell that doesn't read, fails its check or
	///     isn't editable is left as it was and counted.
	/// </summary>
	/// <param name="firstRow">The <c>data-row-index</c> of the marked block's first row, or the focused cell's.</param>
	/// <param name="firstColumn">The <c>data-col-index</c> of its first column.</param>
	/// <param name="lastRow">The <c>data-row-index</c> of its last row.</param>
	/// <param name="lastColumn">The <c>data-col-index</c> of its last column.</param>
	/// <param name="text">The pasted text.</param>
	[JSInvokable]
	public void PasteCells(int firstRow, int firstColumn, int lastRow, int lastColumn, string text) =>
		RunDetached(() => PasteAsync(Math.Min(firstRow, lastRow), Math.Min(firstColumn, lastColumn),
			Math.Max(firstRow, lastRow), Math.Max(firstColumn, lastColumn), text));

	private async Task PasteAsync(int top, int left, int bottom, int right, string text)
	{
		if (!CellRanges || !OnRowSave.HasDelegate || _edit is not null)
		{
			return;
		}

		List<List<string>> block = MokaTsv.Read(text);
		if (block.Count == 0)
		{
			return;
		}

		// One value fills the marked block, as a spreadsheet does; a block goes in from the first cell at its own size.
		bool fill = block.Count == 1 && block[0].Count == 1;
		int rowCount = fill ? bottom - top + 1 : block.Count;
		int columnCount = fill ? right - left + 1 : block.Max(static row => row.Count);
		List<(MokaColumn<TItem> Col, int ColIdx)> columns = VisibleColumnsInOrder();

		int pasted = 0;
		int refused = 0;
		bool anySaved = false;
		for (int r = 0; r < rowCount; r++)
		{
			if (!TryGetRow(top + r, out TItem item))
			{
				refused += columnCount;
				continue;
			}

			TableEdit<TItem> edit = new(item, CopyForEdit(item), RowKey(item), null, isNew: false);
			List<MokaColumn<TItem>> changed = [];
			for (int c = 0; c < columnCount; c++)
			{
				string? cellText = fill ? block[0][0] : c < block[r].Count ? block[r][c] : null;
				int position = left + c;
				if (cellText is null)
				{
					continue;
				}

				if (position >= columns.Count || columns[position].Col is not { Editable: true } col
				                               || !col.TryParseText(cellText, edit.Draft, out object? value)
				                               || col.Validate?.Invoke(edit.Draft, value) is not null)
				{
					refused++;
					continue;
				}

				if (!Equals(col.ValueOf(item), value))
				{
					edit.Set(col, value);
					changed.Add(col);
				}
			}

			if (changed.Count == 0)
			{
				continue;
			}

			List<MokaTableCellChange> changes = ChangesOf(edit, changed);
			if (!CheckEdit(edit, changed))
			{
				refused += changed.Count;
				continue;
			}

			MokaTableRowSave<TItem> save = new(item, edit.Draft, changes, MokaTableSaveKind.Paste);
			if (await RaiseSaveAsync(save, edit))
			{
				pasted += changes.Count;
				anySaved = true;
				ReplaceShownItemAfterPaste(save);
			}
			else
			{
				refused += changes.Count;
			}
		}

		_pasteStatus = Words.Pasted(pasted, refused);
		_lastSave = null;
		if (anySaved && ServerData is null)
		{
			LoadClientData();
		}

		StateHasChanged();
	}

	// A server's row shows its pasted values where it stands, as an edited one does.
	private void ReplaceShownItemAfterPaste(MokaTableRowSave<TItem> save)
	{
		if (ServerData is not null)
		{
			ReplaceShownRow(save.Item, save.Updated);
		}
	}

	private async Task SyncRangesAsync()
	{
		if (CellRanges == _rangesBound)
		{
			return;
		}

		_rangesBound = CellRanges;
		_dotNetRef ??= DotNetObjectReference.Create(this);
		await InvokeTableJsAsync(CellRanges ? "bindRanges" : "unbindRanges", _wrapperRef, _dotNetRef);
	}
}
