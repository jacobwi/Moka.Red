using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Navigation.Sidebar;

namespace Moka.Red.Navigation.Tests.Components;

// An overlay sidebar covers the page on a backdrop, but it had no keyboard handling: Escape did
// not close it, opening it left focus on the page behind, and a closed sidebar's links stayed in
// the tab order.
public class MokaSidebarTests : BunitContext
{
	private const string SidebarModule = "./_content/Moka.Red.Navigation/Sidebar/MokaSidebar.razor.js";
	private const string Links = "<a id=\"home\" href=\"/\">Home</a><a href=\"/docs\">Docs</a>";

	[Fact]
	public async Task Escape_ClosesAnOverlaySidebar()
	{
		SetupSidebarModule();
		bool? open = null;
		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p
			.Add(x => x.Overlay, true)
			.Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => open = v))
			.AddChildContent(Links));

		await cut.Find("nav").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.False(open);
		Assert.Contains("moka-sidebar--closed", cut.Find("nav").ClassList);
		Assert.Empty(cut.FindAll(".moka-sidebar__backdrop"));
	}

	[Fact]
	public async Task Escape_InAnOverlaySidebar_DoesNotReachAHandlerAroundIt()
	{
		SetupSidebarModule();
		var heard = false;
		IRenderedComponent<ContainerFragment> cut = RenderInsideKeyListener(() => heard = true, overlay: true);

		await cut.Find("#home").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.False(heard);
		Assert.Contains("moka-sidebar--closed", cut.Find("nav").ClassList);
	}

	// An inline sidebar has no keys of its own, so it leaves them to the page and stays open.
	[Fact]
	public async Task InlineSidebar_LeavesKeysToThePage()
	{
		var heard = false;
		IRenderedComponent<ContainerFragment> cut = RenderInsideKeyListener(() => heard = true, overlay: false);

		await cut.Find("#home").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.True(heard);
		Assert.Contains("moka-sidebar--open", cut.Find("nav").ClassList);
	}

	[Fact]
	public void OpeningAnOverlaySidebar_MovesFocusIntoIt()
	{
		BunitJSModuleInterop module = SetupSidebarModule();

		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p
			.Add(x => x.Overlay, true)
			.AddChildContent(Links));

		ElementReference sidebar = Assert.IsType<ElementReference>(module.VerifyInvoke("openOverlay").Arguments[0]);
		Assert.Equal(cut.Find("nav").GetAttribute("blazor:elementReference"), sidebar.Id);
		Assert.Equal("-1", cut.Find("nav").GetAttribute("tabindex"));
	}

	[Fact]
	public void OpenedByTheParent_MovesFocusIntoIt()
	{
		BunitJSModuleInterop module = SetupSidebarModule();
		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p
			.Add(x => x.Overlay, true)
			.Add(x => x.Open, false));
		module.VerifyNotInvoke("openOverlay");

		cut.Render(p => p.Add(x => x.Open, true));

		module.VerifyInvoke("openOverlay");
	}

	[Theory]
	[InlineData("escape")]
	[InlineData("backdrop")]
	[InlineData("parent")]
	public async Task ClosingAnOverlaySidebar_HandsFocusBack(string how)
	{
		BunitJSModuleInterop module = SetupSidebarModule();
		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p
			.Add(x => x.Overlay, true)
			.AddChildContent(Links));
		string? sidebarId = cut.Find("nav").GetAttribute("blazor:elementReference");
		module.VerifyNotInvoke("closeOverlay");

		switch (how)
		{
			case "escape":
				await cut.Find("nav").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
				break;
			case "backdrop":
				await cut.Find(".moka-sidebar__backdrop").ClickAsync(new MouseEventArgs());
				break;
			default:
				cut.Render(p => p.Add(x => x.Open, false));
				break;
		}

		JSRuntimeInvocation close = module.VerifyInvoke("closeOverlay");
		ElementReference sidebar = Assert.IsType<ElementReference>(close.Arguments[0]);
		Assert.Equal(sidebarId, sidebar.Id);
		Assert.Equal(true, close.Arguments[1]);
		Assert.True(cut.Find("nav").HasAttribute("inert"));
	}

	// A layout that turns Overlay off on wide screens keeps the sidebar open and on screen, so
	// focus inside it must stay put. Only the Tab trap goes.
	[Fact]
	public void OverlayTurnedOffWhileOpen_LeavesFocusInTheSidebar()
	{
		BunitJSModuleInterop module = SetupSidebarModule();
		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p
			.Add(x => x.Overlay, true)
			.AddChildContent(Links));

		cut.Render(p => p.Add(x => x.Overlay, false));

		Assert.Equal(false, module.VerifyInvoke("closeOverlay").Arguments[1]);
		Assert.False(cut.Find("nav").HasAttribute("inert"));
		Assert.False(cut.Find("nav").HasAttribute("tabindex"));
	}

	// Strict JS interop: an inline sidebar that called its script would throw here.
	[Fact]
	public void InlineSidebar_LeavesFocusAlone()
	{
		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p.AddChildContent(Links));

		Assert.Empty(JSInterop.Invocations);
		Assert.False(cut.Find("nav").HasAttribute("tabindex"));
	}

	// The sidebar only adds a tabindex while it is an open overlay, and it must not remove one the
	// consumer put on it.
	[Fact]
	public void ConsumerTabindex_IsKept()
	{
		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p
			.AddUnmatched("tabindex", "0")
			.AddChildContent(Links));

		Assert.Equal("0", cut.Find("nav").GetAttribute("tabindex"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ClosedSidebar_IsInert(bool overlay)
	{
		SetupSidebarModule();

		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p
			.Add(x => x.Overlay, overlay)
			.Add(x => x.Open, false)
			.AddChildContent(Links));

		Assert.True(cut.Find("nav").HasAttribute("inert"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void OpenSidebar_IsNotInert(bool overlay)
	{
		SetupSidebarModule();

		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p
			.Add(x => x.Overlay, overlay)
			.AddChildContent(Links));

		Assert.False(cut.Find("nav").HasAttribute("inert"));
	}

	[Theory]
	[InlineData("0")]
	[InlineData("0px")]
	[InlineData(" 0rem ")]
	public void SidebarCollapsedToNoWidth_IsInert(string collapsedWidth)
	{
		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p
			.Add(x => x.Collapsed, true)
			.Add(x => x.CollapsedWidth, collapsedWidth)
			.AddChildContent(Links));

		Assert.True(cut.Find("nav").HasAttribute("inert"));
	}

	// The icon rail still shows its links, so they stay reachable.
	[Theory]
	[InlineData("56px")]
	[InlineData("calc(0px + 3rem)")]
	public void MiniMode_StaysReachable(string collapsedWidth)
	{
		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p
			.Add(x => x.Collapsed, true)
			.Add(x => x.CollapsedWidth, collapsedWidth)
			.AddChildContent(Links));

		Assert.False(cut.Find("nav").HasAttribute("inert"));
	}

	[Fact]
	public void ReopenedSidebar_IsNoLongerInert()
	{
		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p
			.Add(x => x.Open, false)
			.AddChildContent(Links));

		cut.Render(p => p.Add(x => x.Open, true));

		Assert.False(cut.Find("nav").HasAttribute("inert"));
	}

	// The page behind an open overlay sidebar kept scrolling under the backdrop.
	[Theory]
	[InlineData("escape")]
	[InlineData("parent")]
	[InlineData("dispose")]
	public async Task AnOverlaySidebar_LocksPageScroll_UntilItCloses(string how)
	{
		BunitJSModuleInterop module = SetupSidebarModule();
		IRenderedComponent<MokaSidebar> cut = Render<MokaSidebar>(p => p
			.Add(x => x.Overlay, true)
			.AddChildContent(Links));
		Assert.Single(module.Invocations["lockBodyScroll"]);
		module.VerifyNotInvoke("unlockBodyScroll");

		switch (how)
		{
			case "escape":
				await cut.Find("nav").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
				break;
			case "parent":
				cut.Render(p => p.Add(x => x.Open, false));
				break;
			default:
				await DisposeComponentsAsync();
				break;
		}

		Assert.Single(module.Invocations["unlockBodyScroll"]);
		Assert.Single(module.Invocations["lockBodyScroll"]);
	}

	[Fact]
	public void AnInlineSidebar_LeavesPageScrollAlone()
	{
		BunitJSModuleInterop module = SetupSidebarModule();

		Render<MokaSidebar>(p => p.AddChildContent(Links));

		module.VerifyNotInvoke("lockBodyScroll");
	}

	// Nothing inside the sidebar collapses it, so the change callback could never be raised.
	[Fact]
	public void Collapsed_HasNoChangeCallback() =>
		Assert.Null(typeof(MokaSidebar).GetProperty("CollapsedChanged"));

	private BunitJSModuleInterop SetupSidebarModule()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(SidebarModule);
		module.SetupVoid("openOverlay", _ => true).SetVoidResult();
		module.SetupVoid("closeOverlay", _ => true).SetVoidResult();
		module.SetupVoid("lockBodyScroll", _ => true).SetVoidResult();
		module.SetupVoid("unlockBodyScroll", _ => true).SetVoidResult();
		return module;
	}

	private IRenderedComponent<ContainerFragment> RenderInsideKeyListener(Action onKeyDown, bool overlay) =>
		Render(b =>
		{
			b.OpenElement(0, "div");
			b.AddAttribute(1, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, onKeyDown));
			b.OpenComponent<MokaSidebar>(2);
			b.AddComponentParameter(3, nameof(MokaSidebar.Overlay), overlay);
			b.AddComponentParameter(4, nameof(MokaSidebar.ChildContent), (RenderFragment)(c => c.AddMarkupContent(0, Links)));
			b.CloseComponent();
			b.CloseElement();
		});
}
