using System.Diagnostics;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using static Moka.Red.Data.Tests.Charts.MokaLineChartTestKit;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     What a modern line chart draws from 10 to 1,000,000 points a series: everything up to twice the plot's width,
///     the lowest and highest value of each pixel past it, raw points again once zoomed in, and nothing at all for a
///     parent that renders again with the same data.
/// </summary>
public sealed class MokaLineChartScaleTests(ITestOutputHelper output) : BunitContext
{
	private static readonly DateTimeOffset Start = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);

	[Theory]
	[InlineData(10)]
	[InlineData(1_000)]
	public void UpToTwiceThePlotsWidth_EveryValueIsDrawn(int count)
	{
		this.Static();
		IRenderedComponent<MokaLineChart> cut = Chart(count);

		Assert.Equal(count, Points(LineOf(cut)).Count);
	}

	[Theory]
	[InlineData(100_000)]
	[InlineData(1_000_000)]
	public void PastIt_ALineDrawsTheLowestAndHighestOfEachPixel(int count)
	{
		this.Static();
		Stopwatch clock = Stopwatch.StartNew();
		IRenderedComponent<MokaLineChart> cut = Chart(count);
		output.WriteLine($"{count:N0} points a series: first render in {clock.Elapsed.TotalMilliseconds:0} ms");

		IReadOnlyList<(char Command, int X, int Y)> points = Points(LineOf(cut));
		Assert.InRange(points.Count, 800, 2 * 800 + 2);
		Assert.Equal(0, points.Min(point => point.Y));
		Assert.Equal(10_000, points.Max(point => point.Y));
		Assert.True(cut.Instance.MeasuresWidth);
	}

	[Fact]
	public void AMillionPoints_RebuildWithinAFewMilliseconds_WithADataVersion()
	{
		this.Static();
		double[] values = Wave(1_000_000);
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.DataVersion, 1L)
			.Add(x => x.Series, [Series("A", values)]));

		Stopwatch clock = Stopwatch.StartNew();
		const int rounds = 5;
		for (int i = 0; i < rounds; i++)
		{
			values[i] = -1;
			cut.Render(p => p.Add(x => x.DataVersion, i + 2L));
		}

		double each = clock.Elapsed.TotalMilliseconds / rounds;
		output.WriteLine($"1,000,000 points: {each:0.0} ms a data change, render included");
		Assert.InRange(each, 0, 1000);
	}

	[Fact]
	public void TheReportedWidth_DecidesHowManyPointsAreDrawn()
	{
		this.Interactive();
		IRenderedComponent<MokaLineChart> cut = Chart(100_000);
		int wide = Points(LineOf(cut)).Count;

		cut.InvokeAsync(() => cut.Instance.OnPlotWidth(320));

		int narrow = Points(LineOf(cut)).Count;
		Assert.InRange(narrow, 320, 2 * 320 + 2);
		Assert.True(narrow < wide);
	}

	[Fact]
	public void Lttb_KeepsTwiceTheWidthInPointsThatHoldTheShape()
	{
		this.Static();
		IRenderedComponent<MokaLineChart> cut = Chart(100_000, p => p.Add(x => x.Downsample, MokaDownsample.Lttb));

		Assert.InRange(Points(LineOf(cut)).Count, 1500, 1600);
	}

	[Fact]
	public void DownsampleNone_DrawsEveryPoint()
	{
		this.Static();
		IRenderedComponent<MokaLineChart> cut = Chart(5_000, p => p.Add(x => x.Downsample, MokaDownsample.None));

		Assert.Equal(5_000, Points(LineOf(cut)).Count);
	}

	[Fact]
	public void ZoomingIn_WindowsBeforeDownsampling_SoAnHourOfAMillionShowsMoreDetail_AndMinutesShowRawPoints()
	{
		this.Static();
		DateTimeOffset[] times = Times(1_000_000, TimeSpan.FromDays(1));
		IRenderedComponent<MokaLineChart> cut = Chart(1_000_000, p => p
			.Add(x => x.Times, times)
			.Add(x => x.Range, new MokaChartRange(Start.AddHours(12), Start.AddHours(12).AddMinutes(1))));

		IReadOnlyList<(char Command, int X, int Y)> raw = Points(LineOf(cut));
		Assert.InRange(raw.Count, 694, 697);
		Assert.Contains(raw, point => point.X < 0);
		Assert.Contains(raw, point => point.X > 10_000);
		Assert.NotNull(cut.Find("svg.moka-linechart__marks clipPath"));
	}

	[Fact]
	public void AParentRenderingAgainWithTheSameData_CostsTheChartNoRender()
	{
		this.Static();
		IRenderedComponent<MokaLineChart> cut = Chart(10_000);
		int renders = cut.RenderCount;

		for (int i = 0; i < 5; i++)
		{
			cut.Render(p => p
				.Add(x => x.Look, MokaChartLook.Quiet)
				.Add(x => x.Series, [Series("A", Wave(10_000))]));
		}

		Assert.Equal(renders, cut.RenderCount);
	}

	[Fact]
	public void WithADataVersion_TheSameVersionCostsNoRender_AndANewOneRendersOnce()
	{
		this.Static();
		double[] values = Wave(100_000);
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.DataVersion, 1L)
			.Add(x => x.Series, [Series("A", values)]));
		int renders = cut.RenderCount;

		MokaLineSeries[] series = [Series("A", values)];
		cut.Render(p => p.Add(x => x.Look, MokaChartLook.Quiet).Add(x => x.DataVersion, 1L).Add(x => x.Series, series));
		int settled = cut.RenderCount;
		cut.Render(p => p.Add(x => x.Look, MokaChartLook.Quiet).Add(x => x.DataVersion, 1L).Add(x => x.Series, series));
		Assert.Equal(settled, cut.RenderCount);

		long version = cut.Instance.GeometryVersion;
		values[0] = 99;
		cut.Render(p => p.Add(x => x.Look, MokaChartLook.Quiet).Add(x => x.DataVersion, 2L).Add(x => x.Series, series));
		Assert.True(cut.RenderCount > settled);
		Assert.Equal(version + 1, cut.Instance.GeometryVersion);
		Assert.True(settled >= renders);
	}

	[Fact]
	public void ALiveWindowOnceASecond_RendersOncePerSample_AndSlides()
	{
		this.Static();
		double[] window = Wave(600);
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Live, true)
			.Add(x => x.Series, [Series("A", window)]));
		int renders = cut.RenderCount;
		long version = cut.Instance.GeometryVersion;

		for (int second = 1; second <= 3; second++)
		{
			window = [.. window.Skip(1), second];
			double[] shown = window;
			cut.Render(p => p.Add(x => x.Look, MokaChartLook.Quiet).Add(x => x.Live, true).Add(x => x.Series, [Series("A", shown)]));
		}

		Assert.Equal(3, cut.Instance.GeometryVersion - version);
		Assert.True(cut.RenderCount > renders);
		Assert.Single(cut.FindAll("g.moka-linechart-slide--a, g.moka-linechart-slide--b"));
	}

	[Fact]
	public void PastSixteenHundredPoints_TheChartAsksForThePlotsWidth()
	{
		this.Static();

		Assert.False(Chart(1_600).Instance.MeasuresWidth);
		Assert.True(Chart(1_601).Instance.MeasuresWidth);
	}

	private IRenderedComponent<MokaLineChart> Chart(int count, Action<ComponentParameterCollectionBuilder<MokaLineChart>>? more = null) =>
		Render<MokaLineChart>(p =>
		{
			p.Add(x => x.Look, MokaChartLook.Quiet).Add(x => x.Series, [Series("A", Wave(count))]);
			more?.Invoke(p);
		});

	private static string? LineOf(IRenderedComponent<MokaLineChart> cut) =>
		PathOf(Group(cut.Find(".moka-linechart"), "A"), "moka-linechart__line");

	// A wave with one spike up and one down, so a reduction that lost them shows.
	private static double[] Wave(int count)
	{
		double[] values = new double[count];
		for (int i = 0; i < count; i++)
		{
			values[i] = 50 + 20 * Math.Sin(i / 50d);
		}

		values[count / 3] = 100;
		values[2 * count / 3] = 0;
		return values;
	}

	private static DateTimeOffset[] Times(int count, TimeSpan span)
	{
		TimeSpan step = span / Math.Max(1, count - 1);
		return [.. Enumerable.Range(0, count).Select(i => Start + step * i)];
	}
}
