using Bunit;
using Moka.Red.Layout.Card;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

/// <summary>
///     The picture in a card's Media is the app's markup, with the app's scope attribute, so the rule that makes it fill
///     the card has to reach it through <c>::deep</c>. Scoped, it never matched, and the docs set the width inline.
/// </summary>
public class MokaCardMediaTests : BunitContext
{
	private const string Stylesheet = "MokaCard.razor.css";

	[Fact]
	public void APictureInMedia_FillsTheCardsWidth()
	{
		IReadOnlyDictionary<string, string> picture = ScopedCss.Declarations(Stylesheet, ".moka-card-media ::deep > img");

		Assert.Equal("100%", picture["width"]);
		Assert.Equal("auto", picture["height"]);
		Assert.Equal("block", picture["display"]);
		Assert.Empty(ScopedCss.Declarations(Stylesheet, ".moka-card-media img"));
	}

	[Fact]
	public void Media_HoldsTheAppsPictureDirectly()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Media, "<img src=\"/cover.png\" alt=\"Cover\">"));

		Assert.Equal("IMG", Assert.Single(cut.Find(".moka-card-media").Children).TagName);
	}
}
