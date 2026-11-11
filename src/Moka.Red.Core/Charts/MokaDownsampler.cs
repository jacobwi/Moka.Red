namespace Moka.Red.Core.Charts;

/// <summary>How the values that fall into one bucket are merged into one.</summary>
public enum MokaChartAggregate
{
	/// <summary>Added together.</summary>
	Sum,

	/// <summary>Their average.</summary>
	Mean,

	/// <summary>The lowest.</summary>
	Min,

	/// <summary>The highest.</summary>
	Max,

	/// <summary>The first one.</summary>
	First,

	/// <summary>The last one.</summary>
	Last
}

/// <summary>
///     Picks which points of a long series to draw. Every method returns indices into the input, in order, so a chart
///     maps a drawn point back to its raw value and tooltips stay exact. The first and last index are always kept, and a
///     value that isn't a finite number is a gap, which the picked points keep so a line still breaks there.
/// </summary>
public static class MokaDownsampler
{
	/// <summary>
	///     For evenly spaced values: splits them into <paramref name="buckets" /> equal runs and keeps, per run, the lowest
	///     and the highest value, so every peak and trough survives. A series of at most twice as many values as buckets
	///     comes back whole.
	/// </summary>
	/// <param name="values">The series.</param>
	/// <param name="buckets">How many runs to split it into, usually the plot's width in pixels.</param>
	/// <returns>The indices to draw, in order.</returns>
	public static int[] MinMax(ReadOnlySpan<double> values, int buckets)
	{
		int count = values.Length;
		buckets = Math.Max(1, buckets);
		if (count <= 2 * buckets)
		{
			return All(count);
		}

		List<int> kept = new(2 * buckets + 3) { 0 };
		for (int bucket = 0; bucket < buckets; bucket++)
		{
			int start = (int)((long)bucket * count / buckets);
			int end = (int)((long)(bucket + 1) * count / buckets);
			Extremes extremes = Extremes.Empty;
			for (int i = start; i < end; i++)
			{
				extremes = extremes.With(values[i], i);
			}

			extremes.AddTo(kept);
		}

		kept.Add(count - 1);
		return Distinct(kept);
	}

	/// <summary>
	///     For values at their own x, such as times: splits the x range into <paramref name="buckets" /> equal spans and
	///     keeps, per span, the lowest and the highest value. <paramref name="x" /> must run upwards.
	/// </summary>
	/// <param name="x">Where each value sits, in order.</param>
	/// <param name="values">The series, one per x.</param>
	/// <param name="buckets">How many spans to split the range into.</param>
	/// <returns>The indices to draw, in order.</returns>
	public static int[] MinMax(ReadOnlySpan<double> x, ReadOnlySpan<double> values, int buckets)
	{
		int count = Math.Min(x.Length, values.Length);
		buckets = Math.Max(1, buckets);
		if (count <= 2 * buckets)
		{
			return All(count);
		}

		(double first, double last) = Ends(x[..count]);
		if (!(last > first))
		{
			return MinMax(values[..count], buckets);
		}

		List<int> kept = new(2 * buckets + 3) { 0 };
		double span = last - first;
		int current = 0;
		Extremes extremes = Extremes.Empty;
		for (int i = 0; i < count; i++)
		{
			if (double.IsFinite(x[i]))
			{
				int bucket = Math.Clamp((int)Math.Floor((x[i] - first) / span * buckets), 0, buckets - 1);
				if (bucket != current)
				{
					extremes.AddTo(kept);
					extremes = Extremes.Empty;
					current = bucket;
				}
			}

			extremes = extremes.With(values[i], i);
		}

		extremes.AddTo(kept);
		kept.Add(count - 1);
		return Distinct(kept);
	}

	/// <summary>
	///     Largest triangle three buckets (Steinarsson): keeps <paramref name="threshold" /> evenly spaced values that hold
	///     the series' shape. Each stretch between gaps gets a share of the threshold by its length.
	/// </summary>
	/// <param name="values">The series.</param>
	/// <param name="threshold">How many points to keep.</param>
	/// <returns>The indices to draw, in order.</returns>
	public static int[] Lttb(ReadOnlySpan<double> values, int threshold) => Lttb(default, values, threshold, false);

	/// <summary>Largest triangle three buckets for values at their own x. <paramref name="x" /> must run upwards.</summary>
	/// <param name="x">Where each value sits, in order.</param>
	/// <param name="values">The series, one per x.</param>
	/// <param name="threshold">How many points to keep.</param>
	/// <returns>The indices to draw, in order.</returns>
	public static int[] Lttb(ReadOnlySpan<double> x, ReadOnlySpan<double> values, int threshold) =>
		Lttb(x, values, threshold, true);

	/// <summary>Every index any of the series keeps, in order, each once. For a crosshair shared by downsampled series.</summary>
	/// <param name="perSeries">The indices each series keeps.</param>
	/// <returns>Their union.</returns>
	public static int[] Union(IReadOnlyList<int[]> perSeries)
	{
		ArgumentNullException.ThrowIfNull(perSeries);

		List<int> all = [];
		foreach (int[] indices in perSeries)
		{
			if (indices is not null)
			{
				all.AddRange(indices);
			}
		}

		all.Sort();
		return Distinct(all);
	}

	/// <summary>
	///     The indices of the points from <paramref name="from" /> to <paramref name="to" />, with one more on each side
	///     where there is one, so a line runs to the edges of a zoomed plot. <paramref name="x" /> must run upwards.
	/// </summary>
	/// <param name="x">Where each point sits, in order.</param>
	/// <param name="from">The window's start.</param>
	/// <param name="to">The window's end.</param>
	/// <returns>The first and last index, both included; (0, -1) when there are no points.</returns>
	public static (int Start, int End) Window(ReadOnlySpan<double> x, double from, double to)
	{
		if (x.Length == 0)
		{
			return (0, -1);
		}

		if (from > to)
		{
			(from, to) = (to, from);
		}

		int firstInside = LowerBound(x, from);
		int lastInside = UpperBound(x, to) - 1;
		int start = Math.Clamp(firstInside - 1, 0, x.Length - 1);
		int end = Math.Clamp(lastInside + 1, 0, x.Length - 1);
		return start <= end ? (start, end) : (end, start);
	}

	/// <summary>
	///     Merges evenly spaced values into at most <paramref name="buckets" /> values, such as bars too many to draw. A
	///     value that isn't a finite number doesn't count, and a bucket with none is NaN.
	/// </summary>
	/// <param name="values">The values.</param>
	/// <param name="buckets">How many values to come back with.</param>
	/// <param name="how">How a bucket's values are merged.</param>
	/// <returns>One value per bucket, or the values as they are when there are no more of them than buckets.</returns>
	public static double[] Buckets(ReadOnlySpan<double> values, int buckets, MokaChartAggregate how)
	{
		int count = values.Length;
		if (buckets <= 0 || count == 0)
		{
			return [];
		}

		int made = Math.Min(buckets, count);
		double[] merged = new double[made];
		for (int bucket = 0; bucket < made; bucket++)
		{
			int start = (int)((long)bucket * count / made);
			int end = (int)((long)(bucket + 1) * count / made);
			merged[bucket] = Aggregate(values[start..end], how);
		}

		return merged;
	}

	/// <summary>
	///     Keeps the first point that falls in each cell of a <paramref name="columns" /> by <paramref name="rows" /> grid,
	///     for a scatter plot with more points than pixels. A point outside the plot counts in the nearest edge cell, and
	///     one whose position isn't a finite number is dropped.
	/// </summary>
	/// <param name="xShare">Each point's x as a share of the plot's width, 0 to 1.</param>
	/// <param name="yShare">Each point's y as a share of the plot's height, 0 to 1.</param>
	/// <param name="columns">Cells across.</param>
	/// <param name="rows">Cells down.</param>
	/// <returns>The indices kept, in order.</returns>
	public static int[] GridDedupe(ReadOnlySpan<double> xShare, ReadOnlySpan<double> yShare, int columns, int rows)
	{
		int count = Math.Min(xShare.Length, yShare.Length);
		columns = Math.Max(1, columns);
		rows = Math.Max(1, rows);
		HashSet<long> taken = [];
		List<int> kept = [];
		for (int i = 0; i < count; i++)
		{
			if (!double.IsFinite(xShare[i]) || !double.IsFinite(yShare[i]))
			{
				continue;
			}

			long column = Math.Clamp((long)Math.Floor(xShare[i] * columns), 0, columns - 1);
			long row = Math.Clamp((long)Math.Floor(yShare[i] * rows), 0, rows - 1);
			if (taken.Add(row * columns + column))
			{
				kept.Add(i);
			}
		}

		return [.. kept];
	}

	private static int[] Lttb(ReadOnlySpan<double> x, ReadOnlySpan<double> values, int threshold, bool byX)
	{
		int count = byX ? Math.Min(x.Length, values.Length) : values.Length;
		threshold = Math.Max(2, threshold);
		if (count <= threshold)
		{
			return All(count);
		}

		IReadOnlyList<(int First, int Last)> runs = MokaSeriesRuns.Of(values[..count]);
		int total = 0;
		foreach ((int first, int last) in runs)
		{
			total += last - first + 1;
		}

		List<int> kept = [0];
		int previousEnd = -1;
		foreach ((int first, int last) in runs)
		{
			if (first > previousEnd + 1)
			{
				kept.Add(previousEnd + 1);
			}

			int length = last - first + 1;
			int share = Math.Max(Math.Min(length, 2), (int)Math.Round((double)threshold * length / total));
			if (share >= length)
			{
				for (int i = first; i <= last; i++)
				{
					kept.Add(i);
				}
			}
			else
			{
				Triangles(x, values, first, last, share, byX, kept);
			}

			previousEnd = last;
		}

		if (previousEnd < count - 1)
		{
			kept.Add(previousEnd + 1);
		}

		kept.Add(count - 1);
		kept.Sort();
		return Distinct(kept);
	}

	private static void Triangles(ReadOnlySpan<double> x, ReadOnlySpan<double> values, int first, int last, int share,
		bool byX, List<int> kept)
	{
		int length = last - first + 1;
		kept.Add(first);
		if (share > 2)
		{
			double every = (double)(length - 2) / (share - 2);
			int anchor = first;
			for (int i = 0; i < share - 2; i++)
			{
				int averageStart = first + (int)Math.Floor((i + 1) * every) + 1;
				int averageEnd = Math.Min(first + (int)Math.Floor((i + 2) * every) + 1, last + 1);
				double averageX = 0;
				double averageY = 0;
				int averaged = Math.Max(0, averageEnd - averageStart);
				for (int j = averageStart; j < averageEnd; j++)
				{
					averageX += byX ? x[j] : j;
					averageY += values[j];
				}

				if (averaged > 0)
				{
					averageX /= averaged;
					averageY /= averaged;
				}
				else
				{
					averageX = byX ? x[last] : last;
					averageY = values[last];
				}

				int rangeStart = first + (int)Math.Floor(i * every) + 1;
				int rangeEnd = Math.Min(first + (int)Math.Floor((i + 1) * every) + 1, last);
				double anchorX = byX ? x[anchor] : anchor;
				double anchorY = values[anchor];
				double largest = -1;
				int picked = rangeStart;
				for (int j = rangeStart; j < rangeEnd; j++)
				{
					double pointX = byX ? x[j] : j;
					double area = Math.Abs((anchorX - averageX) * (values[j] - anchorY) - (anchorX - pointX) * (averageY - anchorY));
					if (area > largest)
					{
						largest = area;
						picked = j;
					}
				}

				kept.Add(picked);
				anchor = picked;
			}
		}

		kept.Add(last);
	}

	private static double Aggregate(ReadOnlySpan<double> values, MokaChartAggregate how)
	{
		double result = double.NaN;
		int counted = 0;
		foreach (double value in values)
		{
			if (!double.IsFinite(value))
			{
				continue;
			}

			counted++;
			result = how switch
			{
				_ when counted == 1 => value,
				MokaChartAggregate.Sum or MokaChartAggregate.Mean => result + value,
				MokaChartAggregate.Min => Math.Min(result, value),
				MokaChartAggregate.Max => Math.Max(result, value),
				MokaChartAggregate.Last => value,
				_ => result
			};
		}

		return how == MokaChartAggregate.Mean && counted > 0 ? result / counted : result;
	}

	private static (double First, double Last) Ends(ReadOnlySpan<double> x)
	{
		double first = double.NaN;
		double last = double.NaN;
		foreach (double value in x)
		{
			if (double.IsFinite(value))
			{
				if (double.IsNaN(first))
				{
					first = value;
				}

				last = value;
			}
		}

		return (first, last);
	}

	private static int LowerBound(ReadOnlySpan<double> x, double value)
	{
		int low = 0;
		int high = x.Length;
		while (low < high)
		{
			int middle = low + (high - low) / 2;
			if (x[middle] < value)
			{
				low = middle + 1;
			}
			else
			{
				high = middle;
			}
		}

		return low;
	}

	private static int UpperBound(ReadOnlySpan<double> x, double value)
	{
		int low = 0;
		int high = x.Length;
		while (low < high)
		{
			int middle = low + (high - low) / 2;
			if (x[middle] <= value)
			{
				low = middle + 1;
			}
			else
			{
				high = middle;
			}
		}

		return low;
	}

	private static int[] All(int count)
	{
		int[] all = new int[count];
		for (int i = 0; i < count; i++)
		{
			all[i] = i;
		}

		return all;
	}

	// The list is sorted; repeats sit next to each other.
	private static int[] Distinct(List<int> sorted)
	{
		List<int> distinct = new(sorted.Count);
		foreach (int index in sorted)
		{
			if (distinct.Count == 0 || distinct[^1] != index)
			{
				distinct.Add(index);
			}
		}

		return [.. distinct];
	}
}

/// <summary>The lowest and highest finite value of a run and its first gap, by index.</summary>
internal readonly record struct Extremes(int Min, double MinValue, int Max, double MaxValue, int Gap)
{
	public static Extremes Empty { get; } = new(-1, double.NaN, -1, double.NaN, -1);

	public Extremes With(double value, int index)
	{
		if (!double.IsFinite(value))
		{
			return Gap < 0 ? this with { Gap = index } : this;
		}

		Extremes next = this;
		if (Min < 0 || value < MinValue)
		{
			next = next with { Min = index, MinValue = value };
		}

		if (Max < 0 || value > MaxValue)
		{
			next = next with { Max = index, MaxValue = value };
		}

		return next;
	}

	// The earlier run's index wins a tie, as a scan in order would.
	public Extremes Merge(Extremes later)
	{
		Extremes merged = this;
		if (later.Min >= 0 && (Min < 0 || later.MinValue < MinValue))
		{
			merged = merged with { Min = later.Min, MinValue = later.MinValue };
		}

		if (later.Max >= 0 && (Max < 0 || later.MaxValue > MaxValue))
		{
			merged = merged with { Max = later.Max, MaxValue = later.MaxValue };
		}

		if (Gap < 0 && later.Gap >= 0)
		{
			merged = merged with { Gap = later.Gap };
		}

		return merged;
	}

	public void AddTo(List<int> kept)
	{
		Span<int> found = [Min, Max, Gap];
		found.Sort();
		foreach (int index in found)
		{
			if (index >= 0)
			{
				kept.Add(index);
			}
		}
	}
}
