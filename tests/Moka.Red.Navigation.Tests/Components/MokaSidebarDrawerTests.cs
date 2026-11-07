using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Navigation.Sidebar;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

public class MokaSidebarDrawerTests : BunitContext
{
	private const string SidebarModule = "./_content/Moka.Red.Navigation/Sidebar/MokaSidebar.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaSidebarDrawerTests()
	{
		_module = JSInterop.SetupModule(SidebarModule);
		_module.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void ASidebar_IsNotADrawer_ByDefault() =>
		Assert.DoesNotContain("moka-sidebar--drawer", Render<MokaSidebar>().Find("nav").ClassList);

	// Above a phone's width a closed drawer is an ordinary sidebar in plain view, so .NET can't make it inert.
	[Fact]
	public void AClosedDrawer_StaysInReach_AndIsNotModal()
	{
		IRenderedComponent<MokaSidebar> cut = Drawer(open: false);

		IElement nav = cut.Find("nav");
		Assert.Contains("moka-sidebar--drawer", nav.ClassList);
		Assert.Contains("moka-sidebar--closed", nav.ClassList);
		Assert.False(nav.HasAttribute("inert"));
		Assert.False(nav.HasAttribute("tabindex"));
		Assert.Empty(cut.FindAll(".moka-sidebar__backdrop"));
		_module.VerifyNotInvoke("openOverlay");
		_module.VerifyNotInvoke("lockBodyScroll");
	}

	[Fact]
	public void AnOpenDrawer_IsModal()
	{
		IRenderedComponent<MokaSidebar> cut = Drawer(open: true);

		Assert.Equal("-1", cut.Find("nav").GetAttribute("tabindex"));
		Assert.Single(cut.FindAll(".moka-sidebar__backdrop"));
		_module.VerifyInvoke("openOverlay");
		_module.VerifyInvoke("lockBodyScroll");
	}

	[Fact]
	public async Task Escape_ClosesAnOpenDrawer()
	{
		bool? open = null;
		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p
			.Add(x => x.Drawer, true)
			.Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, value => open = value)));

		await cut.Find("nav").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.False(open);
		Assert.Empty(cut.FindAll(".moka-sidebar__backdrop"));
	}

	[Fact]
	public async Task TheBackdrop_ClosesAnOpenDrawer()
	{
		IRenderedComponent<MokaSidebar> cut = Drawer(open: true);

		await cut.Find(".moka-sidebar__backdrop").ClickAsync(new MouseEventArgs());

		Assert.Contains("moka-sidebar--closed", cut.Find("nav").ClassList);
	}

	// A closed sidebar drops its spacing because at width 0 it would still show. A closed drawer keeps its width on a
	// wide screen, so it keeps its spacing too.
	[Fact]
	public void AClosedDrawer_KeepsItsSpacing()
	{
		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p
			.Add(x => x.Drawer, true)
			.Add(x => x.Open, false)
			.Add(x => x.MarginValue, "7px")
			.Add(x => x.PaddingValue, "5px"));

		Assert.Equal("margin: 7px; padding: 5px; width: 240px", cut.Find("nav").GetAttribute("style"));
	}

	[Fact]
	public void AClosedDrawer_OnAPhone_IsOutOfReachAndOffScreen()
	{
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations("MokaSidebar.razor.css", ".moka-sidebar--drawer.moka-sidebar--closed");

		Assert.Equal("hidden", rule["visibility"]);
		Assert.Equal("translateX(-100%)", rule["transform"]);
	}

	// A transition's first frame keeps the old value, so a drawer that faded its visibility in was still hidden when it
	// moved focus to its close button: focus stayed on the menu button, and Escape and the Tab trap did nothing.
	[Fact]
	public void ADrawer_IsVisibleTheMomentItOpens_AndHidesOnlyOnceItHasSlidOut()
	{
		Assert.Equal("transform var(--moka-transition-slow), visibility 0s",
			ScopedCss.Declarations("MokaSidebar.razor.css", ".moka-sidebar--drawer")["transition"]);
		Assert.Equal("transform var(--moka-transition-slow), visibility var(--moka-transition-slow)",
			ScopedCss.Declarations("MokaSidebar.razor.css", ".moka-sidebar--drawer.moka-sidebar--closed")["transition"]);
	}

	[Fact]
	public void AClosedDrawer_KeepsItsWidth()
	{
		Assert.Equal(
			"0 !important",
			ScopedCss.Declarations("MokaSidebar.razor.css", ".moka-sidebar--closed:not(.moka-sidebar--overlay):not(.moka-sidebar--drawer)")["width"]);
	}

	// Scrolling past the end of a long menu went on to scroll the page behind it.
	[Fact]
	public void TheScrollingBody_KeepsItsScrollToItself()
	{
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations("MokaSidebar.razor.css", "::deep .moka-sidebar__body");

		Assert.Equal("auto", rule["overflow-y"]);
		Assert.Equal("contain", rule["overscroll-behavior"]);
	}

	private IRenderedComponent<MokaSidebar> Drawer(bool open) => Render<MokaSidebar>(p => p
		.Add(x => x.Drawer, true)
		.Add(x => x.Open, open));
}
