using System.Globalization;
using Moka.Red.Feedback.JobProgress;

namespace Moka.Red.Core.Utilities;

/// <summary>
///     Writes a byte count in the largest unit that fits, such as "1.5 GB", in the current culture. <c>MokaBytes</c>
///     and <c>MokaJobProgress</c> both write through it, so they can't disagree, and an app can call it to put a size
///     in a string.
/// </summary>
public static class MokaByteFormat
{
	// double.ToString keeps at most this many digits that mean anything.
	private const int MostDecimals = 15;

	private static readonly string[] DecimalUnits = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];
	private static readonly string[] BinaryUnits = ["B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB"];

	/// <summary>Writes a byte count with its unit, such as "1.5 GB" or, as a rate, "12 MB/s".</summary>
	/// <param name="bytes">The count. A negative one keeps its sign, and NaN or infinity reads as 0.</param>
	/// <param name="units">Whether to count in powers of 1000 or 1024.</param>
	/// <param name="decimals">
	///     How many digits follow the point, always that many, and clamped to 0 to 15. Null shows one under 100 and none
	///     from there up, dropping a zero, as in "1.5 GB", "2 GB" and "148 MB". A count in plain bytes has none either way.
	/// </param>
	/// <param name="perSecond">Adds "/s" after the unit.</param>
	/// <returns>The count as text.</returns>
	public static string Format(
		double bytes,
		MokaByteUnits units = MokaByteUnits.Decimal,
		int? decimals = null,
		bool perSecond = false)
	{
		(string number, string unit) = Parts(bytes, units, decimals);
		return perSecond ? $"{number} {unit}/s" : $"{number} {unit}";
	}

	/// <summary>
	///     The number and the unit of <see cref="Format" /> apart, for text that says a shared unit once, such as
	///     "148 of 240 MB".
	/// </summary>
	/// <param name="bytes">The count. A negative one keeps its sign, and NaN or infinity reads as 0.</param>
	/// <param name="units">Whether to count in powers of 1000 or 1024.</param>
	/// <param name="decimals">How many digits follow the point, as in <see cref="Format" />.</param>
	/// <returns>The number, and the unit it's in.</returns>
	public static (string Number, string Unit) Parts(
		double bytes,
		MokaByteUnits units = MokaByteUnits.Decimal,
		int? decimals = null)
	{
		bool binary = units == MokaByteUnits.Binary;
		double step = binary ? 1024 : 1000;
		string[] names = binary ? BinaryUnits : DecimalUnits;

		double value = double.IsFinite(bytes) ? bytes : 0;
		int unit = 0;
		while (Math.Abs(value) >= step && unit < names.Length - 1)
		{
			value /= step;
			unit++;
		}

		// 999.6 KB would otherwise print as "1000 KB".
		if (Math.Abs(Rounded(value, unit, decimals)) >= step && unit < names.Length - 1)
		{
			value /= step;
			unit++;
		}

		return (Number(value, unit, decimals), names[unit]);
	}

	/// <summary>
	///     An amount done and its whole, for text such as "148 of 240 MB". The done part says its unit only when the two
	///     differ, as in "900 KB of 2 MB".
	/// </summary>
	/// <param name="done">How much is done.</param>
	/// <param name="whole">The whole.</param>
	/// <param name="units">Whether to count in powers of 1000 or 1024.</param>
	/// <returns>The two as text.</returns>
	public static (string Done, string Whole) Pair(double done, double whole, MokaByteUnits units = MokaByteUnits.Decimal)
	{
		(string doneNumber, string doneUnit) = Parts(done, units);
		(string wholeNumber, string wholeUnit) = Parts(whole, units);
		return (done == 0 || doneUnit == wholeUnit ? doneNumber : $"{doneNumber} {doneUnit}", $"{wholeNumber} {wholeUnit}");
	}

	private static double Rounded(double value, int unit, int? decimals) =>
		Math.Round(value, Places(value, unit, decimals), MidpointRounding.AwayFromZero);

	private static int Places(double value, int unit, int? decimals)
	{
		if (decimals is { } places)
		{
			return unit == 0 ? 0 : Math.Clamp(places, 0, MostDecimals);
		}

		return Math.Abs(value) >= 100 ? 0 : 1;
	}

	private static string Number(double value, int unit, int? decimals)
	{
		if (decimals is not { } places)
		{
			return value.ToString(Math.Abs(value) >= 100 ? "0" : "0.#", CultureInfo.CurrentCulture);
		}

		return value.ToString($"F{Places(value, unit, places)}", CultureInfo.CurrentCulture);
	}
}
