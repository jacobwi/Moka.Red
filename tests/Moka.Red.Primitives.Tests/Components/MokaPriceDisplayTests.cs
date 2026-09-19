using Bunit;
using Moka.Red.Primitives.Price;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaPriceDisplayTests : BunitContext
{
	// A negative count made the format string "F-1", which .NET prints as literal text: "$F-1".
	[Fact]
	public void ANegativeDecimalCount_ShowsWholeAmounts()
	{
		IRenderedComponent<MokaPriceDisplay> cut = Render<MokaPriceDisplay>(p => p
			.Add(x => x.Price, 12.4m)
			.Add(x => x.OriginalPrice, 20m)
			.Add(x => x.DecimalPlaces, -1));

		Assert.Equal("$12", cut.Find(".moka-price-current").TextContent);
		Assert.Equal("$20", cut.Find(".moka-price-original").TextContent);
	}

	// Math.Round's default is banker's rounding, which took 12.5% down to 12%.
	[Theory]
	[InlineData(87.5, "-13%")]
	[InlineData(85.5, "-15%")]
	[InlineData(60, "-40%")]
	public void TheDiscount_RoundsHalfPercentsUp(double price, string expected)
	{
		IRenderedComponent<MokaPriceDisplay> cut = Render<MokaPriceDisplay>(p => p
			.Add(x => x.Price, (decimal)price)
			.Add(x => x.OriginalPrice, 100m));

		Assert.Equal(expected, cut.Find(".moka-price-discount").TextContent);
	}

	[Fact]
	public void APriceAboveTheOriginal_ShowsNoBadge()
	{
		IRenderedComponent<MokaPriceDisplay> cut = Render<MokaPriceDisplay>(p => p
			.Add(x => x.Price, 120m)
			.Add(x => x.OriginalPrice, 100m));

		Assert.Empty(cut.FindAll(".moka-price-discount"));
		Assert.Single(cut.FindAll(".moka-price-original"));
	}
}
