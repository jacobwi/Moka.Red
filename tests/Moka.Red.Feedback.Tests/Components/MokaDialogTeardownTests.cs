using Bunit;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Tests.Shared;

#pragma warning disable CA2000 // The dialog owns each stand-in module and disposes it; the tests count those disposals.

namespace Moka.Red.Feedback.Tests.Components;

// Closing or reloading a tab ends its Blazor Server circuit, and Blazor then disposes every component
// in it. A JS call still on its way fails with TaskCanceledException, which the dialog let through, so
// every page with a dialog logged two unhandled exceptions of its circuit as it closed.
public class MokaDialogTeardownTests : BunitContext
{
	private const string DialogModule = "./_content/Moka.Red.Feedback/moka-dialog.js";
	private const string DragModule = "./_content/Moka.Red.Core/moka-drag.js";

	public MokaDialogTeardownTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task DisposedWhileItsImportIsCanceled_ThrowsNothing()
	{
		using ControlledImport import = new(DialogModule);
		JSInterop.AddInvocationHandler(import);
		Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings"));

		Task disposing = DisposeComponentsAsync();
		import.Cancel();
		await disposing;

		Assert.False(Renderer.UnhandledException.IsCompleted);
	}

	[Fact]
	public async Task ItsImportCanceledBeforeItRendered_ThrowsNothingThenOrWhenDisposed()
	{
		using ControlledImport import = new(DialogModule);
		JSInterop.AddInvocationHandler(import);
		import.Cancel();

		Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, "Settings"));
		await DisposeComponentsAsync();

		Assert.False(Renderer.UnhandledException.IsCompleted);
	}

	// The trap, the scroll lock and the drag were all set up, so disposal asks for each to be undone
	// and both modules to be disposed, and every one of those calls fails.
	[Fact]
	public async Task DisposedAfterTheCircuitEnded_TriesEachReleaseAndThrowsNothing()
	{
		EndingModule dialog = new EndingModule().Returns("trapFocus", 3);
		EndingModule drag = new();
		using ControlledImport dialogImport = new(DialogModule);
		using ControlledImport dragImport = new(DragModule);
		JSInterop.AddInvocationHandler(dialogImport);
		JSInterop.AddInvocationHandler(dragImport);
		dialogImport.Complete(dialog);
		dragImport.Complete(drag);

		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Draggable, true)
			.Add(x => x.Title, "Settings"));
		cut.WaitForAssertion(() =>
		{
			Assert.Contains("trapFocus", dialog.Calls);
			Assert.Contains("lockBodyScroll", dialog.Calls);
			Assert.Contains("makeDraggable", drag.Calls);
		});

		dialog.End();
		drag.End();
		await DisposeComponentsAsync();

		Assert.False(Renderer.UnhandledException.IsCompleted);
		Assert.Contains("releaseFocus", dialog.Calls);
		Assert.Contains("unlockBodyScroll", dialog.Calls);
		Assert.Contains("removeDraggable", drag.Calls);
		Assert.Equal(1, dialog.DisposeAttempts);
		Assert.Equal(1, drag.DisposeAttempts);
	}

	[Fact]
	public async Task DisposedWithTheCircuitStillThere_ReleasesAndDisposesBothModules()
	{
		EndingModule dialog = new EndingModule().Returns("trapFocus", 3);
		EndingModule drag = new();
		using ControlledImport dialogImport = new(DialogModule);
		using ControlledImport dragImport = new(DragModule);
		JSInterop.AddInvocationHandler(dialogImport);
		JSInterop.AddInvocationHandler(dragImport);
		dialogImport.Complete(dialog);
		dragImport.Complete(drag);

		IRenderedComponent<MokaDialog> cut = Render<MokaDialog>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Draggable, true)
			.Add(x => x.Title, "Settings"));
		cut.WaitForAssertion(() => Assert.Contains("makeDraggable", drag.Calls));

		await DisposeComponentsAsync();

		Assert.False(Renderer.UnhandledException.IsCompleted);
		Assert.Equal(1, dialog.Calls.Count(call => call == "releaseFocus"));
		Assert.Equal(1, dialog.Calls.Count(call => call == "unlockBodyScroll"));
		Assert.Equal(1, dialog.DisposeAttempts);
		Assert.Equal(1, drag.DisposeAttempts);
	}
}
