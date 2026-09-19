using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Feedback.BottomSheet;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaBottomSheetTests : BunitContext
{
	private const string DialogModule = "./_content/Moka.Red.Feedback/moka-dialog.js";
	private const string DismissDragModule = "./_content/Moka.Red.Feedback/BottomSheet/MokaBottomSheet.razor.js";
	private const int TrapHandle = 7;

	// Open="@x" with nothing handling OpenChanged: the parent keeps passing true after the sheet
	// closed itself. The sheet used to write its own Open parameter, so that render opened it again.
	[Fact]
	public async Task ClosedByTheBackdrop_StaysClosedWhenAnUnboundParentRendersAgain()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		List<bool> changes = [];
		IRenderedComponent<MokaBottomSheet> cut = Render<MokaBottomSheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.OpenChanged, open => changes.Add(open)));

		await cut.Find(".moka-bottom-sheet-backdrop").ClickAsync(new MouseEventArgs());
		Assert.Empty(cut.FindAll("[role=dialog]"));
		Assert.Equal([false], changes);

		cut.Render(p => p.Add(x => x.Open, true));
		Assert.Empty(cut.FindAll("[role=dialog]"));

		// A parent that follows OpenChanged opens it again by passing false, then true.
		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true));
		Assert.Single(cut.FindAll("[role=dialog]"));
	}

	// A sheet created open asks for the scroll lock from OnParametersSetAsync and again after its
	// first render, both before the module import has come back. It used to import twice and lock
	// twice, and the one unlock on close left the page locked.
	[Fact]
	public async Task CreatedOpen_LocksScrollOnce_AndCloseReleasesIt()
	{
		// bUnit only hands out module references through its own import, so the test takes one from
		// a stand-in module and gives it to the dialog module import when that is allowed to finish.
		BunitJSModuleInterop standIn = JSInterop.SetupModule("stand-in.js");
		standIn.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule(DismissDragModule).Mode = JSRuntimeMode.Loose;
		IJSObjectReference module = await JSInterop.JSRuntime.InvokeAsync<IJSObjectReference>("import", "stand-in.js");
		using PendingImport import = new(DialogModule);
		JSInterop.AddInvocationHandler(import);

		IRenderedComponent<MokaBottomSheet> cut = Render<MokaBottomSheet>(p => p.Add(x => x.Open, true));
		int imports = import.Invocations.Count;
		import.Complete(module);

		Assert.Equal(1, imports);
		cut.WaitForAssertion(() => Assert.Single(standIn.Invocations["lockBodyScroll"]));

		await cut.Find(".moka-bottom-sheet-backdrop").ClickAsync(new MouseEventArgs());

		Assert.Single(standIn.Invocations["lockBodyScroll"]);
		Assert.Single(standIn.Invocations["unlockBodyScroll"]);
	}

	// Nothing moved focus into the sheet, and its Escape handler only hears keys from inside, so
	// Escape did nothing until the user clicked into the sheet.
	[Fact]
	public void Opening_MovesFocusIntoTheSheet()
	{
		BunitJSModuleInterop module = SetUpModules();

		IRenderedComponent<MokaBottomSheet> cut = Render<MokaBottomSheet>(p => p.Add(x => x.Open, true));

		IElement sheet = cut.Find("[role=dialog]");
		ElementReference trapped = Assert.IsType<ElementReference>(module.VerifyInvoke("trapFocus").Arguments[0]);
		Assert.Equal(sheet.GetAttribute("blazor:elementReference"), trapped.Id);

		// A sheet with no control of its own takes the focus itself.
		Assert.Equal("-1", sheet.GetAttribute("tabindex"));
	}

	[Fact]
	public void OpenedByTheParent_MovesFocusIntoTheSheet()
	{
		BunitJSModuleInterop module = SetUpModules();
		IRenderedComponent<MokaBottomSheet> cut = Render<MokaBottomSheet>(p => p.Add(x => x.Open, false));
		module.VerifyNotInvoke("trapFocus");

		cut.Render(p => p.Add(x => x.Open, true));

		module.VerifyInvoke("trapFocus");
	}

	[Fact]
	public async Task Escape_Closes_AndHandsFocusBack()
	{
		BunitJSModuleInterop module = SetUpModules();
		bool? open = null;
		IRenderedComponent<MokaBottomSheet> cut = Render<MokaBottomSheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => open = v)));

		await cut.Find(".moka-bottom-sheet-wrapper").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.False(open);
		Assert.Empty(cut.FindAll("[role=dialog]"));
		Assert.Equal(TrapHandle, module.VerifyInvoke("releaseFocus").Arguments[0]);
	}

	[Fact]
	public async Task ClosingByTheBackdrop_HandsFocusBack()
	{
		BunitJSModuleInterop module = SetUpModules();
		IRenderedComponent<MokaBottomSheet> cut = Render<MokaBottomSheet>(p => p.Add(x => x.Open, true));

		await cut.Find(".moka-bottom-sheet-backdrop").ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll("[role=dialog]"));
		Assert.Equal(TrapHandle, module.VerifyInvoke("releaseFocus").Arguments[0]);
	}

	[Fact]
	public void ClosedByTheParent_HandsFocusBack()
	{
		BunitJSModuleInterop module = SetUpModules();
		IRenderedComponent<MokaBottomSheet> cut = Render<MokaBottomSheet>(p => p.Add(x => x.Open, true));

		cut.Render(p => p.Add(x => x.Open, false));

		Assert.Equal(TrapHandle, module.VerifyInvoke("releaseFocus").Arguments[0]);
	}

	[Fact]
	public async Task DisposedWhileOpen_HandsFocusBack()
	{
		BunitJSModuleInterop module = SetUpModules();
		Render<MokaBottomSheet>(p => p.Add(x => x.Open, true));

		await DisposeComponentsAsync();

		Assert.Equal(TrapHandle, module.VerifyInvoke("releaseFocus").Arguments[0]);
	}

	// The wrapper covers the page above everything outside it, so a backdrop outside the wrapper
	// never got a click in a browser. It sits inside now, beside the sheet rather than around it,
	// and neither handles clicks on the wrapper nor lets the wrapper take focus outside the sheet.
	[Fact]
	public async Task BackdropClick_ClosesTheSheet_FromInsideTheWrapper()
	{
		SetUpModules();
		IRenderedComponent<MokaBottomSheet> cut = Render<MokaBottomSheet>(p => p.Add(x => x.Open, true));

		IElement wrapper = cut.Find(".moka-bottom-sheet-wrapper");
		IElement backdrop = cut.Find(".moka-bottom-sheet-wrapper > .moka-bottom-sheet-backdrop");
		Assert.Null(backdrop.QuerySelector("[role=dialog]"));
		Assert.False(wrapper.HasAttribute("blazor:onclick"));
		Assert.False(wrapper.HasAttribute("tabindex"));

		await backdrop.ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll("[role=dialog]"));
	}

	[Fact]
	public async Task BackdropClick_WithCloseOnBackdropOff_KeepsTheSheetOpen()
	{
		SetUpModules();
		IRenderedComponent<MokaBottomSheet> cut = Render<MokaBottomSheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.CloseOnBackdrop, false));

		await cut.Find(".moka-bottom-sheet-backdrop").ClickAsync(new MouseEventArgs());

		Assert.Single(cut.FindAll("[role=dialog]"));
	}

	// The sheet rendered neither its Id nor unmatched attributes, so a consumer's aria-label was
	// dropped and the dialog had no name.
	[Fact]
	public void Header_NamesTheSheet()
	{
		SetUpModules();
		IRenderedComponent<MokaBottomSheet> cut = Render<MokaBottomSheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Header, "<span>Share</span>"));

		IElement header = cut.Find(".moka-bottom-sheet-header");
		Assert.False(string.IsNullOrEmpty(header.Id));
		Assert.Equal(header.Id, cut.Find("[role=dialog]").GetAttribute("aria-labelledby"));
	}

	[Fact]
	public void WithoutHeader_TakesTheIdAndAnAriaLabel()
	{
		SetUpModules();
		IRenderedComponent<MokaBottomSheet> cut = Render<MokaBottomSheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Id, "share-sheet")
			.AddUnmatched("aria-label", "Share options"));

		IElement sheet = cut.Find("[role=dialog]");
		Assert.Equal("share-sheet", sheet.Id);
		Assert.Equal("Share options", sheet.GetAttribute("aria-label"));
		Assert.False(sheet.HasAttribute("aria-labelledby"));
	}

	// The handle showed a grab cursor and did nothing. Dragged down far enough it closes the sheet
	// through the same path as Escape, and every open gets a listener on its new handle.
	[Fact]
	public async Task DraggingTheHandleDown_ClosesTheSheet_AndHandsFocusBack()
	{
		BunitJSModuleInterop module = SetUpModules();
		BunitJSModuleInterop drag = JSInterop.SetupModule(DismissDragModule);
		drag.Mode = JSRuntimeMode.Loose;
		bool? open = null;
		IRenderedComponent<MokaBottomSheet> cut = Render<MokaBottomSheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => open = v)));

		IElement handle = cut.Find(".moka-bottom-sheet-handle-bar");
		Assert.Equal("true", handle.GetAttribute("aria-hidden"));
		JSRuntimeInvocation bind = drag.VerifyInvoke("bindDismissDrag");
		Assert.Equal(handle.GetAttribute("blazor:elementReference"), Assert.IsType<ElementReference>(bind.Arguments[2]).Id);

		await cut.InvokeAsync(() => cut.Instance.OnHandleDraggedDown());

		Assert.False(open);
		Assert.Empty(cut.FindAll("[role=dialog]"));
		Assert.Equal(TrapHandle, module.VerifyInvoke("releaseFocus").Arguments[0]);

		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true));
		Assert.Equal(2, drag.Invocations["bindDismissDrag"].Count);
	}

	[Fact]
	public void WithoutTheHandle_NothingCanBeDragged()
	{
		SetUpModules();
		BunitJSModuleInterop drag = JSInterop.SetupModule(DismissDragModule);
		drag.Mode = JSRuntimeMode.Loose;

		IRenderedComponent<MokaBottomSheet> cut = Render<MokaBottomSheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.ShowHandle, false));

		Assert.Empty(cut.FindAll(".moka-bottom-sheet-handle-bar"));
		drag.VerifyNotInvoke("bindDismissDrag");
	}

	private BunitJSModuleInterop SetUpModules()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		BunitJSModuleInterop module = JSInterop.SetupModule(DialogModule);
		module.Setup<int>("trapFocus", _ => true).SetResult(TrapHandle);
		module.SetupVoid("releaseFocus", _ => true).SetVoidResult();
		return module;
	}

	// An import of one path that stays pending until the test completes it.
	private sealed class PendingImport(string path) : JSRuntimeInvocationHandlerBase<IJSObjectReference>(
		invocation => invocation.Identifier == "import" && Equals(invocation.Arguments[0], path),
		isCatchAllHandler: false)
	{
		public void Complete(IJSObjectReference module) => SetResultBase(module);
	}
}
