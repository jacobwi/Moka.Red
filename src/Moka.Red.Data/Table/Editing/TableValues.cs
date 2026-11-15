using System.Globalization;

namespace Moka.Red.Data.Table;

// Turning what an editor or a pasted cell holds into a column's own type, and back.
internal static class TableValues
{
	public static Type Plain(Type type) => Nullable.GetUnderlyingType(type) ?? type;

	// The type that also holds no value: int? for int. Nullable<T> has no trimming requirements on T.
	public static Type CanBeEmpty(Type type) =>
		type.IsValueType && Nullable.GetUnderlyingType(type) is null ? typeof(Nullable<>).MakeGenericType(type) : type;

	// What an editor writes when nothing else says what a column holds.
	public static Type? TypeEditedBy(MokaColumnEditor editor) => editor switch
	{
		MokaColumnEditor.Number or MokaColumnEditor.Rating => typeof(double),
		MokaColumnEditor.Date => typeof(DateTime),
		MokaColumnEditor.Checkbox or MokaColumnEditor.Switch => typeof(bool),
		_ => null
	};

	public static bool IsNumber(Type type) => Plain(type) is var plain
	                                          && (plain == typeof(int) || plain == typeof(long) || plain == typeof(double)
	                                              || plain == typeof(decimal) || plain == typeof(float) || plain == typeof(short)
	                                              || plain == typeof(byte) || plain == typeof(uint) || plain == typeof(ulong)
	                                              || plain == typeof(ushort) || plain == typeof(sbyte));

	public static bool IsWholeNumber(Type type) => Plain(type) is var plain
	                                               && (plain == typeof(int) || plain == typeof(long) || plain == typeof(short)
	                                                   || plain == typeof(byte) || plain == typeof(uint) || plain == typeof(ulong)
	                                                   || plain == typeof(ushort) || plain == typeof(sbyte));

	public static bool IsDate(Type type) => Plain(type) is var plain
	                                        && (plain == typeof(DateTime) || plain == typeof(DateTimeOffset) || plain == typeof(DateOnly));

	public static double? ToDouble(object? value) => value switch
	{
		null => null,
		double d => d,
		IConvertible convertible => convertible.ToDouble(CultureInfo.InvariantCulture),
		_ => null
	};

	// A number in the type the column holds. A whole number type rounds, and a number out of its range is refused.
	public static bool TryFromDouble(double? number, Type type, out object? value)
	{
		value = null;
		if (number is not { } n)
		{
			return Nullable.GetUnderlyingType(type) is not null || !type.IsValueType;
		}

		if (double.IsNaN(n) || double.IsInfinity(n))
		{
			return false;
		}

		Type plain = Plain(type);
		try
		{
			value = plain == typeof(double) || plain == typeof(object) ? n
				: plain == typeof(decimal) ? (decimal)n
				: plain == typeof(float) ? (float)n
				: Convert.ChangeType(Math.Round(n, MidpointRounding.AwayFromZero), plain, CultureInfo.InvariantCulture);
			return true;
		}
		catch (OverflowException)
		{
			return false;
		}
	}

	public static DateTime? ToDate(object? value) => value switch
	{
		DateTime dt => dt,
		DateTimeOffset dto => dto.DateTime,
		DateOnly d => d.ToDateTime(TimeOnly.MinValue),
		_ => null
	};

	// A picked day, keeping the time of day and the offset the old value had.
	public static object? FromDate(DateTime? day, Type type, object? old)
	{
		if (day is not { } picked)
		{
			return null;
		}

		Type plain = Plain(type);
		if (plain == typeof(DateOnly))
		{
			return DateOnly.FromDateTime(picked);
		}

		if (plain == typeof(DateTimeOffset))
		{
			DateTimeOffset before = old as DateTimeOffset? ?? new DateTimeOffset(picked.Date, TimeZoneInfo.Local.GetUtcOffset(picked));
			return new DateTimeOffset(picked.Date + before.TimeOfDay, before.Offset);
		}

		TimeSpan time = old is DateTime oldTime ? oldTime.TimeOfDay : TimeSpan.Zero;
		DateTimeKind kind = old is DateTime oldKind ? oldKind.Kind : DateTimeKind.Unspecified;
		return DateTime.SpecifyKind(picked.Date + time, kind);
	}

	// Text, as a pasted cell or a text field holds it, in the type the column holds: the current culture first, then the
	// invariant one, which is how a value copied from another program or this table's own export reads.
	public static bool TryParse(string text, Type type, out object? value, CultureInfo? only = null)
	{
		value = null;
		CultureInfo first = only ?? CultureInfo.CurrentCulture;
		CultureInfo second = only ?? CultureInfo.InvariantCulture;
		string trimmed = text.Trim();
		Type plain = Plain(type);
		if (plain == typeof(string) || plain == typeof(object))
		{
			value = text;
			return true;
		}

		if (trimmed.Length == 0)
		{
			return Nullable.GetUnderlyingType(type) is not null || !type.IsValueType;
		}

		if (IsNumber(plain))
		{
			if (double.TryParse(trimmed, NumberStyles.Float | NumberStyles.AllowThousands, first, out double n)
			    || double.TryParse(trimmed, NumberStyles.Float | NumberStyles.AllowThousands, second, out n))
			{
				return TryFromDouble(n, plain, out value);
			}

			return false;
		}

		if (plain == typeof(bool))
		{
			if (bool.TryParse(trimmed, out bool b))
			{
				value = b;
				return true;
			}

			value = trimmed switch { "1" => true, "0" => false, _ => null };
			return value is not null;
		}

		if (plain == typeof(DateTimeOffset))
		{
			if (DateTimeOffset.TryParse(trimmed, first, DateTimeStyles.AssumeLocal, out DateTimeOffset dto)
			    || DateTimeOffset.TryParse(trimmed, second, DateTimeStyles.AssumeLocal, out dto))
			{
				value = dto;
				return true;
			}

			return false;
		}

		if (plain == typeof(DateTime))
		{
			if (DateTime.TryParse(trimmed, first, DateTimeStyles.None, out DateTime dt)
			    || DateTime.TryParse(trimmed, second, DateTimeStyles.None, out dt))
			{
				value = dt;
				return true;
			}

			return false;
		}

		if (plain == typeof(DateOnly))
		{
			if (DateOnly.TryParse(trimmed, first, out DateOnly d)
			    || DateOnly.TryParse(trimmed, second, out d))
			{
				value = d;
				return true;
			}

			return false;
		}

		if (plain == typeof(TimeOnly))
		{
			if (TimeOnly.TryParse(trimmed, first, out TimeOnly t)
			    || TimeOnly.TryParse(trimmed, second, out t))
			{
				value = t;
				return true;
			}

			return false;
		}

		if (plain == typeof(Guid))
		{
			bool ok = Guid.TryParse(trimmed, out Guid g);
			value = ok ? g : null;
			return ok;
		}

		if (plain.IsEnum)
		{
			bool ok = Enum.TryParse(plain, trimmed, ignoreCase: true, out object? parsed) && Enum.IsDefined(plain, parsed!);
			value = ok ? parsed : null;
			return ok;
		}

		return false;
	}

	// How a list editor's choice writes a value: an enum's name, a number or a date in the invariant culture.
	public static string ToInvariantText(object value) => value switch
	{
		string s => s,
		DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
		DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
		IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
		_ => value.ToString() ?? string.Empty
	};

	// A list editor's choice, written by ToInvariantText, read back.
	public static bool TryParseInvariant(string text, Type type, out object? value) =>
		TryParse(text, type, out value, CultureInfo.InvariantCulture);

	// What a text field shows for a value: the value written in the current culture, as people type it back.
	public static string ToText(object? value) => value switch
	{
		null => string.Empty,
		string s => s,
		IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture),
		_ => value.ToString() ?? string.Empty
	};
}
