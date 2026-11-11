#pragma warning disable CA5394 // Seeded Random keeps the property tests repeatable; nothing here is security related

using Moka.Red.Core.Charts;

namespace Moka.Red.Core.Tests.Charts;

public class MokaDownsamplerTests
{
	private static double[] Noise(int count, int seed, double gapEvery = 0)
	{
		Random random = new(seed);
		double[] values = new double[count];
		for (int i = 0; i < count; i++)
		{
			values[i] = gapEvery > 0 && random.NextDouble() < 1 / gapEvery ? double.NaN : Math.Round(random.NextDouble() * 1000, 1);
		}

		return values;
	}

	private static double[] Indexes(int count) => [.. Enumerable.Range(0, count).Select(i => (double)i)];

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	public void MinMax_KeepsEveryBucketsLowestAndHighest_AgainstABruteForce(int seed)
	{
		const int Buckets = 37;
		double[] values = Noise(1_000 + seed * 113, seed, gapEvery: 50);

		HashSet<int> kept = [.. MokaDownsampler.MinMax(values, Buckets)];

		for (int bucket = 0; bucket < Buckets; bucket++)
		{
			int start = (int)((long)bucket * values.Length / Buckets);
			int end = (int)((long)(bucket + 1) * values.Length / Buckets);
			int[] finite = [.. Enumerable.Range(start, end - start).Where(i => double.IsFinite(values[i]))];
			if (finite.Length > 0)
			{
				Assert.Contains(finite.MinBy(i => values[i]), kept);
				Assert.Contains(finite.MaxBy(i => values[i]), kept);
			}

			int gap = Array.FindIndex(values, start, end - start, double.IsNaN);
			if (gap >= 0)
			{
				Assert.Contains(gap, kept);
			}
		}

		Assert.Contains(0, kept);
		Assert.Contains(values.Length - 1, kept);
		Assert.True(kept.Count <= 3 * Buckets + 2);
	}

	[Fact]
	public void MinMax_ComesBackInOrder_EachOnce()
	{
		int[] kept = MokaDownsampler.MinMax(Noise(5_000, 9), 100);

		Assert.Equal(kept.Order().Distinct(), kept);
	}

	[Fact]
	public void MinMax_LeavesAShortSeriesWhole() =>
		Assert.Equal([0, 1, 2, 3, 4, 5], MokaDownsampler.MinMax(Noise(6, 1), 3));

	[Fact]
	public void MinMax_ByX_KeepsThePeaksOfUnevenSamples()
	{
		double[] x = [0, 1, 2, 3, 50, 51, 52, 100];
		double[] values = [5, 9, 1, 4, 7, 2, 8, 3];

		int[] kept = MokaDownsampler.MinMax(x, values, 2);

		Assert.Equal([0, 1, 2, 5, 6, 7], kept);
	}

	[Theory]
	[InlineData(7, 100)]
	[InlineData(8, 37)]
	[InlineData(9, 1_000)]
	public void Lttb_KeepsTheFirstAndTheLast_AndTheCount(int seed, int threshold)
	{
		double[] values = Noise(5_000, seed);

		int[] kept = MokaDownsampler.Lttb(values, threshold);

		Assert.Equal(threshold, kept.Length);
		Assert.Equal(0, kept[0]);
		Assert.Equal(values.Length - 1, kept[^1]);
		Assert.Equal(kept.Order().Distinct(), kept);
	}

	[Fact]
	public void Lttb_KeepsAGapSoTheLineStillBreaks()
	{
		double[] values = Noise(2_000, 3);
		values[1_000] = double.NaN;

		int[] kept = MokaDownsampler.Lttb(values, 200);

		Assert.Contains(1_000, kept);
	}

	[Fact]
	public void Lttb_ByX_KeepsTheFirstAndTheLast()
	{
		double[] values = Noise(3_000, 4);

		int[] kept = MokaDownsampler.Lttb(Indexes(3_000), values, 300);

		Assert.Equal(300, kept.Length);
		Assert.Equal(2_999, kept[^1]);
	}

	[Theory]
	[InlineData(11, 50, 2)]
	[InlineData(12, 37, 3)]
	[InlineData(13, 64, 4)]
	public void TheStream_EqualsOneShotMinMax_AfterEveryAppend(int seed, int buckets, int doublings)
	{
		int count = buckets << doublings;
		double[] values = Noise(count, seed, gapEvery: 40);
		double[] x = Indexes(count);
		MokaMinMaxStream stream = new(buckets);

		for (int i = 0; i < count; i++)
		{
			stream.Append(x[i], values[i], i);
		}

		Assert.Equal(MokaDownsampler.MinMax(x, values, buckets), stream.Indices());
	}

	[Fact]
	public void TheStream_Resets()
	{
		MokaMinMaxStream stream = new(4);
		stream.Append(0, 1, 0);
		stream.Append(1, 2, 1);

		stream.Reset();

		Assert.Empty(stream.Indices());
	}

	[Fact]
	public void Union_IsSortedAndDistinct() =>
		Assert.Equal([0, 2, 3, 5, 9], MokaDownsampler.Union([[0, 3, 9], [2, 3, 5], []]));

	[Fact]
	public void Window_KeepsOnePointEachSide()
	{
		double[] x = [0, 10, 20, 30, 40, 50];

		Assert.Equal((1, 4), MokaDownsampler.Window(x, 15, 35));
		Assert.Equal((0, 5), MokaDownsampler.Window(x, -5, 60));
		Assert.Equal((0, -1), MokaDownsampler.Window([], 0, 1));
	}

	[Theory]
	[InlineData(MokaChartAggregate.Sum, 3, 7)]
	[InlineData(MokaChartAggregate.Mean, 1.5, 3.5)]
	[InlineData(MokaChartAggregate.Min, 1, 3)]
	[InlineData(MokaChartAggregate.Max, 2, 4)]
	[InlineData(MokaChartAggregate.First, 1, 3)]
	[InlineData(MokaChartAggregate.Last, 2, 4)]
	public void Buckets_MergeEachRun(MokaChartAggregate how, double first, double second) =>
		Assert.Equal([first, second], MokaDownsampler.Buckets([1, 2, 3, 4], 2, how));

	[Fact]
	public void Buckets_SkipGaps_AndAnEmptyBucketIsNaN()
	{
		double[] merged = MokaDownsampler.Buckets([1, double.NaN, double.NaN, double.NaN], 2, MokaChartAggregate.Sum);

		Assert.Equal(1, merged[0]);
		Assert.True(double.IsNaN(merged[1]));
	}

	[Fact]
	public void GridDedupe_KeepsTheFirstPointInEachCell()
	{
		double[] x = [0.1, 0.12, 0.6, 0.61, 2, double.NaN];
		double[] y = [0.1, 0.11, 0.5, 0.9, 0.5, 0.5];

		Assert.Equal([0, 2, 3, 4], MokaDownsampler.GridDedupe(x, y, 4, 4));
	}
}
