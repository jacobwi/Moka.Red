using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Icons;
using Moka.Red.Navigation.AppShell;
using Moka.Red.Navigation.Menu;
using Moka.Red.Primitives.Badge;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// A menu showing only icons hid a group's items, yet its parent still said aria-expanded="true" when pressed, and the
// items couldn't be reached. On the rail a group now opens beside its icon as a flyout: the group's own element, shown
// in the top layer by the menu's script, which also owns the keys while it's open. What the script does in the browser
// can't run here, so these tests cover what the menu renders for it, when it calls it, and how it answers it.
public class MokaMenuFlyoutTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.Navigation/Menu/MokaMenu.razor.js";
	private const string Css = "MokaMenu.razor.css";
	private const string FocusCall = "Blazor._internal.domWrapper.focus";

	private static readonly MokaNavPill Waiting = new(3, MokaBadgeTone.NeedsYou, "3 waiting");

	private readonly BunitJSModuleInterop _module;

	public MokaMenuFlyoutTests()
	{
		_module = JSInterop.SetupModule(ModulePath);
		_module.Setup<bool>("bindRail", _ => true).SetResult(false);
		_module.SetupVoid("unbindRail", _ => true).SetVoidResult();
		_module.SetupVoid("openFlyout", _ => true).SetVoidResult();
		_module.SetupVoid("closeFlyout", _ => true).SetVoidResult();
	}

	private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	public async Task ACollapsedParent_OpensAFlyout_AgainstItsButton()
	{
		IRenderedComponent<MokaMenu> cut = Collapsed();
		string? groupId = cut.Find(".moka-menu-item__submenu").GetAttribute("blazor:elementReference");
		string? buttonId = Parent(cut).GetAttribute("blazor:elementReference");

		await Parent(cut).ClickAsync(new MouseEventArgs());

		JSRuntimeInvocation open = Assert.Single(_module.Invocations["openFlyout"]);
		Assert.False(string.IsNullOrEmpty(Assert.IsType<string>(open.Arguments[0])));
		Assert.Equal(groupId, Assert.IsType<ElementReference>(open.Arguments[1]).Id);
		Assert.Equal(buttonId, Assert.IsType<ElementReference>(open.Arguments[2]).Id);
		Assert.IsType<DotNetObjectReference<MokaMenuItem>>(open.Arguments[3]);

		IElement group = cut.Find(".moka-menu-item__submenu");
		Assert.Equal("manual", group.GetAttribute("popover"));
		Assert.True(group.HasAttribute("data-moka-popup"));
		Assert.False(group.HasAttribute("inert"));
		Assert.Equal("true", Parent(cut).GetAttribute("aria-expanded"));
		Assert.Equal(group.Id, Parent(cut).GetAttribute("aria-controls"));
	}

	[Fact]
	public async Task TheFlyout_ListsTheChildren_WithTheirNamesIconsPillsAndCurrentPage()
	{
		Navigation.NavigateTo("reports/yearly");
		IRenderedComponent<MokaMenu> cut = Collapsed();

		// The current page opened the group for rows, but on the rail nothing shows until the parent is pressed.
		Assert.Equal("false", Parent(cut).GetAttribute("aria-expanded"));
		Assert.Empty(_module.Invocations["openFlyout"]);

		await Parent(cut).ClickAsync(new MouseEventArgs());

		IElement group = cut.Find(".moka-menu-item__flyout");
		IReadOnlyList<IElement> links = [.. group.QuerySelectorAll(":scope > .moka-menu-item__submenu-clip > a.moka-menu-item")];
		Assert.Equal(["Monthly", "Yearly"], links.Select(link => link.QuerySelector(".moka-menu-item__text")?.TextContent));
		Assert.All(links, link =>
		{
			Assert.False(link.HasAttribute("aria-label"));
			Assert.DoesNotContain("moka-menu-item--collapsed", link.ClassList);
			Assert.NotNull(link.QuerySelector(".moka-menu-item__icon"));
		});
		Assert.Equal("3", links[0].QuerySelector(".moka-menu-item__pill")?.TextContent.Trim());
		Assert.Equal("page", links[1].GetAttribute("aria-current"));
		Assert.Empty(group.QuerySelectorAll(".moka-tooltip-trigger, .moka-menu-item-tooltip"));
		Assert.Equal("Reports", group.QuerySelector(".moka-menu-item__flyout-title[aria-hidden=true]")?.TextContent);
	}

	// A flyout lists its group from its own edge, where in place the group is indented under its parent.
	[Fact]
	public void TheFlyout_ListsItsRows_WithoutTheIndent()
	{
		IRenderedComponent<MokaMenu> cut = Collapsed();

		Assert.DoesNotContain("padding-left", cut.Find(".moka-menu-item__flyout a.moka-menu-item").GetAttribute("style") ?? "", StringComparison.Ordinal);
	}

	[Fact]
	public async Task AGroupInsideTheFlyout_IsADisclosure_ThatOpensInPlace()
	{
		IRenderedComponent<MokaMenu> cut = Collapsed();
		await Parent(cut).ClickAsync(new MouseEventArgs());

		IElement inner = cut.Find(".moka-menu-item__flyout button.moka-menu-item");
		Assert.Equal("false", inner.GetAttribute("aria-expanded"));
		IElement innerGroup = cut.Find($"#{inner.GetAttribute("aria-controls")}");
		Assert.False(innerGroup.HasAttribute("popover"));
		Assert.DoesNotContain("moka-menu-item__flyout", innerGroup.ClassList);

		await inner.ClickAsync(new MouseEventArgs());

		Assert.Equal("true", cut.Find(".moka-menu-item__flyout button.moka-menu-item").GetAttribute("aria-expanded"));
		Assert.Equal("true", Parent(cut).GetAttribute("aria-expanded"));
		Assert.Single(_module.Invocations["openFlyout"]);
		Assert.Empty(_module.Invocations["closeFlyout"]);
	}

	[Fact]
	public async Task TheParentsTag_GivesWay_WhileTheFlyoutIsOpen()
	{
		IRenderedComponent<MokaMenu> cut = Collapsed();
		Assert.Equal("Reports", ParentTag(cut)?.TextContent.Trim());

		await Parent(cut).ClickAsync(new MouseEventArgs());
		Assert.Null(ParentTag(cut));

		await Parent(cut).ClickAsync(new MouseEventArgs());
		Assert.Equal("Reports", ParentTag(cut)?.TextContent.Trim());
	}

	[Fact]
	public async Task PressingTheParentAgain_ClosesIt_AndHandsFocusBackIfItWasInside()
	{
		IRenderedComponent<MokaMenu> cut = Collapsed();
		await Parent(cut).ClickAsync(new MouseEventArgs());
		string key = OpenedKey();

		await Parent(cut).ClickAsync(new MouseEventArgs());

		AssertClosed(cut, key, returnFocus: true);
	}

	// Escape, the arrow back to the rail, a press outside and focus leaving are seen by the script, which puts focus
	// where it belongs and asks the item to close.
	[Fact]
	public async Task TheScript_ClosesIt_WithoutMovingFocus()
	{
		IRenderedComponent<MokaMenu> cut = Collapsed();
		await Parent(cut).ClickAsync(new MouseEventArgs());
		string key = OpenedKey();

		await cut.InvokeAsync(() => cut.FindComponents<MokaMenuItem>()[0].Instance.CloseFlyoutAsync());

		AssertClosed(cut, key, returnFocus: false);
	}

	[Fact]
	public async Task GoingToAnotherPage_ClosesIt()
	{
		IRenderedComponent<MokaMenu> cut = Collapsed();
		await Parent(cut).ClickAsync(new MouseEventArgs());
		string key = OpenedKey();

		Navigation.NavigateTo("elsewhere");

		cut.WaitForAssertion(() => AssertClosed(cut, key, returnFocus: true));
	}

	// Focus goes back to the parent before the app's click runs, so a dialog it opens returns focus there.
	[Fact]
	public async Task PressingAButtonInside_ClosesIt_AndFocusesTheParent_BeforeItsClickRuns()
	{
		int focusesBeforeClick = -1;
		IRenderedComponent<MokaMenu> cut = Collapsed(extra: child => child
			.Add(x => x.Text, "Export")
			.Add(x => x.OnClick, _ => focusesBeforeClick = JSInterop.Invocations[FocusCall].Count));
		string? buttonId = Parent(cut).GetAttribute("blazor:elementReference");
		await Parent(cut).ClickAsync(new MouseEventArgs());
		string key = OpenedKey();

		await cut.Find(".moka-menu-item__flyout > .moka-menu-item__submenu-clip > button.moka-menu-item:not(.moka-menu-item--has-children)")
			.ClickAsync(new MouseEventArgs());

		Assert.Equal(1, focusesBeforeClick);
		Assert.Equal(buttonId, Assert.IsType<ElementReference>(JSInterop.Invocations[FocusCall][0].Arguments[0]).Id);
		AssertClosed(cut, key, returnFocus: true);
	}

	[Fact]
	public async Task NothingListens_WhileItsClosed()
	{
		IRenderedComponent<MokaMenu> cut = Collapsed();
		int before = Listeners();

		Assert.Empty(_module.Invocations["openFlyout"]);
		JSRuntimeInvocation bind = Assert.Single(_module.Invocations["bindRail"]);
		Assert.Null(bind.Arguments[1]);

		await Parent(cut).ClickAsync(new MouseEventArgs());
		Assert.Equal(before + 1, Listeners());

		await Parent(cut).ClickAsync(new MouseEventArgs());
		Assert.Equal(before, Listeners());
	}

	[Fact]
	public async Task Disposal_TakesAnOpenFlyoutDown_AndStopsListening()
	{
		int before = Listeners();
		IRenderedComponent<MokaMenu> cut = Collapsed();
		await Parent(cut).ClickAsync(new MouseEventArgs());
		string key = OpenedKey();

		await DisposeComponentsAsync();

		JSRuntimeInvocation close = Assert.Single(_module.Invocations["closeFlyout"]);
		Assert.Equal(key, close.Arguments[0]);
		Assert.Equal(false, close.Arguments[1]);
		Assert.Single(_module.Invocations["unbindRail"]);
		Assert.Equal(before, Listeners());
	}

	[Fact]
	public async Task ExpandingTheMenu_ClosesTheFlyout_AndTheGroupOpensInPlaceAgain()
	{
		IRenderedComponent<MokaMenu> cut = Collapsed();
		await Parent(cut).ClickAsync(new MouseEventArgs());
		string key = OpenedKey();

		cut.Render(p => p.Add(x => x.Collapsed, false));

		AssertClosed(cut, key, returnFocus: true);
		await Parent(cut).ClickAsync(new MouseEventArgs());
		Assert.Equal("true", Parent(cut).GetAttribute("aria-expanded"));
		Assert.Single(_module.Invocations["openFlyout"]);
	}

	// A menu that never shows only icons asks nothing of the browser, and its groups open in place as they always did.
	[Fact]
	public async Task AMenuOfRows_BindsNothing_AndOpensGroupsInPlace()
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.AddChildContent<MokaMenuItem>(Group));
		Assert.False(cut.Find(".moka-menu-item__submenu").HasAttribute("popover"));
		Assert.DoesNotContain("moka-menu-item__flyout", cut.Find(".moka-menu-item__submenu").ClassList);

		await Parent(cut).ClickAsync(new MouseEventArgs());

		Assert.Equal("true", Parent(cut).GetAttribute("aria-expanded"));
		Assert.False(cut.Find(".moka-menu-item__submenu").HasAttribute("popover"));
		Assert.Empty(_module.Invocations["bindRail"]);
		Assert.Empty(_module.Invocations["openFlyout"]);
	}

	[Fact]
	public void AnAdaptiveMenu_WatchesTheRail_ThroughItsScript()
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.Add(x => x.Adaptive, true).AddChildContent<MokaMenuItem>(Group));

		JSRuntimeInvocation bind = Assert.Single(_module.Invocations["bindRail"]);
		Assert.Equal(cut.Find("nav").GetAttribute("blazor:elementReference"), Assert.IsType<ElementReference>(bind.Arguments[0]).Id);
		Assert.IsType<DotNetObjectReference<MokaMenu>>(bind.Arguments[1]);
	}

	// Rows or rail is the browser's call on an adaptive menu, so the parent says expanded for whatever is showing: the
	// group in place in rows, the flyout on the rail.
	[Fact]
	public async Task AnAdaptiveParent_SaysExpandedOnlyForWhatShows()
	{
		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.Add(x => x.Adaptive, true).AddChildContent<MokaMenuItem>(Group));
		MokaMenu menu = cut.Instance;

		await Parent(cut).ClickAsync(new MouseEventArgs());
		Assert.Equal("true", Parent(cut).GetAttribute("aria-expanded"));
		Assert.Empty(_module.Invocations["openFlyout"]);

		await cut.InvokeAsync(() => menu.OnRailChanged(true));
		Assert.Equal("false", Parent(cut).GetAttribute("aria-expanded"));
		Assert.True(cut.Find(".moka-menu-item__flyout").HasAttribute("inert"));

		await Parent(cut).ClickAsync(new MouseEventArgs());
		Assert.Equal("true", Parent(cut).GetAttribute("aria-expanded"));
		string key = OpenedKey();

		await cut.InvokeAsync(() => menu.OnRailChanged(false));
		AssertClosedCall(key, returnFocus: true);
		Assert.Equal("true", Parent(cut).GetAttribute("aria-expanded"));
		Assert.False(cut.Find(".moka-menu-item__submenu").HasAttribute("popover"));
	}

	[Fact]
	public async Task ADeepLink_OnTheRail_OpensTheGroupForRows_ButNoFlyout()
	{
		_module.Setup<bool>("bindRail", _ => true).SetResult(true);
		Navigation.NavigateTo("reports/yearly");

		IRenderedComponent<MokaMenu> cut = Render<MokaMenu>(p => p.Add(x => x.Adaptive, true).AddChildContent<MokaMenuItem>(Group));

		cut.WaitForAssertion(() => Assert.Equal("false", Parent(cut).GetAttribute("aria-expanded")));
		Assert.Contains("moka-menu-item__submenu--open", cut.Find(".moka-menu-item__submenu").ClassList);
		Assert.Empty(_module.Invocations["openFlyout"]);

		await cut.InvokeAsync(() => cut.Instance.OnRailChanged(false));
		Assert.Equal("true", Parent(cut).GetAttribute("aria-expanded"));
	}

	[Fact]
	public void OnTheRail_TheGroupInPlaceIsHidden_AndOnlyTheOpenFlyoutShows()
	{
		foreach (string rail in new[] { ".moka-menu--collapsed", "[data-moka-rail] .moka-menu.moka-menu--adaptive" })
		{
			Assert.Equal("none", ScopedCss.Declarations(Css, $"{rail} ::deep .moka-menu-item__flyout")["display"]);
			Assert.Equal("block", ScopedCss.Declarations(Css, $"{rail} ::deep .moka-menu-item__flyout:popover-open")["display"]);
		}

		Assert.Equal("none", ScopedCss.Declarations(Css, "::deep .moka-menu-item__flyout-title")["display"]);
	}

	// The rail's own rules would draw the flyout's rows as icons with hidden names and corner badges.
	[Theory]
	[InlineData(".moka-menu--collapsed ::deep .moka-menu-item__content:not(.moka-menu-item__submenu *)", "justify-content")]
	[InlineData("[data-moka-rail] .moka-menu.moka-menu--adaptive ::deep .moka-menu-item.moka-fill-width:not(.moka-menu-item__submenu *) > .moka-menu-item__content", "justify-content")]
	[InlineData("[data-moka-rail] .moka-menu.moka-menu--adaptive ::deep .moka-menu-item__text:not(.moka-menu-item__submenu *)", "clip-path")]
	[InlineData(".moka-menu--collapsed ::deep .moka-menu-item__pill:not(.moka-menu-item__submenu *)", "position")]
	[InlineData("[data-moka-rail] .moka-menu.moka-menu--adaptive ::deep .moka-menu-item__chevron:not(.moka-menu-item__submenu *)", "display")]
	public void TheRailsRules_ReachOnlyItsOwnItems(string selector, string property) =>
		Assert.Contains(property, ScopedCss.Declarations(Css, selector).Keys);

	[Fact]
	public void TheOpenFlyout_ReadsEveryPopupVariable_InTheGateBlockOnly()
	{
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations(Css, ".moka-gate .moka-menu ::deep .moka-menu-item__flyout:popover-open");
		Assert.StartsWith("var(--moka-popup-bg, ", rule["background"], StringComparison.Ordinal);
		Assert.StartsWith("var(--moka-popup-border, ", rule["border-color"], StringComparison.Ordinal);
		Assert.StartsWith("var(--moka-popup-radius, ", rule["border-radius"], StringComparison.Ordinal);
		Assert.StartsWith("var(--moka-popup-shadow, ", rule["box-shadow"], StringComparison.Ordinal);
		Assert.Equal(rule["backdrop-filter"], rule["-webkit-backdrop-filter"]);

		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css));
		Assert.DoesNotContain("--moka-popup-", css[..css.IndexOf("/* Gate look.", StringComparison.Ordinal)], StringComparison.Ordinal);
	}

	private IRenderedComponent<MokaMenu> Collapsed(Action<ComponentParameterCollectionBuilder<MokaMenuItem>>? extra = null) =>
		Render<MokaMenu>(p => p
			.Add(x => x.Collapsed, true)
			.AddChildContent<MokaMenuItem>(group =>
			{
				Group(group);
				if (extra is not null)
				{
					group.AddChildContent<MokaMenuItem>(extra);
				}
			}));

	private static void Group(ComponentParameterCollectionBuilder<MokaMenuItem> group) => group
		.Add(x => x.Text, "Reports")
		.Add(x => x.Icon, MokaIcons.File.Document)
		.AddChildContent<MokaMenuItem>(child => child
			.Add(x => x.Text, "Monthly")
			.Add(x => x.Icon, MokaIcons.File.Document)
			.Add(x => x.Href, "reports/monthly")
			.Add(x => x.Pill, Waiting))
		.AddChildContent<MokaMenuItem>(child => child
			.Add(x => x.Text, "Yearly")
			.Add(x => x.Icon, MokaIcons.File.Document)
			.Add(x => x.Href, "reports/yearly")
			.Add(x => x.Match, NavLinkMatch.All))
		.AddChildContent<MokaMenuItem>(child => child
			.Add(x => x.Text, "Archive")
			.AddChildContent<MokaMenuItem>(grandchild => grandchild.Add(x => x.Text, "2024").Add(x => x.Href, "reports/2024")));

	private static IElement Parent(IRenderedComponent<MokaMenu> cut) => cut.Find("button.moka-menu-item--has-children");

	private static IElement? ParentTag(IRenderedComponent<MokaMenu> cut) =>
		cut.Find(".moka-menu-item-tooltip").QuerySelector(":scope > .moka-tooltip-popup");

	private string OpenedKey() => Assert.IsType<string>(_module.Invocations["openFlyout"][^1].Arguments[0]);

	private void AssertClosed(IRenderedComponent<MokaMenu> cut, string key, bool returnFocus)
	{
		AssertClosedCall(key, returnFocus);
		IElement group = cut.Find(".moka-menu-item__submenu");
		Assert.False(group.HasAttribute("popover"));
		Assert.False(group.HasAttribute("data-moka-popup"));
		Assert.Equal("false", Parent(cut).GetAttribute("aria-expanded"));
		Assert.True(group.HasAttribute("inert"));
	}

	private void AssertClosedCall(string key, bool returnFocus)
	{
		JSRuntimeInvocation close = _module.Invocations["closeFlyout"][^1];
		Assert.Equal(key, close.Arguments[0]);
		Assert.Equal(returnFocus, close.Arguments[1]);
	}

	// NavigationManager doesn't say how many handlers it has, so this reads the event's backing field.
	private int Listeners()
	{
		FieldInfo field = typeof(NavigationManager).GetField("_locationChanged", BindingFlags.Instance | BindingFlags.NonPublic)
		                  ?? throw new InvalidOperationException("NavigationManager no longer keeps LocationChanged in _locationChanged.");
		return (field.GetValue(Navigation) as Delegate)?.GetInvocationList().Length ?? 0;
	}
}
