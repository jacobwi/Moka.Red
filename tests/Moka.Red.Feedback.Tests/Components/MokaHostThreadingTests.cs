using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.Feedback.Tests.Components;

/// <summary>
///     The toast and dialog services raise their events on whatever thread calls them. The hosts
///     used to handle those events in async void methods and change their state off the renderer's
///     thread, so a failure there ended the process.
/// </summary>
public class MokaHostThreadingTests : BunitContext
{
	public MokaHostThreadingTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback();
	}

	[Fact]
	public async Task Toast_ShownFromAnotherThread_Renders()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();
		IMokaToastService toasts = Services.GetRequiredService<IMokaToastService>();

		await Task.Run(() => toasts.ShowSuccess("Saved"), Xunit.TestContext.Current.CancellationToken);

		cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".moka-toast")));
	}

	[Fact]
	public async Task Toast_RemovedFromAnotherThread_Disappears()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();
		IMokaToastService toasts = Services.GetRequiredService<IMokaToastService>();
		toasts.ShowInfo("Syncing");
		cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".moka-toast")));

		Guid id = toasts.Toasts[0].Id;
		await Task.Run(() => toasts.Remove(id), Xunit.TestContext.Current.CancellationToken);

		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".moka-toast")));
	}

	[Fact]
	public void Dialog_RequestedFromAnotherThread_Opens()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		IMokaDialogService dialogs = Services.GetRequiredService<IMokaDialogService>();

		// The confirm task only completes when the dialog is answered, so it is not awaited.
		_ = Task.Run(() => dialogs.ConfirmAsync("Delete it?", "Confirm"), Xunit.TestContext.Current.CancellationToken);

		cut.WaitForAssertion(() => Assert.Equal("Confirm", cut.Find(".moka-dialog-title").TextContent));
	}
}
