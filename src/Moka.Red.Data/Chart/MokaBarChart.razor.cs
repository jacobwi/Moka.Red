using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A category bar chart: one or more named series as bars, side by side or stacked, over a
///     common y-axis. Pure SVG, so it themes and scales with the page.
/// </summary>
public partial class MokaBarChart : MokaCartesianChartBase
{
	/// <summary>The series to draw. Null or empty renders the axes alone.</summary>
	[Parameter]
	public IReadOnlyList<MokaBarSeries>? Series { get; set; }

	/// <summary>Whether the series stack onto one bar per category instead of standing side by side.</summary>
	[Parameter]
	public bool Stacked { get; set; }

	/// <summary>What the chart shows, for assistive technology.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-barchart";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-barchart--{SizeToKebab(Size)}")
		.AddClass(Class)
		.Build();

	private sealed record BarGeometry(string Path, string ColorClass, string Title);
	private sealed record SeriesLegend(string Name, string ColorClass);

	private IReadOnlyList<MokaBarSeries> Visible => [.. (Series ?? []).Where(s => s.Values.Count > 0)];

	private int CategoryCount => Visible.Count > 0
		? Visible.Select(s => s.Values.Count).OrderByDescending(c => c).FirstOrDefault()
		: 0;

	private IReadOnlyList<GridLine> Grid => GridLines(YDomain.Min, YDomain.Max);

	private IReadOnlyList<XLabel> XLabels => CategoryLabels(CategoryCount);

	private IReadOnlyList<SeriesLegend> Legend =>
		[.. Visible.Select((series, i) => new SeriesLegend(series.Name, MokaChartPalette.ClassFor(series.Color, i)))];

	// Stacked bars are as tall as the category's totals, so they size the axis, not each value.
	private (double Min, double Max) YDomain
	{
		get
		{
			if (Visible.Count == 0 || CategoryCount == 0)
			{
				return (0, 1);
			}

			if (!Stacked)
			{
				return Domain([.. Visible.SelectMany(s => s.Values)]);
			}

			List<double> totals = [];
			for (int category = 0; category < CategoryCount; category++)
			{
				double positive = 0;
				double negative = 0;
				foreach (MokaBarSeries series in Visible)
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
	}

	private IReadOnlyList<IReadOnlyList<BarGeometry>> Bars
	{
		get
		{
			List<List<BarGeometry>> bars = [];
			if (Visible.Count == 0 || CategoryCount == 0)
			{
				return bars;
			}

			(double min, double max) = YDomain;
			double zero = Y(0, min, max);
			double slot = PlotWidth / CategoryCount;

			for (int category = 0; category < CategoryCount; category++)
			{
				List<BarGeometry> group = [];

				if (Stacked)
				{
					double width = slot * 0.55;
					double x = PadLeft + slot * category + (slot - width) / 2;
					double positiveBase = 0;
					double negativeBase = 0;

					for (int s = 0; s < Visible.Count; s++)
					{
						MokaBarSeries series = Visible[s];
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

						group.Add(new BarGeometry(
							BarPath(x, Y(to, min, max), width, Y(from, min, max)),
							MokaChartPalette.ClassFor(series.Color, s),
							Tooltip(series, category)));
					}
				}
				else
				{
					int drawn = Visible.Count;
					double groupWidth = slot * 0.7;
					double barWidth = groupWidth / drawn;
					double x = PadLeft + slot * category + (slot - groupWidth) / 2;

					for (int s = 0; s < drawn; s++)
					{
						MokaBarSeries series = Visible[s];
						if (category >= series.Values.Count)
						{
							continue;
						}

						double value = series.Values[category];
						group.Add(new BarGeometry(
							BarPath(x + s * barWidth, Y(value, min, max), barWidth, zero),
							MokaChartPalette.ClassFor(series.Color, s),
							Tooltip(series, category)));
					}
				}

				bars.Add(group);
			}

			return bars;
		}
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
