using System.Globalization;
using System.Numerics;

namespace Moka.Red.Forms.NumericField;

// Reads, writes and steps a number field's value in the invariant culture, for every number type of the base library and
// the nullable form of each. The operations are picked once per type by name rather than built by reflection, so a
// trimmed or ahead-of-time compiled app keeps them, and each one is the type's own generic math.
internal static class MokaNumberText<TValue>
{
	private delegate bool Parser(string text, out TValue value);

	private delegate TValue Stepper(TValue value, TValue step, bool up);

	private sealed record Operations(Parser Parse, Stepper Step, TValue Zero);

	private static readonly Type Number = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

	private static readonly Operations? Ops = OperationsFor(Number);

	/// <summary>Whether the type is one the field reads.</summary>
	public static bool IsSupported => Ops is not null;

	/// <summary>Whether the type holds fractions, so the box needs a keypad with a decimal point.</summary>
	public static bool HasFraction { get; } =
		Number == typeof(double) || Number == typeof(float) || Number == typeof(decimal) || Number == typeof(Half);

	/// <summary>Zero in the field's type, which an empty box steps from.</summary>
	public static TValue Zero => Ops is null ? default! : Ops.Zero;

	public static string? Format(TValue? value) =>
		value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value?.ToString();

	public static bool TryParse(string text, out TValue value)
	{
		if (Ops is null)
		{
			value = default!;
			return false;
		}

		return Ops.Parse(text, out value);
	}

	/// <summary>The value one step up or down. A step past either end of the type leaves the value where it is.</summary>
	public static TValue Step(TValue value, TValue step, bool up) => Ops is null ? value : Ops.Step(value, step, up);

	private static Operations? OperationsFor(Type number) =>
		number == typeof(int) ? For<int>()
		: number == typeof(long) ? For<long>()
		: number == typeof(short) ? For<short>()
		: number == typeof(byte) ? For<byte>()
		: number == typeof(sbyte) ? For<sbyte>()
		: number == typeof(uint) ? For<uint>()
		: number == typeof(ulong) ? For<ulong>()
		: number == typeof(ushort) ? For<ushort>()
		: number == typeof(Int128) ? For<Int128>()
		: number == typeof(UInt128) ? For<UInt128>()
		: number == typeof(nint) ? For<nint>()
		: number == typeof(nuint) ? For<nuint>()
		: number == typeof(double) ? For<double>()
		: number == typeof(float) ? For<float>()
		: number == typeof(decimal) ? For<decimal>()
		: number == typeof(Half) ? For<Half>()
		: number == typeof(BigInteger) ? For<BigInteger>()
		: null;

	// TValue is TNumber or TNumber?, and a boxed TNumber unboxes as either.
	private static Operations For<TNumber>() where TNumber : INumber<TNumber> =>
		new(Read<TNumber>, StepBy<TNumber>, (TValue)(object)TNumber.Zero);

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

	// Integer types wrap around past their ends, which the comparison catches without a checked context or an exception.
	private static TValue StepBy<TNumber>(TValue value, TValue step, bool up) where TNumber : INumber<TNumber>
	{
		TNumber from = (TNumber)(object)value!;
		TNumber by = (TNumber)(object)step!;
		TNumber to = up ? from + by : from - by;
		bool wrapped = up ? to < from : to > from;
		return wrapped ? value : (TValue)(object)to;
	}
}
