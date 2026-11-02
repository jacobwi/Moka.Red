using System.Globalization;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Data.Chart;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Components;

/// <summary>
///     The chart family: the geometry they draw, the palette they paint with, and the axes and
///     legends around them.
/// </summary>
public class MokaChartTests : BunitContext
{
	[Fact]
	public void Sparkline_DrawsItsValuesAsAPath()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [1d, 4, 2, 5]));

		string path = cut.Find(".moka-sparkline-line").GetAttribute("d") ?? "";
		Assert.StartsWith("M ", path, StringComparison.Ordinal);
		Assert.Equal(4 - 1, CountSegments(path));
	}

	[Fact]
	public void Sparkline_WithTooFewValues_RendersNoLine()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [1d]));

		Assert.Empty(cut.FindAll(".moka-sparkline-svg"));
	}

	[Fact]
	public void Sparkline_AnExplicitColor_WinsOverThePaletteDefault()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [1d, 2])
			.Add(x => x.Color, MokaColor.Success));

		Assert.Contains("moka-chart-color-success", cut.Find(".moka-sparkline").ClassName,
			StringComparison.Ordinal);
	}

	// "12,50" is not a number in SVG, so the coordinates go out invariant no matter the culture.
	[Fact]
	public void Sparkline_CoordinatesStayInvariant_UnderADecimalCommaCulture()
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("de-DE");
		try
		{
			IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
				.Add(x => x.Values, [1d, 2.5, 3]));

			string path = cut.Find(".moka-sparkline-line").GetAttribute("d") ?? "";
			Assert.NotEmpty(path);
			Assert.DoesNotContain(',', path);
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	[Fact]
	public void LineChart_DrawsOneLinePerSeries_OverOneGrid()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Series,
			[
				new MokaLineSeries { Name = "Revenue", Values = [10, 20, 15] },
				new MokaLineSeries { Name = "Costs", Values = [5, 8, 6] }
			]));

		Assert.Equal(2, cut.FindAll(".moka-linechart-line").Count);
		// YTicks divisions draw YTicks + 1 grid lines
		Assert.Equal(5, cut.FindAll(".moka-linechart-grid").Count);
	}

	[Fact]
	public void LineChart_LegendListsTheSeries_ByDefault()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Series, [new MokaLineSeries { Name = "Revenue", Values = [10, 20] }]));

		Assert.Contains("Revenue", cut.Find(".moka-linechart-legend").TextContent, StringComparison.Ordinal);

		IRenderedComponent<MokaLineChart> hidden = Render<MokaLineChart>(p => p
			.Add(x => x.Series, [new MokaLineSeries { Name = "Revenue", Values = [10, 20] }])
			.Add(x => x.ShowLegend, false));

		Assert.Empty(hidden.FindAll(".moka-linechart-legend"));
	}

	[Fact]
	public void LineChart_PointTooltipsNameTheCategoryAndTheValue()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, ["Mon", "Tue"])
			.Add(x => x.Series, [new MokaLineSeries { Name = "Revenue", Values = [10, 20] }]));

		Assert.Contains("Mon · Revenue: 10", cut.Find(".moka-linechart-point title").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void LineChart_WithoutSeries_DrawsNoLines()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>();

		Assert.Empty(cut.FindAll(".moka-linechart-line"));
		Assert.Empty(cut.FindAll(".moka-linechart-grid"));
	}

	[Fact]
	public void BarChart_Grouped_DrawsOneBarPerSeriesPerCategory()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Series,
			[
				new MokaBarSeries { Name = "A", Values = [1, 2, 3] },
				new MokaBarSeries { Name = "B", Values = [4, 5, 6] }
			]));

		Assert.Equal(6, cut.FindAll(".moka-barchart-bar").Count);
	}

	// Stacked columns are one rect per series, piled on the same spot - so the two series meet in
	// six rects, the same count as grouped, but over three positions.
	[Fact]
	public void BarChart_Stacked_PilesTheSeriesOntoOneColumnPerCategory()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Stacked, true)
			.Add(x => x.Series,
			[
				new MokaBarSeries { Name = "A", Values = [1, 2, 3] },
				new MokaBarSeries { Name = "B", Values = [4, 5, 6] }
			]));

		string[] paths = cut.FindAll(".moka-barchart-bar").Select(b => b.GetAttribute("d") ?? "").ToArray();
		Assert.Equal(6, paths.Length);
		// The first category's two segments start at the same x, piled on one column
		Assert.Equal(paths[0].Split(' ')[1], paths[1].Split(' ')[1]);
	}

	// The bar tops round toward their value, which the Q curves of the path draw.
	[Fact]
	public void BarChart_BarsRoundTheEnds()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Series, [new MokaBarSeries { Name = "A", Values = [4] }]));

		Assert.Contains("Q", cut.Find(".moka-barchart-bar").GetAttribute("d"), StringComparison.Ordinal);
	}

	[Fact]
	public void BarChart_NegativeAndPositiveBars_ShareOneAxis()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Series, [new MokaBarSeries { Name = "A", Values = [-4, 4] }]));

		Assert.Equal(2, cut.FindAll(".moka-barchart-bar").Count);
	}

	[Fact]
	public void DonutChart_DrawsOneArcPerSegment_WithPercentages()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Segments,
			[
				new MokaDonutSegment { Name = "Direct", Value = 50 },
				new MokaDonutSegment { Name = "Referral", Value = 50 }
			]));

		Assert.Equal(2, cut.FindAll(".moka-donutchart-segment").Count);
		Assert.Contains("50%", cut.Find(".moka-donutchart-legend").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void DonutChart_WithoutSegments_ShowsOnlyTheRing()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>();

		Assert.Single(cut.FindAll(".moka-donutchart-track"));
		Assert.Empty(cut.FindAll(".moka-donutchart-segment"));
		Assert.Empty(cut.FindAll(".moka-donutchart-legend"));
	}

	[Fact]
	public void DonutChart_ChildContentSitsInTheRing()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Segments, [new MokaDonutSegment { Name = "Direct", Value = 1 }])
			.AddChildContent("1,024"));

		Assert.Contains("1,024", cut.Find(".moka-donutchart-center").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void SeriesWithoutColors_TakeThePaletteInRotation()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Series,
			[
				new MokaLineSeries { Name = "First", Values = [1, 2] },
				new MokaLineSeries { Name = "Second", Values = [2, 1] }
			]));

		string[] classes = cut.FindAll("g")
			.Where(g => g.ClassList.Any(c => c.StartsWith("moka-chart-color-", StringComparison.Ordinal)))
			.Select(g => g.ClassList.First(c => c.StartsWith("moka-chart-color-", StringComparison.Ordinal)))
			.ToArray();
		Assert.Equal(["moka-chart-color-primary", "moka-chart-color-secondary"], classes);
	}

	// The palette classes are what carry the theme into the strokes; without them the charts paint
	// in one color whatever the theme.
	[Fact]
	public void ThePalette_PaintsFromThemeTokens()
	{
		Assert.Equal("var(--moka-color-primary)",
			ScopedCss.Declarations("MokaBarChart.razor.css", ".moka-barchart .moka-chart-color-primary")["--moka-chart-color"]);
		Assert.Equal("var(--moka-color-success)",
			ScopedCss.Declarations("MokaLineChart.razor.css", ".moka-linechart .moka-chart-color-success")["--moka-chart-color"]);
	}

	private static int CountSegments(string path) => path.Split(' ').Count(t => t == "L");
}
