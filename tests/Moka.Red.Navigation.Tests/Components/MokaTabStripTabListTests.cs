using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Dropdown;
using Moka.Red.Feedback.Popover;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Navigation.Extensions;
using Moka.Red.Navigation.Tabs;
using Moka.Red.Navigation.Tabs.Models;
using Moka.Red.Navigation.Tabs.Services;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// Whether the tabs fit is measured by moka-tabs.js in the browser, and the reveal scrolls and focuses there. These tests
// cover what the strip renders for it, what it asks the script to do and when, and what a pick tells the parent.
public class MokaTabStripTabListTests : BunitContext
{
	private const string TabsModule = "./_content/Moka.Red.Navigation/moka-tabs.js";
	private const string Css = "MokaTabStrip.razor.css";

	private readonly BunitJSModuleInterop _module;

	public MokaTabStripTabListTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(TabsModule);
		_module.Mode = JSRuntimeMode.Loose;
		TopLayerPopup.Setup(JSInterop);
	}

	[Fact]
	public void Off_TheStripRendersAsBefore()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(ThreeTabs(), "a", list: false);

		IElement root = Assert.Single(cut.Nodes.OfType<IElement>());
		Assert.Equal("tablist", root.GetAttribute("role"));
		Assert.Empty(cut.FindAll(".moka-tab-strip-frame"));
		Assert.Empty(cut.FindAll(".moka-tab-list-button"));
		Assert.Empty(_module.Invocations["MokaTabs.watchOverflow"]);
	}

	[Fact]
	public void On_TheButtonSitsAfterTheStrip_InTheSameFrame()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(ThreeTabs(), "a");

		IElement frame = cut.Find(".moka-tab-strip-frame");
		Assert.Equal(2, frame.Children.Length);
		Assert.Equal("tablist", frame.Children[0].GetAttribute("role"));
		Assert.Contains("moka-tab-strip-list", frame.Children[1].ClassList);
		Assert.NotNull(frame.Children[1].QuerySelector("button.moka-tab-list-button"));
		Assert.Empty(cut.Find("[role=tablist]").QuerySelectorAll(".moka-tab-list-button"));
	}

	[Fact]
	public void TheButton_IsNamedByATooltipAtTheRowsEnd()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(ThreeTabs(), "a");

		IconTooltip.AssertNamed(cut.Find(".moka-tab-list-button"), "All tabs", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void TabListLabel_NamesTheButton()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(ThreeTabs(), "a", p => p.Add(x => x.TabListLabel, "Open tabs"));

		IconTooltip.AssertNamed(cut.Find(".moka-tab-list-button"), "Open tabs", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void TheOpenMenu_DropsTheTooltip()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(ThreeTabs(), "a");

		OpenList(cut);

		IElement trigger = cut.Find(".moka-tab-list-button").Closest(".moka-tooltip-trigger")!;
		Assert.DoesNotContain(trigger.Children, child => child.ClassList.Contains("moka-tooltip-popup"));
	}

	[Fact]
	public void TheButton_DrawsTheProvidersIcon()
	{
		Services.AddScoped<MokaTabIconProvider, MarkedIcons>();

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(ThreeTabs(), "a");

		Assert.NotNull(cut.Find(".moka-tab-list-button").QuerySelector(".marked-list"));
	}

	[Fact]
	public void TheBuiltInIcon_IsAChevron()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(ThreeTabs(), "a");

		Assert.NotNull(cut.Find(".moka-tab-list-button").QuerySelector("svg"));
	}

	[Fact]
	public void TheStrip_IsWatched_AndBoundOnce()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(ThreeTabs(), "a");

		string? strip = cut.Find("[role=tablist]").GetAttribute("blazor:elementReference");
		JSRuntimeInvocation watch = _module.VerifyInvoke("MokaTabs.watchOverflow");
		Assert.Equal(strip, Assert.IsType<ElementReference>(watch.Arguments[0]).Id);
		JSRuntimeInvocation bind = _module.VerifyInvoke("MokaTabs.bindTabStrip");
		Assert.Equal(strip, Assert.IsType<ElementReference>(bind.Arguments[0]).Id);
	}

	// The tablist moves into the frame or out of it, so it is a new element that needs its keys again.
	[Fact]
	public void TurningTheListOnLater_BindsTheNewStrip()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(ThreeTabs(), "a", list: false);

		cut.Render(p => p.Add(x => x.ShowTabList, true));

		Assert.Equal(2, _module.Invocations["MokaTabs.bindTabStrip"].Count);
		string? strip = cut.Find("[role=tablist]").GetAttribute("blazor:elementReference");
		Assert.Equal(strip, Assert.IsType<ElementReference>(_module.Invocations["MokaTabs.bindTabStrip"][^1].Arguments[0]).Id);
		Assert.Single(_module.Invocations["MokaTabs.watchOverflow"]);
	}

	[Fact]
	public void TheMenu_ListsEveryTab_InTheStripsOrder()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(ThreeTabs(), "a");

		OpenList(cut);

		Assert.Equal(["Alpha", "Beta", "Gamma"], Titles(cut));
		Assert.Equal(MokaPopoverPosition.BottomEnd, cut.FindComponent<MokaDropdown>().Instance.Position);
	}

	[Fact]
	public void TheCurrentTab_IsMarkedCurrentAndChecked()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(ThreeTabs(), "b");

		OpenList(cut);

		IReadOnlyList<IElement> items = cut.FindAll("[role=menuitem]");
		Assert.Null(items[0].GetAttribute("aria-current"));
		Assert.Equal("true", items[1].GetAttribute("aria-current"));
		Assert.NotNull(items[1].QuerySelector(".moka-tab-list-current svg"));
		Assert.Null(items[0].QuerySelector(".moka-tab-list-current"));
	}

	[Fact]
	public void AnEntry_KeepsTheTabsIconBadgeAndPin()
	{
		List<TabInfo<string>> tabs = ThreeTabs();
		tabs[0].IconClass = "doc-icon";
		tabs[1].Badge = new TabBadgeInfo { Count = 4 };
		tabs[2].IsPinned = true;

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(tabs, "a");
		OpenList(cut);

		IReadOnlyList<IElement> items = cut.FindAll("[role=menuitem]");
		Assert.NotNull(items[0].QuerySelector(".moka-tab-list-icon .doc-icon"));
		Assert.Equal("4", items[1].QuerySelector(".moka-tab-badge")!.TextContent.Trim());
		Assert.NotNull(items[2].QuerySelector(".moka-tab-list-pinned svg"));
		Assert.Null(items[0].QuerySelector(".moka-tab-list-pinned"));
		Assert.All(items, item => Assert.Equal("true", item.QuerySelector(".moka-tab-list-icon")!.GetAttribute("aria-hidden")));
	}

	// Titles line up when some tabs have icons; a list without any keeps no empty column.
	[Fact]
	public void WithoutIcons_NoIconColumn()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(ThreeTabs(), "a");
		OpenList(cut);

		Assert.Empty(cut.FindAll(".moka-tab-list-icon"));
	}

	[Fact]
	public void GroupedTabs_AreListedUnderTheirGroupsName_CollapsedOnesToo()
	{
		List<TabInfo<string>> tabs = ThreeTabs();
		tabs[1].GroupName = "servers";
		tabs[2].GroupName = "servers";
		TabGroupInfo servers = new() { Name = "servers", DisplayTitle = "Servers", IsCollapsed = true, Color = "#4fc3f7" };

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(tabs, "a", p => p.Add(x => x.Groups, [servers]));
		OpenList(cut);

		Assert.Equal(["Alpha", "Beta", "Gamma"], Titles(cut));
		IElement group = cut.Find(".moka-tab-list-group");
		Assert.Equal("group", group.GetAttribute("role"));
		Assert.Equal("Servers", group.GetAttribute("aria-label"));
		Assert.Equal(2, group.QuerySelectorAll("[role=menuitem]").Length);
		IElement heading = group.QuerySelector(".moka-tab-list-heading")!;
		Assert.Equal("Servers", heading.TextContent);
		Assert.Equal("true", heading.GetAttribute("aria-hidden"));
		Assert.Contains("--_tab-group-color: #4fc3f7", heading.GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void Picking_ActivatesTheTab_AndRevealsIt()
	{
		List<string> activated = [];
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(ThreeTabs(), "a", p => p
			.Add(x => x.OnTabActivated, id => activated.Add(id)));
		string? strip = cut.Find("[role=tablist]").GetAttribute("blazor:elementReference");
		OpenList(cut);

		cut.FindAll("[role=menuitem]")[2].Click();

		Assert.Equal(["c"], activated);
		Assert.Empty(cut.FindAll("[role=menu]"));
		JSRuntimeInvocation reveal = _module.VerifyInvoke("MokaTabs.revealTab");
		Assert.Equal(strip, Assert.IsType<ElementReference>(reveal.Arguments[0]).Id);
		Assert.Equal("c", reveal.Arguments[1]);
	}

	// The tab showing may still be out of view, so picking it reveals it too.
	[Fact]
	public void PickingTheCurrentTab_RevealsIt()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(ThreeTabs(), "a");
		OpenList(cut);

		cut.FindAll("[role=menuitem]")[0].Click();

		Assert.Equal("a", _module.VerifyInvoke("MokaTabs.revealTab").Arguments[1]);
	}

	[Fact]
	public void PickingATabInACollapsedGroup_OpensTheGroupFirst_AndRevealsOnceItIsDrawn()
	{
		List<string> calls = [];
		List<TabInfo<string>> tabs = ThreeTabs();
		tabs[2].GroupName = "servers";
		TabGroupInfo servers = new() { Name = "servers", IsCollapsed = true };
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(tabs, "a", p => p
			.Add(x => x.Groups, [servers])
			.Add(x => x.OnGroupCollapseToggled, name => calls.Add($"toggle {name}"))
			.Add(x => x.OnTabActivated, id => calls.Add($"activate {id}")));
		OpenList(cut);

		cut.FindAll("[role=menuitem]")[2].Click();

		Assert.Equal(["toggle servers", "activate c"], calls);
		Assert.Empty(_module.Invocations["MokaTabs.revealTab"]);

		servers.IsCollapsed = false;
		cut.Render(p => p.Add(x => x.Groups, [servers]).Add(x => x.ActiveTabId, "c"));

		Assert.Equal("c", _module.VerifyInvoke("MokaTabs.revealTab").Arguments[1]);
		Assert.DoesNotContain(_module.Invocations["MokaTabs.scrollTabIntoView"], call => Equals(call.Arguments[0], "c"));
	}

	[Fact]
	public void AnotherTabBecomingActive_DropsTheReveal()
	{
		List<TabInfo<string>> tabs = ThreeTabs();
		tabs[2].GroupName = "servers";
		TabGroupInfo servers = new() { Name = "servers", IsCollapsed = true };
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(tabs, "a", p => p.Add(x => x.Groups, [servers]));
		OpenList(cut);
		cut.FindAll("[role=menuitem]")[2].Click();

		cut.Render(p => p.Add(x => x.ActiveTabId, "b"));
		servers.IsCollapsed = false;
		cut.Render(p => p.Add(x => x.Groups, [servers]));

		Assert.Empty(_module.Invocations["MokaTabs.revealTab"]);
	}

	[Fact]
	public void APickedTabThatCloses_IsForgotten()
	{
		List<TabInfo<string>> tabs = ThreeTabs();
		tabs[2].GroupName = "servers";
		TabGroupInfo servers = new() { Name = "servers", IsCollapsed = true };
		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(tabs, "a", p => p.Add(x => x.Groups, [servers]));
		OpenList(cut);
		cut.FindAll("[role=menuitem]")[2].Click();

		cut.Render(p => p.Add(x => x.Tabs, tabs.Take(2).ToList()));
		List<TabInfo<string>> reopened = ThreeTabs();
		servers.IsCollapsed = false;
		cut.Render(p => p.Add(x => x.Tabs, reopened).Add(x => x.Groups, [servers]));

		Assert.Empty(_module.Invocations["MokaTabs.revealTab"]);
	}

	[Fact]
	public async Task TheContainer_PassesTheListOn()
	{
		Services.AddMokaTabs<string>();
		IMokaTabSessionState<string> state = Services.GetRequiredService<IMokaTabSessionState<string>>();
		await state.AddTabAsync(new TabInfo<string> { Id = "a", Title = "Alpha" });

		IRenderedComponent<MokaTabContainer<string>> cut = Render<MokaTabContainer<string>>(p => p
			.Add(x => x.ShowTabList, true)
			.Add(x => x.TabListLabel, "Every tab"));

		MokaTabStrip<string> strip = cut.FindComponent<MokaTabStrip<string>>().Instance;
		Assert.True(strip.ShowTabList);
		Assert.Equal("Every tab", strip.TabListLabel);
		Assert.Equal("Every tab", cut.Find(".moka-tab-list-button").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheContainer_LeavesTheListOffByDefault()
	{
		Services.AddMokaTabs<string>();

		IRenderedComponent<MokaTabContainer<string>> cut = Render<MokaTabContainer<string>>();

		Assert.False(cut.FindComponent<MokaTabStrip<string>>().Instance.ShowTabList);
		Assert.Empty(cut.FindAll(".moka-tab-strip-frame"));
	}

	[Fact]
	public void TheButton_ShowsOnlyWhileTheTabsOverflow_OrItsMenuIsOpen()
	{
		Assert.Equal("none", ScopedCss.Declarations(Css, """.moka-tab-strip:not([data-moka-overflow]) + .moka-tab-strip-list:not(:has([aria-expanded="true"]))""")["display"]);
	}

	[Fact]
	public void TheStrip_GivesTheButtonItsRoom()
	{
		IReadOnlyDictionary<string, string> strip = ScopedCss.Declarations(Css, ".moka-tab-strip-frame > .moka-tab-strip");

		Assert.Equal("1 1 auto", strip["flex"]);
		Assert.Equal("0", strip["min-width"]);
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-tab-strip-list")["flex"]);
	}

	[Theory]
	[InlineData(".moka-tab-strip-list", "background", "var(--moka-tab-strip-bg, var(--moka-color-surface-variant))")]
	[InlineData(".moka-gate .moka-tab-strip-list", "background", "var(--moka-tab-strip-bg, var(--moka-color-surface))")]
	public void TheButtonsArea_TakesTheStripsBackground(string selector, string property, string value)
	{
		Assert.Equal(value, ScopedCss.Declarations(Css, selector)[property]);
	}

	[Fact]
	public void ALongList_Scrolls()
	{
		IReadOnlyDictionary<string, string> list = ScopedCss.Declarations(Css, ".moka-tab-list");

		Assert.Equal("auto", list["overflow-y"]);
		Assert.Equal("min(32rem, 60vh)", list["max-height"]);
	}

	private IRenderedComponent<MokaTabStrip<string>> RenderStrip(
		IReadOnlyList<TabInfo<string>> tabs,
		string activeTabId,
		Action<ComponentParameterCollectionBuilder<MokaTabStrip<string>>>? configure = null,
		bool list = true) =>
		Render<MokaTabStrip<string>>(p =>
		{
			p.Add(x => x.Tabs, tabs).Add(x => x.ActiveTabId, activeTabId).Add(x => x.ShowTabList, list);
			configure?.Invoke(p);
		});

	private static void OpenList(IRenderedComponent<MokaTabStrip<string>> cut) =>
		cut.Find(".moka-tab-strip-list .moka-popover-trigger").Click();

	private static List<string> Titles(IRenderedComponent<MokaTabStrip<string>> cut) =>
		cut.FindAll("[role=menuitem] .moka-tab-list-title").Select(title => title.TextContent).ToList();

	private static List<TabInfo<string>> ThreeTabs() =>
	[
		new() { Id = "a", Title = "Alpha" },
		new() { Id = "b", Title = "Beta" },
		new() { Id = "c", Title = "Gamma" }
	];

	private sealed class MarkedIcons : MokaTabIconProvider
	{
		public override RenderFragment TabListIcon => builder =>
		{
			builder.OpenElement(0, "i");
			builder.AddAttribute(1, "class", "marked-list");
			builder.CloseElement();
		};
	}
}
