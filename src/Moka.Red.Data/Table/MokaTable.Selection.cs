using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;

namespace Moka.Red.Data.Table;

// Picking rows: the boxes, select all, the selection bar and the set handed back to the parent.
public partial class MokaTable<TItem>
{
	private bool? _indeterminateState;
	private ElementReference _selectAllRef;
	private HashSet<TItem> _selectedItems = [];

	// The selection the parent passed and the one the table hands back. A parent that passes SelectedItems and listens
	// owns the selection: a change it doesn't take goes back on screen once its handler has run.
	private readonly MokaInputValue<HashSet<TItem>> _selection;

	/// <summary>
	///     Currently selected items. Two-way bindable. The table works on its own copy and hands a new set to
	///     <see cref="SelectedItemsChanged" />, so the bound set is never mutated in place. A parent that passes this and
	///     handles <see cref="SelectedItemsChanged" /> owns the selection: a change it doesn't take back, by passing the new
	///     set, is undone once its handler has run. Bind it with <c>@bind-SelectedItems</c>.
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
	///     With <see cref="Selectable" />, a click on a row selects it, the way a file list does: a plain click picks that
	///     row alone, Ctrl or Cmd adds or removes it, and Shift picks every row from the last one picked. Controls in the
	///     row keep their clicks. <see cref="OnRowClick" /> is still raised. Default false.
	/// </summary>
	[Parameter]
	public bool SelectOnRowClick { get; set; }

	/// <summary>
	///     Template for bulk actions when items are selected. Receives a copy of the selection, so
	///     changing that set does not change what the table has selected: assign a new set to
	///     <see cref="SelectedItems" /> for that.
	/// </summary>
	[Parameter]
	public RenderFragment<HashSet<TItem>>? SelectionActions { get; set; }

	// Selection state, scoped to the displayed rows, matching HandleSelectAll.
	private bool AllDisplayedRowsSelected => _displayItems.Count > 0 && _displayItems.All(_selectedItems.Contains);

	private bool SomeDisplayedRowsSelected =>
		!AllDisplayedRowsSelected && _displayItems.Any(_selectedItems.Contains);

	private string SelectAllLabel =>
		IsShowMoreLook ? Words.SelectAllShown(RowsNoun)
		: ShowPagination ? Words.SelectAllOnPage(RowsNoun)
		: Words.SelectAll(RowsNoun);

	// The selection bar takes the toolbar's place while rows are picked, for the app's actions or for copying the rows.
	private bool SelectionBarShown =>
		_selectedItems.Count > 0 && BulkActions is null && (SelectionActions is not null || (CopySelectedRows && Selectable));

	// The selection bar's template gets a copy, so it cannot change the selection behind the table's back.
	private HashSet<TItem> SelectionCopy => new(_selectedItems, _selectedItems.Comparer);

	// A copy, so selection changes never mutate the parent's set in place.
	private void TakeSelection(ParameterView parameters)
	{
		if (_selection.Take(parameters, nameof(SelectedItems)))
		{
			ShowSelection(_selection.Shown);
		}
	}

	private void ShowSelection(HashSet<TItem>? selection)
	{
		_selectedItems = selection is null ? [] : new HashSet<TItem>(selection);
		_indeterminateState = null;
	}

	// The parent kept its own selection: the boxes go back to it.
	private void ShowParentSelection()
	{
		ShowSelection(_selection.Shown);
		_hasAnchor = false;
		StateHasChanged();
	}

	// Hands out a new set: mutating (and returning) the parent's instance makes a reference-comparing parent see no
	// change at all. A parent that only listens passes none, so it owns nothing and the table keeps its change.
	private void NotifySelectionChanged()
	{
		_indeterminateState = null;
		_rowsCopied = null;
		_selection.Pick(new HashSet<TItem>(_selectedItems, _selectedItems.Comparer), SelectedItemsChanged);
	}

	private void HandleSelectAll(bool selectAll)
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

		_hasAnchor = false;
		NotifySelectionChanged();
	}

	private void HandleRowSelect(TItem item)
	{
		bool run = _runPick;
		_runPick = false;

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
		else if (!(run && PickRun(item)) && !_selectedItems.Remove(item))
		{
			_selectedItems.Add(item);
		}

		_anchorItem = item;
		_hasAnchor = true;
		NotifySelectionChanged();
	}

	// A click on the row: a plain one picks the row alone, Ctrl or Cmd flips it, Shift picks the run from the last one.
	private void SelectFromRowClick(TItem item, MouseEventArgs e)
	{
		if (SingleSelect || e.CtrlKey || e.MetaKey)
		{
			HandleRowSelect(item);
			return;
		}

		if (e.ShiftKey && _hasAnchor && !EqualityComparer<TItem>.Default.Equals(_anchorItem, item))
		{
			_selectedItems.Add(_anchorItem);
			_runPick = true;
			HandleRowSelect(item);
			return;
		}

		if (_selectedItems.Count == 1 && _selectedItems.Contains(item))
		{
			return;
		}

		_selectedItems.Clear();
		_selectedItems.Add(item);
		_anchorItem = item;
		_hasAnchor = true;
		NotifySelectionChanged();
	}

	private void ClearSelection()
	{
		_selectedItems.Clear();
		_hasAnchor = false;
		NotifySelectionChanged();
	}

	// A row's box. @bind on a box in every row would make a binder per row on every render, which retires and sends a
	// handler for each, so the box is written the way @bind compiles: its handler is the row's, made once, and
	// SetUpdatesAttributeName tells Blazor what the browser holds, so a selection the parent refuses goes back on screen.
	private RenderFragment RowBox(MokaTableRow<TItem> row) => builder =>
	{
		builder.OpenElement(0, "input");
		builder.AddAttribute(1, "type", "checkbox");
		builder.AddAttribute(2, "checked", BindConverter.FormatValue(_selectedItems.Contains(row.Item)));
		builder.AddAttribute(3, "aria-label", RowBoxLabel(row.Item));
		builder.AddAttribute(4, "onclick", RowBoxClickHandler);
		builder.AddEventStopPropagationAttribute(5, "onclick", true);
		builder.AddAttribute(6, "onchange", HandlersFor(row.Index).Select);
		builder.SetUpdatesAttributeName("checked");
		builder.CloseElement();
	};

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
}
