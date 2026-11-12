using System.Runtime.InteropServices;
using Moka.Red.Core.Charts;

namespace Moka.Red.Data.Chart;

public partial class MokaLineChart
{
	// How many places a slide is looked for at, and how many of them are compared in full before giving up.
	private const int SlideSearch = 4096;
	private const int SlideTries = 4;

	private LiveFeed? _feed;

	/// <summary>Whether the last build of a live chart kept what it had drawn from and read only what arrived.</summary>
	internal bool LiveKept => _feed?.Kept ?? false;

	/// <summary>
	///     What a live chart last drew from: a copy of each series and its times, and an index over each series' values. A
	///     window that grew at its end, or slid along (dropped from its start, added at its end), is found by comparing the
	///     new data with the copy, and only what arrived is read; anything else, or a series that changed shape, starts
	///     over. The comparison is exact, bit for bit, so the picks, and the markup, are those a fresh build would make.
	/// </summary>
	private sealed class LiveFeed
	{
		private LiveSeries[] _series = [];
		private bool _hasX;

		// The times as UTC ticks, which a time's x is worked out from, so comparing them tells whether the x moved.
		private long[] _ticks = [];

		// The times as Unix milliseconds, in a buffer that grows by doubling, so it holds more than XCount once it has grown.
		public double[] X { get; private set; } = [];

		public int XCount { get; private set; }

		// Every time finite and none earlier than the one before, which the index needs to pick by time.
		public bool XInOrder { get; private set; }

		public (double Min, double Max)? XBounds { get; private set; }

		public bool Kept { get; private set; }

		public (int Dropped, int Added) Change { get; private set; }

		public MokaMinMaxIndex? IndexFor(int position) => SeriesFor(position)?.Index;

		public LiveSeries? SeriesFor(int position) => position >= 0 && position < _series.Length ? _series[position] : null;

		public void Update(MokaLineSeries[] series, IReadOnlyList<DateTimeOffset>? times)
		{
			double[]?[] copies = new double[]?[series.Length];
			DateTimeOffset[]? timesCopy = null;
			ReadOnlySpan<DateTimeOffset> newTimes = times is null ? default : TimesSpan(times, ref timesCopy);
			int slide = SameShape(series, times is not null) ? FindSlide(series, copies, newTimes) : -1;
			Kept = slide >= 0;
			Change = (0, 0);
			if (Kept)
			{
				int lead = Array.FindIndex(_series, one => one.Length > 0);
				int before = lead >= 0 ? _series[lead].Length : 0;
				int after = lead >= 0 ? series[lead].Values.Count : 0;
				Change = (slide, after - Math.Max(0, before - slide));
				Slide(series, copies, newTimes, slide);
			}
			else
			{
				Restart(series, copies, times is not null, newTimes);
			}
		}

		private bool SameShape(MokaLineSeries[] series, bool hasX)
		{
			if (series.Length != _series.Length || hasX != _hasX)
			{
				return false;
			}

			for (int i = 0; i < series.Length; i++)
			{
				if (!string.Equals(series[i].Key ?? series[i].Name, _series[i].Key, StringComparison.Ordinal))
				{
					return false;
				}
			}

			return true;
		}

		// How many values every series and the times dropped from their start, with what was kept unchanged bit for bit;
		// -1 when the data didn't just grow or slide. A series with times finds the place by its first time, else by its
		// first value, and every candidate is compared in full.
		private int FindSlide(MokaLineSeries[] series, double[]?[] copies, ReadOnlySpan<DateTimeOffset> times)
		{
			int lead = -1;
			for (int i = 0; i < series.Length && lead < 0; i++)
			{
				if (_series[i].Length > 0)
				{
					lead = i;
				}
			}

			if (lead < 0)
			{
				return -1;
			}

			if (Fits(series, copies, times, 0))
			{
				return 0;
			}

			ReadOnlySpan<double> before = _series[lead].Values;
			ReadOnlySpan<double> now = Span(series[lead].Values, ref copies[lead]);
			if (now.IsEmpty)
			{
				return -1;
			}

			int tries = 0;
			if (_hasX && XCount > 0 && !times.IsEmpty)
			{
				long first = times[0].UtcTicks;
				for (int k = 1; k < Math.Min(XCount, SlideSearch) && tries < SlideTries; k++)
				{
					if (_ticks[k] == first)
					{
						tries++;
						if (Fits(series, copies, times, k))
						{
							return k;
						}
					}
				}

				return -1;
			}

			for (int k = 1; k < Math.Min(before.Length, SlideSearch) && tries < SlideTries; k++)
			{
				if (Same(before[k], now[0]))
				{
					tries++;
					if (Fits(series, copies, times, k))
					{
						return k;
					}
				}
			}

			return -1;
		}

		// Whether every series and the times are what they were with the first `dropped` values gone, and no shorter.
		private bool Fits(MokaLineSeries[] series, double[]?[] copies, ReadOnlySpan<DateTimeOffset> times, int dropped)
		{
			for (int i = 0; i < series.Length; i++)
			{
				ReadOnlySpan<double> before = _series[i].Values;
				ReadOnlySpan<double> now = Span(series[i].Values, ref copies[i]);
				int kept = before.Length - dropped;
				if (kept > 0 && (now.Length < kept || !Same(now[..kept], before[dropped..])))
				{
					return false;
				}
			}

			if (!_hasX)
			{
				return true;
			}

			int keptTimes = XCount - dropped;
			if (keptTimes <= 0)
			{
				return true;
			}

			if (times.Length < keptTimes)
			{
				return false;
			}

			for (int i = 0; i < keptTimes; i++)
			{
				if (times[i].UtcTicks != _ticks[i + dropped])
				{
					return false;
				}
			}

			return true;
		}

		private void Slide(MokaLineSeries[] series, double[]?[] copies, ReadOnlySpan<DateTimeOffset> times, int dropped)
		{
			for (int i = 0; i < series.Length; i++)
			{
				ReadOnlySpan<double> now = Span(series[i].Values, ref copies[i]);
				LiveSeries one = _series[i];
				int kept = Math.Max(0, one.Length - dropped);
				one.Index.RemoveFirst(dropped);
				one.Index.Append(now[kept..]);
				one.Take(now, kept, dropped);
			}

			if (!_hasX)
			{
				return;
			}

			int keptTimes = Math.Max(0, XCount - dropped);
			double[] x = Moved(X, keptTimes, dropped, times.Length);
			_ticks = Moved(_ticks, keptTimes, dropped, times.Length);
			bool inOrder = XInOrder;
			for (int i = keptTimes; i < times.Length; i++)
			{
				x[i] = Ms(times[i]);
				_ticks[i] = times[i].UtcTicks;
				inOrder = inOrder && double.IsFinite(x[i]) && (i == 0 || x[i] >= x[i - 1]);
			}

			X = x;
			XCount = times.Length;
			XInOrder = inOrder;
			XBounds = Bounds(x.AsSpan(0, XCount), inOrder);
		}

		private void Restart(MokaLineSeries[] series, double[]?[] copies, bool hasX, ReadOnlySpan<DateTimeOffset> times)
		{
			LiveSeries[] fresh = new LiveSeries[series.Length];
			for (int i = 0; i < series.Length; i++)
			{
				ReadOnlySpan<double> now = Span(series[i].Values, ref copies[i]);
				LiveSeries one = i < _series.Length ? _series[i] : new LiveSeries();
				one.Key = series[i].Key ?? series[i].Name;
				one.Index.Reset(now);
				one.Take(now, 0, 0);
				fresh[i] = one;
			}

			_series = fresh;
			_hasX = hasX;
			if (!hasX)
			{
				XCount = 0;
				XInOrder = false;
				XBounds = null;
				return;
			}

			double[] x = Moved(X, 0, 0, times.Length);
			_ticks = Moved(_ticks, 0, 0, times.Length);
			bool inOrder = true;
			for (int i = 0; i < times.Length; i++)
			{
				x[i] = Ms(times[i]);
				_ticks[i] = times[i].UtcTicks;
				inOrder = inOrder && double.IsFinite(x[i]) && (i == 0 || x[i] >= x[i - 1]);
			}

			X = x;
			XCount = times.Length;
			XInOrder = inOrder;
			XBounds = Bounds(x.AsSpan(0, XCount), inOrder);
		}

		private static (double Min, double Max)? Bounds(ReadOnlySpan<double> x, bool inOrder)
		{
			if (x.IsEmpty)
			{
				return null;
			}

			if (inOrder)
			{
				return (x[0], x[^1]);
			}

			double min = double.PositiveInfinity;
			double max = double.NegativeInfinity;
			foreach (double value in x)
			{
				if (double.IsFinite(value))
				{
					min = Math.Min(min, value);
					max = Math.Max(max, value);
				}
			}

			return min <= max ? (min, max) : null;
		}

		// A buffer at least `length` long holding what it kept, moved down by `dropped`. It grows by doubling, so a window
		// that keeps growing doesn't copy every time.
		private static T[] Moved<T>(T[] buffer, int kept, int dropped, int length)
		{
			T[] room = buffer.Length >= length ? buffer : new T[Math.Max(length, buffer.Length * 2)];
			if (kept > 0 && (dropped > 0 || !ReferenceEquals(room, buffer)))
			{
				Array.Copy(buffer, dropped, room, 0, kept);
			}

			return room;
		}

		private static double Ms(DateTimeOffset time) => MokaChartX.FromDateTimeOffset(time).Value;

		// Bit for bit, so a NaN gap matches itself and nothing that merely compares equal passes for unchanged.
		private static bool Same(double a, double b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);

		private static bool Same(ReadOnlySpan<double> a, ReadOnlySpan<double> b) =>
			MemoryMarshal.Cast<double, long>(a).SequenceEqual(MemoryMarshal.Cast<double, long>(b));

		private static ReadOnlySpan<double> Span(IReadOnlyList<double> values, ref double[]? copy) => values switch
		{
			double[] array => array,
			List<double> list => CollectionsMarshal.AsSpan(list),
			_ => copy ??= [.. values]
		};

		private static ReadOnlySpan<DateTimeOffset> TimesSpan(IReadOnlyList<DateTimeOffset> times, ref DateTimeOffset[]? copy) => times switch
		{
			DateTimeOffset[] array => array,
			List<DateTimeOffset> list => CollectionsMarshal.AsSpan(list),
			_ => copy ??= [.. times]
		};
	}

	/// <summary>One series as a live chart last drew it: a copy of its values, the index over them, and where its gaps are.</summary>
	private sealed class LiveSeries
	{
		private readonly List<long> _gaps = [];
		private double[] _buffer = [];
		private long _origin;
		private int _firstGap;

		public string Key { get; set; } = string.Empty;

		public int Length { get; private set; }

		public MokaMinMaxIndex Index { get; } = new();

		public ReadOnlySpan<double> Values => _buffer.AsSpan(0, Length);

		// Whether a value strictly between these two places is a gap.
		public bool HasGapBetween(int from, int to)
		{
			long after = _origin + from;
			int low = _firstGap;
			int high = _gaps.Count;
			while (low < high)
			{
				int middle = low + (high - low) / 2;
				if (_gaps[middle] <= after)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}

			return low < _gaps.Count && _gaps[low] < _origin + to;
		}

		// The copy becomes `now`: what it kept moves down by `dropped`, and only what arrived is copied in. Gaps are kept
		// by their place since the series started, so a slide only forgets the ones it dropped.
		public void Take(ReadOnlySpan<double> now, int kept, int dropped)
		{
			if (kept == 0 && dropped == 0)
			{
				_origin = 0;
				_gaps.Clear();
				_firstGap = 0;
			}
			else
			{
				_origin += Math.Min(dropped, Length);
				while (_firstGap < _gaps.Count && _gaps[_firstGap] < _origin)
				{
					_firstGap++;
				}

				if (_firstGap > 64 && _firstGap * 2 > _gaps.Count)
				{
					_gaps.RemoveRange(0, _firstGap);
					_firstGap = 0;
				}
			}

			for (int i = kept; i < now.Length; i++)
			{
				if (!double.IsFinite(now[i]))
				{
					_gaps.Add(_origin + i);
				}
			}

			double[] buffer = _buffer.Length >= now.Length ? _buffer : new double[Math.Max(now.Length, _buffer.Length * 2)];
			if (kept > 0 && (dropped > 0 || !ReferenceEquals(buffer, _buffer)))
			{
				Array.Copy(_buffer, dropped, buffer, 0, kept);
			}

			now[kept..].CopyTo(buffer.AsSpan(kept));
			_buffer = buffer;
			Length = now.Length;
		}
	}
}
