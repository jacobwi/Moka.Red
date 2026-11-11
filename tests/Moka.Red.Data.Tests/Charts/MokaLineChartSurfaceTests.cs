using System.Globalization;
using Bunit;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using static Moka.Red.Data.Tests.Charts.MokaLineChartTestKit;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>What the modern line chart tells its surface and frame: hover geometry, tooltips, the table, the strip, live hold.</summary>
public sealed class MokaLineChartSurfaceTests : BunitContext
{
	private static readonly string[] Hours = ["00:00", "01:00", "02:00", "03:00"];
	private static readonly DateTimeOffset Start = new(2026, 11, 11, 14, 0, 0, TimeSpan.Zero);

	private readonly BunitJSModuleInterop _module;

	public MokaLineChartSurfaceTests() => _module = this.Interactive();

	[Fact]
	public void AnInteractiveChart_BindsOnce_WithItsOwnHooks()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Live, true)
			.Add(x => x.PauseWhilePointing, true)
			.Add(x => x.Zoom, MokaChartZoom.Brush | MokaChartZoom.Wheel)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));

		JSRuntimeInvocation bind = Assert.Single(_module.Invocations, i => i.Identifier == "bind");
		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(bind.Arguments[2]);
		Assert.Equal("crosshair", options["mode"]);
		Assert.Equal(true, options["hold"]);
		Assert.Equal(3, options["zoom"]);
		Assert.Equal(false, options["measure"]);
		Assert.Equal("figure", cut.Find(".moka-linechart").GetAttribute("role"));
		Assert.Single(cut.FindAll(".moka-chart-surface"));
	}

	[Fact]
	public void ANewSample_SendsTheScriptNothing()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));
		int calls = _module.Invocations.Count;

		cut.Render(p => p.Add(x => x.Look, MokaChartLook.Quiet).Add(x => x.Labels, Hours).Add(x => x.Series, [Series("LAN", 2, 3, 4, 5)]));

		Assert.Equal(calls, _module.Invocations.Count);
	}

	[Fact]
	public void HoverGeometry_PlacesEachDrawnPoint_AndAHiddenSeriesIsAllGap()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Min, 0d)
			.Add(x => x.Max, 4d)
			.Add(x => x.HiddenSeries, ["Wi-Fi"])
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 0, 1, 2, 4), Series("Wi-Fi", 1, 1, 1, 1)]));

		ChartHoverData data = cut.Instance.HoverData(0)!;
		Assert.Equal([0f, 1f / 3, 2f / 3, 1f], data.X.ToArray(), (a, b) => Math.Abs(a - b) < 1e-5);
		Assert.Equal([0f, 0.25f, 0.5f, 1f], data.Y[0].ToArray());
		Assert.All(data.Y[1].ToArray(), y => Assert.True(float.IsNaN(y)));
		Assert.Equal(4, cut.Instance.DrawnCount(0));
		Assert.Equal(3, cut.Instance.LatestDrawn(0));
		Assert.Equal((0d, 3d), cut.Instance.XExtent(0));
	}

	[Fact]
	public void AWholeColumn_ListsEveryShownSeries_WithItsValue()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Unit, MokaChartUnit.Of("Mbps"))
			.Add(x => x.Series, [Series("LAN", 10, 20, 30, 40), Series("Wi-Fi", 5, 6, 7, 8)]));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 2, null)!;

		Assert.Equal("02:00", info.Heading);
		Assert.Equal(["LAN 30 Mbps", "Wi-Fi 7 Mbps"], info.Rows.Select(row => $"{row.Name} {row.ValueText}"));
		Assert.Equal("02:00, LAN 30 Mbps, Wi-Fi 7 Mbps", info.CellText);
		Assert.Null(cut.Instance.DescribeActive(0, -1, 4, null));
		Assert.Null(cut.Instance.DescribeActive(0, -1, -1, null));
	}

	[Fact]
	public void OnePoint_IsInFocus_AndItsCellNamesItsSeries()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 10, 20, 30, 40), Series("Wi-Fi", 5, 6, 7, 8)]));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, 1, 3, null)!;

		Assert.True(info.Rows[1].Emphasis);
		Assert.Equal("Wi-Fi, 03:00, 8", info.CellText);
		Assert.Equal("Wi-Fi", info.Context.Focus?.SeriesKey);
		Assert.Equal(3, info.Context.Focus?.Index);
		Assert.Equal("03:00", info.Context.Focus?.Category);
	}

	[Fact]
	public void APin_AddsTheChangeSinceIt()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 10, 20, 30, 40), Series("Wi-Fi", 8, 6, 7, 5)]));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 3, 0)!;

		Assert.Equal(["+30", "-3"], info.Rows.Select(row => row.DeltaText));
		Assert.Equal(30, info.Context.Points[0].SincePin);
	}

	[Fact]
	public void AStack_ListsTheTopFirst_AndTheTotal()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Stacked, true)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 10, 20, 30, 40), Series("Wi-Fi", 5, 6, 7, 8)]));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 1, null)!;

		Assert.Equal(["Wi-Fi 6", "LAN 20", "Total 26"], info.Rows.Select(row => $"{row.Name} {row.ValueText}"));
		Assert.Equal(26, info.Context.Points[0].Total);
		ChartHoverData data = cut.Instance.HoverData(0)!;
		Assert.True(data.Y[1].Span[1] > data.Y[0].Span[1]);
	}

	[Fact]
	public void ABand_AddsItsSpread_AndThePointCarriesItsLowAndHigh()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Unit, MokaChartUnit.Milliseconds)
			.Add(x => x.Series, [Series("Median", 100, 110, 120, 130) with { Low = [50, 55, 60, 65], High = [180, 190, 200, 260] }]));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 2, null)!;

		Assert.Contains("5th 60 ms to 95th 200 ms", info.Notes);
		Assert.Equal(60, info.Context.Points[0].Low);
		Assert.Equal(200, info.Context.Points[0].High);
	}

	[Fact]
	public void AnIndexedPoint_SaysItsRawValueToo()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.IndexTo, 100d)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("Sessions", 18400, 20000, 25000, 31832)]));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 3, null)!;

		ChartActiveRow row = Assert.Single(info.Rows);
		Assert.Equal("173", row.ValueText);
		Assert.StartsWith("Sessions, ", row.Name, StringComparison.Ordinal);
		Assert.Contains("31", row.Name, StringComparison.Ordinal);
		Assert.Equal(31832, info.Context.Points[0].Value);
	}

	[Fact]
	public void Annotations_AddTheirNotes()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Hours)
			.Add(x => x.TargetLow, 100d)
			.Add(x => x.TargetHigh, 250d)
			.Add(x => x.Annotations, [new MokaChartThreshold(200) { Label = "Limit" }])
			.Add(x => x.Unit, MokaChartUnit.Milliseconds)
			.Add(x => x.Series, [Series("p95", 150, 212, 280, 90)]));

		Assert.Contains("Inside target", cut.Instance.DescribeActive(0, -1, 0, null)!.Notes);
		Assert.Contains(cut.Instance.DescribeActive(0, -1, 1, null)!.Notes, note => note.Contains("12 ms over", StringComparison.Ordinal));
		Assert.Contains(cut.Instance.DescribeActive(0, -1, 2, null)!.Notes, note => note.Contains("above target", StringComparison.Ordinal));
	}

	[Fact]
	public void ATimeAxis_HeadsEachPointWithItsTime()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.TimeZone, TimeZoneInfo.Utc)
			.Add(x => x.Times, [Start, Start.AddMinutes(30), Start.AddHours(1)])
			.Add(x => x.Series, [Series("LAN", 1, 2, 3)]));

		string heading = cut.Instance.DescribeActive(0, -1, 1, null)!.Heading!;

		Assert.Equal(new DateTime(2026, 11, 11, 14, 30, 0).ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern,
			CultureInfo.CurrentCulture), heading);
	}

	[Fact]
	public void TooltipLabel_StillWinsForTheHeading()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Hours)
			.Add(x => x.TooltipLabel, index => $"Sample {index}")
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));

		Assert.Equal("Sample 2", cut.Instance.DescribeActive(0, -1, 2, null)!.Heading);
	}

	[Fact]
	public void SmallMultiples_GiveEachPanelItsOwnSeries_AndTheRestAreGaps()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Split, true)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4), Series("Wi-Fi", 4, 3, 2, 1)]));

		ChartHoverData second = cut.Instance.HoverData(1)!;
		Assert.All(second.Y[0].ToArray(), y => Assert.True(float.IsNaN(y)));
		Assert.False(float.IsNaN(second.Y[1].Span[0]));
		Assert.Equal(2, cut.FindAll(".moka-chart-surface").Count);
		ChartActiveInfo info = cut.Instance.DescribeActive(1, -1, 0, null)!;
		Assert.True(info.Rows.Single(row => row.Emphasis).Name == "Wi-Fi");
	}

	[Fact]
	public void TheTable_HasAColumnPerShownSeries_AndRawValuesForCsv()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Stacked, true)
			.Add(x => x.Times, [Start, Start.AddHours(1)])
			.Add(x => x.Series, [Series("LAN", 1.5, double.NaN), Series("Wi-Fi", 2, 3)]));

		ChartTableModel table = cut.Instance.TableModel!;

		Assert.Equal(["Time", "LAN", "Wi-Fi", "Total"], table.Headers);
		Assert.Equal(2, table.RowCount);
		Assert.Equal(Start.ToString("o", CultureInfo.InvariantCulture), table.Raw(0, 0));
		Assert.Equal("1.5", table.Raw(0, 1));
		Assert.Equal(string.Empty, table.Raw(1, 1));
		Assert.Equal("3.5", table.Raw(0, 3));
	}

	[Fact]
	public void TheTable_AddsBandAndIndexColumns()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.IndexTo, 100d)
			.Add(x => x.Labels, ["a", "b"])
			.Add(x => x.Series, [Series("Median", 10, 20) with { Low = [5, 10], High = [15, 30] }]));

		ChartTableModel table = cut.Instance.TableModel!;

		Assert.Equal(["Name", "Median", "Median low", "Median high", "Median index"], table.Headers);
		Assert.Equal("200", table.Raw(1, 4));
	}

	[Fact]
	public void TheRangeStrip_DrawsTheFirstShownSeries()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Zoom, MokaChartZoom.Strip)
			.Add(x => x.HiddenSeries, ["LAN"])
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4), Series("Wi-Fi", 4, 3, 2, 1)]));

		Assert.Equal([4d, 3, 2, 1], cut.Instance.OverviewValues);
		Assert.Single(cut.FindAll(".moka-chart-frame__strip"));
	}

	[Fact]
	public void TheLegend_TakesEverySeries_WithItsKeyAndSlot()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Dense)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4) with { Key = "lan", Slot = 4 }, Series("Wi-Fi", 4, 3, 2, 1)]));

		IReadOnlyList<ChartSeriesInfo> info = cut.Instance.SeriesInfo;
		Assert.Equal(["lan", "Wi-Fi"], info.Select(series => series.Key));
		Assert.Equal([4, 2], info.Select(series => series.Slot));
		Assert.Equal(["4", "1"], info.Select(series => series.CurrentText));
	}

	[Fact]
	public void WhileHeld_ALiveChartKeepsItsDrawing_CountsWhatWaits_AndCatchesUpWhenLetGo()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Live, true)
			.Add(x => x.PauseWhilePointing, true)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));
		string before = Line(cut);

		cut.InvokeAsync(() => cut.Instance.OnHold(true));
		cut.Render(p => p.Add(x => x.Look, MokaChartLook.Quiet).Add(x => x.Live, true).Add(x => x.PauseWhilePointing, true)
			.Add(x => x.Labels, Hours).Add(x => x.Series, [Series("LAN", 2, 3, 4, 5)]));
		cut.Render(p => p.Add(x => x.Look, MokaChartLook.Quiet).Add(x => x.Live, true).Add(x => x.PauseWhilePointing, true)
			.Add(x => x.Labels, Hours).Add(x => x.Series, [Series("LAN", 3, 4, 5, 9)]));

		Assert.Equal(before, Line(cut));
		Assert.Equal(2, cut.Instance.Waiting);
		Assert.Contains("2 new", cut.Find(".moka-chart-live").TextContent, StringComparison.Ordinal);

		cut.InvokeAsync(() => cut.Instance.OnHold(false));

		Assert.NotEqual(before, Line(cut));
		Assert.Equal(0, cut.Instance.Waiting);
	}

	[Fact]
	public void ABrush_ZoomsTheChart_AndTellsTheParent()
	{
		MokaChartRange? told = null;
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Zoom, MokaChartZoom.Brush)
			.Add(x => x.RangeChanged, range => told = range)
			.Add(x => x.Labels, ["a", "b", "c", "d", "e", "f", "g", "h", "i"])
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4, 5, 6, 7, 8, 9)]));

		cut.InvokeAsync(() => cut.Instance.OnRange(2, 5));

		Assert.Equal(new MokaChartRange(2, 5), told);
		IReadOnlyList<(char Command, int X, int Y)> points = Points(Line(cut));
		Assert.Equal(6, points.Count);
		Assert.Equal(-3333, points[0].X);
		Assert.Equal(13333, points[^1].X);
	}

	private static string Line(IRenderedComponent<MokaLineChart> cut) =>
		PathOf(Group(cut.Find(".moka-linechart"), "LAN"), "moka-linechart__line") ?? string.Empty;
}
