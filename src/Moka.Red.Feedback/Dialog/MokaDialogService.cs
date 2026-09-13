using Microsoft.AspNetCore.Components;

namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     Default implementation of <see cref="IMokaDialogService" />.
///     Uses <see cref="TaskCompletionSource{T}" /> to provide async dialog results and queues
///     requests made while another dialog is open, so every call completes exactly once.
/// </summary>
public sealed class MokaDialogService : IMokaDialogService, IDisposable
{
	private readonly object _lock = new();
	private readonly Queue<MokaDialogRequest> _pending = new();
	private MokaDialogRequest? _current;
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

			orphaned = [];
			if (_current is not null)
			{
				orphaned.Add(_current);
				_current = null;
			}

			while (_pending.Count > 0)
			{
				orphaned.Add(_pending.Dequeue());
			}
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
	public void Close(bool result = false) => CloseCurrent(request => CoerceResult(request, result));

	/// <inheritdoc />
	public void CloseWithResult(object? result) => CloseCurrent(_ => result);

	private static MokaDialogOptions BuildOptions(Action<MokaDialogOptions>? configure)
	{
		var options = new MokaDialogOptions();
		configure?.Invoke(options);
		return options;
	}

	// Continuations must not run inline on whichever thread happens to close the dialog:
	// the awaiting caller could re-enter the service before the queue has advanced.
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
		bool showNow;

		lock (_lock)
		{
			if (_disposed)
			{
				completion.TrySetResult(CoerceResult(request, false));
				return completion.Task;
			}

			showNow = _current is null;
			if (showNow)
			{
				_current = request;
			}
			else
			{
				_pending.Enqueue(request);
			}
		}

		if (showNow)
		{
			OnDialogRequested?.Invoke(request);
		}

		return completion.Task;
	}

	private void CloseCurrent(Func<MokaDialogRequest, object?> resultFactory)
	{
		MokaDialogRequest? closed;
		MokaDialogRequest? next;

		lock (_lock)
		{
			closed = _current;
			if (closed is null)
			{
				return;
			}

			next = _pending.Count > 0 ? _pending.Dequeue() : null;
			_current = next;
		}

		closed.Completion?.TrySetResult(resultFactory(closed));
		OnDialogClosed?.Invoke();

		if (next is not null)
		{
			OnDialogRequested?.Invoke(next);
		}
	}
}
