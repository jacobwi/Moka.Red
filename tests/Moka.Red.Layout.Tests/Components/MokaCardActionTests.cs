using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Layout.Card;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// A clickable card was a role="button" div, so a switch or a link inside it sat inside a button. The card is now a
// plain element whose one action, the title or a hidden button or link, stretches its ::after over the card.
public partial class MokaCardActionTests : BunitContext
{
	private const string Stylesheet = "MokaCard.razor.css";

	private static RenderFragment Button(string css, Action onClick) => builder =>
	{
		builder.OpenElement(0, "button");
		builder.AddAttribute(1, "type", "button");
		builder.AddAttribute(2, "class", css);
		builder.AddAttribute(3, "onclick", EventCallback.Factory.Create<MouseEventArgs>(new object(), onClick));
		builder.AddContent(4, "Edit");
		builder.CloseElement();
	};

	[Fact]
	public void TheTitle_IsTheClickableCardsButton()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "Backups")
			.Add(x => x.Subtitle, "Nightly")
			.Add(x => x.Clickable, true));

		IElement action = cut.Find(".moka-card-title > button.moka-card-action");
		Assert.Equal("button", action.GetAttribute("type"));
		Assert.Equal("Backups", action.TextContent);
		Assert.DoesNotContain("moka-card-action--hidden", action.ClassList);
		Assert.Equal("Nightly", cut.Find(".moka-card-subtitle").TextContent);
	}

	[Fact]
	public void TheTitle_IsTheLinkCardsLink()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "Backups")
			.Add(x => x.Href, "/backups"));

		IElement action = cut.Find(".moka-card-title > a.moka-card-action");
		Assert.Equal("/backups", action.GetAttribute("href"));
		Assert.Equal("Backups", action.TextContent);
		Assert.Empty(cut.FindAll("button"));
	}

	[Fact]
	public void HrefWins_OverClickable()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "Backups")
			.Add(x => x.Clickable, true)
			.Add(x => x.Href, "/backups"));

		Assert.Single(cut.FindAll("a.moka-card-action"));
		Assert.Empty(cut.FindAll("button"));
	}

	[Fact]
	public void WithoutATitle_AHiddenActionComesFirst_NamedByActionLabel()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Clickable, true)
			.Add(x => x.ActionLabel, "Open backups")
			.Add(x => x.Header, "<strong>Backups</strong>")
			.AddChildContent("Nightly at 02:00"));

		IElement card = cut.Find(".moka-card");
		IElement action = card.FirstElementChild!;
		Assert.True(action.Matches("button.moka-card-action.moka-card-action--hidden"));
		Assert.Equal("Open backups", action.QuerySelector(".moka-visually-hidden")!.TextContent);
		Assert.False(action.HasAttribute("aria-labelledby"));
	}

	[Fact]
	public void AHiddenLink_IsNamedTheSameWay()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Href, "/backups")
			.Add(x => x.ActionLabel, "Open backups")
			.AddChildContent("Nightly at 02:00"));

		IElement action = cut.Find(".moka-card").FirstElementChild!;
		Assert.True(action.Matches("a.moka-card-action.moka-card-action--hidden"));
		Assert.Equal("/backups", action.GetAttribute("href"));
		Assert.Equal("Open backups", action.TextContent);
	}

	// A custom Header hides the Title, which still names the hidden action.
	[Fact]
	public void UnderACustomHeader_TheTitleNamesTheHiddenAction()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Clickable, true)
			.Add(x => x.Title, "Backups")
			.Add(x => x.Header, "<strong>Custom</strong>"));

		Assert.Empty(cut.FindAll(".moka-card-title"));
		Assert.Equal("Backups", cut.Find(".moka-card-action--hidden .moka-visually-hidden").TextContent);
	}

	// The old role="button" card took its name from everything in it. With no title or label the hidden action
	// does the same, through the header and the body.
	[Fact]
	public void WithNothingToNameIt_TheHiddenActionReadsTheHeaderAndBody()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Clickable, true)
			.Add(x => x.Header, "<strong>Backups</strong>")
			.AddChildContent("Nightly at 02:00"));

		IElement action = cut.Find(".moka-card-action--hidden");
		string[] ids = action.GetAttribute("aria-labelledby")!.Split(' ');
		Assert.Equal([cut.Find(".moka-card-header-content").Id, cut.Find(".moka-card-body").Id], ids);
		Assert.Empty(action.QuerySelectorAll(".moka-visually-hidden"));
	}

	[Fact]
	public void ACardWithoutAnAction_GetsNoIds()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Header, "<strong>Backups</strong>")
			.AddChildContent("Nightly at 02:00"));

		Assert.Empty(cut.FindAll("[id]"));
		Assert.Empty(cut.FindAll(".moka-card-action"));
	}

	[Fact]
	public async Task AClickOnTheAction_RaisesOnClick()
	{
		int clicks = 0;
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Clickable, true)
			.Add(x => x.ActionLabel, "Open")
			.Add(x => x.OnClick, _ => clicks++)
			.AddChildContent("Body"));

		await cut.Find(".moka-card-action").ClickAsync(new MouseEventArgs());

		Assert.Equal(1, clicks);
	}

	// A control inside the card is not inside the action, so its click is its own and never the card's.
	[Fact]
	public async Task AControlInTheBody_IsNotInsideTheAction_AndDoesNotClickTheCard()
	{
		int cardClicks = 0;
		int editClicks = 0;
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "Backups")
			.Add(x => x.Clickable, true)
			.Add(x => x.OnClick, _ => cardClicks++)
			.Add(x => x.ChildContent, Button("edit", () => editClicks++)));

		IElement edit = cut.Find("button.edit");
		Assert.Null(edit.Closest(".moka-card-action"));
		Assert.Null(edit.Closest("[role=button]"));

		await edit.ClickAsync(new MouseEventArgs());

		Assert.Equal(1, editClicks);
		Assert.Equal(0, cardClicks);
	}

	[Fact]
	public void LinkAttributesAndTheName_GoOnTheAction_TheRestOnTheCard()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Href, "/backups")
			.Add(x => x.Title, "Backups")
			.AddUnmatched("target", "_blank")
			.AddUnmatched("rel", "noopener")
			.AddUnmatched("aria-label", "Open backups")
			.AddUnmatched("data-test", "card"));

		IElement card = cut.Find(".moka-card");
		IElement action = cut.Find(".moka-card-action");
		Assert.Equal("_blank", action.GetAttribute("target"));
		Assert.Equal("noopener", action.GetAttribute("rel"));
		Assert.Equal("Open backups", action.GetAttribute("aria-label"));
		Assert.Equal("card", card.GetAttribute("data-test"));
		Assert.False(card.HasAttribute("target"));
		Assert.False(card.HasAttribute("aria-label"));
	}

	[Fact]
	public void ACardWithoutAnAction_KeepsEveryAttribute()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "Backups")
			.AddUnmatched("aria-label", "Backups card")
			.AddUnmatched("data-test", "card"));

		IElement card = cut.Find(".moka-card");
		Assert.Equal("Backups card", card.GetAttribute("aria-label"));
		Assert.Equal("card", card.GetAttribute("data-test"));
	}

	// A collapsible card's title is its toggle, and a button can't sit in another, so the action is hidden.
	[Fact]
	public async Task ClickableAndCollapsible_KeepTheTitleAsTheToggle_AndHideTheAction()
	{
		JSInterop.SetupModule("./_content/Moka.Red.Core/moka-keys.js").SetupVoid("bindActivation", _ => true).SetVoidResult();
		int clicks = 0;
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "Backups")
			.Add(x => x.Clickable, true)
			.Add(x => x.Collapsible, true)
			.Add(x => x.OnClick, _ => clicks++)
			.AddChildContent("Body"));

		IElement toggle = cut.Find(".moka-card-header [role=button]");
		Assert.Empty(toggle.QuerySelectorAll("button, a"));
		Assert.Equal("Backups", cut.Find(".moka-card-action--hidden .moka-visually-hidden").TextContent);

		await cut.Find(".moka-card-action").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-card-header").ClickAsync(new MouseEventArgs());

		Assert.Equal(1, clicks);
		Assert.Empty(cut.FindAll(".moka-card-body"));
	}

	[Fact]
	public void TheAction_StretchesItsAfterOverTheCard()
	{
		Assert.Equal("relative", ScopedCss.Declarations(Stylesheet, ".moka-card--clickable")["position"]);

		IReadOnlyDictionary<string, string> action = ScopedCss.Declarations(Stylesheet, ".moka-card-action");
		Assert.Equal("static", action["position"]);

		IReadOnlyDictionary<string, string> after = ScopedCss.Declarations(Stylesheet, ".moka-card-action::after");
		Assert.Equal("\"\"", after["content"]);
		Assert.Equal("absolute", after["position"]);
		Assert.Equal("0", after["inset"]);
		Assert.Equal("1", after["z-index"]);

		IReadOnlyDictionary<string, string> hidden = ScopedCss.Declarations(Stylesheet, ".moka-card-action--hidden");
		Assert.Equal("absolute", hidden["position"]);
		Assert.Equal("0", hidden["inset"]);
	}

	// Raised with :where, so its specificity is the scope attribute alone and a component's own position wins.
	[Fact]
	public void InteractiveThings_SitAboveTheStretchedAction()
	{
		Match rule = RaiseRule().Match(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Stylesheet)));
		Assert.True(rule.Success);

		string[] targets = rule.Groups["targets"].Value.Split(',', StringSplitOptions.TrimEntries);
		Assert.Equal(["a[href]", "button", "input", "select", "textarea", "label", "summary", "[tabindex]"], targets);
		Assert.Contains("position: relative;", rule.Groups["body"].Value, StringComparison.Ordinal);
		Assert.Contains("z-index: 2;", rule.Groups["body"].Value, StringComparison.Ordinal);

		Assert.Equal("2", ScopedCss.Declarations(Stylesheet, ".moka-card--clickable > .moka-card-header--collapsible")["z-index"]);
	}

	[Theory]
	[InlineData(".moka-card--clickable:has(> .moka-card-action:focus-visible)", "var(--moka-color-primary-border)")]
	[InlineData(".moka-card--clickable:has(> .moka-card-header > .moka-card-header-content > .moka-card-title > .moka-card-action:focus-visible)", "var(--moka-color-primary-border)")]
	[InlineData(".moka-gate .moka-card--clickable:has(> .moka-card-action:focus-visible)", "var(--moka-color-primary)")]
	public void TheCard_ShowsTheFocusRing_WhenItsActionHasKeyboardFocus(string selector, string border)
	{
		IReadOnlyDictionary<string, string> ring = ScopedCss.Declarations(Stylesheet, selector);

		Assert.Equal("var(--moka-focus-ring)", ring["box-shadow"]);
		Assert.Equal(border, ring["border-color"]);
		Assert.Equal("none", ScopedCss.Declarations(Stylesheet, ".moka-card-action:focus-visible")["outline"]);
	}

	[Fact]
	public void TheHoverLook_Stays()
	{
		IReadOnlyDictionary<string, string> hover = ScopedCss.Declarations(Stylesheet, ".moka-card--clickable:hover");

		Assert.Equal("var(--moka-color-primary-border)", hover["border-color"]);
		Assert.Equal("var(--moka-shadow-3)", hover["box-shadow"]);
	}

	[GeneratedRegex(@":where\(\.moka-card--clickable\) ::deep :where\((?<targets>[^)]*)\)\s*\{(?<body>[^}]*)\}")]
	private static partial Regex RaiseRule();
}
