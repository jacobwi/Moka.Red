using System.Globalization;

namespace Moka.Red.Data.Table;

/// <summary>
///     Whether a row's value passes one filter. Text operators read the cell's text; value operators read its value,
///     taking the filter's values in the invariant culture as the value's own kind: a number, a date, yes or no, or text.
/// </summary>
internal static class TableFilterMatch
{
	public static bool Matches(MokaTableFilter filter, object? value, string text)
	{
		IReadOnlyList<string> values = filter.Values;
		string first = values.Count > 0 ? values[0] : string.Empty;
		return filter.Operator switch
		{
			MokaFilterOperator.Contains => first.Length == 0 || text.Contains(first, StringComparison.OrdinalIgnoreCase),
			MokaFilterOperator.DoesNotContain => first.Length == 0 || !text.Contains(first, StringComparison.OrdinalIgnoreCase),
			MokaFilterOperator.StartsWith => text.StartsWith(first, StringComparison.OrdinalIgnoreCase),
			MokaFilterOperator.EndsWith => text.EndsWith(first, StringComparison.OrdinalIgnoreCase),
			MokaFilterOperator.IsEmpty => IsEmpty(value, text),
			MokaFilterOperator.IsNotEmpty => !IsEmpty(value, text),
			MokaFilterOperator.Is => values.Count == 0 || Compare(value, first) == 0,
			MokaFilterOperator.IsNot => values.Count == 0 || Compare(value, first) != 0,
			MokaFilterOperator.GreaterThan => Compare(value, first) > 0,
			MokaFilterOperator.AtLeast => Compare(value, first) is >= 0 and not Unordered,
			MokaFilterOperator.LessThan => Compare(value, first) is < 0 and not Unordered,
			MokaFilterOperator.AtMost => Compare(value, first) is <= 0 and not Unordered,
			MokaFilterOperator.Between => values.Count >= 2
			                              && Compare(value, values[0]) is >= 0 and not Unordered
			                              && Compare(value, values[1]) is <= 0 and not Unordered,
			MokaFilterOperator.IsAnyOf => values.Count == 0 || values.Any(candidate => Compare(value, candidate) == 0),
			MokaFilterOperator.IsNoneOf => !values.Any(candidate => Compare(value, candidate) == 0),
			_ => true
		};
	}

	// What a comparison says when the two can't be ordered: a missing value, or a value that isn't a number or a date
	// when the filter's is. It fails every ordering test, and equals nothing.
	private const int Unordered = int.MinValue;

	private static bool IsEmpty(object? value, string text) =>
		value is null || (value is string s ? s.Length == 0 : text.Length == 0);

	/// <summary>
	///     Orders a row's value against a filter value written in the invariant culture, by the row value's kind. Gives
	///     <see cref="Unordered" /> when they can't be compared, which equals nothing and orders after nothing.
	/// </summary>
	internal static int Compare(object? value, string filterValue)
	{
		switch (value)
		{
			case null:
				return Unordered;
			case string text:
				return string.Compare(text, filterValue, StringComparison.OrdinalIgnoreCase);
			case bool flag:
				return bool.TryParse(filterValue, out bool wanted) ? flag.CompareTo(wanted) : Unordered;
			case DateTimeOffset moment when IsDay(filterValue, out DateOnly wantedDay):
				return DateOnly.FromDateTime(moment.Date).CompareTo(wantedDay);
			case DateTimeOffset moment:
				return DateTimeOffset.TryParse(filterValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset other)
					? moment.CompareTo(other)
					: Unordered;
			case DateTime date when IsDay(filterValue, out DateOnly wantedDay):
				return DateOnly.FromDateTime(date).CompareTo(wantedDay);
			case DateTime date:
				return DateTime.TryParse(filterValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime otherDate)
					? date.CompareTo(otherDate)
					: Unordered;
			case DateOnly day:
				return DateOnly.TryParse(filterValue, CultureInfo.InvariantCulture, out DateOnly otherDay) ? day.CompareTo(otherDay) : Unordered;
			case TimeOnly time:
				return TimeOnly.TryParse(filterValue, CultureInfo.InvariantCulture, out TimeOnly otherTime) ? time.CompareTo(otherTime) : Unordered;
			case Enum choice:
				return string.Compare(choice.ToString(), filterValue, StringComparison.OrdinalIgnoreCase);
		}

		if (MokaTablePipeline.AsNumber(value) is { } number)
		{
			return double.TryParse(filterValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double wanted)
				? number.CompareTo(wanted)
				: Unordered;
		}

		return string.Compare(Convert.ToString(value, CultureInfo.InvariantCulture), filterValue, StringComparison.OrdinalIgnoreCase);
	}

	// A day alone, as a date range writes its ends, compares a time by its day, so "up to the 14th" keeps all of the 14th.
	private static bool IsDay(string filterValue, out DateOnly day) =>
		DateOnly.TryParseExact(filterValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day);

	/// <summary>A value written the way filters write theirs: in the invariant culture, dates in ISO 8601.</summary>
	internal static string? Invariant(object? value) => value switch
	{
		null => null,
		string text => text,
		bool flag => flag ? "true" : "false",
		DateTimeOffset moment => moment.ToString("O", CultureInfo.InvariantCulture),
		DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
		DateOnly day => day.ToString("O", CultureInfo.InvariantCulture),
		TimeOnly time => time.ToString("O", CultureInfo.InvariantCulture),
		IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
		_ => value.ToString()
	};
}
