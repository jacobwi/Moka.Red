using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Cheatsheet;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Feedback.Drawer;

namespace Moka.Red.Feedback.Tests.Components;

// A drawer with Overlay is modal, but nothing moved focus into it: its Escape handler only hears
// keys from inside, so Escape did nothing until the user clicked into the drawer.
public class MokaDrawerTests : BunitContext
{
	private const string DialogModule = "./_content/Moka.Red.Feedback/moka-dialog.js";
	private const int TrapHandle = 7;

	[Fact]
	public void Opening_MovesFocusIntoTheDrawer()
	{
		BunitJSModuleInterop module = SetupDialogModule();

		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p.Add(x => x.Open, true));

		string? drawerId = cut.Find("[role=dialog]").GetAttribute("blazor:elementReference");
		ElementReference trapped = Assert.IsType<ElementReference>(module.VerifyInvoke("trapFocus").Arguments[0]);
		Assert.Equal(drawerId, trapped.Id);
	}

	[Fact]
	public void OpenedByTheParent_MovesFocusIntoTheDrawer()
	{
		BunitJSModuleInterop module = SetupDialogModule();
		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p.Add(x => x.Open, false));
		module.VerifyNotInvoke("trapFocus");

		cut.Render(p => p.Add(x => x.Open, true));

		module.VerifyInvoke("trapFocus");
	}

	[Fact]
	public async Task Escape_ClosesTheDrawer_AndHandsFocusBack()
	{
		BunitJSModuleInterop module = SetupDialogModule();
		bool? open = null;
		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => open = v)));

		await cut.Find("[role=dialog]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.False(open);
		Assert.Empty(cut.FindAll("[role=dialog]"));
		Assert.Equal(TrapHandle, module.VerifyInvoke("releaseFocus").Arguments[0]);
	}

	[Theory]
	[InlineData(".moka-drawer-close")]
	[InlineData(".moka-drawer-backdrop")]
	public async Task ClosingByClick_HandsFocusBack(string selector)
	{
		BunitJSModuleInterop module = SetupDialogModule();
		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p.Add(x => x.Open, true));

		await cut.Find(selector).ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll("[role=dialog]"));
		Assert.Equal(TrapHandle, module.VerifyInvoke("releaseFocus").Arguments[0]);
	}

	[Fact]
	public void ClosedByTheParent_HandsFocusBack()
	{
		BunitJSModuleInterop module = SetupDialogModule();
		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p.Add(x => x.Open, true));

		cut.Render(p => p.Add(x => x.Open, false));

		Assert.Equal(TrapHandle, module.VerifyInvoke("releaseFocus").Arguments[0]);
	}

	[Fact]
	public async Task DisposedWhileOpen_HandsFocusBack()
	{
		BunitJSModuleInterop module = SetupDialogModule();
		Render<MokaDrawer>(p => p.Add(x => x.Open, true));

		await DisposeComponentsAsync();

		Assert.Equal(TrapHandle, module.VerifyInvoke("releaseFocus").Arguments[0]);
	}

	// The close button had no type, so in a drawer inside a form it submitted the form.
	[Fact]
	public void CloseButton_DoesNotSubmitAForm()
	{
		SetupDialogModule();

		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p.Add(x => x.Open, true));

		Assert.Equal("button", cut.Find(".moka-drawer-close").GetAttribute("type"));
	}

	// Without the backdrop the page behind stays usable, so the drawer is not modal and leaves
	// focus where it is.
	[Fact]
	public void WithoutOverlay_TheDrawerIsNotModal()
	{
		BunitJSModuleInterop module = SetupDialogModule();

		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Overlay, false));

		IElement drawer = cut.Find("[role=dialog]");
		Assert.False(drawer.HasAttribute("aria-modal"));
		module.VerifyNotInvoke("trapFocus");
	}

	// The page behind a modal drawer scrolled, because the drawer never took the scroll lock that
	// dialogs and bottom sheets share.
	[Fact]
	public void Opening_LocksPageScrolling()
	{
		BunitJSModuleInterop module = SetupDialogModule();

		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p.Add(x => x.Open, true));
		cut.Render(p => p.Add(x => x.Title, "Filters"));

		Assert.Single(module.Invocations["lockBodyScroll"]);
		module.VerifyNotInvoke("unlockBodyScroll");
	}

	[Fact]
	public void WithoutOverlay_ThePageKeepsScrolling()
	{
		BunitJSModuleInterop module = SetupDialogModule();

		Render<MokaDrawer>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Overlay, false));

		module.VerifyNotInvoke("lockBodyScroll");
	}

	[Theory]
	[InlineData("escape")]
	[InlineData("close button")]
	[InlineData("backdrop")]
	[InlineData("parent")]
	[InlineData("overlay off")]
	[InlineData("dispose")]
	public async Task EveryWayOut_GivesTheScrollLockBack(string how)
	{
		BunitJSModuleInterop module = SetupDialogModule();
		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p.Add(x => x.Open, true));

		switch (how)
		{
			case "escape":
				await cut.Find("[role=dialog]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
				break;
			case "close button":
				await cut.Find(".moka-drawer-close").ClickAsync(new MouseEventArgs());
				break;
			case "backdrop":
				await cut.Find(".moka-drawer-backdrop").ClickAsync(new MouseEventArgs());
				break;
			case "parent":
				cut.Render(p => p.Add(x => x.Open, false));
				break;
			case "overlay off":
				cut.Render(p => p.Add(x => x.Overlay, false));
				break;
			default:
				await DisposeComponentsAsync();
				break;
		}

		Assert.Single(module.Invocations["lockBodyScroll"]);
		Assert.Single(module.Invocations["unlockBodyScroll"]);
	}

	[Fact]
	public async Task Escape_DoesNotReachAHandlerAroundTheDrawer()
	{
		SetupDialogModule();
		var heard = false;
		IRenderedComponent<ContainerFragment> cut = Render(b =>
		{
			b.OpenElement(0, "div");
			b.AddAttribute(1, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, () => heard = true));
			b.OpenComponent<MokaDrawer>(2);
			b.AddComponentParameter(3, nameof(MokaDrawer.Open), true);
			b.CloseComponent();
			b.CloseElement();
		});

		await cut.Find(".moka-drawer-close").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.False(heard);
		Assert.Empty(cut.FindAll("[role=dialog]"));
	}

	// A drawer inside a dialog: Escape closed the drawer, then bubbled on and closed the dialog too.
	[Fact]
	public async Task Escape_InADrawerInsideADialog_LeavesTheDialogOpen()
	{
		SetupDialogModule();
		bool? dialogOpen = null;
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings")
			.Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => dialogOpen = v))
			.AddChildContent<MokaDrawer>(d => d.Add(x => x.Open, true).Add(x => x.Title, "Filters")));

		await cut.Find(".moka-drawer").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Null(dialogOpen);
		Assert.Empty(cut.FindAll(".moka-drawer"));
		Assert.Single(cut.FindAll(".moka-dialog"));
	}

	// A cheatsheet opened inside a drawer: Escape closed the cheatsheet and the drawer with it.
	[Fact]
	public async Task Escape_InACheatsheetInsideTheDrawer_LeavesTheDrawerOpen()
	{
		SetupDialogModule();
		bool? drawerOpen = null;
		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => drawerOpen = v))
			.AddChildContent<MokaCheatsheet>(c => c.Add(x => x.Open, true)));

		await cut.Find(".moka-cheatsheet").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Null(drawerOpen);
		Assert.Empty(cut.FindAll(".moka-cheatsheet"));
		Assert.Single(cut.FindAll(".moka-drawer"));
	}

	// A dialog opened inside a drawer: Escape closed the dialog, then bubbled on and closed the drawer.
	[Fact]
	public async Task Escape_InADialogInsideTheDrawer_LeavesTheDrawerOpen()
	{
		SetupDialogModule();
		bool? drawerOpen = null;
		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => drawerOpen = v))
			.AddChildContent<MokaDialog>(d => d.Add(x => x.Open, true).Add(x => x.Title, "Confirm")));

		await cut.Find(".moka-dialog-wrapper").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Null(drawerOpen);
		Assert.Empty(cut.FindAll(".moka-dialog"));
		Assert.Single(cut.FindAll(".moka-drawer"));
	}

	private BunitJSModuleInterop SetupDialogModule()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(DialogModule);
		module.Setup<int>("trapFocus", _ => true).SetResult(TrapHandle);
		module.SetupVoid("releaseFocus", _ => true).SetVoidResult();
		module.SetupVoid("lockBodyScroll", _ => true).SetVoidResult();
		module.SetupVoid("unlockBodyScroll", _ => true).SetVoidResult();
		return module;
	}
}
