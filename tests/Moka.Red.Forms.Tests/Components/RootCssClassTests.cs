using Bunit;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.CreditCard;
using Moka.Red.Forms.SearchInput;
using Moka.Red.Forms.Slider;

namespace Moka.Red.Forms.Tests.Components;

// These inputs built their root class in a private property and rendered that, so the CssClass
// every component overrides was ignored: a derived component could not add to it. Each test renders
// a subclass that adds a class through CssClass and checks the whole list, the consumer's Class last
// before it.
public class RootCssClassTests : BunitContext
{
	[Fact]
	public void SearchInput_RendersItsCssClass()
	{
		IRenderedComponent<TaggedSearchInput> cut = Render<TaggedSearchInput>(p => p
			.Add(x => x.Value, "query")
			.Add(x => x.Loading, true)
			.Add(x => x.Class, "mine"));

		Assert.Equal("moka-search moka-search--loading moka-search--has-value mine tagged",
			cut.Find(".moka-search").GetAttribute("class"));
	}

	[Fact]
	public void CreditCardInput_RendersItsCssClass()
	{
		IRenderedComponent<TaggedCreditCardInput> cut = Render<TaggedCreditCardInput>(p => p.Add(x => x.Class, "mine"));

		Assert.Equal("moka-creditcard mine tagged", cut.Find(".moka-creditcard").GetAttribute("class"));
	}

	[Fact]
	public void Slider_RendersItsCssClassAndStyle()
	{
		IRenderedComponent<TaggedSlider> cut = Render<TaggedSlider>(p => p
			.Add(x => x.Vertical, true)
			.Add(x => x.ShowTicks, true)
			.Add(x => x.Class, "mine")
			.Add(x => x.Style, "width: 10rem"));

		Assert.Equal("moka-slider moka-slider--vertical moka-slider--show-ticks mine tagged",
			cut.Find(".moka-slider").GetAttribute("class"));
		Assert.Equal("width: 10rem", cut.Find(".moka-slider").GetAttribute("style"));
	}

	[Fact]
	public void RangeSlider_RendersItsCssClass()
	{
		IRenderedComponent<TaggedRangeSlider> cut = Render<TaggedRangeSlider>(p => p
			.Add(x => x.Disabled, true)
			.Add(x => x.Class, "mine"));

		Assert.Equal("moka-range-slider moka-range-slider--disabled mine tagged",
			cut.Find(".moka-range-slider").GetAttribute("class"));
	}

	private static string Tag(string cssClass) => new CssBuilder(cssClass).AddClass("tagged").Build();

	private sealed class TaggedSearchInput : MokaSearchInput
	{
		protected override string CssClass => Tag(base.CssClass);
	}

	private sealed class TaggedCreditCardInput : MokaCreditCardInput
	{
		protected override string CssClass => Tag(base.CssClass);
	}

	private sealed class TaggedSlider : MokaSlider
	{
		protected override string CssClass => Tag(base.CssClass);
	}

	private sealed class TaggedRangeSlider : MokaRangeSlider
	{
		protected override string CssClass => Tag(base.CssClass);
	}
}
