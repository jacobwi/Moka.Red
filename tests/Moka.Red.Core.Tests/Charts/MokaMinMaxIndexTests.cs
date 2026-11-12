#pragma warning disable CA5394 // Seeded Random keeps the property tests repeatable; nothing here is security related

using Moka.Red.Core.Charts;

namespace Moka.Red.Core.Tests.Charts;

public class MokaMinMaxIndexTests
{
	// Rounded to a tenth, so ties are common, with a gap now and then: the cases where a scan's order decides the picks.
	private static double[] Noise(int count, int seed, double gapEvery = 40)
	{
		Random random = new(seed);
		double[] values = new double[count];
		for (int i = 0; i < count; i++)
		{
			values[i] = random.NextDouble() < 1 / gapEvery ? double.NaN : Math.Round(random.NextDouble() * 50, 0);
		}

		return values;
	}

	private static int[] Expected(double[] values, int start, int length, int buckets) =>
		[.. MokaDownsampler.MinMax(values.AsSpan(start, length), buckets).Select(i => i + start)];

	private static int[] Expected(double[] x, double[] values, int start, int length, int buckets) =>
		[.. MokaDownsampler.MinMax(x.AsSpan(start, length), values.AsSpan(start, length), buckets).Select(i => i + start)];

	[Theory]
	[InlineData(1, 5_000, 37)]
	[InlineData(2, 10_000, 800)]
	[InlineData(3, 3_001, 1_000)]
	[InlineData(4, 100_003, 413)]
	[InlineData(5, 77, 5)]
	public void MinMax_PicksWhatTheDownsamplerPicks_OverAnyWindow(int seed, int count, int buckets)
	{
		double[] values = Noise(count, seed);
		MokaMinMaxIndex index = new();
		index.Reset(values);
		Random random = new(seed);

		Assert.Equal(Expected(values, 0, count, buckets), index.MinMax(values, 0, count, buckets));
		for (int trial = 0; trial < 20; trial++)
		{
			int start = random.Next(count);
			int length = random.Next(count - start + 1);
			int each = random.Next(1, buckets + 1);
			Assert.Equal(Expected(values, start, length, each), index.MinMax(values, start, length, each));
		}
	}

	[Theory]
	[InlineData(11)]
	[InlineData(12)]
	[InlineData(13)]
	public void AGrowingSeries_PicksWhatAFreshScanPicks_AfterEveryAppend(int seed)
	{
		double[] all = Noise(20_000, seed);
		MokaMinMaxIndex index = new();
		index.Reset(all.AsSpan(0, 5_000));
		Random random = new(seed);
		int count = 5_000;
		while (count < all.Length)
		{
			int added = Math.Min(all.Length - count, random.Next(1, 700));
			index.Append(all.AsSpan(count, added));
			count += added;
			double[] now = all[..count];

			Assert.Equal(count, index.Count);
			Assert.Equal(Expected(now, 0, count, 400), index.MinMax(now, 0, count, 400));
		}
	}

	[Theory]
	[InlineData(21, 1)]
	[InlineData(22, 3)]
	[InlineData(23, 31)]
	[InlineData(24, 32)]
	[InlineData(25, 333)]
	public void ASlidingWindow_PicksWhatAFreshScanPicks_AfterEveryStep(int seed, int step)
	{
		const int Window = 8_000;
		double[] all = Noise(Window + 60 * step, seed);
		MokaMinMaxIndex index = new();
		index.Reset(all.AsSpan(0, Window));
		for (int origin = step; origin + Window <= all.Length; origin += step)
		{
			index.RemoveFirst(step);
			index.Append(all.AsSpan(origin + Window - step, step));
			double[] now = all[origin..(origin + Window)];

			Assert.Equal(Window, index.Count);
			Assert.Equal(Expected(now, 0, Window, 500), index.MinMax(now, 0, Window, 500));
			Assert.Equal(Expected(now, 1234, 4321, 300), index.MinMax(now, 1234, 4321, 300));
		}
	}

	[Theory]
	[InlineData(31)]
	[InlineData(32)]
	[InlineData(33)]
	public void ValuesAtTheirOwnX_PickWhatTheDownsamplerPicks(int seed)
	{
		Random random = new(seed);
		double[] values = Noise(30_000, seed);
		double[] x = new double[values.Length];
		double at = 1_700_000_000_000;
		for (int i = 0; i < x.Length; i++)
		{
			at += random.NextDouble() < 0.01 ? 60_000 : random.Next(0, 3) * 1_000;
			x[i] = at;
		}

		MokaMinMaxIndex index = new();
		index.Reset(values);

		Assert.Equal(Expected(x, values, 0, values.Length, 800), index.MinMax(x, values, 0, values.Length, 800));
		for (int trial = 0; trial < 20; trial++)
		{
			int start = random.Next(values.Length);
			int length = random.Next(values.Length - start + 1);
			int buckets = random.Next(1, 900);
			Assert.Equal(Expected(x, values, start, length, buckets), index.MinMax(x, values, start, length, buckets));
		}
	}

	[Fact]
	public void AllAtOneX_FallsBackToEvenRuns_AsTheDownsamplerDoes()
	{
		double[] values = Noise(5_000, 41);
		double[] x = [.. Enumerable.Repeat(7d, values.Length)];
		MokaMinMaxIndex index = new();
		index.Reset(values);

		Assert.Equal(Expected(x, values, 0, values.Length, 100), index.MinMax(x, values, 0, values.Length, 100));
	}

	[Fact]
	public void DroppingEverything_ThenAppending_StartsAgainCleanly()
	{
		double[] first = Noise(1_000, 51);
		double[] second = Noise(3_000, 52);
		MokaMinMaxIndex index = new();
		index.Reset(first);

		index.RemoveFirst(5_000);
		Assert.Equal(0, index.Count);
		Assert.Empty(index.MinMax(ReadOnlySpan<double>.Empty, 0, 0, 10));

		index.Append(second);
		Assert.Equal(Expected(second, 0, second.Length, 200), index.MinMax(second, 0, second.Length, 200));
	}

	[Fact]
	public void AShortWindow_ComesBackWhole()
	{
		double[] values = [1, double.NaN, 3, 4, 5];
		MokaMinMaxIndex index = new();
		index.Reset(values);

		Assert.Equal([1, 2, 3], index.MinMax(values, 1, 3, 10));
	}

	[Fact]
	public void ValuesOfAnotherLength_AreRefused()
	{
		MokaMinMaxIndex index = new();
		index.Reset([1, 2, 3]);

		Assert.Throws<ArgumentException>(() => index.MinMax(new double[4], 0, 4, 1));
		Assert.Throws<ArgumentOutOfRangeException>(() => index.MinMax(new double[3], 2, 2, 1));
	}
}
