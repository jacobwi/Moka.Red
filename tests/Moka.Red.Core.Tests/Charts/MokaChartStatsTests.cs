#pragma warning disable CA5394 // Seeded Random keeps the property tests repeatable; nothing here is security related

using Moka.Red.Core.Charts;

namespace Moka.Red.Core.Tests.Charts;

public class MokaChartStatsTests
{
	private static double[] OneToHundred => [.. Enumerable.Range(1, 100).Select(i => (double)i)];

	[Theory]
	[InlineData(0, 1)]
	[InlineData(0.25, 1.75)]
	[InlineData(0.5, 2.5)]
	[InlineData(0.75, 3.25)]
	[InlineData(1, 4)]
	[InlineData(2, 4)]
	public void Quantile_InterpolatesBetweenNeighbours(double p, double expected) =>
		Assert.Equal(expected, MokaChartStats.Quantile([1, 2, 3, 4], p), 9);

	[Fact]
	public void Quantile_OfNothing_IsNaN() => Assert.True(double.IsNaN(MokaChartStats.Quantile([], 0.5)));

	[Fact]
	public void Box_FromOneToAHundred()
	{
		MokaBoxSummary box = MokaChartStats.Box(OneToHundred);

		Assert.Equal(1, box.Min);
		Assert.Equal(5.95, box.Low, 9);
		Assert.Equal(25.75, box.Q1, 9);
		Assert.Equal(50.5, box.Median, 9);
		Assert.Equal(75.25, box.Q3, 9);
		Assert.Equal(95.05, box.High, 9);
		Assert.Equal(100, box.Max);
		Assert.Equal(100, box.Count);
		Assert.Equal(10, box.Outliers.Count);
	}

	[Fact]
	public void Box_WithMinMaxWhiskers_HasNoOutliers()
	{
		MokaBoxSummary box = MokaChartStats.Box(OneToHundred, MokaWhiskers.MinMax);

		Assert.Equal(1, box.Low);
		Assert.Equal(100, box.High);
		Assert.Empty(box.Outliers);
	}

	[Fact]
	public void Box_WithTukeyWhiskers_LeavesAFarValueOut()
	{
		double[] values = [10, 11, 12, 13, 14, 15, 16, 200, double.NaN];

		MokaBoxSummary box = MokaChartStats.Box(values, MokaWhiskers.Tukey);

		Assert.Equal(8, box.Count);
		Assert.Equal(16, box.High);
		Assert.Equal([200d], box.Outliers);
	}

	[Fact]
	public void Box_OfNothing_IsNaN()
	{
		MokaBoxSummary box = MokaChartStats.Box([double.NaN]);

		Assert.Equal(0, box.Count);
		Assert.True(double.IsNaN(box.Median));
	}

	[Fact]
	public void Histogram_CountsAddUpToTheFiniteValues()
	{
		Random random = new(5);
		double[] values = [.. Enumerable.Range(0, 10_000).Select(_ => random.NextDouble() * 400), double.NaN, double.PositiveInfinity];

		IReadOnlyList<MokaHistogramBin> bins = MokaChartStats.Histogram(values, 25);

		Assert.Equal(10_000, bins.Sum(bin => bin.Count));
		Assert.Equal(0, bins[0].From);
		Assert.All(bins, bin => Assert.Equal(25, bin.To - bin.From, 9));
	}

	[Fact]
	public void Histogram_BinsStartAtTheOrigin()
	{
		IReadOnlyList<MokaHistogramBin> bins = MokaChartStats.Histogram([12, 14, 31], 10, origin: 5);

		Assert.Equal([(5d, 15d, 2), (15d, 25d, 0), (25d, 35d, 1)], bins.Select(bin => (bin.From, bin.To, bin.Count)));
	}

	[Fact]
	public void Histogram_TooManyBins_WidenTheBucket()
	{
		IReadOnlyList<MokaHistogramBin> bins = MokaChartStats.Histogram([0, 1_000], 1, maxBins: 100);

		Assert.True(bins.Count <= 100);
		Assert.Equal(2, bins.Sum(bin => bin.Count));
	}

	[Fact]
	public void Histogram_NeedsABucketWiderThanZero() =>
		Assert.Throws<ArgumentOutOfRangeException>(() => MokaChartStats.Histogram([1], 0));

	[Fact]
	public void Runs_SplitAtEveryGap()
	{
		double[] values = [double.NaN, 1, 2, double.NaN, 3, double.PositiveInfinity, 4, 5];

		Assert.Equal([(1, 2), (4, 4), (6, 7)], MokaSeriesRuns.Of((IReadOnlyList<double>)values));
		Assert.Equal([(1, 2), (4, 4), (6, 7)], MokaSeriesRuns.Of(values.AsSpan()));
	}
}
