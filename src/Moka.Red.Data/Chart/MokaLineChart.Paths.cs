using System.Globalization;
using System.Text;

namespace Moka.Red.Data.Chart;

public partial class MokaLineChart
{
	private TrackPaths PathsFor(ModernModel model, Track track)
	{
		List<(int First, int Last)> runs = RunsOf(track);
		if (runs.Count == 0)
		{
			return TrackPaths.None;
		}

		StringBuilder line = new();
		foreach ((int first, int last) in runs)
		{
			AppendLine(line, track, track.Ys, first, last);
		}

		// A band is the fill of its own line; only an Area asked for outright goes under it as well. A comparison is a line
		// alone.
		string? area = null;
		if (model.Area != MokaChartArea.None && (!track.HasBand || Area is not null) && !track.IsCompare)
		{
			StringBuilder fill = new();
			foreach ((int first, int last) in runs)
			{
				if (track.BaseYs is { } bases)
				{
					AppendBand(fill, track, track.Ys, bases, first, last);
				}
				else
				{
					AppendLine(fill, track, track.Ys, first, last);
					AppendClose(fill, track.Xs[last], track.Xs[first], model.BaselineY);
				}
			}

			area = fill.ToString();
		}

		string? band = null;
		if (track.HighYs is { } highs && track.LowYs is { } lows)
		{
			StringBuilder shade = new();
			foreach ((int first, int last) in BandRuns(track, highs, lows))
			{
				AppendBand(shade, track, highs, lows, first, last);
			}

			band = shade.Length > 0 ? shade.ToString() : null;
		}

		int finite = 0;
		foreach (int y in track.Ys)
		{
			if (y != int.MinValue)
			{
				finite++;
			}
		}

		bool everyDot = model.Points == MokaChartPoints.Always && finite <= DotsAtMost && !track.IsCompare;
		string? dots = null;
		IReadOnlyList<NativePoint> titled = [];
		if (everyDot)
		{
			StringBuilder marks = new();
			for (int k = 0; k < track.Ys.Length; k++)
			{
				if (track.Ys[k] != int.MinValue && track.Xs[k] != int.MinValue)
				{
					marks.Append(CultureInfo.InvariantCulture, $"M{track.Xs[k]},{track.Ys[k]}h0");
				}
			}

			dots = marks.ToString();
			if (ResolvedTooltip == MokaChartTooltip.Native)
			{
				titled = [.. Enumerable.Range(0, track.Ys.Length)
					.Where(k => track.Ys[k] != int.MinValue && track.Xs[k] != int.MinValue)
					.Select(k => new NativePoint(Coord(track.Xs[k]), Coord(track.Ys[k]), NativeTitle(model, track, track.Drawn[k])))];
			}
		}

		bool endDot = track.Last >= 0 && !everyDot && !track.IsCompare && (model.Points != MokaChartPoints.None || ShowLastPoint);
		string? end = endDot ? $"M{Coord(track.Xs[track.Last])},{Coord(track.Ys[track.Last])}h0" : null;
		return new TrackPaths(line.ToString(), area, band, dots, titled, end, TrendPath(track));
	}

	// The trend through the drawn points, breaking where the moving average has none. A straight line needs only its ends.
	private string? TrendPath(Track track)
	{
		if (track.TrendYs is not { } ys)
		{
			return null;
		}

		StringBuilder path = new();
		bool straight = Trend == MokaChartTrend.Linear;
		int first = -1;
		int last = -1;
		bool open = false;
		for (int k = 0; k < ys.Length; k++)
		{
			if (ys[k] == int.MinValue || track.Xs[k] == int.MinValue)
			{
				open = false;
				continue;
			}

			if (straight)
			{
				first = first < 0 ? k : first;
				last = k;
				continue;
			}

			path.Append(CultureInfo.InvariantCulture, $"{(open ? 'L' : 'M')}{track.Xs[k]},{ys[k]}");
			open = true;
		}

		if (straight && first >= 0)
		{
			path.Append(CultureInfo.InvariantCulture, $"M{track.Xs[first]},{ys[first]}L{track.Xs[last]},{ys[last]}");
		}

		return path.Length > 0 ? path.ToString() : null;
	}

	private static string Coord(int value) => value.ToString(CultureInfo.InvariantCulture);

	// Stretches of drawn points the line runs through without lifting: it lifts at a gap and where a break falls
	// between two points.
	private static List<(int First, int Last)> RunsOf(Track track)
	{
		List<(int First, int Last)> runs = [];
		int start = -1;
		for (int k = 0; k < track.Ys.Length; k++)
		{
			bool point = track.Ys[k] != int.MinValue && track.Xs[k] != int.MinValue
			             && (track.BaseYs is null || track.BaseYs[k] != int.MinValue);
			bool broken = track.BreakBefore is { } breaks && breaks[k];
			if (start >= 0 && (!point || broken))
			{
				runs.Add((start, k - 1));
				start = -1;
			}

			if (point && start < 0)
			{
				start = k;
			}
		}

		if (start >= 0)
		{
			runs.Add((start, track.Ys.Length - 1));
		}

		return runs;
	}

	private static List<(int First, int Last)> BandRuns(Track track, int[] highs, int[] lows)
	{
		List<(int First, int Last)> runs = [];
		int start = -1;
		for (int k = 0; k < highs.Length; k++)
		{
			bool point = highs[k] != int.MinValue && lows[k] != int.MinValue && track.Xs[k] != int.MinValue;
			bool broken = track.BreakBefore is { } breaks && breaks[k];
			if (start >= 0 && (!point || broken))
			{
				runs.Add((start, k - 1));
				start = -1;
			}

			if (point && start < 0)
			{
				start = k;
			}
		}

		if (start >= 0)
		{
			runs.Add((start, highs.Length - 1));
		}

		return runs;
	}

	// One stretch of a line, starting with a move. A lone point gets a zero-length segment so its round cap draws.
	private static void AppendLine(StringBuilder path, Track track, int[] ys, int first, int last)
	{
		int[] xs = track.Xs;
		path.Append(CultureInfo.InvariantCulture, $"M{xs[first]},{ys[first]}");
		if (first == last)
		{
			path.Append("h0");
			return;
		}

		switch (track.Curve)
		{
			case MokaLineCurve.Step:
				for (int k = first + 1; k <= last; k++)
				{
					path.Append(CultureInfo.InvariantCulture, $"H{xs[k]}V{ys[k]}");
				}

				break;
			case MokaLineCurve.Smooth:
				foreach (Segment segment in Monotone(xs, ys, first, last))
				{
					segment.Append(path);
				}

				break;
			default:
				path.Append('L');
				for (int k = first + 1; k <= last; k++)
				{
					path.Append(CultureInfo.InvariantCulture, $"{xs[k]},{ys[k]}");
					if (k < last)
					{
						path.Append(' ');
					}
				}

				break;
		}
	}

	// A stretch between two edges: along the top, then back along the bottom, closed. Steps and curves come back the
	// way they went, so a stacked band's edges meet the bands around it exactly.
	private static void AppendBand(StringBuilder path, Track track, int[] tops, int[] bottoms, int first, int last)
	{
		int[] xs = track.Xs;
		AppendLine(path, track, tops, first, last);
		path.Append(CultureInfo.InvariantCulture, $"L{xs[last]},{bottoms[last]}");
		switch (track.Curve)
		{
			case MokaLineCurve.Step:
				for (int k = last - 1; k >= first; k--)
				{
					path.Append(CultureInfo.InvariantCulture, $"V{bottoms[k]}H{xs[k]}");
				}

				break;
			case MokaLineCurve.Smooth when last > first:
				List<Segment> segments = Monotone(xs, bottoms, first, last);
				for (int s = segments.Count - 1; s >= 0; s--)
				{
					segments[s].AppendReversed(path);
				}

				break;
			default:
				for (int k = last - 1; k >= first; k--)
				{
					path.Append(CultureInfo.InvariantCulture, $"L{xs[k]},{bottoms[k]}");
				}

				break;
		}

		path.Append('Z');
	}

	private static void AppendClose(StringBuilder path, int lastX, int firstX, int baseline) =>
		path.Append(CultureInfo.InvariantCulture, $"L{lastX},{baseline}L{firstX},{baseline}Z");

	// A smooth line through the points as cubic curves that never overshoot a peak (Fritsch and Carlson), the way the
	// classic chart's MokaChartPaths draws it, kept as segments so a band can run back along one.
	private static List<Segment> Monotone(int[] xs, int[] ys, int first, int last)
	{
		int count = last - first + 1;
		List<Segment> segments = new(Math.Max(0, count - 1));
		if (count < 2)
		{
			return segments;
		}

		double[] secants = new double[count - 1];
		double[] tangents = new double[count];
		for (int i = 0; i < count - 1; i++)
		{
			double dx = xs[first + i + 1] - xs[first + i];
			secants[i] = dx > 0 ? (ys[first + i + 1] - ys[first + i]) / dx : 0;
		}

		tangents[0] = secants[0];
		tangents[count - 1] = secants[count - 2];
		for (int i = 1; i < count - 1; i++)
		{
			tangents[i] = secants[i - 1] * secants[i] <= 0 ? 0 : (secants[i - 1] + secants[i]) / 2;
		}

		for (int i = 0; i < count - 1; i++)
		{
			if (secants[i] == 0)
			{
				tangents[i] = 0;
				tangents[i + 1] = 0;
				continue;
			}

			double a = tangents[i] / secants[i];
			double b = tangents[i + 1] / secants[i];
			double sum = a * a + b * b;
			if (sum > 9)
			{
				double tau = 3 / Math.Sqrt(sum);
				tangents[i] = tau * a * secants[i];
				tangents[i + 1] = tau * b * secants[i];
			}
		}

		for (int i = 0; i < count - 1; i++)
		{
			int x0 = xs[first + i];
			int y0 = ys[first + i];
			int x1 = xs[first + i + 1];
			int y1 = ys[first + i + 1];
			double third = (x1 - x0) / 3d;
			segments.Add(new Segment(x0, y0, Round(x0 + third), Round(y0 + tangents[i] * third), Round(x1 - third),
				Round(y1 - tangents[i + 1] * third), x1, y1));
		}

		return segments;
	}

	private static int Round(double value) => (int)Math.Round(value);

	/// <summary>The paths one series draws: its line, the area or band under it, its dots, its end dot and its trend.</summary>
	private sealed record TrackPaths(
		string Line,
		string? Area,
		string? Band,
		string? Dots,
		IReadOnlyList<NativePoint> Titled,
		string? End,
		string? Trend)
	{
		public static TrackPaths None { get; } = new(string.Empty, null, null, null, [], null, null);
	}

	/// <summary>A dot with the browser's own tooltip, for a chart whose tooltip is Native.</summary>
	private sealed record NativePoint(string X, string Y, string Title);

	/// <summary>One cubic curve between two points, with its control points.</summary>
	private readonly record struct Segment(int X0, int Y0, int C1X, int C1Y, int C2X, int C2Y, int X1, int Y1)
	{
		public void Append(StringBuilder path) =>
			path.Append(CultureInfo.InvariantCulture, $"C{C1X},{C1Y} {C2X},{C2Y} {X1},{Y1}");

		public void AppendReversed(StringBuilder path) =>
			path.Append(CultureInfo.InvariantCulture, $"C{C2X},{C2Y} {C1X},{C1Y} {X0},{Y0}");
	}
}
