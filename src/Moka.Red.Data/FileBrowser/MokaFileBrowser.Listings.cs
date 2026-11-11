namespace Moka.Red.Data.FileBrowser;

// What the browser knows of folders other than the one on screen: the tree, the columns view, the path field and the
// preview read them, through the app's own Load.
public partial class MokaFileBrowser<TItem>
{
	private readonly Dictionary<string, IReadOnlyList<MokaFileInfo>> _listings = new(StringComparer.Ordinal);
	private readonly Dictionary<string, CancellationTokenSource> _peeks = new(StringComparer.Ordinal);
	private readonly Dictionary<string, string> _titles = new(StringComparer.Ordinal);
	private readonly Dictionary<string, List<string>> _crumbChildren = new(StringComparer.Ordinal);
	private readonly List<string> _roots = [];
	private readonly HashSet<string> _treeOpen = new(StringComparer.Ordinal);

	private IReadOnlyList<MokaFileCrumb>? _lastCrumbs;
	private string? _treeReselect;

	// The one folder read for the selection. A newer selection cancels it.
	private string? _selectionPeek;

	private bool WantsListings =>
		FolderTree || PathField || FullStatus || (PreviewPane && _previewOpen) || _view.Mode is MokaFileViewMode.Columns or MokaFileViewMode.Cards or MokaFileViewMode.Gallery;

	private IReadOnlyList<MokaFileInfo>? ListingOf(string folder) => _listings.GetValueOrDefault(folder);

	private int? ItemCountOf(string folder) => ListingOf(folder)?.Count;

	private string TitleOf(string folder) => _titles.GetValueOrDefault(folder) ?? folder;

	private void RememberListing(string folderOf)
	{
		if (!WantsListings)
		{
			return;
		}

		List<MokaFileInfo> infos = _rows.ConvertAll(row => row.Info);
		_listings[folderOf] = infos;
		RememberTitles(infos);
	}

	private void RememberTitles(IEnumerable<MokaFileInfo> infos)
	{
		foreach (MokaFileInfo info in infos)
		{
			if (info.IsFolder)
			{
				_titles[info.Key] = info.Name;
			}
		}
	}

	private void ApplyCrumbsParameter()
	{
		if (ReferenceEquals(Crumbs, _lastCrumbs))
		{
			return;
		}

		_lastCrumbs = Crumbs;
		if (Crumbs.Count == 0)
		{
			return;
		}

		if (!_roots.Contains(Crumbs[0].FolderKey))
		{
			_roots.Add(Crumbs[0].FolderKey);
		}

		for (int i = 0; i < Crumbs.Count; i++)
		{
			MokaFileCrumb crumb = Crumbs[i];
			_titles[crumb.FolderKey] = crumb.Title;
			_treeOpen.Add(crumb.FolderKey);
			if (i < Crumbs.Count - 1)
			{
				string child = Crumbs[i + 1].FolderKey;
				if (!_crumbChildren.TryGetValue(crumb.FolderKey, out List<string>? children))
				{
					_crumbChildren[crumb.FolderKey] = children = [];
				}

				if (!children.Contains(child, StringComparer.Ordinal))
				{
					children.Add(child);
				}
			}
		}
	}

	// Another folder may have changed with an action, so what was read of it goes. The folder on screen reloads anyway.
	private void ForgetListings()
	{
		foreach (CancellationTokenSource peek in _peeks.Values)
		{
			peek.Cancel();
		}

		_peeks.Clear();
		_selectionPeek = null;
		IReadOnlyList<MokaFileInfo>? here = ListingOf(CurrentFolder);
		_listings.Clear();
		if (here is not null)
		{
			_listings[CurrentFolder] = here;
		}
	}

	private void CancelPeeks()
	{
		foreach (CancellationTokenSource peek in _peeks.Values)
		{
			peek.Cancel();
		}

		_peeks.Clear();
	}

	// After every render, for what that render drew without knowing it yet.
	private void RequestListings()
	{
		if (_loading || _dotNetRef is null)
		{
			return;
		}

		if (FolderTree)
		{
			foreach (string root in _roots)
			{
				RequestOpenBranches(root);
			}
		}

		if (_view.Mode == MokaFileViewMode.Columns && ParentFolder is { } parent)
		{
			Peek(parent);
		}

		if (_pathOpen && ParsePath(_pathText).Waiting is { } waiting)
		{
			Peek(waiting);
		}

		RequestSelectionListing();
	}

	private void RequestOpenBranches(string folder)
	{
		if (!_treeOpen.Contains(folder))
		{
			return;
		}

		if (ListingOf(folder) is null)
		{
			Peek(folder);
		}

		foreach (string child in TreeChildren(folder))
		{
			RequestOpenBranches(child);
		}
	}

	private void RequestSelectionListing()
	{
		bool wanted = _view.Mode == MokaFileViewMode.Columns || FullStatus || (PreviewPane && _previewOpen);
		string? folder = wanted && _selectedRows.Count == 1 && _selectedRows[0].Info.IsFolder ? _selectedRows[0].Key : null;
		if (string.Equals(folder, _selectionPeek, StringComparison.Ordinal))
		{
			return;
		}

#pragma warning disable CA2000 // The peek that made it disposes it once its load returns.
		if (_selectionPeek is { } previous && _peeks.Remove(previous, out CancellationTokenSource? running))
#pragma warning restore CA2000
		{
			running.Cancel();
		}

		_selectionPeek = folder;
		if (folder is not null)
		{
			Peek(folder);
		}
	}

	private void Peek(string folder)
	{
		if (_listings.ContainsKey(folder) || _peeks.ContainsKey(folder))
		{
			return;
		}

		CancellationTokenSource peek = new();
		_peeks[folder] = peek;
		RunDetached(() => PeekAsync(folder, peek));
	}

	private async Task PeekAsync(string folder, CancellationTokenSource peek)
	{
		try
		{
			IReadOnlyList<TItem> items = await Load(folder, peek.Token);
			if (IsCurrentPeek(folder, peek))
			{
				Dictionary<string, MokaFileInfo> byKey = new(StringComparer.Ordinal);
				foreach (TItem item in items)
				{
					MokaFileInfo info = Describe(item);
					byKey.TryAdd(info.Key, info);
				}

				_listings[folder] = [.. byKey.Values];
				RememberTitles(byKey.Values);
			}
		}
#pragma warning disable CA1031 // A folder that can't be read beside the one on screen just shows as empty there.
		catch (Exception)
#pragma warning restore CA1031
		{
			if (IsCurrentPeek(folder, peek))
			{
				_listings[folder] = [];
			}
		}
		finally
		{
			if (IsCurrentPeek(folder, peek))
			{
				_peeks.Remove(folder);
				StateHasChanged();
			}

			peek.Dispose();
		}
	}

	private bool IsCurrentPeek(string folder, CancellationTokenSource peek) =>
		!peek.IsCancellationRequested && _peeks.TryGetValue(folder, out CancellationTokenSource? current) && ReferenceEquals(current, peek);

	private List<string> TreeChildren(string folder)
	{
		if (ListingOf(folder) is { } listing)
		{
			List<MokaFileInfo> folders = [.. listing.Where(info => info.IsFolder)];
			folders.Sort((a, b) => MokaFileNames.Compare(a.Name, b.Name));
			return folders.ConvertAll(info => info.Key);
		}

		return _crumbChildren.TryGetValue(folder, out List<string>? known) ? known : [];
	}

	private bool TreeHasChildren(string folder) =>
		ListingOf(folder) is not { } listing || listing.Any(info => info.IsFolder);

	private void SetTreeOpen(string folder, bool open)
	{
		if (open)
		{
			_treeOpen.Add(folder);
		}
		else
		{
			_treeOpen.Remove(folder);
		}
	}

	// Clicking the folder on screen would clear its mark, and passing true again changes nothing in the item, so the
	// next render passes false and the one after puts it back.
	private async Task TreePickedAsync(string folder, bool selected)
	{
		bool here = string.Equals(folder, CurrentFolder, StringComparison.Ordinal);
		if (!selected && here)
		{
			_treeReselect = folder;
		}
		else if (selected && !here)
		{
			await GoToAsync(folder);
		}
	}

	private bool TreeSelected(string folder) =>
		string.Equals(folder, CurrentFolder, StringComparison.Ordinal) && !string.Equals(folder, _treeReselect, StringComparison.Ordinal);
}
