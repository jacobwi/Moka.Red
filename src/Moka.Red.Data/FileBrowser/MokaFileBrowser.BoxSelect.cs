using Microsoft.JSInterop;
using Moka.Red.Primitives.SelectionArea;

namespace Moka.Red.Data.FileBrowser;

public partial class MokaFileBrowser<TItem>
{
	private const string BoxModulePath = "./_content/Moka.Red.Core/moka-box-select.js";
	private const string ListSelector = ":scope > .moka-file-browser__body > .moka-file-browser__main > [data-moka-list]";

	private HashSet<string>? _boxBefore;
	private (int Count, int Columns)? _boxShape;

	private int BoxColumns => _view.Mode == MokaFileViewMode.Tiles ? Math.Max(1, _tileColumns) : 1;

	/// <summary>What a box dragged over the list covers, from the browser.</summary>
	/// <param name="phase">A <see cref="MokaBoxSelectPhase" /> value.</param>
	/// <param name="mode">A <see cref="MokaBoxSelectMode" /> value.</param>
	/// <param name="keys">The keys of the drawn rows the box touches.</param>
	/// <param name="range">The first and last line and column, the columns and the count, or null.</param>
	/// <returns>A task that ends once the selection has been applied.</returns>
	[JSInvokable]
	public async Task HandleBoxSelect(int phase, int mode, string[]? keys, int[]? range)
	{
		if (!BoxSelect || !Enum.IsDefined((MokaBoxSelectPhase)phase) || !Enum.IsDefined((MokaBoxSelectMode)mode))
		{
			return;
		}

		MokaBoxSelectRange? covered = range is { Length: 6 }
			? new MokaBoxSelectRange(range[0], range[1], range[2], range[3], range[4], range[5])
			: null;
		await ApplyBoxAsync(new MokaBoxSelectArgs((MokaBoxSelectPhase)phase, (MokaBoxSelectMode)mode, keys ?? [], covered));
		StateHasChanged();
	}

	private async Task ApplyBoxAsync(MokaBoxSelectArgs box)
	{
		if (box.Phase == MokaBoxSelectPhase.Started || _boxBefore is null)
		{
			_boxBefore = new HashSet<string>(_selection, StringComparer.Ordinal);
		}

		List<MokaFileRow<TItem>> covered = CoveredRows(box);
		HashSet<string> next = box.Combine(_boxBefore, covered.Select(row => row.Key), StringComparer.Ordinal);
		_selection.Clear();
		_selection.UnionWith(next);
		RefreshSelection();

		if (box.Phase is MokaBoxSelectPhase.Finished or MokaBoxSelectPhase.Cancelled)
		{
			_boxBefore = null;
			if (box.Phase == MokaBoxSelectPhase.Finished && covered.Count > 0)
			{
				_cursor = covered[0].Key;
				_anchor = covered[0].Key;
			}

			await RaiseSelectionIfChangedAsync();
		}
	}

	// The range counts rows by their place in the list, drawn or not. It is only trusted for the list it was measured on.
	private List<MokaFileRow<TItem>> CoveredRows(MokaBoxSelectArgs box)
	{
		List<MokaFileRow<TItem>> rows = [];
		HashSet<string> seen = new(StringComparer.Ordinal);
		if (box.Range is { } range && range.Count == _visible.Count)
		{
			foreach (int index in range.Indexes)
			{
				MokaFileRow<TItem> row = _visible[index];
				if (seen.Add(row.Key))
				{
					rows.Add(row);
				}
			}
		}

		foreach (string key in box.Keys)
		{
			if (_byKey.TryGetValue(key, out MokaFileRow<TItem>? row) && IsShowing(row) && seen.Add(key))
			{
				rows.Add(row);
			}
		}

		rows.Sort((a, b) => a.Index.CompareTo(b.Index));
		return rows;
	}

	private async Task SyncBoxSelectAsync()
	{
		if (!BoxSelect || _dotNetRef is null)
		{
			if (_boxShape is not null)
			{
				_boxShape = null;
				await SafeModuleInvokeVoidAsync(BoxModulePath, "unbindBoxSelect", _root);
				PutBackBoxStart();
			}

			return;
		}

		(int Count, int Columns) shape = (_visible.Count, BoxColumns);
		if (_boxShape == shape)
		{
			return;
		}

		_boxShape = shape;
		await SafeModuleInvokeVoidAsync(BoxModulePath, "bindBoxSelect", _root, _dotNetRef, BoxOptions(shape.Count, shape.Columns));
	}

	// Turned off halfway, the box is cancelled, and its report arrives after BoxSelect says to ignore it.
	private void PutBackBoxStart()
	{
		if (_boxBefore is null)
		{
			return;
		}

		_selection.Clear();
		_selection.UnionWith(_boxBefore);
		_boxBefore = null;
		RefreshSelection();
		StateHasChanged();
	}

	private static Dictionary<string, object?> BoxOptions(int count, int columns) => new()
	{
		["area"] = ListSelector,
		["box"] = $"{ListSelector} > [data-moka-box]",
		["items"] = "[data-moka-key]",
		["key"] = "data-moka-key",
		["geometry"] = new Dictionary<string, object?>
		{
			["count"] = count,
			["columns"] = columns,
			["index"] = "aria-posinset",
			["base"] = 1
		},
		["method"] = nameof(HandleBoxSelect)
	};
}
