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

	[Fact]
	public void Sparkline_ByDefault_KeepsItsMarkup()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p.Add(x => x.Values, [1d, 2]));

		Assert.Equal("moka-sparkline moka-sparkline--md", cut.Find(".moka-sparkline").ClassName);
		Assert.Null(cut.Find(".moka-sparkline").GetAttribute("style"));
		Assert.Equal("M 0.00 35.00 L 120.00 1.00", cut.Find(".moka-sparkline-line").GetAttribute("d"));
		Assert.Equal("M 0 36 0.00 35.00 L 120.00 1.00 L 120.00 36 Z", cut.Find(".moka-sparkline-area").GetAttribute("d"));
		Assert.Equal("120.00", cut.Find(".moka-sparkline-point").GetAttribute("x1"));
	}

	[Theory]
	[InlineData("24px", "height: 24px")]
	[InlineData("1.5rem", "height: 1.5rem")]
	[InlineData(" 24px ", "height: 24px")]
	[InlineData("24", "height: 24px")]
	public void Sparkline_Height_SetsTheBox(string height, string style)
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [1d, 2])
			.Add(x => x.Height, height));

		Assert.Equal(style, cut.Find(".moka-sparkline").GetAttribute("style"));
	}

	[Theory]
	[InlineData("tall")]
	[InlineData("calc(1px + 2px)")]
	[InlineData("10px; position: fixed")]
	public void Sparkline_Height_IgnoresWhatIsNotALength(string height)
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [1d, 2])
			.Add(x => x.Height, height));

		Assert.Null(cut.Find(".moka-sparkline").GetAttribute("style"));
	}

	[Theory]
	[InlineData("var(--moka-color-success)")]
	[InlineData("#3DD68C")]
	[InlineData("color-mix(in srgb, red 40%, white)")]
	public void Sparkline_ColorValue_PaintsThroughTheChartColor(string color)
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [1d, 2])
			.Add(x => x.ColorValue, color));

		Assert.Equal($"--moka-chart-color: {color}", cut.Find(".moka-sparkline").GetAttribute("style"));
	}

	[Theory]
	[InlineData("red; position: fixed")]
	[InlineData("url(evil.png)")]
	[InlineData("--moka-color-success")]
	public void Sparkline_ColorValue_IgnoresWhatIsNotAColor(string color)
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [1d, 2])
			.Add(x => x.ColorValue, color));

		Assert.Null(cut.Find(".moka-sparkline").GetAttribute("style"));
	}

	[Fact]
	public void Sparkline_StylePutsTheConsumersLast()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [1d, 2])
			.Add(x => x.Height, "24px")
			.Add(x => x.ColorValue, "red")
			.Add(x => x.Style, "width: 100%"));

		Assert.Equal("height: 24px; --moka-chart-color: red; width: 100%", cut.Find(".moka-sparkline").GetAttribute("style"));
	}

	[Fact]
	public void Sparkline_MinAndMax_FixTheScale()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [5d, 5])
			.Add(x => x.Min, 0d)
			.Add(x => x.Max, 10d));

		Assert.Equal("M 0.00 18.00 L 120.00 18.00", cut.Find(".moka-sparkline-line").GetAttribute("d"));
	}

	[Fact]
	public void Sparkline_ValuesOutsideTheScale_StayInTheBox()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [-50d, 50])
			.Add(x => x.Min, 0d)
			.Add(x => x.Max, 10d));

		Assert.Equal("M 0.00 35.00 L 120.00 1.00", cut.Find(".moka-sparkline-line").GetAttribute("d"));
	}

	[Fact]
	public void Sparkline_OnlyMin_TakesTheCeilingFromTheValues()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [10d, 20])
			.Add(x => x.Min, 0d));

		Assert.Equal("M 0.00 18.00 L 120.00 1.00", cut.Find(".moka-sparkline-line").GetAttribute("d"));
	}

	[Fact]
	public void Sparkline_NaN_BreaksTheLineAndTheArea()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [0d, 10, double.NaN, 0, 10]));

		Assert.Equal("M 0.00 35.00 L 30.00 1.00 M 90.00 35.00 L 120.00 1.00", cut.Find(".moka-sparkline-line").GetAttribute("d"));
		Assert.Equal("M 0 36 0.00 35.00 L 30.00 1.00 L 30.00 36 Z M 90 36 90.00 35.00 L 120.00 1.00 L 120.00 36 Z",
			cut.Find(".moka-sparkline-area").GetAttribute("d"));
		Assert.DoesNotContain("NaN", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void Sparkline_NaN_DoesNotPoisonTheScale()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [double.NaN, 0d, 10]));

		Assert.Equal("M 60.00 35.00 L 120.00 1.00", cut.Find(".moka-sparkline-line").GetAttribute("d"));
	}

	[Fact]
	public void Sparkline_ALoneValueBetweenGaps_StillDraws()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [0d, double.NaN, 5, double.NaN, 10]));

		Assert.Contains("M 60.00 18.00 L 60.00 18.00", cut.Find(".moka-sparkline-line").GetAttribute("d"), StringComparison.Ordinal);
	}

	[Fact]
	public void Sparkline_ATrailingGap_MovesTheDotToTheLastValue()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [1d, 2, double.NaN]));

		Assert.Equal("60.00", cut.Find(".moka-sparkline-point").GetAttribute("x1"));
		Assert.Equal("1.00", cut.Find(".moka-sparkline-point").GetAttribute("y1"));
		Assert.Equal("2", cut.Find(".moka-sparkline-point title").TextContent);
	}

	[Theory]
	[InlineData(double.NaN, double.NaN)]
	[InlineData(double.NaN, 5d)]
	[InlineData(double.PositiveInfinity, double.NegativeInfinity)]
	public void Sparkline_WithFewerThanTwoValues_RendersAnEmptyBox(double first, double second)
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [first, second, double.NaN]));

		Assert.Empty(cut.FindAll(".moka-sparkline-svg"));
	}

	[Fact]
	public void LineChart_ByDefault_KeepsItsPaths()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.ShowArea, true)
			.Add(x => x.Series, [new MokaLineSeries { Name = "A", Values = [10, 20] }]));

		Assert.Equal("M 40.00 123.00 L 588.00 12.00", cut.Find(".moka-linechart-line").GetAttribute("d"));
		Assert.Equal("M 40.00 234.00 40.00 123.00 L 588.00 12.00 L 588.00 234.00 Z", cut.Find(".moka-linechart-area").GetAttribute("d"));
	}

	[Fact]
	public void LineChart_ASingleValue_KeepsItsPaths()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.ShowArea, true)
			.Add(x => x.Series, [new MokaLineSeries { Name = "A", Values = [10] }]));

		Assert.Equal("M 314.00 12.00", cut.Find(".moka-linechart-line").GetAttribute("d"));
		Assert.Equal("M 40.00 234.00 314.00 12.00 L 588.00 234.00 Z", cut.Find(".moka-linechart-area").GetAttribute("d"));
	}

	[Fact]
	public void LineChart_NaN_BreaksTheLineAndTheArea_AndDrawsNoPoint()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.ShowArea, true)
			.Add(x => x.Series, [new MokaLineSeries { Name = "A", Values = [10, 20, double.NaN, 10, 20] }]));

		Assert.Equal("M 40.00 123.00 L 177.00 12.00 M 451.00 123.00 L 588.00 12.00", cut.Find(".moka-linechart-line").GetAttribute("d"));
		Assert.Equal(
			"M 40.00 234.00 40.00 123.00 L 177.00 12.00 L 177.00 234.00 Z M 451.00 234.00 451.00 123.00 L 588.00 12.00 L 588.00 234.00 Z",
			cut.Find(".moka-linechart-area").GetAttribute("d"));
		Assert.Equal(4, cut.FindAll(".moka-linechart-point").Count);
		Assert.DoesNotContain("NaN", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void LineChart_NaN_DoesNotPoisonTheAxis()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.YTicks, 1)
			.Add(x => x.Series, [new MokaLineSeries { Name = "A", Values = [double.NaN, 20] }]));

		Assert.Equal(["0", "20"], cut.FindAll(".moka-linechart-ytick")
			.Select(t => t.TextContent)
			.ToArray());
	}

	[Fact]
	public void LineChart_ALoneValueBetweenGaps_StillDraws()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.ShowPoints, false)
			.Add(x => x.Series, [new MokaLineSeries { Name = "A", Values = [double.NaN, 20, double.NaN] }]));

		Assert.Equal("M 314.00 12.00 L 314.00 12.00", cut.Find(".moka-linechart-line").GetAttribute("d"));
	}

	[Fact]
	public void LineChart_ASeriesOfAllNaN_DrawsNothing_ButKeepsTheAxesAndLegend()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.ShowArea, true)
			.Add(x => x.Series, [new MokaLineSeries { Name = "Empty", Values = [double.NaN, double.NaN] }]));

		Assert.Empty(cut.FindAll(".moka-linechart-line"));
		Assert.Empty(cut.FindAll(".moka-linechart-area"));
		Assert.Empty(cut.FindAll(".moka-linechart-point"));
		Assert.Equal(5, cut.FindAll(".moka-linechart-grid").Count);
		Assert.Contains("Empty", cut.Find(".moka-linechart-legend").TextContent, StringComparison.Ordinal);
		Assert.DoesNotContain("NaN", cut.Find("svg").OuterHtml, StringComparison.Ordinal);
	}

	[Fact]
	public void LineChart_ASeriesOfAllNaN_LeavesTheOthersDrawn()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Series,
			[
				new MokaLineSeries { Name = "Empty", Values = [double.NaN, double.NaN] },
				new MokaLineSeries { Name = "Full", Values = [10, 20] }
			]));

		Assert.Single(cut.FindAll(".moka-linechart-line"));
		Assert.Equal(2, cut.FindAll(".moka-linechart-point").Count);
	}

	[Fact]
	public void LineChart_AllNaN_WithAFixedScale_DrawsThatScale()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.YTicks, 1)
			.Add(x => x.Min, 0d)
			.Add(x => x.Max, 100d)
			.Add(x => x.Series, [new MokaLineSeries { Name = "A", Values = [double.NaN] }]));

		Assert.Equal(["0", "100"], cut.FindAll(".moka-linechart-ytick")
			.Select(t => t.TextContent)
			.ToArray());
	}

	private static int CountSegments(string path) => path.Split(' ').Count(t => t == "L");
}
