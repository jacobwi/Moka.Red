using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;

namespace Moka.Red.Data.Chart;

/// <summary>
///     Shared plumbing for the charts that draw on x/y axes: the category labels, the y-axis
///     domain and its ticks, and the geometry that maps values into the viewBox.
/// </summary>
public abstract class MokaCartesianChartBase : MokaVisualComponentBase
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

	/// <summary>Whether the series names are listed under the chart. Defaults to true.</summary>
	[Parameter]
	public bool ShowLegend { get; set; } = true;

	/// <summary>Whether category names are drawn along the x-axis. Defaults to true.</summary>
	[Parameter]
	public bool ShowXLabels { get; set; } = true;

	/// <summary>.NET numeric format string for the y-axis tick labels. Defaults to "0.##".</summary>
	[Parameter]
	public string TickFormat { get; set; } = "0.##";

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
	///     The category labels for an x-axis that thins them with its own width, without script. Labels are taken every
	///     1st, 2nd, 4th, 8th and so on from the first, and each carries the narrowest axis it fits on as
	///     <paramref name="classPrefix" /><c>--w</c> and a width in em, which the stylesheet hides below with a container
	///     query. The last label always shows, and the one before it hides while the two would overlap. A label that
	///     needs a wider axis than the stylesheet knows isn't returned.
	/// </summary>
	/// <param name="count">How many categories the axis has.</param>
	/// <param name="share">Where a category's label is centred, as a share of the axis's width. Evenly spaced.</param>
	/// <param name="classPrefix">The label class the width classes are built on, such as <c>moka-linechart-xtick</c>.</param>
	protected IReadOnlyList<AxisLabel> AxisLabels(int count, Func<int, double> share, string classPrefix)
	{
		ArgumentNullException.ThrowIfNull(share);
		if (count <= 0)
		{
			return [];
		}

		string[] texts = new string[count];
		int longest = 1;
		for (int i = 0; i < count; i++)
		{
			texts[i] = Labels is not null && i < Labels.Count
				? Labels[i] ?? string.Empty
				: i.ToString(CultureInfo.CurrentCulture);
			longest = Math.Max(longest, texts[i].Length);
		}

		double lastShare = share(count - 1);
		if (count == 1)
		{
			return [new AxisLabel(texts[0], lastShare, null)];
		}

		double needed = longest * LabelCharWidth + LabelGap;
		double pitch = share(1) - share(0);
		List<AxisLabel> labels = [];
		for (int i = 0; i < count - 1; i++)
		{
			// i & -i is the largest power of two that divides i: how sparse the labels are when this one is the
			// closest-packed that shows. The first label is in every set.
			double forItsSet = i == 0 ? 0 : needed / (pitch * (i & -i));
			double besideTheLast = needed / (lastShare - share(i));
			if (AxisWidthFor(Math.Max(forItsSet, besideTheLast)) is { } width)
			{
				labels.Add(new AxisLabel(texts[i], share(i),
					$"{classPrefix}--w{width.ToString(CultureInfo.InvariantCulture)}"));
			}
		}

		labels.Add(new AxisLabel(texts[^1], lastShare, null));
		return labels;
	}

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
