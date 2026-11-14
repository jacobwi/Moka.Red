using System.Globalization;
using System.Numerics;

namespace Moka.Red.Forms.NumericField;

// Reads and writes a number field's value in the invariant culture, for every number type of the base library and the
// nullable form of each. The parsers are picked once per type by name rather than built by reflection, so a trimmed or
// ahead-of-time compiled app keeps them, and each one is the type's own generic math TryParse.
internal static class MokaNumberText<TValue>
{
	private delegate bool Parser(string text, out TValue value);

	private static readonly Type Number = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

	private static readonly Parser? Parse = ParserFor(Number);

	/// <summary>Whether the type is one the field reads.</summary>
	public static bool IsSupported => Parse is not null;

	public static string? Format(TValue? value) =>
		value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value?.ToString();

	public static bool TryParse(string text, out TValue value)
	{
		if (Parse is null)
		{
			value = default!;
			return false;
		}

		return Parse(text, out value);
	}

	private static Parser? ParserFor(Type number) =>
		number == typeof(int) ? Read<int>
		: number == typeof(long) ? Read<long>
		: number == typeof(short) ? Read<short>
		: number == typeof(byte) ? Read<byte>
		: number == typeof(sbyte) ? Read<sbyte>
		: number == typeof(uint) ? Read<uint>
		: number == typeof(ulong) ? Read<ulong>
		: number == typeof(ushort) ? Read<ushort>
		: number == typeof(Int128) ? Read<Int128>
		: number == typeof(UInt128) ? Read<UInt128>
		: number == typeof(nint) ? Read<nint>
		: number == typeof(nuint) ? Read<nuint>
		: number == typeof(double) ? Read<double>
		: number == typeof(float) ? Read<float>
		: number == typeof(decimal) ? Read<decimal>
		: number == typeof(Half) ? Read<Half>
		: number == typeof(BigInteger) ? Read<BigInteger>
		: null;

	// TValue is TNumber or TNumber?, and a boxed TNumber unboxes as either.
	private static bool Read<TNumber>(string text, out TValue value) where TNumber : INumber<TNumber>
	{
		if (TNumber.TryParse(text, CultureInfo.InvariantCulture, out TNumber? parsed))
		{
			value = (TValue)(object)parsed;
			return true;
		}

		value = default!;
		return false;
	}
}
