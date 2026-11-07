using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Data.Chart;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Components;

// What a rolling rate chart needs: values written by the app, the latest value and the peak in the legend, round
// ticks in the app's unit, and no render when a parent hands over the same window again.
public class MokaLineChartLiveTests : BunitContext
{
	private static string Rate(double bytes) => bytes switch
	{
		>= 1024 * 1024 => string.Create(CultureInfo.InvariantCulture, $"{bytes / (1024 * 1024):0.#} MiB/s"),
		>= 1024 => string.Create(CultureInfo.InvariantCulture, $"{bytes / 1024:0.#} KiB/s"),
		_ => string.Create(CultureInfo.InvariantCulture, $"{bytes:0} B/s")
	};

	private static MokaLineSeries Down(params double[] values) => new() { Name = "Down", Values = values };

	private static MokaLineSeries Up(params double[] values) => new() { Name = "Up", Values = values };

	private static string[] Ticks(IRenderedComponent<MokaLineChart> cut) =>
		[.. cut.FindAll(".moka-linechart-ytick").Select(t => t.TextContent)];

	[Fact]
	public void Formatter_WritesTheTicksAndTooltips()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Formatter, v => string.Create(CultureInfo.InvariantCulture, $"{v:0} u"))
			.Add(x => x.Series, [Down(0, 40)]));

		Assert.Equal(["0 u", "10 u", "20 u", "30 u", "40 u"], Ticks(cut));
		Assert.Contains("Down: 40 u", cut.FindAll(".moka-linechart-point title")[1].TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void ShowCurrent_PutsEachSeriesLatestValueInTheLegend_AndTheName()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Label, "Throughput")
			.Add(x => x.Formatter, Rate)
			.Add(x => x.ShowCurrent, true)
			.Add(x => x.ShowPeak, true)
			.Add(x => x.Series, [Down(2048, 1024, double.NaN), Up(10, 4096, 512)]));

		Assert.Equal(["1 KiB/s", "512 B/s", "4 KiB/s"], cut.FindAll(".moka-linechart-legend-value").Select(v => v.TextContent));
		Assert.Contains("Peak", cut.Find(".moka-linechart-legend-item--peak").TextContent, StringComparison.Ordinal);
		Assert.Equal("Throughput. Down 1 KiB/s, Up 512 B/s, Peak 4 KiB/s",
			cut.Find(".moka-linechart").GetAttribute("aria-label"));
	}

	[Fact]
	public void WithoutShowCurrent_TheLegendIsNamesOnly()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p.Add(x => x.Series, [Down(1, 2)]));

		Assert.Empty(cut.FindAll(".moka-linechart-legend-value"));
		Assert.Equal("Line chart", cut.Find(".moka-linechart").GetAttribute("aria-label"));
	}

	[Fact]
	public void AutoScale_RoundsTheTopToAStep()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.AutoScale, true)
			.Add(x => x.Formatter, v => v.ToString(CultureInfo.InvariantCulture))
			.Add(x => x.Series, [Down(0, 1.3)]));

		Assert.Equal(["0", "0.5", "1", "1.5"], Ticks(cut));
	}

	[Fact]
	public void ScaleUnits_RoundsTheStepInTheUnitShown()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.AutoScale, true)
			.Add(x => x.ScaleUnits, [1d, 1024, 1024 * 1024])
			.Add(x => x.Formatter, Rate)
			.Add(x => x.Series, [Down(0, 1_300_000)]));

		Assert.Equal(["0 B/s", "512 KiB/s", "1 MiB/s", "1.5 MiB/s"], Ticks(cut));
	}

	[Fact]
	public void SuggestedMax_KeepsAnIdleChartFlat()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.SuggestedMax, 100)
			.Add(x => x.Series, [Down(0, 1, 2)]));

		Assert.Equal("100", Ticks(cut)[^1]);

		cut.Render(p => p.Add(x => x.Series, [Down(0, 250)]));
		Assert.Equal("250", Ticks(cut)[^1]);
	}

	// The labels are text in a column of their own, as wide as the longest one, so a long label never runs off the
	// edge and never squeezes the plot's drawing.
	[Fact]
	public void LongTickLabels_WidenTheirColumn_AndLeaveThePlotAlone()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Formatter, Rate)
			.Add(x => x.Series, [Down(0, 1_300_000)]));

		string longest = Ticks(cut).MaxBy(t => t.Length)!;
		Assert.Equal("317.4 KiB/s", longest);
		Assert.Equal(longest, cut.Find(".moka-linechart-yaxis > .moka-linechart-ysizer").TextContent);
		Assert.Equal("40.00", cut.Find(".moka-linechart-grid").GetAttribute("x1"));
		Assert.StartsWith("M 40.00 ", cut.Find(".moka-linechart-line").GetAttribute("d"), StringComparison.Ordinal);
	}

	[Fact]
	public void Smooth_DrawsCurves()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Smooth, true)
			.Add(x => x.ShowArea, true)
			.Add(x => x.Series, [Down(1, 3, 2, 5)]));

		string line = cut.Find(".moka-linechart-line").GetAttribute("d")!;
		Assert.StartsWith("M 40.00 ", line, StringComparison.Ordinal);
		Assert.Equal(3, line.Split(" C ").Length - 1);
		Assert.Contains(" C ", cut.Find(".moka-linechart-area").GetAttribute("d"), StringComparison.Ordinal);
	}

	[Fact]
	public void ShowLastPoint_MarksEachSeriesLatestValue()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.ShowPoints, false)
			.Add(x => x.ShowLastPoint, true)
			.Add(x => x.Series, [Down(1, 2, double.NaN), Up(3, 4, 5)]));

		IReadOnlyList<IElement> last = cut.FindAll(".moka-linechart-last");
		Assert.Equal(2, last.Count);
		Assert.Equal("314.00", last[0].GetAttribute("x1"));
		Assert.Equal("588.00", last[1].GetAttribute("x1"));
		Assert.Equal(2, cut.FindAll(".moka-linechart-last-ring").Count);
		Assert.Empty(cut.FindAll(".moka-linechart-point"));
	}

	[Fact]
	public void EmptyText_ShowsWhileThereAreNoValues()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.EmptyText, "Waiting for traffic")
			.Add(x => x.Series, [Down(double.NaN, double.NaN)]));

		Assert.Equal("Waiting for traffic", cut.Find(".moka-linechart-empty").TextContent);

		cut.Render(p => p.Add(x => x.Series, [Down(double.NaN, 3)]));
		Assert.Empty(cut.FindAll(".moka-linechart-empty"));
	}

	[Fact]
	public void TheSameWindowAgain_DrawsNothing()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Formatter, Rate)
			.Add(x => x.Series, [Down(1, 2, 3)]));
		int renders = cut.RenderCount;

		cut.Render(p => p
			.Add(x => x.Formatter, Rate)
			.Add(x => x.Series, [Down(1, 2, 3)]));

		Assert.Equal(renders, cut.RenderCount);

		cut.Render(p => p
			.Add(x => x.Formatter, Rate)
			.Add(x => x.Series, [Down(1, 2, 4)]));
		Assert.Equal(renders + 1, cut.RenderCount);
	}

	// An app that adds to its own list and renders again still sees the chart move.
	[Fact]
	public void AListChangedInPlace_StillRedraws()
	{
		List<double> values = [1, 2];
		MokaLineSeries[] series = [new() { Name = "A", Values = values }];
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p.Add(x => x.Series, series));
		Assert.Equal(2, cut.FindAll(".moka-linechart-point").Count);

		values.Add(3);
		cut.Render(p => p.Add(x => x.Series, series));

		Assert.Equal(3, cut.FindAll(".moka-linechart-point").Count);
	}

	[Fact]
	public void Live_SlidesOneStepWhenTheWindowMoves()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Live, true)
			.Add(x => x.LiveInterval, TimeSpan.FromMilliseconds(500))
			.Add(x => x.Series, [Down(1, 2, 3)]));

		IElement slide = cut.Find("g[clip-path] > g");
		Assert.Null(slide.GetAttribute("class"));
		Assert.NotNull(cut.Find("clipPath rect"));

		cut.Render(p => p.Add(x => x.Series, [Down(2, 3, 4)]));
		slide = cut.Find("g[clip-path] > g");
		Assert.Equal("moka-linechart-slide--a", slide.GetAttribute("class"));
		Assert.Equal("--_slide-shift: 274.00px; --_slide-time: 500ms", slide.GetAttribute("style"));

		cut.Render(p => p.Add(x => x.Series, [Down(3, 4, 5)]));
		Assert.Equal("moka-linechart-slide--b", cut.Find("g[clip-path] > g").GetAttribute("class"));

		// A window still filling has a new length, so its points just move.
		cut.Render(p => p.Add(x => x.Series, [Down(3, 4, 5, 6)]));
		Assert.Null(cut.Find("g[clip-path] > g").GetAttribute("class"));
	}

	[Fact]
	public void WithoutLive_ThereIsNoClip()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p.Add(x => x.Series, [Down(1, 2)]));

		Assert.Empty(cut.FindAll("clipPath"));
	}

	[Fact]
	public void TheSlide_IsDrawnFromTokens()
	{
		Assert.Equal("moka-linechart-slide-a var(--_slide-time, 1s) linear",
			ScopedCss.Declarations("MokaLineChart.razor.css", ".moka-linechart-slide--a")["animation"]);
	}
}
