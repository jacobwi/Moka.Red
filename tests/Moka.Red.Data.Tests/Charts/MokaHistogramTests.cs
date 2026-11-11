#pragma warning disable CA5394 // Seeded Random keeps the tests repeatable; nothing here is security related
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Charts;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

public sealed class MokaHistogramTests : BunitContext
{
	private static readonly double[] Times = [12, 18, 31, 37, 44, 45, 61, 88, 90, 140];

	public MokaHistogramTests() => this.Static();

	[Fact]
	public void CountsEveryValueIntoItsBucket_AndDrawsAColumnPerBucket()
	{
		IRenderedComponent<MokaHistogram> cut = Render<MokaHistogram>(p => p
			.Add(x => x.Values, Times)
			.Add(x => x.BucketSize, 25)
			.Add(x => x.Origin, 0));

		IReadOnlyList<IElement> columns = cut.FindAll(".moka-chart-cols > .moka-chart-col");
		Assert.Equal(6, columns.Count);
		Assert.All(columns, column => Assert.Contains("moka-chart-col--tile", column.ClassList));
		Assert.Equal(6, cut.Instance.DrawnCount(0));
		Assert.Equal(10, Enumerable.Range(0, 6).Sum(i => (int)cut.Instance.DescribeActive(0, -1, i, null)!.Context.Points[0].Value));
		Assert.Equal(["2", "4", "1", "2", "0", "1"],
			Enumerable.Range(0, 6).Select(i => cut.Instance.DescribeActive(0, -1, i, null)!.Rows[0].ValueText));
	}

	[Fact]
	public void AnEmptyBucket_DrawsNoBar_ButKeepsItsPlace()
	{
		IRenderedComponent<MokaHistogram> cut = Render<MokaHistogram>(p => p.Add(x => x.Values, Times).Add(x => x.BucketSize, 25));

		Assert.Empty(cut.FindAll(".moka-chart-cols > .moka-chart-col")[4].QuerySelectorAll(".moka-chart-mark"));
	}

	[Fact]
	public void TheXAxis_IsLabelledAtTheBucketsEdges()
	{
		IRenderedComponent<MokaHistogram> cut = Render<MokaHistogram>(p => p
			.Add(x => x.Values, Times)
			.Add(x => x.BucketSize, 25)
			.Add(x => x.Unit, MokaChartUnit.Milliseconds));

		Assert.Equal(7, cut.Instance.CategoryCount);
		Assert.Equal("0 ms", cut.Instance.CategoryText(0));
		Assert.Equal("150 ms", cut.Instance.CategoryText(6));
		Assert.Equal(0.5, cut.Instance.CategoryShare(3, 7));
	}

	[Fact]
	public void TheTooltip_SaysTheBucket_TheCount_AndItsShare()
	{
		IRenderedComponent<MokaHistogram> cut = Render<MokaHistogram>(p => p
			.Add(x => x.Values, Times)
			.Add(x => x.BucketSize, 25)
			.Add(x => x.Unit, MokaChartUnit.Milliseconds)
			.Add(x => x.CountLabel, "requests"));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 1, null)!;
		Assert.Equal("25 ms to 50 ms", info.Heading);
		ChartActiveRow row = Assert.Single(info.Rows);
		Assert.Equal("4", row.ValueText);
		Assert.Equal("requests", row.Name);
		Assert.Equal("40%", row.DeltaText);
		Assert.Equal("25 ms to 50 ms, 4 requests, 40%", info.CellText);
		Assert.Null(cut.Instance.DescribeActive(0, -1, 6, null));
	}

	[Fact]
	public void PercentileLines_SitAtTheirValue_Labelled()
	{
		IRenderedComponent<MokaHistogram> cut = Render<MokaHistogram>(p => p
			.Add(x => x.Values, [.. Enumerable.Range(0, 100).Select(i => (double)i)])
			.Add(x => x.BucketSize, 10)
			.Add(x => x.Origin, 0)
			.Add(x => x.Percentiles, [95, 50]));

		IReadOnlyList<IElement> lines = cut.FindAll(".moka-histogram__line");
		Assert.Equal(2, lines.Count);
		Assert.Equal("p50 49.5", lines[0].TextContent.Trim());
		Assert.Contains("--_x: 49.5%", lines[0].GetAttribute("style"), StringComparison.Ordinal);
		Assert.Contains("moka-histogram__line--right", lines[0].ClassList);
		Assert.Equal("p95 94.05", lines[1].TextContent.Trim());
		Assert.Contains("moka-histogram__line--left", lines[1].ClassList);
		Assert.Contains("--_row: 1", lines[1].GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void WithoutABucketSize_ItPicksARoundOne_FromTheSpread()
	{
		Random random = new(5);
		double[] values = [.. Enumerable.Range(0, 4200).Select(_ => Math.Exp(Math.Log(118) + (0.52 * Gauss(random))))];
		IRenderedComponent<MokaHistogram> cut = Render<MokaHistogram>(p => p
			.Add(x => x.Values, values)
			.Add(x => x.Unit, MokaChartUnit.Milliseconds));

		ChartActiveInfo first = cut.Instance.DescribeActive(0, -1, 0, null)!;
		double width = first.Context.Points[0].High!.Value - first.Context.Points[0].Low!.Value;
		Assert.Contains(width, new[] { 5d, 10, 20, 25, 40, 50 });
		Assert.InRange(cut.Instance.DrawnCount(0), 10, 200);
	}

	[Fact]
	public void AMillionValues_CountInOnePass()
	{
		Random random = new(9);
		double[] values = [.. Enumerable.Range(0, 1_000_000).Select(_ => random.NextDouble() * 1000)];
		System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
		IRenderedComponent<MokaHistogram> cut = Render<MokaHistogram>(p => p
			.Add(x => x.Values, values)
			.Add(x => x.DataVersion, 1)
			.Add(x => x.BucketSize, 10));
		watch.Stop();

		Assert.Equal(100, cut.Instance.DrawnCount(0));
		Assert.InRange(watch.ElapsedMilliseconds, 0, 5000);
	}

	[Fact]
	public void TheTable_ListsEachBucket()
	{
		IRenderedComponent<MokaHistogram> cut = Render<MokaHistogram>(p => p
			.Add(x => x.Values, Times)
			.Add(x => x.BucketSize, 25)
			.Add(x => x.CountLabel, "requests"));

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(["From", "To", "Requests"], table.Headers);
		Assert.Equal(6, table.RowCount);
		Assert.Equal(["25", "50", "4"], Enumerable.Range(0, 3).Select(c => table.Raw(1, c)));
	}

	[Fact]
	public void NothingToCount_IsTheEmptyState()
	{
		IRenderedComponent<MokaHistogram> cut = Render<MokaHistogram>(p => p.Add(x => x.Values, [double.NaN]));

		Assert.False(cut.Instance.HasData);
		Assert.Empty(cut.FindAll(".moka-chart-mark"));
		Assert.NotNull(cut.Find(".moka-chart-frame__state--empty"));
	}

	[Fact]
	public void TheValuesUnit_StaysOffTheCounts()
	{
		IRenderedComponent<MokaHistogram> cut = Render<MokaHistogram>(p => p
			.Add(x => x.Values, Times)
			.Add(x => x.BucketSize, 25)
			.Add(x => x.Unit, MokaChartUnit.Milliseconds));

		Assert.DoesNotContain(cut.FindAll(".moka-chart-plot__ytick"), tick => tick.TextContent.Contains("ms", StringComparison.Ordinal));
	}

	private static double Gauss(Random random) =>
		Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());
}
