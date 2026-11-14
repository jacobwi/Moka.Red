using System.Numerics;
using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Forms.NumericField;

namespace Moka.Red.Forms.Tests.Components;

// A number field over a nullable type holds no value as well as numbers: an empty box is null, and null shows an empty
// box. It used to take only non-nullable types, so an optional number became 0 as soon as its box was emptied.
public class NumericFieldNullableTests : BunitContext
{
	public NumericFieldNullableTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void Null_ShowsAnEmptyBox()
	{
		IRenderedComponent<MokaNumericField<int?>> cut = Render<MokaNumericField<int?>>(p => p.Add(x => x.Value, null));

		Assert.Equal(string.Empty, cut.Find("input").GetAttribute("value") ?? string.Empty);
	}

	[Fact]
	public void EmptyingTheBox_ReportsNull()
	{
		double? reported = 4;
		IRenderedComponent<MokaNumericField<double?>> cut = Render<MokaNumericField<double?>>(p => p
			.Add(x => x.Value, 2.5)
			.Add(x => x.ValueChanged, value => reported = value));

		cut.Find("input").Input(string.Empty);

		Assert.Null(reported);
	}

	[Fact]
	public void ANumber_IsReadInTheUnderlyingType()
	{
		decimal? reported = null;
		IRenderedComponent<MokaNumericField<decimal?>> cut = Render<MokaNumericField<decimal?>>(p => p
			.Add(x => x.ValueChanged, value => reported = value));

		IElement input = cut.Find("input");
		input.Input("12.75");

		Assert.Equal(12.75m, reported);
		Assert.Equal("12.75", cut.Find("input").GetAttribute("value"));
	}

	// A type that can't be empty keeps what it always did: an empty box is zero.
	[Fact]
	public void ATypeThatCantBeEmpty_StillTakesZeroForAnEmptyBox()
	{
		int reported = 7;
		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>(p => p
			.Add(x => x.Value, 3)
			.Add(x => x.ValueChanged, value => reported = value));

		cut.Find("input").Input(string.Empty);

		Assert.Equal(0, reported);
	}

	[Fact]
	public void EveryNumberTypeOfTheBaseLibrary_IsRead()
	{
		Assert.Equal(BigInteger.Parse("123456789012345678901234567890", System.Globalization.CultureInfo.InvariantCulture),
			ReadAs<BigInteger?>("123456789012345678901234567890"));
		Assert.Equal((Half)1.5, ReadAs<Half>("1.5"));
		Assert.Equal((byte)200, ReadAs<byte?>("200"));
		Assert.Equal(Int128.MaxValue, ReadAs<Int128>(Int128.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture)));
	}

	[Fact]
	public void AnotherType_IsRefused_WhenTheFieldIsMade()
	{
		Exception? error = Record.Exception(() => Render<MokaNumericField<string>>());

		Exception cause = error is TargetInvocationException { InnerException: { } inner } ? inner : error!;
		InvalidOperationException refused = Assert.IsType<InvalidOperationException>(cause);
		Assert.Contains("System.String", refused.Message, StringComparison.Ordinal);
	}

	private TValue? ReadAs<TValue>(string text)
	{
		TValue? reported = default;
		IRenderedComponent<MokaNumericField<TValue>> cut = Render<MokaNumericField<TValue>>(p => p
			.Add(x => x.ValueChanged, value => reported = value));
		cut.Find("input").Input(text);
		return reported;
	}
}
