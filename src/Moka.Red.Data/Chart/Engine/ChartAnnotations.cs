using System.Globalization;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Icons;

namespace Moka.Red.Data.Chart;

/// <summary>A threshold as drawn: where its line runs on the value axis, what it reads, and its tone.</summary>
/// <param name="Key">Its identity among the chart's annotations.</param>
/// <param name="Share">Where it runs, as a share of the value axis.</param>
/// <param name="Text">What it reads at its end.</param>
/// <param name="ToneClass">The class that colors it.</param>
internal sealed record PlotThreshold(string Key, double Share, string Text, string ToneClass);

/// <summary>A band as drawn: where it starts and ends on the value axis, and what it reads.</summary>
/// <param name="Key">Its identity among the chart's annotations.</param>
/// <param name="From">Its low end, as a share of the value axis.</param>
/// <param name="To">Its high end.</param>
/// <param name="Text">What it reads, or null.</param>
internal sealed record PlotBand(string Key, double From, double To, string? Text);

/// <summary>An event as drawn: its marker on the events row and the guide and span it shows through the plot.</summary>
/// <param name="Key">Its identity among the chart's annotations.</param>
/// <param name="Share">Where it happened, as a share of the x axis.</param>
/// <param name="Width">How much of the axis its span covers; 0 for a moment.</param>
/// <param name="Name">The marker's accessible name, such as "Deploy, 14:05".</param>
/// <param name="Label">What it is called.</param>
/// <param name="Detail">More about it.</param>
/// <param name="When">When it happened, written.</param>
/// <param name="Icon">The marker's shape.</param>
/// <param name="ToneClass">The class that colors it.</param>
internal sealed record PlotEvent(
	string Key,
	double Share,
	double Width,
	string Name,
	string? Label,
	string? Detail,
	string When,
	MokaIconDefinition Icon,
	string ToneClass);

/// <summary>A window as drawn: the span it hatches, and the label above the plot.</summary>
/// <param name="Key">Its identity among the chart's annotations.</param>
/// <param name="From">Where it starts, as a share of the x axis.</param>
/// <param name="To">Where it ends.</param>
/// <param name="Text">What it reads above the plot, or null.</param>
internal sealed record PlotWindow(string Key, double From, double To, string? Text);

/// <summary>Everything a plot draws over its data to explain it.</summary>
/// <param name="Thresholds">Dashed lines at values.</param>
/// <param name="Bands">Soft bands between values, the target band among them.</param>
/// <param name="Events">Markers under the plot.</param>
/// <param name="Windows">Hatched spans.</param>
internal sealed record PlotAnnotations(
	IReadOnlyList<PlotThreshold> Thresholds,
	IReadOnlyList<PlotBand> Bands,
	IReadOnlyList<PlotEvent> Events,
	IReadOnlyList<PlotWindow> Windows)
{
	/// <summary>Nothing drawn.</summary>
	public static PlotAnnotations Empty { get; } = new([], [], [], []);

	/// <summary>Whether a window labels itself above the plot, which then keeps a row for it.</summary>
	public bool LabelsAbove => Windows.Any(window => !string.IsNullOrEmpty(window.Text));
}

/// <summary>
///     Turns a chart's <see cref="MokaChartAnnotation" />s and target band into what the plot draws, and writes the notes
///     a tooltip adds for a point: how far over a threshold, whether inside the target, which event or window it is in.
/// </summary>
internal static class ChartAnnotations
{
	private static readonly MokaIconDefinition Diamond = Solid(MokaIcons.Content.Diamond);
	private static readonly MokaIconDefinition Triangle = Solid(MokaIcons.Content.Triangle);
	private static readonly MokaIconDefinition Circle = Solid(MokaIcons.Content.Circle);
	private static readonly MokaIconDefinition Square = Solid(MokaIcons.Toggle.Square);

	/// <summary>The marker drawn for a shape: the icon's own outline, filled.</summary>
	/// <param name="shape">The shape.</param>
	internal static MokaIconDefinition IconFor(MokaChartMarker shape) => shape switch
	{
		MokaChartMarker.Triangle => Triangle,
		MokaChartMarker.Circle => Circle,
		MokaChartMarker.Square => Square,
		_ => Diamond
	};

	/// <summary>The class that colors an annotation in a tone.</summary>
	/// <param name="tone">The tone.</param>
	internal static string ToneClass(MokaColor tone) => $"moka-chart-plot__tone--{MokaEnumHelpers.ToCssClass(tone)}";

	/// <summary>Builds what the plot draws.</summary>
	/// <param name="annotations">The chart's annotations.</param>
	/// <param name="targetLow">The bottom of the target band.</param>
	/// <param name="targetHigh">The top of the target band.</param>
	/// <param name="valueShare">Where a value sits along the value axis, 0 to 1.</param>
	/// <param name="valueText">A value written as the value axis writes it.</param>
	/// <param name="x">The x axis, for events and windows; null on a chart of rows, which draws neither.</param>
	/// <param name="words">The chart's words.</param>
	internal static PlotAnnotations Build(IReadOnlyList<MokaChartAnnotation>? annotations, double? targetLow, double? targetHigh,
		Func<double, double> valueShare, Func<double, string> valueText, ChartXFrame? x, MokaChartText words)
	{
		ArgumentNullException.ThrowIfNull(valueShare);
		ArgumentNullException.ThrowIfNull(valueText);
		ArgumentNullException.ThrowIfNull(words);

		List<PlotThreshold> thresholds = [];
		List<PlotBand> bands = [];
		List<PlotEvent> events = [];
		List<PlotWindow> windows = [];

		if (targetLow is not null || targetHigh is not null)
		{
			AddBand(bands, "target", targetLow, targetHigh, null, valueShare);
		}

		int position = 0;
		foreach (MokaChartAnnotation annotation in annotations ?? [])
		{
			string key = annotation.Key ?? $"a{(position++).ToString(CultureInfo.InvariantCulture)}";
			switch (annotation)
			{
				case MokaChartThreshold threshold when double.IsFinite(threshold.Value):
					double share = valueShare(threshold.Value);
					if (double.IsFinite(share) && share >= -0.0001 && share <= 1.0001)
					{
						thresholds.Add(new PlotThreshold(key, Math.Clamp(share, 0, 1),
							threshold.Label ?? valueText(threshold.Value), ToneClass(threshold.Tone)));
					}

					break;
				case MokaChartBand band:
					AddBand(bands, key, band.Low, band.High, band.Label, valueShare);
					break;
				case MokaChartEvent happened when x is not null:
					AddEvent(events, key, happened, x, words);
					break;
				case MokaChartWindow window when x is not null:
					AddWindow(windows, key, window, x);
					break;
			}
		}

		return thresholds.Count + bands.Count + events.Count + windows.Count == 0
			? PlotAnnotations.Empty
			: new PlotAnnotations(thresholds, bands, events, windows);
	}

	/// <summary>
	///     What a tooltip adds for a value at a place on the x axis, in the chart's words: how far over each threshold it
	///     is, whether it is inside each band, and which events and windows it falls in. A value that isn't a number, such
	///     as a gap or a column of shares, still gets the events and windows at its place.
	/// </summary>
	/// <param name="annotations">The chart's annotations.</param>
	/// <param name="targetLow">The bottom of the target band.</param>
	/// <param name="targetHigh">The top of the target band.</param>
	/// <param name="value">The value, or NaN for none.</param>
	/// <param name="x">Where it is along the x axis, as the annotations count it, or null.</param>
	/// <param name="xTolerance">How near an event a point has to be to name it, along the x axis.</param>
	/// <param name="valueText">A value written as the value axis writes it.</param>
	/// <param name="words">The chart's words.</param>
	internal static List<string> Notes(IReadOnlyList<MokaChartAnnotation>? annotations, double? targetLow, double? targetHigh,
		double value, double? x, double xTolerance, Func<double, string> valueText, MokaChartText words)
	{
		ArgumentNullException.ThrowIfNull(valueText);
		ArgumentNullException.ThrowIfNull(words);

		List<string> notes = [];
		bool valued = double.IsFinite(value);
		if (!valued && x is null)
		{
			return notes;
		}

		if (valued && (targetLow is not null || targetHigh is not null))
		{
			notes.Add(BandNote(targetLow, targetHigh, value, valueText, words));
		}

		foreach (MokaChartAnnotation annotation in annotations ?? [])
		{
			switch (annotation)
			{
				case MokaChartThreshold threshold when valued && double.IsFinite(threshold.Value) && value > threshold.Value:
					notes.Add(words.OverThreshold(valueText(value - threshold.Value), threshold.Label ?? valueText(threshold.Value)));
					break;
				case MokaChartBand band when valued:
					notes.Add(BandNote(band.Low, band.High, value, valueText, words));
					break;
				case MokaChartEvent happened when x is { } at && !string.IsNullOrEmpty(happened.Label) && Covers(happened, at, xTolerance):
					notes.Add(happened.Label);
					break;
				case MokaChartWindow window when x is { } at && !string.IsNullOrEmpty(window.Label) && Inside(window.From.Value, window.To.Value, at):
					notes.Add(window.Label);
					break;
			}
		}

		return notes;
	}

	/// <summary>Whether a window that breaks the line covers a place on the x axis, so a value there counts as a gap.</summary>
	/// <param name="annotations">The chart's annotations.</param>
	/// <param name="x">The place, as the annotations count it.</param>
	internal static bool Breaks(IReadOnlyList<MokaChartAnnotation>? annotations, double x)
	{
		if (annotations is null || !double.IsFinite(x))
		{
			return false;
		}

		foreach (MokaChartAnnotation annotation in annotations)
		{
			if (annotation is MokaChartWindow { BreakLine: true } window && Inside(window.From.Value, window.To.Value, x))
			{
				return true;
			}
		}

		return false;
	}

	private static MokaIconDefinition Solid(MokaIconDefinition icon) => new(icon.Name, icon.SvgPath, icon.ViewBox, true);

	private static bool Inside(double from, double to, double x) =>
		x >= Math.Min(from, to) && x <= Math.Max(from, to);

	private static bool Covers(MokaChartEvent happened, double x, double tolerance) =>
		happened.To is { } to
			? Inside(happened.At.Value, to.Value, x)
			: Math.Abs(x - happened.At.Value) <= Math.Max(0, tolerance);

	private static string BandNote(double? low, double? high, double value, Func<double, string> valueText, MokaChartText words)
	{
		if (high is { } top && value > top)
		{
			return words.AboveTarget(valueText(value - top));
		}

		return low is { } bottom && value < bottom ? words.BelowTarget(valueText(bottom - value)) : words.InsideTarget;
	}

	private static void AddBand(List<PlotBand> bands, string key, double? low, double? high, string? text,
		Func<double, double> valueShare)
	{
		double from = low is { } l && double.IsFinite(l) ? valueShare(l) : 0;
		double to = high is { } h && double.IsFinite(h) ? valueShare(h) : 1;
		if (!double.IsFinite(from) || !double.IsFinite(to))
		{
			return;
		}

		(from, to) = (Math.Clamp(Math.Min(from, to), 0, 1), Math.Clamp(Math.Max(from, to), 0, 1));
		if (to > from)
		{
			bands.Add(new PlotBand(key, from, to, text));
		}
	}

	private static void AddEvent(List<PlotEvent> events, string key, MokaChartEvent happened, ChartXFrame x, MokaChartText words)
	{
		double share = x.Share(happened.At.Value);
		if (!double.IsFinite(share) || share < -0.0001 || share > 1.0001)
		{
			return;
		}

		double width = 0;
		string when = x.Text(happened.At.Value);
		if (happened.To is { } to && double.IsFinite(x.Share(to.Value)))
		{
			width = Math.Clamp(x.Share(to.Value), 0, 1) - Math.Clamp(share, 0, 1);
			when = words.Between(when, x.Text(to.Value));
		}

		string name = string.IsNullOrEmpty(happened.Label) ? when : $"{happened.Label}, {when}";
		events.Add(new PlotEvent(key, Math.Clamp(share, 0, 1), Math.Max(0, width), name, happened.Label, happened.Detail, when,
			IconFor(happened.Shape), ToneClass(happened.Tone)));
	}

	private static void AddWindow(List<PlotWindow> windows, string key, MokaChartWindow window, ChartXFrame x)
	{
		double from = x.Share(Math.Min(window.From.Value, window.To.Value));
		double to = x.Share(Math.Max(window.From.Value, window.To.Value));
		if (!double.IsFinite(from) || !double.IsFinite(to) || to <= 0 || from >= 1)
		{
			return;
		}

		windows.Add(new PlotWindow(key, Math.Max(0, from), Math.Min(1, to), window.Label));
	}
}
