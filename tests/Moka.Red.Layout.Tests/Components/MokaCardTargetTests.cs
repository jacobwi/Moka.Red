using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Layout.Card;

namespace Moka.Red.Layout.Tests.Components;

// A card could only open its link in another tab through a target attribute, with no rel and nothing telling a screen
// reader where the link goes.
public class MokaCardTargetTests : BunitContext
{
	public MokaCardTargetTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ATitleLinkForANewTab_SaysSoInItsName()
	{
		IElement link = Render<MokaCard>(p => p
			.Add(x => x.Href, "https://example.com/reports/q3")
			.Add(x => x.Target, "_blank")
			.Add(x => x.Title, "Q3 report")).Find(".moka-card-title > a.moka-card-action");

		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
		Assert.False(link.HasAttribute("aria-labelledby"));
		Assert.Equal("Q3 report, opens in a new tab", link.TextContent);
		Assert.Equal(", opens in a new tab", link.QuerySelector(".moka-visually-hidden")!.TextContent);
	}

	[Fact]
	public void AHiddenLinkNamedByItsLabel_SaysSoInsideIt()
	{
		IElement link = Render<MokaCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Target, "_blank")
			.Add(x => x.ActionLabel, "Q3 report")
			.AddChildContent("<p>Revenue</p>")).Find("a.moka-card-action--hidden");

		Assert.Equal(["Q3 report", ", opens in a new tab"],
			link.QuerySelectorAll(".moka-visually-hidden").Select(span => span.TextContent));
		Assert.False(link.HasAttribute("aria-labelledby"));
	}

	[Fact]
	public void AHiddenLinkNamedByTheCardsContent_AddsTheWordsToItsName()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Target, "report-window")
			.Add(x => x.Header, "<strong>Q3 report</strong>")
			.AddChildContent("<p>Revenue</p>"));

		IElement link = cut.Find("a.moka-card-action--hidden");
		string[] ids = link.GetAttribute("aria-labelledby")!.Split(' ');
		Assert.Equal(3, ids.Length);
		Assert.Equal("Q3 report", cut.Find($"#{ids[0]}").TextContent);
		Assert.Equal("Revenue", cut.Find($"#{ids[1]}").TextContent);
		Assert.Equal(", opens in a new tab", link.QuerySelector($"#{ids[2]}")?.TextContent);
	}

	[Fact]
	public void AnAriaLabel_EndsInTheWords()
	{
		IElement link = Render<MokaCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Target, "_blank")
			.AddUnmatched("aria-label", "Open the Q3 report")
			.AddChildContent("<p>Revenue</p>")).Find("a.moka-card-action");

		Assert.Equal("Open the Q3 report, opens in a new tab", link.GetAttribute("aria-label"));
	}

	[Fact]
	public void NewTabText_ReplacesTheWords()
	{
		IElement link = Render<MokaCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Target, "_blank")
			.Add(x => x.NewTabText, "öppnas i en ny flik")
			.Add(x => x.Title, "Q3 report")).Find("a.moka-card-action");

		Assert.Equal("Q3 report, öppnas i en ny flik", link.TextContent);
	}

	[Theory]
	[InlineData("_self")]
	[InlineData("_top")]
	public void ATargetInThisTab_AddsNoRelOrWords(string target)
	{
		IElement link = Render<MokaCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Target, target)
			.Add(x => x.Title, "Q3 report")).Find("a.moka-card-action");

		Assert.Equal(target, link.GetAttribute("target"));
		Assert.False(link.HasAttribute("rel"));
		Assert.Equal("Q3 report", link.TextContent);
	}

	// Parameters bind without regard to case, so a target attribute written in markup is Target.
	[Fact]
	public void ATargetAttribute_IsTarget()
	{
		IElement link = Render<MokaCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.AddUnmatched("target", "_blank")
			.Add(x => x.Title, "Q3 report")).Find("a.moka-card-action");

		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
		Assert.Equal("Q3 report, opens in a new tab", link.TextContent);
	}

	[Fact]
	public void TheAppsOwnRel_StillWins() =>
		Assert.Equal("noopener", Render<MokaCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Target, "_blank")
			.AddUnmatched("rel", "noopener")
			.Add(x => x.Title, "Q3 report")).Find("a.moka-card-action").GetAttribute("rel"));

	[Fact]
	public void ACardWithoutTarget_KeepsItsLinksMarkup()
	{
		IElement link = Render<MokaCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Title, "Q3 report")).Find("a.moka-card-action");

		Assert.Equal(["class", "href"], link.Attributes.Select(a => a.Name)
			.Where(name => !name.StartsWith("b-", StringComparison.Ordinal) && !name.StartsWith("blazor:", StringComparison.Ordinal)));
		Assert.Equal("Q3 report", link.InnerHtml);
	}

	[Fact]
	public void TargetOnAClickableCardWithoutHref_IsIgnored()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Clickable, true)
			.Add(x => x.Target, "_blank")
			.Add(x => x.Title, "Q3 report"));

		IElement button = cut.Find("button.moka-card-action");
		Assert.False(button.HasAttribute("target"));
		Assert.False(button.HasAttribute("rel"));
		Assert.Empty(cut.FindAll(".moka-visually-hidden"));
	}

	[Fact]
	public async Task AClickOnTheLink_StillRaisesOnClick()
	{
		int clicks = 0;
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Href, "/reports/q3")
			.Add(x => x.Target, "_blank")
			.Add(x => x.Title, "Q3 report")
			.Add(x => x.OnClick, _ => clicks++));

		await cut.Find("a.moka-card-action").ClickAsync(new MouseEventArgs());

		Assert.Equal(1, clicks);
	}
}
