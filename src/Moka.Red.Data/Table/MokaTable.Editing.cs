using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Core.Enums;
using Moka.Red.Feedback.Dialog;

namespace Moka.Red.Data.Table;

// Editing rows in place: a cell or a whole row opens with editors that fit the columns, the values are checked, and the
// app saves them through OnRowSave. The table writes into a copy and changes none of the app's objects.
public partial class MokaTable<TItem>
{
	private TableEdit<TItem>? _edit;

	// The editor to focus after the next render, by its column, or the first editor of the row when null.
	private bool _editFocusPending;
	private MokaColumn<TItem>? _editFocusColumn;

	// Leaving an editor with Enter or Escape removes it, and focus with it; the cell takes focus back.
	private bool _cellFocusPending;

	// The last save, which Ctrl+Z or the undo button puts back.
	private TableUndo? _lastSave;

	// The EditContext the validators were last drawn against.
	private EditContext? _validatorsDrawnFor;

	/// <summary>
	///     How cells open for editing: one at a time, or a whole row with Save and Cancel. Default
	///     <see cref="MokaTableEditMode.Cell" />. Columns opt in with <see cref="MokaColumn{TItem}.Editable" />, and a
	///     <see cref="MokaEditColumn{TItem}" /> draws Edit, Save, Cancel and Delete buttons in each row.
	/// </summary>
	[Parameter]
	public MokaTableEditMode EditMode { get; set; }

	/// <summary>
	///     Saves an edited, added or pasted row, or puts back the last save on undo. The table hands over the row as it was,
	///     the row with the changes written into it and the list of changes, and changes none of your objects: save them to
	///     your data and pass the table its rows again. A message added with
	///     <see cref="MokaTableRowSave{TItem}.AddError(string, string)" />, or an exception, keeps the row open with it.
	///     Rows in memory are run through the search, filters and sort again afterwards; a server-side source's row shows
	///     the saved values without asking the server, and an added row loads the page again.
	/// </summary>
	[Parameter]
	public EventCallback<MokaTableRowSave<TItem>> OnRowSave { get; set; }

	/// <summary>
	///     Deletes a row, from the Delete button of a <see cref="MokaEditColumn{TItem}" />. With <see cref="ConfirmDelete" />
	///     the table asks first. Delete it from your data; the table loads its rows again afterwards.
	/// </summary>
	[Parameter]
	public EventCallback<TItem> OnRowDelete { get; set; }

	/// <summary>
	///     Asks before a row is deleted, in a dialog of the app's <c>IMokaDialogService</c>, which runs the delete with the
	///     dialog open and shows a failure there. Default true. Without the dialog service registered the row is deleted at
	///     once.
	/// </summary>
	[Parameter]
	public bool ConfirmDelete { get; set; } = true;

	/// <summary>
	///     Makes the row an Add button starts from. With it and <see cref="OnRowSave" /> the toolbar has an Add button, and
	///     the new row opens with every editable cell at <see cref="AddRowPosition" />.
	/// </summary>
	[Parameter]
	public Func<TItem>? NewItem { get; set; }

	/// <summary>Where a new row opens. Default <see cref="MokaTableAddPosition.Top" />.</summary>
	[Parameter]
	public MokaTableAddPosition AddRowPosition { get; set; }

	/// <summary>
	///     Copies a row before it is edited, for a class whose properties the columns' setters set in place: the setters
	///     then write into the copy, never into your object. A record needs none, since a setter with <c>with</c> makes a
	///     new one.
	/// </summary>
	[Parameter]
	public Func<TItem, TItem>? EditCopy { get; set; }

	/// <summary>
	///     Validator components, such as <c>&lt;DataAnnotationsValidator /&gt;</c>, run against an <c>EditContext</c> of
	///     the row being edited before it is saved. A column finds its messages under its
	///     <see cref="MokaColumn{TItem}.Property" />; a message about the whole row shows over the row's editors.
	/// </summary>
	[Parameter]
	public RenderFragment? Validators { get; set; }

	/// <summary>
	///     Shows a line under the rows after a save, saying what changed, with an Undo button. Default true. Ctrl+Z on a
	///     cell undoes the last save either way.
	/// </summary>
	[Parameter]
	public bool ShowUndo { get; set; } = true;

	/// <summary>Whether a row is open for editing, or a new one for adding.</summary>
	public bool IsEditing => _edit is not null;

	/// <summary>Whether there is a save to undo.</summary>
	public bool CanUndo => _lastSave is not null;

	internal MokaTableText EditWords => Words;

	private bool CanAddRows => NewItem is not null && OnRowSave.HasDelegate;

	private bool AnyEditable => _columns.Exists(static col => col.Editable);

	/// <summary>Opens a row for editing, at one column in cell mode, or the whole row in row mode.</summary>
	/// <param name="item">The row, one the table shows.</param>
	/// <param name="column">The title of the column to open in cell mode, or null for the first editable one.</param>
	/// <returns>A task that completes once the editor is drawn.</returns>
	public async Task StartEditAsync(TItem item, string? column = null)
	{
		MokaColumn<TItem>? col = column is null
			? EditableColumns().FirstOrDefault()
			: _columns.Find(c => c.Editable && c.Title == column);
		if (col is not null && await BeginEditAsync(item, col))
		{
			StateHasChanged();
		}
	}

	/// <summary>Opens a new row for adding, made by <see cref="NewItem" />, with every editable cell.</summary>
	/// <returns>A task that completes once the row is drawn.</returns>
	public async Task AddRowAsync()
	{
		if (NewItem is null || !await CloseOpenEditAsync())
		{
			return;
		}

		TItem item = NewItem();
		_edit = new TableEdit<TItem>(item, CopyForEdit(item), null, null, isNew: true);
		_editFocusPending = true;
		_editFocusColumn = null;
		StateHasChanged();
	}

	/// <summary>Saves the open row, as Enter or the Save button does.</summary>
	/// <returns>Whether the row was saved, or there was nothing to save.</returns>
	public async Task<bool> SaveEditAsync()
	{
		bool saved = _edit is null || await SaveOpenEditAsync();
		StateHasChanged();
		return saved;
	}

	/// <summary>Closes the open row without saving it, as Escape does.</summary>
	public void CancelEdit()
	{
		CancelOpenEdit();
		StateHasChanged();
	}

	/// <summary>Puts back the values the last save changed, through <see cref="OnRowSave" />.</summary>
	/// <returns>Whether the undo was saved.</returns>
	public async Task<bool> UndoAsync()
	{
		bool undone = await UndoLastSaveAsync();
		StateHasChanged();
		return undone;
	}

	// ── State the row template reads ──

	private bool IsEditingRow(MokaTableRow<TItem> row) =>
		_edit is { IsNew: false } edit && Equals(edit.Key, RowKey(row.Item));

	internal bool IsEditingItem(TItem item) => _edit is { IsNew: false } edit && Equals(edit.Key, RowKey(item));

	internal bool IsNewItem(TItem item) => _edit is { IsNew: true } edit && ReferenceEquals(edit.Draft, item);

	private bool IsEditingCell(MokaTableRow<TItem> row, MokaColumn<TItem> col) =>
		_edit is { IsNew: false } edit && col.Editable && Equals(edit.Key, RowKey(row.Item))
		&& (edit.Column is null || ReferenceEquals(edit.Column, col));

	internal bool IsSavingEdit => _edit?.Saving == true;

	internal bool HasDeletes => OnRowDelete.HasDelegate;

	internal TableEdit<TItem>? OpenEdit => _edit;

	private IEnumerable<MokaColumn<TItem>> EditableColumns() =>
		VisibleColumnsInOrder().Select(static entry => entry.Col).Where(static col => col.Editable);

	internal static MokaColumnEditor EditorOf(MokaColumn<TItem> col, TItem item) =>
		col.Editor == MokaColumnEditor.Auto ? col.AutoEditor(item) : col.Editor;

	internal string EditorLabel(MokaColumn<TItem> col) => ColumnName(col);

	/// <summary>What a column is called: its title, else what its kind shows, else its place among the columns.</summary>
	internal string ColumnName(MokaColumn<TItem> col) =>
		col.Title ?? col.KindName ?? Words.ColumnNumber(_columns.IndexOf(col) + 1);

	/// <summary>A column's menu: by its title or kind, else by its place, worded so the number doesn't read as a title.</summary>
	internal string ColumnMenuLabel(MokaColumn<TItem> col) => (col.Title ?? col.KindName) is { } name
		? Words.ColumnMenu(name)
		: Words.ColumnMenuAt(_columns.IndexOf(col) + 1);

	// A list editor's choices: the column's own, else its filter's, else an enum's values, else the values the loaded rows
	// have, each written the way the column's text reads back.
	internal IReadOnlyList<MokaFilterOption> EditChoicesFor(MokaColumn<TItem> col, TItem item)
	{
		if (col.EditOptions is { } own)
		{
			return own;
		}

		if (col.FilterOptions is { } filter)
		{
			return filter;
		}

		Type type = TableValues.Plain(col.EditValueType(item));
		if (type.IsEnum)
		{
			return Enum.GetNames(type).Select(static name => new MokaFilterOption(name, name)).ToList();
		}

		HashSet<string> seen = new(StringComparer.Ordinal);
		List<MokaFilterOption> found = [];
		foreach (TItem row in LoadedItems)
		{
			object? value = col.ValueOf(row);
			if (value is null)
			{
				continue;
			}

			string written = TableValues.ToInvariantText(value);
			if (seen.Add(written))
			{
				found.Add(new MokaFilterOption(written, col.TextOf(row)));
			}

			if (found.Count == MaxEditChoices)
			{
				break;
			}
		}

		found.Sort(static (a, b) => StringComparer.CurrentCulture.Compare(a.Text, b.Text));
		return found;
	}

	private const int MaxEditChoices = 200;

	// ── Opening and closing ──

	private TItem CopyForEdit(TItem item) => EditCopy is null ? item : EditCopy(item);

	private async Task<bool> BeginEditAsync(TItem item, MokaColumn<TItem> col)
	{
		if (!col.Editable)
		{
			return false;
		}

		object key = RowKey(item)!;
		if (_edit is { IsNew: false } open && Equals(open.Key, key))
		{
			if (EditMode == MokaTableEditMode.Cell && !ReferenceEquals(open.Column, col))
			{
				if (!await SaveOpenEditAsync())
				{
					return false;
				}

				_edit = new TableEdit<TItem>(item, CopyForEdit(item), key, col, isNew: false);
			}

			_editFocusPending = true;
			_editFocusColumn = col;
			return true;
		}

		if (!await CloseOpenEditAsync())
		{
			return false;
		}

		_pasteStatus = null;
		_edit = new TableEdit<TItem>(item, CopyForEdit(item), key,
			EditMode == MokaTableEditMode.Cell ? col : null, isNew: false);
		_editFocusPending = true;
		_editFocusColumn = col;
		return true;
	}

	// An open edit has to be saved before another starts. A save that fails keeps it open, and the new one doesn't start.
	private async Task<bool> CloseOpenEditAsync() => _edit is null || await SaveOpenEditAsync();

	private void CancelOpenEdit()
	{
		if (_edit is not { } edit)
		{
			return;
		}

		FocusCellOf(edit);
		_edit = null;
	}

	// Focus goes back to the cell the edit was in, found by the row's key once a save has drawn the rows again, since a
	// sort can move it and an added row takes its place among them. A new row that wasn't saved leaves focus on the first
	// cell of the grid.
	private void FocusCellOf(TableEdit<TItem> edit)
	{
		int row = RowIndexOfEdit(edit);
		row = row < 0 && edit.IsNew ? 0 : row;
		MokaColumn<TItem>? col = edit.Column ?? _editFocusColumn ?? EditableColumns().FirstOrDefault();
		int position = col is null ? 0 : VisibleColumnsInOrder().FindIndex(entry => ReferenceEquals(entry.Col, col));
		if (row >= 0 && row < _displayRows.Count)
		{
			_focusedCell = (_displayRows[row].Index, Math.Max(position, 0));
			_cellFocusPending = true;
		}
	}

	private int RowIndexOfKey(object? key) => key is null ? -1 : _displayRows.FindIndex(row => Equals(RowKey(row.Item), key));

	// Without an ItemKey a row is known by the item itself, and after a save the app's list holds the updated copy.
	private int RowIndexOfEdit(TableEdit<TItem> edit)
	{
		int row = RowIndexOfKey(RowKey(edit.Draft));
		return row >= 0 || edit.IsNew ? row : RowIndexOfKey(edit.Key);
	}

	// ── Values ──

	// A value entered draws only its editor again, which shows the value and what is wrong with it: a keystroke in a
	// table of thousands of rows doesn't walk every row.
	internal void SetEditValue(MokaColumn<TItem> col, object? value)
	{
		if (_edit is not { Saving: false } edit)
		{
			return;
		}

		edit.Set(col, value);
		edit.SetError(col, CheckValue(col, edit, value));
	}

	// Text typed into a text editor for a column whose values aren't text: kept as typed, and the value only once it reads.
	internal void SetEditText(MokaColumn<TItem> col, string? text)
	{
		if (_edit is not { Saving: false } edit)
		{
			return;
		}

		edit.SetText(col, text);
		Type type = col.EditValueType(edit.Draft);
		if (TableValues.TryParse(text ?? string.Empty, type, out object? value))
		{
			edit.Set(col, value);
			edit.SetError(col, CheckValue(col, edit, value));
		}
		else
		{
			edit.SetError(col, Words.InvalidValue(EditorLabel(col)));
		}
	}

	// A number from the number editor, null for an empty box, in the column's own type. A column that can't hold null, or
	// a number its type can't hold, keeps the last value with a message, as unreadable text does.
	internal void SetEditNumber(MokaColumn<TItem> col, double? number)
	{
		if (_edit is not { Saving: false } edit)
		{
			return;
		}

		if (TableValues.TryFromDouble(number, col.EditValueType(edit.Draft), out object? value))
		{
			edit.ClearText(col);
			SetEditValue(col, value);
		}
		else
		{
			edit.SetText(col, TableValues.ToText(number));
			edit.SetError(col, Words.InvalidValue(EditorLabel(col)));
		}
	}

	private static string? CheckValue(MokaColumn<TItem> col, TableEdit<TItem> edit, object? value) =>
		col.Validate?.Invoke(edit.Draft, value);

	// ── Saving ──

	private async Task<bool> SaveOpenEditAsync()
	{
		if (_edit is not { Saving: false } edit)
		{
			return _edit is null;
		}

		List<MokaColumn<TItem>> scope = edit.IsNew || edit.Column is null
			? EditableColumns().ToList()
			: [edit.Column];
		List<MokaTableCellChange> changes = ChangesOf(edit, scope);
		if (changes.Count == 0 && !edit.IsNew && !edit.HasErrors)
		{
			CancelOpenEdit();
			return true;
		}

		// Validators take one EditContext for their life, and a record's setter made a new copy since they were drawn:
		// the render draws them against it before they are asked.
		if (Validators is not null && !ReferenceEquals(edit.EditContext(wanted: true), _validatorsDrawnFor))
		{
			StateHasChanged();
			await Task.Yield();
			if (!ReferenceEquals(_edit, edit))
			{
				return false;
			}
		}

		if (!CheckEdit(edit, scope))
		{
			_editFocusPending = true;
			_editFocusColumn = edit.FirstInvalid(scope) ?? _editFocusColumn;
			return false;
		}

		MokaTableRowSave<TItem> save = new(edit.Original, edit.Draft, changes,
			edit.IsNew ? MokaTableSaveKind.Add : MokaTableSaveKind.Edit);
		if (!await RaiseSaveAsync(save, edit))
		{
			_editFocusPending = true;
			_editFocusColumn = edit.FirstInvalid(scope) ?? _editFocusColumn;
			return false;
		}

		// Without an ItemKey a row is known by the item itself, and the app's list now holds the updated one.
		if (!edit.IsNew)
		{
			_lastSave = new TableUndo(RowKey(edit.Draft), edit.Key, RowName(edit.Original), changes);
		}

		_edit = null;
		await AfterSaveAsync(save);
		FocusCellOf(edit);
		return true;
	}

	private static List<MokaTableCellChange> ChangesOf(TableEdit<TItem> edit, List<MokaColumn<TItem>> scope)
	{
		List<MokaTableCellChange> changes = [];
		foreach (MokaColumn<TItem> col in scope)
		{
			if (!edit.HasValue(col))
			{
				continue;
			}

			object? before = col.ValueOf(edit.Original);
			object? after = edit.ValueOf(col);
			if (edit.IsNew || !Equals(before, after))
			{
				changes.Add(new MokaTableCellChange(col.Title ?? string.Empty, before, after));
			}
		}

		return changes;
	}

	// Each column's own check, then the validators against the copy. In cell mode only the edited cell counts: a value
	// someone didn't touch shouldn't stop a save.
	private bool CheckEdit(TableEdit<TItem> edit, List<MokaColumn<TItem>> scope)
	{
		edit.RowErrors.Clear();
		foreach (MokaColumn<TItem> col in scope)
		{
			if (edit.HasText(col) && edit.ErrorOf(col) is not null && !edit.TextReads(col))
			{
				continue;
			}

			edit.SetError(col, CheckValue(col, edit, edit.ValueOf(col)));
		}

		if (edit.EditContext(Validators is not null) is { } context)
		{
			context.Validate();
			List<string> unfiled = context.GetValidationMessages().ToList();
			foreach (MokaColumn<TItem> col in EditableColumns())
			{
				List<string> messages = context.GetValidationMessages(new FieldIdentifier(context.Model, col.Property ?? col.Title ?? string.Empty)).ToList();
				foreach (string message in messages)
				{
					unfiled.Remove(message);
				}

				if (messages.Count > 0 && scope.Contains(col) && edit.ErrorOf(col) is null)
				{
					edit.SetError(col, messages[0]);
				}
			}

			// A message filed under no column is about the whole row, which only a whole-row edit can answer.
			if (edit.Column is null)
			{
				edit.RowErrors.AddRange(unfiled);
			}
		}

		return !edit.HasErrorsIn(scope) && edit.RowErrors.Count == 0;
	}

	private async Task<bool> RaiseSaveAsync(MokaTableRowSave<TItem> save, TableEdit<TItem> edit)
	{
		if (!OnRowSave.HasDelegate)
		{
			return true;
		}

		edit.Saving = true;
		StateHasChanged();
		try
		{
			await OnRowSave.InvokeAsync(save);
		}
#pragma warning disable CA1031 // Whatever the save throws is what the person reads, and the row stays open for another try.
		catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
		{
			edit.RowErrors.Add(ex.Message);
		}
		finally
		{
			edit.Saving = false;
		}

		foreach (KeyValuePair<string?, string> error in save.Errors)
		{
			MokaColumn<TItem>? col = error.Key is null ? null : _columns.Find(c => c.Title == error.Key);
			if (col is null)
			{
				edit.RowErrors.Add(error.Value);
			}
			else
			{
				edit.SetError(col, error.Value);
			}
		}

		return edit.RowErrors.Count == 0 && save.Errors.Count == 0;
	}

	// Rows in memory go through the pipeline again, so a list changed in place shows the change. A server's row shows the
	// saved values where it stands; a new row's place is the server's to say.
	private async Task AfterSaveAsync(MokaTableRowSave<TItem> save)
	{
		if (ServerData is null && _stream is null)
		{
			if (Items is not null || _streamedItems is not null)
			{
				LoadClientData();
			}

			return;
		}

		if (save.IsNew || RemoteRows)
		{
			await RefreshRowsAsync();
			return;
		}

		ReplaceShownRow(save.Item, save.Updated);
	}

	private void ReplaceShownRow(TItem before, TItem after)
	{
		object? key = RowKey(before);
		List<TItem> items = [.. _displayItems];
		int at = items.FindIndex(item => Equals(RowKey(item), key));
		if (at < 0)
		{
			return;
		}

		items[at] = after;
		SetDisplayItems(items, _firstRowIndex);
		GroupServerRows();
	}

	private string? RowName(TItem item) => ItemName?.Invoke(item);

	// ── Undo ──

	private async Task<bool> UndoLastSaveAsync()
	{
		if (_lastSave is not { } last || !await CloseOpenEditAsync())
		{
			return false;
		}

		int at = RowIndexOfKey(last.Key);
		at = at >= 0 ? at : RowIndexOfKey(last.KeyBefore);
		if (at < 0)
		{
			_lastSave = null;
			return false;
		}

		TItem current = _displayRows[at].Item;
		TItem draft = CopyForEdit(current);
		List<MokaTableCellChange> back = [];
		foreach (MokaTableCellChange change in last.Changes)
		{
			if (_columns.Find(c => c.Title == change.Column) is not { } col)
			{
				continue;
			}

			back.Add(new MokaTableCellChange(change.Column, col.ValueOf(current), change.OldValue));
			if (col.CanWrite)
			{
				draft = col.WriteValue(draft, change.OldValue);
			}
		}

		MokaTableRowSave<TItem> save = new(current, draft, back, MokaTableSaveKind.Undo);
		TableEdit<TItem> holder = new(current, draft, last.Key, null, isNew: false);
		if (!await RaiseSaveAsync(save, holder))
		{
			_undoProblem = holder.RowErrors.Count > 0 ? holder.RowErrors[0] : save.Errors.Count > 0 ? save.Errors[0].Value : null;
			return false;
		}

		_lastSave = null;
		_undoProblem = null;
		await AfterSaveAsync(save);
		return true;
	}

	private string? _undoProblem;

	private string? UndoText => _lastSave is { } last ? Words.Saved(last.Name) : null;

	private string? UndoStatus => _undoProblem is { } problem ? Words.UndoFailed(problem) : UndoText ?? _pasteStatus;

	// How the open cells save: one at a time, which Tab and leaving the cell do, or the whole row with Save.
	private string EditingKind => _edit?.Column is null ? "row" : "cell";

	private void DismissUndo()
	{
		_lastSave = null;
		_undoProblem = null;
		_pasteStatus = null;
	}

	// ── Deleting ──

	internal async Task DeleteRowAsync(TItem item)
	{
		if (!OnRowDelete.HasDelegate)
		{
			return;
		}

		if (_edit is { } edit && (IsNewItem(item) || Equals(edit.Key, RowKey(item))))
		{
			_edit = null;
		}

		IMokaDialogService? dialogs = ConfirmDelete ? Services.GetService<IMokaDialogService>() : null;
		if (dialogs is null)
		{
			await OnRowDelete.InvokeAsync(item);
		}
		else if (!await dialogs.ConfirmAsync(Words.DeleteRowMessage(RowName(item)), Words.DeleteRowTitle, options =>
		         {
			         options.ConfirmText = Words.Delete;
			         options.ConfirmColor = MokaColor.Error;
			         options.OnConfirmAsync = _ => OnRowDelete.InvokeAsync(item);
		         }))
		{
			return;
		}

		if (_lastSave is { } last && (Equals(last.Key, RowKey(item)) || Equals(last.KeyBefore, RowKey(item))))
		{
			_lastSave = null;
		}

		await RefreshRowsAsync();
	}

	// After a delete or an added row: rows in memory run through the pipeline again, a server's load again behind the rows
	// on screen.
	private async Task RefreshRowsAsync()
	{
		if (ServerData is null && _stream is null)
		{
			LoadClientData();
			return;
		}

		if (RemoteRows)
		{
			await ReloadRemoteRowsAsync();
			return;
		}

		await LoadDataAsync(keepRows: _displayRows.Count > 0);
	}

	// ── Keys from the script ──

	/// <summary>
	///     Called from JS for a key pressed in an open editor: Enter saves, Escape puts the edit back, and in cell mode Tab
	///     and Shift+Tab save and open the next or the previous editable cell. Values reach .NET before the key does.
	/// </summary>
	/// <param name="key">The key: <c>Enter</c>, <c>Escape</c>, <c>Tab</c> or <c>Shift+Tab</c>.</param>
	[JSInvokable]
	public void OnEditorKey(string key) => RunDetached(async () =>
	{
		if (_edit is not { Saving: false } edit)
		{
			return;
		}

		switch (key)
		{
			case "Escape":
				CancelOpenEdit();
				break;
			case "Enter":
				await SaveOpenEditAsync();
				break;
			case "Tab" or "Shift+Tab" when edit.Column is { } col && !edit.IsNew:
				await StepToNextCellAsync(edit, col, key == "Tab");
				break;
			default:
				return;
		}

		StateHasChanged();
	});

	/// <summary>
	///     Called from JS when focus leaves the open cell in cell mode, which saves it. A call about a cell that is no longer
	///     the one open, because Tab or Enter moved the edit on first, does nothing.
	/// </summary>
	/// <param name="rowIndex">The <c>data-row-index</c> of the cell focus left.</param>
	/// <param name="colIndex">Its <c>data-col-index</c>.</param>
	[JSInvokable]
	public void OnEditorLeft(int rowIndex, int colIndex) => RunDetached(async () =>
	{
		if (_edit is { Saving: false, Column: { } col, IsNew: false } edit && IsOpenAt(edit, col, rowIndex, colIndex))
		{
			await SaveOpenEditAsync();
			_cellFocusPending = false;
			StateHasChanged();
		}
	});

	private bool IsOpenAt(TableEdit<TItem> edit, MokaColumn<TItem> col, int rowIndex, int colIndex) =>
		RowIndexOfKey(edit.Key) is var at and >= 0 && _displayRows[at].Index == rowIndex
		&& VisibleColumnsInOrder().FindIndex(entry => ReferenceEquals(entry.Col, col)) == colIndex;

	// Tab goes on to the next editable cell of the row, then to the next row's first; Shift+Tab goes back.
	private async Task StepToNextCellAsync(TableEdit<TItem> edit, MokaColumn<TItem> col, bool forward)
	{
		if (!await SaveOpenEditAsync())
		{
			return;
		}

		// Counted from where the saved row stands now, which a sort on the changed column may have moved.
		int row = RowIndexOfEdit(edit);
		if (row < 0)
		{
			return;
		}

		List<MokaColumn<TItem>> editable = EditableColumns().ToList();
		int at = editable.IndexOf(col) + (forward ? 1 : -1);
		if (at < 0 || at >= editable.Count)
		{
			row += forward ? 1 : -1;
			at = forward ? 0 : editable.Count - 1;
		}

		if (row >= 0 && row < _displayRows.Count && at >= 0 && at < editable.Count)
		{
			_cellFocusPending = false;
			await BeginEditAsync(_displayRows[row].Item, editable[at]);
		}
	}

	private async Task FocusOpenEditorAsync()
	{
		if (!_editFocusPending)
		{
			return;
		}

		_editFocusPending = false;
		if (_edit is null)
		{
			return;
		}

		int position = _editFocusColumn is { } col
			? VisibleColumnsInOrder().FindIndex(entry => ReferenceEquals(entry.Col, col))
			: -1;
		await InvokeTableJsAsync("focusEditInput", _wrapperRef, position);
	}

	/// <summary>What the last save changed, for undo.</summary>
	/// <param name="Key">The row's key after the save.</param>
	/// <param name="KeyBefore">Its key before, for a row the app changed in place.</param>
	/// <param name="Name">The row's name, from <see cref="ItemName" />.</param>
	/// <param name="Changes">The values it changed.</param>
	private sealed record TableUndo(object? Key, object? KeyBefore, string? Name, IReadOnlyList<MokaTableCellChange> Changes);
}
