using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Layout.Accordion;

namespace Moka.Red.Layout.Tests.Components;

public class MokaAccordionTests : BunitContext
{
	// The header reported no state, and a closed item's content stayed in the tab order.
	[Fact]
	public async Task Header_ReportsItsState_AndAClosedBodyIsInert()
	{
		IRenderedComponent<MokaAccordionItem> cut = Render<MokaAccordionItem>(p => p
			.Add(x => x.Title, "Shipping")
			.AddChildContent("<a href=\"#\">Rates</a>"));

		IElement header = cut.Find(".moka-accordion-item-header");
		IElement body = cut.Find(".moka-accordion-item-body");
		Assert.Equal("false", header.GetAttribute("aria-expanded"));
		Assert.Equal(body.Id, header.GetAttribute("aria-controls"));
		Assert.Equal(header.Id, body.GetAttribute("aria-labelledby"));
		Assert.True(body.HasAttribute("inert"));

		await header.ClickAsync(new MouseEventArgs());

		Assert.Equal("true", cut.Find(".moka-accordion-item-header").GetAttribute("aria-expanded"));
		Assert.False(cut.Find(".moka-accordion-item-body").HasAttribute("inert"));
	}

	// The body was capped with an inline max-height of 500px, which cut off taller content.
	[Fact]
	public void TheBody_HasNoHeightCap()
	{
		IRenderedComponent<MokaAccordionItem> cut = Render<MokaAccordionItem>(p => p
			.Add(x => x.Title, "Shipping")
			.Add(x => x.DefaultExpanded, true));

		Assert.False(cut.Find(".moka-accordion-item-body").HasAttribute("style"));
	}

	[Fact]
	public void IdStyleAndAttributes_ReachTheRoot()
	{
		IRenderedComponent<MokaAccordionItem> cut = Render<MokaAccordionItem>(p => p
			.Add(x => x.Title, "Shipping")
			.Add(x => x.Id, "shipping")
			.Add(x => x.Style, "margin-top: 4px")
			.AddUnmatched("data-section", "shipping"));

		IElement root = cut.Find(".moka-accordion-item");
		Assert.Equal("shipping", root.Id);
		Assert.Equal("margin-top: 4px", root.GetAttribute("style"));
		Assert.Equal("shipping", root.GetAttribute("data-section"));
	}
}
