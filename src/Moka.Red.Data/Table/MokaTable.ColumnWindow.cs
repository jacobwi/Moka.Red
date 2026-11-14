using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Moka.Red.Data.Table;

// Column virtualization: for a table of many columns only those in view draw their cells. The header keeps every heading,
// a row of cheap cells whose widths moka-table.js measures once and again when they change; scrolling sideways sends .NET
// the columns in view only when they change, and the rows draw those, the pinned columns, and one empty cell spanning
// each run of columns out of view. Rows keep their columns lined up because the table lays its columns out from the
// headings' widths alone.
public partial class MokaTable<TItem>
{
	// The columns drawn before the script has measured: about a wide screen's worth.
	private const int FirstWindowColumns = 12;

	private (int First, int Last)? _columnWindow;
	private bool _columnWatchBound;
	private ColumnGap? _gapBefore;
	private ColumnGap? _gapAfter;

	/// <summary>
	///     Draws only the cells of the columns in view, for tables of about 50 columns or more: scrolling sideways draws the
	///     columns that come into view. Pinned columns always draw. Default false. The table lays its columns out at fixed
	///     widths, each column's <see cref="MokaColumn{TItem}.Width" /> or 10rem, and it needs to scroll sideways in its own
	///     box, as it does with <see cref="Responsive" /> on (the default) or a <see cref="MinWidth" />. A grouped table's
	///     headings draw the totals of the columns in view.
	/// </summary>
	[Parameter]
	public bool VirtualizeColumns { get; set; }

	private const string WindowColumnWidth = "10rem";

	/// <summary>
	///     Called from JS when the columns in view change as the table scrolls sideways or changes size, with a few beyond
	///     each edge.
	/// </summary>
	/// <param name="first">The <c>data-col-index</c> of the first column to draw.</param>
	/// <param name="last">The <c>data-col-index</c> of the last.</param>
	[JSInvokable]
	public void OnColumnWindow(int first, int last)
	{
		if (!VirtualizeColumns || _columnWindow == (first, last))
		{
			return;
		}

		_columnWindow = (first, last);
		StateHasChanged();
	}

	// The columns a row draws: every one, or under column virtualization the pinned ones and those in the window, with a
	// gap standing in for each run left out. The list it is given comes back when nothing is left out.
	private List<(MokaColumn<TItem> Col, int ColIdx)> Windowed(List<(MokaColumn<TItem> Col, int ColIdx)> columns)
	{
		if (!VirtualizeColumns || columns.Count == 0)
		{
			return columns;
		}

		int firstFree = _lastStartPin + 1;
		int lastFree = _firstEndPin >= 0 ? _firstEndPin - 1 : VisibleColumnCount - 1;
		(int first, int last) = _columnWindow ?? (firstFree, firstFree + FirstWindowColumns - 1);
		first = Math.Clamp(first, firstFree, Math.Max(firstFree, lastFree));
		last = Math.Clamp(last, first, Math.Max(first, lastFree));
		if (first == firstFree && last >= lastFree)
		{
			return columns;
		}

		_gapBefore ??= new ColumnGap();
		_gapAfter ??= new ColumnGap();
		_gapBefore.Span = first - firstFree;
		_gapAfter.Span = lastFree - last;

		List<(MokaColumn<TItem> Col, int ColIdx)> drawn = new(columns.Count);
		foreach ((MokaColumn<TItem> col, int position) in columns)
		{
			if (position < 0 || position < firstFree || position > lastFree || (position >= first && position <= last))
			{
				drawn.Add((col, position));
			}
			else if (position == firstFree && _gapBefore.Span > 0)
			{
				drawn.Add((_gapBefore, position));
			}
			else if (position == last + 1 && _gapAfter.Span > 0)
			{
				drawn.Add((_gapAfter, position));
			}
		}

		return drawn;
	}

	// While columns come and go, a row's cells shift places among their siblings, and Blazor would hand a cell element,
	// with focus or an open editor in it, to another column. Keyed by its column, a cell keeps its element. A null key is
	// no key, so a table that draws every column pays nothing for it.
	private MokaColumn<TItem>? WindowKey(MokaColumn<TItem> col) => VirtualizeColumns ? col : null;

	private async Task SyncColumnWatchAsync()
	{
		bool wanted = VirtualizeColumns && DrawHeader;
		if (wanted == _columnWatchBound)
		{
			return;
		}

		_columnWatchBound = wanted;
		if (!wanted)
		{
			_columnWindow = null;
		}

		_dotNetRef ??= DotNetObjectReference.Create(this);
		await InvokeTableJsAsync(wanted ? "watchColumns" : "unwatchColumns", _wrapperRef, _dotNetRef);
	}

	/// <summary>A run of columns out of view, drawn as one empty cell spanning them.</summary>
	private sealed class ColumnGap : MokaColumn<TItem>
	{
		public ColumnGap() => Resizable = false;

		/// <summary>How many columns it spans.</summary>
		public int Span { get; set; }
	}
}
