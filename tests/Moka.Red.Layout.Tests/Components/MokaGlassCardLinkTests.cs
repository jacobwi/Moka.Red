using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Layout.GlassCard;

namespace Moka.Red.Layout.Tests.Components;

// A glass card could only be a button, so a card that leads to a page had no link to open in another tab, copy or
// middle-click. With Href it works as MokaCard does: one link stretched over the card.
public sealed class MokaGlassCardLinkTests : BunitContext
{
	public MokaGlassCardLinkTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static IEnumerable<string> OwnAttributes(IElement element) => element.Attributes.Select(a => a.Name)
		.Where(name => !name.StartsWith("b-", StringComparison.Ordinal) && !name.StartsWith("blazor:", StringComparison.Ordinal));

	[Fact]
	public void Href_MakesTheTitleTheCardsLink()
	{
		IRenderedComponent<MokaGlassCard> cut = Render<MokaGlassCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Title, "Q3 report")
			.AddChildContent("<p>Revenue</p>"));

		IElement card = cut.Find(".moka-glass-card");
		IElement link = cut.Find(".moka-glass-card__title > a.moka-glass-card__action");
		Assert.Equal("/reports/q3", link.GetAttribute("href"));
		Assert.Equal(["class", "href"], OwnAttributes(link));
		Assert.Equal("Q3 report", link.InnerHtml);
		Assert.Contains("moka-glass-card--link", card.ClassList);
		Assert.Contains("moka-glass-card--clickable", card.ClassList);
		Assert.False(card.HasAttribute("role"));
		Assert.False(card.HasAttribute("tabindex"));
		Assert.False(card.HasAttribute("blazor:onclick"));
		Assert.Single(cut.FindAll("a"));
	}

	[Fact]
	public void AClickableCardWithHref_IsALinkNotAButton()
	{
		IRenderedComponent<MokaGlassCard> cut = Render<MokaGlassCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Clickable, true)
			.Add(x => x.OnClick, () => { })
			.Add(x => x.Title, "Q3 report"));

		IElement card = cut.Find(".moka-glass-card");
		Assert.False(card.HasAttribute("role"));
		Assert.False(card.HasAttribute("tabindex"));
		Assert.False(card.HasAttribute("blazor:onclick"));
		Assert.NotNull(cut.Find("a.moka-glass-card__action"));
		Assert.Empty(JSInterop.Invocations);
	}

	[Fact]
	public async Task AClickOnTheLink_RaisesOnClick()
	{
		int clicks = 0;
		IRenderedComponent<MokaGlassCard> cut = Render<MokaGlassCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Title, "Q3 report")
			.Add(x => x.OnClick, () => clicks++));

		await cut.Find("a.moka-glass-card__action").ClickAsync(new MouseEventArgs());

		Assert.Equal(1, clicks);
	}

	[Fact]
	public void ALinkWithNothingListening_HasNoClickListener() =>
		Assert.False(Render<MokaGlassCard>(p => p
				.Add(x => x.Href, "/reports/q3")
				.Add(x => x.Title, "Q3 report"))
			.Find("a.moka-glass-card__action").HasAttribute("blazor:onclick"));

	[Fact]
	public void ALinkForANewTab_HasRel_AndSaysSoInItsName()
	{
		IElement link = Render<MokaGlassCard>(p => p
			.Add(x => x.Href, "https://status.example.com")
			.Add(x => x.Target, "_blank")
			.Add(x => x.Title, "Status page")).Find("a.moka-glass-card__action");

		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
		Assert.False(link.HasAttribute("aria-labelledby"));
		Assert.Equal("Status page, opens in a new tab", link.TextContent);
	}

	[Fact]
	public void NewTabText_ReplacesTheWords() =>
		Assert.Equal("Status page, öppnas i en ny flik", Render<MokaGlassCard>(p => p
			.Add(x => x.Href, "https://status.example.com")
			.Add(x => x.Target, "_blank")
			.Add(x => x.NewTabText, "öppnas i en ny flik")
			.Add(x => x.Title, "Status page")).Find("a.moka-glass-card__action").TextContent);

	[Theory]
	[InlineData("_self")]
	[InlineData("_top")]
	public void ATargetInThisTab_AddsNoRelOrWords(string target)
	{
		IElement link = Render<MokaGlassCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Target, target)
			.Add(x => x.Title, "Q3 report")).Find("a.moka-glass-card__action");

		Assert.Equal(target, link.GetAttribute("target"));
		Assert.False(link.HasAttribute("rel"));
		Assert.Equal("Q3 report", link.TextContent);
	}

	[Fact]
	public void ACustomHeader_GetsAHiddenLinkNamedByActionLabel()
	{
		IRenderedComponent<MokaGlassCard> cut = Render<MokaGlassCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Target, "_blank")
			.Add(x => x.ActionLabel, "Q3 report")
			.Add(x => x.Header, "<strong>Quarter three</strong>")
			.AddChildContent("<p>Revenue</p>"));

		IElement link = cut.Find(".moka-glass-card > a.moka-glass-card__action--hidden");
		Assert.Equal(["Q3 report", ", opens in a new tab"],
			link.QuerySelectorAll(".moka-visually-hidden").Select(span => span.TextContent));
		Assert.False(link.HasAttribute("aria-labelledby"));
		Assert.False(cut.Find(".moka-glass-card__header").HasAttribute("id"));
	}

	[Fact]
	public void AHiddenLinkWithNoName_IsNamedByTheHeaderAndBody()
	{
		IRenderedComponent<MokaGlassCard> cut = Render<MokaGlassCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Target, "report-window")
			.Add(x => x.Header, "<strong>Q3 report</strong>")
			.AddChildContent("<p>Revenue</p>"));

		IElement link = cut.Find("a.moka-glass-card__action--hidden");
		string[] ids = link.GetAttribute("aria-labelledby")!.Split(' ');
		Assert.Equal(3, ids.Length);
		Assert.Equal("Q3 report", cut.Find($"#{ids[0]}").TextContent);
		Assert.Equal("Revenue", cut.Find($"#{ids[1]}").TextContent.Trim());
		Assert.Equal(", opens in a new tab", link.QuerySelector($"#{ids[2]}")?.TextContent);
	}

	[Fact]
	public void AnAriaLabel_NamesTheLink_AndEndsInTheWords()
	{
		IRenderedComponent<MokaGlassCard> cut = Render<MokaGlassCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Target, "_blank")
			.AddUnmatched("aria-label", "Open the Q3 report")
			.AddUnmatched("data-section", "reports")
			.AddChildContent("<p>Revenue</p>"));

		IElement card = cut.Find(".moka-glass-card");
		IElement link = cut.Find("a.moka-glass-card__action");
		Assert.Equal("Open the Q3 report, opens in a new tab", link.GetAttribute("aria-label"));
		Assert.False(link.HasAttribute("aria-labelledby"));
		Assert.False(card.HasAttribute("aria-label"));
		Assert.Equal("reports", card.GetAttribute("data-section"));
	}

	[Fact]
	public void TheAppsOwnRel_GoesOnTheLink_AndWins()
	{
		IRenderedComponent<MokaGlassCard> cut = Render<MokaGlassCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Target, "_blank")
			.AddUnmatched("rel", "noopener")
			.Add(x => x.Title, "Q3 report"));

		Assert.Equal("noopener", cut.Find("a.moka-glass-card__action").GetAttribute("rel"));
		Assert.False(cut.Find(".moka-glass-card").HasAttribute("rel"));
	}

	[Fact]
	public void AScriptUrl_LeavesAClickableCardAButton()
	{
		IRenderedComponent<MokaGlassCard> cut = Render<MokaGlassCard>(p => p
			.Add(x => x.Href, "javascript:alert(1)")
			.Add(x => x.Target, "_blank")
			.Add(x => x.Clickable, true)
			.Add(x => x.OnClick, () => { })
			.Add(x => x.Title, "Q3 report"));

		IElement card = cut.Find(".moka-glass-card");
		Assert.Empty(cut.FindAll("a"));
		Assert.Equal("button", card.GetAttribute("role"));
		Assert.DoesNotContain("moka-glass-card--link", card.ClassList);
		Assert.Equal("Q3 report", cut.Find(".moka-glass-card__title").InnerHtml);
	}

	[Fact]
	public void ACardWithoutHref_KeepsItsMarkup()
	{
		IRenderedComponent<MokaGlassCard> cut = Render<MokaGlassCard>(p => p
			.Add(x => x.Title, "Q3 report")
			.Add(x => x.Target, "_blank")
			.AddUnmatched("aria-label", "Quarter three")
			.AddChildContent("<p>Revenue</p>"));

		IElement card = cut.Find(".moka-glass-card");
		Assert.Equal("Quarter three", card.GetAttribute("aria-label"));
		Assert.Equal(["class", "style", "aria-label"], OwnAttributes(card));
		Assert.Equal("Q3 report", cut.Find(".moka-glass-card__title").InnerHtml);
		Assert.False(cut.Find(".moka-glass-card__header").HasAttribute("id"));
		Assert.False(cut.Find(".moka-glass-card__body").HasAttribute("id"));
		Assert.Empty(cut.FindAll("a"));
		Assert.Empty(cut.FindAll(".moka-visually-hidden"));
	}

	// A card nobody can click took a click listener anyway, a round trip for every click on it.
	[Fact]
	public void AStaticCard_HasNoClickListener() =>
		Assert.False(Render<MokaGlassCard>(p => p.Add(x => x.Title, "Q3 report"))
			.Find(".moka-glass-card").HasAttribute("blazor:onclick"));
}
