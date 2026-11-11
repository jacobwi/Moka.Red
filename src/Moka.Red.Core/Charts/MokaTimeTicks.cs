using System.Globalization;
using System.Text;

namespace Moka.Red.Core.Charts;

/// <summary>What a time tick marks, which decides how strongly it is drawn.</summary>
public enum MokaTimeTickLevel
{
	/// <summary>A tick at the axis's step that the next coarser step skips.</summary>
	Minor,

	/// <summary>A tick the next coarser step also lands on.</summary>
	Major,

	/// <summary>Midnight on an axis stepping in hours or less: the day begins.</summary>
	Day,

	/// <summary>The first of a month on an axis stepping in days or less.</summary>
	Month,

	/// <summary>The first of January on an axis stepping in months or less.</summary>
	Year
}

/// <summary>A tick on a time axis.</summary>
/// <param name="At">When it falls, in the axis's time zone.</param>
/// <param name="Text">What it reads, such as <c>14:00</c>, <c>Tue 12</c> or <c>Mar</c>.</param>
/// <param name="Level">What it marks.</param>
/// <param name="NeedEm">
///     The narrowest axis, in em, it shows on: the need of the coarsest step that lands on it. An axis halves its ticks
///     as it narrows by hiding those whose need is wider than itself.
/// </param>
public sealed record MokaTimeTick(DateTimeOffset At, string Text, MokaTimeTickLevel Level, double NeedEm);

/// <summary>The time zone, culture and text of a time axis.</summary>
public sealed record MokaTimeTickOptions
{
	/// <summary>
	///     The zone ticks are aligned and written in. Null uses <see cref="TimeZoneInfo.Local" />, which on Blazor Server is
	///     the server's zone.
	/// </summary>
	public TimeZoneInfo? Zone { get; init; }

	/// <summary>The culture ticks are written in. Null uses the current culture.</summary>
	public CultureInfo? Culture { get; init; }

	/// <summary>A .NET date format for the minor and major ticks, in place of the one picked for the step.</summary>
	public string? Format { get; init; }

	/// <summary>
	///     Writes every tick in place of <see cref="Format" /> and the patterns picked for the step. A step whose ticks it
	///     can't tell apart gives way to a coarser one, as it does for <see cref="Format" />.
	/// </summary>
	public Func<DateTimeOffset, string>? Formatter { get; init; }

	/// <summary>
	///     Whether times are written on a 12-hour clock, in the ticks and in <see cref="MokaTimeTicks.TooltipText" />. Null
	///     follows the culture. <see cref="Format" /> and <see cref="TooltipFormat" /> are written as they are given.
	/// </summary>
	public bool? Hour12 { get; init; }

	/// <summary>A .NET date format for <see cref="MokaTimeTicks.TooltipText" />, in place of the one picked for the span.</summary>
	public string? TooltipFormat { get; init; }

	/// <summary>The widest axis, in em, the ticks are worked out for. Defaults to 120.</summary>
	public double MaxAxisEm { get; init; } = 120;
}

/// <summary>
///     Picks the steps of a time axis by its span and width: seconds, minutes and hours aligned to the clock in the
///     axis's zone, days, weeks starting on Monday, months and years, with midnight and the first of each month marked.
/// </summary>
public static class MokaTimeTicks
{
	private const double AverageMonthDays = 365.2425 / 12;
	private const int MostTicks = 2000;

	private enum StepKind
	{
		Clock,
		Days,
		Week,
		Months,
		Years
	}

	private readonly record struct TimeStep(StepKind Kind, int Count, TimeSpan Length, double LabelEm);

	private static readonly TimeStep[] Ladder =
	[
		Clock(TimeSpan.FromSeconds(5)), Clock(TimeSpan.FromSeconds(10)), Clock(TimeSpan.FromSeconds(15)),
		Clock(TimeSpan.FromSeconds(30)),
		Clock(TimeSpan.FromMinutes(1)), Clock(TimeSpan.FromMinutes(2)), Clock(TimeSpan.FromMinutes(5)),
		Clock(TimeSpan.FromMinutes(10)), Clock(TimeSpan.FromMinutes(15)), Clock(TimeSpan.FromMinutes(30)),
		Clock(TimeSpan.FromHours(1)), Clock(TimeSpan.FromHours(2)), Clock(TimeSpan.FromHours(3)), Clock(TimeSpan.FromHours(6)),
		Clock(TimeSpan.FromHours(12)),
		new(StepKind.Days, 1, TimeSpan.FromDays(1), 4.0),
		new(StepKind.Days, 2, TimeSpan.FromDays(2), 4.0),
		new(StepKind.Week, 1, TimeSpan.FromDays(7), 4.0),
		new(StepKind.Months, 1, TimeSpan.FromDays(AverageMonthDays), 2.8),
		new(StepKind.Months, 3, TimeSpan.FromDays(3 * AverageMonthDays), 2.8),
		new(StepKind.Years, 1, TimeSpan.FromDays(365.2425), 2.6)
	];

	private static readonly int DayIndex = Array.FindIndex(Ladder, s => s.Kind == StepKind.Days);
	private static readonly int MonthIndex = Array.FindIndex(Ladder, s => s.Kind == StepKind.Months);
	private static readonly int YearIndex = Ladder.Length - 1;

	/// <summary>The ticks of a time axis from <paramref name="from" /> to <paramref name="to" />, in time order.</summary>
	/// <param name="from">Where the axis starts.</param>
	/// <param name="to">Where the axis ends.</param>
	/// <param name="options">The zone, culture, text and widest axis. Null uses the defaults.</param>
	/// <returns>
	///     The ticks at the finest step that fits the widest axis, with month starts added between days. Two ticks next to
	///     each other never read the same: a step finer than the text can tell apart, such as seconds written as
	///     <c>HH:mm</c>, gives way to a coarser one.
	/// </returns>
	public static IReadOnlyList<MokaTimeTick> For(DateTimeOffset from, DateTimeOffset to, MokaTimeTickOptions? options = null)
	{
		if (to <= from)
		{
			return [];
		}

		TimeZoneInfo zone = options?.Zone ?? TimeZoneInfo.Local;
		CultureInfo culture = options?.Culture ?? CultureInfo.CurrentCulture;
		TimeSpan span = to - from;
		for (int chosen = IndexFor(span, AxisEm(options)); ; chosen++)
		{
			List<MokaTimeTick> ticks = TicksAt(chosen, from, to, span, zone, culture, options);
			if (!Repeats(ticks))
			{
				return ticks;
			}

			if (chosen == Ladder.Length - 1)
			{
				return WithoutRepeats(ticks);
			}
		}
	}

	/// <summary>
	///     The step a time axis this wide takes for this span: the finest one whose labels fit. Months and years are
	///     given at their average lengths.
	/// </summary>
	/// <param name="span">The time the axis covers.</param>
	/// <param name="axisEm">The axis's width in em.</param>
	/// <returns>The step.</returns>
	public static TimeSpan StepFor(TimeSpan span, double axisEm) =>
		Ladder[IndexFor(span, double.IsFinite(axisEm) && axisEm > 0 ? axisEm : 120)].Length;

	/// <summary>
	///     A point's time written for a tooltip, with as much of the date as the span needs: the time within a day, the
	///     weekday and time up to six days, and the date and time beyond, since a longer span can start and end on the same
	///     weekday.
	/// </summary>
	/// <param name="at">The point's time.</param>
	/// <param name="span">The time the chart covers.</param>
	/// <param name="options">
	///     The zone, culture and clock, and <see cref="MokaTimeTickOptions.TooltipFormat" /> in place of the pattern
	///     picked for the span. Null uses the defaults.
	/// </param>
	/// <returns>The text.</returns>
	public static string TooltipText(DateTimeOffset at, TimeSpan span, MokaTimeTickOptions? options = null)
	{
		TimeZoneInfo zone = options?.Zone ?? TimeZoneInfo.Local;
		CultureInfo culture = options?.Culture ?? CultureInfo.CurrentCulture;
		DateTimeOffset local = TimeZoneInfo.ConvertTime(at, zone);
		if (options?.TooltipFormat is { Length: > 0 } own)
		{
			return local.ToString(own, culture);
		}

		DateTimeFormatInfo format = culture.DateTimeFormat;
		string time = WithClock(local.Second != 0 || span < TimeSpan.FromMinutes(1) ? format.LongTimePattern : format.ShortTimePattern,
			options?.Hour12);

		string pattern = span <= TimeSpan.FromDays(1) ? time
			: span <= TimeSpan.FromDays(6) ? $"ddd {time}"
			: $"{format.ShortDatePattern} {time}";
		return local.ToString(pattern, culture);
	}

	/// <summary>
	///     Whether a .NET date format writes the hour on a 12-hour clock (<c>h</c>) or a 24-hour one (<c>H</c>); null when it
	///     writes no hour. Quoted text and escaped characters don't count.
	/// </summary>
	/// <param name="format">The format, such as <c>h:mm tt</c>.</param>
	/// <returns>True for a 12-hour clock, false for a 24-hour one, null for neither.</returns>
	public static bool? WritesHour12(string? format)
	{
		if (string.IsNullOrEmpty(format))
		{
			return null;
		}

		for (int i = 0; i < format.Length; i++)
		{
			char c = format[i];
			if (c is '\'' or '"')
			{
				int close = format.IndexOf(c, i + 1);
				i = close < 0 ? format.Length : close;
			}
			else if (c == '\\')
			{
				i++;
			}
			else if (c is 'h' or 'H')
			{
				return c == 'h';
			}
		}

		return null;
	}

	private static TimeStep Clock(TimeSpan length) =>
		new(StepKind.Clock, 1, length, length < TimeSpan.FromMinutes(1) ? 4.6 : 3.4);

	private static List<MokaTimeTick> TicksAt(int chosen, DateTimeOffset from, DateTimeOffset to, TimeSpan span, TimeZoneInfo zone,
		CultureInfo culture, MokaTimeTickOptions? options)
	{
		TimeStep step = Ladder[chosen];
		List<DateTimeOffset> instants = Instants(step, from, to, zone);
		if ((step.Kind == StepKind.Days && step.Count > 1) || step.Kind == StepKind.Week)
		{
			instants.AddRange(Instants(Ladder[MonthIndex], from, to, zone));
		}

		List<MokaTimeTick> ticks = [];
		foreach (DateTimeOffset instant in instants.Distinct().Order().Take(MostTicks))
		{
			DateTimeOffset local = TimeZoneInfo.ConvertTime(instant, zone);
			MokaTimeTickLevel level = LevelOf(chosen, local.DateTime);
			string text = options?.Formatter?.Invoke(local) ?? TextOf(level, step, local, culture, options);
			ticks.Add(new MokaTimeTick(local, text, level, NeedOf(span, local.DateTime)));
		}

		return ticks;
	}

	private static bool Repeats(List<MokaTimeTick> ticks)
	{
		for (int i = 1; i < ticks.Count; i++)
		{
			if (string.Equals(ticks[i].Text, ticks[i - 1].Text, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	// Even years can read alike, under a format that writes no year; the first of each run keeps its place.
	private static List<MokaTimeTick> WithoutRepeats(List<MokaTimeTick> ticks)
	{
		List<MokaTimeTick> kept = [];
		foreach (MokaTimeTick tick in ticks)
		{
			if (kept.Count == 0 || !string.Equals(kept[^1].Text, tick.Text, StringComparison.Ordinal))
			{
				kept.Add(tick);
			}
		}

		return kept;
	}

	// A culture's time pattern moved onto the clock asked for: H and HH become h with an AM or PM designator, or h
	// becomes HH and the designator goes. Quoted text, such as the 'h' in Canadian French's HH 'h' mm, stays.
	private static string WithClock(string pattern, bool? hour12)
	{
		if (hour12 is not { } twelve || WritesHour12(pattern) is not { } current || current == twelve)
		{
			return pattern;
		}

		StringBuilder written = new(pattern.Length + 3);
		bool designator = false;
		for (int i = 0; i < pattern.Length; i++)
		{
			char c = pattern[i];
			if (c is '\'' or '"')
			{
				int close = pattern.IndexOf(c, i + 1);
				int end = close < 0 ? pattern.Length - 1 : close;
				written.Append(pattern, i, end - i + 1);
				i = end;
			}
			else if (c == '\\' && i + 1 < pattern.Length)
			{
				written.Append(pattern, i, 2);
				i++;
			}
			else if (c is 'h' or 'H' or 't')
			{
				while (i + 1 < pattern.Length && pattern[i + 1] == c)
				{
					i++;
				}

				if (c != 't')
				{
					written.Append(twelve ? "h" : "HH");
				}
				else if (twelve)
				{
					written.Append("tt");
					designator = true;
				}
			}
			else
			{
				written.Append(c);
			}
		}

		if (twelve)
		{
			return designator ? written.ToString() : $"{written} tt";
		}

		// The designator's space goes with it: "h:mm tt" is "HH:mm", and Korean's "tt h:mm" too.
		StringBuilder single = new(written.Length);
		foreach (char c in written.ToString().Trim())
		{
			if (c != ' ' || single.Length == 0 || single[^1] != ' ')
			{
				single.Append(c);
			}
		}

		return single.ToString();
	}

	private static double AxisEm(MokaTimeTickOptions? options) =>
		options?.MaxAxisEm is { } em && double.IsFinite(em) && em > 0 ? em : 120;

	private static double NeedFor(TimeSpan span, TimeStep step) => span / step.Length * step.LabelEm;

	private static int IndexFor(TimeSpan span, double axisEm)
	{
		for (int i = 0; i < Ladder.Length; i++)
		{
			if (NeedFor(span, Ladder[i]) <= axisEm)
			{
				return i;
			}
		}

		return Ladder.Length - 1;
	}

	private static double NeedOf(TimeSpan span, DateTime local)
	{
		for (int i = Ladder.Length - 1; i >= 0; i--)
		{
			if (Lands(Ladder[i], local))
			{
				return NeedFor(span, Ladder[i]);
			}
		}

		return NeedFor(span, Ladder[0]);
	}

	private static MokaTimeTickLevel LevelOf(int chosen, DateTime local)
	{
		if (chosen < YearIndex && Lands(Ladder[YearIndex], local))
		{
			return MokaTimeTickLevel.Year;
		}

		if (chosen < MonthIndex && Lands(Ladder[MonthIndex], local))
		{
			return MokaTimeTickLevel.Month;
		}

		if (chosen < DayIndex && Lands(Ladder[DayIndex], local))
		{
			return MokaTimeTickLevel.Day;
		}

		return chosen == Ladder.Length - 1 || Lands(Ladder[chosen + 1], local)
			? MokaTimeTickLevel.Major
			: MokaTimeTickLevel.Minor;
	}

	// Whether a step's grid, aligned to the clock and the calendar in the zone, passes through this wall-clock time.
	private static bool Lands(TimeStep step, DateTime local)
	{
		if (step.Kind == StepKind.Clock)
		{
			return local.TimeOfDay.Ticks % step.Length.Ticks == 0;
		}

		if (local.TimeOfDay != TimeSpan.Zero)
		{
			return false;
		}

		return step.Kind switch
		{
			StepKind.Days => step.Count == 1 || DateOnly.FromDateTime(local).DayNumber % step.Count == 0,
			StepKind.Week => local.DayOfWeek == DayOfWeek.Monday,
			StepKind.Months => local.Day == 1 && (local.Month - 1) % step.Count == 0,
			_ => local.Day == 1 && local.Month == 1
		};
	}

	private static List<DateTimeOffset> Instants(TimeStep step, DateTimeOffset from, DateTimeOffset to, TimeZoneInfo zone)
	{
		List<DateTimeOffset> instants = [];
		DateTimeOffset localFrom = TimeZoneInfo.ConvertTime(from, zone);

		if (step.Kind == StepKind.Clock && step.Length < TimeSpan.FromHours(1))
		{
			// Every step under an hour divides an hour, and zones shift by whole hours or halves, so the grid stays on
			// the clock across a change of offset.
			long into = localFrom.TimeOfDay.Ticks % step.Length.Ticks;
			DateTimeOffset at = into == 0 ? from : from + TimeSpan.FromTicks(step.Length.Ticks - into);
			for (; at <= to && instants.Count < MostTicks; at += step.Length)
			{
				instants.Add(at);
			}

			return instants;
		}

		DateTimeOffset localTo = TimeZoneInfo.ConvertTime(to, zone);
		DateTime firstDay = step.Kind switch
		{
			StepKind.Months => new DateTime(localFrom.Year, localFrom.Month, 1),
			StepKind.Years => new DateTime(localFrom.Year, 1, 1),
			_ => localFrom.Date
		};

		for (DateTime day = firstDay; day <= localTo.DateTime && instants.Count < MostTicks; day = Next(step, day))
		{
			if (step.Kind == StepKind.Clock)
			{
				for (TimeSpan hour = TimeSpan.Zero; hour < TimeSpan.FromDays(1); hour += step.Length)
				{
					Add(instants, day + hour, zone, from, to);
				}
			}
			else if (Lands(step, day))
			{
				Add(instants, day, zone, from, to);
			}
		}

		return instants;
	}

	private static DateTime Next(TimeStep step, DateTime day) => step.Kind switch
	{
		StepKind.Months => day.AddMonths(1),
		StepKind.Years => day.AddYears(1),
		_ => day.AddDays(1)
	};

	// A wall-clock time the zone skips (the hour lost when clocks go forward) has no tick; one it repeats gets one, in
	// standard time.
	private static void Add(List<DateTimeOffset> instants, DateTime wall, TimeZoneInfo zone, DateTimeOffset from,
		DateTimeOffset to)
	{
		DateTime unspecified = DateTime.SpecifyKind(wall, DateTimeKind.Unspecified);
		if (zone.IsInvalidTime(unspecified))
		{
			return;
		}

		DateTimeOffset instant = new(TimeZoneInfo.ConvertTimeToUtc(unspecified, zone), TimeSpan.Zero);
		if (instant >= from && instant <= to)
		{
			instants.Add(instant);
		}
	}

	private static string TextOf(MokaTimeTickLevel level, TimeStep step, DateTimeOffset local, CultureInfo culture,
		MokaTimeTickOptions? options)
	{
		string pattern = level switch
		{
			MokaTimeTickLevel.Year => "yyyy",
			MokaTimeTickLevel.Month => "MMM",
			MokaTimeTickLevel.Day => "ddd d",
			_ when options?.Format is { Length: > 0 } format => format,
			_ => StepPattern(step, local, culture.DateTimeFormat, options?.Hour12)
		};

		return local.ToString(pattern, culture);
	}

	private static string StepPattern(TimeStep step, DateTimeOffset local, DateTimeFormatInfo format, bool? hour12) => step.Kind switch
	{
		StepKind.Clock when step.Length < TimeSpan.FromMinutes(1) => WithClock(format.LongTimePattern, hour12),
		StepKind.Clock => HourPattern(WithClock(format.ShortTimePattern, hour12), local.Minute == 0),
		StepKind.Days when step.Count == 1 => "ddd d",
		StepKind.Days or StepKind.Week => format.MonthDayPattern.TrimStart().StartsWith('M') ? "MMM d" : "d MMM",
		StepKind.Months => "MMM",
		_ => "yyyy"
	};

	// The culture's short time with seconds dropped. A twelve-hour clock writes a whole hour as "2 PM".
	private static string HourPattern(string shortTime, bool wholeHour)
	{
		string pattern = shortTime.Replace(":ss", string.Empty, StringComparison.Ordinal)
			.Replace(".ss", string.Empty, StringComparison.Ordinal);
		return wholeHour && pattern.Contains('t', StringComparison.Ordinal)
			? pattern.Replace(":mm", string.Empty, StringComparison.Ordinal).Replace(".mm", string.Empty, StringComparison.Ordinal)
			: pattern;
	}
}
