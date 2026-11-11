using System.Diagnostics.CodeAnalysis;

namespace Moka.Red.Core.Charts;

/// <summary>
///     <see cref="MokaDownsampler.MinMax(ReadOnlySpan{double}, ReadOnlySpan{double}, int)" /> for a series fed a sample at
///     a time, in bounded memory. It keeps a fixed number of buckets over a growing x range; when a sample lands past
///     the end, neighbouring buckets merge in pairs and each bucket covers twice the span, so an append never rescans
///     the series. The first bucket's width is the distance between the first two samples.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
	Justification = "A stream of samples, the word charts use; it is not a System.IO.Stream.")]
public sealed class MokaMinMaxStream
{
	private readonly Extremes[] _buckets;
	private double _origin = double.NaN;
	private double _width = double.NaN;
	private double _lastX = double.NaN;
	private int _first = -1;
	private int _last = -1;

	/// <summary>Creates a stream that keeps this many buckets.</summary>
	/// <param name="buckets">How many buckets to keep, usually the plot's width in pixels. At least 1.</param>
	public MokaMinMaxStream(int buckets)
	{
		_buckets = new Extremes[Math.Max(1, buckets)];
		Array.Fill(_buckets, Extremes.Empty);
	}

	/// <summary>
	///     Adds a sample. Its x has to be at least the last one's; an x that runs backwards, or isn't a finite number, counts
	///     as the last one's.
	/// </summary>
	/// <param name="x">Where the sample sits, such as its time.</param>
	/// <param name="value">The sample. A value that isn't a finite number is a gap.</param>
	/// <param name="index">The sample's index in the series, which <see cref="Indices" /> gives back.</param>
	public void Append(double x, double value, int index)
	{
		if (_first < 0)
		{
			_first = index;
			_origin = double.IsFinite(x) ? x : 0;
			_lastX = _origin;
		}
		else if (double.IsFinite(x) && x > _lastX)
		{
			_lastX = x;
		}

		if (double.IsNaN(_width) && _lastX > _origin)
		{
			_width = _lastX - _origin;
		}

		int bucket = BucketOf(_lastX);
		while (bucket >= _buckets.Length)
		{
			Halve();
			bucket = BucketOf(_lastX);
		}

		_buckets[bucket] = _buckets[bucket].With(value, index);
		_last = index;
	}

	/// <summary>The indices to draw, in order: each bucket's lowest and highest value and first gap, and the first and last sample.</summary>
	/// <returns>The indices.</returns>
	public int[] Indices()
	{
		if (_first < 0)
		{
			return [];
		}

		List<int> kept = [_first];
		foreach (Extremes bucket in _buckets)
		{
			bucket.AddTo(kept);
		}

		kept.Add(_last);
		kept.Sort();
		List<int> distinct = new(kept.Count);
		foreach (int index in kept)
		{
			if (distinct.Count == 0 || distinct[^1] != index)
			{
				distinct.Add(index);
			}
		}

		return [.. distinct];
	}

	/// <summary>Forgets every sample.</summary>
	public void Reset()
	{
		Array.Fill(_buckets, Extremes.Empty);
		_origin = double.NaN;
		_width = double.NaN;
		_lastX = double.NaN;
		_first = -1;
		_last = -1;
	}

	private int BucketOf(double x) =>
		double.IsNaN(_width) ? 0 : (int)Math.Min(int.MaxValue, Math.Floor((x - _origin) / _width));

	private void Halve()
	{
		int half = (_buckets.Length + 1) / 2;
		for (int i = 0; i < half; i++)
		{
			Extremes left = _buckets[2 * i];
			_buckets[i] = 2 * i + 1 < _buckets.Length ? left.Merge(_buckets[2 * i + 1]) : left;
		}

		for (int i = half; i < _buckets.Length; i++)
		{
			_buckets[i] = Extremes.Empty;
		}

		_width *= 2;
	}
}
