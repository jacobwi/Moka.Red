using AngleSharp.Dom;
using Bunit;
using Moka.Red.Primitives.Typography;

namespace Moka.Red.Primitives.Tests.Components;

// A citation could open its source in another tab only through markup of the app's own, with no rel and nothing telling
// a screen reader where the link goes.
public sealed class MokaBlockquoteTargetTests : BunitContext
{
	private IRenderedComponent<MokaBlockquote> RenderQuote(string? href, string? target, string? newTabText = null) =>
		Render<MokaBlockquote>(p =>
		{
			p.Add(x => x.Citation, "Grace Hopper")
				.Add(x => x.CitationHref, href)
				.Add(x => x.CitationTarget, target)
				.AddChildContent("It is easier to ask forgiveness than it is to get permission.");
			if (newTabText is not null)
			{
				p.Add(x => x.NewTabText, newTabText);
			}
		});

	[Fact]
	public void ACitationForANewTab_HasRel_AndSaysWhereItOpens()
	{
		IElement link = RenderQuote("https://example.com/hopper", "_blank").Find("cite > a");

		Assert.Equal("https://example.com/hopper", link.GetAttribute("href"));
		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
		Assert.Equal("Grace Hopper, opens in a new tab", link.TextContent);
		Assert.Equal(", opens in a new tab", link.QuerySelector(".moka-visually-hidden")!.TextContent);
	}

	[Fact]
	public void NewTabText_ReplacesTheWords() =>
		Assert.Equal("Grace Hopper, öppnas i en ny flik",
			RenderQuote("https://example.com/hopper", "quotes", "öppnas i en ny flik").Find("cite > a").TextContent);

	[Theory]
	[InlineData("_self")]
	[InlineData("_parent")]
	public void ATargetInThisTab_AddsNoRelOrWords(string target)
	{
		IElement link = RenderQuote("https://example.com/hopper", target).Find("cite > a");

		Assert.Equal(target, link.GetAttribute("target"));
		Assert.False(link.HasAttribute("rel"));
		Assert.Equal("Grace Hopper", link.TextContent);
	}

	[Fact]
	public void ACitationWithoutTarget_KeepsItsLinksMarkup()
	{
		IElement link = RenderQuote("https://example.com/hopper", null).Find("cite > a");

		Assert.Equal(["href"], link.Attributes.Select(a => a.Name)
			.Where(name => !name.StartsWith("b-", StringComparison.Ordinal)));
		Assert.Equal("Grace Hopper", link.InnerHtml);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("javascript:alert(1)")]
	public void ATargetWithoutALink_ShowsTheCitationAsText(string? href)
	{
		IRenderedComponent<MokaBlockquote> cut = RenderQuote(href, "_blank");

		Assert.Empty(cut.FindAll("a"));
		Assert.Empty(cut.FindAll(".moka-visually-hidden"));
		Assert.Equal("Grace Hopper", cut.Find("cite").TextContent.Trim());
	}
}
