using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A category line chart: one or more named series drawn as polylines over a common y-axis,
///     with grid lines, tick labels and a legend. Pure SVG, so it themes and scales with the page.
/// </summary>
public partial class MokaLineChart : MokaCartesianChartBase
{
	/// <summary>
	///     The series to draw. Null or empty renders the axes alone. A <c>double.NaN</c> in a series is a gap: its
	///     line and area break there and no point is drawn.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaLineSeries>? Series { get; set; }

	/// <summary>Whether a dot marks every value. Defaults to true.</summary>
	[Parameter]
	public bool ShowPoints { get; set; } = true;

	/// <summary>Whether the area between each line and the x-axis is filled. Defaults to false.</summary>
	[Parameter]
	public bool ShowArea { get; set; }

	/// <summary>What the chart shows, for assistive technology.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-linechart";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-linechart--{SizeToKebab(Size)}")
		.AddClass(Class)
		.Build();

	// Geometry leaves the component as strings: every number goes through Num, so an SVG attribute
	// never carries a culture's decimal comma.
	private sealed record PointGeometry(string Cx, string Cy, string Title);
	private sealed record SeriesGeometry(MokaLineSeries Series, string ColorClass, string Line, string? Area,
		IReadOnlyList<PointGeometry> Points);

	private IReadOnlyList<MokaLineSeries> Visible => [.. (Series ?? []).Where(s => s.Values.Count > 0)];

	private int CategoryCount => Visible.Count > 0 ? Visible[0].Values.Count : 0;

	private IReadOnlyList<GridLine> Grid => GridLines(YDomain.Min, YDomain.Max);

	private IReadOnlyList<XLabel> XLabels => CategoryLabels(CategoryCount);

	private IReadOnlyList<SeriesGeometry> Geometry
	{
		get
		{
			List<SeriesGeometry> geometry = [];
			(double min, double max) = YDomain;
			string bottom = Num(PadTop + PlotHeight);

			for (int s = 0; s < Visible.Count; s++)
			{
				MokaLineSeries series = Visible[s];
				IReadOnlyList<(int First, int Last)> runs = MokaChartRuns.Of(series.Values);
				string line = string.Join(' ', runs.Select(run => RunPath(series, run, min, max)));
				string? area = ShowArea && runs.Count > 0
					? string.Join(' ', runs.Select(run => AreaPath(series, run, bottom, min, max)))
					: null;

				List<PointGeometry> points = [];
				if (ShowPoints)
				{
					for (int i = 0; i < series.Values.Count; i++)
					{
						if (double.IsFinite(series.Values[i]))
						{
							points.Add(new PointGeometry(
								Num(PadLeft + X(i, series.Values.Count)),
								Num(Y(series.Values[i], min, max)),
								Tooltip(series, i)));
						}
					}
				}

				geometry.Add(new SeriesGeometry(series, MokaChartPalette.ClassFor(series.Color, s), line, area, points));
			}

			return geometry;
		}
	}

	private (double Min, double Max) YDomain
	{
		get
		{
			List<double> all = [];
			foreach (MokaLineSeries series in Visible)
			{
				all.AddRange(series.Values);
			}

			return all.Count > 0 ? Domain(all) : (0, 1);
		}
	}

	private static string RunPath(MokaLineSeries series, (int First, int Last) run, double min, double max)
	{
		StringBuilder sb = new();
		for (int i = run.First; i <= run.Last; i++)
		{
			sb.Append(i == run.First ? "M " : " L ");
			AppendPoint(sb, series, i, min, max);
		}

		// A lone value between two gaps needs a segment for the round cap to draw.
		if (run.First == run.Last && series.Values.Count > 1)
		{
			sb.Append(" L ");
			AppendPoint(sb, series, run.First, min, max);
		}

		return sb.ToString();
	}

	private static void AppendPoint(StringBuilder sb, MokaLineSeries series, int index, double min, double max)
	{
		sb.Append(Num(PadLeft + X(index, series.Values.Count)));
		sb.Append(' ');
		sb.Append(Num(Y(series.Values[index], min, max)));
	}

	// One stretch of the line closed along the x-axis. A stretch that reaches a side of the plot closes on that side.
	private static string AreaPath(MokaLineSeries series, (int First, int Last) run, string bottom, double min, double max)
	{
		int count = series.Values.Count;
		double left = run.First == 0 ? PadLeft : PadLeft + X(run.First, count);
		double right = run.Last == count - 1 ? PadLeft + PlotWidth : PadLeft + X(run.Last, count);
		return $"M {Num(left)} {bottom} {RunPath(series, run, min, max)[2..]} L {Num(right)} {bottom} Z";
	}

	private string Tooltip(MokaLineSeries series, int index) =>
		Labels is not null && index < Labels.Count
			? $"{Labels[index]} · {series.Name}: {series.Values[index].ToString(TickFormat, CultureInfo.CurrentCulture)}"
			: $"{series.Name}: {series.Values[index].ToString(TickFormat, CultureInfo.CurrentCulture)}";
}
