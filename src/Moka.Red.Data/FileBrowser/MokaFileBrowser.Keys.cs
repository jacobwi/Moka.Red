using Microsoft.JSInterop;

namespace Moka.Red.Data.FileBrowser;

public partial class MokaFileBrowser<TItem>
{
	private const int NothingToReveal = -1;

	/// <summary>A key pressed on the list, from the browser. Returns the line to scroll into view, or -1.</summary>
	/// <param name="key">The key's name.</param>
	/// <param name="ctrl">Whether Control or Command was held.</param>
	/// <param name="shift">Whether Shift was held.</param>
	/// <param name="alt">Whether Alt was held.</param>
	/// <param name="pageRows">How many rows fit on a page of the list.</param>
	/// <returns>The line to scroll into view, or -1.</returns>
	[JSInvokable]
	public async Task<int> HandleKey(string key, bool ctrl, bool shift, bool alt, int pageRows)
	{
		ArgumentNullException.ThrowIfNull(key);
		if (_renamingKey is not null || _creating || _pendingDelete is not null || (Keys & KeyFor(key, ctrl, shift, alt)) == 0)
		{
			return NothingToReveal;
		}

		int reveal = alt ? await HandleAltKeyAsync(key) : ctrl ? await HandleCtrlKeyAsync(key) : await HandlePlainKeyAsync(key, shift, pageRows);
		StateHasChanged();
		return reveal;
	}

	/// <summary>Letters typed on the list, from the browser. Moves to the next name starting with them.</summary>
	/// <param name="prefix">The letters typed so far.</param>
	/// <returns>The line to scroll into view, or -1.</returns>
	[JSInvokable]
	public async Task<int> HandleTypeAhead(string prefix)
	{
		if (_visible.Count == 0 || string.IsNullOrEmpty(prefix) || _renamingKey is not null || _creating || !Keys.HasFlag(MokaFileKeys.TypeAhead))
		{
			return NothingToReveal;
		}

		bool repeated = prefix.Length > 1 && prefix.All(letter => char.ToUpperInvariant(letter) == char.ToUpperInvariant(prefix[0]));
		string needle = repeated ? prefix[..1] : prefix;
		int start = CursorRow is { } row && IsShowing(row) ? row.Index : 0;
		int offset = needle.Length == 1 && CursorRow is not null ? 1 : 0;
		for (int step = 0; step < _visible.Count; step++)
		{
			MokaFileRow<TItem> candidate = _visible[(start + offset + step) % _visible.Count];
			if (candidate.Info.Name.StartsWith(needle, StringComparison.CurrentCultureIgnoreCase))
			{
				await SelectOnlyAsync(candidate);
				StateHasChanged();
				return LineOf(candidate);
			}
		}

		return NothingToReveal;
	}

	/// <summary>A row dragged onto a folder, from the browser.</summary>
	/// <param name="source">The key of the row dragged.</param>
	/// <param name="target">The key of the folder it was dropped on.</param>
	/// <returns>A task that ends when the move is done.</returns>
	[JSInvokable]
	public async Task HandleDrop(string source, string target)
	{
		if (Move is null || !_byKey.TryGetValue(source, out MokaFileRow<TItem>? dragged) ||
		    string.Equals(target, _folder, StringComparison.Ordinal))
		{
			return;
		}

		List<MokaFileRow<TItem>> moving = _selection.Contains(source) ? [.. _selectedRows] : [dragged];
		moving.RemoveAll(row => string.Equals(row.Key, target, StringComparison.Ordinal));
		if (moving.Count == 0)
		{
			return;
		}

		await RunAsync(() => Move(moving.ConvertAll(row => row.Item), target));
		StateHasChanged();
	}

	/// <summary>How many tiles fit across, from the browser, when it changes.</summary>
	/// <param name="columns">The number of tiles across.</param>
	[JSInvokable]
	public void HandleColumns(int columns)
	{
		if (columns < 1 || columns == _tileColumns)
		{
			return;
		}

		_tileColumns = columns;
		RebuildTileRows();
		if (_view.Mode == MokaFileViewMode.Tiles)
		{
			StateHasChanged();
		}
	}

	private int LineOf(MokaFileRow<TItem> row) =>
		_view.Mode == MokaFileViewMode.Tiles ? row.Index / Math.Max(1, _tileColumns) : row.Index;

	// The script decides the same way, since it has to choose before .NET hears whether to cancel the browser's default.
	private static MokaFileKeys KeyFor(string key, bool ctrl, bool shift, bool alt)
	{
		if (alt)
		{
			return !ctrl && !shift && key is "ArrowLeft" or "ArrowRight" ? MokaFileKeys.History : MokaFileKeys.None;
		}

		if (ctrl)
		{
			return shift ? MokaFileKeys.None : key.ToUpperInvariant() switch
			{
				"ARROWDOWN" or "ARROWUP" or "ARROWLEFT" or "ARROWRIGHT" or "HOME" or "END" => MokaFileKeys.Move,
				"A" => MokaFileKeys.SelectAll,
				"C" => MokaFileKeys.Copy,
				"X" => MokaFileKeys.Cut,
				"V" => MokaFileKeys.Paste,
				"D" => MokaFileKeys.Duplicate,
				_ => MokaFileKeys.None
			};
		}

		if (key is "ArrowDown" or "ArrowUp" or "ArrowLeft" or "ArrowRight" or "Home" or "End" or "PageDown" or "PageUp")
		{
			return MokaFileKeys.Move;
		}

		return shift ? MokaFileKeys.None : key switch
		{
			" " => MokaFileKeys.Toggle,
			"Enter" => MokaFileKeys.Open,
			"Backspace" => MokaFileKeys.Up,
			"F2" => MokaFileKeys.Rename,
			"Delete" => MokaFileKeys.Delete,
			"Escape" => MokaFileKeys.Escape,
			"F5" => MokaFileKeys.Refresh,
			_ => MokaFileKeys.None
		};
	}

	private async Task<int> HandleAltKeyAsync(string key)
	{
		switch (key)
		{
			case "ArrowLeft":
				await GoBackAsync();
				break;
			case "ArrowRight":
				await GoForwardAsync();
				break;
			default:
				break;
		}

		return NothingToReveal;
	}

	private async Task<int> HandleCtrlKeyAsync(string key)
	{
		switch (key.ToUpperInvariant())
		{
			case "A":
				await SelectAllAsync();
				break;
			case "C" when Copy is null:
				if (TargetRows().Count > 0)
				{
					await OnCopyUnavailable.InvokeAsync();
				}

				break;
			case "C":
				HoldForCopy(TargetRows());
				break;
			case "X":
				HoldForMove(TargetRows());
				break;
			case "V":
				await PasteIntoAsync(CurrentFolder);
				break;
			case "D":
				await DuplicateAsync(TargetRows());
				break;
			default:
				return MoveCursorOnly(key);
		}

		return NothingToReveal;
	}

	// Control and an arrow moves the cursor and leaves the selection, for Space to add to it.
	private int MoveCursorOnly(string key)
	{
		int target = TargetIndex(key, 1);
		if (target < 0)
		{
			return NothingToReveal;
		}

		_cursor = _visible[target].Key;
		return LineOf(_visible[target]);
	}

	private async Task<int> HandlePlainKeyAsync(string key, bool shift, int pageRows)
	{
		switch (key)
		{
			case "ArrowDown" or "ArrowUp" or "ArrowLeft" or "ArrowRight" or "Home" or "End" or "PageDown" or "PageUp":
				return await MoveSelectionAsync(key, shift, pageRows);
			case " ":
				if (CursorRow is { } toggled && IsShowing(toggled))
				{
					await ToggleAsync(toggled);
				}

				break;
			case "Enter" when OnOpenFiles.HasDelegate && _selectedRows.Count > 1 && !_selectedRows.Exists(row => row.Info.IsFolder):
				await OnOpenFiles.InvokeAsync(SelectedItems);
				break;
			case "Enter":
				if (OpenTarget() is { } open)
				{
					await OpenAsync(open);
				}

				break;
			case "F2":
				BeginRename(OnlyTarget());
				break;
			case "Delete":
				await RequestDeleteAsync(TargetRows());
				break;
			case "Backspace":
				await GoUpAsync();
				break;
			case "F5":
				await ReloadAsync();
				break;
			case "Escape":
				if (Keys.HasFlag(MokaFileKeys.EscapeDropsHeld))
				{
					_held = null;
				}

				await ClearSelectionAsync();
				_cursor = null;
				break;
			default:
				break;
		}

		return NothingToReveal;
	}

	private MokaFileRow<TItem>? OpenTarget() => CursorRow is { } row && IsShowing(row) ? row : OnlyTarget();

	private MokaFileRow<TItem>? OnlyTarget()
	{
		List<MokaFileRow<TItem>> targets = TargetRows();
		return targets.Count == 1 ? targets[0] : null;
	}

	private async Task<int> MoveSelectionAsync(string key, bool extend, int pageRows)
	{
		int target = TargetIndex(key, pageRows);
		if (target < 0)
		{
			return NothingToReveal;
		}

		MokaFileRow<TItem> row = _visible[target];
		if (extend)
		{
			await SelectRangeAsync(row, addToSelection: false);
		}
		else
		{
			await SelectOnlyAsync(row);
		}

		return LineOf(row);
	}

	private int TargetIndex(string key, int pageRows)
	{
		if (_visible.Count == 0)
		{
			return -1;
		}

		int current = CursorRow is { } row && IsShowing(row) ? row.Index : -1;
		bool tiles = _view.Mode == MokaFileViewMode.Tiles;
		int across = tiles ? Math.Max(1, _tileColumns) : 1;
		int page = Math.Max(1, pageRows) * across;
		int? target = key switch
		{
			"ArrowDown" => current < 0 ? 0 : current + across,
			"ArrowUp" => current < 0 ? _visible.Count - 1 : current - across,
			"ArrowRight" when tiles => current < 0 ? 0 : current + 1,
			"ArrowLeft" when tiles => current < 0 ? _visible.Count - 1 : current - 1,
			"PageDown" => current < 0 ? 0 : current + page,
			"PageUp" => current < 0 ? 0 : current - page,
			"Home" => 0,
			"End" => _visible.Count - 1,
			_ => null
		};

		return target is { } index ? Math.Clamp(index, 0, _visible.Count - 1) : -1;
	}
}
