using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;
using Moka.Red.Navigation.Menu;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// A parent with an Href is a page that also holds a group: the name is the link, and a disclosure button beside it opens
// and closes the group, as in WAI-ARIA's disclosure navigation with top-level links. A click on the name where the person
// already is opens or closes the group in place of following the link, which the click's preventDefault decides as it
// renders. bUnit follows no links, so "follows the link" here means the click isn't cancelled.
public class MokaMenuGroupLinkTests : BunitContext
{
	private const string Cancelled = "blazor:onclick:preventDefault";
	private const string ModulePath = "./_content/Moka.Red.Navigation/Menu/MokaMenu.razor.js";
	private const string FocusCall = "Blazor._internal.domWrapper.focus";

	private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	public void AParentWithALink_IsALink_WithADisclosureButtonBesideIt()
	{
		Navigation.NavigateTo("home");
		IRenderedComponent<MokaMenu> cut = Menu();

		IElement link = Link(cut);
		IElement toggle = Toggle(cut);
		Assert.Contains("moka-menu-item__row", link.ParentElement!.ClassList);
		Assert.NotNull(toggle.Closest(".moka-menu-item__row"));
		Assert.Null(toggle.Closest("a"));
		Assert.Equal("reports", link.GetAttribute("href"));
		Assert.False(link.HasAttribute("aria-expanded"));
		Assert.DoesNotContain("moka-menu-item--has-children", link.ClassList);
		Assert.Empty(link.QuerySelectorAll(".moka-menu-item__chevron"));

		Assert.Equal("button", toggle.GetAttribute("type"));
		Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
		Assert.Equal(Group(cut).Id, toggle.GetAttribute("aria-controls"));
		Assert.NotNull(toggle.QuerySelector(".moka-menu-item__chevron"));
		IconTooltip.AssertNamed(toggle, "Show Reports", MokaTooltipPosition.Right);
		Assert.True(Group(cut).HasAttribute("inert"));
	}

	[Fact]
	public async Task TheArrow_OnlyOpensAndClosesTheGroup()
	{
		Navigation.NavigateTo("home");
		List<bool> told = [];
		int picks = 0;
		IRenderedComponent<MokaMenu> cut = Menu(parent: p => p
			.Add(x => x.ExpandedChanged, value => told.Add(value))
			.Add(x => x.OnClick, _ => picks++));

		await Toggle(cut).ClickAsync(new MouseEventArgs());

		AssertOpen(cut, true);
		IconTooltip.AssertNamed(Toggle(cut), "Hide Reports", MokaTooltipPosition.Right);

		await Toggle(cut).ClickAsync(new MouseEventArgs());

		AssertOpen(cut, false);
		Assert.Equal([true, false], told);
		Assert.Equal(0, picks);
		Assert.False(Toggle(cut).HasAttribute(Cancelled));
	}

	[Fact]
	public async Task TheArrowsWords_CanBeReplaced()
	{
		IRenderedComponent<MokaMenu> cut = Menu(parent: p => p
			.Add(x => x.ShowLabel, text => $"Open {text}")
			.Add(x => x.HideLabel, text => $"Close {text}"));
		IconTooltip.AssertNamed(Toggle(cut), "Open Reports", MokaTooltipPosition.Right);

		await Toggle(cut).ClickAsync(new MouseEventArgs());

		IconTooltip.AssertNamed(Toggle(cut), "Close Reports", MokaTooltipPosition.Right);
	}

	[Fact]
	public async Task TheName_ClickedFromElsewhere_FollowsTheLink_AndOpensTheGroup()
	{
		Navigation.NavigateTo("home");
		List<bool> told = [];
		int picks = 0;
		IRenderedComponent<MokaMenu> cut = Menu(parent: p => p
			.Add(x => x.ExpandedChanged, value => told.Add(value))
			.Add(x => x.OnClick, _ => picks++));
		Assert.False(Link(cut).HasAttribute(Cancelled));

		await Link(cut).ClickAsync(new MouseEventArgs { Detail = 1 });

		AssertOpen(cut, true);
		Assert.Equal([true], told);
		Assert.Equal(1, picks);
	}

	[Fact]
	public async Task OnItsOwnPage_TheName_OpensAndClosesTheGroup_WithoutLeaving()
	{
		Navigation.NavigateTo("reports");
		int picks = 0;
		IRenderedComponent<MokaMenu> cut = Menu(parent: p => p.Add(x => x.OnClick, _ => picks++));
		AssertOpen(cut, true);
		Assert.Equal("page", Link(cut).GetAttribute("aria-current"));
		Assert.True(Link(cut).HasAttribute(Cancelled));

		await Link(cut).ClickAsync(new MouseEventArgs { Detail = 1 });

		AssertOpen(cut, false);
		Assert.True(Link(cut).HasAttribute(Cancelled));

		await Link(cut).ClickAsync(new MouseEventArgs { Detail = 1 });

		AssertOpen(cut, true);
		Assert.Equal(0, picks);
	}

	// Enter on a focused link makes the browser click it, with a detail of 0, so the cancelled click and its handler cover
	// the keyboard too. The link has no key handler of its own.
	[Theory]
	[InlineData(1, true)]
	[InlineData(0, true)]
	[InlineData(1, false)]
	[InlineData(0, false)]
	public async Task TheName_OnItsOwnOpenPage_ByMouseOrEnter(int detail, bool collapseOnRepeat)
	{
		Navigation.NavigateTo("reports");
		int picks = 0;
		IRenderedComponent<MokaMenu> cut = Menu(parent: p => p
			.Add(x => x.CollapseOnRepeatClick, collapseOnRepeat)
			.Add(x => x.OnClick, _ => picks++));
		Assert.False(Link(cut).HasAttribute("blazor:onkeydown"));
		Assert.Equal(collapseOnRepeat, Link(cut).HasAttribute(Cancelled));

		await Link(cut).ClickAsync(new MouseEventArgs { Detail = detail });

		AssertOpen(cut, !collapseOnRepeat);
		Assert.Equal(collapseOnRepeat ? 0 : 1, picks);
	}

	[Fact]
	public async Task OnAPageInsideTheOpenGroup_TheNameClosesIt_AndThenFollowsTheLinkAgain()
	{
		Navigation.NavigateTo("reports/monthly");
		int picks = 0;
		IRenderedComponent<MokaMenu> cut = Menu(parent: p => p.Add(x => x.OnClick, _ => picks++));
		AssertOpen(cut, true);
		Assert.Null(Link(cut).GetAttribute("aria-current"));
		Assert.True(Link(cut).HasAttribute(Cancelled));

		await Link(cut).ClickAsync(new MouseEventArgs { Detail = 1 });

		AssertOpen(cut, false);
		Assert.Equal(0, picks);
		Assert.False(Link(cut).HasAttribute(Cancelled));

		await Link(cut).ClickAsync(new MouseEventArgs { Detail = 1 });

		AssertOpen(cut, true);
		Assert.Equal(1, picks);
	}

	[Fact]
	public void OnAPageInsideAGroupThePersonOpenedFromElsewhere_TheNameFollowsTheLink()
	{
		Navigation.NavigateTo("home");
		IRenderedComponent<MokaMenu> cut = Menu();

		Toggle(cut).Click();

		Assert.False(Link(cut).HasAttribute(Cancelled));
	}

	[Fact]
	public void AChildLightingInsideTheOpenGroup_CancelsTheNamesClick()
	{
		IRenderedComponent<MokaMenuItem> cut = Render<MokaMenuItem>(p => p
			.Add(x => x.Text, "Reports")
			.Add(x => x.Href, "reports")
			.Add(x => x.Expanded, true)
			.AddChildContent<MokaMenuItem>(child => child.Add(x => x.Text, "Monthly").Add(x => x.Href, "reports/monthly")));
		Assert.False(cut.Find("a.moka-menu-item--group-link").HasAttribute(Cancelled));

		cut.FindComponent<MokaMenuItem>().Render(p => p.Add(x => x.Active, true));

		Assert.True(cut.Find("a.moka-menu-item--group-link").HasAttribute(Cancelled));

		cut.FindComponent<MokaMenuItem>().Render(p => p.Add(x => x.Active, false));

		Assert.False(cut.Find("a.moka-menu-item--group-link").HasAttribute(Cancelled));
	}

	// The auto-open from the current page and the person's own choice don't fight: a group they closed stays closed until
	// another page inside it lights, its own page included.
	[Fact]
	public async Task AGroupThePersonClosed_StaysClosed_UntilAnotherPageInsideLights()
	{
		Navigation.NavigateTo("reports/monthly");
		IRenderedComponent<MokaMenu> cut = Menu();

		await Link(cut).ClickAsync(new MouseEventArgs { Detail = 1 });
		cut.Render();
		Navigation.NavigateTo("reports/monthly?tab=2");
		AssertOpen(cut, false);

		Navigation.NavigateTo("reports/yearly");
		cut.WaitForAssertion(() => AssertOpen(cut, true));

		await Link(cut).ClickAsync(new MouseEventArgs { Detail = 1 });
		AssertOpen(cut, false);

		Navigation.NavigateTo("reports");
		cut.WaitForAssertion(() => AssertOpen(cut, true));
	}

	[Fact]
	public void TheParentsOwnPage_OpensItsGroup_WhenItLights()
	{
		Navigation.NavigateTo("home");
		IRenderedComponent<MokaMenu> cut = Menu();
		AssertOpen(cut, false);

		Navigation.NavigateTo("reports");

		cut.WaitForAssertion(() =>
		{
			AssertOpen(cut, true);
			Assert.Equal("page", Link(cut).GetAttribute("aria-current"));
		});
	}

	[Fact]
	public void CollapseOnRepeatClick_ComesFromTheMenu_AndAnItemCanSayOtherwise()
	{
		Navigation.NavigateTo("reports");

		Assert.True(Link(Menu()).HasAttribute(Cancelled));
		Assert.False(Link(Menu(p => p.Add(x => x.CollapseOnRepeatClick, false))).HasAttribute(Cancelled));
		Assert.True(Link(Menu(p => p.Add(x => x.CollapseOnRepeatClick, false), p => p.Add(x => x.CollapseOnRepeatClick, true)))
			.HasAttribute(Cancelled));
	}

	// The menu is cascaded as a fixed value, so a link that takes the menu's setting hears a change from the menu itself,
	// even inside content that doesn't redraw.
	[Fact]
	public void ChangingTheMenusSetting_ReachesItsLinks_InsideContentThatDoesntRedraw()
	{
		Navigation.NavigateTo("reports");
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.AddChildContent<Unchanging>(holder => holder
			.AddChildContent<MokaMenuItem>(Reports)));
		Assert.True(Link(cut).HasAttribute(Cancelled));

		cut.Render(p => p.Add(x => x.CollapseOnRepeatClick, false));

		Assert.False(Link(cut).HasAttribute(Cancelled));
	}

	[Fact]
	public async Task AModifiedClick_IsLeftToTheBrowser()
	{
		Navigation.NavigateTo("home");
		int picks = 0;
		IRenderedComponent<MokaMenu> cut = Menu(parent: p => p.Add(x => x.OnClick, _ => picks++));

		await Link(cut).ClickAsync(new MouseEventArgs { Detail = 1, CtrlKey = true });
		await Link(cut).ClickAsync(new MouseEventArgs { Detail = 1, MetaKey = true });
		await Link(cut).ClickAsync(new MouseEventArgs { Detail = 1, ShiftKey = true });

		AssertOpen(cut, false);
		Assert.Equal(0, picks);
	}

	[Fact]
	public void ADisabledParent_IsALinkThatGoesNowhere_AndItsArrowIsDisabled()
	{
		IRenderedComponent<MokaMenu> cut = Menu(parent: p => p.Add(x => x.Disabled, true));

		Assert.False(Link(cut).HasAttribute("href"));
		Assert.Equal("link", Link(cut).GetAttribute("role"));
		Assert.Equal("true", Link(cut).GetAttribute("aria-disabled"));
		Assert.True(Toggle(cut).HasAttribute("disabled"));
	}

	// Items that use none of this render as they did: no row, no arrow, no cancelled click, and a page change that lights
	// nothing inside a group redraws nothing.
	[Fact]
	public void ItemsThatUseNoneOfIt_RenderAsBefore()
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p
			.AddChildContent<MokaMenuItem>(item => item.Add(x => x.Text, "Home").Add(x => x.Href, "home"))
			.AddChildContent<MokaMenuItem>(group => group
				.Add(x => x.Text, "Admin")
				.AddChildContent<MokaMenuItem>(child => child.Add(x => x.Text, "Users").Add(x => x.Href, "admin/users"))));

		Assert.Empty(cut.FindAll(".moka-menu-item__row, .moka-menu-item__toggle, .moka-menu-item--group-link"));
		Assert.All(cut.FindAll("a.moka-menu-item"), link => Assert.False(link.HasAttribute(Cancelled)));
		Assert.Contains("moka-menu-item--has-children", cut.Find("button.moka-menu-item").ClassList);
	}

	[Fact]
	public void APageChangeThatLightsNothingInside_RedrawsNothing()
	{
		Navigation.NavigateTo("home");
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.AddChildContent<MokaMenuItem>(group => group
			.Add(x => x.Text, "Reports")
			.Add(x => x.Href, "reports")
			.Add(x => x.Match, NavLinkMatch.All)
			.AddChildContent<MokaMenuItem>(child => child.Add(x => x.Text, "Monthly").Add(x => x.Href, "reports/monthly"))));
		IRenderedComponent<MokaMenuItem> parent = cut.FindComponent<MokaMenuItem>();
		int renders = parent.RenderCount;

		Navigation.NavigateTo("settings");
		Navigation.NavigateTo("reports/monthly");

		Assert.Equal(renders, parent.RenderCount);
	}

	[Fact]
	public async Task OnTheRail_TheParentsIcon_OpensTheFlyout_WhichStartsWithItsPage()
	{
		BunitJSModuleInterop module = RailModule();
		Navigation.NavigateTo("reports");
		int picks = 0;
		IRenderedComponent<MokaMenu> cut = Menu(p => p.Add(x => x.Collapsed, true), p => p.Add(x => x.OnClick, _ => picks++));

		IElement parent = cut.Find("button.moka-menu-item--has-children");
		Assert.Empty(cut.FindAll(".moka-menu-item__row, a.moka-menu-item--group-link"));
		Assert.Equal("Reports", parent.GetAttribute("aria-label"));

		await parent.ClickAsync(new MouseEventArgs());

		Assert.Single(module.Invocations["openFlyout"]);
		Assert.Equal(0, picks);
		IElement flyout = cut.Find(".moka-menu-item__flyout");
		IElement first = flyout.QuerySelector(":scope > .moka-menu-item__submenu-clip > :first-child")!;
		Assert.Equal("A", first.TagName);
		Assert.Contains("moka-menu-item__flyout-link", first.ClassList);
		Assert.Contains("moka-menu-item__flyout-title", first.ClassList);
		Assert.Equal("reports", first.GetAttribute("href"));
		Assert.Equal("page", first.GetAttribute("aria-current"));
		Assert.Equal("Reports", first.TextContent);
		Assert.False(first.HasAttribute("aria-hidden"));
	}

	[Fact]
	public async Task FollowingThePageAtTheTopOfTheFlyout_ClosesIt_FocusesTheIcon_AndCountsAsAPick()
	{
		BunitJSModuleInterop module = RailModule();
		Navigation.NavigateTo("home");
		int picks = 0;
		IRenderedComponent<MokaMenu> cut = Menu(p => p.Add(x => x.Collapsed, true), p => p.Add(x => x.OnClick, _ => picks++));
		await cut.Find("button.moka-menu-item--has-children").ClickAsync(new MouseEventArgs());
		IElement link = cut.Find("a.moka-menu-item__flyout-link");
		Assert.Null(link.GetAttribute("aria-current"));

		await link.ClickAsync(new MouseEventArgs { Detail = 1 });

		JSRuntimeInvocation close = Assert.Single(module.Invocations["closeFlyout"]);
		Assert.Equal(true, close.Arguments[1]);
		Assert.Single(JSInterop.Invocations[FocusCall]);
		Assert.Equal(1, picks);
		Assert.Equal("false", cut.Find("button.moka-menu-item--has-children").GetAttribute("aria-expanded"));
	}

	[Fact]
	public void TheFlyoutsPageLink_IsAStopForTheFlyoutsArrowKeys()
	{
		string script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaMenu.razor.js"));

		Assert.Contains("a.moka-menu-item__flyout-link[href]", script, StringComparison.Ordinal);
		Assert.Contains("button.moka-menu-item__toggle:not(:disabled)", script, StringComparison.Ordinal);
	}

	private BunitJSModuleInterop RailModule()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
		module.Setup<bool>("bindRail", _ => true).SetResult(false);
		module.SetupVoid("unbindRail", _ => true).SetVoidResult();
		module.SetupVoid("openFlyout", _ => true).SetVoidResult();
		module.SetupVoid("closeFlyout", _ => true).SetVoidResult();
		JSInterop.SetupVoid(FocusCall, _ => true).SetVoidResult();
		return module;
	}

	private IRenderedComponent<MokaMenu> Menu(
		Action<ComponentParameterCollectionBuilder<MokaMenu>>? menu = null,
		Action<ComponentParameterCollectionBuilder<MokaMenuItem>>? parent = null) =>
		Render<MokaMenu>(p =>
		{
			menu?.Invoke(p);
			p.AddChildContent<MokaMenuItem>(group =>
			{
				parent?.Invoke(group);
				Reports(group);
			});
		});

	private static void Reports(ComponentParameterCollectionBuilder<MokaMenuItem> group) => group
		.Add(x => x.Text, "Reports")
		.Add(x => x.Icon, MokaIcons.File.Document)
		.Add(x => x.Href, "reports")
		.Add(x => x.Match, NavLinkMatch.All)
		.AddChildContent<MokaMenuItem>(child => Page(child, "Monthly", "reports/monthly"))
		.AddChildContent<MokaMenuItem>(child => Page(child, "Yearly", "reports/yearly"));

	private static void Page(ComponentParameterCollectionBuilder<MokaMenuItem> child, string text, string href) => child
		.Add(x => x.Text, text)
		.Add(x => x.Href, href)
		.Add(x => x.Match, NavLinkMatch.All);

	private static IElement Link(IRenderedComponent<MokaMenu> cut) => cut.Find("a.moka-menu-item--group-link");

	private static IElement Toggle(IRenderedComponent<MokaMenu> cut) => cut.Find("button.moka-menu-item__toggle");

	private static IElement Group(IRenderedComponent<MokaMenu> cut) => cut.Find(".moka-menu-item__submenu");

	private static void AssertOpen(IRenderedComponent<MokaMenu> cut, bool open)
	{
		Assert.Equal(open ? "true" : "false", Toggle(cut).GetAttribute("aria-expanded"));
		Assert.Equal(open, Group(cut).ClassList.Contains("moka-menu-item__submenu--open"));
		Assert.Equal(!open, Group(cut).HasAttribute("inert"));
	}

	// Draws its content once and never again, as content a parent doesn't redraw would.
	private sealed class Unchanging : ComponentBase
	{
		private bool _drawn;

		[Parameter]
		public RenderFragment? ChildContent { get; set; }

		protected override bool ShouldRender() => !_drawn;

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			_drawn = true;
			builder.AddContent(0, ChildContent);
		}
	}
}
