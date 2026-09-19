using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Layout.Panel;

namespace Moka.Red.Layout.Tests.Components;

public class MokaPanelTests : BunitContext
{
	// The toggle was an unnamed icon button with no state, and a collapsed body stayed in the tab order.
	[Fact]
	public async Task Toggle_ReportsItsState_AndACollapsedBodyIsInert()
	{
		IRenderedComponent<MokaPanel> cut = Render<MokaPanel>(p => p
			.Add(x => x.Title, "Logs")
			.Add(x => x.Collapsible, true)
			.AddChildContent("<a href=\"#\">Link</a>"));

		IElement toggle = cut.Find(".moka-panel-toggle");
		Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
		Assert.Equal(cut.Find(".moka-panel-body").Id, toggle.GetAttribute("aria-controls"));
		Assert.Equal(cut.Find(".moka-panel-title").Id, toggle.GetAttribute("aria-labelledby"));
		Assert.False(cut.Find(".moka-panel-body").HasAttribute("inert"));

		await toggle.ClickAsync(new MouseEventArgs());

		Assert.Equal("false", cut.Find(".moka-panel-toggle").GetAttribute("aria-expanded"));
		Assert.True(cut.Find(".moka-panel-body").HasAttribute("inert"));
	}

	[Fact]
	public void AToggleWithoutATitle_NamesItself()
	{
		IRenderedComponent<MokaPanel> cut = Render<MokaPanel>(p => p
			.Add(x => x.Collapsible, true)
			.Add(x => x.Actions, "<span>Actions</span>"));

		IElement toggle = cut.Find(".moka-panel-toggle");
		Assert.False(toggle.HasAttribute("aria-labelledby"));
		Assert.Equal("Toggle panel", toggle.GetAttribute("aria-label"));
	}

	// The body was capped with an inline max-height of 1000px, which cut off taller content.
	[Fact]
	public void TheBody_HasNoHeightCap()
	{
		IRenderedComponent<MokaPanel> cut = Render<MokaPanel>(p => p
			.Add(x => x.Title, "Logs")
			.Add(x => x.Collapsible, true));

		Assert.False(cut.Find(".moka-panel-body").HasAttribute("style"));
	}
}
