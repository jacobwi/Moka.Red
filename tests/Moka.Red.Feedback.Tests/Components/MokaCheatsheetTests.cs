using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Cheatsheet;
using Moka.Red.Feedback.Dialog;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaCheatsheetTests : BunitContext
{
	private const string DialogModule = "./_content/Moka.Red.Feedback/moka-dialog.js";
	private const int TrapHandle = 7;

	// An open cheatsheet traps focus through moka-dialog.js.
	public MokaCheatsheetTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static IReadOnlyList<MokaCheatsheetGroup> SampleGroups =>
	[
		new("General",
		[
			new MokaCheatsheetItem("Command palette", ["Ctrl", "K"]),
			new MokaCheatsheetItem("Close", ["Esc"])
		]),
		new("Create", [new MokaCheatsheetItem("New", ["Ctrl", "N"])])
	];

	[Fact]
	public void Closed_RendersNothing()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, false)
			.Add(x => x.Groups, SampleGroups));

		Assert.Empty(cut.FindAll(".moka-cheatsheet"));
	}

	[Fact]
	public void Open_RendersBackdropAndDialog()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Groups, SampleGroups));

		Assert.NotNull(cut.Find(".moka-cheatsheet-backdrop"));
		IElement dialog = cut.Find(".moka-cheatsheet");
		Assert.Equal("dialog", dialog.GetAttribute("role"));
		Assert.Equal("true", dialog.GetAttribute("aria-modal"));
	}

	[Fact]
	public void Title_Default()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, true));

		Assert.Equal("Keyboard Shortcuts", cut.Find(".moka-cheatsheet-title").TextContent);
	}

	[Fact]
	public void Groups_RenderWithRowsAndKbd()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Groups, SampleGroups));

		Assert.Equal(2, cut.FindAll(".moka-cheatsheet-group").Count);
		Assert.Equal(3, cut.FindAll(".moka-cheatsheet-row").Count);
		// Ctrl+K, Esc, Ctrl+N = 5 kbd chips
		Assert.Equal(5, cut.FindAll(".moka-cheatsheet-keys .moka-kbd").Count);
	}

	[Fact]
	public void FooterContent_Renders()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.FooterContent, "<span id=\"foot\">hint</span>"));

		Assert.NotNull(cut.Find(".moka-cheatsheet-footer").QuerySelector("#foot"));
	}

	[Fact]
	public void CloseButton_InvokesOpenChanged()
	{
		var newState = true;
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => newState = v)));

		cut.Find(".moka-cheatsheet-close").Click();

		Assert.False(newState);
	}

	[Fact]
	public void CloseButton_DoesNotSubmitAForm()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p.Add(x => x.Open, true));

		Assert.Equal("button", cut.Find(".moka-cheatsheet-close").GetAttribute("type"));
	}

	// The overlay is modal, but nothing moved focus into it, and its Escape handler only hears
	// keys from inside. Escape did nothing until the user clicked into the cheatsheet.
	[Fact]
	public void Opening_MovesFocusIntoTheDialog()
	{
		BunitJSModuleInterop module = SetupDialogModule();

		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p.Add(x => x.Open, true));

		string? dialogId = cut.Find("[role=dialog]").GetAttribute("blazor:elementReference");
		ElementReference trapped = Assert.IsType<ElementReference>(module.VerifyInvoke("trapFocus").Arguments[0]);
		Assert.Equal(dialogId, trapped.Id);
	}

	[Fact]
	public void OpenedByTheParent_MovesFocusIntoTheDialog()
	{
		BunitJSModuleInterop module = SetupDialogModule();
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p.Add(x => x.Open, false));
		module.VerifyNotInvoke("trapFocus");

		cut.Render(p => p.Add(x => x.Open, true));

		module.VerifyInvoke("trapFocus");
	}

	[Fact]
	public async Task Escape_Closes_AndHandsFocusBack()
	{
		BunitJSModuleInterop module = SetupDialogModule();
		bool? open = null;
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => open = v)));

		await cut.Find("[role=dialog]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.False(open);
		Assert.Empty(cut.FindAll("[role=dialog]"));
		Assert.Equal(TrapHandle, module.VerifyInvoke("releaseFocus").Arguments[0]);
	}

	[Theory]
	[InlineData(".moka-cheatsheet-close")]
	[InlineData(".moka-cheatsheet-backdrop")]
	public async Task ClosingByClick_HandsFocusBack(string selector)
	{
		BunitJSModuleInterop module = SetupDialogModule();
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p.Add(x => x.Open, true));

		await cut.Find(selector).ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll("[role=dialog]"));
		Assert.Equal(TrapHandle, module.VerifyInvoke("releaseFocus").Arguments[0]);
	}

	[Fact]
	public void ClosedByTheParent_HandsFocusBack()
	{
		BunitJSModuleInterop module = SetupDialogModule();
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p.Add(x => x.Open, true));

		cut.Render(p => p.Add(x => x.Open, false));

		Assert.Equal(TrapHandle, module.VerifyInvoke("releaseFocus").Arguments[0]);
	}

	[Fact]
	public async Task DisposedWhileOpen_HandsFocusBack()
	{
		BunitJSModuleInterop module = SetupDialogModule();
		Render<MokaCheatsheet>(p => p.Add(x => x.Open, true));

		await DisposeComponentsAsync();

		Assert.Equal(TrapHandle, module.VerifyInvoke("releaseFocus").Arguments[0]);
	}

	// The page behind the modal cheatsheet scrolled, because it never took the scroll lock that
	// dialogs and bottom sheets share.
	[Fact]
	public void Opening_LocksPageScrolling()
	{
		BunitJSModuleInterop module = SetupDialogModule();

		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p.Add(x => x.Open, true));
		cut.Render(p => p.Add(x => x.Groups, SampleGroups));

		Assert.Single(module.Invocations["lockBodyScroll"]);
		module.VerifyNotInvoke("unlockBodyScroll");
	}

	[Fact]
	public void Closed_LeavesPageScrollingAlone()
	{
		BunitJSModuleInterop module = SetupDialogModule();

		Render<MokaCheatsheet>(p => p.Add(x => x.Open, false));

		module.VerifyNotInvoke("lockBodyScroll");
	}

	[Theory]
	[InlineData("escape")]
	[InlineData("close button")]
	[InlineData("backdrop")]
	[InlineData("parent")]
	[InlineData("dispose")]
	public async Task EveryWayOut_GivesTheScrollLockBack(string how)
	{
		BunitJSModuleInterop module = SetupDialogModule();
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p.Add(x => x.Open, true));

		switch (how)
		{
			case "escape":
				await cut.Find("[role=dialog]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
				break;
			case "close button":
				await cut.Find(".moka-cheatsheet-close").ClickAsync(new MouseEventArgs());
				break;
			case "backdrop":
				await cut.Find(".moka-cheatsheet-backdrop").ClickAsync(new MouseEventArgs());
				break;
			case "parent":
				cut.Render(p => p.Add(x => x.Open, false));
				break;
			default:
				await DisposeComponentsAsync();
				break;
		}

		Assert.Single(module.Invocations["lockBodyScroll"]);
		Assert.Single(module.Invocations["unlockBodyScroll"]);
	}

	[Fact]
	public async Task Escape_DoesNotReachAHandlerAroundTheCheatsheet()
	{
		SetupDialogModule();
		var heard = false;
		IRenderedComponent<ContainerFragment> cut = Render(b =>
		{
			b.OpenElement(0, "div");
			b.AddAttribute(1, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, () => heard = true));
			b.OpenComponent<MokaCheatsheet>(2);
			b.AddComponentParameter(3, nameof(MokaCheatsheet.Open), true);
			b.CloseComponent();
			b.CloseElement();
		});

		await cut.Find(".moka-cheatsheet-close").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.False(heard);
		Assert.Empty(cut.FindAll("[role=dialog]"));
	}

	// Opened from inside a dialog: Escape closed the cheatsheet, then bubbled on and closed the dialog.
	[Fact]
	public async Task Escape_InACheatsheetInsideADialog_LeavesTheDialogOpen()
	{
		SetupDialogModule();
		bool? dialogOpen = null;
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Help")
			.Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => dialogOpen = v))
			.AddChildContent<MokaCheatsheet>(c => c.Add(x => x.Open, true)));

		await cut.Find(".moka-cheatsheet").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Null(dialogOpen);
		Assert.Empty(cut.FindAll(".moka-cheatsheet"));
		Assert.Single(cut.FindAll(".moka-dialog"));
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
