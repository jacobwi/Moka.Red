using Microsoft.AspNetCore.Components;
using Moka.Red.Feedback.Dialog;

namespace Moka.Red.Feedback.Tests.Services;

/// <summary>
///     Covers stacking and result coercion. Previously a second dialog
///     overwrote the pending TaskCompletionSource and the first await never completed,
///     and Close(false) resolved to null rather than false.
/// </summary>
public class MokaDialogServiceTests
{
	[Fact]
	public async Task ConfirmAsync_ReturnsTrue_WhenClosedWithTrue()
	{
		using var service = new MokaDialogService();

		Task<bool> pending = service.ConfirmAsync("Delete this?");
		service.Close(true);

		Assert.True(await pending);
	}

	[Fact]
	public async Task ConfirmAsync_ReturnsFalse_WhenClosedWithFalse()
	{
		using var service = new MokaDialogService();

		Task<bool> pending = service.ConfirmAsync("Delete this?");
		service.Close(false);

		Assert.False(await pending);
	}

	[Fact]
	public async Task SecondDialog_OpensOnTop_AndBothComplete()
	{
		// The original service dropped the first request's TaskCompletionSource on the
		// floor, leaving a permanently pending task.
		using var service = new MokaDialogService();

		Task<bool> first = service.ConfirmAsync("First");
		Task<bool> second = service.ConfirmAsync("Second");

		service.Close(true);
		Assert.True(await second);
		Assert.False(first.IsCompleted);

		service.Close(false);
		Assert.False(await first);
	}

	[Fact]
	public void OnDialogRequested_FiresForEveryRequest()
	{
		using var service = new MokaDialogService();
		int raised = 0;
		service.OnDialogRequested += _ => raised++;

		_ = service.ConfirmAsync("First");
		_ = service.ConfirmAsync("Second");

		Assert.Equal(2, raised);
		Assert.Equal(["First", "Second"], service.OpenDialogs.Select(d => d.Message));
	}

	// Requests used to queue behind the open dialog, so a dialog whose action awaited another
	// dialog waited forever behind itself.
	[Fact]
	public async Task ADialogsAction_CanAwaitAnotherDialog()
	{
		using var service = new MokaDialogService();

		async Task<bool> DeleteAsync()
		{
			bool sure = await service.ConfirmAsync("Really delete?");
			return sure;
		}

		Task<object?> outer = service.ShowComponentAsync<DummyComponent>("Files");
		Task<bool> action = DeleteAsync();
		Assert.Equal(2, service.OpenDialogs.Count);

		service.Close(true);
		Assert.True(await action);
		Assert.False(outer.IsCompleted);

		service.Close(false);
		Assert.Null(await outer);
	}

	[Fact]
	public async Task ClosingADialogBelowTheTop_LeavesTheTopOpen()
	{
		using var service = new MokaDialogService();
		Task<bool> lower = service.ConfirmAsync("Lower");
		Task<bool> upper = service.ConfirmAsync("Upper");

		service.Close(service.OpenDialogs[0], true);

		Assert.True(await lower);
		Assert.False(upper.IsCompleted);
		Assert.Equal("Upper", Assert.Single(service.OpenDialogs).Message);
	}

	[Fact]
	public async Task CloseWithResult_ForARequest_DeliversThatResult()
	{
		using var service = new MokaDialogService();
		Task<object?> picker = service.ShowComponentAsync<DummyComponent>("Pick");
		_ = service.ConfirmAsync("On top");

		service.CloseWithResult(service.OpenDialogs[0], "picked");

		Assert.Equal("picked", await picker);
	}

	[Fact]
	public async Task CloseAll_CancelsEveryOpenDialog()
	{
		using var service = new MokaDialogService();
		int closedEvents = 0;
		service.OnDialogClosed += () => closedEvents++;
		Task<bool> confirm = service.ConfirmAsync("A");
		Task<string?> prompt = service.PromptAsync("B", null, "typed");
		Task<object?> component = service.ShowComponentAsync<DummyComponent>("C");

		service.CloseAll();

		Assert.False(await confirm);
		Assert.Null(await prompt);
		Assert.Null(await component);
		Assert.Empty(service.OpenDialogs);
		Assert.Equal(1, closedEvents);
	}

	[Fact]
	public void Close_WithNothingOpen_DoesNothing()
	{
		using var service = new MokaDialogService();
		bool closed = false;
		service.OnDialogClosed += () => closed = true;

		service.Close(true);
		service.CloseAll();

		Assert.False(closed);
	}

	[Fact]
	public async Task OnDialogClosed_IsRaised_WhenADialogCloses()
	{
		using var service = new MokaDialogService();
		bool closed = false;
		service.OnDialogClosed += () => closed = true;

		Task<bool> pending = service.ConfirmAsync("Anything");
		service.Close(true);
		await pending;

		Assert.True(closed);
	}

	[Fact]
	public async Task PromptAsync_ReturnsNull_WhenCancelled()
	{
		using var service = new MokaDialogService();

		Task<string?> pending = service.PromptAsync("Your name?");
		service.Close(false);

		Assert.Null(await pending);
	}

	[Fact]
	public async Task PromptAsync_ReturnsTheValue_WhenClosedWithResult()
	{
		using var service = new MokaDialogService();

		Task<string?> pending = service.PromptAsync("Your name?");
		service.CloseWithResult("Ada");

		Assert.Equal("Ada", await pending);
	}

	[Fact]
	public void PromptAsync_AcceptsAConfigureOverload()
	{
		using var service = new MokaDialogService();
		MokaDialogOptions? captured = null;
		service.OnDialogRequested += r => captured = r.Options;

		_ = service.PromptAsync("Your name?", "Title", "default", o => o.ConfirmText = "Save");

		Assert.NotNull(captured);
		Assert.Equal("Save", captured.ConfirmText);
	}

	[Fact]
	public async Task Dispose_ResolvesEveryPendingRequest()
	{
		// Circuit teardown must not leave awaiting callers hanging forever.
		using var service = new MokaDialogService();
		Task<bool> first = service.ConfirmAsync("First");
		Task<bool> second = service.ConfirmAsync("Second");

		service.Dispose();

		await first;
		await second;
		Assert.True(first.IsCompleted && second.IsCompleted);
	}

	private sealed class DummyComponent : ComponentBase;
}
