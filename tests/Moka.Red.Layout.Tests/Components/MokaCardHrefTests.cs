using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Layout.Card;

namespace Moka.Red.Layout.Tests.Components;

// Href went into the card's anchor as it was, so a javascript: URL ran script when the card was clicked.
public class MokaCardHrefTests : BunitContext
{
	[Theory]
	[InlineData("javascript:alert(document.domain)")]
	[InlineData(" JavaScript:alert(1)")]
	[InlineData("jav\nascript:alert(1)")]
	[InlineData("data:text/html,<script>alert(1)</script>")]
	public void AScriptUrl_LeavesAPlainCard(string href)
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Href, href)
			.Add(x => x.Title, "Q3 report"));

		Assert.Empty(cut.FindAll("a"));
		Assert.Empty(cut.FindAll("[href]"));
		Assert.DoesNotContain("moka-card--clickable", cut.Find(".moka-card").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void AScriptUrl_OnAClickableCard_LeavesAButton()
	{
		JSInterop.SetupModule("./_content/Moka.Red.Core/moka-keys.js").SetupVoid("bindActivation", _ => true);
		int clicks = 0;
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Href, "javascript:alert(1)")
			.Add(x => x.Clickable, true)
			.Add(x => x.OnClick, _ => clicks++));

		cut.Find(".moka-card").Click(new MouseEventArgs());

		Assert.Equal("button", cut.Find(".moka-card").GetAttribute("role"));
		Assert.Equal(1, clicks);
	}

	[Fact]
	public void AWebUrl_MakesTheCardALink()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Title, "Q3 report"));

		Assert.Equal("/reports/q3", cut.Find("a.moka-card").GetAttribute("href"));
	}
}
