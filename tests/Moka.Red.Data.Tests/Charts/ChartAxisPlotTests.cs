using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     The axes <see cref="MokaChartPlot" /> draws around a column chart: the value axis's ticks, side, placement, units and
///     grid, the category, time and number axes along the bottom, titles, templates, and the model cache.
/// </summary>
public sealed class ChartAxisPlotTests : BunitContext
{
	private static readonly string[] Hours = [.. Enumerable.Range(0, 12).Select(i => $"{i * 2:00}:00")];

	public ChartAxisPlotTests() => this.Static();

	[Fact]
	public void TheStandardLook_DrawsTodaysValueAxis_EqualDivisionsWithLinesAtEveryTick()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, Hours), [10, 20, 40]);

		Assert.Equal(["0", "10", "20", "30", "40"], cut.FindAll(".moka-chart-plot__ytick").Select(t => t.TextContent));
		Assert.Contains("moka-chart-plot--grid-lines", cut.Find(".moka-chart-plot").ClassName, StringComparison.Ordinal);
		Assert.Equal(5, cut.FindAll(".moka-chart-plot__gline").Count);
		Assert.Contains("moka-chart-plot--ticks-outside", cut.Find(".moka-chart-plot").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TheValueAxis_ThinsWithThePlotsHeight_ByAContainerQuery()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, Hours), [10, 20, 40]);

		string[] classes = [.. cut.FindAll(".moka-chart-plot__ytick").Select(t => t.ClassName ?? "")];
		Assert.Contains("moka-chart-plot__ytick--h8", classes[1], StringComparison.Ordinal);
		Assert.Contains("moka-chart-plot__ytick--h8", classes[3], StringComparison.Ordinal);
		string css = ScopedCssText();
		Assert.Contains("@container moka-chart-y (height < 8em) { .moka-chart-plot__yticks .moka-chart-plot__ytick--h8 { display: none; } }", css,
			StringComparison.Ordinal);
		Assert.Contains("container: moka-chart-y / size;", css, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(MokaChartGrid.Lines, "moka-chart-plot--grid-lines", 5)]
	[InlineData(MokaChartGrid.Hairlines, "moka-chart-plot--grid-hairlines", 5)]
	[InlineData(MokaChartGrid.Baseline, "moka-chart-plot--grid-baseline", 5)]
	[InlineData(MokaChartGrid.Dotted, "moka-chart-plot--grid-dotted", 5)]
	[InlineData(MokaChartGrid.None, "moka-chart-plot--grid-none", 0)]
	public void GridStyles_AreClassesOnThePlot(MokaChartGrid grid, string cls, int lines)
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, Hours).Add(x => x.Grid, grid), [10, 40]);

		Assert.Contains(cls, cut.Find(".moka-chart-plot").ClassName, StringComparison.Ordinal);
		Assert.Equal(lines, cut.FindAll(".moka-chart-plot__gline").Count);
	}

	[Fact]
	public void TheBaseline_IsZero_WhenTheAxisCrossesIt()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, Hours).Add(x => x.Grid, MokaChartGrid.Hairlines),
			[-20, 40]);

		Assert.Equal(33.333, ShareOf(cut.Find(".moka-chart-plot__gline--base")), 3);
		Assert.Single(cut.FindAll(".moka-chart-plot__gline--base"));
		Assert.Equal(6, cut.FindAll(".moka-chart-plot__gline").Count);
	}

	[Fact]
	public void ABaselineOnATick_IsDrawnOnce()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, Hours), [-20, 20]);

		Assert.Equal(50, ShareOf(cut.Find(".moka-chart-plot__gline--base")), 3);
		Assert.Equal(5, cut.FindAll(".moka-chart-plot__gline").Count);
	}

	[Theory]
	[InlineData(MokaTickPlacement.Inside, "moka-chart-plot--ticks-inside")]
	[InlineData(MokaTickPlacement.Auto, "moka-chart-plot--ticks-auto")]
	public void TickLabels_Inside_And_Auto_AreClasses(MokaTickPlacement placement, string cls)
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, Hours).Add(x => x.TickLabels, placement), [10, 40]);

		Assert.Contains(cls, cut.Find(".moka-chart-plot").ClassName, StringComparison.Ordinal);
		Assert.Contains("@container moka-chart-plot (width < 20em)", ScopedCssText(), StringComparison.Ordinal);
	}

	[Fact]
	public void ValueSideEnd_PutsTheAxisOnTheRight()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, Hours).Add(x => x.ValueSide, MokaAxisSide.End), [10, 40]);

		Assert.Contains("moka-chart-plot--end", cut.Find(".moka-chart-plot").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TheYAxisWinsOverTheChartsShortcuts()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p
			.Add(x => x.Labels, Hours)
			.Add(x => x.ValueSide, MokaAxisSide.End)
			.Add(x => x.Grid, MokaChartGrid.Dotted)
			.Add(x => x.YAxis, new MokaChartAxis { Side = MokaAxisSide.Start, Grid = MokaChartGrid.Baseline }), [10, 40]);

		string classes = cut.Find(".moka-chart-plot").ClassName!;
		Assert.DoesNotContain("moka-chart-plot--end", classes, StringComparison.Ordinal);
		Assert.Contains("moka-chart-plot--grid-baseline", classes, StringComparison.Ordinal);
	}

	[Fact]
	public void TheQuietLook_RoundsTicks_AndGoesCompactFrom10000()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, Hours).Add(x => x.Look, MokaChartLook.Quiet), [1200, 11800],
			MokaTheme.Default);

		Assert.Equal(["0", "4K", "8K", "12K"], cut.FindAll(".moka-chart-plot__ytick").Select(t => t.TextContent));
		Assert.Contains("moka-chart-plot--grid-hairlines", cut.Find(".moka-chart-plot").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TheMatrixLook_StaysStandard_WhateverTheLookParameter()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, Hours).Add(x => x.Look, MokaChartLook.Quiet), [1200, 11800],
			MokaTheme.Dark);

		Assert.Contains("moka-chart-plot--grid-lines", cut.Find(".moka-chart-plot").ClassName, StringComparison.Ordinal);
		Assert.Equal("11800", cut.FindAll(".moka-chart-plot__ytick")[^1].TextContent);
	}

	[Fact]
	public void Units_RoundInTheUnitTheTopReaches_AndCarryIt()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p
			.Add(x => x.Labels, Hours)
			.Add(x => x.YAxis, new MokaChartAxis { Unit = MokaChartUnit.Bytes, Nice = true }), [0, 3.4e9]);

		Assert.Equal(["0", "1 GB", "2 GB", "3 GB", "4 GB"], cut.FindAll(".moka-chart-plot__ytick").Select(t => t.TextContent));
	}

	[Fact]
	public void ShowUnitFalse_MovesTheUnitIntoTheAxisTitle()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p
			.Add(x => x.Labels, Hours)
			.Add(x => x.YAxis, new MokaChartAxis { Unit = MokaChartUnit.Bytes, Nice = true, ShowUnit = false, Title = "Written" }), [0, 3.4e9]);

		Assert.Equal("Written (GB)", cut.Find(".moka-chart-plot__ytitle").TextContent);
		Assert.Equal("4", cut.FindAll(".moka-chart-plot__ytick")[^1].TextContent);
	}

	[Fact]
	public void TheChartsUnit_IsTheValueAxissShortcut()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p
			.Add(x => x.Labels, Hours)
			.Add(x => x.Unit, MokaChartUnit.Percent)
			.Add(x => x.YAxis, new MokaChartAxis { Nice = true }), [0, 83]);

		Assert.Equal("100%", cut.FindAll(".moka-chart-plot__ytick")[^1].TextContent);
	}

	[Fact]
	public void ALogAxis_WritesEachPowerOfTenInTheLargestUnitItIsWholeIn()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p
			.Add(x => x.Labels, Hours)
			.Add(x => x.YAxis, new MokaChartAxis { Scale = MokaAxisScale.Log, Unit = MokaChartUnit.Milliseconds }), [40, 90_000]);

		Assert.Equal(["10 ms", "100 ms", "1 s", "10 s", "100 s"],
			cut.FindAll(".moka-chart-plot__ytick").Where(t => !t.ClassList.Contains("moka-chart-plot__ytick--minor")).Select(t => t.TextContent));
	}

	[Fact]
	public void ALogAxisOverFewDecades_MarksItsMinorTicks()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p
			.Add(x => x.Labels, Hours)
			.Add(x => x.YAxis, new MokaChartAxis { Scale = MokaAxisScale.Log }), [3, 80]);

		Assert.NotEmpty(cut.FindAll(".moka-chart-plot__ytick--minor"));
		Assert.NotEmpty(cut.FindAll(".moka-chart-plot__gline--minor"));
	}

	[Fact]
	public void TickCount_TickValues_MinAndMax_ShapeTheValueAxis()
	{
		IRenderedComponent<ChartAxisTestChart> counted = Chart(p => p.Add(x => x.Labels, Hours).Add(x => x.YAxis, new MokaChartAxis { TickCount = 2 }),
			[0, 40]);
		Assert.Equal(["0", "20", "40"], counted.FindAll(".moka-chart-plot__ytick").Select(t => t.TextContent));

		IRenderedComponent<ChartAxisTestChart> explicitTicks = Chart(p => p.Add(x => x.Labels, Hours)
			.Add(x => x.YAxis, new MokaChartAxis { TickValues = [0, 25, 75] }), [0, 40]);
		Assert.Equal(["0", "25", "75"], explicitTicks.FindAll(".moka-chart-plot__ytick").Select(t => t.TextContent));

		IRenderedComponent<ChartAxisTestChart> fixedEnds = Chart(p => p.Add(x => x.Labels, Hours)
			.Add(x => x.YAxis, new MokaChartAxis { Min = 10, Max = 50, Nice = true }), [0, 40]);
		Assert.Equal("10", fixedEnds.FindAll(".moka-chart-plot__ytick")[0].TextContent);
		Assert.Equal("50", fixedEnds.FindAll(".moka-chart-plot__ytick")[^1].TextContent);
	}

	[Fact]
	public void SuggestedMax_RaisesTheTop_ButHigherDataStillWins()
	{
		IRenderedComponent<ChartAxisTestChart> raised = Chart(p => p.Add(x => x.Labels, Hours)
			.Add(x => x.YAxis, new MokaChartAxis { SuggestedMax = 100, Nice = true }), [0, 40]);
		Assert.Equal("100", raised.FindAll(".moka-chart-plot__ytick")[^1].TextContent);

		IRenderedComponent<ChartAxisTestChart> data = Chart(p => p.Add(x => x.Labels, Hours)
			.Add(x => x.YAxis, new MokaChartAxis { SuggestedMax = 10, Nice = true }), [0, 40]);
		Assert.Equal("40", data.FindAll(".moka-chart-plot__ytick")[^1].TextContent);
	}

	[Fact]
	public void AFormatter_OnTheAxis_WinsOverTheChartsAndTheFormat()
	{
		Func<double, string> axisText = v => $"<{v}>";
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p
			.Add(x => x.Labels, Hours)
			.Add(x => x.Formatter, v => "chart")
			.Add(x => x.YAxis, new MokaChartAxis { Formatter = axisText, Format = "0.00" }), [0, 40]);

		Assert.Equal("<40>", cut.FindAll(".moka-chart-plot__ytick")[^1].TextContent);
	}

	[Fact]
	public void TickTemplates_DrawEachTick()
	{
		RenderFragment<MokaAxisTick> template = tick => builder =>
		{
			builder.OpenElement(0, "b");
			builder.AddContent(1, $"[{tick.Text}]");
			builder.CloseElement();
		};
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p
			.Add(x => x.Labels, ["a", "b"])
			.Add(x => x.YAxis, new MokaChartAxis { TickTemplate = template })
			.Add(x => x.XAxis, new MokaChartAxis { TickTemplate = template }), [0, 40]);

		Assert.Equal("[40]", cut.FindAll(".moka-chart-plot__ytick b")[^1].TextContent);
		Assert.Equal(["[a]", "[b]"], cut.FindAll(".moka-chart-plot__xtick b").Select(b => b.TextContent));
	}

	[Fact]
	public void CategoryLabels_SitAtTheirShares_WithTheirVariables()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, ["Mon", "Tue", "Wed", "Thu"]).Add(x => x.Slots, true),
			[1, 2, 3, 4]);

		IReadOnlyList<IElement> labels = cut.FindAll(".moka-chart-plot__xtick");
		Assert.Equal(["Mon", "Tue", "Wed", "Thu"], labels.Select(l => l.TextContent));
		Assert.Equal([12.5, 37.5, 62.5, 87.5], labels.Select(ShareOf));
		Assert.Contains("--_hw: 0.99em", labels[0].GetAttribute("style"), StringComparison.Ordinal);
		Assert.Contains("moka-chart-plot__x--fit-auto", cut.Find(".moka-chart-plot__x").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void BlankLabels_TakeNoPlace()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, ["a", "", " ", "d"]), [1, 2, 3, 4]);

		Assert.Equal(["a", "d"], cut.FindAll(".moka-chart-plot__xtick").Select(l => l.TextContent));
	}

	[Fact]
	public void LabelFit_IsTheCategoryAxissShortcut_AndTheXAxisWins()
	{
		IRenderedComponent<ChartAxisTestChart> shortcut = Chart(p => p.Add(x => x.Labels, Hours).Add(x => x.LabelFit, MokaAxisLabelFit.Rotate),
			[1, 2]);
		Assert.Contains("--fit-rotate", shortcut.Find(".moka-chart-plot__x").ClassName, StringComparison.Ordinal);

		IRenderedComponent<ChartAxisTestChart> axis = Chart(p => p.Add(x => x.Labels, Hours).Add(x => x.LabelFit, MokaAxisLabelFit.Rotate)
			.Add(x => x.XAxis, new MokaChartAxis { Fit = MokaAxisLabelFit.Wrap }), [1, 2]);
		Assert.Contains("--fit-wrap", axis.Find(".moka-chart-plot__x").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TruncatedLabels_KeepTheirWholeTextInATitle_AndInTheMarkup()
	{
		string[] domains = [.. Enumerable.Range(0, 20).Select(i => $"telemetry-{i}.microsoft.com")];
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, domains).Add(x => x.LabelFit, MokaAxisLabelFit.Truncate),
			[.. Enumerable.Range(0, 20).Select(i => (double)i)]);

		Assert.All(cut.FindAll(".moka-chart-plot__xtick"), label => Assert.Equal(label.TextContent, label.GetAttribute("title")));
	}

	[Fact]
	public void UncutLabels_HaveNoTitle()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, ["Mon", "Tue"]), [1, 2]);

		Assert.All(cut.FindAll(".moka-chart-plot__xtick"), label => Assert.Null(label.GetAttribute("title")));
	}

	[Fact]
	public void Scroll_PutsTheCategoriesInAScroller_AsWideAsTheyNeed_AndTheValueAxisOutside()
	{
		string[] hosts = [.. Enumerable.Range(0, 80).Select(i => $"h{i}")];
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, hosts).Add(x => x.LabelFit, MokaAxisLabelFit.Scroll),
			[.. Enumerable.Range(0, 80).Select(i => (double)i)]);

		IElement scroller = cut.Find(".moka-chart-plot__scroll");
		Assert.Contains("--_scroll-min: 220em", scroller.GetAttribute("style"), StringComparison.Ordinal);
		Assert.NotNull(scroller.QuerySelector(".moka-chart-plot__box"));
		Assert.NotNull(scroller.QuerySelector(".moka-chart-plot__x"));
		Assert.Null(scroller.QuerySelector(".moka-chart-plot__y"));
		Assert.Contains("moka-chart-plot--scroll", cut.Find(".moka-chart-plot").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void ShowXLabelsFalse_And_AHiddenXAxis_DrawNoLabels()
	{
		Assert.Empty(Chart(p => p.Add(x => x.Labels, Hours).Add(x => x.ShowXLabels, false), [1, 2]).FindAll(".moka-chart-plot__x"));
		Assert.Empty(Chart(p => p.Add(x => x.Labels, Hours).Add(x => x.XAxis, new MokaChartAxis { Visible = false }), [1, 2])
			.FindAll(".moka-chart-plot__x"));
		Assert.Empty(Chart(p => p.Add(x => x.Labels, Hours).Add(x => x.YAxis, new MokaChartAxis { Visible = false }), [1, 2])
			.FindAll(".moka-chart-plot__y"));
	}

	[Fact]
	public void AFixedHeight_ReplacesTheProportion()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, Hours).Add(x => x.Height, "140px"), [1, 2]);

		Assert.Contains("height: 140px", cut.Find(".moka-chart-plot__grid").GetAttribute("style"), StringComparison.Ordinal);
		Assert.Empty(cut.FindAll(".moka-chart-plot__ratio"));
		Assert.Contains("moka-chart-plot--height", cut.Find(".moka-chart-plot").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TheTimeAxis_StepsByTheClock_MarksDays_AndDrawsALineAtEachDayStart()
	{
		DateTimeOffset[] times = ChartAxisPreviewTests.TimesOver(TimeSpan.FromHours(48), 97, new DateTimeOffset(2026, 11, 9, 18, 0, 0, TimeSpan.Zero));
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Times, times).Add(x => x.TimeZone, TimeZoneInfo.Utc),
			[.. Enumerable.Range(0, 97).Select(i => (double)i)], culture: "en-GB");

		IReadOnlyList<IElement> days = cut.FindAll(".moka-chart-plot__xtick--day");
		Assert.Equal(["Tue 10", "Wed 11"], days.Select(d => d.TextContent));
		Assert.Equal(2, cut.FindAll(".moka-chart-plot__vline--day").Count);
		Assert.Contains(cut.FindAll(".moka-chart-plot__xtick"), t => t.TextContent == "12:00");
		Assert.Contains("moka-chart-plot__x--time", cut.Find(".moka-chart-plot__x").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TimeTicks_OfOneStep_ShareOneWidth()
	{
		DateTimeOffset[] times = ChartAxisPreviewTests.TimesOver(TimeSpan.FromHours(48), 97, new DateTimeOffset(2026, 11, 9, 18, 0, 0, TimeSpan.Zero));
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Times, times).Add(x => x.TimeZone, TimeZoneInfo.Utc),
			[.. Enumerable.Range(0, 97).Select(i => (double)i)], culture: "en-GB");

		string WidthOf(string text, int nth = 0) => cut.FindAll(".moka-chart-plot__xtick").Where(t => t.TextContent == text).ElementAt(nth).ClassList
			.Single(c => c.StartsWith("moka-chart-plot__xtick--w", StringComparison.Ordinal));
		Assert.Equal(WidthOf("06:00"), WidthOf("18:00", 1));
		Assert.NotEqual(WidthOf("06:00"), WidthOf("12:00"));
	}

	[Fact]
	public void ATimeFormat_And_AFormatter_WriteTheTicks()
	{
		DateTimeOffset[] times = ChartAxisPreviewTests.TimesOver(TimeSpan.FromHours(6), 13, new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero));
		IRenderedComponent<ChartAxisTestChart> formatted = Chart(p => p.Add(x => x.Times, times).Add(x => x.TimeZone, TimeZoneInfo.Utc)
			.Add(x => x.XAxis, new MokaChartAxis { TimeFormat = "HH'h'" }), [.. Enumerable.Range(0, 13).Select(i => (double)i)], culture: "en-GB");
		Assert.Contains(formatted.FindAll(".moka-chart-plot__xtick"), t => t.TextContent == "03h");
	}

	[Fact]
	public void ANumberAxis_IsNiceAndThinsByRank()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.NumberRange, ((double, double)?)(3, 97)), [1, 2]);

		Assert.Equal(["0", "20", "40", "60", "80", "100"], cut.FindAll(".moka-chart-plot__xtick").Select(t => t.TextContent));
		Assert.Equal(ChartXKind.Number, cut.Instance.Base.ColumnPlotModel(0).XKind);
	}

	[Fact]
	public void TheModel_IsWorkedOutOncePerGeometryVersion()
	{
		double[] values = [1, 2];
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, Hours).Add(x => x.Title, "A"), values);
		ColumnPlotModel first = cut.Instance.Base.ColumnPlotModel(0);

		cut.Render(p => p.Add(x => x.Values, values).Add(x => x.Labels, Hours).Add(x => x.Title, "A title"));
		Assert.Same(first, cut.Instance.Base.ColumnPlotModel(0));

		cut.Render(p => p.Add(x => x.Values, new double[] { 1, 200 }).Add(x => x.Labels, Hours).Add(x => x.Title, "A title"));
		Assert.NotSame(first, cut.Instance.Base.ColumnPlotModel(0));
	}

	[Fact]
	public void TheFrameARebuildSees_IsTheOneTheRenderUses()
	{
		DateTimeOffset[] times = ChartAxisPreviewTests.TimesOver(TimeSpan.FromHours(6), 13);
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Times, times), [.. Enumerable.Range(0, 13).Select(i => (double)i)]);

		ChartXFrame rebuilt = cut.Instance.FrameAtRebuild!;
		ChartXFrame drawn = cut.Instance.Base.XFrame();
		Assert.Equal(rebuilt.From, drawn.From);
		Assert.Equal(rebuilt.To, drawn.To);
		Assert.Equal(ChartXKind.Time, drawn.Kind);
		Assert.Equal(0.5, drawn.Share(MokaChartX.FromDateTimeOffset(times[6]).Value), 6);
	}

	[Fact]
	public void AReversedXAxis_RunsTheOtherWay()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart(p => p.Add(x => x.Labels, ["a", "b", "c"]).Add(x => x.XAxis, new MokaChartAxis { Reverse = true }),
			[1, 2, 3]);

		Assert.Equal(1, cut.Instance.Base.XShare(0), 6);
		Assert.Equal(0, cut.Instance.Base.XShare(2), 6);
	}

	private IRenderedComponent<ChartAxisTestChart> Chart(Action<ComponentParameterCollectionBuilder<ChartAxisTestChart>> set,
		double[] values, MokaTheme? theme = null, string culture = "en-US")
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
		try
		{
			return Render<ChartAxisTestChart>(p =>
			{
				p.Add(x => x.Values, values);
				if (theme is not null)
				{
					p.AddCascadingValue(theme);
				}

				set(p);
			});
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	internal static double ShareOf(IElement element)
	{
		string style = element.GetAttribute("style") ?? "";
		int start = style.IndexOf("--_s:", StringComparison.Ordinal) + 5;
		int end = style.IndexOf('%', start);
		return double.Parse(style[start..end], CultureInfo.InvariantCulture);
	}

	internal static string ScopedCssText() =>
		File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaChartPlot.razor.css")).ReplaceLineEndings("\n");
}
