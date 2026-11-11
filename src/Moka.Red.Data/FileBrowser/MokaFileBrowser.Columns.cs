using Microsoft.JSInterop;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.FileBrowser;

public partial class MokaFileBrowser<TItem>
{
	private readonly Dictionary<string, double> _columnWidths = new(StringComparer.Ordinal);

	private IReadOnlyDictionary<string, double>? _lastColumnWidths;

	private string ColumnMinimum => FormattableString.Invariant($"{MinColumnWidth}");

	// On the element above the head and the list, so a drag moves one variable and not every cell of the column.
	private string? ColumnWidthVariables
	{
		get
		{
			if (!ResizableColumns || _columnWidths.Count == 0)
			{
				return null;
			}

			StyleBuilder style = new();
			for (int i = 0; i < _columns.Count; i++)
			{
				if (_columnWidths.TryGetValue(_columns[i].Id, out double width))
				{
					style.AddStyle(ColumnVariable(i), FormattableString.Invariant($"{Math.Max(MinColumnWidth, width)}px"));
				}
			}

			return style.Build();
		}
	}

	/// <summary>A column dragged to a new width, from the browser.</summary>
	/// <param name="column">The column's id.</param>
	/// <param name="width">Its width in CSS pixels.</param>
	/// <returns>A task that ends when the app has heard.</returns>
	[JSInvokable]
	public async Task HandleColumnResized(string column, double width)
	{
		ArgumentNullException.ThrowIfNull(column);
		if (!ResizableColumns || !double.IsFinite(width) || Column(column) is null)
		{
			return;
		}

		_columnWidths[column] = Math.Max(MinColumnWidth, width);
		await RaiseColumnWidthsAsync();
		StateHasChanged();
	}

	/// <summary>A column given its own width back, from a double click on its edge.</summary>
	/// <param name="column">The column's id.</param>
	/// <returns>A task that ends when the app has heard.</returns>
	[JSInvokable]
	public async Task HandleColumnReset(string column)
	{
		ArgumentNullException.ThrowIfNull(column);
		if (_columnWidths.Remove(column))
		{
			await RaiseColumnWidthsAsync();
			StateHasChanged();
		}
	}

	private static string ColumnVariable(int index) => FormattableString.Invariant($"--_fb-col-{index}");

	private void ApplyColumnWidthsParameter()
	{
		if (ReferenceEquals(ColumnWidths, _lastColumnWidths))
		{
			return;
		}

		_lastColumnWidths = ColumnWidths;
		_columnWidths.Clear();
		if (ColumnWidths is null)
		{
			return;
		}

		foreach ((string column, double width) in ColumnWidths)
		{
			if (double.IsFinite(width))
			{
				_columnWidths[column] = width;
			}
		}
	}

	private Task RaiseColumnWidthsAsync() =>
		ColumnWidthsChanged.InvokeAsync(new Dictionary<string, double>(_columnWidths, StringComparer.Ordinal));

	// The line holding the rename keeps its element wherever it moves to, because Blazor can't move a keyed tile between
	// parents. Every tile is on one line, so the keys never repeat. A key of its own type, so a line can never be taken
	// for the row with the same key when a view swaps one for the other in the same place.
	private LineKey TileLineKey(IReadOnlyList<MokaFileRow<TItem>> tiles)
	{
		foreach (MokaFileRow<TItem> tile in tiles)
		{
			if (IsEditing(tile))
			{
				return new LineKey(tile.Key);
			}
		}

		return new LineKey(tiles[0].Key);
	}

	private readonly record struct LineKey(string Key);
}
