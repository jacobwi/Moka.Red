using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Icons;
using Moka.Red.Navigation.AppShell;
using Moka.Red.Navigation.Menu;
using Moka.Red.Primitives.Badge;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

public class MokaMenuAdaptiveTests : BunitContext
{
	private static readonly MokaNavPill Blocked = new(2, MokaBadgeTone.NeedsYou, "2 blocked in the last hour");

	[Fact]
	public void AnItem_HasNoPill_ByDefault()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(adaptive: false, collapsed: false, pill: null);

		Assert.Empty(cut.FindAll(".moka-menu-item__pill"));
		Assert.Empty(cut.FindAll(".moka-visually-hidden"));
	}

	[Fact]
	public void APill_IsABadgeInItsTone_WithItsReasonForScreenReaders()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(adaptive: false, collapsed: false, Blocked);

		IElement badge = cut.Find(".moka-menu-item__pill");
		Assert.Contains("moka-badge--tone-needs-you", badge.ClassList);
		Assert.Equal("true", badge.GetAttribute("aria-hidden"));
		Assert.Equal("2", badge.TextContent.Trim());
		Assert.Equal(", 2 blocked in the last hour", cut.Find("a .moka-visually-hidden").TextContent);
	}

	[Fact]
	public void APill_InAnIconOnlyMenu_IsInTheNameAndTheTooltip()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(adaptive: false, collapsed: true, Blocked);

		Assert.Equal("Intrusions, 2 blocked in the last hour", cut.Find("a.moka-menu-item").GetAttribute("aria-label"));
		Assert.Single(cut.FindAll(".moka-menu-item__pill"));
		Assert.Equal("2 blocked in the last hour", cut.Find(".moka-tooltip-popup .moka-menu-item__why").TextContent);
	}

	[Fact]
	public void AMenu_IsNotAdaptive_ByDefault()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(adaptive: false, collapsed: false, pill: null);

		Assert.Equal("moka-menu moka-fill-width moka-menu--dense", cut.Find("nav").ClassName);
		Assert.Empty(cut.FindAll(".moka-tooltip-trigger"));
	}

	[Fact]
	public void AnAdaptiveItem_KeepsItsTextAndSitsInItsTag()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(adaptive: true, collapsed: false, pill: null);

		Assert.Contains("moka-menu--adaptive", cut.Find("nav").ClassList);
		IElement link = cut.Find("a.moka-menu-item");
		Assert.False(link.HasAttribute("aria-label"));
		Assert.Equal("Intrusions", link.QuerySelector(".moka-menu-item__text")?.TextContent);
		IElement popup = cut.Find(".moka-menu-item-tooltip > .moka-tooltip-popup");
		Assert.Contains("moka-tooltip-popup--right", popup.ClassList);
		Assert.Equal("Intrusions", popup.TextContent.Trim());
	}

	[Fact]
	public void AnAdaptiveItem_ShowsItsTagAtOnce()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(adaptive: true, collapsed: false, pill: null);

		Assert.Contains("--moka-tooltip-delay: 0ms", cut.Find(".moka-tooltip-popup").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void AnAdaptiveItem_PutsThePillsReasonInItsTag()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(adaptive: true, collapsed: false, Blocked);

		IElement popup = cut.Find(".moka-tooltip-popup");
		Assert.StartsWith("Intrusions", popup.TextContent.Trim(), StringComparison.Ordinal);
		Assert.Equal("2 blocked in the last hour", popup.QuerySelector(".moka-menu-item__why")?.TextContent);
	}

	[Fact]
	public void Collapsed_WinsOverAdaptive()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(adaptive: true, collapsed: true, pill: null);

		Assert.DoesNotContain("moka-menu--adaptive", cut.Find("nav").ClassList);
		Assert.Contains("--moka-tooltip-delay: 300ms", cut.Find(".moka-tooltip-popup").GetAttribute("style"), StringComparison.Ordinal);
	}

	// The tag has to stay away everywhere but on the rail, or hovering a row with its name in plain view would show it again.
	[Fact]
	public void TheTag_ShowsOnlyOnTheRail()
	{
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations(
			"MokaMenu.razor.css",
			".moka-menu--adaptive:not([data-moka-rail] *) ::deep .moka-menu-item-tooltip > .moka-tooltip-popup");

		Assert.Equal("none !important", rule["display"]);
	}

	[Fact]
	public void ThePill_OnTheRail_IsACornerBadgeAt15Pixels()
	{
		const string Rail = "[data-moka-rail] .moka-menu.moka-menu--adaptive ::deep .moka-menu-item__pill";

		Assert.Equal("absolute", ScopedCss.Declarations("MokaMenu.razor.css", Rail)["position"]);
		Assert.Equal("0.9375rem", ScopedCss.Declarations("MokaMenu.razor.css", $"{Rail} .moka-badge__indicator")["min-width"]);
	}

	// The row's link is named by its text, so the rail clips it rather than removing it.
	[Fact]
	public void TheText_OnTheRail_StaysForScreenReaders()
	{
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations(
			"MokaMenu.razor.css",
			"[data-moka-rail] .moka-menu.moka-menu--adaptive ::deep .moka-menu-item__text");

		Assert.Equal("inset(50%)", rule["clip-path"]);
		Assert.DoesNotContain("display", rule.Keys);
	}

	private IRenderedComponent<MokaMenu> RenderMenu(bool adaptive, bool collapsed, MokaNavPill? pill) =>
		Render<MokaMenu>(p => p
			.Add(x => x.Adaptive, adaptive)
			.Add(x => x.Collapsed, collapsed)
			.AddChildContent<MokaMenuItem>(item => item
				.Add(x => x.Text, "Intrusions")
				.Add(x => x.Icon, MokaIcons.Status.Warning)
				.Add(x => x.Href, "/intrusions")
				.Add(x => x.Pill, pill)));
}
