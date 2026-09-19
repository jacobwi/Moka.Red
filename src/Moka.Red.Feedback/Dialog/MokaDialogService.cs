using Microsoft.AspNetCore.Components;

namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     Default implementation of <see cref="IMokaDialogService" />.
///     Uses <see cref="TaskCompletionSource{T}" /> to provide async dialog results. Every request
///     opens straight away, on top of any dialog already open, and completes exactly once.
/// </summary>
public sealed class MokaDialogService : IMokaDialogService, IDisposable
{
	private readonly object _lock = new();

	// Bottom first; the last one is on top. Requests used to queue behind the open dialog, so a
	// dialog whose action awaited another dialog waited forever behind itself.
	private readonly List<MokaDialogRequest> _open = [];
	private bool _disposed;

	/// <summary>
	///     Resolves every open and queued dialog as cancelled so no caller is left awaiting
	///     a task that can never complete.
	/// </summary>
	public void Dispose()
	{
		List<MokaDialogRequest> orphaned;

		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			orphaned = [.. _open];
			_open.Clear();
		}

		foreach (MokaDialogRequest request in orphaned)
		{
			request.Completion?.TrySetResult(CoerceResult(request, false));
		}
	}

	/// <inheritdoc />
	public event Action<MokaDialogRequest>? OnDialogRequested;

	/// <inheritdoc />
	public event Action? OnDialogClosed;

	/// <inheritdoc />
	public IReadOnlyList<MokaDialogRequest> OpenDialogs
	{
		get
		{
			lock (_lock)
			{
				return [.. _open];
			}
		}
	}

	/// <inheritdoc />
	public async Task<bool> ConfirmAsync(string message, string? title = null,
		Action<MokaDialogOptions>? configure = null)
	{
		TaskCompletionSource<object?> tcs = CreateCompletion();

		var request = new MokaDialogRequest
		{
			Title = title ?? "Confirm",
			Message = message,
			Options = BuildOptions(configure),
			Type = MokaDialogType.Confirm,
			Completion = tcs
		};

		object? result = await SubmitAsync(request, tcs);
		return result is true;
	}

	/// <inheritdoc />
	public Task<string?> PromptAsync(string message, string? title = null, string? defaultValue = null)
		=> PromptAsync(message, title, defaultValue, null);

	/// <inheritdoc />
	public async Task<string?> PromptAsync(string message, string? title, string? defaultValue,
		Action<MokaDialogOptions>? configure)
	{
		TaskCompletionSource<object?> tcs = CreateCompletion();

		var request = new MokaDialogRequest
		{
			Title = title ?? "Input",
			Message = message,
			Options = BuildOptions(configure),
			Type = MokaDialogType.Prompt,
			DefaultValue = defaultValue,
			CurrentValue = defaultValue,
			Completion = tcs
		};

		object? result = await SubmitAsync(request, tcs);
		return result as string;
	}

	/// <inheritdoc />
	public async Task ShowAsync(string title, RenderFragment content, Action<MokaDialogOptions>? configure = null)
	{
		TaskCompletionSource<object?> tcs = CreateCompletion();

		var request = new MokaDialogRequest
		{
			Title = title,
			Content = content,
			Options = BuildOptions(configure),
			Type = MokaDialogType.Show,
			Completion = tcs
		};

		await SubmitAsync(request, tcs);
	}

	/// <inheritdoc />
	public async Task<object?> ShowComponentAsync<TComponent>(
		string title,
		Action<Dictionary<string, object>>? parameters = null,
		Action<MokaDialogOptions>? configure = null) where TComponent : IComponent
	{
		var componentParams = new Dictionary<string, object>();
		parameters?.Invoke(componentParams);

		TaskCompletionSource<object?> tcs = CreateCompletion();

		var request = new MokaDialogRequest
		{
			Title = title,
			Options = BuildOptions(configure),
			Type = MokaDialogType.Component,
			ComponentType = typeof(TComponent),
			ComponentParameters = componentParams,
			Completion = tcs
		};

		return await SubmitAsync(request, tcs);
	}

	/// <inheritdoc />
	public void Close(bool result = false) => CloseTop(request => CoerceResult(request, result));

	/// <inheritdoc />
	public void Close(MokaDialogRequest request, bool result = false)
	{
		ArgumentNullException.ThrowIfNull(request);
		CloseRequest(request, CoerceResult(request, result));
	}

	/// <inheritdoc />
	public void CloseWithResult(object? result) => CloseTop(_ => result);

	/// <inheritdoc />
	public void CloseWithResult(MokaDialogRequest request, object? result)
	{
		ArgumentNullException.ThrowIfNull(request);
		CloseRequest(request, result);
	}

	/// <inheritdoc />
	public void CloseAll()
	{
		List<MokaDialogRequest> closed;

		lock (_lock)
		{
			if (_open.Count == 0)
			{
				return;
			}

			closed = [.. _open];
			_open.Clear();
		}

		// Top first, the order a user would dismiss them in.
		for (int i = closed.Count - 1; i >= 0; i--)
		{
			closed[i].Completion?.TrySetResult(CoerceResult(closed[i], false));
		}

		OnDialogClosed?.Invoke();
	}

	private static MokaDialogOptions BuildOptions(Action<MokaDialogOptions>? configure)
	{
		var options = new MokaDialogOptions();
		configure?.Invoke(options);
		return options;
	}

	// Continuations must not run inline on whichever thread happens to close the dialog:
	// the awaiting caller could re-enter the service before its own dialog is off the stack.
	private static TaskCompletionSource<object?> CreateCompletion()
		=> new(TaskCreationOptions.RunContinuationsAsynchronously);

	// Close(bool) carries a yes/no answer. Each dialog type needs that answer in its own
	// result shape, otherwise PromptAsync sees a boxed bool and ShowComponentAsync sees
	// "false" where its contract promises null.
	private static object? CoerceResult(MokaDialogRequest request, bool confirmed) => request.Type switch
	{
		MokaDialogType.Prompt => confirmed ? request.CurrentValue ?? string.Empty : null,
		MokaDialogType.Component => confirmed ? (object?)true : null,
		_ => confirmed
	};

	private Task<object?> SubmitAsync(MokaDialogRequest request, TaskCompletionSource<object?> completion)
	{
		lock (_lock)
		{
			if (_disposed)
			{
				completion.TrySetResult(CoerceResult(request, false));
				return completion.Task;
			}

			_open.Add(request);
		}

		OnDialogRequested?.Invoke(request);
		return completion.Task;
	}

	private void CloseTop(Func<MokaDialogRequest, object?> resultFactory)
	{
		MokaDialogRequest top;

		lock (_lock)
		{
			if (_open.Count == 0)
			{
				return;
			}

			top = _open[^1];
			_open.RemoveAt(_open.Count - 1);
		}

		top.Completion?.TrySetResult(resultFactory(top));
		OnDialogClosed?.Invoke();
	}

	private void CloseRequest(MokaDialogRequest request, object? result)
	{
		lock (_lock)
		{
			if (!_open.Remove(request))
			{
				return;
			}
		}

		request.Completion?.TrySetResult(result);
		OnDialogClosed?.Invoke();
	}
}
