using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Base;
using TestContext = Xunit.TestContext;

namespace Moka.Red.Core.Tests.Base;

// RunDetached is for work nothing can await, such as a timer's callback. Its failures have to reach the
// renderer, the way a lifecycle method's do, and it has to be safe to call from a thread-pool thread.
public class RunDetachedTests : BunitContext
{
	private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

	public static TheoryData<bool> BothBases => [true, false];

	[Theory]
	[MemberData(nameof(BothBases))]
	public async Task AFailureFromAsyncWork_ReachesTheRenderer(bool input)
	{
		IDetached detached = RenderOne(input);

		await Task.Run(() => detached.Run(async () =>
		{
			await Task.Yield();
			throw new InvalidOperationException("Work failed");
		}), TestContext.Current.CancellationToken);

		Exception error = await Renderer.UnhandledException.WaitAsync(Wait, TestContext.Current.CancellationToken);
		Assert.Equal("Work failed", error.Message);
	}

	[Theory]
	[MemberData(nameof(BothBases))]
	public async Task AFailureFromSynchronousWork_ReachesTheRenderer(bool input)
	{
		IDetached detached = RenderOne(input);

		await Task.Run(() => detached.Run(() => throw new InvalidOperationException("Work failed")),
			TestContext.Current.CancellationToken);

		Exception error = await Renderer.UnhandledException.WaitAsync(Wait, TestContext.Current.CancellationToken);
		Assert.Equal("Work failed", error.Message);
	}

	// A disposed DbContext throws this as well, so it cannot be taken to mean the renderer went away.
	[Theory]
	[MemberData(nameof(BothBases))]
	public async Task AnObjectDisposedExceptionFromTheWork_ReachesTheRenderer(bool input)
	{
		IDetached detached = RenderOne(input);

		detached.Run(() => throw new ObjectDisposedException("DbContext"));

		Exception error = await Renderer.UnhandledException.WaitAsync(Wait, TestContext.Current.CancellationToken);
		Assert.IsType<ObjectDisposedException>(error);
	}

	[Theory]
	[MemberData(nameof(BothBases))]
	public async Task TheWork_RunsOnTheRenderersThread(bool input)
	{
		IDetached detached = RenderOne(input);
		var onRenderer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		await Task.Run(() => detached.Run(() => onRenderer.SetResult(Renderer.Dispatcher.CheckAccess())),
			TestContext.Current.CancellationToken);

		Assert.True(await onRenderer.Task.WaitAsync(Wait, TestContext.Current.CancellationToken));
	}

	[Theory]
	[MemberData(nameof(BothBases))]
	public async Task AfterDisposal_TheWorkDoesNotStart(bool input)
	{
		IDetached detached = RenderOne(input);
		bool ran = false;

		await DisposeComponentsAsync();
		detached.Run(() => ran = true);
		await Task.Delay(20, TestContext.Current.CancellationToken);

		Assert.False(ran);
		Assert.False(Renderer.UnhandledException.IsCompleted);
	}

	[Theory]
	[MemberData(nameof(BothBases))]
	public async Task AFailureAfterDisposal_IsNotReported(bool input)
	{
		IDetached detached = RenderOne(input);
		var answer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		detached.Run(async () => await answer.Task);

		await DisposeComponentsAsync();
		answer.SetException(new InvalidOperationException("Too late"));
		await Task.Delay(20, TestContext.Current.CancellationToken);

		Assert.False(Renderer.UnhandledException.IsCompleted);
	}

	private IDetached RenderOne(bool input) => input
		? Render<InputComponent>().Instance
		: Render<PlainComponent>().Instance;

	internal interface IDetached
	{
		void Run(Func<Task> work);

		void Run(Action work);
	}

	public sealed class PlainComponent : MokaComponentBase, IDetached
	{
		protected override string RootClass => "plain-component";

		void IDetached.Run(Func<Task> work) => RunDetached(work);

		void IDetached.Run(Action work) => RunDetached(work);

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
		}
	}

	public sealed class InputComponent : MokaInputBase<string>, IDetached
	{
		protected override string RootClass => "input-component";

		void IDetached.Run(Func<Task> work) => RunDetached(work);

		void IDetached.Run(Action work) => RunDetached(work);

		protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
		{
			result = value ?? "";
			validationErrorMessage = "";
			return true;
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
		}
	}
}
