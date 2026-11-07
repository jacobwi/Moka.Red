using System.Globalization;

namespace Moka.Red.Forms.TimePicker;

/// <summary>Reads a time of day as people type it. Ported from MokaGate's <c>RouterClock.Parse</c>.</summary>
internal static class MokaTimeParser
{
	private static readonly char[] Separators = [':', '.', 'H'];

	/// <summary>
	///     Reads 21:30, 9:30 pm, 9.30p, 21h30, 2130, 930pm, 9 pm or 21. Null when the text isn't a time of day.
	/// </summary>
	/// <param name="text">What was typed.</param>
	/// <param name="format24">
	///     Whether the field writes 24-hour times. On a 24-hour clock a bare 9 is 09:00. On a 12-hour clock it names
	///     two times, so <paramref name="shownAfternoon" /> picks one, the way the popup's hour column keeps its AM or PM.
	/// </param>
	/// <param name="shownAfternoon">Whether the time the field holds now is in the afternoon. Null when it holds none.</param>
	internal static TimeSpan? Parse(string? text, bool format24, bool? shownAfternoon = null)
	{
		string typed = (text ?? string.Empty).Trim().ToUpperInvariant()
			.Replace(" ", string.Empty, StringComparison.Ordinal)
			.Replace(".M.", "M", StringComparison.Ordinal);
		(string digits, bool? afternoon) = SplitMarker(typed);

		int split = digits.IndexOfAny(Separators);
		(string hours, string minutes) = split >= 0 ? (digits[..split], digits[(split + 1)..])
			: digits.Length is 3 or 4 ? (digits[..^2], digits[^2..])
			: (digits, "00");

		if (hours.Length is 0 or > 2 || minutes.Length != 2 || !hours.All(char.IsAsciiDigit) ||
		    !minutes.All(char.IsAsciiDigit))
		{
			return null;
		}

		int hour = int.Parse(hours, CultureInfo.InvariantCulture);
		int minute = int.Parse(minutes, CultureInfo.InvariantCulture);
		bool? half = afternoon ?? (!format24 && hour is >= 1 and <= 12 ? shownAfternoon : null);
		if (half is { } pm)
		{
			if (hour is < 1 or > 12)
			{
				return null;
			}

			hour = (hour % 12) + (pm ? 12 : 0);
		}

		return hour < 24 && minute < 60 ? new TimeSpan(hour, minute, 0) : null;
	}

	private static (string Digits, bool? Afternoon) SplitMarker(string typed)
	{
		if (typed.EndsWith("PM", StringComparison.Ordinal))
		{
			return (typed[..^2], true);
		}

		if (typed.EndsWith("AM", StringComparison.Ordinal))
		{
			return (typed[..^2], false);
		}

		if (typed.EndsWith('P'))
		{
			return (typed[..^1], true);
		}

		return typed.EndsWith('A') ? (typed[..^1], false) : (typed, null);
	}
}
