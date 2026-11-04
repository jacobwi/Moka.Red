using Bunit;
using Microsoft.JSInterop;

namespace Moka.Red.Tests.Shared;

/// <summary>
///     A JS module that answers until <see cref="End" />, then fails every call and its own disposal
///     with a <see cref="TaskCanceledException" />, the way Blazor Server fails a call still on its way
///     when the circuit ends under it. bUnit's own module references never fail to dispose.
/// </summary>
internal sealed class EndingModule : IJSObjectReference
{
	private readonly Dictionary<string, object?> _results = new(StringComparer.Ordinal);

	/// <summary>Every function called on the module, in order, including the ones that failed.</summary>
	public List<string> Calls { get; } = [];

	/// <summary>How many times the module was asked to dispose itself.</summary>
	public int DisposeAttempts { get; private set; }

	/// <summary>Whether the circuit has ended.</summary>
	public bool Ended { get; private set; }

	/// <summary>Makes <paramref name="identifier" /> return <paramref name="result" /> while the circuit lasts.</summary>
	public EndingModule Returns(string identifier, object? result)
	{
		_results[identifier] = result;
		return this;
	}

	/// <summary>Ends the circuit: from now on every call fails.</summary>
	public void End() => Ended = true;

	public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
		InvokeAsync<TValue>(identifier, CancellationToken.None, args);

	public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
	{
		Calls.Add(identifier);
		if (Ended)
		{
			return ValueTask.FromException<TValue>(new TaskCanceledException());
		}

		return ValueTask.FromResult(_results.TryGetValue(identifier, out object? result) && result is TValue value
			? value
			: default!);
	}

	public ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, object?[]? args) =>
		InvokeAsync<IJSObjectReference>(identifier, CancellationToken.None, args);

	public ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, CancellationToken cancellationToken,
		object?[]? args) => InvokeAsync<IJSObjectReference>(identifier, cancellationToken, args);

	public ValueTask<TValue> GetValueAsync<TValue>(string identifier) =>
		InvokeAsync<TValue>(identifier, CancellationToken.None, null);

	public ValueTask<TValue> GetValueAsync<TValue>(string identifier, CancellationToken cancellationToken) =>
		InvokeAsync<TValue>(identifier, cancellationToken, null);

	public async ValueTask SetValueAsync<TValue>(string identifier, TValue value) =>
		await InvokeAsync<object?>(identifier, CancellationToken.None, [value]);

	public async ValueTask SetValueAsync<TValue>(string identifier, TValue value, CancellationToken cancellationToken) =>
		await InvokeAsync<object?>(identifier, cancellationToken, [value]);

	public ValueTask DisposeAsync()
	{
		DisposeAttempts++;
		return Ended ? ValueTask.FromException(new TaskCanceledException()) : ValueTask.CompletedTask;
	}
}

/// <summary>
///     The import of one module path, left pending until the test completes it with a module of its
///     own or cancels it, as the circuit ending cancels an import still on its way.
/// </summary>
internal sealed class ControlledImport(string path) : JSRuntimeInvocationHandlerBase<IJSObjectReference>(
	invocation => invocation.Identifier == "import" && Equals(invocation.Arguments[0], path),
	isCatchAllHandler: false)
{
	/// <summary>Completes the import with <paramref name="module" />.</summary>
	public void Complete(IJSObjectReference module) => SetResultBase(module);

	/// <summary>Cancels the import, which its callers see as a <see cref="TaskCanceledException" />.</summary>
	public void Cancel() => SetCanceledBase();
}
