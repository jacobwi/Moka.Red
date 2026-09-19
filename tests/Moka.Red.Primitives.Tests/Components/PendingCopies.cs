using Bunit;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>
///     Answers each <c>copyToClipboard</c> call with a task the test completes, so two copies can be made
///     to finish together.
/// </summary>
internal sealed class PendingCopies() : JSRuntimeInvocationHandlerBase<bool>(
	invocation => invocation.Identifier == "copyToClipboard", isCatchAllHandler: false)
{
	private readonly List<TaskCompletionSource<bool>> _calls = [];
	private readonly Lock _lock = new();

	public int Count
	{
		get
		{
			lock (_lock)
			{
				return _calls.Count;
			}
		}
	}

	/// <summary>Finishes call <paramref name="index" /> as a successful copy.</summary>
	public void Complete(int index)
	{
		TaskCompletionSource<bool> call;
		lock (_lock)
		{
			call = _calls[index];
		}

		call.SetResult(true);
	}

	/// <summary>Waits until at least <paramref name="count" /> calls have been made.</summary>
	public void WaitFor(int count) =>
		Assert.True(SpinWait.SpinUntil(() => Count >= count, TimeSpan.FromSeconds(5)),
			$"Expected {count} copy calls, saw {Count}.");

	protected override Task<bool> HandleAsync(JSRuntimeInvocation invocation)
	{
		// The base records the invocation. Its own task never completes, so each call gets one of these.
		_ = base.HandleAsync(invocation);
		var call = new TaskCompletionSource<bool>();
		lock (_lock)
		{
			_calls.Add(call);
		}

		return call.Task;
	}
}
