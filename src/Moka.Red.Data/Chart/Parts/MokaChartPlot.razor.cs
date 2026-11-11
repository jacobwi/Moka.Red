using System.Collections;
using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     Shared chart chrome: the axes, grid, labels and annotations around a cartesian chart's marks, or the value axis
///     and rows of a chart drawn in rows. The chart passes itself and its marks; the plot reads the axes from the chart.
///     Labels that don't fit stagger, rotate, wrap, cut or thin by container queries on the axis's own width, so nothing
///     is measured by script. Not meant to be used on its own, and draws nothing without a chart.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed partial class MokaChartPlot : ComponentBase
{
	private const double LabelFlipShare = 0.7;

	private RowIndexList? _rowIndices;

	/// <summary>The chart this plot belongs to.</summary>
	[Parameter]
	public MokaCartesianChartBase? Chart { get; set; }

	/// <summary>The panel it draws, for charts drawn as small multiples.</summary>
	[Parameter]
	public int Panel { get; set; }

	/// <summary>The marks of a column plot, authored by the chart so its stylesheet styles them.</summary>
	[Parameter]
	public RenderFragment? Marks { get; set; }

	/// <summary>A row's track, by index. With it the plot draws rows instead of columns.</summary>
	[Parameter]
	public RenderFragment<int>? Row { get; set; }

	/// <summary>How many rows a row plot draws. Zero or less takes the chart's count.</summary>
	[Parameter]
	public int RowCount { get; set; }

	/// <summary>Draws a row's name in place of its text, which still names it in a tooltip.</summary>
	[Parameter]
	public RenderFragment<int>? RowName { get; set; }

	/// <summary>Draws what a row reads at its end, in place of the chart's figure text.</summary>
	[Parameter]
	public RenderFragment<int>? RowEnd { get; set; }

	/// <summary>
	///     Drawn down the rows' track, over the grid and under the rows, such as a gantt chart's line at now. Positioned in
	///     percent of the track's width; drawn again only when the geometry moves.
	/// </summary>
	[Parameter]
	public RenderFragment? RowsOverlay { get; set; }

	/// <summary>A strip over a column plot, such as the readout of the point under the pointer.</summary>
	[Parameter]
	public RenderFragment? Readout { get; set; }

	/// <summary>A column beside the plot on the far side from the value axis, for names or values at the lines' ends.</summary>
	[Parameter]
	public RenderFragment? Gutter { get; set; }

	private bool HasFixedHeight => Chart?.HeightValue is not null;

	// Handlers here change the chart, which redraws itself; the plot never renders for an event.
	Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);

	private string ColumnClass(ColumnPlotModel model) => new CssBuilder("moka-chart-plot")
		.AddClass("moka-chart-plot--end", model.ValueSide == MokaAxisSide.End)
		.AddClass(PlacementClass(model.TickLabels))
		.AddClass("moka-chart-plot--height", HasFixedHeight)
		.AddClass("moka-chart-plot--scroll", model.ScrollMinEm is not null)
		.AddClass("moka-chart-plot--no-y", !model.ShowValueAxis)
		.AddClass(GridClass(model.Grid))
		.Build();

	private string? GridStyle(ColumnPlotModel model) => new StyleBuilder()
		.AddStyle("height", Chart?.HeightValue)
		.AddStyle("--_plot-max", HasFixedHeight ? null : Chart!.PlotMaxHeight)
		.Build();

	private static string PlacementClass(MokaTickPlacement placement) => placement switch
	{
		MokaTickPlacement.Inside => "moka-chart-plot--ticks-inside",
		MokaTickPlacement.Auto => "moka-chart-plot--ticks-auto",
		_ => "moka-chart-plot--ticks-outside"
	};

	private static string GridClass(MokaChartGrid grid) => grid switch
	{
		MokaChartGrid.Hairlines => "moka-chart-plot--grid-hairlines",
		MokaChartGrid.Baseline => "moka-chart-plot--grid-baseline",
		MokaChartGrid.Dotted => "moka-chart-plot--grid-dotted",
		MokaChartGrid.None => "moka-chart-plot--grid-none",
		_ => "moka-chart-plot--grid-lines"
	};

	private static string ValueTickClass(PlotTick tick) => new CssBuilder("moka-chart-plot__ytick")
		.AddClass("moka-chart-plot__ytick--minor", tick.Minor)
		.AddClass(tick.HideClass)
		.Build();

	// A line far along the track labels itself on its left, so the label stays inside the plot.
	private static string VThresholdClass(PlotThreshold threshold) => new CssBuilder("moka-chart-plot__vthreshold")
		.AddClass(threshold.ToneClass)
		.AddClass("moka-chart-plot__vthreshold--start", threshold.Share > LabelFlipShare)
		.Build();

	private static string GridLineClass(PlotTick tick) => new CssBuilder("moka-chart-plot__gline")
		.AddClass("moka-chart-plot__gline--minor", tick.Minor)
		.Build();

	private static RenderFragment TickContent(RenderFragment<MokaAxisTick>? template, MokaAxisTick tick) =>
		template is not null ? template(tick) : builder => builder.AddContent(0, tick.Text);

	// A place as a percentage of the plot box, which is what stretched marks keep.
	private static string? Share(string name, double share) => new StyleBuilder()
		.AddStyle(name, double.IsFinite(share) ? ChartAxisLayout.Percent(share) : null)
		.Build();

	// Where a span starts and how much of the axis it covers.
	private static string? Span(double from, double to) => new StyleBuilder()
		.AddStyle("--_s", double.IsFinite(from) ? ChartAxisLayout.Percent(from) : null)
		.AddStyle("--_w", double.IsFinite(to) && double.IsFinite(from) ? ChartAxisLayout.Percent(Math.Max(0, to - from)) : null)
		.Build();

	private static string? ScrollStyle(double leastEm) => new StyleBuilder()
		.AddStyle("--_scroll-min", ChartAxisLayout.Em(leastEm))
		.Build();

	private string RowsClass(RowPlotModel model) => new CssBuilder("moka-chart-plot moka-chart-plot--rows")
		.AddClass("moka-chart-plot--no-figure", RowEnd is null && !model.HasFigure)
		.AddClass("moka-chart-plot--virtual", model.Virtualize)
		.AddClass("moka-chart-plot--scroller", CssValues.IsLengthExpression(model.MaxHeight))
		.AddClass(model.NamesAboveBelow is { } width
			? $"moka-chart-plot--above-w{width.ToString(CultureInfo.InvariantCulture)}"
			: null)
		.AddClass(GridClass(model.Grid))
		.Build();

	// Rows read their height from one number, the one Virtualize is given as ItemSize, so the two always agree.
	private static string? RowsStyle(RowPlotModel model) => new StyleBuilder()
		.AddStyle("--_row-h", $"{model.RowHeight.ToString("0.##", CultureInfo.InvariantCulture)}px")
		.AddStyle("--_name-w", model.Virtualize
			? $"min(40%, {ChartAxisLayout.Em(model.NameEm + (ChartAxisLayout.GapEm / 2))})"
			: null)
		.AddStyle("--_fig-w", model.Virtualize ? ChartAxisLayout.Em(model.FigureEm) : null)
		.Build();

	private static string? ScrollerStyle(RowPlotModel model) => new StyleBuilder()
		.AddStyle("max-height", CssValues.IsLengthExpression(model.MaxHeight) ? CssValues.WithPixels(model.MaxHeight.Trim()) : null)
		.Build();

	// Virtualize takes a collection, and a list of row numbers that holds none costs nothing however long it is.
	private RowIndexList RowIndices(int count)
	{
		if (_rowIndices is null || _rowIndices.Count != count)
		{
			_rowIndices = new RowIndexList(count);
		}

		return _rowIndices;
	}

	/// <summary>The numbers 0 to <c>count - 1</c> as a read-only list, without storing them.</summary>
	private sealed class RowIndexList(int count) : IList<int>, IReadOnlyList<int>
	{
		public int Count { get; } = Math.Max(0, count);

		public bool IsReadOnly => true;

		public int this[int index]
		{
			get => (uint)index < (uint)Count ? index : throw new ArgumentOutOfRangeException(nameof(index));
			set => throw new NotSupportedException();
		}

		public IEnumerator<int> GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
			{
				yield return i;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public bool Contains(int item) => (uint)item < (uint)Count;

		public int IndexOf(int item) => Contains(item) ? item : -1;

		public void CopyTo(int[] array, int arrayIndex)
		{
			ArgumentNullException.ThrowIfNull(array);
			for (int i = 0; i < Count; i++)
			{
				array[arrayIndex + i] = i;
			}
		}

		public void Add(int item) => throw new NotSupportedException();

		public void Clear() => throw new NotSupportedException();

		public void Insert(int index, int item) => throw new NotSupportedException();

		public bool Remove(int item) => throw new NotSupportedException();

		public void RemoveAt(int index) => throw new NotSupportedException();
	}
}
