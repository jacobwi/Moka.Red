using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Interactions;
using Moka.Red.Navigation.Tabs;
using Moka.Red.Navigation.Tabs.Models;
using Moka.Red.Navigation.Tabs.Plugins;

namespace Moka.Red.Navigation.Tests.Components;

// Arrow keys, Enter, Space and Delete are wired in moka-tabs.js. These tests cover what the strip
// renders for them and for screen readers, and the .NET side of each path.
public class MokaTabStripTests : BunitContext
{
	private const string TabsModule = "./_content/Moka.Red.Navigation/moka-tabs.js";

	[Fact]
	public void Tabs_ReportTheirStateAsStrings()
	{
		SetupTabsModule();

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(TwoTabs(), "a");

		IElement alpha = Tab(cut, "a");
		IElement beta = Tab(cut, "b");
		Assert.Equal("true", alpha.GetAttribute("aria-selected"));
		Assert.Equal("false", beta.GetAttribute("aria-selected"));
		Assert.Equal("0", alpha.GetAttribute("tabindex"));
		Assert.Equal("-1", beta.GetAttribute("tabindex"));
	}

	// draggable is an enumerated attribute: an empty value means "auto", which a div is not.
	[Fact]
	public void DraggableTabs_SayTrue()
	{
		SetupTabsModule();
		List<TabInfo<string>> tabs = TwoTabs();
		tabs[1].IsDraggable = false;

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(tabs, "a");

		Assert.Equal("true", Tab(cut, "a").GetAttribute("draggable"));
		Assert.False(Tab(cut, "b").HasAttribute("draggable"));
	}

	[Fact]
	public void TabButtons_StayOutOfTheTabOrderAndTheTabsName()
	{
		SetupTabsModule();

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(TwoTabs(), "a");

		IElement alpha = Tab(cut, "a");
		foreach (IElement button in alpha.QuerySelectorAll(".moka-tab-close, .moka-tab-pin"))
		{
			Assert.Equal("-1", button.GetAttribute("tabindex"));
			Assert.Equal("true", button.GetAttribute("aria-hidden"));
		}

		Assert.Equal(2, alpha.QuerySelectorAll("button").Length);
	}

	[Fact]
	public async Task OnTabContextMenu_ReplacesTheBuiltInMenu()
	{
		SetupTabsModule();
		MokaItemContextMenuArgs<TabInfo<string>>? received = null;

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(TwoTabs(), "a", p => p
			.Add(x => x.OnTabContextMenu, args => received = args));

		IElement beta = Tab(cut, "b");
		Assert.True(beta.HasAttribute("blazor:oncontextmenu:preventDefault"));

		await beta.ContextMenuAsync(new MouseEventArgs { ClientX = 40, ClientY = 12 });

		Assert.NotNull(received);
		Assert.Equal("b", received.Item.Id);
		Assert.Equal(40, received.MouseEvent.ClientX);
		Assert.Empty(cut.FindAll(".moka-context-menu"));
	}

	[Fact]
	public void WithNoMenuAtAll_TheBrowsersMenuIsLeftAlone()
	{
		SetupTabsModule();

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(TwoTabs(), "a", p => p
			.Add(x => x.AllowContextMenu, false));

		Assert.False(Tab(cut, "a").HasAttribute("blazor:oncontextmenu:preventDefault"));
	}

	[Fact]
	public async Task TheBuiltInMenu_ListsPluginItemsBeforeCustomItems()
	{
		SetupTabsModule();
		var registry = new MokaTabPluginRegistry([new MenuPlugin()]);
		ContextMenuItem custom = new() { Text = "Custom", OnClick = () => Task.CompletedTask };

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(TwoTabs(), "a", p => p
			.Add(x => x.PluginRegistry, registry)
			.Add(x => x.CustomContextMenuItems, [custom]));

		await Tab(cut, "b").ContextMenuAsync(new MouseEventArgs());

		IElement menu = cut.Find("[role=menu]");
		var labels = menu.QuerySelectorAll("[role=menuitem]").Select(item => item.TextContent.Trim()).ToList();
		Assert.Equal(["From plugin: Beta", "Custom"], labels.TakeLast(2));
	}

	// A menu item that awaits a dialog used to keep the menu open over the dialog until it finished.
	[Fact]
	public async Task TheBuiltInMenu_ClosesBeforeAnItemsActionRuns()
	{
		SetupTabsModule();
		var pending = new TaskCompletionSource();
		ContextMenuItem slow = new() { Text = "Slow", OnClick = () => pending.Task };

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(TwoTabs(), "a", p => p
			.Add(x => x.CustomContextMenuItems, [slow]));

		await Tab(cut, "a").ContextMenuAsync(new MouseEventArgs());
		IElement item = cut.FindAll("[role=menuitem]").Single(b => b.TextContent.Trim() == "Slow");
		Task click = item.ClickAsync(new MouseEventArgs());

		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".moka-context-menu")));
		Assert.False(click.IsCompleted);

		pending.SetResult();
		await click;
	}

	[Fact]
	public async Task Escape_ClosesTheBuiltInMenu()
	{
		SetupTabsModule();

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(TwoTabs(), "a");

		await Tab(cut, "a").ContextMenuAsync(new MouseEventArgs());
		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Empty(cut.FindAll(".moka-context-menu"));
	}

	[Fact]
	public async Task AMiddleClick_ClosesAClosableTab()
	{
		SetupTabsModule();
		var closed = new List<string>();
		List<TabInfo<string>> tabs = TwoTabs();
		tabs.Add(new TabInfo<string> { Id = "pinned", Title = "Pinned", IsPinned = true });

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(tabs, "a", p => p
			.Add(x => x.OnTabClosed, id => closed.Add(id)));

		await Tab(cut, "a").MouseUpAsync(new MouseEventArgs { Button = 0 });
		await Tab(cut, "b").MouseUpAsync(new MouseEventArgs { Button = 1 });
		await Tab(cut, "pinned").MouseUpAsync(new MouseEventArgs { Button = 1 });

		Assert.Equal(["b"], closed);
	}

	[Fact]
	public async Task CloseOnMiddleClickFalse_KeepsTheTab()
	{
		SetupTabsModule();
		var closed = new List<string>();

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(TwoTabs(), "a", p => p
			.Add(x => x.CloseOnMiddleClick, false)
			.Add(x => x.OnTabClosed, id => closed.Add(id)));

		await Tab(cut, "b").MouseUpAsync(new MouseEventArgs { Button = 1 });

		Assert.Empty(closed);
	}

	// moka-tabs.js calls this for Delete on a focused tab.
	[Fact]
	public async Task TheDeleteKeyPath_ClosesOnlyATabThatCanClose()
	{
		SetupTabsModule();
		var closed = new List<string>();
		List<TabInfo<string>> tabs = TwoTabs();
		tabs[1].IsClosable = false;

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(tabs, "a", p => p
			.Add(x => x.ShowCloseButton, false)
			.Add(x => x.OnTabClosed, id => closed.Add(id)));

		await cut.InvokeAsync(() => cut.Instance.CloseTabFromKeyboard("a"));
		await cut.InvokeAsync(() => cut.Instance.CloseTabFromKeyboard("b"));
		await cut.InvokeAsync(() => cut.Instance.CloseTabFromKeyboard("missing"));

		Assert.Equal(["a"], closed);
	}

	[Fact]
	public void TheStrip_BindsItsKeyboardHandlingToItsOwnElement()
	{
		BunitJSModuleInterop module = SetupTabsModule();

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(TwoTabs(), "a");

		JSRuntimeInvocation bind = module.VerifyInvoke("MokaTabs.bindTabStrip");
		ElementReference strip = Assert.IsType<ElementReference>(bind.Arguments[0]);
		Assert.Equal(strip.Id, cut.Find("[role=tablist]").GetAttribute("blazor:elementReference"));

		JSRuntimeInvocation scroll = module.VerifyInvoke("MokaTabs.scrollTabIntoView");
		Assert.Equal("a", scroll.Arguments[0]);
		Assert.Equal(strip, scroll.Arguments[1]);
	}

	// The theme used to reach the strip only through MokaTabContainer, and most of it was then
	// overridden by the strip's own stylesheet.
	[Fact]
	public async Task ATheme_AppliesToAStripOnItsOwn_AndToItsMenu()
	{
		SetupTabsModule();
		var theme = new TabTheme { ActiveTabColor = "#00e676", ContextMenuBackground = "#101015" };

		IRenderedComponent<MokaTabStrip<string>> cut = RenderStrip(TwoTabs(), "a", p => p
			.Add(x => x.Theme, theme));

		Assert.Contains("--moka-tab-active-color: #00e676", cut.Find("[role=tablist]").GetAttribute("style"),
			StringComparison.Ordinal);

		await Tab(cut, "a").ContextMenuAsync(new MouseEventArgs());
		Assert.Contains("--moka-tab-ctx-bg: #101015", cut.Find("[role=menu]").GetAttribute("style"),
			StringComparison.Ordinal);
	}

	private IRenderedComponent<MokaTabStrip<string>> RenderStrip(
		IReadOnlyList<TabInfo<string>> tabs,
		string activeTabId,
		Action<ComponentParameterCollectionBuilder<MokaTabStrip<string>>>? configure = null) =>
		Render<MokaTabStrip<string>>(p =>
		{
			p.Add(x => x.Tabs, tabs).Add(x => x.ActiveTabId, activeTabId);
			configure?.Invoke(p);
		});

	private static IElement Tab(IRenderedComponent<MokaTabStrip<string>> cut, string id) =>
		cut.Find($"[role=tab][data-tab-id='{id}']");

	private static List<TabInfo<string>> TwoTabs() =>
	[
		new() { Id = "a", Title = "Alpha" },
		new() { Id = "b", Title = "Beta" }
	];

	private BunitJSModuleInterop SetupTabsModule()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TabsModule);
		module.Mode = JSRuntimeMode.Loose;
		return module;
	}

	private sealed class MenuPlugin : IMokaTabPlugin
	{
		public string Name => "menu";

		public string? Description => null;

		public IEnumerable<ContextMenuItem> GetContextMenuItems<TValue>(TabInfo<TValue> tab) =>
			[new ContextMenuItem { Text = $"From plugin: {tab.Title}", OnClick = () => Task.CompletedTask }];
	}
}
