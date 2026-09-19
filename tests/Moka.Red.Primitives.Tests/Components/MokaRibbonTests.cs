using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Ribbon;

namespace Moka.Red.Primitives.Tests.Components;

// The wrapper rendered the raw Style parameter, so the Margin, Padding and Rounded parameters it
// inherits never reached the page.
public class MokaRibbonTests : BunitContext
{
	private static IElement Wrapper(IRenderedComponent<MokaRibbon> cut) => cut.Find(".moka-ribbon-wrapper");

	[Fact]
	public void Rounded_RoundsTheWrapper()
	{
		IRenderedComponent<MokaRibbon> cut = Render<MokaRibbon>(p => p
			.Add(x => x.Text, "New")
			.Add(x => x.Rounded, MokaRounding.Lg));

		Assert.Contains("border-radius: var(--moka-radius-lg)", Wrapper(cut).GetAttribute("style"),
			StringComparison.Ordinal);
	}

	[Fact]
	public void MarginAndPadding_ReachTheWrapper()
	{
		IRenderedComponent<MokaRibbon> cut = Render<MokaRibbon>(p => p
			.Add(x => x.Text, "New")
			.Add(x => x.Margin, MokaSpacingScale.Md)
			.Add(x => x.PaddingValue, "4px"));

		string? style = Wrapper(cut).GetAttribute("style");
		Assert.Contains("margin: var(--moka-spacing-md)", style, StringComparison.Ordinal);
		Assert.Contains("padding: 4px", style, StringComparison.Ordinal);
	}

	[Fact]
	public void StyleAndClass_StillApply()
	{
		IRenderedComponent<MokaRibbon> cut = Render<MokaRibbon>(p => p
			.Add(x => x.Text, "New")
			.Add(x => x.Rounded, MokaRounding.Md)
			.Add(x => x.Style, "width:280px")
			.Add(x => x.Class, "promo"));

		IElement wrapper = Wrapper(cut);
		Assert.EndsWith("width:280px", wrapper.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("moka-ribbon-wrapper promo", wrapper.ClassName);
	}
}
