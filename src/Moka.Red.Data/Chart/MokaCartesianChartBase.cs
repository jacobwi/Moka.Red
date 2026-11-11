using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;

namespace Moka.Red.Data.Chart;

/// <summary>
///     Shared plumbing for the charts that draw on x/y axes: the category labels, the y-axis
///     domain and its ticks, and the geometry that maps values into the viewBox. Two orientations share it: columns,
///     with categories, times or numbers across and values up, and rows, with names down and values across.
/// </summary>
public abstract partial class MokaCartesianChartBase : MokaChartBase
{
	// The viewBox every cartesian chart draws in. Fixed coordinates keep the math in one place;
	// the svg scales to the width of its box.
	protected const double ViewWidth = 600;
	protected const double ViewHeight = 260;
	protected const double PadLeft = 40;
	protected const double PadTop = 12;
	protected const double PadRight = 12;
	protected const double PadBottom = 26;
	protected static double PlotWidth => ViewWidth - PadLeft - PadRight;
	protected static double PlotHeight => ViewHeight - PadTop - PadBottom;

	// The axis widths, in the x-axis's own em, that a label can need before it shows. Each chart's stylesheet has a
	// container query for every one, and a label's need is rounded up to the next.
	private static readonly int[] AxisWidths = [5, 6, 8, 10, 12, 15, 19, 24, 30, 38, 48, 60, 75, 95, 120];

	// A label character's width and the space kept between two labels, in em. On the generous side: a label that
	// hides a little early costs less than two that overlap.
	private const double LabelCharWidth = 0.66;
	private const double LabelGap = 1;

	/// <summary>A grid line: the y it runs at, the baseline its tick label sits on, and its text.</summary>
	protected sealed record GridLine(string Y, string BaselineY, string Text);

	/// <summary>A category label along the x-axis.</summary>
	protected sealed record XLabel(string X, string Text);

	/// <summary>
	///     A category label along an x-axis that thins with its own width: its text, where it is centred as a share of
	///     the axis's width, and the class that hides it on an axis too narrow for it, or null when it always shows.
	/// </summary>
	protected sealed record AxisLabel(string Text, double Share, string? WidthClass);

	private sealed record ClassicAxisSpot(string Text, double Share, double HalfWidth);

	private MokaChartRange? _range;
	private MokaChartRange? _lastRangeParameter;

	/// <summary>Category names along the x-axis, one per value.</summary>
	[Parameter]
	public IReadOnlyList<string>? Labels { get; set; }

	/// <summary>Number of y-axis divisions to draw grid lines for. Defaults to 4.</summary>
	[Parameter]
	public int YTicks { get; set; } = 4;

	/// <summary>Fixed floor of the y-axis. Null = computed from the values.</summary>
	[Parameter]
	public double? Min { get; set; }

	/// <summary>Fixed ceiling of the y-axis. Null = computed from the values.</summary>
	[Parameter]
	public double? Max { get; set; }

	/// <summary>Whether category names are drawn along the x-axis. Defaults to true.</summary>
	[Parameter]
	public bool ShowXLabels { get; set; } = true;

	/// <summary>.NET numeric format string for the y-axis tick labels. Defaults to "0.##".</summary>
	[Parameter]
	public string TickFormat { get; set; } = "0.##";

	/// <summary>One time per value. With them, x is proportional to time and the axis steps in clock and calendar units.</summary>
	[Parameter]
	public IReadOnlyList<DateTimeOffset>? Times { get; set; }

	/// <summary>How the x axis is drawn. Null takes the look's.</summary>
	[Parameter]
	public MokaChartAxis? XAxis { get; set; }

	/// <summary>How the y axis is drawn. Null takes the look's.</summary>
	[Parameter]
	public MokaChartAxis? YAxis { get; set; }

	/// <summary>The units the value axis is written and rounded in, a shortcut for that axis's <see cref="MokaChartAxis.Unit" />.</summary>
	[Parameter]
	public MokaChartUnit? Unit { get; set; }

	/// <summary>What category labels that don't fit do, a shortcut for that axis's <see cref="MokaChartAxis.Fit" />.</summary>
	[Parameter]
	public MokaAxisLabelFit? LabelFit { get; set; }

	/// <summary>The lines behind the data. Null takes the look's.</summary>
	[Parameter]
	public MokaChartGrid? Grid { get; set; }

	/// <summary>Where the value axis's numbers sit. Null takes the look's.</summary>
	[Parameter]
	public MokaTickPlacement? TickLabels { get; set; }

	/// <summary>Which side of the plot the value axis is on. Null takes the look's.</summary>
	[Parameter]
	public MokaAxisSide? ValueSide { get; set; }

	/// <summary>
	///     The zone a time axis is aligned and written in. Null uses a registered <see cref="TimeProvider" />'s zone, else
	///     <see cref="TimeZoneInfo.Local" />, which on Blazor Server is the server's.
	/// </summary>
	[Parameter]
	public TimeZoneInfo? TimeZone { get; set; }

	/// <summary>Thresholds, bands, events and windows drawn over the plot.</summary>
	[Parameter]
	public IReadOnlyList<MokaChartAnnotation>? Annotations { get; set; }

	/// <summary>The bottom of a target band.</summary>
	[Parameter]
	public double? TargetLow { get; set; }

	/// <summary>The top of a target band.</summary>
	[Parameter]
	public double? TargetHigh { get; set; }

	/// <summary>The span the chart is zoomed into. Null shows everything.</summary>
	[Parameter]
	public MokaChartRange? Range { get; set; }

	/// <summary>Raised when a brush, the wheel, the range strip or a preset zooms the chart, and with null when it resets.</summary>
	[Parameter]
	public EventCallback<MokaChartRange?> RangeChanged { get; set; }

	/// <summary>How the chart can be zoomed. Null is not at all.</summary>
	[Parameter]
	public MokaChartZoom? Zoom { get; set; }

	/// <summary>The time ranges the toolbar offers, such as the last hour and the last day.</summary>
	[Parameter]
	public IReadOnlyList<MokaChartRangePreset>? RangePresets { get; set; }

	/// <summary>The label of the preset in force.</summary>
	[Parameter]
	public string? RangePreset { get; set; }

	/// <summary>Raised when a preset is picked.</summary>
	[Parameter]
	public EventCallback<string?> RangePresetChanged { get; set; }

	/// <summary>How a long series is reduced before it is drawn. Defaults to <see cref="MokaDownsample.Auto" />.</summary>
	[Parameter]
	public MokaDownsample Downsample { get; set; }

	/// <summary>Whether rows are drawn only as they scroll into view. Null turns it on past 200 rows.</summary>
	[Parameter]
	public bool? Virtualize { get; set; }

	/// <summary>Each row's height in pixels. Null takes the look's.</summary>
	[Parameter]
	public double? RowHeight { get; set; }

	/// <summary>The height rows scroll in, as a CSS length.</summary>
	[Parameter]
	public string? MaxHeight { get; set; }

	/// <summary>
	///     The span the chart shows: the parent's <see cref="Range" /> until a brush, the wheel, the strip or a preset
	///     chooses another, kept here so the next parent render doesn't undo it.
	/// </summary>
	internal override MokaChartRange? CurrentRange => _range;

	/// <summary>Zooms to a span, or back out with null, redraws, and tells the parent. For the toolbar and the range strip.</summary>
	/// <param name="range">The span, or null for everything.</param>
	internal Task SetRangeAsync(MokaChartRange? range)
	{
		if (_range == range)
		{
			return Task.CompletedTask;
		}

		_range = range;
		Redraw();
		return RangeChanged.InvokeAsync(range);
	}

	/// <inheritdoc />
	internal override Task RangeChosenAsync(double from, double to)
	{
		bool time = Times is { Count: > 0 } && XAxis?.Scale is not (MokaAxisScale.Category or MokaAxisScale.Linear or MokaAxisScale.Log);
		MokaChartRange? range = double.IsFinite(from) && double.IsFinite(to) && from != to
			? new MokaChartRange(new MokaChartX(Math.Min(from, to), time), new MokaChartX(Math.Max(from, to), time))
			: null;
		if (_range == range)
		{
			return Task.CompletedTask;
		}

		_range = range;
		Redraw();
		if (RangeChanged.HasDelegate)
		{
			RunDetached(() => RangeChanged.InvokeAsync(range));
		}

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	/// <remarks>
	///     Annotations and range presets are records, compared item by item, so a parent that builds the same list again
	///     every render doesn't draw the chart again. A chart that compares its own data calls this for every other name.
	/// </remarks>
	internal override bool SameValue(string name, object? before, object? after) => name switch
	{
		nameof(Annotations) => ChartSnapshot.SameItems(after as IReadOnlyList<MokaChartAnnotation>, before as IReadOnlyList<MokaChartAnnotation>),
		nameof(RangePresets) => ChartSnapshot.SameItems(after as IReadOnlyList<MokaChartRangePreset>, before as IReadOnlyList<MokaChartRangePreset>),
		_ => base.SameValue(name, before, after)
	};

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		if (Range != _lastRangeParameter)
		{
			_lastRangeParameter = Range;
			_range = Range;
		}

		base.OnParametersSet();
	}

	/// <summary>
	///     The y-axis domain: the fixed <see cref="Min" /> and <see cref="Max" /> when set, otherwise
	///     computed so that values that are all positive start at zero, all negative end at zero,
	///     and a flat range still has room to draw. A value that isn't a finite number doesn't count.
	/// </summary>
	protected (double Min, double Max) Domain(IReadOnlyList<double> values)
	{
		ArgumentNullException.ThrowIfNull(values);

		double dataMin = double.MaxValue;
		double dataMax = double.MinValue;
		foreach (double value in values)
		{
			if (!double.IsFinite(value))
			{
				continue;
			}

			if (value < dataMin)
			{
				dataMin = value;
			}

			if (value > dataMax)
			{
				dataMax = value;
			}
		}

		if (dataMin > dataMax)
		{
			(dataMin, dataMax) = (0, 1);
		}

		double min = Min ?? (dataMin >= 0 ? 0 : dataMin);
		double max = Max ?? (dataMax <= 0 ? 0 : dataMax);

		if (max - min < double.Epsilon)
		{
			min -= 1;
			max += 1;
		}

		return (min, max);
	}

	/// <summary>The tick values from the domain's floor to its ceiling, one per division.</summary>
	protected IReadOnlyList<double> Ticks(double min, double max)
	{
		int divisions = Math.Max(1, YTicks);
		double[] ticks = new double[divisions + 1];
		for (int i = 0; i <= divisions; i++)
		{
			ticks[i] = min + (max - min) * i / divisions;
		}

		return ticks;
	}

	// Labels crowd once there are more than a dozen categories, so every step-th one is drawn.
	protected static int LabelStep(int count) => Math.Max(1, (int)Math.Ceiling(count / 12d));

	/// <summary>The grid lines for a domain, with the tick label each one carries.</summary>
	protected IReadOnlyList<GridLine> GridLines(double min, double max) =>
		[.. Ticks(min, max).Select(tick =>
		{
			double y = Y(tick, min, max);
			return new GridLine(Num(y), Num(y + 4), tick.ToString(TickFormat, CultureInfo.CurrentCulture));
		})];

	/// <summary>The category labels for a count of categories, every step-th one drawn.</summary>
	protected IReadOnlyList<XLabel> CategoryLabels(int count)
	{
		List<XLabel> labels = [];
		for (int i = 0; i < count; i += LabelStep(count))
		{
			string text = Labels is not null && i < Labels.Count
				? Labels[i]
				: i.ToString(CultureInfo.CurrentCulture);
			labels.Add(new XLabel(Num(PadLeft + X(i, count)), text));
		}

		return labels;
	}

	/// <summary>
	///     The category labels for an x-axis that thins them with its own width, without script. A blank label takes no
	///     place on the axis. Among the labels with text, the last always shows, the first outranks the rest, and the
	///     others rank by the largest power of two dividing their position, so evenly filled axes halve as they narrow.
	///     Each label shows once the axis is wide enough for it to sit beside every label that outranks it, judged by the
	///     length of both, and carries that width as <paramref name="classPrefix" /><c>--w</c> and a width in em, which
	///     the stylesheet hides below with a container query. A label that needs a wider axis than the stylesheet knows
	///     isn't returned.
	/// </summary>
	/// <param name="count">How many categories the axis has.</param>
	/// <param name="share">Where a category's label is centred, as a share of the axis's width.</param>
	/// <param name="classPrefix">The label class the width classes are built on, such as <c>moka-linechart-xtick</c>.</param>
	protected IReadOnlyList<AxisLabel> AxisLabels(int count, Func<int, double> share, string classPrefix)
	{
		ArgumentNullException.ThrowIfNull(share);

		List<ClassicAxisSpot> spots = [];
		for (int i = 0; i < count; i++)
		{
			string? text = Labels is not null && i < Labels.Count ? Labels[i] : i.ToString(CultureInfo.CurrentCulture);
			if (!string.IsNullOrWhiteSpace(text))
			{
				spots.Add(new ClassicAxisSpot(text, share(i), text.Length * LabelCharWidth / 2));
			}
		}

		if (spots.Count == 0)
		{
			return [];
		}

		// Everything placed fits at once on the widest axis, so this stays short however long the series is.
		ClassicAxisSpot last = spots[^1];
		List<ClassicAxisSpot> placed = [last];
		int?[] widths = new int?[spots.Count];
		foreach (int position in Enumerable.Range(0, spots.Count - 1).OrderByDescending(AxisRank))
		{
			double needed = 0;
			foreach (ClassicAxisSpot other in placed)
			{
				needed = Math.Max(needed, AxisWidthBetween(spots[position], other));
			}

			if (AxisWidthFor(needed) is { } width)
			{
				widths[position] = width;
				placed.Add(spots[position]);
			}
		}

		List<AxisLabel> labels = [];
		for (int position = 0; position < spots.Count - 1; position++)
		{
			if (widths[position] is { } width)
			{
				labels.Add(new AxisLabel(spots[position].Text, spots[position].Share,
					$"{classPrefix}--w{width.ToString(CultureInfo.InvariantCulture)}"));
			}
		}

		labels.Add(new AxisLabel(last.Text, last.Share, null));
		return labels;
	}

	private static int AxisRank(int position) => position == 0 ? int.MaxValue : position & -position;

	private static double AxisWidthBetween(ClassicAxisSpot a, ClassicAxisSpot b) =>
		(a.HalfWidth + b.HalfWidth + LabelGap) / Math.Abs(a.Share - b.Share);

	private static int? AxisWidthFor(double needed)
	{
		if (!double.IsFinite(needed) || needed < 0)
		{
			return null;
		}

		foreach (int width in AxisWidths)
		{
			if (width >= needed)
			{
				return width;
			}
		}

		return null;
	}

	protected static double X(int index, int count) =>
		count <= 1 ? PlotWidth / 2 : (double)index / (count - 1) * PlotWidth;

	protected static double Y(double value, double min, double max)
	{
		double proportion = Math.Clamp((value - min) / (max - min), 0, 1);
		return PadTop + (1 - proportion) * PlotHeight;
	}

	// Geometry leaves the components as strings, so an SVG attribute never carries a culture's
	// decimal comma.
	protected static string Num(double value) => value.ToString("F2", CultureInfo.InvariantCulture);
}
