using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Tests.Utilities;

public class MokaEnumHelpersTests
{
	[Theory]
	[InlineData(MokaBarcodeFormat.Code128, "code128")]
	[InlineData(MokaBarcodeFormat.Code39, "code39")]
	[InlineData(MokaBarcodeFormat.EAN13, "ean13")]
	[InlineData(MokaBarcodeFormat.EAN8, "ean8")]
	[InlineData(MokaBarcodeFormat.UPC, "upc")]
	public void ToCssClass_KeepsAcronymsIntact(MokaBarcodeFormat format, string expected)
	{
		// Hyphenating before every capital shattered acronyms: EAN13 became "e-a-n13".
		string result = MokaEnumHelpers.ToCssClass(format);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(MokaJustify.SpaceBetween, "space-between")]
	[InlineData(MokaJustify.SpaceAround, "space-around")]
	[InlineData(MokaJustify.Center, "center")]
	public void ToCssClass_SplitsOnWordBoundaries(MokaJustify justify, string expected)
	{
		string result = MokaEnumHelpers.ToCssClass(justify);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(MokaSize.Xs, "xs")]
	[InlineData(MokaSize.Sm, "sm")]
	[InlineData(MokaSize.Md, "md")]
	public void ToCssClass_LowercasesSingleWordValues(MokaSize size, string expected)
	{
		string result = MokaEnumHelpers.ToCssClass(size);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void ToCssClass_IsStableAcrossCalls()
	{
		// Results are memoised per (enum type, value); make sure the cache returns the same text.
		string first = MokaEnumHelpers.ToCssClass(MokaBarcodeFormat.EAN13);
		string second = MokaEnumHelpers.ToCssClass(MokaBarcodeFormat.EAN13);

		Assert.Equal(first, second);
	}
}
