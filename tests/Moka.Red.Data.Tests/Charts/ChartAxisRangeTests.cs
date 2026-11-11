using Bunit;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     The zoom's data side: the span the chart shows, kept against the parent's next render, chosen by the script or the
///     toolbar, and the windows of points a chart draws inside it.
/// </summary>
public sealed class ChartAxisRangeTests : BunitContext
{
	private static readonly DateTimeOffset Start = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
	private static readonly DateTimeOffset[] Times = ChartAxisPreviewTests.TimesOver(TimeSpan.FromHours(24), 97, Start);

	public ChartAxisRangeTests() => this.Static();

	[Fact]
	public void TheParentsRange_ZoomsTheTimeAxis_AndTheFrameFollows()
	{
		MokaChartRange range = new(Start.AddHours(6), Start.AddHours(12));
		IRenderedComponent<ChartAxisTestChart> cut = TimeChart(range);

		ChartXFrame frame = cut.Instance.Base.XFrame();
		Assert.Equal(MokaChartX.FromDateTimeOffset(Start.AddHours(6)).Value, frame.From);
		Assert.Equal(MokaChartX.FromDateTimeOffset(Start.AddHours(12)).Value, frame.To);
		Assert.Equal(0.5, cut.Instance.Base.XShare(MokaChartX.FromDateTimeOffset(Start.AddHours(9)).Value), 6);
	}

	[Fact]
	public void XWindow_HoldsThePointsInTheRange_PaddedByOne()
	{
		IRenderedComponent<ChartAxisTestChart> cut = TimeChart(new MokaChartRange(Start.AddHours(6), Start.AddHours(12)));
		double[] x = cut.Instance.Base.TimeValues();

		Assert.Equal((23, 49), cut.Instance.Base.XWindow(x));
		Assert.Equal((0, 96), TimeChart(null).Instance.Base.XWindow(x));
	}

	[Fact]
	public void ACategoryAxis_ZoomsByIndex()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Render<ChartAxisTestChart>(p => p
			.Add(x => x.Values, [.. Enumerable.Range(0, 20).Select(i => (double)i)])
			.Add(x => x.Range, new MokaChartRange(5, 10)));

		Assert.Equal((4, 11), cut.Instance.Base.CategoryWindow(20));
		Assert.Equal(0.4, cut.Instance.Base.XShare(7), 6);
		Assert.Equal(["5", "10"], cut.FindAll(".moka-chart-plot__xtick").Select(t => t.TextContent).Where(t => t is "5" or "10"));
		Assert.DoesNotContain(cut.FindAll(".moka-chart-plot__xtick"), t => t.TextContent == "2");
	}

	[Fact]
	public async Task TheScriptsRange_ZoomsRedrawsAndTellsTheParent_AndNaNResets()
	{
		List<MokaChartRange?> told = [];
		IRenderedComponent<ChartAxisTestChart> cut = TimeChart(null, told);
		long version = cut.Instance.Base.GeometryVersion;
		double from = MokaChartX.FromDateTimeOffset(Start.AddHours(2)).Value;
		double to = MokaChartX.FromDateTimeOffset(Start.AddHours(4)).Value;

		await cut.InvokeAsync(() => cut.Instance.Base.OnRange(to, from));

		Assert.Equal(new MokaChartRange(new MokaChartX(from, true), new MokaChartX(to, true)), cut.Instance.Base.CurrentRange);
		Assert.True(cut.Instance.Base.GeometryVersion > version);
		cut.WaitForAssertion(() => Assert.Single(told));

		await cut.InvokeAsync(() => cut.Instance.Base.OnRange(double.NaN, double.NaN));
		Assert.Null(cut.Instance.Base.CurrentRange);
		cut.WaitForAssertion(() => Assert.Equal(2, told.Count));
	}

	[Fact]
	public async Task AChosenRange_SurvivesTheParentsNextRender_UntilTheParentPassesANewOne()
	{
		IRenderedComponent<ChartAxisTestChart> cut = TimeChart(null);
		MokaChartRange chosen = new(Start.AddHours(1), Start.AddHours(3));
		await cut.InvokeAsync(() => cut.Instance.Base.SetRangeAsync(chosen));

		cut.Render(p => p.Add(x => x.Values, Values()).Add(x => x.Times, Times).Add(x => x.TimeZone, TimeZoneInfo.Utc)
			.Add(x => x.Range, (MokaChartRange?)null).Add(x => x.Title, "again"));
		Assert.Equal(chosen, cut.Instance.Base.CurrentRange);

		MokaChartRange parents = new(Start.AddHours(5), Start.AddHours(6));
		cut.Render(p => p.Add(x => x.Values, Values()).Add(x => x.Times, Times).Add(x => x.TimeZone, TimeZoneInfo.Utc)
			.Add(x => x.Range, parents).Add(x => x.Title, "again"));
		Assert.Equal(parents, cut.Instance.Base.CurrentRange);
	}

	[Fact]
	public void ANumericAxis_ZoomsToExactlyTheRange()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Render<ChartAxisTestChart>(p => p
			.Add(x => x.Values, [1d, 2])
			.Add(x => x.NumberRange, ((double, double)?)(0, 100))
			.Add(x => x.Range, new MokaChartRange(12.5, 37.5)));

		ChartXFrame frame = cut.Instance.Base.XFrame();
		Assert.Equal(12.5, frame.From);
		Assert.Equal(37.5, frame.To);
	}

	private IRenderedComponent<ChartAxisTestChart> TimeChart(MokaChartRange? range, List<MokaChartRange?>? told = null) =>
		Render<ChartAxisTestChart>(p =>
		{
			p.Add(x => x.Values, Values());
			p.Add(x => x.Times, Times);
			p.Add(x => x.TimeZone, TimeZoneInfo.Utc);
			p.Add(x => x.Range, range);
			p.Add(x => x.Title, "first");
			if (told is not null)
			{
				p.Add(x => x.RangeChanged, value => told.Add(value));
			}
		});

	private static double[] Values() => [.. Enumerable.Range(0, 97).Select(i => (double)i)];
}
