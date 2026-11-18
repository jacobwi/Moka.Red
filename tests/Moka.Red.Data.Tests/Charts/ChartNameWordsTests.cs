using Bunit;
using Moka.Red.Data.Chart;
using Moka.Red.Data.Heatmap;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     A chart without a <c>Label</c> names itself by its kind, from <see cref="MokaChartText" />, so an app in another
///     language translates that too, on the classic path as on the frame.
/// </summary>
public sealed class ChartNameWordsTests : BunitContext
{
	private static readonly MokaChartText German = MokaChartText.Default with
	{
		LineChart = "Liniendiagramm",
		BarChart = "Balkendiagramm",
		Heatmap = "Wärmekarte",
		DotPlot = "Punktdiagramm",
		Described = (kind, contents) => $"{kind} ({contents})"
	};

	public ChartNameWordsTests() => this.Static();

	[Fact]
	public void TheClassicLineAndBarCharts_NameThemselvesInTheChartsWords()
	{
		IRenderedComponent<MokaLineChart> line = Render<MokaLineChart>(p => p
			.Add(x => x.Text, German)
			.Add(x => x.Series, [new MokaLineSeries { Name = "a", Values = [1d, 2] }]));
		IRenderedComponent<MokaBarChart> bar = Render<MokaBarChart>(p => p
			.Add(x => x.Text, German)
			.Add(x => x.Series, [new MokaBarSeries { Name = "a", Values = [1d, 2] }]));

		Assert.True(line.Instance.IsClassic);
		Assert.Equal("Liniendiagramm", line.Find(".moka-linechart").GetAttribute("aria-label"));
		Assert.True(bar.Instance.IsClassic);
		Assert.Equal("Balkendiagramm", bar.Find(".moka-barchart").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheClassicHeatmapGrid_NamesItselfInTheChartsWords()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Text, German)
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.GridValues, [[1d, 2], [3d, 4]]));

		Assert.Equal("Wärmekarte", cut.Find("[role=img]").GetAttribute("aria-label"));
	}

	[Fact]
	public void AChartThatSumsUpItsData_JoinsItsKindAndContentsInTheChartsWords()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p
			.Add(x => x.Text, German)
			.Add(x => x.Rows, [new MokaDotPlotRow("Router", 1)]));

		Assert.StartsWith("Punktdiagramm (Router", cut.Find(".moka-dotplot").GetAttribute("aria-label"), StringComparison.Ordinal);
	}

	[Fact]
	public void InEnglish_TheNamesReadAsTheyAlwaysHave()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p.Add(x => x.Rows, [new MokaDotPlotRow("Router", 1)]));

		Assert.StartsWith("Dot plot: Router", cut.Find(".moka-dotplot").GetAttribute("aria-label"), StringComparison.Ordinal);
	}
}
