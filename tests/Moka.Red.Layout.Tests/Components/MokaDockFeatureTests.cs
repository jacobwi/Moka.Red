using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Layout.DockLayout;

namespace Moka.Red.Layout.Tests.Components;

public class MokaDockFeatureTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Layout/DockLayout/MokaDockLayout.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaDockFeatureTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void TheMarkup_DeclaresRowsColumnsAndGroups()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();

		MokaDockSplitNode row = Assert.IsType<MokaDockSplitNode>(cut.Instance.CurrentState.Root);
		Assert.Equal("row", row.Id);
		Assert.Equal("240px", row.Children[0].Size);
		Assert.Equal(["explorer", "search"], Assert.IsType<MokaDockGroupNode>(row.Children[0]).Panels);
		MokaDockSplitNode column = Assert.IsType<MokaDockSplitNode>(row.Children[1]);
		Assert.True(Assert.IsType<MokaDockGroupNode>(column.Children[0]).Content);
		Assert.Equal(3, cut.FindAll("[data-moka-dock-group]").Count);
		Assert.Equal("240px var(--_dock-gutter) minmax(0px, 1fr)",
			MokaDockLayoutTests.StyleValue(cut.Find("[data-moka-dock-split='row']"), "grid-template-columns"));
	}

	[Fact]
	public void Groups_AreRegionsNamedByTheirTitle_ThatOwnTheirShowingPanel()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();

		IElement side = cut.Find("[data-moka-dock-group='side']");
		Assert.Equal("region", side.GetAttribute("role"));
		Assert.Equal("Explorer", side.GetAttribute("aria-label"));
		Assert.Equal("explorer", side.GetAttribute("aria-owns"));
	}

	[Fact]
	public void Tabs_FollowTheTabsPattern()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();

		IElement list = cut.Find("[data-moka-dock-group='side'] [role=tablist]");
		Assert.Equal("Explorer tabs", list.GetAttribute("aria-label"));
		IElement[] tabs = [.. list.QuerySelectorAll("[role=tab]")];
		Assert.Equal(["true", "false"], tabs.Select(t => t.GetAttribute("aria-selected")));
		Assert.Equal(["0", "-1"], tabs.Select(t => t.GetAttribute("tabindex")));
		Assert.Equal("explorer", tabs[0].GetAttribute("aria-controls"));
		IElement host = cut.Find("#explorer");
		Assert.Equal("tabpanel", host.GetAttribute("role"));
		Assert.Equal(tabs[0].Id, host.GetAttribute("aria-labelledby"));
	}

	// Panel content renders once, where it is declared, and is drawn over its group's slot. A move changes only
	// which slot, so the content keeps its component instance and its state.
	[Fact]
	public async Task APanelThatMoves_KeepsItsComponentState()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();
		await cut.Find("[data-probe='explorer']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
		DockProbe probe = Probe(cut, "explorer");

		await cut.InvokeAsync(() => cut.Instance.MovePanelAsync("explorer", new MokaDockTarget("bottom", MokaDockZone.Center)));
		await cut.InvokeAsync(() => cut.Instance.FloatPanelAsync("explorer"));
		await cut.InvokeAsync(() => cut.Instance.AutoHidePanelAsync("explorer", MokaDockPosition.Left));
		await cut.InvokeAsync(() => cut.Instance.ActivatePanelAsync("explorer"));

		Assert.Same(probe, Probe(cut, "explorer"));
		Assert.Equal("1", cut.Find("[data-probe='explorer']").TextContent);
		Assert.Equal(AnchorName(Anchor(cut.Find("[data-moka-dock-flyout] .moka-dock-group__slot"))),
			MokaDockLayoutTests.StyleValue(cut.Find("#explorer"), "position-anchor"));
	}

	[Fact]
	public void AHostIsDrawnOverItsGroupsSlot()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();

		int anchor = Anchor(cut.Find("[data-moka-dock-group='bottom'] .moka-dock-group__slot"));
		Assert.Equal(AnchorName(anchor), MokaDockLayoutTests.StyleValue(cut.Find("#terminal"), "position-anchor"));
		Assert.Equal("bottom", cut.Find("#terminal").GetAttribute("data-moka-dock-in"));
	}

	// A tab switch redraws the group it happens in and the two hosts it shows and hides, not the other groups and not
	// any panel's content.
	[Fact]
	public void ATabSwitch_RedrawsOnlyItsGroup()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();
		cut.Find("[data-moka-dock-tab='problems']").Click();
		cut.Find("[data-moka-dock-tab='terminal']").Click();
		Dictionary<string, int> groups = GroupRenders(cut);
		Dictionary<string, int> probes = ProbeRenders(cut);

		cut.Find("[data-moka-dock-tab='problems']").Click();

		Dictionary<string, int> groupsAfter = GroupRenders(cut);
		Assert.True(groupsAfter["bottom"] > groups["bottom"]);
		Assert.Equal(groups["side"], groupsAfter["side"]);
		Assert.Equal(groups["content"], groupsAfter["content"]);
		Assert.Equal(probes, ProbeRenders(cut));
	}

	[Fact]
	public async Task AResize_RedrawsNoGroup()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();
		Dictionary<string, int> groups = GroupRenders(cut);

		await cut.InvokeAsync(() => cut.Instance.OnSplitResized("row", [300, 700]));

		Assert.Equal("300px", Assert.IsType<MokaDockSplitNode>(cut.Instance.CurrentState.Root).Children[0].Size);
		Assert.Equal(groups, GroupRenders(cut));
	}

	[Fact]
	public void Panels_RenderOnFirstShow_AndStayMounted()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();
		Assert.Empty(cut.FindAll("[data-probe='search']"));

		cut.Find("[data-moka-dock-tab='search']").Click();
		cut.Find("[data-probe='search']").Click();
		cut.Find("[data-moka-dock-tab='explorer']").Click();

		Assert.True(cut.Find("#search").HasAttribute("hidden"));
		Assert.Equal("1", cut.Find("[data-probe='search']").TextContent);
	}

	[Fact]
	public void KeepAliveOff_RemovesTheContentWhileHidden()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Group("g", null,
			DockKit.Panel("a", body: DockKit.Probe("a"), extra: new Dictionary<string, object?> { [nameof(MokaDockPanel.KeepAlive)] = false }),
			DockKit.Panel("b"))));
		Assert.Single(cut.FindAll("[data-probe='a']"));

		cut.Find("[data-moka-dock-tab='b']").Click();

		Assert.Empty(cut.FindAll("[data-probe='a']"));
	}

	// Gutters are window splitters: focusable separators named by what they resize, with a value to read.
	[Fact]
	public void Gutters_AreFocusableSeparators()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Row("r",
			DockKit.Group("a", "1fr", DockKit.Panel("one", "One")),
			DockKit.Group("b", "3fr", DockKit.Panel("two", "Two")))));

		IElement gutter = cut.Find("[data-moka-dock-gutter]");
		Assert.Equal("separator", gutter.GetAttribute("role"));
		Assert.Equal("0", gutter.GetAttribute("tabindex"));
		Assert.Equal("vertical", gutter.GetAttribute("aria-orientation"));
		Assert.Equal("Resize One and Two", gutter.GetAttribute("aria-label"));
		Assert.Equal("25", gutter.GetAttribute("aria-valuenow"));
		Assert.Equal("one", gutter.GetAttribute("aria-controls"));
		Assert.Equal("minmax(0px, 1fr) var(--_dock-gutter) minmax(0px, 3fr)",
			MokaDockLayoutTests.StyleValue(cut.Find("[data-moka-dock-split='r']"), "grid-template-columns"));
	}

	[Fact]
	public void AGroupThatDoesNotResize_HasAFixedGutter()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Row(
			DockKit.Group("a", "200px", new Dictionary<string, object?> { [nameof(MokaDockGroup.Resizable)] = false }, DockKit.Panel("one")),
			DockKit.Group("b", null, DockKit.Panel("two")))));

		IElement gutter = cut.Find(".moka-dock-gutter");
		Assert.Contains("moka-dock-gutter--fixed", gutter.ClassList);
		Assert.Null(gutter.GetAttribute("role"));
		Assert.Null(gutter.GetAttribute("tabindex"));
	}

	[Fact]
	public void TheSplitCarriesItsLimitsForTheScript()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Row("r",
			DockKit.Group("a", "200px", new Dictionary<string, object?> { [nameof(MokaDockGroup.MinSize)] = "120px", [nameof(MokaDockGroup.MaxSize)] = "40%" },
				DockKit.Panel("one")),
			DockKit.Group("b", null, DockKit.Panel("two")))));

		IElement split = cut.Find("[data-moka-dock-split='r']");
		Assert.Equal("120px|", split.GetAttribute("data-moka-dock-min"));
		Assert.Equal("40%|", split.GetAttribute("data-moka-dock-max"));
		Assert.Equal("0|1", split.GetAttribute("data-moka-dock-flex"));
	}

	[Fact]
	public async Task OnSplitResized_WritesSharesInProportion_AndOnSplitResetGoesBack()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Row("r",
			DockKit.Group("a", "1fr", DockKit.Panel("one")),
			DockKit.Group("b", "1fr", DockKit.Panel("two")))));

		await cut.InvokeAsync(() => cut.Instance.OnSplitResized("r", [300, 100]));
		MokaDockSplitNode split = Assert.IsType<MokaDockSplitNode>(cut.Instance.CurrentState.Root);
		Assert.Equal(["1.5fr", "0.5fr"], split.Children.Select(c => c.Size));

		await cut.InvokeAsync(() => cut.Instance.OnSplitReset("r", 0));
		split = Assert.IsType<MokaDockSplitNode>(cut.Instance.CurrentState.Root);
		Assert.Equal(["1fr", "1fr"], split.Children.Select(c => c.Size));
	}

	[Theory]
	[InlineData("panel", "terminal", "side", "center", 1)]
	[InlineData("panel", "terminal", "content", "right", -1)]
	[InlineData("group", "bottom", null, "top", -1)]
	public async Task OnDrop_MovesWhatWasDragged(string kind, string id, string? node, string zone, int index)
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();
		MokaDockPanelChange? moved = null;
		cut.Render(p => p.Add(x => x.PanelMoved, change => moved = change).AddChildContent(DockKit.Ide()));

		await cut.InvokeAsync(() => cut.Instance.OnDrop(kind, id, node, zone, index));

		MokaDockState state = cut.Instance.CurrentState;
		Assert.NotNull(moved);
		switch (zone)
		{
			case "center":
				Assert.Equal(["explorer", "terminal", "search"], state.FindGroup("side")!.Panels);
				Assert.Equal("Terminal moved", cut.Find("[role=status]").TextContent);
				break;
			case "right":
				Assert.NotEqual("bottom", state.Locate("terminal").Group);
				break;
			default:
				MokaDockSplitNode root = Assert.IsType<MokaDockSplitNode>(state.Root);
				Assert.Equal(MokaDockSplitDirection.Column, root.Direction);
				Assert.Equal("bottom", root.Children[0].Id);
				break;
		}
	}

	[Fact]
	public async Task OnDropFloat_FloatsAtThePointer()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();

		await cut.InvokeAsync(() => cut.Instance.OnDropFloat("panel", "search", 30, 40, 360, 280));

		MokaDockFloatingWindow window = Assert.Single(cut.Instance.CurrentState.Floating);
		Assert.Equal((30d, 40d), (window.X, window.Y));
		Assert.Single(cut.FindAll("[data-moka-dock-float]"));
	}

	// An empty grip starts no drag and draws no grab cursor; a floating window keeps its grip, since moving it docks nothing.
	[Fact]
	public async Task DraggingOff_EmptiesTheDockedGrips_AndKeepsTheFloatingWindowsGrip()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p
			.Add(x => x.AllowDragging, false)
			.AddChildContent(DockKit.Ide()));

		await cut.InvokeAsync(() => cut.Instance.FloatPanelAsync("search"));

		IElement window = cut.Find("[data-moka-dock-float]");
		Assert.Equal(window.GetAttribute("data-moka-dock-float"),
			window.QuerySelector("[data-moka-dock-grip]")!.GetAttribute("data-moka-dock-grip"));
		IElement[] docked = [.. cut.FindAll("[data-moka-dock-grip]").Where(grip => grip.Closest("[data-moka-dock-float]") is null)];
		Assert.NotEmpty(docked);
		Assert.All(docked, grip => Assert.Equal(string.Empty, grip.GetAttribute("data-moka-dock-grip")));

		cut.Render(p => p.Add(x => x.AllowDragging, true).AddChildContent(DockKit.Ide()));

		Assert.All(cut.FindAll("[data-moka-dock-grip]"), grip => Assert.NotEqual(string.Empty, grip.GetAttribute("data-moka-dock-grip")));
	}

	[Fact]
	public async Task FloatingOff_RefusesToFloat()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Group("g", null,
			DockKit.Panel("a", extra: new Dictionary<string, object?> { [nameof(MokaDockPanel.Floatable)] = false }))));

		await cut.InvokeAsync(() => cut.Instance.OnDropFloat("group", "g", 0, 0, 300, 200));

		Assert.Empty(cut.Instance.CurrentState.Floating);
		Assert.Empty(cut.FindAll("button[aria-label=Undock]"));
	}

	[Fact]
	public async Task OnFloatFocused_BringsAWindowToTheFront()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();
		await cut.InvokeAsync(() => cut.Instance.FloatPanelAsync("search"));
		await cut.InvokeAsync(() => cut.Instance.FloatPanelAsync("problems"));
		string back = cut.Instance.CurrentState.Floating[0].Group.Id;

		await cut.InvokeAsync(() => cut.Instance.OnFloatFocused(back));

		IElement[] windows = [.. cut.FindAll("[data-moka-dock-float]")];
		Assert.Equal(back, windows[^1].GetAttribute("data-moka-dock-float"));
		Assert.StartsWith("z-index: 4", windows[^1].GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task Maximize_AndEscape_BringItBack()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();

		await cut.Find("[data-moka-dock-group='bottom'] button[aria-label=Maximize]").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
		Assert.Contains("moka-dock-group--maximized", cut.Find("[data-moka-dock-group='bottom']").ClassList);
		Assert.Equal("2", MokaDockLayoutTests.StyleValue(cut.Find("#terminal"), "z-index"));

		await cut.InvokeAsync(() => cut.Instance.OnEscape());

		Assert.DoesNotContain("moka-dock-group--maximized", cut.Find("[data-moka-dock-group='bottom']").ClassList);
	}

	[Fact]
	public async Task OnCycleTab_AndOnCloseKey_WorkTheShowingGroup()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Group("g", null,
			DockKit.Panel("a"), DockKit.Panel("b", extra: new Dictionary<string, object?> { [nameof(MokaDockPanel.Closable)] = true }))));

		await cut.InvokeAsync(() => cut.Instance.OnCycleTab("g", 1));
		Assert.Equal("b", cut.Instance.CurrentState.FindGroup("g")!.Shown);
		await cut.InvokeAsync(() => cut.Instance.OnCycleTab("g", 1));
		Assert.Equal("a", cut.Instance.CurrentState.FindGroup("g")!.Shown);

		await cut.InvokeAsync(() => cut.Instance.OnCloseKey("a"));
		Assert.Equal(MokaDockPlace.Docked, cut.Instance.CurrentState.Locate("a").Place);
		await cut.InvokeAsync(() => cut.Instance.OnCloseKey("b"));
		Assert.Equal(MokaDockPlace.Hidden, cut.Instance.CurrentState.Locate("b").Place);
		Assert.False(cut.Find("#a").HasAttribute("hidden"));
	}

	[Fact]
	public async Task AutoHide_PutsTabsInASideBar_ThatOpenAFlyout()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();

		await cut.InvokeAsync(() => cut.Instance.AutoHidePanelAsync("terminal", MokaDockPosition.Bottom));
		IElement bar = cut.Find(".moka-dock-sidebar--bottom");
		Assert.Equal("toolbar", bar.GetAttribute("role"));
		Assert.Equal(["Terminal", "Problems"], bar.QuerySelectorAll("button").Select(b => b.TextContent.Trim()));
		Assert.True(cut.Find("#terminal").HasAttribute("hidden"));

		await cut.InvokeAsync(() => cut.Instance.OnFlyoutHover("bottom"));
		Assert.Single(cut.FindAll("[data-moka-dock-flyout]"));
		Assert.False(cut.Find("#terminal").HasAttribute("hidden"));
		Assert.Equal("true", cut.Find(".moka-dock-sidebar__tab").GetAttribute("aria-expanded"));

		await cut.InvokeAsync(() => cut.Instance.OnFlyoutDismiss());
		Assert.Empty(cut.FindAll("[data-moka-dock-flyout]"));
	}

	[Fact]
	public void TheMenu_OffersEveryMoveFromTheKeyboard()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();

		cut.Find("[data-moka-dock-group='bottom'] button[aria-label='Panel options']").Click();

		string[] items = [.. cut.FindAll(".moka-dropdown-menu .moka-dropdown-item").Select(i => i.TextContent.Trim())];
		Assert.Contains("Undock", items);
		Assert.Contains("Auto-hide", items);
		Assert.Contains("Dock left", items);
		Assert.Contains("Move to Explorer", items);
		Assert.Contains("Move to Content", items);
	}

	[Theory]
	[InlineData(MokaDockTabStyle.Underline, "moka-dock-tabs--underline")]
	[InlineData(MokaDockTabStyle.Editor, "moka-dock-tabs--editor")]
	[InlineData(MokaDockTabStyle.Pills, "moka-dock-tabs--pills")]
	public void TabStyles_DrawTheirStrip(MokaDockTabStyle style, string cssClass)
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.Add(x => x.TabStyle, style).AddChildContent(DockKit.Ide()));

		Assert.Contains(cssClass, cut.Find("[data-moka-dock-group='side'] [role=tablist]").ClassList);
	}

	[Fact]
	public void TheSwitcher_ShowsTheTitleAndAListOfTheOthers()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.Add(x => x.TabStyle, MokaDockTabStyle.Switcher).AddChildContent(DockKit.Ide()));

		Assert.Empty(cut.FindAll("[role=tablist]"));
		Assert.Contains("Explorer", cut.Find("[data-moka-dock-group='side'] .moka-dock-switcher__button").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void AGroup_CanSetItsOwnTabStyle()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Group("g", null,
			new Dictionary<string, object?> { [nameof(MokaDockGroup.TabStyle)] = MokaDockTabStyle.Pills }, DockKit.Panel("a"), DockKit.Panel("b"))));

		Assert.Contains("moka-dock-tabs--pills", cut.Find("[role=tablist]").ClassList);
	}

	[Theory]
	[InlineData(MokaDockLook.Ide, "moka-dock-layout--ide")]
	[InlineData(MokaDockLook.Cards, "moka-dock-layout--cards")]
	[InlineData(MokaDockLook.Minimal, "moka-dock-layout--minimal")]
	[InlineData(MokaDockLook.Terminal, "moka-dock-layout--terminal")]
	public void Looks_PutTheirClassOnTheRoot_OnTheNewDesign(MokaDockLook look, string cssClass)
	{
		IRenderedComponent<MokaDockLayout> fromParameter = RenderThemed(MokaTheme.Default, look);
		IRenderedComponent<MokaDockLayout> fromTheme = RenderThemed(MokaTheme.Default.WithDocks(look), null);

		Assert.Contains(cssClass, fromParameter.Find(".moka-dock-layout").ClassList);
		Assert.Contains(cssClass, fromTheme.Find(".moka-dock-layout").ClassList);
	}

	[Fact]
	public void TheOriginalDesign_DrawsClassic_WhateverItIsAsked()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderThemed(MokaTheme.Dark, MokaDockLook.Ide);

		Assert.Equal(["moka-dock-layout"], cut.Find(".moka-dock-layout").ClassList);
		Assert.Empty(cut.FindAll(".moka-dock-activity"));
	}

	[Fact]
	public async Task TheIdeLook_ListsHiddenAndPinnedPanelsInItsActivityBar()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderThemed(MokaTheme.Default, MokaDockLook.Ide);
		MokaDockLayout layout = cut.Instance;

		await cut.InvokeAsync(() => layout.ClosePanelAsync("search", force: true));
		await cut.InvokeAsync(() => layout.AutoHidePanelAsync("terminal", MokaDockPosition.Bottom));

		IElement bar = cut.Find(".moka-dock-activity");
		Assert.Equal(["Terminal", "Problems", "Search"], bar.QuerySelectorAll("button").Select(b => b.GetAttribute("aria-label")));
		Assert.Empty(cut.FindAll(".moka-dock-sidebar"));
	}

	[Fact]
	public async Task NarrowSheets_FillWithTheMainArea_AndOpenOtherPanelsAsSheets()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();

		await cut.InvokeAsync(() => cut.Instance.OnNarrowChanged(true));

		Assert.Single(cut.FindAll("[data-moka-dock-mode='main']"));
		IElement[] buttons = [.. cut.FindAll(".moka-dock-narrow-bar button")];
		Assert.Equal(["Explorer", "Search", "Terminal", "Problems"], buttons.Select(b => b.TextContent.Trim()));
		Assert.True(cut.Find("#terminal").HasAttribute("hidden"));

		await buttons[2].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

		IElement sheet = cut.Find("[role=dialog]");
		Assert.NotNull(sheet.QuerySelector("[data-probe='terminal']"));
		Assert.Equal("true", cut.FindAll(".moka-dock-narrow-bar button")[2].GetAttribute("aria-expanded"));
	}

	[Fact]
	public async Task NarrowTabs_PutEveryPanelInOneGroup_WithTheMainAreaFirst()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.Add(x => x.NarrowMode, MokaDockNarrowMode.Tabs).AddChildContent(DockKit.Ide()));

		await cut.InvokeAsync(() => cut.Instance.OnNarrowChanged(true));

		IElement group = cut.Find("[data-moka-dock-mode='narrow']");
		Assert.Equal(["Content", "Explorer", "Search", "Terminal", "Problems"],
			group.QuerySelectorAll("[role=tab]").Select(t => t.TextContent.Trim()));
		IElement terminal = cut.Find("#terminal");
		Assert.Equal(AnchorName(Anchor(group.QuerySelector(".moka-dock-group__slot")!)),
			MokaDockLayoutTests.StyleValue(terminal, "position-anchor"));

		await cut.InvokeAsync(() => cut.Instance.OnNarrowChanged(false));
		Assert.Empty(cut.FindAll("[data-moka-dock-mode='narrow']"));
	}

	[Fact]
	public async Task NarrowStack_PutsGroupsInAColumn_EachFoldingOpenAndShut()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.Add(x => x.NarrowMode, MokaDockNarrowMode.Stack).AddChildContent(DockKit.Ide()));

		await cut.InvokeAsync(() => cut.Instance.OnNarrowChanged(true));

		Assert.Equal(3, cut.FindAll("[data-moka-dock-mode='stack']").Count);
		IElement toggle = cut.Find("[data-moka-dock-group='bottom'] .moka-dock-stack-toggle");
		Assert.Equal("true", toggle.GetAttribute("aria-expanded"));

		await toggle.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

		Assert.Equal("false", cut.Find("[data-moka-dock-group='bottom'] .moka-dock-stack-toggle").GetAttribute("aria-expanded"));
		Assert.True(cut.Find("#terminal").HasAttribute("hidden"));
	}

	[Fact]
	public async Task StorageKey_RestoresSaves_AndResetForgets()
	{
		FakeStorage storage = new();
		storage.Values["ide"] = RenderIdeState().Move("terminal", new MokaDockTarget("side", MokaDockZone.Center)).ToJson();
		Services.AddSingleton<IMokaDockStorage>(storage);

		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.Add(x => x.StorageKey, "ide").AddChildContent(DockKit.Ide()));

		Assert.Equal("side", cut.Instance.CurrentState.Locate("terminal").Group);

		await cut.InvokeAsync(() => cut.Instance.MovePanelAsync("terminal", new MokaDockTarget("bottom", MokaDockZone.Center)));
		Assert.Equal("bottom", MokaDockState.FromJson(storage.Values["ide"])!.Locate("terminal").Group);

		await cut.InvokeAsync(() => cut.Instance.ResetAsync());
		Assert.False(storage.Values.ContainsKey("ide"));
		Assert.Equal("bottom", cut.Instance.CurrentState.Locate("terminal").Group);
	}

	[Fact]
	public void StorageKey_WithoutAStorage_UsesTheBrowsers()
	{
		_module.Setup<string?>("readStorage", _ => true).SetResult(null);

		Render<MokaDockLayout>(p => p.Add(x => x.StorageKey, "ide").AddChildContent(DockKit.Ide()));

		Assert.Equal("ide", Assert.Single(_module.Invocations["readStorage"]).Arguments[0]);
	}

	[Fact]
	public void MigrateState_BringsAnOlderSavedLayoutUpToDate()
	{
		FakeStorage storage = new();
		MokaDockState old = RenderIdeState().Move("problems", new MokaDockTarget("side", MokaDockZone.Center)) with { AppVersion = 1 };
		storage.Values["ide"] = old.ToJson().Replace("\"problems\"", "\"issues\"", StringComparison.Ordinal);
		Services.AddSingleton<IMokaDockStorage>(storage);
		int? seen = null;

		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p
			.Add(x => x.StorageKey, "ide")
			.Add(x => x.StateVersion, 2)
			.Add(x => x.MigrateState, (saved, version) =>
			{
				seen = version;
				return MokaDockState.FromJson(saved.ToJson().Replace("\"issues\"", "\"problems\"", StringComparison.Ordinal));
			})
			.AddChildContent(DockKit.Ide()));

		Assert.Equal(1, seen);
		Assert.Equal("side", cut.Instance.CurrentState.Locate("problems").Group);
	}

	[Fact]
	public void ASavedLayoutFromANewerVersion_IsLeftAlone()
	{
		FakeStorage storage = new();
		storage.Values["ide"] = (RenderIdeState().Move("problems", new MokaDockTarget("side", MokaDockZone.Center)) with { AppVersion = 5 }).ToJson();
		Services.AddSingleton<IMokaDockStorage>(storage);

		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.Add(x => x.StorageKey, "ide").Add(x => x.StateVersion, 2).AddChildContent(DockKit.Ide()));

		Assert.Equal("bottom", cut.Instance.CurrentState.Locate("problems").Group);
	}

	[Fact]
	public async Task State_IsBindable()
	{
		MokaDockState? bound = null;
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p
			.Add(x => x.StateChanged, state => bound = state)
			.AddChildContent(DockKit.Ide()));

		await cut.InvokeAsync(() => cut.Instance.MovePanelAsync("search", new MokaDockTarget("bottom", MokaDockZone.Center)));
		Assert.Same(cut.Instance.CurrentState, bound);

		MokaDockState next = bound!.Move("search", new MokaDockTarget("side", MokaDockZone.Center));
		cut.Render(p => p.Add(x => x.State, next).AddChildContent(DockKit.Ide()));

		Assert.Equal("side", cut.Instance.CurrentState.Locate("search").Group);
		Assert.Equal("side", cut.Find("#search").GetAttribute("data-moka-dock-in"));
	}

	[Fact]
	public async Task OpenPanelAsync_OpensADocumentInTheMainArea()
	{
		MokaDockPanelChange? opened = null;
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.Add(x => x.PanelOpened, change => opened = change).AddChildContent(DockKit.Ide()));

		await cut.InvokeAsync(() => cut.Instance.OpenPanelAsync(new MokaDockPanelOptions
		{
			Id = "doc",
			Title = "Readme",
			Component = MokaDockComponent.Create<DockProbe>(new Dictionary<string, object?> { [nameof(DockProbe.Name)] = "doc" })
		}));

		Assert.Equal(["doc"], cut.Instance.CurrentState.FindGroup("content")!.Panels);
		Assert.Equal("content", opened!.Location.Group);
		Assert.NotNull(cut.Find("#doc [data-probe='doc']"));
		Assert.True(cut.Find("[data-moka-dock-host='moka-dock-content']").HasAttribute("hidden"));
		Assert.Equal("Readme", cut.Find("[data-moka-dock-group='content'] [role=tab]").TextContent.Trim());
	}

	[Fact]
	public async Task ClosePanelAsync_AsksCanClose_AndForgetsADocument()
	{
		bool allow = false;
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Ide()));
		await cut.InvokeAsync(() => cut.Instance.OpenPanelAsync(new MokaDockPanelOptions
		{
			Id = "doc",
			Title = "Readme",
			Content = DockKit.Text("text"),
			CanClose = () => Task.FromResult(allow)
		}));

		Assert.False(await cut.InvokeAsync(() => cut.Instance.ClosePanelAsync("doc")));
		Assert.Equal(MokaDockPlace.Docked, cut.Instance.CurrentState.Locate("doc").Place);

		allow = true;
		Assert.True(await cut.InvokeAsync(() => cut.Instance.ClosePanelAsync("doc")));
		Assert.Equal(MokaDockPlace.None, cut.Instance.CurrentState.Locate("doc").Place);
		Assert.Empty(cut.FindAll("#doc"));
		Assert.False(cut.Find("[data-moka-dock-host='moka-dock-content']").HasAttribute("hidden"));
	}

	[Fact]
	public async Task ADeclaredPanelThatCloses_CanBeShownAgain()
	{
		MokaDockPanelChange? closed = null;
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.Add(x => x.PanelClosed, change => closed = change).AddChildContent(DockKit.Ide()));

		await cut.InvokeAsync(() => cut.Instance.ClosePanelAsync("problems"));
		Assert.Equal(MokaDockPlace.Hidden, closed!.Location.Place);
		Assert.True(cut.Find("#problems").HasAttribute("hidden"));

		await cut.InvokeAsync(() => cut.Instance.ShowPanelAsync("problems"));
		Assert.Equal("bottom", cut.Instance.CurrentState.Locate("problems").Group);
	}

	[Fact]
	public void APanelThatStartsHidden_IsClosed()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Group("g", null,
			DockKit.Panel("a"), DockKit.Panel("b", extra: new Dictionary<string, object?> { [nameof(MokaDockPanel.Visible)] = false }))));

		Assert.Equal(MokaDockPlace.Hidden, cut.Instance.CurrentState.Locate("b").Place);
		Assert.Empty(cut.FindAll("[data-moka-dock-tab='b']"));
	}

	[Fact]
	public async Task ApplyPresetAsync_SwitchesLayout_WhenTheBrowserHasNoTransition()
	{
		MokaDockState focus = new() { Root = new MokaDockGroupNode { Id = "all", Panels = ["terminal", "explorer"] } };
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p
			.Add(x => x.Presets, [new MokaDockPreset("focus", focus)])
			.AddChildContent(DockKit.Ide()));

		Assert.True(await cut.InvokeAsync(() => cut.Instance.ApplyPresetAsync("focus")));
		Assert.False(await cut.InvokeAsync(() => cut.Instance.ApplyPresetAsync("missing")));

		Assert.Equal("all", cut.Instance.CurrentState.Locate("terminal").Group);
		Assert.NotEqual(MokaDockPlace.None, cut.Instance.CurrentState.Locate("problems").Place);
	}

	[Fact]
	public async Task ApplyPresetAsync_LeavesTheApplyToTheTransition_WhichAppliesOnceThePageShowsIt()
	{
		MokaDockState focus = new() { Root = new MokaDockGroupNode { Id = "all", Panels = ["terminal", "explorer"] } };
		_module.Setup<bool>("transition", _ => true).SetResult(true);
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p
			.Add(x => x.Presets, [new MokaDockPreset("focus", focus)])
			.AddChildContent(DockKit.Ide()));

		Assert.True(await cut.InvokeAsync(() => cut.Instance.ApplyPresetAsync("focus")));
		Assert.Equal("focus", Assert.Single(_module.Invocations["transition"]).Arguments[2]);
		Assert.Equal("bottom", cut.Instance.CurrentState.Locate("terminal").Group);

		await cut.InvokeAsync(() => cut.Instance.ApplyPresetNowAsync("focus"));

		Assert.Equal("all", cut.Instance.CurrentState.Locate("terminal").Group);
		Assert.Equal(AnchorName(Anchor(cut.Find("[data-moka-dock-group='all'] .moka-dock-group__slot"))),
			MokaDockLayoutTests.StyleValue(cut.Find("#terminal"), "position-anchor"));
	}

	// A preset or Reset has no place for a document opened from code: it stays open in the main area, drawn there,
	// and nothing reports it closed.
	[Fact]
	public async Task PresetsAndReset_KeepPanelsOpenedFromCode()
	{
		List<string> closed = [];
		MokaDockState focus = new() { Root = new MokaDockGroupNode { Id = "all", Panels = ["terminal", "explorer"] } };
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p
			.Add(x => x.Presets, [new MokaDockPreset("focus", focus)])
			.Add(x => x.PanelClosed, change => closed.Add(change.Panel))
			.AddChildContent(DockKit.Ide()));
		await cut.InvokeAsync(() => cut.Instance.OpenPanelAsync(new MokaDockPanelOptions { Id = "doc", Title = "Readme", Content = DockKit.Text("text") }));

		await cut.InvokeAsync(() => cut.Instance.ApplyPresetAsync("focus"));

		Assert.Equal("content", cut.Instance.CurrentState.Locate("doc").Group);
		Assert.False(cut.Find("#doc").HasAttribute("hidden"));
		Assert.Equal(AnchorName(Anchor(cut.Find("[data-moka-dock-group='content'] .moka-dock-group__slot"))),
			MokaDockLayoutTests.StyleValue(cut.Find("#doc"), "position-anchor"));

		await cut.InvokeAsync(() => cut.Instance.ResetAsync());

		Assert.Equal("content", cut.Instance.CurrentState.Locate("doc").Group);
		Assert.Equal("bottom", cut.Instance.CurrentState.Locate("terminal").Group);
		Assert.Empty(closed);
	}

	[Fact]
	public async Task OpenPanelAsync_AgainOnAnOpenPanel_TakesItsNewOptions()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();
		await cut.InvokeAsync(() => cut.Instance.OpenPanelAsync(new MokaDockPanelOptions { Id = "doc", Title = "Readme", Content = DockKit.Text("text") }));

		await cut.InvokeAsync(() => cut.Instance.OpenPanelAsync(new MokaDockPanelOptions { Id = "doc", Title = "Readme (edited)", Content = DockKit.Text("edited") }));

		Assert.Equal("Readme (edited)", cut.Find("[data-moka-dock-group='content'] [role=tab]").TextContent.Trim());
		Assert.Equal("edited", cut.Find("#doc").TextContent.Trim());
	}

	// Where a host is drawn depends on the narrow mode, so a new mode draws every host, a document's included, over
	// the slots that mode has.
	[Fact]
	public async Task ANarrowModeChange_DrawsEveryHostOverItsNewSlot()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.Add(x => x.NarrowMode, MokaDockNarrowMode.Tabs).AddChildContent(DockKit.Ide()));
		await cut.InvokeAsync(() => cut.Instance.OpenPanelAsync(new MokaDockPanelOptions { Id = "doc", Title = "Readme", Content = DockKit.Text("text") }));
		await cut.InvokeAsync(() => cut.Instance.OnNarrowChanged(true));

		cut.Render(p => p.Add(x => x.NarrowMode, MokaDockNarrowMode.Stack).AddChildContent(DockKit.Ide()));

		Assert.Equal(AnchorName(Anchor(cut.Find("[data-moka-dock-group='content'] .moka-dock-group__slot"))),
			MokaDockLayoutTests.StyleValue(cut.Find("#doc"), "position-anchor"));
		Assert.Equal(AnchorName(Anchor(cut.Find("[data-moka-dock-group='bottom'] .moka-dock-group__slot"))),
			MokaDockLayoutTests.StyleValue(cut.Find("#terminal"), "position-anchor"));

		cut.Render(p => p.Add(x => x.NarrowMode, MokaDockNarrowMode.Tabs).AddChildContent(DockKit.Ide()));

		string narrow = AnchorName(Anchor(cut.Find("[data-moka-dock-mode='narrow'] .moka-dock-group__slot")));
		Assert.Equal(narrow, MokaDockLayoutTests.StyleValue(cut.Find("#doc"), "position-anchor"));
		Assert.Equal(narrow, MokaDockLayoutTests.StyleValue(cut.Find("#terminal"), "position-anchor"));
	}

	[Fact]
	public async Task NewWindows_FitAShortLayout()
	{
		_module.Setup<double[]>("measureMain", _ => true).SetResult([500, 200]);
		IRenderedComponent<MokaDockLayout> cut = RenderIde();

		await cut.InvokeAsync(() => cut.Instance.FloatPanelAsync("terminal"));
		await cut.Find("[data-moka-dock-group='side'] button[aria-label=Undock]").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

		Assert.Equal(2, cut.Instance.CurrentState.Floating.Count);
		foreach (MokaDockFloatingWindow window in cut.Instance.CurrentState.Floating)
		{
			Assert.Equal((360, 200), (window.Width, window.Height));
			Assert.Equal(0, window.Y);
			Assert.True(window.X >= 0 && window.X + window.Width <= 500);
		}
	}

	[Fact]
	public async Task OnEscape_WithNothingToUndo_ChangesAndDrawsNothing()
	{
		int changes = 0;
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.Add(x => x.StateChanged, _ => changes++).AddChildContent(DockKit.Ide()));
		IRenderedComponent<MokaDockFrame> frame = cut.FindComponent<MokaDockFrame>();
		int renders = frame.RenderCount;
		MokaDockState before = cut.Instance.CurrentState;

		await cut.InvokeAsync(() => cut.Instance.OnEscape());

		Assert.Equal(renders, frame.RenderCount);
		Assert.Same(before, cut.Instance.CurrentState);
		Assert.Equal(0, changes);
	}

	[Fact]
	public async Task PopOut_OpensAWindowWhoseSessionShowsThePanel_AndClosingItBringsThePanelBack()
	{
		Services.AddMokaDockWindows();
		SetRendererInfo(new RendererInfo("Server", true));
		_module.Setup<bool>("openWindow", _ => true).SetResult(true);
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Group("g", null,
			DockKit.Panel("a", extra: new Dictionary<string, object?>
			{
				[nameof(MokaDockPanel.Component)] = MokaDockComponent.Create<DockProbe>(new Dictionary<string, object?> { [nameof(DockProbe.Name)] = "a" })
			}),
			DockKit.Panel("b"))));
		await cut.InvokeAsync(() => cut.Instance.OnPopOutSupport(true));

		Assert.True(await cut.InvokeAsync(() => cut.Instance.PopOutPanelAsync("a")));
		Assert.False(await cut.InvokeAsync(() => cut.Instance.PopOutPanelAsync("b")));

		Assert.Equal(MokaDockPlace.PoppedOut, cut.Instance.CurrentState.Locate("a").Place);
		string url = (string)Assert.Single(_module.Invocations["openWindow"]).Arguments[1]!;
		string key = url[(url.IndexOf("session=", StringComparison.Ordinal) + 8)..];
		MokaDockWindowSession session = Services.GetRequiredService<MokaDockWindows>().Find(key)!;
		Assert.Equal("a", session.Panel);

		Services.GetRequiredService<NavigationManager>().NavigateTo("moka-dock-window?session=" + key);
		IRenderedComponent<MokaDockWindowPage> page = Render<MokaDockWindowPage>();
		Assert.NotNull(page.Find("[data-probe='a']"));

		await cut.InvokeAsync(() => cut.Instance.OnWindowClosed("a"));
		Assert.Equal("g", cut.Instance.CurrentState.Locate("a").Group);
		Assert.True(session.Ended);
	}

	[Fact]
	public async Task PopOut_WithoutTheService_IsNotOffered()
	{
		SetRendererInfo(new RendererInfo("Server", true));
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Group("g", null,
			DockKit.Panel("a", extra: new Dictionary<string, object?> { [nameof(MokaDockPanel.Component)] = MokaDockComponent.Create<DockProbe>() }))));

		await cut.InvokeAsync(() => cut.Instance.OnPopOutSupport(true));

		Assert.False(await cut.InvokeAsync(() => cut.Instance.PopOutPanelAsync("a")));
	}

	[Fact]
	public void TheDropOverlay_IsATopLayerPopover()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderIde();

		IElement drop = cut.Find("[data-moka-dock-drop]");
		Assert.Equal("manual", drop.GetAttribute("popover"));
		Assert.Equal("true", drop.GetAttribute("aria-hidden"));
	}

	[Fact]
	public void APanelOutsideALayout_DrawsAsAPlainBox()
	{
		IRenderedComponent<MokaDockPanel> cut = Render<MokaDockPanel>(p => p.Add(x => x.Title, "Alone").AddChildContent("body"));

		Assert.Equal("Alone", cut.Find(".moka-dock-panel > .moka-dock-panel-header > .moka-dock-panel-title").TextContent);
		Assert.Equal("body", cut.Find(".moka-dock-panel-body").TextContent);
	}

	private IRenderedComponent<MokaDockLayout> RenderIde() => Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Ide()));

	private static MokaDockState RenderIdeState()
	{
		using BunitContext context = new();
		context.JSInterop.Mode = JSRuntimeMode.Loose;
		return context.Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Ide())).Instance.CurrentState;
	}

	private IRenderedComponent<MokaDockLayout> RenderThemed(MokaTheme theme, MokaDockLook? look)
	{
		IRenderedComponent<CascadingValue<MokaTheme>> host = Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, theme)
			.AddChildContent<MokaDockLayout>(layout => layout.Add(x => x.Look, look).AddChildContent(DockKit.Ide())));
		return host.FindComponent<MokaDockLayout>();
	}

	private static DockProbe Probe(IRenderedComponent<MokaDockLayout> cut, string name) =>
		cut.FindComponents<DockProbe>().Single(p => p.Instance.Name == name).Instance;

	private static string AnchorName(int anchor) => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"--moka-dock-{anchor}");

	private static int Anchor(IElement slot) => int.Parse(slot.GetAttribute("data-moka-dock-slot")!, System.Globalization.CultureInfo.InvariantCulture);

	private static Dictionary<string, int> GroupRenders(IRenderedComponent<MokaDockLayout> cut) =>
		cut.FindComponents<MokaDockGroupView>().ToDictionary(g => g.Instance.Group.Id, g => g.RenderCount);

	private static Dictionary<string, int> ProbeRenders(IRenderedComponent<MokaDockLayout> cut) =>
		cut.FindComponents<DockProbe>().ToDictionary(p => p.Instance.Name!, p => p.RenderCount);

	private sealed class FakeStorage : IMokaDockStorage
	{
		public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);

		public Task SaveAsync(string key, string value)
		{
			Values[key] = value;
			return Task.CompletedTask;
		}

		public Task<string?> LoadAsync(string key) => Task.FromResult(Values.GetValueOrDefault(key));

		public Task RemoveAsync(string key)
		{
			Values.Remove(key);
			return Task.CompletedTask;
		}
	}
}
