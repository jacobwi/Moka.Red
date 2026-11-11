using System.Globalization;

namespace Moka.Red.Core.Charts;

/// <summary>One step of a <see cref="MokaChartUnit" />, such as kilobytes: its size in the base unit and its name.</summary>
/// <param name="Factor">How many of the base unit one of this step is, such as 1000 for KB.</param>
/// <param name="Suffix">What follows a number in this step, such as <c>KB</c>. Empty writes the number alone.</param>
/// <param name="Multiples">
///     The round steps ticks take in this unit, such as 1, 2, 5, 10, 15 and 30 for minutes. Null uses 1, 2, 2.5, 4 and 5
///     times a power of ten.
/// </param>
public readonly record struct MokaUnitStep(double Factor, string Suffix, IReadOnlyList<double>? Multiples = null);

/// <summary>
///     The units a chart writes its values in, such as B, KB and MB, so a tick reads "1.5 GB" rather than 1500000000.
///     A value is written in the largest step its size reaches.
/// </summary>
public sealed record MokaChartUnit
{
	private static readonly double[] MinuteMultiples = [1, 2, 5, 10, 15, 30];
	private static readonly double[] HourMultiples = [1, 2, 3, 6, 12];

	/// <summary>The steps, smallest first. The first has a factor of 1.</summary>
	public required IReadOnlyList<MokaUnitStep> Steps { get; init; }

	/// <summary>What goes between a number and its suffix. Defaults to a space.</summary>
	public string Separator { get; init; } = " ";

	/// <summary>
	///     How far into the second step a value must reach before it leaves the first, as a multiple of the second step's
	///     factor. 10 keeps 9,500 as itself and writes 12,000 as 12K; later steps start at their own factor, so 3,400,000
	///     is 3.4M. Defaults to 1.
	/// </summary>
	public double SwitchAt { get; init; } = 1;

	/// <summary>Bytes in powers of 1000: B, KB, MB, GB, TB, PB, EB, the names <c>MokaByteFormat</c> writes.</summary>
	public static MokaChartUnit Bytes { get; } = Scaled(1000, "B", "KB", "MB", "GB", "TB", "PB", "EB");

	/// <summary>Bytes in powers of 1024: B, KiB, MiB, GiB, TiB, PiB, EiB.</summary>
	public static MokaChartUnit BytesBinary { get; } = Scaled(1024, "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB");

	/// <summary>A byte rate in powers of 1000: B/s, KB/s, MB/s and up.</summary>
	public static MokaChartUnit BytesPerSecond { get; } =
		Scaled(1000, "B/s", "KB/s", "MB/s", "GB/s", "TB/s", "PB/s", "EB/s");

	/// <summary>A byte rate in powers of 1024: B/s, KiB/s, MiB/s and up.</summary>
	public static MokaChartUnit BytesPerSecondBinary { get; } =
		Scaled(1024, "B/s", "KiB/s", "MiB/s", "GiB/s", "TiB/s", "PiB/s", "EiB/s");

	/// <summary>A bit rate in powers of 1000: bps, Kbps, Mbps, Gbps, Tbps.</summary>
	public static MokaChartUnit BitsPerSecond { get; } = Scaled(1000, "bps", "Kbps", "Mbps", "Gbps", "Tbps");

	/// <summary>A duration counted in milliseconds: ms, s, min and h, with minutes and hours on clock-friendly steps.</summary>
	public static MokaChartUnit Milliseconds { get; } = new()
	{
		Steps =
		[
			new MokaUnitStep(1, "ms"),
			new MokaUnitStep(1000, "s"),
			new MokaUnitStep(60_000, "min", MinuteMultiples),
			new MokaUnitStep(3_600_000, "h", HourMultiples)
		]
	};

	/// <summary>A duration counted in seconds: s, min and h, with minutes and hours on clock-friendly steps.</summary>
	public static MokaChartUnit Seconds { get; } = new()
	{
		Steps =
		[
			new MokaUnitStep(1, "s"),
			new MokaUnitStep(60, "min", MinuteMultiples),
			new MokaUnitStep(3600, "h", HourMultiples)
		]
	};

	/// <summary>A share in percent, written right after the number: 12%.</summary>
	public static MokaChartUnit Percent { get; } = new() { Steps = [new MokaUnitStep(1, "%")], Separator = string.Empty };

	/// <summary>Plain counts written short once they reach ten thousand: 9,500, then 12K, 3.4M and 1.2B.</summary>
	public static MokaChartUnit Compact { get; } = new()
	{
		Steps =
		[
			new MokaUnitStep(1, string.Empty),
			new MokaUnitStep(1e3, "K"),
			new MokaUnitStep(1e6, "M"),
			new MokaUnitStep(1e9, "B")
		],
		Separator = string.Empty,
		SwitchAt = 10
	};

	/// <summary>A unit with one step, such as <c>req/s</c> or <c>°C</c>.</summary>
	/// <param name="suffix">What follows each number.</param>
	/// <param name="separator">What goes between the number and the suffix.</param>
	/// <returns>The unit.</returns>
	public static MokaChartUnit Of(string suffix, string separator = " ") =>
		new() { Steps = [new MokaUnitStep(1, suffix ?? string.Empty)], Separator = separator ?? string.Empty };

	/// <summary>The largest step a value of this size is written in.</summary>
	/// <param name="top">The value, usually the top of an axis. Its sign is ignored.</param>
	/// <returns>The step.</returns>
	public MokaUnitStep StepFor(double top)
	{
		if (Steps is not { Count: > 0 })
		{
			return new MokaUnitStep(1, string.Empty);
		}

		double size = double.IsFinite(top) ? Math.Abs(top) : 0;
		MokaUnitStep step = Steps[0];
		for (int i = 1; i < Steps.Count; i++)
		{
			if (size >= Steps[i].Factor * SwitchFor(i))
			{
				step = Steps[i];
			}
		}

		return step;
	}

	/// <summary>
	///     Writes a value in the step its size reaches, such as "1.5 GB". A value that rounds up to the next step moves
	///     to it, so 999.96 KB reads "1 MB".
	/// </summary>
	/// <param name="value">The value, in the base unit. NaN or infinity reads as 0.</param>
	/// <param name="decimals">
	///     How many digits follow the point, always that many. Null shows one under 100 and none from there up, dropping a
	///     trailing zero, as <c>MokaByteFormat</c> does.
	/// </param>
	/// <param name="culture">The culture to write the number in. Null uses the current culture.</param>
	/// <returns>The value with its suffix.</returns>
	public string Format(double value, int? decimals = null, CultureInfo? culture = null)
	{
		if (Steps is not { Count: > 0 })
		{
			return Number(double.IsFinite(value) ? value : 0, decimals, culture ?? CultureInfo.CurrentCulture);
		}

		double size = double.IsFinite(value) ? value : 0;
		int index = IndexOf(StepFor(size));

		// Divided one step at a time, as MokaByteFormat does, so the two round a value the same way.
		double scaled = size;
		for (int i = 1; i <= index; i++)
		{
			scaled /= Ratio(i);
		}

		if (index + 1 < Steps.Count && Math.Abs(Round(scaled, decimals)) >= Ratio(index + 1) * SwitchFor(index + 1))
		{
			index++;
			scaled /= Ratio(index);
		}

		return Join(Number(scaled, decimals, culture ?? CultureInfo.CurrentCulture), Steps[index].Suffix);
	}

	/// <summary>Puts a written number and a suffix together with the <see cref="Separator" />.</summary>
	/// <param name="number">The number, already written.</param>
	/// <param name="suffix">The suffix. Empty leaves the number alone.</param>
	/// <returns>The two together.</returns>
	public string Join(string number, string suffix) =>
		string.IsNullOrEmpty(suffix) ? number : $"{number}{Separator}{suffix}";

	private int IndexOf(MokaUnitStep step)
	{
		if (Steps is null)
		{
			return 0;
		}

		for (int i = 0; i < Steps.Count; i++)
		{
			if (Steps[i].Equals(step))
			{
				return i;
			}
		}

		return 0;
	}

	private double SwitchFor(int index) => index == 1 && double.IsFinite(SwitchAt) && SwitchAt > 0 ? SwitchAt : 1;

	private double Ratio(int index)
	{
		double below = Steps[index - 1].Factor;
		double ratio = Steps[index].Factor / below;
		return double.IsFinite(ratio) && ratio > 0 ? ratio : 1;
	}

	private static double Round(double value, int? decimals) =>
		Math.Round(value, decimals is { } places ? Math.Clamp(places, 0, 15) : Math.Abs(value) >= 100 ? 0 : 1,
			MidpointRounding.AwayFromZero);

	private static string Number(double value, int? decimals, CultureInfo culture) =>
		decimals is { } places
			? value.ToString($"F{Math.Clamp(places, 0, 15).ToString(CultureInfo.InvariantCulture)}", culture)
			: value.ToString(Math.Abs(value) >= 100 ? "0" : "0.#", culture);

	private static MokaChartUnit Scaled(double factor, params string[] names)
	{
		MokaUnitStep[] steps = new MokaUnitStep[names.Length];
		double size = 1;
		for (int i = 0; i < names.Length; i++)
		{
			steps[i] = new MokaUnitStep(size, names[i]);
			size *= factor;
		}

		return new MokaChartUnit { Steps = steps };
	}
}
