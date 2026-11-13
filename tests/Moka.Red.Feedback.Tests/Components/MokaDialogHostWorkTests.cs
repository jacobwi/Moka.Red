using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Feedback.Extensions;

namespace Moka.Red.Feedback.Tests.Components;

/// <summary>
///     A confirm handed its work stays open while the work runs, shows a failure and lets the person try again, and
///     closes once the work has succeeded or stopped.
/// </summary>
public class MokaDialogHostWorkTests : BunitContext
{
	public MokaDialogHostWorkTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback();
	}

	private IMokaDialogService Dialogs => Services.GetRequiredService<IMokaDialogService>();

	[Fact]
	public async Task WhileTheWorkRuns_TheDialogStaysOpenAndBusy()
	{
		TaskCompletionSource done = new();
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<bool> answer = Dialogs.ConfirmAsync("Delete 9 items?", "Delete", o => o.OnConfirmAsync = _ => done.Task);
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));

		Task click = Button(cut, "OK").ClickAsync(new MouseEventArgs());

		cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("[role=dialog]").GetAttribute("aria-busy")));
		IElement confirm = Button(cut, "OK");
		Assert.Contains("moka-btn--loading", confirm.ClassName, StringComparison.Ordinal);
		Assert.Equal("true", confirm.GetAttribute("aria-disabled"));
		Assert.Equal("true", confirm.GetAttribute("aria-busy"));
		Assert.True(cut.Find(".moka-dialog-close").HasAttribute("disabled"));
		Assert.False(answer.IsCompleted);

		done.SetResult();
		await click;

		Assert.True(await answer);
		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role=dialog]")));
	}

	[Fact]
	public async Task WhileTheWorkRuns_EscapeAndTheBackdropDoNotCloseIt()
	{
		TaskCompletionSource done = new();
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<bool> answer = Dialogs.ConfirmAsync("Delete?", "Delete", o => o.OnConfirmAsync = _ => done.Task);
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));
		Task click = Button(cut, "OK").ClickAsync(new MouseEventArgs());
		cut.WaitForAssertion(() => Assert.True(cut.Find("[role=dialog]").HasAttribute("aria-busy")));

		await cut.Find(".moka-dialog-wrapper").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		await cut.Find(".moka-dialog-backdrop").ClickAsync(new MouseEventArgs());

		Assert.False(answer.IsCompleted);
		Assert.Single(cut.FindAll("[role=dialog]"));

		done.SetResult();
		await click;
		Assert.True(await answer);
	}

	[Fact]
	public async Task AFailure_ShowsItsMessageAndKeepsTheDialogOpenForAnotherTry()
	{
		int tries = 0;
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<bool> answer = Dialogs.ConfirmAsync("Delete?", "Delete", o => o.OnConfirmAsync = async _ =>
		{
			await Task.Yield();
			if (++tries == 1)
			{
				throw new InvalidOperationException("The library is read-only right now.");
			}
		});
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));

		await Button(cut, "OK").ClickAsync(new MouseEventArgs());

		cut.WaitForAssertion(() => Assert.Equal("The library is read-only right now.",
			cut.Find(".moka-dialog-failure .moka-alert").TextContent.Trim()));
		Assert.False(answer.IsCompleted);
		Assert.False(cut.Find("[role=dialog]").HasAttribute("aria-busy"));
		Assert.False(Button(cut, "OK").HasAttribute("disabled"));

		await Button(cut, "OK").ClickAsync(new MouseEventArgs());

		Assert.True(await answer);
		Assert.Equal(2, tries);
	}

	[Fact]
	public async Task AFailure_SaysFailedTextWhenTheAppGaveOne()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		_ = Dialogs.ConfirmAsync("Delete?", "Delete", o =>
		{
			o.OnConfirmAsync = _ => Task.FromException(new InvalidOperationException("Code 0x80004005"));
			o.FailedText = "Couldn't delete them. Try again.";
		});
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));

		await Button(cut, "OK").ClickAsync(new MouseEventArgs());

		cut.WaitForAssertion(() => Assert.Equal("Couldn't delete them. Try again.",
			cut.Find(".moka-dialog-failure").TextContent.Trim()));
	}

	[Fact]
	public async Task TheFailure_IsAnnouncedAsAnAlert()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		_ = Dialogs.ConfirmAsync("Delete?", "Delete", o => o.OnConfirmAsync = _ => throw new InvalidOperationException("No"));
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));

		await Button(cut, "OK").ClickAsync(new MouseEventArgs());

		cut.WaitForAssertion(() => Assert.Equal("alert", cut.Find(".moka-dialog-failure .moka-alert").GetAttribute("role")));
	}

	[Fact]
	public async Task Cancel_StopsTheWorkAndClosesOnceItHasStopped()
	{
		CancellationToken seen = default;
		TaskCompletionSource stopped = new();
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<bool> answer = Dialogs.ConfirmAsync("Delete?", "Delete", o => o.OnConfirmAsync = async token =>
		{
			seen = token;
			await stopped.Task;
			token.ThrowIfCancellationRequested();
		});
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));
		Task click = Button(cut, "OK").ClickAsync(new MouseEventArgs());
		cut.WaitForAssertion(() => Assert.True(cut.Find("[role=dialog]").HasAttribute("aria-busy")));

		await Button(cut, "Cancel").ClickAsync(new MouseEventArgs());

		Assert.True(seen.IsCancellationRequested);
		Assert.False(answer.IsCompleted);
		cut.WaitForAssertion(() => Assert.True(Button(cut, "Cancel").HasAttribute("disabled")));

		stopped.SetResult();
		await click;
		Assert.False(await answer);
		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role=dialog]")));
	}

	// It happened, so the caller hears that it did.
	[Fact]
	public async Task WorkThatFinishesAfterCancel_StillAnswersTrue()
	{
		TaskCompletionSource done = new();
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<bool> answer = Dialogs.ConfirmAsync("Delete?", "Delete", o => o.OnConfirmAsync = _ => done.Task);
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));
		Task click = Button(cut, "OK").ClickAsync(new MouseEventArgs());
		cut.WaitForAssertion(() => Assert.True(cut.Find("[role=dialog]").HasAttribute("aria-busy")));

		await Button(cut, "Cancel").ClickAsync(new MouseEventArgs());
		done.SetResult();
		await click;

		Assert.True(await answer);
	}

	[Fact]
	public async Task ClosingThroughTheService_CancelsTheWork()
	{
		CancellationToken seen = default;
		TaskCompletionSource never = new();
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<bool> answer = Dialogs.ConfirmAsync("Delete?", "Delete", o => o.OnConfirmAsync = token =>
		{
			seen = token;
			return never.Task.WaitAsync(token);
		});
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));
		Task click = Button(cut, "OK").ClickAsync(new MouseEventArgs());
		cut.WaitForAssertion(() => Assert.True(cut.Find("[role=dialog]").HasAttribute("aria-busy")));

		await cut.InvokeAsync(Dialogs.CloseAll);

		Assert.False(await answer);
		cut.WaitForAssertion(() => Assert.True(seen.IsCancellationRequested));
		await click;
		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role=dialog]")));
	}

	[Fact]
	public async Task AConfirmWithoutWork_ClosesAsSoonAsItIsConfirmed()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<bool> answer = Dialogs.ConfirmAsync("Delete?", "Delete");
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));

		Assert.False(Button(cut, "OK").ClassName!.Contains("moka-btn--loading", StringComparison.Ordinal));
		await Button(cut, "OK").ClickAsync(new MouseEventArgs());

		Assert.True(await answer);
		Assert.Empty(cut.FindAll(".moka-dialog-failure"));
	}

	[Fact]
	public async Task APrompt_IgnoresTheWork()
	{
		bool ran = false;
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<string?> answer = Dialogs.PromptAsync("Name?", "Rename", "Holidays", o => o.OnConfirmAsync = _ =>
		{
			ran = true;
			return Task.CompletedTask;
		});
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));

		await Button(cut, "OK").ClickAsync(new MouseEventArgs());

		Assert.Equal("Holidays", await answer);
		Assert.False(ran);
	}

	[Fact]
	public void TheOptions_InitialFocusReachesTheDialog()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		_ = Dialogs.ConfirmAsync("Delete?", "Delete", o => o.InitialFocus = MokaDialogInitialFocus.FirstControl);

		cut.WaitForAssertion(() => Assert.Equal(MokaDialogInitialFocus.FirstControl,
			cut.FindComponent<MokaDialog>().Instance.InitialFocus));
	}

	// A busy button keeps a hidden status ("Loading") inside it, which isn't part of its label.
	private static IElement Button(IRenderedComponent<MokaDialogHost> cut, string text) =>
		cut.FindAll("[role=dialog] .moka-dialog-actions button").Single(b => Label(b) == text);

	private static string Label(IElement button) => string.Concat(button.Descendants<IText>()
		.Where(node => node.ParentElement?.Closest(".moka-visually-hidden") is null)
		.Select(node => node.Data)).Trim();
}
