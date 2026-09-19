using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Dialog;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaDialogTests : BunitContext
{
	private const string DialogModule = "./_content/Moka.Red.Feedback/moka-dialog.js";
	private const string DragModule = "./_content/Moka.Red.Core/moka-drag.js";

	public MokaDialogTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void Title_LabelsTheDialog()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings"));

		IElement dialog = cut.Find("[role=dialog]");
		IElement title = cut.Find(".moka-dialog-title");
		Assert.False(string.IsNullOrEmpty(title.Id));
		Assert.Equal(title.Id, dialog.GetAttribute("aria-labelledby"));
	}

	[Fact]
	public void WithoutTitle_HasNoLabelledBy()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.ShowCloseButton, false));

		Assert.False(cut.Find("[role=dialog]").HasAttribute("aria-labelledby"));
	}

	[Fact]
	public void WithoutTitle_TakesAnAriaLabel()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.AddUnmatched("aria-label", "Keyboard shortcuts"));

		IElement dialog = cut.Find("[role=dialog]");
		Assert.Equal("Keyboard shortcuts", dialog.GetAttribute("aria-label"));
		Assert.False(dialog.HasAttribute("aria-labelledby"));
	}

	[Fact]
	public void LoadsDialogModule_BeforeItOpens()
	{
		Render<MokaDialog>(p => p.Add(x => x.Open, false));

		JSRuntimeInvocation import = JSInterop.VerifyInvoke("import");
		Assert.Equal(DialogModule, import.Arguments[0]);
	}

	// The drag setup is left pending forever. If the trap still ran, it did not wait behind it.
	[Fact]
	public void TrapsFocus_WithoutWaitingOnDragSetup()
	{
		BunitJSModuleInterop dialogModule = JSInterop.SetupModule(DialogModule);
		dialogModule.Setup<int>("trapFocus", _ => true).SetResult(1);
		BunitJSModuleInterop dragModule = JSInterop.SetupModule(DragModule);
		dragModule.SetupVoid("makeDraggable", _ => true);

		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Draggable, true)
			.Add(x => x.Title, "Move me"));
		cut.Render(p => p.Add(x => x.Open, true));

		dialogModule.VerifyInvoke("trapFocus");
	}

	// Open="@x" with nothing handling OpenChanged: the parent keeps passing true after Escape.
	// The dialog used to write its own Open parameter, so that next render opened it again.
	[Fact]
	public async Task ClosedByEscape_StaysClosedWhenAnUnboundParentRendersAgain()
	{
		List<bool> changes = [];
		int closes = 0;
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings")
			.Add(x => x.OpenChanged, open => changes.Add(open))
			.Add(x => x.OnClose, () => closes++));

		await cut.Find(".moka-dialog-wrapper").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		Assert.Empty(cut.FindAll("[role=dialog]"));
		Assert.Equal([false], changes);
		Assert.Equal(1, closes);

		cut.Render(p => p.Add(x => x.Open, true));
		Assert.Empty(cut.FindAll("[role=dialog]"));

		// A parent that follows OpenChanged opens it again by passing false, then true.
		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true));
		Assert.Single(cut.FindAll("[role=dialog]"));
	}

	// Closing through the parent skipped the drag reset, so the flag still said "attached" when the
	// dialog opened again with a new header, and that header never got the drag.
	[Fact]
	public void ReopenedAfterTheParentClosedIt_IsDraggableAgain()
	{
		BunitJSModuleInterop dragModule = JSInterop.SetupModule(DragModule);
		dragModule.Mode = JSRuntimeMode.Loose;

		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Draggable, true)
			.Add(x => x.Title, "Move me"));
		ElementReference firstHeader = Assert.IsType<ElementReference>(
			Assert.Single(dragModule.Invocations["makeDraggable"]).Arguments[2]);

		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true));

		IReadOnlyList<JSRuntimeInvocation> attached = dragModule.Invocations["makeDraggable"];
		Assert.Equal(2, attached.Count);
		Assert.NotEqual(firstHeader.Id, Assert.IsType<ElementReference>(attached[1].Arguments[2]).Id);
	}

	// A dialog the user dragged and the parent then closed used to open again where it was left.
	[Fact]
	public async Task ReopenedAfterTheParentClosedIt_IsCenteredAgain()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Draggable, true)
			.Add(x => x.Title, "Move me"));
		await cut.InvokeAsync(() => cut.Instance.OnDragMoved(40, 60));
		Assert.Contains("moka-dialog--moved", cut.Find("[role=dialog]").ClassList);

		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true));

		Assert.DoesNotContain("moka-dialog--moved", cut.Find("[role=dialog]").ClassList);
	}

	// The wrapper covers the page above everything outside it, so a backdrop outside the wrapper
	// never got a click in a browser. It sits inside now, beside the box rather than around it: a
	// press in the box released on the backdrop clicks the wrapper, the element both share.
	[Fact]
	public async Task BackdropClick_ClosesTheDialog_FromInsideTheWrapper()
	{
		List<bool> changes = [];
		int closes = 0;
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings")
			.Add(x => x.OpenChanged, open => changes.Add(open))
			.Add(x => x.OnClose, () => closes++));

		IElement backdrop = cut.Find(".moka-dialog-wrapper > .moka-dialog-backdrop");
		Assert.Null(backdrop.QuerySelector("[role=dialog]"));

		await backdrop.ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll("[role=dialog]"));
		Assert.Equal([false], changes);
		Assert.Equal(1, closes);
	}

	[Fact]
	public async Task BackdropClick_WithCloseOnBackdropClickOff_KeepsTheDialogOpen()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings")
			.Add(x => x.CloseOnBackdropClick, false));

		await cut.Find(".moka-dialog-backdrop").ClickAsync(new MouseEventArgs());

		Assert.Single(cut.FindAll("[role=dialog]"));
	}

	// A press in the box released outside it clicks the wrapper, so the wrapper must not act on
	// clicks. It must not take focus either: it is outside the box the focus trap watches, and a
	// press beside or inside the box used to focus it, from where Shift+Tab left the dialog. The
	// box takes that focus now, and a press on the backdrop moves no focus at all.
	[Fact]
	public void Wrapper_NeitherHandlesClicksNorTakesFocus()
	{
		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings"));

		IElement wrapper = cut.Find(".moka-dialog-wrapper");
		Assert.False(wrapper.HasAttribute("blazor:onclick"));
		Assert.False(wrapper.HasAttribute("tabindex"));
		Assert.Equal("-1", cut.Find("[role=dialog]").GetAttribute("tabindex"));
		Assert.True(cut.Find(".moka-dialog-backdrop").HasAttribute("blazor:onmousedown:preventDefault"));
	}

	// A render that finished while trapFocus was still out set up a second trap. Its handle
	// replaced the first one, which was never released.
	[Fact]
	public async Task ARenderWhileTrapFocusIsOut_SetsUpNoSecondTrap()
	{
		BunitJSModuleInterop dialogModule = JSInterop.SetupModule(DialogModule);
		dialogModule.Mode = JSRuntimeMode.Loose;
		JSRuntimeInvocationHandler<int> trap = dialogModule.Setup<int>("trapFocus", _ => true);
		dialogModule.SetupVoid("releaseFocus", _ => true).SetVoidResult();

		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings"));
		cut.Render(p => p.Add(x => x.Title, "Settings again"));
		Assert.Single(dialogModule.Invocations["trapFocus"]);

		trap.SetResult(3);
		await cut.Find(".moka-dialog-wrapper").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Single(dialogModule.Invocations["trapFocus"]);
		cut.WaitForAssertion(() => Assert.Equal(3, dialogModule.VerifyInvoke("releaseFocus").Arguments[0]));
	}

	// Closed before trapFocus came back, the close found no trap to release, and the trap that
	// came back afterwards stayed on for good.
	[Fact]
	public async Task ClosedWhileTrapFocusIsOut_ReleasesTheTrapWhenItComesBack()
	{
		BunitJSModuleInterop dialogModule = JSInterop.SetupModule(DialogModule);
		dialogModule.Mode = JSRuntimeMode.Loose;
		JSRuntimeInvocationHandler<int> trap = dialogModule.Setup<int>("trapFocus", _ => true);
		dialogModule.SetupVoid("releaseFocus", _ => true).SetVoidResult();

		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings"));
		await cut.Find(".moka-dialog-wrapper").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		dialogModule.VerifyNotInvoke("releaseFocus");

		trap.SetResult(4);

		cut.WaitForAssertion(() => Assert.Equal(4, dialogModule.VerifyInvoke("releaseFocus").Arguments[0]));
	}

	[Fact]
	public async Task DisposedWhileTrapFocusIsOut_ReleasesTheTrapWhenItComesBack()
	{
		BunitJSModuleInterop dialogModule = JSInterop.SetupModule(DialogModule);
		dialogModule.Mode = JSRuntimeMode.Loose;
		JSRuntimeInvocationHandler<int> trap = dialogModule.Setup<int>("trapFocus", _ => true);
		dialogModule.SetupVoid("releaseFocus", _ => true).SetVoidResult();
		Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings"));

		Task disposing = DisposeComponentsAsync();
		trap.SetResult(6);
		await disposing;

		Assert.Equal(6, dialogModule.VerifyInvoke("releaseFocus").Arguments[0]);
	}

	// The position went into the style through the current culture, and "412,5px" is not CSS: under
	// a culture with a decimal comma the dialog fell back to its centering left and top.
	[Fact]
	public async Task DraggedPosition_IsWrittenWithADecimalPointUnderAnyCulture()
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("de-DE");
		try
		{
			IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
				.Add(x => x.Open, true)
				.Add(x => x.Draggable, true)
				.Add(x => x.Title, "Move me"));

			await cut.InvokeAsync(() => cut.Instance.OnDragMoved(412.5, 80.25));

			string style = cut.Find("[role=dialog]").GetAttribute("style") ?? "";
			Assert.Contains("left: 412.5px", style, StringComparison.Ordinal);
			Assert.Contains("top: 80.25px", style, StringComparison.Ordinal);
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}
}
