namespace Moka.Red.Core.Charts;

/// <summary>Where a box plot's whiskers end.</summary>
public enum MokaWhiskers
{
	/// <summary>At the 5th and the 95th percentile.</summary>
	Percentile5To95,

	/// <summary>At the lowest and the highest value, so there are no outliers.</summary>
	MinMax,

	/// <summary>At the most extreme values within one and a half times the middle half's spread of its edges.</summary>
	Tukey
}

/// <summary>What a box plot draws for one group of values.</summary>
/// <param name="Min">The lowest value.</param>
/// <param name="Low">Where the lower whisker ends.</param>
/// <param name="Q1">The 25th percentile, the box's lower edge.</param>
/// <param name="Median">The 50th percentile.</param>
/// <param name="Q3">The 75th percentile, the box's upper edge.</param>
/// <param name="High">Where the upper whisker ends.</param>
/// <param name="Max">The highest value.</param>
/// <param name="Count">How many values there were.</param>
/// <param name="Outliers">The values beyond the whiskers, in order.</param>
public sealed record MokaBoxSummary(
	double Min,
	double Low,
	double Q1,
	double Median,
	double Q3,
	double High,
	double Max,
	int Count,
	IReadOnlyList<double> Outliers);

/// <summary>One bar of a histogram: the values from <see cref="From" /> up to, not including, <see cref="To" />.</summary>
/// <param name="From">Where the bin starts.</param>
/// <param name="To">Where the next bin starts.</param>
/// <param name="Count">How many values fell in it.</param>
public readonly record struct MokaHistogramBin(double From, double To, int Count);

/// <summary>
///     A straight line fitted through points by least squares. It keeps the points' middle, so a line over times in Unix
///     milliseconds reads back without losing its digits to the size of the x values.
/// </summary>
/// <param name="Slope">How much y rises for each unit of x. NaN when there was no line to fit.</param>
/// <param name="MeanX">The middle of the points' x values, which the line passes over at <see cref="MeanY" />.</param>
/// <param name="MeanY">The middle of their y values.</param>
/// <param name="Count">How many points it was fitted through.</param>
public readonly record struct MokaLinearFit(double Slope, double MeanX, double MeanY, int Count)
{
	/// <summary>Whether there was a line to fit: two points or more, at two x values or more.</summary>
	public bool HasLine => double.IsFinite(Slope);

	/// <summary>Where the line crosses x = 0.</summary>
	public double Intercept => MeanY - Slope * MeanX;

	/// <summary>The line's y at an x. NaN when there is no line.</summary>
	/// <param name="x">The x.</param>
	public double At(double x) => HasLine ? MeanY + Slope * (x - MeanX) : double.NaN;
}

/// <summary>Quantiles, box summaries, histograms, moving averages and straight-line trends over a set of values.</summary>
public static class MokaChartStats
{
	/// <summary>
	///     The trailing moving average: each value's mean with the values before it in a window of
	///     <paramref name="window" />. A value that isn't a finite number doesn't count, so the mean steps over a gap.
	/// </summary>
	/// <param name="values">The series, in order.</param>
	/// <param name="window">How many values each mean covers, the value itself included. Less than 1 counts as 1.</param>
	/// <returns>
	///     One mean per value. NaN for the first <c>window - 1</c> values, before a window has filled, and where the window
	///     holds no finite value.
	/// </returns>
	public static double[] MovingAverage(ReadOnlySpan<double> values, int window)
	{
		window = Math.Max(1, window);
		double[] means = new double[values.Length];
		double sum = 0;
		int finite = 0;
		for (int i = 0; i < values.Length; i++)
		{
			if (double.IsFinite(values[i]))
			{
				sum += values[i];
				finite++;
			}

			int leaving = i - window;
			if (leaving >= 0 && double.IsFinite(values[leaving]))
			{
				sum -= values[leaving];
				finite--;
			}

			// A running sum drifts as values come and go; once the window holds nothing it starts clean again.
			if (finite == 0)
			{
				sum = 0;
			}

			means[i] = i < window - 1 || finite == 0 ? double.NaN : sum / finite;
		}

		return means;
	}

	/// <summary>The straight line nearest to the points, by least squares. A pair with an x or y that isn't a finite number doesn't count.</summary>
	/// <param name="x">Each point's x.</param>
	/// <param name="y">Each point's y, one per x.</param>
	/// <returns>The line; <see cref="MokaLinearFit.HasLine" /> is false with fewer than two points or a single x.</returns>
	public static MokaLinearFit LinearFit(ReadOnlySpan<double> x, ReadOnlySpan<double> y)
	{
		int count = Math.Min(x.Length, y.Length);
		double sumX = 0;
		double sumY = 0;
		int used = 0;
		for (int i = 0; i < count; i++)
		{
			if (double.IsFinite(x[i]) && double.IsFinite(y[i]))
			{
				sumX += x[i];
				sumY += y[i];
				used++;
			}
		}

		if (used == 0)
		{
			return new MokaLinearFit(double.NaN, double.NaN, double.NaN, 0);
		}

		double meanX = sumX / used;
		double meanY = sumY / used;
		double across = 0;
		double together = 0;
		for (int i = 0; i < count; i++)
		{
			if (double.IsFinite(x[i]) && double.IsFinite(y[i]))
			{
				double dx = x[i] - meanX;
				across += dx * dx;
				together += dx * (y[i] - meanY);
			}
		}

		double slope = used >= 2 && across > 0 ? together / across : double.NaN;
		return new MokaLinearFit(slope, meanX, meanY, used);
	}

	/// <summary>The straight line nearest to evenly spaced values, each at its index, by least squares.</summary>
	/// <param name="y">The values, in order. One that isn't a finite number doesn't count.</param>
	/// <returns>The line, with x counted in indices.</returns>
	public static MokaLinearFit LinearFit(ReadOnlySpan<double> y)
	{
		double[] x = new double[y.Length];
		for (int i = 0; i < x.Length; i++)
		{
			x[i] = i;
		}

		return LinearFit(x, y);
	}

	/// <summary>
	///     The value below which a share <paramref name="p" /> of the values fall, interpolating between neighbours
	///     (Hyndman and Fan's type 7, what spreadsheets and NumPy use).
	/// </summary>
	/// <param name="sorted">The values, lowest first, all finite.</param>
	/// <param name="p">The share, 0 to 1. Clamped.</param>
	/// <returns>The quantile, or NaN when there are no values.</returns>
	public static double Quantile(ReadOnlySpan<double> sorted, double p)
	{
		if (sorted.Length == 0 || double.IsNaN(p))
		{
			return double.NaN;
		}

		double position = (sorted.Length - 1) * Math.Clamp(p, 0, 1);
		int below = (int)Math.Floor(position);
		int above = Math.Min(below + 1, sorted.Length - 1);
		return sorted[below] + (position - below) * (sorted[above] - sorted[below]);
	}

	/// <summary>What a box plot draws for these values. A value that isn't a finite number doesn't count.</summary>
	/// <param name="values">The values, in any order.</param>
	/// <param name="whiskers">Where the whiskers end.</param>
	/// <returns>The summary. Every number is NaN when there are no values.</returns>
	public static MokaBoxSummary Box(ReadOnlySpan<double> values, MokaWhiskers whiskers = MokaWhiskers.Percentile5To95)
	{
		double[] sorted = Finite(values);
		if (sorted.Length == 0)
		{
			return new MokaBoxSummary(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, 0, []);
		}

		Array.Sort(sorted);
		double q1 = Quantile(sorted, 0.25);
		double median = Quantile(sorted, 0.5);
		double q3 = Quantile(sorted, 0.75);
		(double low, double high) = whiskers switch
		{
			MokaWhiskers.MinMax => (sorted[0], sorted[^1]),
			MokaWhiskers.Tukey => Tukey(sorted, q1, q3),
			_ => (Quantile(sorted, 0.05), Quantile(sorted, 0.95))
		};

		List<double> outliers = [];
		foreach (double value in sorted)
		{
			if (value < low || value > high)
			{
				outliers.Add(value);
			}
		}

		return new MokaBoxSummary(sorted[0], low, q1, median, q3, high, sorted[^1], sorted.Length, outliers);
	}

	/// <summary>
	///     Counts the values into bins <paramref name="bucketSize" /> wide. A value that isn't a finite number doesn't
	///     count, so the counts add up to the finite values.
	/// </summary>
	/// <param name="values">The values, in any order.</param>
	/// <param name="bucketSize">How wide each bin is. Must be above zero.</param>
	/// <param name="origin">
	///     A value bins start at; the first bin starts at the last such edge at or below the lowest value. Null starts bins
	///     at multiples of the size.
	/// </param>
	/// <param name="maxBins">The most bins to come back with. More widen each bin by a whole factor until they fit.</param>
	/// <returns>The bins, lowest first, empty ones included. Empty when there are no values.</returns>
	public static IReadOnlyList<MokaHistogramBin> Histogram(ReadOnlySpan<double> values, double bucketSize,
		double? origin = null, int maxBins = 200)
	{
		if (!double.IsFinite(bucketSize) || bucketSize <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(bucketSize), bucketSize, "A bin has to be wider than zero.");
		}

		double[] finite = Finite(values);
		if (finite.Length == 0)
		{
			return [];
		}

		double lowest = finite.Min();
		double highest = finite.Max();
		double anchor = origin is { } given && double.IsFinite(given) ? given : 0;
		maxBins = Math.Max(1, maxBins);

		double size = bucketSize;
		double start = Start(lowest, anchor, size);
		long count = BinCount(start, highest, size);
		if (count > maxBins)
		{
			size *= Math.Ceiling((double)count / maxBins);
			start = Start(lowest, anchor, size);
			count = BinCount(start, highest, size);
			while (count > maxBins)
			{
				size *= 2;
				start = Start(lowest, anchor, size);
				count = BinCount(start, highest, size);
			}
		}

		int[] counts = new int[count];
		foreach (double value in finite)
		{
			long bin = (long)Math.Floor((value - start) / size);
			counts[Math.Clamp(bin, 0, count - 1)]++;
		}

		MokaHistogramBin[] bins = new MokaHistogramBin[count];
		for (int i = 0; i < count; i++)
		{
			double from = start + i * size;
			bins[i] = new MokaHistogramBin(from, from + size, counts[i]);
		}

		return bins;
	}

	private static double Start(double lowest, double anchor, double size) =>
		anchor + Math.Floor((lowest - anchor) / size) * size;

	private static long BinCount(double start, double highest, double size) =>
		(long)Math.Floor((highest - start) / size) + 1;

	private static (double Low, double High) Tukey(double[] sorted, double q1, double q3)
	{
		double reach = 1.5 * (q3 - q1);
		double low = sorted[0];
		double high = sorted[^1];
		foreach (double value in sorted)
		{
			if (value >= q1 - reach)
			{
				low = value;
				break;
			}
		}

		for (int i = sorted.Length - 1; i >= 0; i--)
		{
			if (sorted[i] <= q3 + reach)
			{
				high = sorted[i];
				break;
			}
		}

		return (low, high);
	}

	private static double[] Finite(ReadOnlySpan<double> values)
	{
		List<double> finite = new(values.Length);
		foreach (double value in values)
		{
			if (double.IsFinite(value))
			{
				finite.Add(value);
			}
		}

		return [.. finite];
	}
}
