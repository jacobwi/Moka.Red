using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     A bar series' own color per value: the bars it names take it, the series keeps its identity (its slot class, its
///     legend entry, its hatching and its key), and anything that isn't a color falls back to the series' color.
/// </summary>
public sealed class MokaBarChartColorsTests : BunitContext
{
	private const string Busy = "var(--moka-color-warning)";
	private static readonly string[] Hours = ["13", "14", "15", "16"];
	private static readonly MokaBarSeries Other = new() { Name = "Other", Values = [1, 1, 1, 1] };

	public MokaBarChartColorsTests() => this.Static();

	[Fact]
	public void TheNamedBars_TakeTheirOwnColor_AndKeepTheSeriesSlotAndHatching()
	{
		IRenderedComponent<MokaBarChart> cut = Chart([Traffic([null, Busy, "not a color; top: 0", Busy])]);

		IReadOnlyList<IElement> marks = cut.FindAll(".moka-chart-mark");
		Assert.Equal(4, marks.Count);
		Assert.All(marks, mark => Assert.Contains("moka-chart-c1", mark.ClassList));
		Assert.All(marks, mark => Assert.Contains("moka-chart-hatch", mark.ClassList));
		Assert.All(marks, mark => Assert.Equal("traffic", mark.GetAttribute("data-moka-key")));
		Assert.DoesNotContain("--moka-chart-color", marks[0].GetAttribute("style"), StringComparison.Ordinal);
		Assert.Contains($"--moka-chart-color: {Busy}", marks[1].GetAttribute("style"), StringComparison.Ordinal);
		Assert.DoesNotContain("--moka-chart-color", marks[2].GetAttribute("style"), StringComparison.Ordinal);
		Assert.Contains($"--moka-chart-color: {Busy}", marks[3].GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void TheLegend_KeepsTheSeriesColor()
	{
		IRenderedComponent<MokaBarChart> cut = Chart([Traffic([Busy, Busy, Busy, Busy]), Other]);

		ChartSeriesInfo info = cut.Instance.SeriesInfo[0];
		Assert.Equal("moka-chart-c1", info.ColorClass);
		Assert.Null(info.ColorStyle);
	}

	[Fact]
	public void TheTooltipRow_ShowsTheBarsOwnColor()
	{
		IRenderedComponent<MokaBarChart> cut = Chart([Traffic([null, Busy, null, null])]);

		Assert.Contains(Busy, cut.Instance.DescribeActive(0, -1, 1, null)!.Rows[0].ColorStyle, StringComparison.Ordinal);
		Assert.Null(cut.Instance.DescribeActive(0, -1, 0, null)!.Rows[0].ColorStyle);
	}

	[Fact]
	public void AHighlightElsewhere_StillMutesTheBars()
	{
		IRenderedComponent<MokaBarChart> cut = Chart([Traffic([Busy, Busy, Busy, Busy]), Other], p => p.Add(x => x.Highlight, "Other"));

		IElement mark = cut.FindAll(".moka-chart-mark").First(m => m.GetAttribute("data-moka-key") == "traffic");
		Assert.Contains("moka-chart-muted", mark.ClassList);
		Assert.DoesNotContain("--moka-chart-color", mark.GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void StackedBars_TakeTheirOwnColorToo()
	{
		IRenderedComponent<MokaBarChart> cut = Chart([Traffic([null, Busy, null, null]), Other], p => p.Add(x => x.Stacked, true));

		IElement column = cut.FindAll(".moka-chart-cols > .moka-chart-col")[1];
		Assert.Contains($"--moka-chart-color: {Busy}", column.QuerySelector("[data-moka-key=traffic]")!.GetAttribute("style"),
			StringComparison.Ordinal);
	}

	[Fact]
	public void Colors_TakeTheChartOffTheClassicPath_AndCompareByValue()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Series, [new MokaBarSeries { Name = "Traffic", Values = [1, 2], Colors = [null, Busy] }])
			.Add(x => x.Labels, ["a", "b"]));
		Assert.False(cut.Instance.IsClassic);
		long version = cut.Instance.GeometryVersion;

		cut.Render(p => p
			.Add(x => x.Series, [new MokaBarSeries { Name = "Traffic", Values = [1, 2], Colors = [null, Busy] }])
			.Add(x => x.Labels, ["a", "b"]));
		Assert.Equal(version, cut.Instance.GeometryVersion);

		cut.Render(p => p
			.Add(x => x.Series, [new MokaBarSeries { Name = "Traffic", Values = [1, 2], Colors = [Busy, Busy] }])
			.Add(x => x.Labels, ["a", "b"]));
		Assert.NotEqual(version, cut.Instance.GeometryVersion);
	}

	private static MokaBarSeries Traffic(IReadOnlyList<string?> colors) =>
		new() { Name = "Traffic", Key = "traffic", Values = [3, 9, 4, 8], Colors = colors };

	private IRenderedComponent<MokaBarChart> Chart(IReadOnlyList<MokaBarSeries> series,
		Action<ComponentParameterCollectionBuilder<MokaBarChart>>? more = null) =>
		Render<MokaBarChart>(p =>
		{
			p.Add(x => x.Series, series);
			p.Add(x => x.Labels, Hours);
			p.Add(x => x.Look, MokaChartLook.Quiet);
			p.Add(x => x.Texture, true);
			p.Add(x => x.Legend, MokaChartLegendPosition.None);
			more?.Invoke(p);
		});
}
