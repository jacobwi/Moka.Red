using Microsoft.AspNetCore.Components.Web;

namespace Moka.Red.Data.FileBrowser;

public partial class MokaFileBrowser<TItem>
{
	private readonly HashSet<string> _selection = new(StringComparer.Ordinal);

	private string? _anchor;
	private string? _cursor;
	private HashSet<string>? _lastRaised;

	// Only once a folder's items are known can a selection be checked against them.
	private bool _listed;
	private IReadOnlySet<string>? _lastSelected;
	private List<MokaFileRow<TItem>> _selectedRows = [];

	private MokaFileRow<TItem>? CursorRow =>
		_cursor is not null && _byKey.TryGetValue(_cursor, out MokaFileRow<TItem>? row) ? row : null;

	// What an action works on: the selection, or the row under the cursor when nothing is selected.
	private List<MokaFileRow<TItem>> TargetRows()
	{
		if (_selectedRows.Count > 0)
		{
			return _selectedRows;
		}

		return CursorRow is { } row && IsShowing(row) ? [row] : [];
	}

	private void ApplySelectedParameter()
	{
		if (ReferenceEquals(Selected, _lastSelected))
		{
			return;
		}

		_lastSelected = Selected;
		if (Selected is null || _selection.SetEquals(Selected))
		{
			return;
		}

		_selection.Clear();
		_selection.UnionWith(Selected);
		RefreshSelection();
		_lastRaised = new HashSet<string>(_selection, StringComparer.Ordinal);
	}

	private void ClearSelectionSilently()
	{
		_selection.Clear();
		_selectedRows = [];
	}

	// Rows can leave through a reload or a filter, and what they took from the selection goes with them.
	private void RefreshSelection()
	{
		if (_listed)
		{
			_selection.IntersectWith(_visible.Select(row => row.Key));
		}

		_selectedRows = _visible.FindAll(row => _selection.Contains(row.Key));
	}

	private Task RaiseSelectionIfChangedAsync()
	{
		if (_lastRaised is null ? _selection.Count == 0 : _lastRaised.SetEquals(_selection))
		{
			return Task.CompletedTask;
		}

		_lastRaised = new HashSet<string>(_selection, StringComparer.Ordinal);
		return SelectedChanged.InvokeAsync(new HashSet<string>(_selection, StringComparer.Ordinal));
	}

	private void SelectOnlyCore(MokaFileRow<TItem> row)
	{
		_selection.Clear();
		_selection.Add(row.Key);
		_cursor = row.Key;
		_anchor = row.Key;
		RefreshSelection();
	}

	private Task SelectOnlyAsync(MokaFileRow<TItem> row)
	{
		SelectOnlyCore(row);
		return RaiseSelectionIfChangedAsync();
	}

	private Task ToggleAsync(MokaFileRow<TItem> row)
	{
		if (!_selection.Add(row.Key))
		{
			_selection.Remove(row.Key);
		}

		_cursor = row.Key;
		_anchor = row.Key;
		RefreshSelection();
		return RaiseSelectionIfChangedAsync();
	}

	private Task SelectRangeAsync(MokaFileRow<TItem> row, bool addToSelection)
	{
		int anchorIndex = _anchor is not null && _byKey.TryGetValue(_anchor, out MokaFileRow<TItem>? anchorRow) && IsShowing(anchorRow)
			? anchorRow.Index
			: row.Index;
		int first = Math.Min(anchorIndex, row.Index);
		int last = Math.Max(anchorIndex, row.Index);

		if (!addToSelection)
		{
			_selection.Clear();
		}

		for (int i = first; i <= last; i++)
		{
			_selection.Add(_visible[i].Key);
		}

		_cursor = row.Key;
		_anchor ??= row.Key;
		RefreshSelection();
		return RaiseSelectionIfChangedAsync();
	}

	private Task SelectAllAsync()
	{
		_selection.Clear();
		_selection.UnionWith(_visible.Select(row => row.Key));
		RefreshSelection();
		return RaiseSelectionIfChangedAsync();
	}

	private Task ClearSelectionAsync()
	{
		_selection.Clear();
		_selectedRows = [];
		_anchor = null;
		return RaiseSelectionIfChangedAsync();
	}

	private Task HandleListClick() => _selection.Count > 0 ? ClearSelectionAsync() : Task.CompletedTask;

	private async Task HandleItemClick(MokaFileRow<TItem> row, MouseEventArgs args)
	{
		if (IsEditing(row))
		{
			return;
		}

		bool toggle = args.CtrlKey || args.MetaKey;
		if (args.ShiftKey)
		{
			await SelectRangeAsync(row, toggle);
		}
		else if (toggle)
		{
			await ToggleAsync(row);
		}
		else if (_coarsePointer && _selection.Count == 0)
		{
			await OpenAsync(row);
		}
		else if (_coarsePointer)
		{
			await ToggleAsync(row);
		}
		else
		{
			await SelectOnlyAsync(row);
		}
	}

	private Task HandleItemDoubleClickAsync(MokaFileRow<TItem> row) =>
		IsEditing(row) || _coarsePointer ? Task.CompletedTask : OpenAsync(row);

	private async Task OpenAsync(MokaFileRow<TItem> row)
	{
		if (row.Info.IsFolder)
		{
			await GoToAsync(row.Key);
		}
		else if (OnOpenFile.HasDelegate)
		{
			await OnOpenFile.InvokeAsync(row.Item);
		}
	}
}
