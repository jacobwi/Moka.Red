using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using static Moka.Red.Data.Tests.Charts.MokaLineChartTestKit;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     Where the chart packages meet: the words every chart writes, values at their own precision, the legend the frame
///     draws or leaves out, the range strip, the plot's height, long category axes, held live charts and the row plot's
///     thresholds.
/// </summary>
public sealed class ChartIntegrationTests : BunitContext
{
	private static readonly string[] Hours = ["00:00", "01:00", "02:00", "03:00"];
	private static readonly DateTimeOffset Day = new(2026, 3, 9, 0, 0, 0, TimeSpan.Zero);

	private static readonly MokaChartText German = MokaChartText.Default with
	{
		Name = "Name",
		Value = "Wert",
		Share = "Anteil",
		From = "Von",
		To = "Nach",
		Time = "Zeit",
		Low = name => $"{name} tief",
		High = name => $"{name} hoch",
		Index = name => $"{name} Index",
		Between = (from, to) => $"{from} bis {to}",
		MoreSeries = count => $"{count} weitere zeigen",
		FewerSeries = "Weniger zeigen",
		Series = "Reihen",
		LoadFailed = "Daten fehlen."
	};

	public ChartIntegrationTests() => this.StaticFrame();

	[Fact]
	public void TheWholes_NameTheirTableColumns_InTheChartsWords()
	{
		IRenderedComponent<MokaDonutChart> donut = Render<MokaDonutChart>(p => p
			.Add(x => x.Text, German)
			.Add(x => x.Segments, [new MokaDonutSegment { Name = "A", Value = 3 }, new MokaDonutSegment { Name = "B", Value = 1 }]));
		IRenderedComponent<MokaTreemap> treemap = Render<MokaTreemap>(p => p
			.Add(x => x.Text, German)
			.Add(x => x.Nodes, [new MokaTreemapNode("A", 3), new MokaTreemapNode("B", 1)]));
		IRenderedComponent<MokaSankey> sankey = Render<MokaSankey>(p => p
			.Add(x => x.Text, German)
			.Add(x => x.Links, [new MokaSankeyLink("A", "B", 3)]));

		Assert.Equal(["Name", "Wert", "Anteil"], donut.Instance.TableModel!.Headers);
		Assert.Equal(["Name", "Wert", "Anteil"], treemap.Instance.TableModel!.Headers);
		Assert.Equal(["Von", "Nach", "Wert"], sankey.Instance.TableModel!.Headers);
	}

	[Fact]
	public void ALineChartsTable_NamesTimesBandsAndIndexes_InTheChartsWords()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Text, German)
			.Add(x => x.IndexTo, 0)
			.Add(x => x.Times, [Day, Day.AddHours(1), Day.AddHours(2)])
			.Add(x => x.Series, [Series("LAN", 10, 20, 30) with { Low = [9, 18, 27], High = [11, 22, 33] }]));

		Assert.Equal(["Zeit", "LAN", "LAN tief", "LAN hoch", "LAN Index"], cut.Instance.TableModel!.Headers);
	}

	[Fact]
	public void AnEventsSpan_IsWrittenWithTheChartsWord()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Text, German)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Annotations, [new MokaChartEvent(1) { To = 2, Label = "Deploy" }])
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));

		PlotEvent happened = Assert.Single(cut.Instance.ColumnPlotModel(0).Annotations.Events);
		Assert.Equal("01:00 bis 02:00", happened.When);
	}

	[Fact]
	public async Task TheLegendsFoldButtons_AndItsName_TakeTheChartsWords_UnlessTheLegendNamesThem()
	{
		MokaChartLegendItem[] items = [new("a", "A"), new("b", "B"), new("c", "C")];
		IRenderedComponent<MokaChartLegend> cut = Render<MokaChartLegend>(p => p
			.Add(x => x.Text, German)
			.Add(x => x.FoldAbove, 1)
			.Add(x => x.Items, items));

		await cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(2));
		Assert.Equal("Reihen", cut.Find(".moka-chart-legend").GetAttribute("aria-label"));
		Assert.Equal("2 weitere zeigen", cut.Find("[data-moka-more]").GetAttribute("aria-label"));

		await cut.Find("[data-moka-more]").ClickAsync(new MouseEventArgs());
		Assert.Equal("Weniger zeigen", cut.Find("[data-moka-more]").GetAttribute("aria-label"));

		cut.Render(p => p.Add(x => x.Text, German).Add(x => x.FoldAbove, 1).Add(x => x.Items, items)
			.Add(x => x.LessLabel, "Zuklappen"));
		Assert.Equal("Zuklappen", cut.Find("[data-moka-more]").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheErrorState_SaysTheChartsWord_WithoutErrorText()
	{
		IRenderedComponent<FrameChart> cut = Render<FrameChart>(p => p
			.Add(x => x.Text, German)
			.Add(x => x.State, MokaChartState.Error)
			.Add(x => x.Keys, ["LAN"])
			.Add(x => x.Data, [[1d, 2]]));

		Assert.Contains("Daten fehlen.", cut.Find(".moka-chart-frame__state--error").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void TheLegendsDoubleClick_IsolatesAnEntry_AndASecondOneBringsTheRestBack()
	{
		IReadOnlyCollection<string> hidden = [];
		IRenderedComponent<MokaChartLegend> cut = Render<MokaChartLegend>(p => p
			.Add(x => x.Actions, MokaChartLegendActions.Isolate)
			.Add(x => x.Items, [new MokaChartLegendItem("a", "A"), new("b", "B"), new("c", "C")])
			.Add(x => x.HiddenChanged, (IReadOnlyCollection<string> keys) => hidden = keys));

		DoubleClick(cut, "b");
		Assert.Equal(["a", "c"], hidden.Order());

		DoubleClick(cut, "b");
		Assert.Empty(hidden);

		static void DoubleClick(IRenderedComponent<MokaChartLegend> legend, string key)
		{
			legend.Find($"[data-moka-series='{key}']").Click(new MouseEventArgs { Detail = 1 });
			legend.Find($"[data-moka-series='{key}']").Click(new MouseEventArgs { Detail = 2 });
		}
	}

	[Fact]
	public void TooltipsAndTables_WriteAValueAtItsOwnPrecision_WhileTicksKeepTheStep()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Unit, MokaChartUnit.Bytes)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 26.4e9, 10e9, 5e9, 30e9)]));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 0, null)!;
		Assert.Equal("26.4 GB", info.Rows[0].ValueText);
		Assert.Equal("26.4 GB", info.Context.Points[0].ValueText);
		Assert.Equal("26.4 GB", cut.Instance.TableModel!.Display(0, 1));
		Assert.All(cut.Instance.ColumnPlotModel(0).ValueTicks.Skip(1), tick => Assert.EndsWith("GB", tick.Tick.Text, StringComparison.Ordinal));
		Assert.DoesNotContain(cut.Instance.ColumnPlotModel(0).ValueTicks, tick => tick.Tick.Text.Contains("26.4", StringComparison.Ordinal));
	}

	[Fact]
	public void ABarsTooltip_AndItsPrintedValue_KeepTheirPrecision()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Unit, MokaChartUnit.Milliseconds)
			.Add(x => x.Labels, ["a", "b"])
			.Add(x => x.Series, [new MokaBarSeries { Name = "Took", Values = [1_234, 400] }]));

		Assert.Equal("1.2 s", cut.Instance.DescribeActive(0, -1, 0, null)!.Rows[0].ValueText);
	}

	[Fact]
	public void TheRowValueAxis_IsWrittenByTheChartsFormatter()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p
			.Add(x => x.Formatter, (Func<double, string>)Points)
			.Add(x => x.Rows, [new MokaDotPlotRow("a", 12), new MokaDotPlotRow("b", 40)]));

		IReadOnlyList<PlotTick> ticks = cut.Instance.RowPlotModel().ValueTicks;
		Assert.NotEmpty(ticks);
		Assert.All(ticks, tick => Assert.EndsWith(" pts", tick.Text, StringComparison.Ordinal));

		static string Points(double value) => value.ToString("0", CultureInfo.InvariantCulture) + " pts";
	}

	[Fact]
	public void OneSeries_HasNoLegend_UnlessAskedFor_OrHidden()
	{
		IRenderedComponent<FrameChart> one = Render<FrameChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Title, "LAN")
			.Add(x => x.Keys, ["LAN"])
			.Add(x => x.Data, [[1d, 2]]));
		IRenderedComponent<FrameChart> asked = Render<FrameChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Legend, MokaChartLegendPosition.Bottom)
			.Add(x => x.Keys, ["LAN"])
			.Add(x => x.Data, [[1d, 2]]));
		IRenderedComponent<FrameChart> hidden = Render<FrameChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.HiddenSeries, ["LAN"])
			.Add(x => x.Keys, ["LAN"])
			.Add(x => x.Data, [[1d, 2]]));
		IRenderedComponent<FrameChart> two = Render<FrameChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Keys, ["LAN", "WAN"])
			.Add(x => x.Data, [[1d, 2], [3d, 4]]));

		Assert.Empty(one.FindAll(".moka-chart-legend"));
		Assert.Single(asked.FindAll(".moka-chart-legend__item"));
		Assert.Single(hidden.FindAll(".moka-chart-legend__item"));
		Assert.Equal(2, two.FindAll(".moka-chart-legend__item").Count);
	}

	[Fact]
	public void ADonutBar_TakesTheFramesLegend_WhileTheRingListsItsOwn()
	{
		MokaDonutSegment[] segments = [new() { Name = "A", Value = 3 }, new() { Name = "B", Value = 1 }];
		IRenderedComponent<MokaDonutChart> bar = Render<MokaDonutChart>(p => p
			.Add(x => x.Shape, MokaDonutShape.Bar)
			.Add(x => x.Segments, segments));
		IRenderedComponent<MokaDonutChart> waffle = Render<MokaDonutChart>(p => p
			.Add(x => x.Shape, MokaDonutShape.Waffle)
			.Add(x => x.Segments, segments));

		Assert.Equal(2, bar.FindAll(".moka-chart-legend__item").Count);
		Assert.Empty(waffle.FindAll(".moka-chart-legend"));
		Assert.Equal(2, waffle.Instance.SeriesInfo.Count);
		Assert.Equal("A", waffle.Find(".moka-donutchart-group").GetAttribute("data-moka-key"));
	}

	[Fact]
	public void TheRangeStrip_ShowsOnlyOnAChartThatZoomsAlongItsXAxis()
	{
		IRenderedComponent<MokaLineChart> line = Render<MokaLineChart>(p => p
			.Add(x => x.Zoom, MokaChartZoom.Strip)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));
		IRenderedComponent<MokaBarChart> bars = Render<MokaBarChart>(p => p
			.Add(x => x.Zoom, MokaChartZoom.Strip)
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [new MokaBarSeries { Name = "A", Values = [1, 2, 3, 4] }]));
		IRenderedComponent<MokaGanttChart> rows = Render<MokaGanttChart>(p => p
			.Add(x => x.Zoom, MokaChartZoom.Strip)
			.Add(x => x.Now, Day.AddHours(3))
			.Add(x => x.Rows, [new MokaGanttRow("a", Day) { End = Day.AddHours(1) }, new MokaGanttRow("b", Day.AddHours(1)) { End = Day.AddHours(2) }]));

		Assert.Single(line.FindAll(".moka-chart-strip"));
		Assert.Empty(bars.FindAll(".moka-chart-strip"));
		Assert.Empty(rows.FindAll(".moka-chart-strip"));
	}

	[Fact]
	public void AScatterPoint_SaysWhereItSits_AndHowBigItIs()
	{
		IRenderedComponent<MokaScatterChart> cut = Render<MokaScatterChart>(p => p
			.Add(x => x.Series, [new MokaScatterSeries("a", [new MokaScatterPoint(3, 4, 12)])]));

		MokaChartPoint point = cut.Instance.DescribeActive(0, 0, 0, null)!.Context.Focus!;

		Assert.Equal(3, point.X);
		Assert.Equal(12, point.Size);
	}

	[Fact]
	public void TheOtherCharts_SayWhereAPointSitsAlongX()
	{
		IRenderedComponent<MokaLineChart> line = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Times, [Day, Day.AddHours(1)])
			.Add(x => x.Series, [Series("LAN", 1, 2)]));
		IRenderedComponent<MokaBarChart> bars = Render<MokaBarChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [new MokaBarSeries { Name = "A", Values = [1, 2, 3, 4] }]));

		Assert.Equal(MokaChartX.FromDateTimeOffset(Day.AddHours(1)).Value, line.Instance.DescribeActive(0, -1, 1, null)!.Context.Points[0].X);
		Assert.Equal(2, bars.Instance.DescribeActive(0, -1, 2, null)!.Context.Points[0].X);
	}

	[Theory]
	[InlineData(MokaSize.Xs, "6rem")]
	[InlineData(MokaSize.Sm, "9rem")]
	[InlineData(MokaSize.Md, "14rem")]
	[InlineData(MokaSize.Lg, "19rem")]
	public void TheColumnPlot_IsNoTallerThanItsSizeAllows(MokaSize size, string most)
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Size, size)
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [new MokaBarSeries { Name = "A", Values = [1, 2, 3, 4] }]));

		Assert.Equal(most, RowChartKit.Var(cut.Find(".moka-chart-plot__grid"), "--_plot-max"));
	}

	[Fact]
	public void AFixedHeight_TakesThePlotOutOfItsCap()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Height, "200px")
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));

		Assert.Null(RowChartKit.Var(cut.Find(".moka-chart-plot__grid"), "--_plot-max"));
	}

	[Fact]
	public void AMillionCategories_LayOutOnlyAStrideOfLabels_KeepingTheEnds()
	{
		IRenderedComponent<CountingAxisChart> cut = Render<CountingAxisChart>(p => p.Add(x => x.Count, 1_000_000));

		IReadOnlyList<PlotLabel> labels = cut.Instance.ColumnPlotModel(0).X.Labels;

		Assert.InRange(cut.Instance.TextsAsked, 1, 2_000);
		Assert.InRange(labels.Count, 2, 600);
		Assert.Equal("c0", labels[0].Text);
		Assert.Equal("c999999", labels[^1].Text);
	}

	[Fact]
	public void AFewHundredCategories_AreAllLaidOut()
	{
		IRenderedComponent<CountingAxisChart> cut = Render<CountingAxisChart>(p => p.Add(x => x.Count, 300));

		_ = cut.Instance.ColumnPlotModel(0);

		Assert.Equal(300, cut.Instance.TextsAsked);
	}

	[Fact]
	public void AnAxisThatSaysSo_ReachesZero_OrLeavesIt()
	{
		IRenderedComponent<MokaLineChart> hugs = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.YAxis, new MokaChartAxis { IncludeZero = false, Nice = true })
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 52, 60, 70, 88)]));
		IRenderedComponent<MokaCandlestickChart> prices = Render<MokaCandlestickChart>(p => p
			.Add(x => x.YAxis, new MokaChartAxis { IncludeZero = true })
			.Add(x => x.Candles, [new MokaCandle(Day, 100, 104, 98, 102), new MokaCandle(Day.AddDays(1), 102, 106, 101, 105)]));
		IRenderedComponent<MokaDotPlot> dots = Render<MokaDotPlot>(p => p
			.Add(x => x.XAxis, new MokaChartAxis { IncludeZero = true })
			.Add(x => x.Rows, [new MokaDotPlotRow("a", 52), new MokaDotPlotRow("b", 88)]));

		Assert.True(hugs.Instance.ColumnPlotModel(0).Scale.Min >= 50);
		Assert.Equal(0, prices.Instance.ColumnPlotModel(0).Scale.Min);
		Assert.Equal(0, dots.Instance.RowPlotModel().Scale!.Min);
	}

	[Fact]
	public void ARowThresholdFarAlong_LabelsItselfOnItsLeft()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Render<ChartAxisTestChart>(p => p
			.Add(x => x.Rows, true)
			.Add(x => x.Labels, ["a", "b"])
			.Add(x => x.Values, [0d, 10])
			.Add(x => x.Annotations, [new MokaChartThreshold(9) { Key = "far" }, new MokaChartThreshold(2) { Key = "near" }]));

		IReadOnlyList<IElement> lines = cut.FindAll(".moka-chart-plot__vthreshold");
		Assert.Equal(2, lines.Count);
		Assert.Contains("moka-chart-plot__vthreshold--start", lines[0].ClassList);
		Assert.DoesNotContain("moka-chart-plot__vthreshold--start", lines[1].ClassList);
	}

	[Fact]
	public void AChartOfParts_FindsThePartUnderThePointer_ByItsMark_AndLetsThePointerThrough()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<MokaDonutChart> donut = Render<MokaDonutChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Segments, [new MokaDonutSegment { Name = "A", Value = 3 }, new MokaDonutSegment { Name = "B", Value = 1 }]));

		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(
			Assert.Single(module.Invocations, i => i.Identifier == "bind").Arguments[2]);
		IElement surface = donut.Find(".moka-chart-surface");
		Assert.Equal(true, options["hit"]);
		Assert.Equal("column", options["mode"]);
		Assert.Contains("moka-chart-surface--hit", surface.ClassList);
		Assert.Equal("2", surface.GetAttribute("data-moka-count"));
		Assert.EndsWith("Left and Right move between parts.", donut.Find("[role=grid]").GetAttribute("aria-label"), StringComparison.Ordinal);
		Assert.Equal(["0", "1"], donut.FindAll(".moka-donutchart-segment").Select(s => s.GetAttribute("data-i")));
	}

	[Fact]
	public void AHeldLiveChart_KeepsItsGeometryVersion_UntilItLetsGo()
	{
		this.Interactive();
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Live, true)
			.Add(x => x.PauseWhilePointing, true)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));
		long before = cut.Instance.GeometryVersion;

		cut.InvokeAsync(() => cut.Instance.OnHold(true));
		cut.Render(p => p.Add(x => x.Look, MokaChartLook.Quiet).Add(x => x.Live, true).Add(x => x.PauseWhilePointing, true)
			.Add(x => x.Labels, Hours).Add(x => x.Series, [Series("LAN", 2, 3, 4, 5)]));

		Assert.Equal(before, cut.Instance.GeometryVersion);
		Assert.Equal(before.ToString(CultureInfo.InvariantCulture), cut.Find(".moka-chart-surface").GetAttribute("data-moka-version"));

		cut.InvokeAsync(() => cut.Instance.OnHold(false));

		Assert.True(cut.Instance.GeometryVersion > before);
	}
}

/// <summary>A chart with as many categories as asked, each labelled, that counts how many labels it was asked for.</summary>
public sealed class CountingAxisChart : MokaCartesianChartBase
{
	/// <summary>How many categories.</summary>
	[Parameter]
	public int Count { get; set; }

	/// <summary>How many times a category's label was asked for.</summary>
	public int TextsAsked { get; private set; }

	/// <inheritdoc />
	protected override string RootClass => "counting-axis-chart";

	internal override bool HasData => Count > 0;

	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => [];

	internal override int CategoryCount => Count;

	internal override string CategoryText(int index)
	{
		TextsAsked++;
		return "c" + index.ToString(CultureInfo.InvariantCulture);
	}

	internal override (double Min, double Max) ValueRange(int panel) => (0, 1);

	internal override void Rebuild() => TextsAsked = 0;

	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn) => null;

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
	}
}
