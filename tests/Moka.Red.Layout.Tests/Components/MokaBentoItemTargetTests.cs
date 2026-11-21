using AngleSharp.Dom;
using Bunit;
using Moka.Red.Layout.BentoGrid;

namespace Moka.Red.Layout.Tests.Components;

// The head's Open link always opened in the board's own tab, so an app with a card for another site had no way to send
// it to a new tab with rel and words for screen readers.
public sealed class MokaBentoItemTargetTests : BunitContext
{
	public MokaBentoItemTargetTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule("./_content/Moka.Red.Core/moka-keys.js").Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule("./_content/Moka.Red.Layout/BentoGrid/MokaBentoGrid.razor.js").Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void AnOpenLinkForANewTab_SaysSo()
	{
		IElement open = Item("https://example.com/status", "_blank").Find("a.moka-bento-item__open");

		Assert.Equal("https://example.com/status", open.GetAttribute("href"));
		Assert.Equal("_blank", open.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", open.GetAttribute("rel"));
		Assert.Equal("Open, opens in a new tab", open.TextContent);
		Assert.Equal(", opens in a new tab", open.QuerySelector(".moka-visually-hidden")!.TextContent);
	}

	[Fact]
	public void NewTabText_ReplacesTheWords()
	{
		IElement open = Render<MokaBentoItem>(p => p
			.Add(x => x.Title, "Status")
			.Add(x => x.Href, "https://example.com/status")
			.Add(x => x.Target, "status")
			.Add(x => x.OpenText, "Öppna")
			.Add(x => x.NewTabText, "öppnas i en ny flik")).Find("a.moka-bento-item__open");

		Assert.Equal("Öppna, öppnas i en ny flik", open.TextContent);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("_self")]
	public void AnOpenLinkForThisTab_AddsNothing(string? target)
	{
		IElement open = Item("/status", target).Find("a.moka-bento-item__open");

		Assert.Equal(target, open.GetAttribute("target"));
		Assert.False(open.HasAttribute("rel"));
		Assert.Equal("Open", open.TextContent);
		Assert.Empty(open.Children);
	}

	[Fact]
	public void AnOpenLinkWithoutTarget_KeepsItsMarkup()
	{
		IElement open = Item("/status", null).Find("a.moka-bento-item__open");

		Assert.Equal(["class", "href"], open.Attributes.Select(a => a.Name).Where(name => !name.StartsWith("b-", StringComparison.Ordinal)));
		Assert.Equal("Open", open.InnerHtml);
	}

	[Fact]
	public void TargetWithoutHref_DrawsNoLink() =>
		Assert.Empty(Render<MokaBentoItem>(p => p
			.Add(x => x.Card, new MokaBoardCard("status"))
			.Add(x => x.Title, "Status")
			.Add(x => x.Target, "_blank")).FindAll("a"));

	[Fact]
	public void AScriptAddress_DrawsNoLink() =>
		Assert.Empty(Item("javascript:alert(1)", "_blank").FindAll("a"));

	private IRenderedComponent<MokaBentoItem> Item(string href, string? target) =>
		Render<MokaBentoItem>(p => p
			.Add(x => x.Card, new MokaBoardCard("status"))
			.Add(x => x.Title, "Status")
			.Add(x => x.Href, href)
			.Add(x => x.Target, target));
}
