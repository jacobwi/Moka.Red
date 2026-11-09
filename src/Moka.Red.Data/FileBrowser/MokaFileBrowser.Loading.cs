namespace Moka.Red.Data.FileBrowser;

public partial class MokaFileBrowser<TItem>
{
	private CancellationTokenSource? _loadCancellation;
	private bool _loading;
	private int _loadVersion;
	private Exception? _problem;
	private string? _selectNameAfterLoad;
	private string? _revealAfterLoad;
	private int? _cursorAfterLoad;
	private string? _lastFilter;
	private string _raisedFilter = "";

	private bool CanKeepList => KeepListOnLoadFailure && _listed && _problem is null;

	private async Task GoToAsync(string folder, HistoryStep step = HistoryStep.Visit, string? select = null)
	{
		if (string.Equals(folder, _folder, StringComparison.Ordinal))
		{
			if (select is not null)
			{
				await RevealOrWaitAsync(select);
			}

			return;
		}

		if (CanKeepList)
		{
			await LoadAsync(fresh: false, new PendingFolder(folder, step, select, FromParameter: false));
			return;
		}

		EnterFolder(folder, step);
		_revealAfterLoad = select ?? _revealAfterLoad;
		await FolderChanged.InvokeAsync(folder);
		await LoadAsync(fresh: true);
	}

	private void EnterFolder(string folder, HistoryStep step)
	{
		_folder = folder;
		switch (step)
		{
			case HistoryStep.Back:
				_history.StepBack();
				break;
			case HistoryStep.Forward:
				_history.StepForward();
				break;
			default:
				_history.Visit(folder);
				break;
		}

		ForgetFolder();
		FocusListWhenDrawn();
	}

	private string? FolderLeft => ReselectOnReturn ? CurrentFolder : null;

	private Task GoBackAsync() =>
		_history.PeekBack() is { } folder ? GoToAsync(folder, HistoryStep.Back, FolderLeft) : Task.CompletedTask;

	private Task GoForwardAsync() =>
		_history.PeekForward() is { } folder ? GoToAsync(folder, HistoryStep.Forward, FolderLeft) : Task.CompletedTask;

	private Task GoUpAsync() => ParentFolder is { } parent ? GoToAsync(parent, select: FolderLeft) : Task.CompletedTask;

	private Task RefreshAsync() => ReloadAsync();

	private Task RetryAsync() => LoadAsync(fresh: true);

	// A folder's rows, selection, filter and open editors mean nothing in the next one.
	private void ForgetFolder()
	{
		_rows = [];
		_visible = [];
		_tileRows = [];
		_byKey.Clear();
		_listed = false;
		_filter = "";
		_cursor = null;
		_anchor = null;
		_renamingKey = null;
		_creating = false;
		_failure = null;
		ClearSelectionSilently();
	}

	private async Task LoadAsync(bool fresh, PendingFolder? pending = null)
	{
		int version = ++_loadVersion;
		await CancelRunningLoadAsync();
		_loadCancellation = new CancellationTokenSource();
		CancellationToken token = _loadCancellation.Token;
		string folder = pending?.Folder ?? CurrentFolder;

		_loading = true;
		_problem = null;
		if (pending is not null)
		{
			_failure = null;
		}

		if (fresh)
		{
			_listed = false;
			_rows = [];
			RebuildVisible();
		}

		StateHasChanged();
		bool entered = false;
		bool refused = false;
		try
		{
			IReadOnlyList<TItem> items = await Load(folder, token);
			if (version != _loadVersion)
			{
				return;
			}

			if (pending is not null)
			{
				EnterFolder(pending.Folder, pending.Step);
				_revealAfterLoad = pending.Select ?? _revealAfterLoad;
				entered = true;
				if (pending.FromParameter)
				{
					ApplySelectedParameter();
				}
			}

			Accept(items);
		}
#pragma warning disable CA1031 // Any failure of the app's load is what the person sees, in place of the list or above it.
		catch (Exception ex)
#pragma warning restore CA1031
		{
			if (version != _loadVersion || token.IsCancellationRequested)
			{
				return;
			}

			_revealAfterLoad = null;
			if (pending is not null || (KeepListOnLoadFailure && _listed))
			{
				ShowFailure(ex, Text.LoadFailed, pending is null ? () => LoadAsync(fresh: false) : () => LoadAsync(fresh: false, pending));
				refused = pending?.FromParameter == true;
			}
			else
			{
				_problem = ex;
				_rows = [];
				_byKey.Clear();
				RebuildVisible();
			}
		}
		finally
		{
			if (version == _loadVersion)
			{
				_loading = false;
				_measureColumns = _view.Mode == MokaFileViewMode.Tiles;
				_focusListAt = _rendersBuilt + 1;
				StateHasChanged();
			}
		}

		if (entered && !pending!.FromParameter)
		{
			await FolderChanged.InvokeAsync(folder);
		}
		else if (refused)
		{
			await FolderChanged.InvokeAsync(CurrentFolder);
		}

		await RaiseFilterIfChangedAsync();
		await RaiseSelectionIfChangedAsync();
	}

	private async Task CancelRunningLoadAsync()
	{
		CancellationTokenSource? running = _loadCancellation;
		_loadCancellation = null;
		if (running is null)
		{
			return;
		}

		await running.CancelAsync();
		running.Dispose();
	}

	private void Accept(IReadOnlyList<TItem> items)
	{
		_byKey.Clear();
		List<MokaFileRow<TItem>> rows = new(items.Count);
		foreach (TItem item in items)
		{
			MokaFileInfo info = Describe(item);
			MokaFileRow<TItem> row = new(item, info);
			if (_byKey.TryAdd(info.Key, row))
			{
				rows.Add(row);
			}
		}

		_rows = rows;
		_listed = true;
		ReadItemFilter();
		RebuildVisible();
		ApplyNameSelection();
		ApplyReveal();
		ApplyCursorAfterDelete();
	}

	private void ApplyNameSelection()
	{
		string? name = _selectNameAfterLoad;
		_selectNameAfterLoad = null;
		if (name is null)
		{
			return;
		}

		MokaFileRow<TItem>? row = _visible.Find(candidate => string.Equals(candidate.Info.Name, name, StringComparison.Ordinal));
		if (row is not null)
		{
			SelectOnlyCore(row);
			FocusListWhenDrawn();
		}
	}

	private void ApplyReveal()
	{
		string? key = _revealAfterLoad;
		_revealAfterLoad = null;
		if (key is not null)
		{
			RevealCore(key);
		}
	}

	private async Task RevealOrWaitAsync(string key)
	{
		if (_loading)
		{
			_revealAfterLoad = key;
		}
		else if (RevealCore(key))
		{
			await RaiseFilterThenSelectionAsync();
		}
	}

	// A filter box can be hiding the row, and asking for it by key means it should show. An item filter is the app's own say.
	private bool RevealCore(string key)
	{
		if (!_byKey.TryGetValue(key, out MokaFileRow<TItem>? row) || row.FilteredOut)
		{
			return false;
		}

		if (!IsShowing(row))
		{
			_filter = "";
			RebuildVisible();
		}

		SelectOnlyCore(row);
		_revealLine = LineOf(row);
		_revealAt = _rendersBuilt + 1;
		return true;
	}

	// The row that took the place of what was deleted carries the cursor on, so the next key doesn't start from the top.
	private void ApplyCursorAfterDelete()
	{
		int? place = _cursorAfterLoad;
		_cursorAfterLoad = null;
		if (place is { } index && CursorRow is null && _visible.Count > 0)
		{
			_cursor = _visible[Math.Min(index, _visible.Count - 1)].Key;
			_anchor = _cursor;
		}
	}

	private Task ApplyFilterAsync(string filter)
	{
		_filter = filter ?? "";
		RebuildVisible();
		return RaiseFilterThenSelectionAsync();
	}

	private Task ClearFilterAsync()
	{
		_filter = "";
		RebuildVisible();
		return RaiseFilterThenSelectionAsync();
	}

	private async Task RaiseFilterThenSelectionAsync()
	{
		await RaiseFilterIfChangedAsync();
		await RaiseSelectionIfChangedAsync();
	}

	private Task RaiseFilterIfChangedAsync()
	{
		if (string.Equals(_filter, _raisedFilter, StringComparison.Ordinal))
		{
			return Task.CompletedTask;
		}

		_raisedFilter = _filter;
		_lastFilter = _filter;
		return FilterChanged.InvokeAsync(_filter);
	}

	private void ApplyFilterParameter()
	{
		if (Filter is null || string.Equals(Filter, _lastFilter, StringComparison.Ordinal))
		{
			return;
		}

		_lastFilter = Filter;
		if (!string.Equals(Filter, _filter, StringComparison.Ordinal))
		{
			_filter = Filter;
			_raisedFilter = Filter;
			RebuildVisible();
		}
	}

	// Runs on every parent render, since whatever the predicate reads can change while the delegate stays the same.
	// It only re-sorts when an answer changed.
	private bool ReadItemFilter()
	{
		if (ItemFilter is null && !_itemFilterWasSet)
		{
			return false;
		}

		_itemFilterWasSet = ItemFilter is not null;
		bool changed = false;
		foreach (MokaFileRow<TItem> row in _rows)
		{
			bool filteredOut = ItemFilter is { } keeps && !keeps(row.Item);
			changed |= filteredOut != row.FilteredOut;
			row.FilteredOut = filteredOut;
		}

		return changed;
	}

	private void RebuildVisible()
	{
		string needle = FilterNeedle;
		List<MokaFileRow<TItem>> visible = _rows.FindAll(row =>
			!row.FilteredOut && (needle.Length == 0 || row.Info.Name.Contains(needle, StringComparison.CurrentCultureIgnoreCase)));
		visible.Sort(CompareRows);
		for (int i = 0; i < visible.Count; i++)
		{
			visible[i].Index = i;
		}

		_visible = visible;
		RebuildTileRows();
		RefreshSelection();
	}

	private void RebuildTileRows()
	{
		int columns = Math.Max(1, _tileColumns);
		_tileRows = new List<IReadOnlyList<MokaFileRow<TItem>>>((_visible.Count / columns) + 1);
		for (int start = 0; start < _visible.Count; start += columns)
		{
			_tileRows.Add(_visible.GetRange(start, Math.Min(columns, _visible.Count - start)));
		}
	}

	// The name and then the key settle ties, so the order is total.
	private int CompareRows(MokaFileRow<TItem> a, MokaFileRow<TItem> b)
	{
		if (_view.FoldersFirst)
		{
			int group = (a.Info.IsFolder ? 0 : 1).CompareTo(b.Info.IsFolder ? 0 : 1);
			if (group != 0)
			{
				return group;
			}
		}

		int result = CompareByField(a, b);
		if (result != 0)
		{
			return _view.Descending ? -result : result;
		}

		result = MokaFileNames.Compare(a.Info.Name, b.Info.Name);
		return result != 0 ? result : string.CompareOrdinal(a.Key, b.Key);
	}

	private int CompareByField(MokaFileRow<TItem> a, MokaFileRow<TItem> b)
	{
		switch (_view.SortBy)
		{
			case MokaFileSort.Name:
				return MokaFileNames.Compare(a.Info.Name, b.Info.Name);
			case MokaFileSort.Size when Column(MokaFileSort.Size) is not { Compare: not null }:
				return Nullable.Compare(a.Info.Size, b.Info.Size);
			case MokaFileSort.Modified when Column(MokaFileSort.Modified) is not { Compare: not null }:
				return Nullable.Compare(a.Info.Modified, b.Info.Modified);
			default:
				return Column(_view.SortBy)?.Compare?.Invoke(a.Item, b.Item) ?? 0;
		}
	}

	private MokaFileColumn<TItem>? Column(string id) =>
		_columns.FirstOrDefault(column => string.Equals(column.Id, id, StringComparison.Ordinal));

	private Task SortByAsync(string? sortBy) =>
		SetViewAsync(_view with { SortBy = sortBy ?? MokaFileSort.Name });

	private Task ReverseSortAsync() => SetViewAsync(_view with { Descending = !_view.Descending });

	// A heading sorts by its column and, clicked again, reverses it.
	private Task HeadingClickAsync(string id) => string.Equals(_view.SortBy, id, StringComparison.Ordinal)
		? ReverseSortAsync()
		: SetViewAsync(_view with { SortBy = id, Descending = false });

	private Task SetModeAsync(MokaFileViewMode mode) => SetViewAsync(_view with { Mode = mode });

	private async Task SetViewAsync(MokaFileView view)
	{
		if (view == _view)
		{
			return;
		}

		bool modeChanged = view.Mode != _view.Mode;
		_view = view;
		_lastView = view;
		_measureColumns = modeChanged && view.Mode == MokaFileViewMode.Tiles;
		RebuildVisible();
		await ViewChanged.InvokeAsync(view);
	}

	private enum HistoryStep
	{
		Visit,
		Back,
		Forward
	}

	private sealed record PendingFolder(string Folder, HistoryStep Step, string? Select, bool FromParameter);
}
