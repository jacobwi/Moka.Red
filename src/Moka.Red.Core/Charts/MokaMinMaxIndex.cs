namespace Moka.Red.Core.Charts;

/// <summary>
///     The lowest and highest value and the first gap of every block of 32 samples of a series that grows at its end
///     or slides along, such as a live window. <see cref="MinMax(ReadOnlySpan{double}, int, int, int)" /> then picks
///     the points to draw over any window of the series from the blocks it covers whole and the few values at its two
///     ends, instead of reading every value. The picks are exactly the ones
///     <see cref="MokaDownsampler.MinMax(ReadOnlySpan{double}, int)" /> makes from the same values, ties and gaps
///     included.
/// </summary>
/// <remarks>
///     The index keeps no values of its own: every call is handed the series as it is now, and has to be handed the same
///     values the index was told about through <see cref="Reset" />, <see cref="Append" /> and <see cref="RemoveFirst" />.
/// </remarks>
public sealed class MokaMinMaxIndex
{
	private const int BlockSize = 32;

	private Block[] _blocks = new Block[16];
	private int _head;
	private int _used;
	private long _firstBlock;
	private long _origin;
	private long _end;

	/// <summary>How many samples the index covers.</summary>
	public int Count => (int)(_end - _origin);

	/// <summary>Forgets everything and covers these values.</summary>
	/// <param name="values">The series.</param>
	public void Reset(ReadOnlySpan<double> values)
	{
		_head = 0;
		_used = 0;
		_firstBlock = 0;
		_origin = 0;
		_end = 0;
		Append(values);
	}

	/// <summary>Adds samples at the end.</summary>
	/// <param name="values">The new samples, in order.</param>
	public void Append(ReadOnlySpan<double> values)
	{
		foreach (double value in values)
		{
			long block = _end / BlockSize;
			int offset = (int)(_end % BlockSize);
			if (offset == 0 || _used == 0)
			{
				AddBlock(block);
			}

			ref Block last = ref _blocks[_head + _used - 1];
			last = last.With(value, offset);
			_end++;
		}
	}

	/// <summary>Drops samples from the start, as a sliding window does.</summary>
	/// <param name="count">How many. More than there are drops them all.</param>
	public void RemoveFirst(int count)
	{
		if (count <= 0)
		{
			return;
		}

		_origin = Math.Min(_end, _origin + count);
		long keep = _origin / BlockSize;
		int dropped = (int)Math.Clamp(keep - _firstBlock, 0, _used);
		_head += dropped;
		_used -= dropped;
		_firstBlock += dropped;
		if (_used == 0)
		{
			_head = 0;
			_firstBlock = _end / BlockSize;
		}
	}

	/// <summary>
	///     The points to draw of a window of the series: the same indices <see cref="MokaDownsampler.MinMax(ReadOnlySpan{double}, int)" />
	///     returns for <c>values.Slice(start, length)</c>, with <paramref name="start" /> added, so they index
	///     <paramref name="values" />.
	/// </summary>
	/// <param name="values">The series the index covers, all of it.</param>
	/// <param name="start">Where the window starts.</param>
	/// <param name="length">How many values it holds.</param>
	/// <param name="buckets">How many runs to split it into.</param>
	/// <returns>The indices to draw, in order.</returns>
	public int[] MinMax(ReadOnlySpan<double> values, int start, int length, int buckets)
	{
		Check(values, start, length);
		buckets = Math.Max(1, buckets);
		if (length <= 2 * buckets)
		{
			return Offset(MokaDownsampler.All(length), start);
		}

		List<int> kept = new(2 * buckets + 3) { start };
		for (int bucket = 0; bucket < buckets; bucket++)
		{
			int from = start + (int)((long)bucket * length / buckets);
			int to = start + (int)((long)(bucket + 1) * length / buckets);
			Range(values, from, to).AddTo(kept);
		}

		kept.Add(start + length - 1);
		return MokaDownsampler.Distinct(kept);
	}

	/// <summary>
	///     The points to draw of a window of values at their own x, such as times: the same indices
	///     <see cref="MokaDownsampler.MinMax(ReadOnlySpan{double}, ReadOnlySpan{double}, int)" /> returns for the two slices,
	///     with <paramref name="start" /> added. Every x in the window has to be a finite number, none lower than the one
	///     before; the downsampler itself takes any x.
	/// </summary>
	/// <param name="x">Where each value of the series sits, one per value.</param>
	/// <param name="values">The series the index covers, all of it.</param>
	/// <param name="start">Where the window starts.</param>
	/// <param name="length">How many values it holds.</param>
	/// <param name="buckets">How many spans to split its x range into.</param>
	/// <returns>The indices to draw, in order.</returns>
	public int[] MinMax(ReadOnlySpan<double> x, ReadOnlySpan<double> values, int start, int length, int buckets)
	{
		Check(values, start, length);
		if (x.Length < start + length)
		{
			throw new ArgumentException("There has to be an x for every value of the window.", nameof(x));
		}

		buckets = Math.Max(1, buckets);
		if (length <= 2 * buckets)
		{
			return Offset(MokaDownsampler.All(length), start);
		}

		double first = x[start];
		double last = x[start + length - 1];
		if (!(last > first))
		{
			return MinMax(values, start, length, buckets);
		}

		double span = last - first;
		List<int> kept = new(2 * buckets + 3) { start };
		int end = start + length;
		int run = start;
		while (run < end)
		{
			int bucket = MokaDownsampler.SpanBucket(x[run], first, span, buckets);
			int next = FirstPast(x, run + 1, end, bucket, first, span, buckets);
			Range(values, run, next).AddTo(kept);
			run = next;
		}

		kept.Add(end - 1);
		return MokaDownsampler.Distinct(kept);
	}

	// The first index from `from` on whose x falls in a later span than `bucket`; x runs upwards, so the spans do too.
	private static int FirstPast(ReadOnlySpan<double> x, int from, int to, int bucket, double first, double span, int buckets)
	{
		int low = from;
		int high = to;
		while (low < high)
		{
			int middle = low + (high - low) / 2;
			if (MokaDownsampler.SpanBucket(x[middle], first, span, buckets) <= bucket)
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

	// The extremes of values[from..to), in the order a scan would meet them: the loose values before the first whole
	// block, the whole blocks, then the loose values after the last.
	private Extremes Range(ReadOnlySpan<double> values, int from, int to)
	{
		long a0 = _origin + from;
		long a1 = _origin + to;
		long firstWhole = (a0 + BlockSize - 1) / BlockSize;
		long endWhole = a1 / BlockSize;
		Extremes extremes = Extremes.Empty;
		if (firstWhole >= endWhole)
		{
			for (int i = from; i < to; i++)
			{
				extremes = extremes.With(values[i], i);
			}

			return extremes;
		}

		int headEnd = (int)(firstWhole * BlockSize - _origin);
		for (int i = from; i < headEnd; i++)
		{
			extremes = extremes.With(values[i], i);
		}

		for (long block = firstWhole; block < endWhole; block++)
		{
			int start = (int)(block * BlockSize - _origin);
			extremes = extremes.Merge(_blocks[_head + (int)(block - _firstBlock)].AsExtremes(start));
		}

		for (int i = (int)(endWhole * BlockSize - _origin); i < to; i++)
		{
			extremes = extremes.With(values[i], i);
		}

		return extremes;
	}

	private void Check(ReadOnlySpan<double> values, int start, int length)
	{
		if (values.Length != Count)
		{
			throw new ArgumentException("The values aren't the series the index covers.", nameof(values));
		}

		ArgumentOutOfRangeException.ThrowIfNegative(start);
		ArgumentOutOfRangeException.ThrowIfNegative(length);
		if (start + length > values.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(length), length, "The window runs past the end of the series.");
		}
	}

	private static int[] Offset(int[] indices, int start)
	{
		for (int i = 0; i < indices.Length; i++)
		{
			indices[i] += start;
		}

		return indices;
	}

	private void AddBlock(long block)
	{
		if (_used == 0)
		{
			_head = 0;
			_firstBlock = block;
		}

		if (_head + _used == _blocks.Length)
		{
			if (_used * 2 <= _blocks.Length)
			{
				Array.Copy(_blocks, _head, _blocks, 0, _used);
			}
			else
			{
				Block[] grown = new Block[_blocks.Length * 2];
				Array.Copy(_blocks, _head, grown, 0, _used);
				_blocks = grown;
			}

			_head = 0;
		}

		_blocks[_head + _used] = Block.Empty;
		_used++;
	}

	// One block's extremes by their offset in the block, so a stream that runs for years never outgrows an int.
	private readonly record struct Block(int Min, double MinValue, int Max, double MaxValue, int Gap)
	{
		public static Block Empty { get; } = new(-1, double.NaN, -1, double.NaN, -1);

		public Block With(double value, int offset)
		{
			if (!double.IsFinite(value))
			{
				return Gap < 0 ? this with { Gap = offset } : this;
			}

			Block next = this;
			if (Min < 0 || value < MinValue)
			{
				next = next with { Min = offset, MinValue = value };
			}

			if (Max < 0 || value > MaxValue)
			{
				next = next with { Max = offset, MaxValue = value };
			}

			return next;
		}

		public Extremes AsExtremes(int start) => new(
			Min < 0 ? -1 : start + Min, MinValue,
			Max < 0 ? -1 : start + Max, MaxValue,
			Gap < 0 ? -1 : start + Gap);
	}
}
