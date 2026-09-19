using AngleSharp.Dom;
using Bunit;
using Moka.Red.Primitives.Button;
using Moka.Red.Primitives.List;
using Moka.Red.Primitives.Typography;

namespace Moka.Red.Primitives.Tests.Components;

// Every Href went into the anchor as it was, so a javascript: URL from user data ran script in the
// page when the link was followed.
public class LinkHrefTests : BunitContext
{
	public static TheoryData<string> ScriptUrls =>
	[
		"javascript:alert(document.domain)",
		"  JavaScript:alert(1)",
		"java\tscript:alert(1)",
		"vbscript:msgbox(1)",
		"data:text/html,<script>alert(1)</script>"
	];

	private static void AssertNoHref<TComponent>(IRenderedComponent<TComponent> cut)
		where TComponent : Microsoft.AspNetCore.Components.IComponent =>
		Assert.Empty(cut.FindAll("[href]"));

	[Theory]
	[MemberData(nameof(ScriptUrls))]
	public void MokaLink_DropsAScriptUrl(string href)
	{
		IRenderedComponent<MokaLink> cut = Render<MokaLink>(p => p
			.Add(x => x.Href, href)
			.AddChildContent("Docs"));

		IElement link = cut.Find("a.moka-link");
		Assert.False(link.HasAttribute("href"));
		Assert.Equal("Docs", link.TextContent.Trim());
	}

	[Fact]
	public void MokaLink_KeepsAWebUrl()
	{
		IRenderedComponent<MokaLink> cut = Render<MokaLink>(p => p.Add(x => x.Href, "https://example.com/docs"));

		Assert.Equal("https://example.com/docs", cut.Find("a").GetAttribute("href"));
	}

	[Theory]
	[MemberData(nameof(ScriptUrls))]
	public void MokaButton_WithAScriptUrl_IsAPlainButton(string href)
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p
			.Add(x => x.Href, href)
			.AddChildContent("Open"));

		Assert.Empty(cut.FindAll("a"));
		Assert.Equal("button", cut.Find("button").GetAttribute("type"));
		AssertNoHref(cut);
	}

	[Fact]
	public void MokaButton_KeepsARelativeUrl()
	{
		IRenderedComponent<MokaButton> cut = Render<MokaButton>(p => p.Add(x => x.Href, "/orders/10442"));

		Assert.Equal("/orders/10442", cut.Find("a.moka-btn").GetAttribute("href"));
	}

	[Theory]
	[MemberData(nameof(ScriptUrls))]
	public void MokaListItem_WithAScriptUrl_IsAPlainRow(string href)
	{
		IRenderedComponent<MokaListItem> cut = Render<MokaListItem>(p => p
			.Add(x => x.Href, href)
			.Add(x => x.Text, "Invoices"));

		Assert.Empty(cut.FindAll("a"));
		Assert.Equal("listitem", cut.Find(".moka-list-item").GetAttribute("role"));
		AssertNoHref(cut);
	}

	[Fact]
	public void MokaListItem_KeepsAMailtoUrl()
	{
		IRenderedComponent<MokaListItem> cut = Render<MokaListItem>(p => p
			.Add(x => x.Href, "mailto:team@example.com")
			.Add(x => x.Text, "Mail us"));

		Assert.Equal("mailto:team@example.com", cut.Find("a").GetAttribute("href"));
	}

	[Theory]
	[MemberData(nameof(ScriptUrls))]
	public void MokaBlockquote_ShowsACitationWithAScriptUrlAsText(string href)
	{
		IRenderedComponent<MokaBlockquote> cut = Render<MokaBlockquote>(p => p
			.Add(x => x.Citation, "Grace Hopper")
			.Add(x => x.CitationHref, href)
			.AddChildContent("It is easier to ask forgiveness than it is to get permission."));

		Assert.Empty(cut.FindAll("a"));
		Assert.Equal("Grace Hopper", cut.Find("cite").TextContent.Trim());
	}

	[Fact]
	public void MokaBlockquote_KeepsACitationLink()
	{
		IRenderedComponent<MokaBlockquote> cut = Render<MokaBlockquote>(p => p
			.Add(x => x.Citation, "Grace Hopper")
			.Add(x => x.CitationHref, "https://example.com/hopper"));

		Assert.Equal("https://example.com/hopper", cut.Find("cite a").GetAttribute("href"));
	}
}
