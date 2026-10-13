using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Navigation.Tabs;
using Moka.Red.Navigation.Tabs.Models;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// The pin and close buttons and the tab's own tooltip were titles, so the browser's pale, late hint showed instead of
// Moka's, and the tab's title showed over the close button too.
public class MokaTabStripTooltipTests : BunitContext
{
	public MokaTabStripTooltipTests() =>
		JSInterop.SetupModule("./_content/Moka.Red.Navigation/moka-tabs.js").Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TheCloseButton_IsNamedByATooltipBelowTheStrip()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(new TabInfo<string> { Id = "a", Title = "Alpha" });

		IconTooltip.AssertNamed(Tab(cut).QuerySelector(".moka-tab-close")!, "Close", MokaTooltipPosition.Bottom);
	}

	[Theory]
	[InlineData(false, "Pin")]
	[InlineData(true, "Unpin")]
	public void ThePinButton_IsNamedByATooltipBelowTheStrip(bool pinned, string name)
	{
		IRenderedComponent<MokaTabStrip<string>> cut =
			RenderStrip(new TabInfo<string> { Id = "a", Title = "Alpha", IsPinned = pinned });

		IconTooltip.AssertNamed(Tab(cut).QuerySelector(".moka-tab-pin")!, name, MokaTooltipPosition.Bottom);
	}

	// The tab shows its title, so the tooltip describes it rather than naming it, and keeps its line breaks.
	[Fact]
	public void ATabsTooltip_ShowsInAMokaTooltip_AndDescribesTheTab()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(new TabInfo<string>
		{
			Id = "a",
			Title = "web01",
			Tooltip = "root@web01:22\nvim /etc/nginx/nginx.conf"
		});

		IElement tab = Tab(cut);
		Assert.False(tab.HasAttribute("title"));
		Assert.Equal("root@web01:22\nvim /etc/nginx/nginx.conf", tab.GetAttribute("aria-description"));

		IElement trigger = tab.ParentElement!;
		Assert.Contains("moka-tab-tooltip", trigger.ClassList);
		IElement popup = Assert.Single(trigger.Children, c => c.ClassList.Contains("moka-tooltip-popup"));
		Assert.Equal("root@web01:22\nvim /etc/nginx/nginx.conf", popup.TextContent);
		Assert.Contains("moka-tooltip-popup--bottom", popup.ClassList);
		Assert.Contains("moka-tooltip-popup--wrap", popup.ClassList);
	}

	[Fact]
	public void ATabWithoutATooltip_ShowsNone()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(new TabInfo<string> { Id = "a", Title = "Alpha" });

		IElement tab = Tab(cut);
		Assert.False(tab.HasAttribute("aria-description"));
		Assert.DoesNotContain(tab.ParentElement!.Children, c => c.ClassList.Contains("moka-tooltip-popup"));
	}

	// Every tab sits in its tooltip's element, which is what the strip lays out and keys, so the tabs keep their
	// order and identity whether or not they have a tooltip.
	[Fact]
	public void EveryTab_IsTheOnlyChildOfItsTooltip()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(
			new TabInfo<string> { Id = "a", Title = "Alpha", Tooltip = "First" },
			new TabInfo<string> { Id = "b", Title = "Beta" });

		IElement strip = cut.Find("[role=tablist]");
		Assert.All(strip.Children, child =>
		{
			Assert.Contains("moka-tab-tooltip", child.ClassList);
			Assert.Single(child.Children, c => c.GetAttribute("role") == "tab");
		});
		Assert.Equal(["a", "b"], strip.QuerySelectorAll("[role=tab]").Select(t => t.GetAttribute("data-tab-id")));
	}

	// The tab's tooltip and the close button's would both open below, one over the other, so the strip hides the
	// tab's while the pointer is on a button inside the tab.
	[Theory]
	[InlineData(".moka-tab-strip ::deep .moka-tab-tooltip:has(.moka-tooltip-trigger:hover) > .moka-tooltip-popup")]
	[InlineData(".moka-tab-strip ::deep .moka-tab-tooltip:has(.moka-tab-actions:hover) > .moka-tooltip-popup")]
	public void TheTabsTooltip_GivesWayToAButtonsOwn(string selector) =>
		Assert.Equal("none", ScopedCss.Declarations("MokaTabStrip.razor.css", selector)["display"]);

	private IRenderedComponent<MokaTabStrip<string>> RenderStrip(params TabInfo<string>[] tabs) =>
		Render<MokaTabStrip<string>>(p => p.Add(x => x.Tabs, tabs).Add(x => x.ActiveTabId, tabs[0].Id));

	private static IElement Tab(IRenderedComponent<MokaTabStrip<string>> cut) => cut.Find("[role=tab]");
}
