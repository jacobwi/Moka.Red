using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Tests.Shared;

#pragma warning disable CA2000 // SafeDisposeAsync disposes the stand-in module; the test counts that disposal.

namespace Moka.Red.Core.Tests.Base;

public class MokaJsInteropTests
{
	[Fact]
	public void IsCircuitGone_HoldsForALostOrEndedCircuit()
	{
		Assert.True(MokaJsInterop.IsCircuitGone(new JSDisconnectedException("The circuit was lost.")));
		Assert.True(MokaJsInterop.IsCircuitGone(new TaskCanceledException()));
		Assert.True(MokaJsInterop.IsCircuitGone(new OperationCanceledException()));
		Assert.True(MokaJsInterop.IsCircuitGone(new ObjectDisposedException("JSObjectReference")));
	}

	// What Blazor's JS side answers a call on an object it doesn't hold with, as a reloaded BlazorWebView page does.
	[Theory]
	[InlineData(EndingModule.MissingObjectMessage)]
	[InlineData("JS object instance with ID 7 does not exist (has it been disposed?).")]
	public void IsCircuitGone_HoldsForAnObjectThePageNoLongerHolds(string message) =>
		Assert.True(MokaJsInterop.IsCircuitGone(new JSException(message)));

	// A script error, or a message that only resembles the missing-object answer, is a fault worth hearing about.
	[Theory]
	[InlineData("TypeError: Cannot read properties of null (reading 'remove')")]
	[InlineData("Could not find 'detach' ('detach' was undefined).")]
	[InlineData("JS object instance with ID x does not exist (has it been disposed?).")]
	[InlineData("JS object instance with ID  does not exist (has it been disposed?).")]
	[InlineData("Error: JS object instance with ID 7 does not exist (has it been disposed?).")]
	[InlineData("JS object instance with ID 7 was replaced.")]
	[InlineData("")]
	public void IsCircuitGone_DoesNotHoldForAScriptError(string message) =>
		Assert.False(MokaJsInterop.IsCircuitGone(new JSException(message)));

	[Fact]
	public void IsCircuitGone_DoesNotHoldForOtherFailures() =>
		Assert.False(MokaJsInterop.IsCircuitGone(
			new InvalidOperationException("JavaScript interop calls cannot be issued at this time.")));

	[Fact]
	public async Task SafeInvokeVoidAsync_AfterAWebViewReload_ReportsTheCallUndone()
	{
		EndingModule module = new();
		module.Reload();

		bool done = await module.SafeInvokeVoidAsync("detach", 3);

		Assert.False(done);
		Assert.Equal(["detach"], module.Calls);
	}

	[Fact]
	public async Task SafeDisposeAsync_AfterAWebViewReload_DisposesTheReference()
	{
		EndingModule module = new();
		module.Reload();

		await module.SafeDisposeAsync();

		Assert.Equal(1, module.DisposeAttempts);
	}

	[Fact]
	public async Task SafeInvokeVoidAsync_StillThrowsAScriptError()
	{
		ThrowingModule module = new(new JSException("TypeError: observer is undefined"));

		await Assert.ThrowsAsync<JSException>(async () => await module.SafeInvokeVoidAsync("detach"));
	}

	private sealed class ThrowingModule(Exception failure) : IJSObjectReference
	{
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
			ValueTask.FromException<TValue>(failure);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
			object?[]? args) => ValueTask.FromException<TValue>(failure);

		public ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, object?[]? args) =>
			ValueTask.FromException<IJSObjectReference>(failure);

		public ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier,
			CancellationToken cancellationToken, object?[]? args) => ValueTask.FromException<IJSObjectReference>(failure);

		public ValueTask<TValue> GetValueAsync<TValue>(string identifier) => ValueTask.FromException<TValue>(failure);

		public ValueTask<TValue> GetValueAsync<TValue>(string identifier, CancellationToken cancellationToken) =>
			ValueTask.FromException<TValue>(failure);

		public ValueTask SetValueAsync<TValue>(string identifier, TValue value) => ValueTask.FromException(failure);

		public ValueTask SetValueAsync<TValue>(string identifier, TValue value, CancellationToken cancellationToken) =>
			ValueTask.FromException(failure);

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
}
