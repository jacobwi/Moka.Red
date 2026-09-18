using AngleSharp.Dom;
using Bunit;
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
}
