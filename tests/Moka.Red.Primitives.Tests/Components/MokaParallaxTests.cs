using Bunit;
using Moka.Red.Primitives.Parallax;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// BackgroundImage went into url('...') as it was, so a quote and a parenthesis ended the url() and
// whatever followed became declarations of the background layer.
public class MokaParallaxTests : BunitContext
{
	private static string BackgroundStyle(IRenderedComponent<MokaParallax> cut) =>
		cut.Find(".moka-parallax-background").GetAttribute("style") ?? "";

	[Theory]
	[InlineData("x.png'); background-color: red; content: url('y")]
	[InlineData("x.png\"); background-color: red; content: url(\"y")]
	[InlineData("x.png\\'); background-color: red")]
	[InlineData("x.png\n); background-color: red")]
	[InlineData("x.png'); position: fixed; inset: 0; z-index: 9999; background: url('https://example.com/overlay.png")]
	public void AHostileImageUrl_StaysInsideTheUrl(string src)
	{
		IRenderedComponent<MokaParallax> cut = Render<MokaParallax>(p => p.Add(x => x.BackgroundImage, src));

		Assert.Equal(["background-image"], CssDeclarations.PropertyNames(BackgroundStyle(cut)));
	}

	[Fact]
	public void AnImageUrl_IsTheBackground()
	{
		IRenderedComponent<MokaParallax> cut = Render<MokaParallax>(p => p
			.Add(x => x.BackgroundImage, "https://example.com/hero.jpg?w=1200&h=600"));

		Assert.Equal("background-image: url(\"https://example.com/hero.jpg?w=1200&h=600\")", BackgroundStyle(cut));
	}

	[Fact]
	public void BackgroundContent_TakesPriorityOverTheImage()
	{
		IRenderedComponent<MokaParallax> cut = Render<MokaParallax>(p => p
			.Add(x => x.BackgroundImage, "https://example.com/hero.jpg")
			.Add(x => x.BackgroundContent, "<div class=\"layer\"></div>"));

		Assert.Null(cut.Find(".moka-parallax-background").GetAttribute("style"));
	}
}
