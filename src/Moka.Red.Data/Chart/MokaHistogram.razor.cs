using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A histogram: how many of a set of raw values fell in each bucket, such as requests per 25 ms of response time,
///     with lines at chosen percentiles. It answers whether something is usually fast with a long tail, or slow for
///     everyone.
/// </summary>
/// <remarks>
///     The values are counted in one pass, so a million values cost about as much as copying them; pass
///     <see cref="MokaChartBase.DataVersion" /> so they aren't copied to be compared. Percentiles sort the values once.
///     <see cref="MokaCartesianChartBase.Unit" /> and <see cref="MokaChartBase.Formatter" /> write the values, along the
///     x axis and in the tooltip; the counts up the y axis take <see cref="MokaCartesianChartBase.YAxis" />.
/// </remarks>
public partial class MokaHistogram : MokaCartesianChartBase, IChartColumns
{
	private const int MeasureAbove = 100;
	private const double PixelsPerBin = 3;
	private const int SampleSize = 20_000;
	private const double LineHeadroom = 0.18;

	private double[]? _snapshot;
	private List<MokaHistogramBin> _bins = [];
	private int _total;
	private double _wanted;
	private List<PercentileLine> _lines = [];
	private (double Min, double Max) _valueRange = (double.NaN, double.NaN);
	private MokaScale? _scale;
	private ChartColumnsModel? _model;
	private string _colorClass = "moka-chart-c1";
	private string? _colorStyle;

	/// <summary>The raw values. A value that isn't a finite number isn't counted.</summary>
	[Parameter]
	public IReadOnlyList<double>? Values { get; set; }

	/// <summary>
	///     How wide each bucket is, in the values' own units. Null works one out from the spread of the values
	///     (Freedman and Diaconis), rounded to a step that reads well in <see cref="MokaCartesianChartBase.Unit" />.
	/// </summary>
	[Parameter]
	public double? BucketSize { get; set; }

	/// <summary>A value buckets start at, such as 0. Null starts them at multiples of the bucket size.</summary>
	[Parameter]
	public double? Origin { get; set; }

	/// <summary>Percentiles to draw a line at, from 0 to 100, such as <c>[50, 95]</c>.</summary>
	[Parameter]
	public IReadOnlyList<double>? Percentiles { get; set; }

	/// <summary>What the values are, for the tooltip: "1,204 requests". Defaults to "values".</summary>
	[Parameter]
	public string? CountLabel { get; set; }

	/// <summary>Writes a percentile line's label, given the percentile and its value. Defaults to "p95 240 ms".</summary>
	[Parameter]
	public Func<double, string, string>? PercentileText { get; set; }

	/// <summary>
	///     The most buckets drawn. More widen each bucket by a whole factor until they fit, and so does a plot with less
	///     than three pixels a bucket. Defaults to 200.
	/// </summary>
	[Parameter]
	public int MaxBins { get; set; } = 200;

	/// <inheritdoc />
	protected override string RootClass => "moka-histogram";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-histogram--{SizeToKebab(Size)}")
		.AddClass(ModernRootClasses)
		.AddClass(Class)
		.Build();

	/// <summary>A line at a percentile: where it runs, which side its label reads on, and what it says.</summary>
	private sealed record PercentileLine(string Class, string? Style, string Text);

	/// <inheritdoc />
	internal override string DefaultLabel => "Histogram";

	/// <inheritdoc />
	internal override bool HasData => _bins.Count > 0;

	/// <inheritdoc />
	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => [];

	/// <inheritdoc />
	internal override MokaChartTooltip AutoTooltip => MokaChartTooltip.Column;

	/// <inheritdoc />
	internal override bool MeasuresWidth => _wanted > MeasureAbove;

	/// <inheritdoc />
	internal override MokaChartRange? CurrentRange => null;

	/// <inheritdoc />
	/// <remarks>The values' unit is the x axis's; the counts up the y axis are plain numbers.</remarks>
	internal override MokaChartUnit? ValueAxisUnit => null;

	/// <inheritdoc />
	internal override Func<double, string>? ValueAxisFormatter => null;

	/// <inheritdoc />
	/// <remarks>Counts read best on round steps, whatever the look.</remarks>
	internal override bool NiceValueAxis => true;

	/// <inheritdoc />
	/// <remarks>The x axis is labelled at the buckets' edges, one more than there are buckets.</remarks>
	internal override int CategoryCount => _bins.Count == 0 ? 0 : _bins.Count + 1;

	/// <inheritdoc />
	internal override ChartTableModel? TableModel => new(
		[Words.From, Words.To, CountHeading],
		_bins.Count,
		(row, column) => column switch
		{
			0 => EdgeText(_bins[row].From),
			1 => EdgeText(_bins[row].To),
			_ => _bins[row].Count.ToString("N0", CultureInfo.CurrentCulture)
		},
		(row, column) => column switch
		{
			0 => _bins[row].From.ToString(CultureInfo.InvariantCulture),
			1 => _bins[row].To.ToString(CultureInfo.InvariantCulture),
			_ => _bins[row].Count.ToString(CultureInfo.InvariantCulture)
		});

	private string Counted => CountLabel ?? "values";

	private string CountHeading => string.IsNullOrEmpty(CountLabel)
		? "Count"
		: char.ToUpper(CountLabel[0], CultureInfo.CurrentCulture) + CountLabel[1..];

	/// <inheritdoc />
	internal override (double Min, double Max) ValueRange(int panel) => _valueRange;

	/// <inheritdoc />
	internal override string CategoryText(int index) =>
		_bins.Count == 0 ? string.Empty : EdgeText(index < _bins.Count ? _bins[Math.Max(0, index)].From : _bins[^1].To);

	/// <inheritdoc />
	internal override int DrawnCount(int panel) => _bins.Count;

	/// <inheritdoc />
	ChartColumnsModel? IChartColumns.Columns(int panel) => _model;

	/// <inheritdoc />
	internal override bool SameValue(string name, object? before, object? after) => name == nameof(Values)
		? ChartSnapshot.SameItems(after as IReadOnlyList<double>, _snapshot)
		: base.SameValue(name, before, after);

	/// <inheritdoc />
	internal override bool ComparesItems(string name) => name == nameof(Percentiles) || base.ComparesItems(name);

	/// <inheritdoc />
	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn)
	{
		if (drawn < 0 || drawn >= _bins.Count)
		{
			return null;
		}

		MokaHistogramBin bin = _bins[drawn];
		string heading = Words.Between(EdgeText(bin.From), EdgeText(bin.To));
		string count = bin.Count.ToString("N0", CultureInfo.CurrentCulture);
		double share = _total > 0 ? (double)bin.Count / _total : 0;
		int? pin = pinnedDrawn is { } p && p >= 0 && p < _bins.Count ? p : null;
		string delta = pin is { } at
			? (bin.Count - _bins[at].Count).ToString("+#,##0;-#,##0;0", CultureInfo.CurrentCulture)
			: ChartWholes.Percent(share);
		List<string> notes = [.. AnnotationNotes(bin.Count, drawn + 0.5, 0.5)];
		MokaChartPoint point = new("values", Counted, 0, drawn, bin.Count, count)
		{
			Category = heading,
			X = bin.From,
			Low = bin.From,
			High = bin.To,
			Total = _total,
			Share = share,
			SincePin = pin is { } since ? bin.Count - _bins[since].Count : null
		};
		string cell = $"{heading}, {count} {Counted}, {ChartWholes.Percent(share)}";
		return new ChartActiveInfo(heading, [new ChartActiveRow(Counted, _colorClass, _colorStyle, count, delta, false)], notes,
			notes.Count > 0 ? $"{cell}, {string.Join(", ", notes)}" : cell,
			new MokaChartTooltipContext(drawn, heading, [point], point, pin, notes));
	}

	/// <inheritdoc />
	internal override void Rebuild()
	{
		_snapshot = DataVersion is null ? Values?.ToArray() : null;
		_colorClass = SeriesClass(1, Color, false);
		_colorStyle = SeriesStyle(1, null);

		double[] values = Finite(Values);
		_total = values.Length;
		_wanted = 0;
		_bins = [];
		_lines = [];
		_model = null;
		_valueRange = (double.NaN, double.NaN);
		_scale = ValueScale(YAxis, double.NaN, double.NaN);
		if (values.Length == 0)
		{
			return;
		}

		double[]? sorted = Percentiles is { Count: > 0 } ? Sorted(values) : null;
		double size = BucketSize is { } chosen && double.IsFinite(chosen) && chosen > 0 ? chosen : AutoBucket(values, sorted);
		int most = Math.Max(1, MaxBins);
		if (PlotWidthBucket is { } width && width > 0)
		{
			most = Math.Clamp((int)(width / PixelsPerBin), 1, most);
		}

		_wanted = Math.Min(Math.Max(1, MaxBins), Math.Floor((values.Max() - values.Min()) / size) + 2);
		_bins = [.. MokaChartStats.Histogram(values, size, Origin, most)];
		int highest = _bins.Max(bin => bin.Count);
		_lines = sorted is null ? [] : Lines(sorted);
		_valueRange = (0, highest * (_lines.Count > 0 ? 1 + LineHeadroom : 1));
		_scale = ValueScale(YAxis, _valueRange.Min, _valueRange.Max);
		bool rounded = (Spec.BarEnds ?? MokaBarEnds.Rounded) == MokaBarEnds.Rounded;
		_model = new ChartColumnsModel("moka-chart-cols moka-chart-cols--tile", null, _bins.Count, index => Column(index, rounded));
	}

	private ChartColumn Column(int index, bool rounded)
	{
		MokaHistogramBin bin = _bins[index];
		bool reverse = YAxis?.Reverse ?? false;
		double baseline = Flip(Share(0), reverse);
		double top = Flip(Share(bin.Count), reverse);
		ChartBoxEnds ends = !rounded ? ChartBoxEnds.None : top >= baseline ? ChartBoxEnds.RoundHigh : ChartBoxEnds.RoundLow;
		List<ChartBox> marks = bin.Count > 0
			? [ChartColumns.Mark("bar", Math.Min(baseline, top), Math.Max(baseline, top), _colorClass, _colorStyle, "values", ends, "moka-chart-hatch")]
			: [];
		string? title = !Interactive
			? $"{Words.Between(EdgeText(bin.From), EdgeText(bin.To))}: {bin.Count.ToString("N0", CultureInfo.CurrentCulture)} {Counted}"
			: null;
		return new ChartColumn(index, "moka-chart-col moka-chart-col--tile", ChartColumns.SlotStyle(index, _bins.Count), title,
			[new ChartLane(marks, [])]);
	}

	private List<PercentileLine> Lines(double[] sorted)
	{
		List<PercentileLine> lines = [];
		double from = _bins[0].From;
		double to = _bins[^1].To;
		int row = 0;
		foreach (double percentile in Percentiles!.Where(p => double.IsFinite(p) && p >= 0 && p <= 100).Distinct().Order())
		{
			double value = MokaChartStats.Quantile(sorted, percentile / 100);
			double share = to > from ? Math.Clamp((value - from) / (to - from), 0, 1) : 0.5;
			string valueText = EdgeText(value);
			string text = PercentileText?.Invoke(percentile, valueText)
			              ?? $"p{percentile.ToString("0.#", CultureInfo.CurrentCulture)} {valueText}";
			string side = share > 0.6 ? "moka-histogram__line--left" : "moka-histogram__line--right";
			lines.Add(new PercentileLine($"moka-histogram__line {side}", new StyleBuilder()
				.AddStyle("--_x", ChartColumns.Css(share))
				.AddStyle("--_row", row.ToString(CultureInfo.InvariantCulture))
				.Build(), text));
			row++;
		}

		return lines;
	}

	private double Share(double value) => _scale?.Share(value) is { } share && double.IsFinite(share) ? Math.Clamp(share, 0, 1) : 0;

	private static double Flip(double share, bool reverse) => reverse ? 1 - share : share;

	// Freedman and Diaconis: twice the spread of the middle half over the cube root of the count, read from a sample
	// when the values aren't sorted, so a million values stay one pass.
	private double AutoBucket(double[] values, double[]? sorted)
	{
		double[] sample = sorted ?? Sample(values);
		double q1 = MokaChartStats.Quantile(sample, 0.25);
		double q3 = MokaChartStats.Quantile(sample, 0.75);
		double raw = 2 * (q3 - q1) / Math.Cbrt(values.Length);
		if (!double.IsFinite(raw) || raw <= 0)
		{
			double spread = sample[^1] - sample[0];
			raw = spread > 0 ? spread / Math.Ceiling(Math.Sqrt(values.Length)) : 1;
		}

		MokaChartUnit? unit = XAxis?.Unit ?? Unit;
		if (unit is not null)
		{
			MokaUnitStep step = unit.StepFor(raw);
			if (step.Factor > 0)
			{
				return MokaNiceScale.NiceStep(raw / step.Factor, step.Multiples) * step.Factor;
			}
		}

		return MokaNiceScale.NiceStep(raw);
	}

	private static double[] Sample(double[] values)
	{
		if (values.Length <= SampleSize)
		{
			return Sorted(values);
		}

		double[] sample = new double[SampleSize];
		double stride = values.Length / (double)SampleSize;
		for (int i = 0; i < SampleSize; i++)
		{
			sample[i] = values[(int)(i * stride)];
		}

		Array.Sort(sample);
		return sample;
	}

	private static double[] Sorted(double[] values)
	{
		double[] sorted = [.. values];
		Array.Sort(sorted);
		return sorted;
	}

	private static double[] Finite(IReadOnlyList<double>? values)
	{
		if (values is null || values.Count == 0)
		{
			return [];
		}

		List<double> finite = new(values.Count);
		foreach (double value in values)
		{
			if (double.IsFinite(value))
			{
				finite.Add(value);
			}
		}

		return [.. finite];
	}

	// A value as the x axis writes it: the axis's own formatter or unit, else the chart's, else the tick format.
	private string EdgeText(double value)
	{
		if ((XAxis?.Formatter ?? Formatter) is { } formatter)
		{
			return formatter(value);
		}

		if (XAxis?.Format is { Length: > 0 } format)
		{
			return value.ToString(format, CultureInfo.CurrentCulture);
		}

		if ((XAxis?.Unit ?? Unit) is { } unit)
		{
			return unit.Format(value, null, CultureInfo.CurrentCulture);
		}

		return value.ToString(TickFormat, CultureInfo.CurrentCulture);
	}
}
