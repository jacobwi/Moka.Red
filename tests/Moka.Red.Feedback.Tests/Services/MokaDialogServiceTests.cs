using Moka.Red.Feedback.Dialog;

namespace Moka.Red.Feedback.Tests.Services;

/// <summary>
///     Covers the queueing and result-coercion behaviour. Previously a second dialog
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
	public async Task SecondDialog_StillCompletes_WhenTwoAreOpened()
	{
		// The original service dropped the first request's TaskCompletionSource on the
		// floor, leaving a permanently pending task.
		using var service = new MokaDialogService();

		Task<bool> first = service.ConfirmAsync("First");
		Task<bool> second = service.ConfirmAsync("Second");

		service.Close(true);
		Assert.True(await first);

		service.Close(false);
		Assert.False(await second);
	}

	[Fact]
	public void OnDialogRequested_FiresForTheFirstRequestOnly_WhileOneIsOpen()
	{
		using var service = new MokaDialogService();
		int raised = 0;
		service.OnDialogRequested += _ => raised++;

		_ = service.ConfirmAsync("First");
		_ = service.ConfirmAsync("Second");

		Assert.Equal(1, raised);
	}

	[Fact]
	public void OnDialogRequested_FiresForTheQueuedRequest_AfterTheFirstCloses()
	{
		using var service = new MokaDialogService();
		int raised = 0;
		service.OnDialogRequested += _ => raised++;

		_ = service.ConfirmAsync("First");
		_ = service.ConfirmAsync("Second");
		service.Close(true);

		Assert.Equal(2, raised);
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
}
