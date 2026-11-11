using System.Globalization;

namespace Moka.Red.Core.Charts;

/// <summary>How values are spread along an axis.</summary>
public enum MokaScaleKind
{
	/// <summary>Equal steps for equal differences.</summary>
	Linear,

	/// <summary>Equal steps for equal ratios: 1, 10, 100.</summary>
	Log,

	/// <summary>Points in time, as Unix milliseconds.</summary>
	Time
}

/// <summary>What an axis has to show: its fixed ends, the values it must reach, and how its ticks are rounded.</summary>
public sealed record MokaScaleOptions
{
	/// <summary>A fixed bottom. Lower values sit on it. Null fits the data.</summary>
	public double? Min { get; init; }

	/// <summary>A fixed top. Higher values sit on it. Null fits the data.</summary>
	public double? Max { get; init; }

	/// <summary>The axis starts at least this low. Lower data still lowers it.</summary>
	public double? SuggestedMin { get; init; }

	/// <summary>The axis reaches at least this high. Higher data still raises it.</summary>
	public double? SuggestedMax { get; init; }

	/// <summary>How many divisions to aim for. Defaults to 4.</summary>
	public int TickCount { get; init; } = 4;

	/// <summary>
	///     Rounds the ends out to a round step in the unit the top of the axis reaches. False divides the span into
	///     <see cref="TickCount" /> equal parts, as today's charts do. Defaults to true.
	/// </summary>
	public bool Nice { get; init; } = true;

	/// <summary>Ticks to draw instead of working them out. The axis spans them and the data.</summary>
	public IReadOnlyList<double>? TickValues { get; init; }

	/// <summary>The units the ticks are written and rounded in. Null writes plain numbers.</summary>
	public MokaChartUnit? Unit { get; init; }

	/// <summary>
	///     Whether data that is all positive starts at zero and data that is all negative ends at zero. Defaults to true.
	/// </summary>
	public bool IncludeZero { get; init; } = true;

	/// <summary>Values the axis must include, such as thresholds, band edges and targets.</summary>
	public IReadOnlyList<double>? Reach { get; init; }

	/// <summary>The most ticks the axis gets, whatever else asks for more. Defaults to 50.</summary>
	public int MaxTicks { get; init; } = 50;
}

/// <summary>
///     A worked-out axis: its ends, its ticks, the step between them, and the unit and decimals they are written in.
///     Build one with <see cref="MokaNiceScale" /> or <see cref="MokaLogScale" />.
/// </summary>
/// <param name="Min">The value at the bottom of the axis.</param>
/// <param name="Max">The value at the top of the axis.</param>
/// <param name="Ticks">The tick values, bottom first. A log axis lists its minor ticks too: those that aren't a power of ten.</param>
/// <param name="Step">The step between ticks in the base unit, or null when they aren't evenly stepped.</param>
/// <param name="Unit">The unit step the ticks are written in.</param>
/// <param name="Decimals">The fewest decimals that write every tick exactly, in that unit.</param>
/// <param name="Kind">How values are spread along the axis.</param>
public sealed record MokaScale(
	double Min,
	double Max,
	IReadOnlyList<double> Ticks,
	double? Step,
	MokaUnitStep Unit,
	int Decimals,
	MokaScaleKind Kind)
{
	/// <summary>
	///     The unit the scale was built with. A log axis writes each value in its own step of it, so its ticks read 1 ms, 10
	///     ms, 100 ms and then 1 s. Null on a scale built without a unit.
	/// </summary>
	public MokaChartUnit? ChartUnit { get; init; }

	/// <summary>Where a value sits along the axis: 0 at <see cref="Min" />, 1 at <see cref="Max" />, not clamped.</summary>
	/// <param name="value">The value.</param>
	/// <returns>The share, or NaN for a value a log axis can't show (zero or less) or that isn't a number.</returns>
	public double Share(double value)
	{
		if (!double.IsFinite(value))
		{
			return double.NaN;
		}

		if (Kind == MokaScaleKind.Log)
		{
			if (value <= 0 || Min <= 0 || Max <= 0)
			{
				return double.NaN;
			}

			double low = Math.Log10(Min);
			double span = Math.Log10(Max) - low;
			return span <= 0 ? 0.5 : (Math.Log10(value) - low) / span;
		}

		double range = Max - Min;
		return range == 0 ? 0.5 : (value - Min) / range;
	}

	/// <summary>
	///     Writes a value the way the axis writes its ticks: in the axis's unit, with the culture's digit grouping, so 12000
	///     reads "12,000" or "12K". On an evenly stepped axis every value takes the step's decimals, so 1.080 sits between
	///     1.076 and 1.084; a log axis writes each with the decimals it needs. Zero has no suffix unless the unit has just
	///     one step, as in "0%".
	/// </summary>
	/// <param name="value">The value, in the base unit.</param>
	/// <param name="withUnit">Whether the unit's suffix follows the number.</param>
	/// <param name="culture">The culture to write the number in. Null uses the current culture.</param>
	/// <returns>The value as text.</returns>
	public string Format(double value, bool withUnit = true, CultureInfo? culture = null)
	{
		CultureInfo format = culture ?? CultureInfo.CurrentCulture;
		if (!double.IsFinite(value))
		{
			return value.ToString(format);
		}

		if (value == 0)
		{
			value = 0;
		}

		if (Kind == MokaScaleKind.Log && ChartUnit is not null)
		{
			MokaUnitStep step = ChartUnit.StepFor(value);
			string text = Grouped(value / (step.Factor > 0 ? step.Factor : 1), format);
			return withUnit ? ChartUnit.Join(text, step.Suffix) : text;
		}

		double factor = double.IsFinite(Unit.Factor) && Unit.Factor > 0 ? Unit.Factor : 1;
		string number = Kind == MokaScaleKind.Log
			? (value / factor).ToString(MokaScaleMath.GroupedPattern(MokaScaleMath.DecimalsOf(value / factor)), format)
			: (value / factor).ToString(MokaScaleMath.FixedPattern(Decimals), format);
		if (!withUnit || string.IsNullOrEmpty(Unit.Suffix))
		{
			return number;
		}

		bool singleStep = ChartUnit is null || ChartUnit.Steps.Count <= 1;
		if (value == 0 && !singleStep)
		{
			return number;
		}

		return ChartUnit is null ? $"{number} {Unit.Suffix}" : ChartUnit.Join(number, Unit.Suffix);
	}

	private static string Grouped(double value, CultureInfo culture) =>
		value.ToString(MokaScaleMath.GroupedPattern(MokaScaleMath.DecimalsOf(value)), culture);
}

/// <summary>The rounding helpers the scales share.</summary>
internal static class MokaScaleMath
{
	public const int MostDecimals = 6;

	/// <summary>The fewest decimals, up to six, that write a value exactly.</summary>
	public static int DecimalsOf(double value)
	{
		if (!double.IsFinite(value))
		{
			return 0;
		}

		double size = Math.Abs(value);
		for (int places = 0; places < MostDecimals; places++)
		{
			double scaled = size * Math.Pow(10, places);
			if (Math.Abs(Math.Round(scaled) - scaled) < 1e-6)
			{
				return places;
			}
		}

		return MostDecimals;
	}

	/// <summary>The fewest decimals that write every value exactly.</summary>
	public static int DecimalsOf(IEnumerable<double> values)
	{
		int most = 0;
		foreach (double value in values)
		{
			most = Math.Max(most, DecimalsOf(value));
		}

		return most;
	}

	/// <summary>A number pattern with digit grouping and up to this many decimals, trailing zeros dropped.</summary>
	public static string GroupedPattern(int decimals) =>
		decimals <= 0 ? "#,##0" : "#,##0." + new string('#', Math.Min(decimals, 15));

	/// <summary>A number pattern with digit grouping and exactly this many decimals, trailing zeros kept.</summary>
	public static string FixedPattern(int decimals) =>
		decimals <= 0 ? "#,##0" : "#,##0." + new string('0', Math.Min(decimals, 15));
}
