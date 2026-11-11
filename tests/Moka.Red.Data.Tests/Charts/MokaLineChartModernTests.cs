using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using static Moka.Red.Data.Tests.Charts.MokaLineChartTestKit;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>The modern line chart's marks: lines, stacks, bands, steps, curves, dots, areas, emphasis and panels.</summary>
public sealed class MokaLineChartModernTests : BunitContext
{
	private static readonly string[] Three = ["a", "b", "c"];

	public MokaLineChartModernTests() => this.Static();

	public static TheoryData<string> NewParameters =>
	[
		"Stacked", "Curve", "Points", "Area", "DirectLabels", "Split", "IndexTo", "BreakGapsOver", "PauseWhilePointing",
		"Title", "Highlight", "HighlightSeries", "Look", "Tooltip", "Keyboard", "Times", "YAxis", "Unit", "Grid", "Annotations",
		"TargetLow", "Range", "Zoom", "Legend", "HiddenSeries", "State", "Controls", "Colors", "SeriesLow", "SeriesSlot",
		"SeriesColorValue", "SeriesCurve"
	];

	[Fact]
	public void TodaysParameters_KeepTheClassicChart()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Three)
			.Add(x => x.ShowArea, true)
			.Add(x => x.Smooth, true)
			.Add(x => x.ShowLastPoint, true)
			.Add(x => x.Curve, MokaLineCurve.Smooth)
			.Add(x => x.Tooltip, MokaChartTooltip.Native)
			.Add(x => x.Look, MokaChartLook.Standard)
			.Add(x => x.Series, [Series("A", 1, 2, 3) with { Key = "a" }]));

		Assert.True(cut.Instance.IsClassic);
		Assert.Empty(cut.FindAll(".moka-chart"));
		Assert.NotEmpty(cut.FindAll("svg.moka-linechart-plot"));
	}

	[Theory]
	[MemberData(nameof(NewParameters))]
	public void AnyNewParameter_TurnsTheChartModern(string parameter)
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p =>
		{
			p.Add(x => x.Labels, Three);
			MokaLineSeries series = Series("A", 1, 2, 3);
			switch (parameter)
			{
				case "Stacked": p.Add(x => x.Stacked, true); break;
				case "Curve": p.Add(x => x.Curve, MokaLineCurve.Step); break;
				case "Points": p.Add(x => x.Points, MokaChartPoints.Hover); break;
				case "Area": p.Add(x => x.Area, MokaChartArea.Fade); break;
				case "DirectLabels": p.Add(x => x.DirectLabels, MokaDirectLabels.Names); break;
				case "Split": p.Add(x => x.Split, true); break;
				case "IndexTo": p.Add(x => x.IndexTo, 100d); break;
				case "BreakGapsOver": p.Add(x => x.BreakGapsOver, TimeSpan.FromMinutes(5)); break;
				case "PauseWhilePointing": p.Add(x => x.PauseWhilePointing, true); break;
				case "Title": p.Add(x => x.Title, "Traffic"); break;
				case "Highlight": p.Add(x => x.Highlight, "A"); break;
				case "HighlightSeries": p.Add(x => x.HighlightSeries, ["A"]); break;
				case "Look": p.Add(x => x.Look, MokaChartLook.Bold); break;
				case "Tooltip": p.Add(x => x.Tooltip, MokaChartTooltip.Crosshair); break;
				case "Keyboard": p.Add(x => x.Keyboard, true); break;
				case "Times": p.Add(x => x.Times, Hours(3)); break;
				case "YAxis": p.Add(x => x.YAxis, new MokaChartAxis { Nice = true }); break;
				case "Unit": p.Add(x => x.Unit, MokaChartUnit.Percent); break;
				case "Grid": p.Add(x => x.Grid, MokaChartGrid.Dotted); break;
				case "Annotations": p.Add(x => x.Annotations, [new MokaChartThreshold(2)]); break;
				case "TargetLow": p.Add(x => x.TargetLow, 1d); break;
				case "Range": p.Add(x => x.Range, new MokaChartRange(0, 1)); break;
				case "Zoom": p.Add(x => x.Zoom, MokaChartZoom.Brush); break;
				case "Legend": p.Add(x => x.Legend, MokaChartLegendPosition.Top); break;
				case "HiddenSeries": p.Add(x => x.HiddenSeries, []); break;
				case "State": p.Add(x => x.State, MokaChartState.Loading); break;
				case "Controls": p.Add(x => x.Controls, MokaChartControls.None); break;
				case "Colors": p.Add(x => x.Colors, ["#ff0000"]); break;
				case "SeriesLow": series = series with { Low = [0, 1, 2], High = [2, 3, 4] }; break;
				case "SeriesSlot": series = series with { Slot = 3 }; break;
				case "SeriesColorValue": series = series with { ColorValue = "var(--moka-chart-4)" }; break;
				case "SeriesCurve": series = series with { Curve = MokaLineCurve.Step }; break;
			}

			p.Add(x => x.Series, [series]);
		});

		Assert.False(cut.Instance.IsClassic);
		Assert.NotEmpty(cut.FindAll(".moka-linechart.moka-chart"));
	}

	[Fact]
	public void TurningTheNewParameterOff_BringsTheClassicChartBack()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Title, "Traffic")
			.Add(x => x.Series, [Series("A", 1, 2, 3)]));
		Assert.False(cut.Instance.IsClassic);

		cut.Render(p => p.Add(x => x.Title, (string?)null));

		Assert.True(cut.Instance.IsClassic);
		Assert.NotEmpty(cut.FindAll("svg.moka-linechart-plot"));
	}

	[Fact]
	public void TheMatrixLook_StaysClassicForALook_ButNotForAFeature()
	{
		RenderTree.TryAdd<CascadingValue<MokaTheme>>(p => p.Add(x => x.Value, MokaTheme.Dark.WithCharts(MokaChartLook.Quiet)));
		IRenderedComponent<MokaLineChart> looked = Render<MokaLineChart>(p => p.Add(x => x.Series, [Series("A", 1, 2)]));
		IRenderedComponent<MokaLineChart> stacked = Render<MokaLineChart>(p => p
			.Add(x => x.Stacked, true)
			.Add(x => x.Series, [Series("A", 1, 2)]));

		Assert.True(looked.Instance.IsClassic);
		Assert.False(stacked.Instance.IsClassic);
		Assert.Contains("moka-chart-look-standard", stacked.Find(".moka-linechart").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void AQuietChart_DrawsEachLineThroughItsValues()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p.Add(x => x.Series, [Series("A", 0, 50, 100)]));

		Assert.Equal([('M', 0, 10000), ('L', 5000, 5000), ('L', 10000, 0)], Points(LineOf(cut, "A")));
	}

	[Fact]
	public void AGap_BreaksTheLine_AndALoneValueStillDraws()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.Series, [Series("A", 0, 100, double.NaN, 50, double.NaN)]), ["a", "b", "c", "d", "e"]);

		string? line = LineOf(cut, "A");
		Assert.Equal(2, Moves(line));
		Assert.Contains("M7500,5000h0", line, StringComparison.Ordinal);
	}

	[Fact]
	public void AStack_DrawsEachTopAsTheTotalSoFar_AndEachBandDownToTheOneBelow()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.Stacked, true)
			.Add(x => x.Min, 0d)
			.Add(x => x.Max, 100d)
			.Add(x => x.Series, [Series("A", 10, 20, 30), Series("B", 40, 40, 40)]));

		Assert.Equal([9000, 8000, 7000], Points(LineOf(cut, "A")).Select(point => point.Y));
		Assert.Equal([5000, 4000, 3000], Points(LineOf(cut, "B")).Select(point => point.Y));
		IReadOnlyList<(char Command, int X, int Y)> band = Points(PathOf(Group(cut.Find(".moka-linechart"), "B"), "moka-linechart__area"));
		Assert.Equal([5000, 4000, 3000, 7000, 8000, 9000], band.Select(point => point.Y));
	}

	[Fact]
	public void InAStack_AGapIsAGapInItsOwnBand_AndNothingForTheBandsAbove()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.Stacked, true)
			.Add(x => x.Min, 0d)
			.Add(x => x.Max, 100d)
			.Add(x => x.Series, [Series("A", 10, double.NaN, 30), Series("B", 40, 40, 40)]));

		Assert.Equal(2, Moves(LineOf(cut, "A")));
		Assert.Equal([5000, 6000, 3000], Points(LineOf(cut, "B")).Select(point => point.Y));
	}

	[Fact]
	public void ABand_RunsAlongTheHighsAndBackAlongTheLows()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.Min, 0d)
			.Add(x => x.Max, 100d)
			.Add(x => x.Series, [Series("Median", 50, 50, 50) with { Low = [40, 30, 20], High = [60, 70, 80] }]));

		IReadOnlyList<(char Command, int X, int Y)> band =
			Points(PathOf(Group(cut.Find(".moka-linechart"), "Median"), "moka-linechart__band"));
		Assert.Equal([4000, 3000, 2000, 8000, 7000, 6000], band.Select(point => point.Y));
	}

	[Fact]
	public void AStepLine_HoldsEachValueUntilTheNext()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.Curve, MokaLineCurve.Step)
			.Add(x => x.Min, 0d)
			.Add(x => x.Max, 10d)
			.Add(x => x.Series, [Series("A", 2, 5, 3)]));

		Assert.Equal("M0,8000H5000V5000H10000V7000", LineOf(cut, "A"));
	}

	[Fact]
	public void ASeriesCurve_WinsOverTheChartsAndSmoothDrawsCurves()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.Curve, MokaLineCurve.Step)
			.Add(x => x.Series, [Series("A", 2, 5, 3) with { Curve = MokaLineCurve.Smooth }, Series("B", 1, 2, 3)]));

		Assert.Contains("C", LineOf(cut, "A"), StringComparison.Ordinal);
		Assert.Contains("H", LineOf(cut, "B"), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(MokaChartPoints.Always, 3, false)]
	[InlineData(MokaChartPoints.Hover, 0, true)]
	[InlineData(MokaChartPoints.None, 0, false)]
	public void Points_AreEveryValue_TheEnd_OrNothing(MokaChartPoints points, int dots, bool endDot)
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.Points, points)
			.Add(x => x.Series, [Series("A", 2, 5, 3)]));

		IElement group = Group(cut.Find(".moka-linechart"), "A");
		Assert.Equal(dots, Moves(PathOf(group, "moka-linechart__dot")));
		Assert.Equal(endDot, cut.FindAll("path.moka-linechart__dot--end").Count == 1);
		Assert.Equal(points == MokaChartPoints.None, cut.Find(".moka-linechart").ClassList.Contains("moka-linechart--points-none"));
	}

	[Fact]
	public void PastFourHundredValues_ALineHasDotsOnlyWherePointed()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.Points, MokaChartPoints.Always)
			.Add(x => x.Series, [Series("A", [.. Enumerable.Range(0, 401).Select(i => (double)i)])]));

		Assert.Null(PathOf(Group(cut.Find(".moka-linechart"), "A"), "moka-linechart__dot"));
		Assert.Single(cut.FindAll("path.moka-linechart__dot--end"));
	}

	[Fact]
	public void ShowPointsOff_MeansNoPoints()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.ShowPoints, false)
			.Add(x => x.Series, [Series("A", 2, 5, 3)]));

		Assert.Empty(cut.FindAll("path.moka-linechart__dot"));
	}

	[Fact]
	public void ANativeTooltip_PutsATitleOnEveryDot()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Title, "Load")
			.Add(x => x.Labels, Three)
			.Add(x => x.Series, [Series("A", 2, 5, 3)]));

		Assert.Equal(["a · A: 2", "b · A: 5", "c · A: 3"], cut.FindAll("line.moka-linechart__dot title").Select(t => t.TextContent));
	}

	[Theory]
	[InlineData(MokaChartArea.Flat, "moka-linechart__area--flat", false)]
	[InlineData(MokaChartArea.Fade, "moka-linechart__area--fade", true)]
	public void AnArea_IsAFlatTintOrAFade(MokaChartArea area, string className, bool gradient)
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.Area, area)
			.Add(x => x.Series, [Series("A", 2, 5, 3)]));

		IElement path = cut.Find("path.moka-linechart__area");
		Assert.Contains(className, path.ClassName, StringComparison.Ordinal);
		Assert.Equal(gradient, cut.FindAll("linearGradient").Count == 1);
		Assert.Equal(gradient, path.GetAttribute("fill")?.StartsWith("url(#", StringComparison.Ordinal) == true);
	}

	[Fact]
	public void ShowArea_FillsWithTheLooksTint_AndNothingFillsWithoutIt()
	{
		IRenderedComponent<MokaLineChart> with = Quiet(p => p.Add(x => x.ShowArea, true).Add(x => x.Series, [Series("A", 2, 5, 3)]));
		IRenderedComponent<MokaLineChart> without = Quiet(p => p.Add(x => x.Series, [Series("A", 2, 5, 3)]));

		Assert.Single(with.FindAll("path.moka-linechart__area--flat"));
		Assert.Empty(without.FindAll("path.moka-linechart__area"));
	}

	[Fact]
	public void ABand_LeavesTheAreaUnderItsLineOut_UnlessAskedFor()
	{
		MokaLineSeries banded = Series("A", 2, 5, 3) with { Low = [1, 4, 2], High = [3, 6, 4] };
		IRenderedComponent<MokaLineChart> soft = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Soft)
			.Add(x => x.Series, [banded]));
		IRenderedComponent<MokaLineChart> asked = Quiet(p => p.Add(x => x.Area, MokaChartArea.Flat).Add(x => x.Series, [banded]));

		Assert.Empty(soft.FindAll("path.moka-linechart__area"));
		Assert.Single(asked.FindAll("path.moka-linechart__area"));
	}

	[Fact]
	public void AHighlight_MutesTheRest_DrawsThemFirst_AndNamesItAtItsEnd()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Soft)
			.Add(x => x.Highlight, "Search")
			.Add(x => x.Series, [Series("Search", 1, 2), Series("Auth", 2, 1), Series("Files", 3, 3)]));

		Assert.Equal(["Auth", "Files", "Search"], GroupKeys(cut.Find(".moka-linechart")));
		Assert.Contains("moka-chart-muted", Group(cut.Find(".moka-linechart"), "Auth").ClassName, StringComparison.Ordinal);
		Assert.Contains("moka-linechart__series--muted", Group(cut.Find(".moka-linechart"), "Auth").ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain("moka-chart-muted", Group(cut.Find(".moka-linechart"), "Search").ClassName, StringComparison.Ordinal);
		Assert.Equal(["Search"], cut.FindAll(".moka-linechart__end-name").Select(e => e.TextContent));
	}

	[Fact]
	public void SmallMultiples_DrawOnePanelPerShownSeries_OnOneScale()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.Split, true)
			.Add(x => x.HiddenSeries, ["C"])
			.Add(x => x.Series, [Series("A", 1, 2, 3), Series("B", 10, 20, 30), Series("C", 100, 200, 300)]));

		IReadOnlyList<IElement> panels = cut.FindAll(".moka-linechart__panel");
		Assert.Equal(2, panels.Count);
		Assert.Equal(["A", "B"], panels.Select(panel => panel.QuerySelector(".moka-linechart__panel-name")!.TextContent));
		Assert.Equal(["A"], GroupKeys(panels[0]));
		Assert.Equal(["B"], GroupKeys(panels[1]));
		Assert.Equal(2, cut.Instance.PanelCount);
		Assert.Equal(cut.Instance.ValueRange(0), cut.Instance.ValueRange(1));
		Assert.Equal(panels[0].QuerySelectorAll(".moka-chart-plot__ytick").Select(t => t.TextContent),
			panels[1].QuerySelectorAll(".moka-chart-plot__ytick").Select(t => t.TextContent));
		Assert.Equal(["30"], panels[1].QuerySelectorAll(".moka-linechart__panel-value b").Select(b => b.TextContent));
	}

	[Fact]
	public void SplitColumns_CapsThePanelsAcross()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.Split, true)
			.Add(x => x.SplitColumns, 2)
			.Add(x => x.Series, [Series("A", 1, 2), Series("B", 2, 1)]));

		Assert.Contains("--_columns: 2", cut.Find(".moka-linechart__panels").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void IndexTo_DividesEachSeriesByItsFirstValue()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.IndexTo, 100d)
			.Add(x => x.Min, 0d)
			.Add(x => x.Max, 200d)
			.Add(x => x.Series, [Series("Sessions", 1000, 1500, 2000), Series("Users", 10, 10, 15)]));

		Assert.Equal([5000, 2500, 0], Points(LineOf(cut, "Sessions")).Select(point => point.Y));
		Assert.Equal([5000, 5000, 2500], Points(LineOf(cut, "Users")).Select(point => point.Y));
	}

	[Fact]
	public void AnIndexedAxis_StartsJustUnderItsLowestValue_InPlainNumbers()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.IndexTo, 100d)
			.Add(x => x.Unit, MokaChartUnit.Milliseconds)
			.Add(x => x.Series, [Series("A", 50, 60, 85)]));

		string[] ticks = [.. cut.FindAll(".moka-chart-plot__ytick").Select(t => t.TextContent)];
		Assert.Equal("100", ticks[0]);
		Assert.DoesNotContain(ticks, tick => tick.Contains("ms", StringComparison.Ordinal));
	}

	[Fact]
	public void BreakGapsOver_BreaksTheLineWhereSamplesStopped()
	{
		DateTimeOffset start = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.BreakGapsOver, TimeSpan.FromMinutes(10))
			.Add(x => x.Times, [start, start.AddMinutes(5), start.AddMinutes(40), start.AddMinutes(45)])
			.Add(x => x.Series, [Series("A", 1, 2, 3, 4)]));

		Assert.Equal(2, Moves(LineOf(cut, "A")));
	}

	[Fact]
	public void AWindowThatBreaksTheLine_MakesItsValuesGaps()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.Annotations, [new MokaChartWindow(1.5, 2.5) { Label = "Maintenance" }])
			.Add(x => x.Series, [Series("A", 1, 2, 3, 4, 5)]), ["a", "b", "c", "d", "e"]);

		Assert.Equal(2, Moves(LineOf(cut, "A")));
		Assert.Equal(4, Points(LineOf(cut, "A")).Count);
	}

	[Fact]
	public void SeriesSharingAName_RenderWithoutKeys_AndRenderAgain()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p.Add(x => x.Series, [Series("A", 1, 2), Series("A", 2, 3)]));

		cut.Render(p => p.Add(x => x.Series, [Series("A", 3, 2), Series("A", 1, 3)]));

		Assert.Equal(2, cut.FindAll("g.moka-linechart__series").Count);
	}

	[Fact]
	public void ColorValue_AndSlot_ColorTheSeries()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p.Add(x => x.Series,
		[
			Series("A", 1, 2) with { Slot = 5 },
			Series("B", 2, 1) with { ColorValue = "var(--moka-chart-3)" },
			Series("C", 2, 1) with { ColorValue = "red; background: url(x)" }
		]));

		IElement root = cut.Find(".moka-linechart");
		Assert.Contains("moka-chart-c5", Group(root, "A").ClassName, StringComparison.Ordinal);
		Assert.Equal("--moka-chart-color: var(--moka-chart-3)", Group(root, "B").GetAttribute("style"));
		Assert.Null(Group(root, "C").GetAttribute("style"));
	}

	[Fact]
	public void AStaticRender_IsAnImageWithNoSurface_AndItsMarksAreHidden()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.ShowLegend, false)
			.Add(x => x.Series, [Series("A", 1, 2, 3)]));

		Assert.Equal("img", cut.Find(".moka-linechart").GetAttribute("role"));
		Assert.Empty(cut.FindAll(".moka-chart-surface"));
		Assert.Equal("true", Marks(cut.Find(".moka-linechart")).GetAttribute("aria-hidden"));
	}

	[Fact]
	public void ShowPeak_SaysTheHighestValueUnderThePlot()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.ShowPeak, true)
			.Add(x => x.Series, [Series("A", 1, 7, 3), Series("B", 2, 4, 6)]));

		Assert.Equal("Peak 7", string.Join(' ', cut.Find(".moka-linechart__peak").TextContent
			.Split((char[])[' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)));
	}

	[Fact]
	public void TheReadoutLook_PutsAStripOverThePlot()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Dense)
			.Add(x => x.Labels, Three)
			.Add(x => x.Series, [Series("A", 1, 2, 3)]));

		Assert.Contains("3", cut.Find(".moka-chart-plot__readout").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void TheEmptyState_ShowsWithoutValues()
	{
		IRenderedComponent<MokaLineChart> cut = Quiet(p => p
			.Add(x => x.EmptyText, "Waiting for traffic")
			.Add(x => x.Series, [Series("A", double.NaN, double.NaN)]));

		Assert.Equal(MokaChartState.Empty, cut.Instance.ResolvedState);
		Assert.Contains("Waiting for traffic", cut.Markup, StringComparison.Ordinal);
	}

	private IRenderedComponent<MokaLineChart> Quiet(Action<ComponentParameterCollectionBuilder<MokaLineChart>> parameters,
		string[]? labels = null) =>
		Render<MokaLineChart>(p =>
		{
			p.Add(x => x.Look, MokaChartLook.Quiet).Add(x => x.DirectLabels, MokaDirectLabels.None).Add(x => x.Labels, labels ?? Three);
			parameters(p);
		});

	private static string? LineOf(IRenderedComponent<MokaLineChart> cut, string key) =>
		PathOf(Group(cut.Find(".moka-linechart"), key), "moka-linechart__line");

	private static DateTimeOffset[] Hours(int count) =>
		[.. Enumerable.Range(0, count).Select(i => new DateTimeOffset(2026, 11, 11, i, 0, 0, TimeSpan.Zero))];
}
