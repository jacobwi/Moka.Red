using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Charts;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

public sealed class MokaBoxPlotTests : BunitContext
{
	private static readonly double[] Login = [.. Enumerable.Range(1, 101).Select(i => (double)i)];

	public MokaBoxPlotTests() => this.Static();

	[Fact]
	public void DrawsABoxPerGroup_FromTheValuesSummary()
	{
		IRenderedComponent<MokaBoxPlot> cut = Render<MokaBoxPlot>(p => p
			.Add(x => x.Groups, [new MokaBoxPlotGroup("login", Login), new MokaBoxPlotGroup("search", [.. Login.Select(v => v * 2)])]));

		Assert.Equal(2, cut.FindAll(".moka-chart-plot__row").Count);
		IElement lane = cut.Find(".moka-boxplot__lane");
		MokaBoxSummary summary = MokaChartStats.Box(Login);
		MokaScale scale = cut.Instance.RowPlotModel().Scale!;
		Assert.Equal(scale.Share(summary.Median) * 100, RowChartKit.Percent(RowChartKit.Var(lane, "--_bx-md")), 2);
		Assert.Equal(scale.Share(summary.Q1) * 100, RowChartKit.Percent(RowChartKit.Var(lane, "--_bx-q1")), 2);
		Assert.Equal(scale.Share(summary.Low) * 100, RowChartKit.Percent(RowChartKit.Var(lane, "--_bx-lo")), 2);
		Assert.NotNull(lane.QuerySelector(".moka-boxplot__box"));
		Assert.NotNull(lane.QuerySelector(".moka-boxplot__median"));
		Assert.Equal(2, lane.QuerySelectorAll(".moka-boxplot__whisker").Length);
		Assert.Empty(cut.FindAll("svg"));
	}

	[Fact]
	public void APrecomputedSummary_WinsOverTheValues()
	{
		MokaBoxSummary summary = new(0, 10, 20, 30, 40, 50, 60, 1204, []);
		IRenderedComponent<MokaBoxPlot> cut = Render<MokaBoxPlot>(p => p
			.Add(x => x.Groups, [new MokaBoxPlotGroup("api", [1, 2, 3]) { Summary = summary }]));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 0, null)!;
		Assert.Equal("30", info.Rows[0].ValueText);
		Assert.Equal("1,204 values", info.Notes[0]);
		Assert.Equal("api, Median 30, Middle half 20 to 40, 5th to 95th 10 to 50, 1,204 values", info.CellText);
	}

	[Fact]
	public void ShowPoints_DrawsOnePathPerRow_AndPastMaxPointsAnEvenStrideWithTheEnds()
	{
		double[] many = [.. Enumerable.Range(0, 1000).Select(i => (double)(999 - i))];
		IRenderedComponent<MokaBoxPlot> cut = Render<MokaBoxPlot>(p => p
			.Add(x => x.Groups, [new MokaBoxPlotGroup("api", many)])
			.Add(x => x.ShowPoints, true)
			.Add(x => x.MaxPoints, 50));

		IElement svg = cut.Find(".moka-boxplot__points");
		Assert.Equal("true", svg.GetAttribute("aria-hidden"));
		string d = Assert.Single(svg.QuerySelectorAll("path")).GetAttribute("d")!;
		string[] segments = d.Split('M', StringSplitOptions.RemoveEmptyEntries);
		Assert.Equal(50, segments.Length);
		Assert.All(segments, segment => Assert.EndsWith("h0", segment, StringComparison.Ordinal));
		MokaScale scale = cut.Instance.RowPlotModel().Scale!;
		double[] xs = [.. segments.Select(s => double.Parse(s.Split(' ')[0], CultureInfo.InvariantCulture))];
		Assert.Equal(scale.Share(0) * 1000, xs.Min(), 0);
		Assert.Equal(scale.Share(999) * 1000, xs.Max(), 0);
	}

	[Fact]
	public void TukeyWhiskers_DrawTheValuesPastThem_WithoutPoints()
	{
		double[] values = [.. Enumerable.Repeat(10d, 20), 11, 12, 9, 200];
		IRenderedComponent<MokaBoxPlot> cut = Render<MokaBoxPlot>(p => p
			.Add(x => x.Groups, [new MokaBoxPlotGroup("api", values)])
			.Add(x => x.Whiskers, MokaWhiskers.Tukey));

		string d = cut.Find(".moka-boxplot__points path").GetAttribute("d")!;
		Assert.Equal(MokaChartStats.Box(values, MokaWhiskers.Tukey).Outliers.Count, d.Split('M', StringSplitOptions.RemoveEmptyEntries).Length);
		Assert.Contains("Whiskers", cut.Find(".moka-boxplot__key").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void TheKey_SaysWhatEachPartMeans_AndCanBeLeftOut()
	{
		IRenderedComponent<MokaBoxPlot> cut = Render<MokaBoxPlot>(p => p
			.Add(x => x.Groups, [new MokaBoxPlotGroup("api", Login)])
			.Add(x => x.ShowPoints, true));

		Assert.Equal(["Middle half", "Median", "5th to 95th", "One value"],
			cut.FindAll(".moka-boxplot__key-item").Select(item => item.TextContent.Trim()));

		cut.Render(p => p.Add(x => x.ShowKey, false));
		Assert.Empty(cut.FindAll(".moka-boxplot__key"));
	}

	[Fact]
	public void RowsAreALittleTallerThanTheLooks_ForThePoints()
	{
		IRenderedComponent<MokaBoxPlot> cut = Render<MokaBoxPlot>(p => p.Add(x => x.Groups, [new MokaBoxPlotGroup("api", Login)]));

		Assert.Contains("--_row-h: 32px", cut.Find(".moka-chart-plot").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void TheTable_HasTheFiveNumbersAndTheCount()
	{
		IRenderedComponent<MokaBoxPlot> cut = Render<MokaBoxPlot>(p => p.Add(x => x.Groups, [new MokaBoxPlotGroup("api", Login)]));

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(["Name", "Low", "Q1", "Median", "Q3", "High", "Count"], table.Headers);
		Assert.Equal("51", table.Raw(0, 3));
		Assert.Equal("101", table.Raw(0, 6));
	}

	[Fact]
	public void AValueChangedInPlace_DrawsAgain()
	{
		List<double> values = [1, 2, 3, 4, 5];
		IRenderedComponent<MokaBoxPlot> cut = Render<MokaBoxPlot>(p => p.Add(x => x.Groups, [new MokaBoxPlotGroup("api", values)]));
		long before = cut.Instance.GeometryVersion;

		values[4] = 50;
		cut.Render(p => p.Add(x => x.Groups, [new MokaBoxPlotGroup("api", values)]));

		Assert.True(cut.Instance.GeometryVersion > before);
	}
}
