using Bunit;
using Moka.Red.Data.Chart;
using Moka.Red.Data.Heatmap;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Charts;

// A modern chart's root is a query container, which takes no width from its content. In a parent that shrinks to fit,
// such as the docs site's centred preview, 0.4.0 drew every new chart 0 px wide until the roots filled their container.
public sealed class ChartFillTests : BunitContext
{
	public ChartFillTests() => this.Static();

	[Fact]
	public void AModernChart_FillsItsContainer()
	{
		IRenderedComponent<MokaHistogram> cut = Render<MokaHistogram>(p => p
			.Add(x => x.Values, [12d, 30, 41, 55, 80])
			.Add(x => x.BucketSize, 25));

		Assert.Contains("moka-fill-width", cut.Find(".moka-histogram").ClassList);
	}

	// As a flex column the root measured its frame, a query container with no width of its own, at its narrowest: the
	// title broke word by word and a 144 px ring stood in a 525 px chart. Every chart root is a block.
	[Fact]
	public void TheModernDonutRoot_IsABlock_NotAFlexColumn()
	{
		IReadOnlyDictionary<string, string> root = ScopedCss.Declarations("MokaDonutChart.razor.css", ".moka-donutchart.moka-chart");

		Assert.Equal("block", root["display"]);
		Assert.DoesNotContain("flex-direction", root.Keys);
	}

	[Fact]
	public void AHeatmapGrid_FillsItsContainer()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.GridValues, [[1d, 2d], [3d, 4d]]));

		Assert.Contains("moka-fill-width", cut.Find(".moka-heatmap--grid").ClassList);
	}
}
