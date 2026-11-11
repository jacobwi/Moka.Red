using Bunit;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     Short lists of options (colors, highlighted series, annotations, range presets, percentiles) compare item by item
///     against a copy, on every chart and with or without a <c>DataVersion</c>: the same list built again every render
///     draws nothing again, and one changed in place still draws. And a chart's height follows a CSS expression.
/// </summary>
public sealed class ChartOptionListTests : BunitContext
{
	private static readonly string[] Hours = ["00:00", "06:00", "12:00", "18:00"];
	private static readonly MokaLineSeries[] Line = [new() { Name = "LAN", Values = [10, 20, 30, 40] }];

	public ChartOptionListTests() => this.Static();

	[Fact]
	public void TheSameColorsAndHighlights_InNewLists_DrawNothingAgain_OnAnyChart()
	{
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p
			.Add(x => x.Values, [1d, 2])
			.Add(x => x.Colors, ["#112233", "red"])
			.Add(x => x.HighlightSeries, ["a"]));
		int rebuilds = cut.Instance.Rebuilds;
		int renders = cut.Instance.Renders;

		cut.Render(p => p.Add(x => x.Values, [1d, 2]).Add(x => x.Colors, ["#112233", "red"]).Add(x => x.HighlightSeries, ["a"]));

		Assert.Equal((rebuilds, renders), (cut.Instance.Rebuilds, cut.Instance.Renders));

		cut.Render(p => p.Add(x => x.Values, [1d, 2]).Add(x => x.Colors, ["#112233", "blue"]).Add(x => x.HighlightSeries, ["a"]));

		Assert.Equal(rebuilds + 1, cut.Instance.Rebuilds);
	}

	[Fact]
	public void AListOfOptionsChangedInPlace_StillDrawsAgain()
	{
		List<string> colors = ["#112233", "red"];
		IRenderedComponent<TestChart> cut = Render<TestChart>(p => p.Add(x => x.Values, [1d, 2]).Add(x => x.Colors, colors));
		int rebuilds = cut.Instance.Rebuilds;

		colors[1] = "blue";
		cut.Render(p => p.Add(x => x.Values, [1d, 2]).Add(x => x.Colors, colors));

		Assert.Equal(rebuilds + 1, cut.Instance.Rebuilds);
	}

	[Fact]
	public void WithADataVersion_TheSameAnnotations_InANewList_DrawNothingAgain()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => LineParameters(p, 1, [new MokaChartThreshold(25) { Label = "Limit" }]));
		long version = cut.Instance.GeometryVersion;

		cut.Render(p => LineParameters(p, 1, [new MokaChartThreshold(25) { Label = "Limit" }]));
		Assert.Equal(version, cut.Instance.GeometryVersion);

		cut.Render(p => LineParameters(p, 1, [new MokaChartThreshold(35) { Label = "Limit" }]));
		Assert.NotEqual(version, cut.Instance.GeometryVersion);
	}

	[Fact]
	public void TheSameRangePresetsAndPercentiles_InNewLists_DrawNothingAgain()
	{
		IRenderedComponent<MokaHistogram> histogram = Render<MokaHistogram>(p => p.Add(x => x.Values, [1d, 2, 3, 4, 5]).Add(x => x.Percentiles, [50d]));
		long before = histogram.Instance.GeometryVersion;
		histogram.Render(p => p.Add(x => x.Values, [1d, 2, 3, 4, 5]).Add(x => x.Percentiles, [50d]));
		Assert.Equal(before, histogram.Instance.GeometryVersion);

		IRenderedComponent<MokaLineChart> line = Render<MokaLineChart>(p => p.Add(x => x.Series, Line).Add(x => x.Labels, Hours)
			.Add(x => x.RangePresets, [new MokaChartRangePreset("1 h", TimeSpan.FromHours(1))]));
		long version = line.Instance.GeometryVersion;
		line.Render(p => p.Add(x => x.Series, Line).Add(x => x.Labels, Hours)
			.Add(x => x.RangePresets, [new MokaChartRangePreset("1 h", TimeSpan.FromHours(1))]));
		Assert.Equal(version, line.Instance.GeometryVersion);
	}

	[Theory]
	[InlineData("calc(8rem * var(--app-text-scale, 1))")]
	[InlineData("var(--chart-height, 12rem)")]
	[InlineData("clamp(6rem, 30vh, 14rem)")]
	[InlineData("230px")]
	public void Height_TakesALengthOrAnExpression(string height)
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Series, Line)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Title, "Traffic")
			.Add(x => x.Height, height));

		Assert.Contains("moka-chart-plot--height", cut.Find(".moka-chart-plot").ClassList);
		Assert.Contains($"height: {height}", cut.Find(".moka-chart-plot__grid").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("calc(1px); position: fixed")]
	[InlineData("url(x)")]
	[InlineData("tall")]
	public void Height_IgnoresAnythingElse(string height)
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Series, Line)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Title, "Traffic")
			.Add(x => x.Height, height));

		Assert.DoesNotContain("moka-chart-plot--height", cut.Find(".moka-chart-plot").ClassList);
		Assert.DoesNotContain("height:", cut.Find(".moka-chart-plot__grid").GetAttribute("style") ?? string.Empty, StringComparison.Ordinal);
	}

	[Fact]
	public void TheChartsOfParts_TakeAnExpressionToo()
	{
		const string height = "calc(10rem + 2vh)";
		IRenderedComponent<MokaTreemap> treemap = Render<MokaTreemap>(p => p
			.Add(x => x.Nodes, [new MokaTreemapNode("a", 3), new MokaTreemapNode("b", 1)])
			.Add(x => x.Height, height));
		IRenderedComponent<MokaDonutChart> donut = Render<MokaDonutChart>(p => p
			.Add(x => x.Segments, [new MokaDonutSegment { Name = "a", Value = 3 }])
			.Add(x => x.Height, height));

		Assert.Contains($"--moka-treemap-height: {height}", treemap.Markup, StringComparison.Ordinal);
		Assert.Contains($"--moka-donut-size: {height}", donut.Markup, StringComparison.Ordinal);
	}

	private static ComponentParameterCollectionBuilder<MokaLineChart> LineParameters(ComponentParameterCollectionBuilder<MokaLineChart> p,
		long version, MokaChartAnnotation[] annotations) => p
		.Add(x => x.Series, Line)
		.Add(x => x.Labels, Hours)
		.Add(x => x.DataVersion, version)
		.Add(x => x.Annotations, annotations);
}
