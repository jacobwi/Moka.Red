using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Icons;
using Moka.Red.Navigation.AppShell;
using Moka.Red.Navigation.Menu;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// A menu link had no target, so a page outside the app (another service's own web page) could only open in place.
public class MokaMenuItemTargetTests : BunitContext
{
	private const string Cancelled = "blazor:onclick:preventDefault";

	public MokaMenuItemTargetTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ALinkToANewTab_HasItsTargetAndRel_AndSaysSo()
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.AddChildContent<MokaMenuItem>(item => item
			.Add(x => x.Text, "Ad blocker")
			.Add(x => x.Href, "http://192.168.1.2:3000/")
			.Add(x => x.Target, "_blank")));

		IElement link = cut.Find("a.moka-menu-item");
		Assert.Equal("http://192.168.1.2:3000/", link.GetAttribute("href"));
		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
		Assert.Equal("true", link.QuerySelector(".moka-menu-item__external")!.GetAttribute("aria-hidden"));
		Assert.Equal(", opens in a new tab", link.QuerySelector(".moka-visually-hidden")!.TextContent);
	}

	[Fact]
	public void ALinkWithoutATarget_IsUnchanged()
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.AddChildContent<MokaMenuItem>(item => item
			.Add(x => x.Text, "Devices")
			.Add(x => x.Href, "devices")));

		IElement link = cut.Find("a.moka-menu-item");
		Assert.False(link.HasAttribute("target"));
		Assert.False(link.HasAttribute("rel"));
		Assert.Empty(link.QuerySelectorAll(".moka-menu-item__external"));
		Assert.Empty(link.QuerySelectorAll(".moka-visually-hidden"));
	}

	[Theory]
	[InlineData("_self")]
	[InlineData("_TOP")]
	public void ATargetThatStaysInTheTab_RendersAsGiven_AndAddsNothing(string target)
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.AddChildContent<MokaMenuItem>(item => item
			.Add(x => x.Text, "Devices")
			.Add(x => x.Href, "devices")
			.Add(x => x.Target, target)));

		IElement link = cut.Find("a.moka-menu-item");
		Assert.Equal(target, link.GetAttribute("target"));
		Assert.False(link.HasAttribute("rel"));
		Assert.Empty(link.QuerySelectorAll(".moka-menu-item__external"));
	}

	[Fact]
	public void ADisabledLink_DropsItsTargetWithItsHref()
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.AddChildContent<MokaMenuItem>(item => item
			.Add(x => x.Text, "Ad blocker")
			.Add(x => x.Href, "http://192.168.1.2:3000/")
			.Add(x => x.Target, "_blank")
			.Add(x => x.Disabled, true)));

		IElement link = cut.Find("a.moka-menu-item");
		Assert.False(link.HasAttribute("href"));
		Assert.False(link.HasAttribute("target"));
		Assert.False(link.HasAttribute("rel"));
	}

	[Fact]
	public void ACollapsedMenu_NamesTheNewTabInTheIconsLabel()
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p
			.Add(x => x.Collapsed, true)
			.AddChildContent<MokaMenuItem>(item => item
				.Add(x => x.Text, "Ad blocker")
				.Add(x => x.Icon, MokaIcons.Action.Settings)
				.Add(x => x.Href, "http://192.168.1.2:3000/")
				.Add(x => x.Target, "_blank")
				.Add(x => x.NewTabText, "öppnas i en ny flik")));

		Assert.Equal("Ad blocker, öppnas i en ny flik", cut.Find("a.moka-menu-item").GetAttribute("aria-label"));
	}

	// A click on a group's link where the person already is opens or closes the group. One that opens another tab
	// always opens it, since this page stays as it is.
	[Fact]
	public void AGroupLinkToANewTab_IsNeverCancelled()
	{
		Services.GetRequiredService<BunitNavigationManager>().NavigateTo("reports");
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.AddChildContent<MokaMenuItem>(group => group
			.Add(x => x.Text, "Reports")
			.Add(x => x.Href, "reports")
			.Add(x => x.Target, "_blank")
			.Add(x => x.Match, NavLinkMatch.All)
			.AddChildContent<MokaMenuItem>(child => child.Add(x => x.Text, "Monthly").Add(x => x.Href, "reports/monthly"))));

		IElement link = cut.Find("a.moka-menu-item--group-link");
		Assert.Equal("page", link.GetAttribute("aria-current"));
		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.False(link.HasAttribute(Cancelled));
	}

	[Fact]
	public void TheRail_HidesTheMark() =>
		Assert.Equal("none", ScopedCss.Declarations("MokaMenu.razor.css",
			"[data-moka-rail] .moka-menu.moka-menu--adaptive ::deep .moka-menu-item__external:not(.moka-menu-item__submenu *)")["display"]);

	[Fact]
	public void TheShell_PassesAnEntrysTargetAndWords()
	{
		JSInterop.SetupModule("./_content/Moka.Red.Navigation/AppShell/MokaAppShell.razor.js").Mode = JSRuntimeMode.Loose;
		SetRendererInfo(new RendererInfo("Server", true));
		MokaNavEntry[] entries =
		[
			new(string.Empty, "Home", "/", MokaIcons.Navigation.Home),
			new("Apps", "Ad blocker", "http://192.168.1.2:3000/", MokaIcons.Action.Settings) { Target = "_blank" }
		];

		IRenderedComponent<MokaAppShell> cut = Render<MokaAppShell>(p => p
			.Add(x => x.Entries, entries)
			.Add(x => x.NewTabText, "neue Registerkarte"));

		IElement link = cut.Find("a.moka-menu-item[target]");
		Assert.Equal("http://192.168.1.2:3000/", link.GetAttribute("href"));
		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
		Assert.Equal(", neue Registerkarte", link.QuerySelector(".moka-visually-hidden")!.TextContent);
		Assert.Null(MokaNavEntry.Find(entries, "/http:/192.168.1.2:3000"));
		Assert.Single(cut.FindAll("a.moka-menu-item[target]"));
	}
}
