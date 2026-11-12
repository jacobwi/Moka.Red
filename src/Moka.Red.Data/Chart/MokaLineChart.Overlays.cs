using System.Globalization;
using Moka.Red.Core.Charts;

namespace Moka.Red.Data.Chart;

public partial class MokaLineChart
{
	private int ShownTrendWindow => Math.Max(1, TrendWindow);

	// The trend at each drawn point. A moving average is worked out once over all of the values; a straight line is
	// fitted once and read off at each point's x.
	private void MeasureTrend(ModernModel model, Track track)
	{
		track.TrendMeans = null;
		track.TrendFit = default;
		track.TrendTops = null;
		if (Trend == MokaChartTrend.None || Stacked || track.Hidden || track.IsCompare || track.Count == 0 || track.Drawn.Length == 0)
		{
			return;
		}

		double[]? copy = null;
		ReadOnlySpan<double> values = track.Factor == 1 && model.Breaks is null
			? SpanOf(track.Series.Values, ref copy)[..track.Count]
			: AsDrawn(model, track);
		if (Trend == MokaChartTrend.MovingAverage)
		{
			track.TrendMeans = MokaChartStats.MovingAverage(values, ShownTrendWindow);
		}
		else
		{
			track.TrendFit = model.X is null
				? MokaChartStats.LinearFit(values)
				: MokaChartStats.LinearFit(model.X.AsSpan(0, track.Count), values);
		}

		double[] tops = new double[track.Drawn.Length];
		for (int k = 0; k < tops.Length; k++)
		{
			tops[k] = TrendAt(model, track, track.Drawn[k]);
		}

		track.TrendTops = tops;
	}

	private static double[] AsDrawn(ModernModel model, Track track)
	{
		double[] values = new double[track.Count];
		for (int i = 0; i < values.Length; i++)
		{
			values[i] = Drawn(track, model, i);
		}

		return values;
	}

	// The trend's value at a value's place, NaN where it has none.
	private static double TrendAt(ModernModel model, Track track, int index)
	{
		if (track.TrendMeans is { } means)
		{
			return index >= 0 && index < means.Length ? means[index] : double.NaN;
		}

		return track.TrendFit.HasLine && index >= 0 && index < track.Count
			? track.TrendFit.At(model.X is null ? index : model.X[index])
			: double.NaN;
	}

	private string TrendName(Track track) => Trend == MokaChartTrend.MovingAverage
		? Words.AverageOf(track.Series.Name, ShownTrendWindow)
		: Words.TrendOf(track.Series.Name);

	// The comparisons a series shows against: those drawn with it.
	private static IEnumerable<Track> ComparisonsOf(ModernModel model, Track track) =>
		model.Tracks.Where(other => !other.Hidden && ReferenceEquals(other.Base, track));

	// How far a series' value is from each of its comparisons there, in the chart's words: "+12% vs Last week".
	private string? VersusText(ModernModel model, Track track, int index, double shown)
	{
		if (!double.IsFinite(shown))
		{
			return null;
		}

		List<string> parts = [];
		foreach (Track other in ComparisonsOf(model, track))
		{
			double then = Drawn(other, model, index);
			if (double.IsFinite(then))
			{
				parts.Add(Words.Versus(Growth(model, shown, then), other.Series.Name));
			}
		}

		return parts.Count == 0 ? null : string.Join("; ", parts);
	}

	// The change as a share of what it is compared with, signed; from zero, the change itself.
	private string Growth(ModernModel model, double now, double then)
	{
		if (then == 0)
		{
			return Signed(model, now - then);
		}

		double percent = (now - then) / Math.Abs(then) * 100;
		string number = Math.Abs(percent).ToString("0.#", CultureInfo.CurrentCulture);
		return percent >= 0.05 ? $"+{number}%" : percent <= -0.05 ? $"-{number}%" : $"{number}%";
	}

	// The first comparison's value at a place, for the point a tooltip template gets.
	private static double? ComparisonAt(ModernModel model, Track track, int index)
	{
		foreach (Track other in ComparisonsOf(model, track))
		{
			if (Drawn(other, model, index) is var value && double.IsFinite(value))
			{
				return value;
			}
		}

		return null;
	}
}
