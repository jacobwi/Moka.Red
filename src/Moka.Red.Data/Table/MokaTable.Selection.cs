using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;

namespace Moka.Red.Data.Table;

// Picking rows: the boxes, select all, the selection bar and the set handed back to the parent. Under SelectAllMatching
// over remote windows, select all picks every row that matches, kept as data grids over server-side rows keep it: a
// flag for every matching row and the keys of the rows unticked since, because most of those rows were never loaded.
public partial class MokaTable<TItem>
{
	// Stands in for a null row in the keys of the rows unticked.
	private static readonly object NullKey = new();

	private bool? _indeterminateState;
	private ElementReference _selectAllRef;
	private HashSet<TItem> _selectedItems = [];

	// The selection the parent passed and the one the table hands back. A parent that passes SelectedItems and listens
	// owns the selection: a change it doesn't take goes back on screen once its handler has run.
	private readonly MokaInputValue<HashSet<TItem>> _selection;

	// Every row that matches, but for the rows unticked since, by key, with what matched when it was taken.
	private bool _allMatching;
	private readonly Dictionary<object, TItem> _except = [];
	private string? _matchingSignature;

	// Where the last box touched stands in the whole result under remote windows, so a run can reach rows not loaded.
	private int _anchorPlace = -1;

	// A run of picks reaching past the window waits for those rows; a newer run or a reload lets it go.
#pragma warning disable CA2213 // Each run owns and disposes its own. This only points at the one running.
	private CancellationTokenSource? _runCancellation;
#pragma warning restore CA2213

	/// <summary>
	///     Currently selected items. Two-way bindable. The table works on its own copy and hands a new set to
	///     <see cref="SelectedItemsChanged" />, so the bound set is never mutated in place. A parent that passes this and
	///     handles <see cref="SelectedItemsChanged" /> owns the selection: a change it doesn't take back, by passing the new
	///     set, is undone once its handler has run. Bind it with <c>@bind-SelectedItems</c>. A new set passed in ends a
	///     selection of every matching row (<see cref="SelectAllMatching" />).
	/// </summary>
	[Parameter]
	[SuppressMessage("Usage", "CA2227:Collection properties should be read only",
		Justification = "Blazor two-way binding requires a setter.")]
	public HashSet<TItem>? SelectedItems { get; set; }

	/// <summary>Callback when selected items change. Always receives a new set instance.</summary>
	[Parameter]
	public EventCallback<HashSet<TItem>> SelectedItemsChanged { get; set; }

	/// <summary>
	///     With <see cref="ServerData" /> and <see cref="Virtualize" />, the select-all box picks every row that matches the
	///     search, the filters and the quick filter, the rows the table never loaded included, rather than the rows loaded;
	///     unticking a row then leaves it out. Read what is picked from <see cref="SelectionChanged" /> or
	///     <see cref="CurrentSelection" />, since <see cref="SelectedItems" /> holds only the rows picked one by one. A new
	///     search, filter or quick filter clears it, and a plain click on a row under <see cref="SelectOnRowClick" /> picks
	///     that row alone. Unticked rows are known by <see cref="ItemKey" />, else by the item itself. Default false: the
	///     select-all box picks the rows loaded.
	/// </summary>
	[Parameter]
	public bool SelectAllMatching { get; set; }

	/// <summary>
	///     Raised whenever what is picked changes, with all of it: the rows picked one by one, or under
	///     <see cref="SelectAllMatching" /> every matching row but those unticked since.
	/// </summary>
	[Parameter]
	public EventCallback<MokaTableSelection<TItem>> SelectionChanged { get; set; }

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

	/// <summary>
	///     What is picked now: the rows picked one by one, or under <see cref="SelectAllMatching" /> every matching row but
	///     those unticked since, with the request they match.
	/// </summary>
	/// <returns>A new record on each call.</returns>
	public MokaTableSelection<TItem> CurrentSelection() => _allMatching
		? new MokaTableSelection<TItem>
		{
			AllMatching = true,
			Except = [.. _except.Values],
			Count = AllMatchingCount,
			Matching = BuildRequest(1, Math.Max(_totalItems, 1)) with { StartIndex = 0 }
		}
		: new MokaTableSelection<TItem> { Items = PickedInDisplayOrder(), Count = _selectedItems.Count };

	// Whether select all means every matching row: only rows that come window by window are never all loaded.
	private bool MatchesAll => SelectAllMatching && RemoteRows && Selectable && !SingleSelect;

	private int? AllMatchingCount => _totalKnown ? Math.Max(0, _totalItems - _except.Count) : null;

	private bool AnyPicked => _allMatching || _selectedItems.Count > 0;

	private string PickedCountText => !_allMatching ? Words.SelectedCount(_selectedItems.Count)
		: AllMatchingCount is { } count ? Words.SelectedCount(count)
		: Words.AllSelected;

	private object PickKey(TItem item) => RowKey(item) ?? NullKey;

	private bool IsPicked(TItem item) => _allMatching ? !_except.ContainsKey(PickKey(item)) : _selectedItems.Contains(item);

	private void SetPicked(TItem item, bool on)
	{
		if (_allMatching)
		{
			if (on)
			{
				_except.Remove(PickKey(item));
			}
			else
			{
				_except[PickKey(item)] = item;
			}
		}
		else if (on)
		{
			_selectedItems.Add(item);
		}
		else
		{
			_selectedItems.Remove(item);
		}
	}

	// The select-all box: the rows on screen, or under SelectAllMatching every matching row.
	private bool AllDisplayedRowsSelected => MatchesAll
		? _allMatching && _except.Count == 0
		: _displayItems.Count > 0 && _displayItems.All(_selectedItems.Contains);

	private bool SomeDisplayedRowsSelected => MatchesAll
		? _allMatching ? _except.Count > 0 : _selectedItems.Count > 0
		: !AllDisplayedRowsSelected && _displayItems.Any(_selectedItems.Contains);

	private string SelectAllLabel =>
		MatchesAll ? Words.SelectAllMatching(RowsNoun)
		: IsShowMoreLook ? Words.SelectAllShown(RowsNoun)
		: ShowPagination ? Words.SelectAllOnPage(RowsNoun)
		: Words.SelectAll(RowsNoun);

	// The selection bar takes the toolbar's place while rows are picked, for the app's actions or for copying the rows.
	private bool SelectionBarShown =>
		AnyPicked && BulkActions is null && (SelectionActions is not null || (CopySelectedRows && Selectable));

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
		_allMatching = false;
		_except.Clear();
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
		if (SelectionChanged.HasDelegate)
		{
			MokaTableSelection<TItem> now = CurrentSelection();
			RunDetached(() => SelectionChanged.InvokeAsync(now));
		}
	}

	private void HandleSelectAll(bool selectAll)
	{
		if (MatchesAll)
		{
			CancelRemoteRun();
			_selectedItems.Clear();
			_except.Clear();
			_allMatching = selectAll && (!_totalKnown || _totalItems > 0);
			_matchingSignature = _allMatching ? MatchingSignature() : null;
			_hasAnchor = false;
			NotifySelectionChanged();
			return;
		}

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

	// A box ticked or unticked, by a click, a change or Space; with Shift, the run from the last one touched.
	private Task HandleRowSelectAsync(TItem item, int place)
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
		else if (run && _hasAnchor && RemoteRows && _anchorPlace >= 0 && _anchorPlace != place)
		{
			return PickRemoteRunAsync(item, place);
		}
		else if (!(run && _hasAnchor && PickRun(item)))
		{
			SetPicked(item, !IsPicked(item));
			EndAllMatchingWhenEmpty();
		}

		Anchor(item, place);
		NotifySelectionChanged();
		return Task.CompletedTask;
	}

	private void Anchor(TItem item, int place)
	{
		_anchorItem = item;
		_anchorPlace = RemoteRows ? place : -1;
		_hasAnchor = true;
	}

	// A click on the row: a plain one picks the row alone, Ctrl or Cmd flips it, Shift picks the run from the last one.
	private Task SelectFromRowClickAsync(TItem item, int place, MouseEventArgs e)
	{
		if (SingleSelect || e.CtrlKey || e.MetaKey)
		{
			return HandleRowSelectAsync(item, place);
		}

		if (e.ShiftKey && _hasAnchor && !EqualityComparer<TItem>.Default.Equals(_anchorItem, item))
		{
			SetPicked(_anchorItem, true);
			_runPick = true;
			return HandleRowSelectAsync(item, place);
		}

		if (!_allMatching && _selectedItems.Count == 1 && _selectedItems.Contains(item))
		{
			return Task.CompletedTask;
		}

		_allMatching = false;
		_except.Clear();
		_selectedItems.Clear();
		_selectedItems.Add(item);
		Anchor(item, place);
		NotifySelectionChanged();
		return Task.CompletedTask;
	}

	// Every row from the last box touched to this one takes the state of the last box, the way file managers do.
	private bool PickRun(TItem item)
	{
		int to = RowIndexOf(item);
		int from = RowIndexOf(_anchorItem);
		if (from < 0 || to < 0)
		{
			return false;
		}

		bool pick = IsPicked(_anchorItem);
		for (int i = Math.Min(from, to); i <= Math.Max(from, to); i++)
		{
			SetPicked(_displayRows[i].Item, pick);
		}

		EndAllMatchingWhenEmpty();
		_resyncBoxRow = _displayRows[to].Index;
		return true;
	}

	// A run whose end lies outside the window: the rows the window holds take the state at once, and the source is asked
	// for the rest, as many requests as stretches of rows the run leaves out of the window (and, grouped, out of folded
	// groups, whose rows aren't in the run).
	private async Task PickRemoteRunAsync(TItem item, int place)
	{
		bool pick = IsPicked(_anchorItem);
		List<(int First, int Count)> runs = PicksBetween(Math.Min(_anchorPlace, place), Math.Max(_anchorPlace, place));
		Dictionary<int, TItem> known = new(_displayRows.Count);
		foreach (MokaTableRow<TItem> row in _displayRows)
		{
			known[row.Index] = row.Item;
		}

		foreach ((int first, int count) in runs)
		{
			for (int at = first; at < first + count; at++)
			{
				if (known.TryGetValue(at, out TItem? shown))
				{
					SetPicked(shown, pick);
				}
			}
		}

		Anchor(item, place);
		_resyncBoxRow = place;
		EndAllMatchingWhenEmpty();
		NotifySelectionChanged();

		List<(int First, int Count)> missing = MissingRuns(runs, known);
		if (missing.Count == 0 || ServerData is not { } source)
		{
			return;
		}

		CancelRemoteRun();
		using CancellationTokenSource cancellation = new();
		_runCancellation = cancellation;
		int version = _remoteVersion;
		MokaTableResult<TItem>[] answers;
		try
		{
			StateHasChanged();
			answers = await Task.WhenAll(missing.Select(run =>
				source(WindowRequest(run.First, run.Count, rowsOnly: true, cancellation.Token))));
		}
		catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
		{
			return;
		}
		finally
		{
			if (ReferenceEquals(_runCancellation, cancellation))
			{
				_runCancellation = null;
			}
		}

		if (cancellation.IsCancellationRequested || version != _remoteVersion)
		{
			return;
		}

		foreach (MokaTableResult<TItem> answer in answers)
		{
			foreach (TItem fetched in answer.Items)
			{
				SetPicked(fetched, pick);
			}
		}

		EndAllMatchingWhenEmpty();
		NotifySelectionChanged();
	}

	// The rows a run covers: every row between, or grouped, the rows of the lines between, which leaves out folded groups.
	private List<(int First, int Count)> PicksBetween(int from, int to)
	{
		if (_remoteGrouped && LineOfRow(from) is var first and >= 0 && LineOfRow(to) is var last and >= 0)
		{
			return RowRunsInLines(first, last + 1);
		}

		return [(from, to - from + 1)];
	}

	private void CancelRemoteRun()
	{
		_runCancellation?.Cancel();
		_runCancellation = null;
	}

	// Unticking every matching row is picking none.
	private void EndAllMatchingWhenEmpty()
	{
		if (_allMatching && _totalKnown && _except.Count >= _totalItems)
		{
			_allMatching = false;
			_except.Clear();
		}
	}

	// What decides which rows match, as text, so a change to it can be told.
	private string MatchingSignature() => string.Join('\u001E',
		string.IsNullOrWhiteSpace(_searchTerm) ? "" : _searchTerm,
		_filterJoin.ToString(),
		ActiveQuickFilter?.Name ?? "",
		string.Join('\u001D', _filters.Select(static filter =>
			filter.Column + '\u001F' + filter.Operator + '\u001F' + string.Join('\u001F', filter.Values))));

	// Every matching row means the rows that matched when it was taken: a new search, filter or quick filter, or rows
	// that no longer come window by window, end it.
	private void EndAllMatchingIfTheMatchChanged()
	{
		if (_allMatching && (!MatchesAll || MatchingSignature() != _matchingSignature))
		{
			_allMatching = false;
			_except.Clear();
			_matchingSignature = null;
			_hasAnchor = false;
			NotifySelectionChanged();
		}
	}

	private void ClearSelection()
	{
		CancelRemoteRun();
		_selectedItems.Clear();
		_allMatching = false;
		_except.Clear();
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
		builder.AddAttribute(2, "checked", BindConverter.FormatValue(IsPicked(row.Item)));
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
