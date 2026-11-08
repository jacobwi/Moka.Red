using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A category bar chart: one or more named series as bars, side by side or stacked, over a
///     common y-axis. The plot fills its box's width at the box's height, the tick labels are text beside and
///     under it, and the category labels thin out as the chart narrows.
/// </summary>
public partial class MokaBarChart : MokaCartesianChartBase
{
	// The plot's own svg covers the viewBox from here right and from the top down to the x-axis, and stretches to fill
	// its cell. The tick labels are HTML beside and under it, so text never stretches with it.
	private const double PlotLeft = PadLeft;
	private const double PlotBoxWidth = ViewWidth - PlotLeft;
	private const double PlotBoxHeight = ViewHeight - PadBottom;

	private IReadOnlyList<MokaBarSeries> _visible = [];
	private int _categoryCount;
	private IReadOnlyList<YTick> _grid = [];
	private string _widestTick = string.Empty;
	private IReadOnlyList<XTick> _xLabels = [];
	private IReadOnlyList<BarGeometry> _bars = [];
	private IReadOnlyList<SeriesLegend> _legend = [];

	/// <summary>The series to draw. Null or empty renders the axes alone.</summary>
	[Parameter]
	public IReadOnlyList<MokaBarSeries>? Series { get; set; }

	/// <summary>Whether the series stack onto one bar per category instead of standing side by side.</summary>
	[Parameter]
	public bool Stacked { get; set; }

	/// <summary>What the chart shows, for assistive technology.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>
	///     A fixed height for the plot and its labels, as a CSS length such as "180px", whatever the width. Without it
	///     the chart keeps a 600 by 260 proportion, no taller than its <c>Size</c> allows. Either way the plot fills the
	///     whole width.
	/// </summary>
	[Parameter]
	public string? Height { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-barchart";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-barchart--{SizeToKebab(Size)}")
		.AddClass(Class)
		.Build();

	private static string PlotViewBox => $"{Num(PlotLeft)} 0 {Num(PlotBoxWidth)} {Num(PlotBoxHeight)}";

	private string? FrameStyle => new StyleBuilder()
		.AddStyle("height", Height)
		.Build();

	private string FrameClass => new CssBuilder("moka-barchart-frame")
		.AddClass("moka-barchart-frame--fixed", FrameStyle is not null)
		.AddClass("moka-barchart-frame--no-x", !ShowXLabels)
		.Build();

	private sealed record BarGeometry(string Path, string ColorClass, string Title);
	private sealed record SeriesLegend(string Name, string ColorClass);

	/// <summary>A grid line's y in the viewBox, and its label placed at the same height, as a share of the plot's.</summary>
	private sealed record YTick(string Y, string? Style, string Text);

	/// <summary>A category label placed under its slot, as a share of the plot's width, with its classes.</summary>
	private sealed record XTick(string? Style, string Text, string Class);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		_visible = [.. (Series ?? []).Where(s => s.Values.Count > 0)];
		_categoryCount = _visible.Count > 0 ? _visible.Max(s => s.Values.Count) : 0;

		(double min, double max) = YDomain();
		IReadOnlyList<double> ticks = Ticks(min, max);
		string[] tickTexts = [.. ticks.Select(tick => tick.ToString(TickFormat, CultureInfo.CurrentCulture))];
		_widestTick = tickTexts.Length == 0 ? string.Empty : tickTexts.MaxBy(t => t.Length)!;
		_grid = _categoryCount > 0
			? [.. ticks.Select((tick, i) =>
			{
				double y = Y(tick, min, max);
				return new YTick(Num(y), Placed("top", y / PlotBoxHeight), tickTexts[i]);
			})]
			: [];

		double slot = _categoryCount > 0 ? PlotWidth / _categoryCount : 0;
		_xLabels =
		[
			.. AxisLabels(_categoryCount, i => slot * (i + 0.5) / PlotBoxWidth, "moka-barchart-xtick")
				.Select(label => new XTick(
					Placed("left", label.Share),
					label.Text,
					new CssBuilder("moka-barchart-tick moka-barchart-xtick").AddClass(label.WidthClass).Build()))
		];
		_bars = BarsFor(min, max);
		_legend = [.. _visible.Select((series, i) => new SeriesLegend(series.Name, MokaChartPalette.ClassFor(series.Color, i)))];
	}

	// A label's place as a percentage of the plot box, which is what the stretched plot keeps.
	private static string? Placed(string side, double share) => new StyleBuilder()
		.AddStyle(side, FormattableString.Invariant($"{share * 100:0.###}%"))
		.Build();

	// Stacked bars are as tall as the category's totals, so they size the axis, not each value.
	private (double Min, double Max) YDomain()
	{
		if (_visible.Count == 0 || _categoryCount == 0)
		{
			return (0, 1);
		}

		if (!Stacked)
		{
			return Domain([.. _visible.SelectMany(s => s.Values)]);
		}

		List<double> totals = [];
		for (int category = 0; category < _categoryCount; category++)
		{
			double positive = 0;
			double negative = 0;
			foreach (MokaBarSeries series in _visible)
			{
				if (category >= series.Values.Count)
				{
					continue;
				}

				double value = series.Values[category];
				if (value >= 0)
				{
					positive += value;
				}
				else
				{
					negative += value;
				}
			}

			totals.Add(positive);
			totals.Add(negative);
		}

		return Domain(totals);
	}

	private List<BarGeometry> BarsFor(double min, double max)
	{
		List<BarGeometry> bars = [];
		if (_visible.Count == 0 || _categoryCount == 0)
		{
			return bars;
		}

		double zero = Y(0, min, max);
		double slot = PlotWidth / _categoryCount;

		for (int category = 0; category < _categoryCount; category++)
		{
			if (Stacked)
			{
				double width = slot * 0.55;
				double x = PadLeft + slot * category + (slot - width) / 2;
				double positiveBase = 0;
				double negativeBase = 0;

				for (int s = 0; s < _visible.Count; s++)
				{
					MokaBarSeries series = _visible[s];
					if (category >= series.Values.Count)
					{
						continue;
					}

					double value = series.Values[category];
					if (value == 0)
					{
						continue;
					}

					double from = value >= 0 ? positiveBase : negativeBase;
					double to = from + value;
					if (value >= 0)
					{
						positiveBase = to;
					}
					else
					{
						negativeBase = to;
					}

					bars.Add(new BarGeometry(
						BarPath(x, Y(to, min, max), width, Y(from, min, max)),
						MokaChartPalette.ClassFor(series.Color, s),
						Tooltip(series, category)));
				}
			}
			else
			{
				int drawn = _visible.Count;
				double groupWidth = slot * 0.7;
				double barWidth = groupWidth / drawn;
				double x = PadLeft + slot * category + (slot - groupWidth) / 2;

				for (int s = 0; s < drawn; s++)
				{
					MokaBarSeries series = _visible[s];
					if (category >= series.Values.Count)
					{
						continue;
					}

					double value = series.Values[category];
					bars.Add(new BarGeometry(
						BarPath(x + s * barWidth, Y(value, min, max), barWidth, zero),
						MokaChartPalette.ClassFor(series.Color, s),
						Tooltip(series, category)));
				}
			}
		}

		return bars;
	}

	// A bar with rounded ends on the side it points to: the top for positive values, the bottom for
	// negative ones, and the plain edge against the axis either way.
	private static string BarPath(double x, double top, double width, double bottom)
	{
		double height = Math.Abs(bottom - top);
		double radius = Math.Clamp(Math.Min(3, width / 2), 0, height);
		string right = Num(x + width);

		if (bottom >= top)
		{
			return string.Create(CultureInfo.InvariantCulture,
				$"M {Num(x)} {Num(bottom)} L {Num(x)} {Num(top + radius)} Q {Num(x)} {Num(top)} {Num(x + radius)} {Num(top)} " +
				$"L {Num(x + width - radius)} {Num(top)} Q {right} {Num(top)} {right} {Num(top + radius)} L {right} {Num(bottom)} Z");
		}

		return string.Create(CultureInfo.InvariantCulture,
			$"M {Num(x)} {Num(top)} L {Num(x)} {Num(bottom - radius)} Q {Num(x)} {Num(bottom)} {Num(x + radius)} {Num(bottom)} " +
			$"L {Num(x + width - radius)} {Num(bottom)} Q {right} {Num(bottom)} {right} {Num(bottom - radius)} L {right} {Num(top)} Z");
	}

	private string Tooltip(MokaBarSeries series, int index) =>
		Labels is not null && index < Labels.Count
			? $"{Labels[index]} · {series.Name}: {series.Values[index].ToString(TickFormat, CultureInfo.CurrentCulture)}"
			: $"{series.Name}: {series.Values[index].ToString(TickFormat, CultureInfo.CurrentCulture)}";
}
