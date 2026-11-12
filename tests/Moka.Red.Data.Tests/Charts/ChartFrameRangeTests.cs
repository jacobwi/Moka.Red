using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Charts;
using Moka.Red.Data.Chart;
using Moka.Red.Forms.RangeBar;
using Moka.Red.Primitives.SegmentedControl;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     Zooming from the frame: range presets in the toolbar, Reset zoom while zoomed by hand, and the range strip under
///     the plot. Each goes through the same path a brush does, so the chart keeps one zoom.
/// </summary>
public sealed class ChartFrameRangeTests : BunitContext
{
	private static readonly IReadOnlyList<MokaChartRangePreset> Presets =
	[
		new("1 h", TimeSpan.FromHours(1)),
		new("24 h", TimeSpan.FromHours(24)),
		new("7 d", TimeSpan.FromDays(7))
	];

	private static readonly IReadOnlyList<DateTimeOffset> Week = ChartFrameTestKit.Hours(169);

	[Fact]
	public async Task APreset_ZoomsToTheLastSpanOfTheData_AndSaysWhich()
	{
		Services.AddSingleton<TimeProvider>(new FixedClock(Week[^1].AddDays(3)));
		this.StaticFrame();
		string? preset = null;
		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.Controls, MokaChartControls.Ranges)
			.Add(x => x.RangePresetChanged, label => preset = label));

		await cut.InvokeAsync(() => cut.FindComponent<MokaSegmentedControl>().Instance.ValueChanged.InvokeAsync("24 h"));

		Assert.Equal("24 h", preset);
		(double from, double to) = Assert.Single(cut.Instance.Chosen);
		Assert.Equal(MokaChartX.FromDateTimeOffset(Week[^1]).Value, to);
		Assert.Equal(MokaChartX.FromDateTimeOffset(Week[^1].AddHours(-24)).Value, from);
		Assert.Equal("24 h", cut.FindComponent<MokaSegmentedControl>().Instance.Value);
	}

	[Fact]
	public async Task APreset_EndsNow_WhenTheDataRunsPastNow()
	{
		DateTimeOffset now = Week[100];
		Services.AddSingleton<TimeProvider>(new FixedClock(now));
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Controls, MokaChartControls.Ranges));

		await cut.InvokeAsync(() => cut.FindComponent<MokaSegmentedControl>().Instance.ValueChanged.InvokeAsync("1 h"));

		Assert.Equal((MokaChartX.FromDateTimeOffset(now.AddHours(-1)).Value, MokaChartX.FromDateTimeOffset(now).Value), Assert.Single(cut.Instance.Chosen));
	}

	[Fact]
	public async Task ResetZoom_ShowsOnlyWhileZoomedByHand()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Controls, MokaChartControls.ResetZoom | MokaChartControls.Ranges));
		Assert.Empty(cut.Find("[data-moka-control='reset-zoom']").Children);

		await cut.InvokeAsync(() => cut.FindComponent<MokaSegmentedControl>().Instance.ValueChanged.InvokeAsync("1 h"));
		Assert.Empty(cut.Find("[data-moka-control='reset-zoom']").Children);

		await cut.InvokeAsync(() => cut.Instance.Base.ChooseRangeAsync(Week[10].ToUnixTimeMilliseconds(), Week[20].ToUnixTimeMilliseconds()));
		Assert.Equal("Reset zoom", cut.Find("[data-moka-control='reset-zoom'] button").TextContent.Trim());
		Assert.Null(cut.FindComponent<MokaSegmentedControl>().Instance.Value);

		await cut.Find("[data-moka-control='reset-zoom'] button").ClickAsync(new MouseEventArgs());
		(double from, double to) = cut.Instance.Chosen[^1];
		Assert.True(double.IsNaN(from) && double.IsNaN(to));
		Assert.Empty(cut.Find("[data-moka-control='reset-zoom']").Children);
	}

	[Fact]
	public void TheStrip_ShowsOnlyWithStripZoom_AndSpansTheData()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> without = Chart();
		IRenderedComponent<FrameChart> with = Chart(p => p.Add(x => x.Zoom, MokaChartZoom.Strip));

		Assert.Empty(without.FindComponents<MokaRangeBar>());
		MokaRangeBar strip = with.FindComponent<MokaRangeBar>().Instance;
		Assert.Equal(MokaChartX.FromDateTimeOffset(Week[0]).Value, strip.Min);
		Assert.Equal(MokaChartX.FromDateTimeOffset(Week[^1]).Value, strip.Max);
		Assert.Equal(strip.Min, strip.Start);
		Assert.Equal(strip.Max, strip.End);
		Assert.True(strip.MoveRange);
	}

	[Fact]
	public void OnAWeek_TheArrowsMoveAnHour_AndShiftADay()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Zoom, MokaChartZoom.Strip));

		MokaRangeBar strip = cut.FindComponent<MokaRangeBar>().Instance;
		Assert.Equal(TimeSpan.FromHours(1).TotalMilliseconds, strip.Step);
		Assert.Equal(TimeSpan.FromDays(1).TotalMilliseconds, strip.LargeStep);
	}

	[Fact]
	public void OnCategories_TheArrowsMoveOneCategory()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Render<FrameChart>(p => p
			.Add(x => x.Keys, ["A"])
			.Add(x => x.Data, [[1d, 2, 3, 4]])
			.Add(x => x.Labels, ["Mon", "Tue", "Wed", "Thu"])
			.Add(x => x.Zoom, MokaChartZoom.Strip));

		MokaRangeBar strip = cut.FindComponent<MokaRangeBar>().Instance;
		Assert.Equal(0, strip.Min);
		Assert.Equal(3, strip.Max);
		Assert.Equal(1, strip.Step);
		Assert.Equal("Wed", strip.Formatter!(2));
	}

	[Fact]
	public async Task LettingTheStripGo_ZoomsTheChart_AndAllOfItResets()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Zoom, MokaChartZoom.Strip));
		MokaRangeBar strip = cut.FindComponent<MokaRangeBar>().Instance;

		await cut.InvokeAsync(() => strip.ValueChanged.InvokeAsync(new MokaRange(strip.Min + 3_600_000, strip.Max - 3_600_000)));
		Assert.Equal((strip.Min + 3_600_000, strip.Max - 3_600_000), cut.Instance.Chosen[^1]);
		Assert.Equal(strip.Min + 3_600_000, cut.FindComponent<MokaRangeBar>().Instance.Start);

		await cut.InvokeAsync(() => strip.ValueChanged.InvokeAsync(new MokaRange(strip.Min, strip.Max)));
		Assert.True(double.IsNaN(cut.Instance.Chosen[^1].From));
	}

	[Fact]
	public void TheStrip_DrawsTheOverview_BehindItsWindow()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.Zoom, MokaChartZoom.Strip)
			.Add(x => x.Overview, [1d, 4, 2, 8]));

		Assert.NotNull(cut.Find(".moka-range-bar__track .moka-range-bar__track-content .moka-chart-strip__overview-host.moka-chart-c1 .moka-sparkline"));
	}

	[Fact]
	public void TheStripsFlags_WriteTimes()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.Zoom, MokaChartZoom.Strip)
			.Add(x => x.TimeZone, TimeZoneInfo.Utc));

		MokaRangeBar strip = cut.FindComponent<MokaRangeBar>().Instance;
		string text = strip.Formatter!(MokaChartX.FromDateTimeOffset(Week[30]).Value);

		Assert.Equal(MokaTimeTicks.TooltipText(Week[30], TimeSpan.FromDays(8), new MokaTimeTickOptions { Zone = TimeZoneInfo.Utc }), text);
		Assert.NotEqual(strip.Formatter(strip.Min), strip.Formatter(strip.Max));
	}

	private IRenderedComponent<FrameChart> Chart(Action<ComponentParameterCollectionBuilder<FrameChart>>? more = null) =>
		Render<FrameChart>(p =>
		{
			p.Add(x => x.Keys, ["LAN"])
				.Add(x => x.Data, [[.. Enumerable.Range(0, Week.Count).Select(i => (double)i)]])
				.Add(x => x.Times, Week)
				.Add(x => x.RangePresets, Presets);
			more?.Invoke(p);
		});

	private sealed class FixedClock(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}
}
