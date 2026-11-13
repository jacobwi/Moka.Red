using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Hotkeys;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;
using Moka.Red.Navigation.AppShell;
using Moka.Red.Navigation.Menu;
using Moka.Red.Primitives.Badge;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

public class MokaAppShellTests : BunitContext
{
	private const string ShellModule = "./_content/Moka.Red.Navigation/AppShell/MokaAppShell.razor.js";

	private static readonly MokaNavEntry[] Entries =
	[
		new(string.Empty, "Home", "/", MokaIcons.Navigation.Home),
		new("Network", "Devices", "/devices", MokaIcons.Action.Search, "names", new MokaNavPill(14, MokaBadgeTone.Count, "14 devices")),
		new("System", "Settings", "/settings", MokaIcons.Action.Settings),
		new("Network", "Firewall", "/firewall", MokaIcons.Content.Filter)
	];

	private readonly BunitJSModuleInterop _module;

	// The shell needs an interactive layout, so that's how these render it unless a test says otherwise. Handed to bUnit
	// as the shell renders, since bUnit takes no new services once the renderer exists and some tests add the hotkeys.
	private RendererInfo _renderer = new("Server", true);

	public MokaAppShellTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ShellModule);
		_module.Mode = JSRuntimeMode.Loose;
	}

	private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	public void TheSlots_RenderInTheirPlaces()
	{
		IRenderedComponent<MokaAppShell> cut = Shell(p => p
			.Add(x => x.Brand, "<b id=\"brand\"></b>")
			.Add(x => x.Status, "<b id=\"status\"></b>")
			.Add(x => x.Account, "<b id=\"account\"></b>")
			.Add(x => x.TopBarEnd, "<b id=\"end\"></b>")
			.Add(x => x.ChildContent, "<b id=\"page\"></b>"));

		Assert.NotNull(cut.Find(".moka-app-shell__brand #brand"));
		Assert.NotNull(cut.Find(".moka-app-shell__status #status"));
		Assert.NotNull(cut.Find(".moka-sidebar__footer #account"));
		Assert.NotNull(cut.Find("header.moka-app-shell__topbar #end"));
		Assert.NotNull(cut.Find("main.moka-app-shell__main #page"));
	}

	[Fact]
	public void AShellWithoutThem_DrawsNoEmptyBoxes()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();

		Assert.Empty(cut.FindAll(".moka-sidebar__footer"));
		Assert.Empty(cut.FindAll(".moka-app-shell__status"));
		Assert.DoesNotContain("moka-app-shell__head--status", cut.Find(".moka-app-shell__head").ClassList);
	}

	[Fact]
	public void AStatus_MarksTheHead_SoItCanTakeTheMonoLinesPlace()
	{
		IRenderedComponent<MokaAppShell> cut = Shell(p => p.Add(x => x.Status, "All good"));

		Assert.Contains("moka-app-shell__head--status", cut.Find(".moka-app-shell__head").ClassList);
	}

	[Fact]
	public void ClassAndAttributes_GoOnTheRoot()
	{
		IRenderedComponent<MokaAppShell> cut = Shell(p => p.Add(x => x.Class, "mine").AddUnmatched("data-x", "1"));

		IElement root = cut.Find(".moka-app-shell");
		Assert.Contains("mine", root.ClassList);
		Assert.Equal("1", root.GetAttribute("data-x"));
	}

	[Fact]
	public void TheMenu_ListsGroups_InTheOrderTheyFirstAppear()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();

		IReadOnlyList<IElement> groups = cut.FindAll(".moka-app-shell__group");
		Assert.Equal(3, groups.Count);
		Assert.False(groups[0].HasAttribute("role"));
		Assert.Empty(groups[0].QuerySelectorAll(".moka-app-shell__group-name"));
		Assert.Equal("group", groups[1].GetAttribute("role"));
		Assert.Equal("Network", groups[1].GetAttribute("aria-label"));
		Assert.Equal("true", groups[1].QuerySelector(".moka-app-shell__group-name")?.GetAttribute("aria-hidden"));
		Assert.Equal(
			["Devices", "Firewall"],
			groups[1].QuerySelectorAll(".moka-menu-item__text").Select(text => text.TextContent));
		Assert.Equal("System", groups[2].GetAttribute("aria-label"));
	}

	[Fact]
	public void TheMenu_LinksRelativeToTheBaseAddress()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();

		Assert.Equal(["./", "devices", "firewall", "settings"], cut.FindAll("a.moka-menu-item").Select(link => link.GetAttribute("href")));
	}

	[Fact]
	public void TheMenu_IsAdaptive_AndNamed()
	{
		IRenderedComponent<MokaAppShell> cut = Shell(p => p.Add(x => x.NavLabel, "Router"));

		IElement menu = cut.Find(".moka-menu");
		Assert.Contains("moka-menu--adaptive", menu.ClassList);
		Assert.Equal("Router", menu.GetAttribute("aria-label"));
	}

	[Fact]
	public void TheEntryForTheAddress_IsTheCurrentPage()
	{
		Navigation.NavigateTo("devices/tv");

		IRenderedComponent<MokaAppShell> cut = Shell();

		IElement current = Assert.Single(cut.FindAll("[aria-current=page]"));
		Assert.Equal("devices", current.GetAttribute("href"));
	}

	[Fact]
	public void GoingToAnotherPage_MovesTheHighlightAndTheTitle()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();
		Assert.Equal("Home", cut.Find(".moka-app-shell__bar-title").TextContent);

		Navigation.NavigateTo("settings");

		cut.WaitForAssertion(() =>
		{
			Assert.Equal("Settings", cut.Find(".moka-app-shell__bar-title").TextContent);
			Assert.Equal("settings", Assert.Single(cut.FindAll("[aria-current=page]")).GetAttribute("href"));
		});
	}

	[Fact]
	public void TopBarTitle_WinsOverTheEntry()
	{
		IRenderedComponent<MokaAppShell> cut = Shell(p => p.Add(x => x.TopBarTitle, "Your router"));

		Assert.Equal("Your router", cut.Find(".moka-app-shell__bar-title").TextContent);
	}

	[Fact]
	public void AnAddressTheMenuDoesNotList_LeavesTheTitleEmpty()
	{
		Navigation.NavigateTo("elsewhere");

		IRenderedComponent<MokaAppShell> cut = Shell();

		Assert.Empty(cut.Find(".moka-app-shell__bar-title").TextContent);
		Assert.Empty(cut.FindAll("[aria-current=page]"));
	}

	[Fact]
	public void APill_ShowsWithItsReason_InTheRowAndItsTag()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();

		IElement link = cut.Find("a[href=devices]");
		Assert.Contains("moka-badge--tone-count", link.QuerySelector(".moka-menu-item__pill")!.ClassList);
		Assert.Equal(", 14 devices", link.QuerySelector(".moka-visually-hidden")?.TextContent);
		IElement popup = link.ParentElement!.QuerySelector(":scope > .moka-tooltip-popup")!;
		Assert.Contains("moka-tooltip-popup--right", popup.ClassList);
		Assert.Equal("14 devices", popup.QuerySelector(".moka-menu-item__why")?.TextContent);
	}

	[Fact]
	public void EveryPillTone_ReachesItsBadge()
	{
		MokaNavEntry[] entries =
		[
			new("A", "One", "/one", MokaIcons.Action.Add, Pill: new MokaNavPill(1, MokaBadgeTone.Limited, "limited")),
			new("A", "Two", "/two", MokaIcons.Action.Add, Pill: new MokaNavPill(2, MokaBadgeTone.NeedsYou, "needs you")),
			new("A", "Three", "/three", MokaIcons.Action.Add, Pill: new MokaNavPill(3, MokaBadgeTone.Ready, "ready"))
		];

		SetRendererInfo(_renderer);
		IRenderedComponent<MokaAppShell> cut = Render<MokaAppShell>(p => p.Add(x => x.Entries, entries));

		IReadOnlyList<IElement> pills = cut.FindAll(".moka-menu-item__pill");
		Assert.Contains("moka-badge--tone-limited", pills[0].ClassList);
		Assert.Contains("moka-badge--tone-needs-you", pills[1].ClassList);
		Assert.Contains("moka-badge--tone-ready", pills[2].ClassList);
	}

	[Fact]
	public void AnEntrysPicture_ShowsInTheMenu_AndOthersKeepTheirIcons()
	{
		MokaNavEntry[] entries =
		[
			new("", "Home", "/", MokaIcons.Navigation.Home),
			new("Plugins", "Notes", "/notes", MokaIcons.Content.Plug) { ImageSource = "data:image/png;base64,iVBORw0KGgo=" },
			new("", "Settings", "/settings", MokaIcons.Action.Settings) { Bottom = true, ImageSource = "/settings.webp" }
		];

		SetRendererInfo(_renderer);
		IRenderedComponent<MokaAppShell> cut = Render<MokaAppShell>(p => p.Add(x => x.Entries, entries));

		IReadOnlyList<IElement> images = cut.FindAll("img.moka-menu-item__image");
		Assert.Equal(["data:image/png;base64,iVBORw0KGgo=", "/settings.webp"], images.Select(image => image.GetAttribute("src")));
		Assert.Single(cut.FindAll(".moka-menu-item__icon"));
	}

	// The first thing inside the root of a prerendered page, so it has run before anything in the shell can be drawn.
	[Fact]
	public void AStaticRender_WritesTheScriptThatSetsTheRail_AsTheRootsFirstChild()
	{
		_renderer = new RendererInfo("Static", false);

		IRenderedComponent<MokaAppShell> cut = Shell();

		IElement script = cut.Find(".moka-app-shell").Children[0];
		Assert.Equal("SCRIPT", script.TagName);
		Assert.False(script.HasAttribute("nonce"));
		Assert.Contains("data-moka-rail", script.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void AStaticRender_GivesTheScriptTheNonce_Escaped()
	{
		_renderer = new RendererInfo("Static", false);

		IRenderedComponent<MokaAppShell> cut = Shell(p => p.Add(x => x.Nonce, "abc\"d"));

		Assert.Equal("abc\"d", cut.Find(".moka-app-shell > script").GetAttribute("nonce"));
		Assert.Single(cut.FindAll("script"));
	}

	// Blazor inserts markup through a template and recreates each script in it by setting its text, which a Trusted
	// Types policy blocks: under one, an app's whole signed-in UI stopped rendering. And there's no paint left to beat.
	[Theory]
	[InlineData("Server", null)]
	[InlineData("WebAssembly", null)]
	[InlineData("WebView", null)]
	[InlineData("WebAssembly", "abc")]
	public void AnInteractiveRender_WritesNoScript(string platform, string? nonce)
	{
		_renderer = new RendererInfo(platform, true);

		IRenderedComponent<MokaAppShell> cut = Shell(p => p.Add(x => x.Nonce, nonce));

		Assert.Empty(cut.FindAll("script"));
		Assert.Equal("HEADER", cut.Find(".moka-app-shell").Children[0].TagName);
	}

	// Blazor replaces a prerendered shell with a new element when it goes interactive, and that render writes no script,
	// so the state the prerendered script decided has to be somewhere Blazor leaves alone, or the rail would jump.
	[Fact]
	public void TheScriptAndTheModule_SetTheRailOnHtml_WhichBlazorNeverReplaces()
	{
		_renderer = new RendererInfo("Static", false);
		const string OnHtml = "document.documentElement.toggleAttribute('data-moka-rail'";
		string module = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaAppShell.razor.js"));

		IRenderedComponent<MokaAppShell> cut = Shell();

		Assert.Contains(OnHtml, cut.Find(".moka-app-shell > script").TextContent, StringComparison.Ordinal);
		Assert.Contains(OnHtml, module, StringComparison.Ordinal);
		Assert.DoesNotContain("shell.toggleAttribute", module, StringComparison.Ordinal);
	}

	// A full-height page, such as a file explorer, couldn't drop the 120 px kept under the page for a floating bar.
	[Fact]
	public void APageThatFillsTheMainArea_GetsTheWindowsHeight_AndOnlyTheRoomABarTakes()
	{
		IReadOnlyDictionary<string, string> fill = ScopedCss.Declarations("MokaAppShell.razor.css",
			".moka-app-shell__main:has(> .moka-fill-main)");

		Assert.Equal("calc(100dvh - var(--moka-appbar-space, 0px))", fill["height"]);
		Assert.Equal("calc(var(--moka-main-padding-top) + var(--moka-action-bar-space, 0px))", fill["padding-bottom"]);
	}

	[Fact]
	public void TheRailRules_ReadTheAttributeOnAnAncestor()
	{
		Assert.Equal("4.25rem", ScopedCss.Declarations("MokaAppShell.razor.css", "[data-moka-rail] .moka-app-shell")["--_moka-shell-nav"]);
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaAppShell.razor.css"));
		Assert.DoesNotContain(".moka-app-shell[data-moka-rail]", css, StringComparison.Ordinal);
	}

	// The script can't import the module, so the two hold the same rule, and this keeps them the same.
	[Theory]
	[InlineData("moka-app-shell-rail")]
	[InlineData("(max-width: 720px)")]
	[InlineData("(max-width: 1279.98px)")]
	[InlineData("data-moka-rail-choice")]
	[InlineData("data-moka-rail")]
	public void TheScriptAndTheModule_AgreeOn(string value)
	{
		_renderer = new RendererInfo("Static", false);
		string module = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaAppShell.razor.js"));
		IRenderedComponent<MokaAppShell> cut = Shell();

		Assert.Contains(value, module, StringComparison.Ordinal);
		Assert.Contains(value, cut.Find(".moka-app-shell > script").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void TheRail_IsAutomatic_ByDefault() =>
		Assert.Equal("automatic", Shell().Find(".moka-app-shell").GetAttribute("data-moka-rail-choice"));

	[Theory]
	[InlineData(MokaRailChoice.Expanded, "expanded")]
	[InlineData(MokaRailChoice.Collapsed, "collapsed")]
	[InlineData(MokaRailChoice.Automatic, "automatic")]
	public void TheChoice_IsOnTheRoot_ForTheScript(MokaRailChoice choice, string expected)
	{
		IRenderedComponent<MokaAppShell> cut = Shell(p => p.Add(x => x.Rail, choice));

		Assert.Equal(expected, cut.Find(".moka-app-shell").GetAttribute("data-moka-rail-choice"));
	}

	// Only script writes the attribute itself, or .NET's next render could take away what the script decided.
	[Fact]
	public void TheShell_NeverRendersTheRailAttribute()
	{
		IRenderedComponent<MokaAppShell> cut = Shell(p => p.Add(x => x.Rail, MokaRailChoice.Collapsed));

		Assert.False(cut.Find(".moka-app-shell").HasAttribute("data-moka-rail"));
	}

	[Fact]
	public void TheFirstRender_BindsTheShell()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();

		JSRuntimeInvocation call = _module.VerifyInvoke("bindShell");
		Assert.Equal(cut.Find(".moka-app-shell").GetAttribute("blazor:elementReference"), Assert.IsType<ElementReference>(call.Arguments[1]).Id);
	}

	[Fact]
	public void AFlipKeptFromEarlierInTheTab_WinsOverTheChoice_AndIsReported()
	{
		_module.Setup<string>("bindShell", _ => true).SetResult("collapsed");
		List<MokaRailChoice> reported = [];

		IRenderedComponent<MokaAppShell> cut = Shell(p => p.Add(x => x.RailChanged, reported.Add));

		Assert.Equal([MokaRailChoice.Collapsed], reported);
		Assert.Equal("collapsed", cut.Find(".moka-app-shell").GetAttribute("data-moka-rail-choice"));
	}

	[Fact]
	public void AKeptFlipThatMatchesTheChoice_IsNotReported()
	{
		_module.Setup<string>("bindShell", _ => true).SetResult("collapsed");
		List<MokaRailChoice> reported = [];

		Shell(p => p.Add(x => x.Rail, MokaRailChoice.Collapsed).Add(x => x.RailChanged, reported.Add));

		Assert.Empty(reported);
	}

	[Fact]
	public void WithNothingKept_NothingIsReported()
	{
		List<MokaRailChoice> reported = [];

		Shell(p => p.Add(x => x.RailChanged, reported.Add));

		Assert.Empty(reported);
	}

	[Fact]
	public void ANewChoice_ReplacesAFlip()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();
		_module.VerifyNotInvoke("chooseRail");

		cut.Render(p => p.Add(x => x.Rail, MokaRailChoice.Collapsed));

		_module.VerifyInvoke("chooseRail");
		Assert.Equal("collapsed", cut.Find(".moka-app-shell").GetAttribute("data-moka-rail-choice"));
	}

	[Fact]
	public void TheSameChoiceAgain_ChangesNothing()
	{
		IRenderedComponent<MokaAppShell> cut = Shell(p => p.Add(x => x.Rail, MokaRailChoice.Collapsed));

		cut.Render(p => p.Add(x => x.Rail, MokaRailChoice.Collapsed));

		_module.VerifyNotInvoke("chooseRail");
	}

	[Fact]
	public async Task AFlip_IsReported_AndDrawn()
	{
		_module.Setup<string>("flipRail", _ => true).SetResult("expanded");
		List<MokaRailChoice> reported = [];
		IRenderedComponent<MokaAppShell> cut = Shell(p => p
			.Add(x => x.Rail, MokaRailChoice.Collapsed)
			.Add(x => x.RailChanged, reported.Add));

		await cut.InvokeAsync(cut.Instance.FlipRailAsync);

		Assert.Equal([MokaRailChoice.Expanded], reported);
		Assert.Equal("expanded", cut.Find(".moka-app-shell").GetAttribute("data-moka-rail-choice"));
	}

	// @bind-Rail passes a flip straight back. Treated as a new choice it would forget the flip, and a reload would lose it.
	[Fact]
	public async Task AFlipEchoedBackByABinding_StaysKept()
	{
		_module.Setup<string>("flipRail", _ => true).SetResult("collapsed");
		IRenderedComponent<MokaAppShell> cut = Shell();

		await cut.InvokeAsync(cut.Instance.FlipRailAsync);
		cut.Render(p => p.Add(x => x.Rail, MokaRailChoice.Collapsed));

		_module.VerifyNotInvoke("chooseRail");
	}

	// The module answers null on a phone, which has no rail to flip.
	[Fact]
	public async Task AFlipOnAPhone_ChangesNothing()
	{
		_module.Setup<string>("flipRail", _ => true).SetResult(null!);
		List<MokaRailChoice> reported = [];
		IRenderedComponent<MokaAppShell> cut = Shell(p => p.Add(x => x.RailChanged, reported.Add));

		await cut.InvokeAsync(cut.Instance.FlipRailAsync);

		Assert.Empty(reported);
		Assert.Equal("automatic", cut.Find(".moka-app-shell").GetAttribute("data-moka-rail-choice"));
	}

	[Fact]
	public void TheBracketKey_IsInTheRegistry_ForOutsideTextFields()
	{
		MokaHotkeyService hotkeys = new();
		Services.AddSingleton<IMokaHotkeyService>(hotkeys);

		Shell();

		MokaHotkey hotkey = Assert.Single(hotkeys.Hotkeys);
		Assert.Equal("[", hotkey.Keys);
		Assert.False(hotkey.AllowInInput);
		Assert.False(string.IsNullOrEmpty(hotkey.Description));
	}

	[Fact]
	public async Task TheBracketKey_FlipsTheRail()
	{
		MokaHotkeyService hotkeys = new();
		Services.AddSingleton<IMokaHotkeyService>(hotkeys);
		_module.Setup<string>("flipRail", _ => true).SetResult("collapsed");
		List<MokaRailChoice> reported = [];
		IRenderedComponent<MokaAppShell> cut = Shell(p => p.Add(x => x.RailChanged, reported.Add));

		await cut.InvokeAsync(() => hotkeys.InvokeAsync(hotkeys.Hotkeys[0].Id));

		Assert.Equal([MokaRailChoice.Collapsed], reported);
	}

	[Fact]
	public async Task Leaving_TakesTheKeyOutOfTheRegistry_AndStopsWatching()
	{
		MokaHotkeyService hotkeys = new();
		Services.AddSingleton<IMokaHotkeyService>(hotkeys);
		Shell();

		await DisposeComponentsAsync();

		Assert.Empty(hotkeys.Hotkeys);
		_module.VerifyInvoke("unbindShell");
	}

	// The phone's top bar is sticky, so a link to part of a page has to land below it. It reports what it covers to the
	// property a fixed MokaAppBar uses, which moka.css makes the page's top scroll padding.
	[Fact]
	public void ThePhonesTopBar_ReportsTheRoomItTakes()
	{
		(BunitJSModuleInterop module, _) = SpaceModule();
		IRenderedComponent<MokaAppShell> cut = Shell();
		string? topBar = cut.Find("header.moka-app-shell__topbar").GetAttribute("blazor:elementReference");

		cut.Render(p => p.Add(x => x.TopBarTitle, "Devices"));

		JSRuntimeInvocation watch = Assert.Single(module.Invocations["watchSpace"]);
		Assert.Equal(topBar, Assert.IsType<ElementReference>(watch.Arguments[0]).Id);
		Assert.Equal("--moka-appbar-space", watch.Arguments[1]);
		Assert.False(Assert.IsType<bool>(watch.Arguments[2]));
	}

	// The page's scroll padding counts the bar as out of view, so focusing its menu button scrolled the page by half a
	// window. Its controls count as sitting just below it instead.
	[Fact]
	public void ThePhonesTopBar_ControlsCountAsInView()
	{
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations("MokaAppShell.razor.css", ".moka-app-shell__topbar ::deep *");

		Assert.Equal("calc(-1 * var(--moka-appbar-space, 0px))", rule["scroll-margin-top"]);
		Assert.Equal("var(--moka-appbar-space, 0px)", rule["scroll-margin-bottom"]);
	}

	[Fact]
	public async Task Leaving_GivesTheRoomBack()
	{
		(_, BunitJSModuleInterop space) = SpaceModule();
		Shell();

		await DisposeComponentsAsync();

		Assert.Single(space.Invocations["dispose"]);
	}

	// An app that registered only the core services still gets a shell, without the key.
	[Fact]
	public void WithoutTheRegistry_TheShellStillRenders() =>
		Assert.NotNull(Shell().Find(".moka-app-shell"));

	[Fact]
	public void TheMenuButton_IsNamedByATooltip()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();

		IElement button = cut.Find(".moka-app-shell__topbar button");
		IconTooltip.AssertNamed(button, "Open the menu", MokaTooltipPosition.BottomStart);
		Assert.Equal(cut.Find("nav.moka-app-shell__nav").Id, button.GetAttribute("aria-controls"));
		Assert.Equal("false", button.GetAttribute("aria-expanded"));
	}

	[Fact]
	public void TheCloseButton_IsNamedByATooltip_AndTakesFocusOnOpening()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();

		IElement button = cut.Find(".moka-app-shell__close");
		IconTooltip.AssertNamed(button, "Close the menu", MokaTooltipPosition.BottomEnd);
		Assert.True(button.HasAttribute("data-autofocus"));
	}

	[Fact]
	public void TheButtonLabels_CanBeChanged()
	{
		IRenderedComponent<MokaAppShell> cut = Shell(p => p
			.Add(x => x.OpenMenuLabel, "Menu")
			.Add(x => x.CloseMenuLabel, "Close"));

		IconTooltip.AssertNamed(cut.Find(".moka-app-shell__topbar button"), "Menu", MokaTooltipPosition.BottomStart);
		IconTooltip.AssertNamed(cut.Find(".moka-app-shell__close"), "Close", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void TheSidebar_TakesItsWidthFromTheStylesheet()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();

		IElement nav = cut.Find("nav.moka-app-shell__nav");
		Assert.Contains("moka-sidebar--drawer", nav.ClassList);
		Assert.Equal("width: var(--_moka-shell-nav)", nav.GetAttribute("style"));
	}

	[Fact]
	public void AClosedDrawer_IsNotALandmarkOfItsOwn()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();

		IElement nav = cut.Find("nav.moka-app-shell__nav");
		Assert.Equal("none", nav.GetAttribute("role"));
		Assert.False(nav.HasAttribute("aria-modal"));
		Assert.False(nav.HasAttribute("aria-label"));
		Assert.Contains("moka-sidebar--closed", nav.ClassList);
		_module.VerifyNotInvoke("setDrawerOpen");
	}

	[Fact]
	public void TheMenuButton_OpensTheDrawer()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();

		OpenDrawer(cut);

		IElement nav = cut.Find("nav.moka-app-shell__nav");
		Assert.Equal("dialog", nav.GetAttribute("role"));
		Assert.Equal("true", nav.GetAttribute("aria-modal"));
		Assert.Equal("Main", nav.GetAttribute("aria-label"));
		Assert.Contains("moka-sidebar--open", nav.ClassList);
		Assert.Single(cut.FindAll(".moka-sidebar__backdrop"));
		Assert.Equal("true", cut.Find(".moka-app-shell__topbar button").GetAttribute("aria-expanded"));
		Assert.Equal(true, Assert.Single(_module.Invocations, call => call.Identifier == "setDrawerOpen").Arguments[1]);
	}

	[Fact]
	public void TheCloseButton_ClosesTheDrawer()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();
		OpenDrawer(cut);

		cut.Find(".moka-app-shell__close").Click();

		AssertDrawerClosed(cut);
	}

	[Fact]
	public void Escape_ClosesTheDrawer()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();
		OpenDrawer(cut);

		cut.Find("nav.moka-app-shell__nav").KeyDown(new KeyboardEventArgs { Key = "Escape" });

		AssertDrawerClosed(cut);
	}

	[Fact]
	public void TheScrim_ClosesTheDrawer()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();
		OpenDrawer(cut);

		cut.Find(".moka-sidebar__backdrop").Click();

		AssertDrawerClosed(cut);
	}

	[Fact]
	public void APick_ClosesTheDrawer()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();
		OpenDrawer(cut);

		cut.Find("a[href=settings]").Click();

		AssertDrawerClosed(cut);
	}

	[Fact]
	public void AnyWayOfReachingAPage_ClosesTheDrawer()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();
		OpenDrawer(cut);

		Navigation.NavigateTo("settings");

		cut.WaitForAssertion(() => AssertDrawerClosed(cut));
	}

	// How the module's swipe left and growing past a phone's width reach .NET.
	[Fact]
	public async Task TheModule_CanCloseTheDrawer()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();
		OpenDrawer(cut);

		await cut.InvokeAsync(cut.Instance.CloseDrawerAsync);

		AssertDrawerClosed(cut);
	}

	[Fact]
	public void ClosingTheDrawer_StopsTheSwipe()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();
		OpenDrawer(cut);

		cut.Find(".moka-app-shell__close").Click();

		Assert.Equal(
			[true, false],
			_module.Invocations.Where(call => call.Identifier == "setDrawerOpen").Select(call => call.Arguments[1]));
	}

	// A click on a wider screen shouldn't redraw the whole shell.
	[Fact]
	public void TheRows_ListenForAPick_OnlyWhileTheDrawerIsOut()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();
		Assert.All(cut.FindComponents<MokaMenuItem>(), item => Assert.False(item.Instance.OnClick.HasDelegate));

		OpenDrawer(cut);

		Assert.All(cut.FindComponents<MokaMenuItem>(), item => Assert.True(item.Instance.OnClick.HasDelegate));
	}

	[Fact]
	public void APickInTheAccountCard_ClosesTheDrawer()
	{
		RenderFragment account = builder =>
		{
			builder.OpenComponent<MokaAccountCard>(0);
			builder.AddComponentParameter(1, nameof(MokaAccountCard.Name), "BC");
			builder.AddComponentParameter(2, nameof(MokaAccountCard.Items), (IReadOnlyList<MokaAccountItem>)[new MokaAccountItem("Sign out")]);
			builder.CloseComponent();
		};
		IRenderedComponent<MokaAppShell> cut = Shell(p => p.Add(x => x.Account, account));
		OpenDrawer(cut);

		cut.Find(".moka-account-card__card").Click();
		cut.Find("[role=menuitem]").Click();

		AssertDrawerClosed(cut);
	}

	[Fact]
	public void TheSizes_AreTheOnesOnTheBoards()
	{
		const string File = "MokaAppShell.razor.css";

		Assert.Equal("15.5rem", ScopedCss.Values(File, ".moka-app-shell", "--_moka-shell-nav")[0]);
		Assert.Equal("4.25rem", ScopedCss.Declarations(File, "[data-moka-rail] .moka-app-shell")["--_moka-shell-nav"]);
		Assert.Equal("3.25rem", ScopedCss.Declarations(File, ".moka-app-shell__topbar")["height"]);
	}

	[Fact]
	public void TheMainArea_HasThePaddingAndGapTheSpecAsksFor()
	{
		const string File = "MokaAppShell.razor.css";

		Assert.Equal("var(--moka-main-padding)", ScopedCss.Declarations(File, ".moka-app-shell__main")["padding"]);
		Assert.Equal("var(--moka-spacing-xl)", ScopedCss.Declarations(File, ".moka-app-shell__main")["gap"]);
		Assert.Equal("column", ScopedCss.Declarations(File, ".moka-app-shell__main")["flex-direction"]);
		Assert.Equal("var(--moka-spacing-xl)", ScopedCss.Declarations(File, ".moka-app-shell__main")["padding-inline"]);
	}

	[Fact]
	public void TheSideMenu_IsStickyAtFullHeight()
	{
		IReadOnlyDictionary<string, string> nav = ScopedCss.Declarations("MokaAppShell.razor.css", ".moka-app-shell ::deep .moka-sidebar.moka-app-shell__nav");

		Assert.Equal("sticky", nav["position"]);
		Assert.Equal("100dvh", nav["height"]);
	}

	// A backdrop-filter on the bar itself would make it the containing block of its tooltips' fixed popups.
	[Fact]
	public void TheTopBarsBlur_IsOnALayerOfItsOwn()
	{
		const string File = "MokaAppShell.razor.css";

		Assert.Equal("blur(var(--moka-backdrop-blur))", ScopedCss.Declarations(File, ".moka-app-shell__topbar::before")["backdrop-filter"]);
		Assert.DoesNotContain("backdrop-filter", ScopedCss.Declarations(File, ".moka-app-shell__topbar").Keys);
	}

	[Fact]
	public void TheTopBar_ShowsOnlyOnAPhone() =>
		Assert.Equal(["none", "flex"], ScopedCss.Values("MokaAppShell.razor.css", ".moka-app-shell__topbar", "display"));

	[Fact]
	public void TheBrandsWords_HideOnTheRail() =>
		Assert.Equal(
			"none",
			ScopedCss.Declarations("MokaAppShell.razor.css", "[data-moka-rail] .moka-app-shell .moka-app-shell__brand ::deep .moka-app-shell__brand-text")["display"]);

	[Fact]
	public void AGroupsName_BecomesALineOnTheRail()
	{
		IReadOnlyDictionary<string, string> name = ScopedCss.Declarations("MokaAppShell.razor.css", "[data-moka-rail] .moka-app-shell .moka-app-shell__group-name");

		Assert.Equal("1px", name["height"]);
		Assert.Equal("transparent", name["color"]);
	}

	[Fact]
	public void TheRail_ShowsNoNames_ByDefault()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();

		Assert.DoesNotContain("moka-app-shell--labels", cut.Find(".moka-app-shell").ClassList);
		Assert.DoesNotContain("moka-menu--rail-labels", cut.Find(".moka-menu").ClassList);
	}

	[Fact]
	public void RailLabels_MarksTheShellAndItsMenu()
	{
		IRenderedComponent<MokaAppShell> cut = Shell(p => p.Add(x => x.RailLabels, true));

		Assert.Contains("moka-app-shell--labels", cut.Find(".moka-app-shell").ClassList);
		Assert.Contains("moka-menu--rail-labels", cut.Find(".moka-menu").ClassList);
	}

	// The labelled rail: the name shows under the icon, the rail widens to fit it, and a tag stays only where it adds
	// something. The script marks the cut names, since only a browser can measure them.
	[Fact]
	public void ALabelledRail_ShowsTheNames_AndIsWider()
	{
		IReadOnlyDictionary<string, string> text = ScopedCss.Declarations("MokaMenu.razor.css",
			"[data-moka-rail] .moka-menu.moka-menu--adaptive.moka-menu--rail-labels ::deep .moka-menu-item__text");
		IReadOnlyDictionary<string, string> tile = ScopedCss.Declarations("MokaMenu.razor.css",
			"[data-moka-rail] .moka-menu.moka-menu--adaptive.moka-menu--rail-labels ::deep .moka-menu-item.moka-fill-width > .moka-menu-item__content");

		Assert.Equal("none", text["clip-path"]);
		Assert.Equal("static", text["position"]);
		Assert.Equal("column", tile["flex-direction"]);
		Assert.Equal("5rem", ScopedCss.Declarations("MokaAppShell.razor.css", "[data-moka-rail] .moka-app-shell--labels")["--_moka-shell-nav"]);
		Assert.Equal("none !important", ScopedCss.Declarations("MokaMenu.razor.css",
			"[data-moka-rail] .moka-menu.moka-menu--adaptive.moka-menu--rail-labels ::deep .moka-menu-item-tooltip:not([data-moka-cut]):not(:has(.moka-menu-item__pill)) > .moka-tooltip-popup")["display"]);
	}

	[Fact]
	public void ABottomEntry_SitsUnderTheGroups_AndIsCurrentOnItsPage()
	{
		MokaNavEntry[] entries =
		[
			new(string.Empty, "Home", "/", MokaIcons.Navigation.Home),
			new("System", "Settings", "/settings", MokaIcons.Action.Settings) { Bottom = true }
		];
		Navigation.NavigateTo("settings");
		SetRendererInfo(_renderer);

		IRenderedComponent<MokaAppShell> cut = Render<MokaAppShell>(p => p.Add(x => x.Entries, entries));

		IElement settings = Assert.Single(cut.Find(".moka-app-shell__bottom").QuerySelectorAll("a.moka-menu-item"));
		Assert.Equal("settings", settings.GetAttribute("href"));
		Assert.Equal("page", settings.GetAttribute("aria-current"));
		Assert.Contains("moka-menu-item--active", settings.ClassList);
		Assert.Null(cut.Find(".moka-app-shell__groups").QuerySelector("a[href=settings]"));
		Assert.Empty(cut.FindAll(".moka-app-shell__group-name"));
	}

	[Fact]
	public void WithoutBottomEntries_ThereIsNoBottomList()
	{
		IRenderedComponent<MokaAppShell> cut = Shell();

		Assert.Empty(cut.FindAll(".moka-app-shell__bottom"));
		Assert.Equal(4, cut.Find(".moka-app-shell__groups").QuerySelectorAll("a.moka-menu-item").Length);
	}

	// The current tile reads the theme's current-place tokens, so Solid fills it. A focus ring on a filled tile needs an
	// edge in the tile's ink, or the accent edge vanishes into the fill.
	[Fact]
	public void TheCurrentTile_ReadsTheCurrentPlaceTokens_AndKeepsAVisibleFocusEdge()
	{
		IReadOnlyDictionary<string, string> current = ScopedCss.Declarations("MokaMenu.razor.css",
			".moka-gate .moka-menu ::deep .moka-menu-item--active > .moka-menu-item__content");
		IReadOnlyDictionary<string, string> focused = ScopedCss.Declarations("MokaMenu.razor.css",
			".moka-gate .moka-menu ::deep .moka-menu-item--active:focus-visible > .moka-menu-item__content");

		Assert.Equal("var(--moka-current-bg)", current["background-color"]);
		Assert.Equal("var(--moka-current-color)", current["color"]);
		Assert.Equal("var(--moka-current-shadow)", current["box-shadow"]);
		Assert.Equal("inset 0 0 0 1px var(--moka-current-edge), var(--moka-focus-ring)", focused["box-shadow"]);
	}

	private static void OpenDrawer(IRenderedComponent<MokaAppShell> cut) => cut.Find(".moka-app-shell__topbar button").Click();

	private static void AssertDrawerClosed(IRenderedComponent<MokaAppShell> cut)
	{
		Assert.Empty(cut.FindAll(".moka-sidebar__backdrop"));
		Assert.Equal("none", cut.Find("nav.moka-app-shell__nav").GetAttribute("role"));
		Assert.Equal("false", cut.Find(".moka-app-shell__topbar button").GetAttribute("aria-expanded"));
	}

	private (BunitJSModuleInterop Module, BunitJSModuleInterop Space) SpaceModule()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Moka.Red.Core/moka-space.js");
		BunitJSModuleInterop space = module.SetupModule(invocation => invocation.Identifier == "watchSpace");
		space.Mode = JSRuntimeMode.Loose;
		return (module, space);
	}

	private IRenderedComponent<MokaAppShell> Shell(Action<ComponentParameterCollectionBuilder<MokaAppShell>>? parameters = null)
	{
		SetRendererInfo(_renderer);
		return Render<MokaAppShell>(p =>
		{
			p.Add(x => x.Entries, Entries);
			parameters?.Invoke(p);
		});
	}
}
