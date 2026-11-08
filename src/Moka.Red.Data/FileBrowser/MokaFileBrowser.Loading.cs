namespace Moka.Red.Data.FileBrowser;

public partial class MokaFileBrowser<TItem>
{
	private CancellationTokenSource? _loadCancellation;
	private bool _loading;
	private int _loadVersion;
	private Exception? _problem;
	private string? _selectNameAfterLoad;

	private async Task GoToAsync(string folder, bool record = true)
	{
		if (string.Equals(folder, _folder, StringComparison.Ordinal))
		{
			return;
		}

		_folder = folder;
		if (record)
		{
			_history.Visit(folder);
		}

		ForgetFolder();
		FocusListWhenDrawn();
		await FolderChanged.InvokeAsync(folder);
		await LoadAsync(fresh: true);
	}

	private Task GoBackAsync() => _history.Back() is { } folder ? GoToAsync(folder, record: false) : Task.CompletedTask;

	private Task GoForwardAsync() => _history.Forward() is { } folder ? GoToAsync(folder, record: false) : Task.CompletedTask;

	private Task GoUpAsync() => ParentFolder is { } parent ? GoToAsync(parent) : Task.CompletedTask;

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

	private async Task LoadAsync(bool fresh)
	{
		int version = ++_loadVersion;
		await CancelRunningLoadAsync();
		_loadCancellation = new CancellationTokenSource();
		CancellationToken token = _loadCancellation.Token;
		string folder = CurrentFolder;

		_loading = true;
		_problem = null;
		if (fresh)
		{
			_listed = false;
			_rows = [];
			RebuildVisible();
		}

		StateHasChanged();
		try
		{
			IReadOnlyList<TItem> items = await Load(folder, token);
			if (version != _loadVersion)
			{
				return;
			}

			Accept(items);
		}
#pragma warning disable CA1031 // Any failure of the app's load is what the person sees in place of the list.
		catch (Exception ex)
#pragma warning restore CA1031
		{
			if (version != _loadVersion || token.IsCancellationRequested)
			{
				return;
			}

			_problem = ex;
			_rows = [];
			_byKey.Clear();
			RebuildVisible();
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
		RebuildVisible();
		ApplyNameSelection();
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

	private Task ApplyFilterAsync(string filter)
	{
		_filter = filter ?? "";
		RebuildVisible();
		return RaiseSelectionIfChangedAsync();
	}

	private Task ClearFilterAsync()
	{
		_filter = "";
		RebuildVisible();
		return RaiseSelectionIfChangedAsync();
	}

	private void RebuildVisible()
	{
		string needle = FilterNeedle;
		IEnumerable<MokaFileRow<TItem>> rows = needle.Length > 0
			? _rows.Where(row => row.Info.Name.Contains(needle, StringComparison.CurrentCultureIgnoreCase))
			: _rows;

		List<MokaFileRow<TItem>> visible = rows.ToList();
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

	// Folders come first whichever way the sort runs. The name and then the key settle ties, so the order is total.
	private int CompareRows(MokaFileRow<TItem> a, MokaFileRow<TItem> b)
	{
		int group = (a.Info.IsFolder ? 0 : 1).CompareTo(b.Info.IsFolder ? 0 : 1);
		if (group != 0)
		{
			return group;
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
}
