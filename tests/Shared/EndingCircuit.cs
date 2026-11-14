using Bunit;
using Microsoft.JSInterop;

namespace Moka.Red.Tests.Shared;

/// <summary>
///     A JS module that answers until <see cref="End" />, then fails every call and its own disposal
///     with a <see cref="TaskCanceledException" />, the way Blazor Server fails a call still on its way
///     when the circuit ends under it. bUnit's own module references never fail to dispose. After
///     <see cref="Reload" /> it fails as a BlazorWebView does once its page has reloaded.
/// </summary>
internal sealed class EndingModule : IJSObjectReference
{
	/// <summary>
	///     What Blazor's JS side answers a call on an object it doesn't hold with, in .NET 9 and 10: the
	///     error's message, then its stack.
	/// </summary>
	public const string MissingObjectMessage =
		"JS object instance with ID 39 does not exist (has it been disposed?).\n" +
		"Error: JS object instance with ID 39 does not exist (has it been disposed?).\n" +
		"    at S (https://0.0.0.1/_framework/blazor.webview.js:1:2484)";

	private readonly Dictionary<string, object?> _results = new(StringComparer.Ordinal);
	private bool _disposeFails;
	private Func<Exception>? _failure;

	/// <summary>Every function called on the module, in order, including the ones that failed.</summary>
	public List<string> Calls { get; } = [];

	/// <summary>How many times the module was asked to dispose itself.</summary>
	public int DisposeAttempts { get; private set; }

	/// <summary>Whether the browser side is gone, through <see cref="End" /> or <see cref="Reload" />.</summary>
	public bool Ended => _failure is not null;

	/// <summary>Makes <paramref name="identifier" /> return <paramref name="result" /> while the circuit lasts.</summary>
	public EndingModule Returns(string identifier, object? result)
	{
		_results[identifier] = result;
		return this;
	}

	/// <summary>Ends the circuit: from now on every call fails.</summary>
	public void End()
	{
		_failure = static () => new TaskCanceledException();
		_disposeFails = true;
	}

	/// <summary>
	///     Reloads the page under the module, as a BlazorWebView does: from now on every call on it reaches a
	///     page that never had it and fails with <see cref="MissingObjectMessage" />, while its disposal, a call
	///     on the page's global object that only forgets the id, still succeeds.
	/// </summary>
	public void Reload()
	{
		_failure = static () => new JSException(MissingObjectMessage);
		_disposeFails = false;
	}

	public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
		InvokeAsync<TValue>(identifier, CancellationToken.None, args);

	public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
	{
		Calls.Add(identifier);
		if (_failure is not null)
		{
			return ValueTask.FromException<TValue>(_failure());
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
		return _disposeFails ? ValueTask.FromException(new TaskCanceledException()) : ValueTask.CompletedTask;
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
