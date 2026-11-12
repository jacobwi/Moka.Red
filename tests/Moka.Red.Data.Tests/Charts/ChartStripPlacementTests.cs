using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using Moka.Red.Tests.Shared;
using static Moka.Red.Data.Tests.Charts.MokaLineChartTestKit;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     The range strip lies under the plot, lined up with the plot box rather than the whole frame, and its overview
///     places each value at its time on a time axis.
/// </summary>
public sealed class ChartStripPlacementTests : BunitContext
{
	private const string Plot = "MokaChartPlot.razor.css";
	private static readonly DateTimeOffset Start = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
	private static readonly string[] Hours = ["00:00", "01:00", "02:00", "03:00"];

	public ChartStripPlacementTests()
	{
		this.Static();
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void OnePlot_DrawsTheStripUnderItsOwnGrid_NotTheFrame()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Zoom, MokaChartZoom.Strip)
			.Add(x => x.Height, "120px")
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));

		IElement plot = cut.Find(".moka-chart-plot");
		Assert.Contains("moka-chart-plot--strip", plot.ClassList);
		IElement strip = cut.Find(".moka-chart-plot__strip");
		Assert.Contains("moka-chart-plot--strip", strip.ParentElement!.ClassList);
		Assert.Equal("skip", strip.GetAttribute("data-moka-export"));
		Assert.NotNull(strip.QuerySelector(".moka-chart-strip .moka-range-bar"));
		Assert.Empty(cut.FindAll(".moka-chart-frame__strip"));
		Assert.Empty(cut.FindAll(".moka-chart-plot__grid .moka-chart-plot__strip"));
	}

	[Fact]
	public void SmallMultiples_LeaveTheStripToTheFrame()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Zoom, MokaChartZoom.Strip)
			.Add(x => x.Split, true)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4), Series("WAN", 4, 3, 2, 1)]));

		Assert.Single(cut.FindAll(".moka-chart-frame__strip .moka-chart-strip"));
		Assert.Empty(cut.FindAll(".moka-chart-plot__strip"));
		Assert.All(cut.FindAll(".moka-chart-plot"), plot => Assert.DoesNotContain("moka-chart-plot--strip", plot.ClassList));
	}

	[Fact]
	public void WithoutAStrip_ThePlotIsAsItWas()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));

		Assert.DoesNotContain("moka-chart-plot--strip", cut.Find(".moka-chart-plot").ClassList);
		Assert.Empty(cut.FindAll(".moka-chart-strip"));
	}

	[Fact]
	public void OnATimeAxis_TheOverviewPlacesEachValueAtItsTime()
	{
		DateTimeOffset[] times = [Start, Start.AddHours(1), Start.AddHours(5), Start.AddHours(6)];
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Zoom, MokaChartZoom.Strip)
			.Add(x => x.Times, times)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));

		MokaSparkline overview = cut.FindComponent<MokaSparkline>().Instance;
		Assert.Equal(times.Select(t => (double)t.ToUnixTimeMilliseconds()), overview.Positions);
		Assert.Equal(times[0].ToUnixTimeMilliseconds(), overview.PositionMin);
		Assert.Equal(times[^1].ToUnixTimeMilliseconds(), overview.PositionMax);

		string line = cut.Find(".moka-sparkline-line").GetAttribute("d")!;
		Assert.StartsWith("M 0.00 ", line, StringComparison.Ordinal);
		Assert.Contains(" L 20.00 ", line, StringComparison.Ordinal);
		Assert.Contains(" L 100.00 ", line, StringComparison.Ordinal);
	}

	[Fact]
	public void OnATimeAxisPinnedPastItsData_TheOverviewSpansThePinnedEnds()
	{
		DateTimeOffset[] times = [Start.AddHours(1), Start.AddHours(2), Start.AddHours(3)];
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Zoom, MokaChartZoom.Strip)
			.Add(x => x.Times, times)
			.Add(x => x.XAxis, new MokaChartAxis { MinTime = Start, MaxTime = Start.AddHours(4) })
			.Add(x => x.Series, [Series("LAN", 1, 2, 3)]));

		MokaSparkline overview = cut.FindComponent<MokaSparkline>().Instance;
		Assert.Equal(Start.ToUnixTimeMilliseconds(), overview.PositionMin);
		Assert.Equal(Start.AddHours(4).ToUnixTimeMilliseconds(), overview.PositionMax);
		Assert.StartsWith("M 30.00 ", cut.Find(".moka-sparkline-line").GetAttribute("d"), StringComparison.Ordinal);
	}

	[Fact]
	public void OnCategories_TheOverviewIsEvenlySpaced()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Zoom, MokaChartZoom.Strip)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 1, 2, 3, 4)]));

		Assert.Null(cut.FindComponent<MokaSparkline>().Instance.Positions);
	}

	[Fact]
	public void ALongSeries_IsCutDownByTime_KeepingAPositionForEveryValue()
	{
		DateTimeOffset[] times = [.. Enumerable.Range(0, 10_000).Select(i => Start.AddSeconds(i < 5_000 ? i : i * 10))];
		double[] values = [.. Enumerable.Range(0, 10_000).Select(i => Math.Sin(i / 50d))];
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Zoom, MokaChartZoom.Strip)
			.Add(x => x.Times, times)
			.Add(x => x.Series, [Series("LAN", values)]));

		IReadOnlyList<double> overview = cut.Instance.OverviewValues!;
		IReadOnlyList<double> positions = cut.Instance.OverviewPositions!;
		Assert.Equal(overview.Count, positions.Count);
		Assert.True(overview.Count < 2_000);
		int[] expected = MokaDownsampler.MinMax([.. times.Select(t => (double)t.ToUnixTimeMilliseconds())], values, 400);
		Assert.Equal(expected.Select(i => values[i]), overview);
	}

	[Fact]
	public void TheStrip_TakesThePlotsColumnsThroughASubgrid()
	{
		IReadOnlyDictionary<string, string> plot = ScopedCss.Declarations(Plot, ".moka-chart-plot--strip");
		Assert.Equal("grid", plot["display"]);
		Assert.Equal("[ystart] auto [plot] minmax(0, 1fr) [gutter] auto [yend] auto", plot["grid-template-columns"]);
		Assert.Equal("var(--_strip-reach)", plot["padding-inline"]);

		Assert.Equal("subgrid", ScopedCss.Declarations(Plot, ".moka-chart-plot--strip > .moka-chart-plot__grid")["grid-template-columns"]);
		IReadOnlyDictionary<string, string> strip = ScopedCss.Declarations(Plot, ".moka-chart-plot--strip > .moka-chart-plot__strip");
		Assert.Equal("plot", strip["grid-column"]);
		Assert.Contains("calc(-1 * var(--_strip-reach))", strip["margin"], StringComparison.Ordinal);
	}
}
