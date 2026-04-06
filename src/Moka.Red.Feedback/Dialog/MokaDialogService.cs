using Microsoft.AspNetCore.Components;

namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     Default implementation of <see cref="IMokaDialogService" />.
///     Uses <see cref="TaskCompletionSource{T}" /> to provide async dialog results.
/// </summary>
public sealed class MokaDialogService : IMokaDialogService
{
	private TaskCompletionSource<object?>? _currentCompletion;

	/// <inheritdoc />
	public event Action<MokaDialogRequest>? OnDialogRequested;

	/// <inheritdoc />
	public event Action? OnDialogClosed;

	/// <inheritdoc />
	public async Task<bool> ConfirmAsync(string message, string? title = null,
		Action<MokaDialogOptions>? configure = null)
	{
		var options = new MokaDialogOptions();
		configure?.Invoke(options);

		var tcs = new TaskCompletionSource<object?>();
		_currentCompletion = tcs;

		var request = new MokaDialogRequest
		{
			Title = title ?? "Confirm",
			Message = message,
			Options = options,
			Type = MokaDialogType.Confirm,
			Completion = tcs
		};

		OnDialogRequested?.Invoke(request);

		object? result = await tcs.Task;
		return result is true;
	}

	/// <inheritdoc />
	public async Task<string?> PromptAsync(string message, string? title = null, string? defaultValue = null)
	{
		var tcs = new TaskCompletionSource<object?>();
		_currentCompletion = tcs;

		var request = new MokaDialogRequest
		{
			Title = title ?? "Input",
			Message = message,
			Options = new MokaDialogOptions(),
			Type = MokaDialogType.Prompt,
			DefaultValue = defaultValue,
			Completion = tcs
		};

		OnDialogRequested?.Invoke(request);

		object? result = await tcs.Task;
		return result as string;
	}

	/// <inheritdoc />
	public async Task ShowAsync(string title, RenderFragment content, Action<MokaDialogOptions>? configure = null)
	{
		var options = new MokaDialogOptions();
		configure?.Invoke(options);

		var tcs = new TaskCompletionSource<object?>();
		_currentCompletion = tcs;

		var request = new MokaDialogRequest
		{
			Title = title,
			Content = content,
			Options = options,
			Type = MokaDialogType.Show,
			Completion = tcs
		};

		OnDialogRequested?.Invoke(request);

		await tcs.Task;
	}

	/// <inheritdoc />
	public async Task<object?> ShowComponentAsync<TComponent>(
		string title,
		Action<Dictionary<string, object>>? parameters = null,
		Action<MokaDialogOptions>? configure = null) where TComponent : IComponent
	{
		var options = new MokaDialogOptions();
		configure?.Invoke(options);

		var componentParams = new Dictionary<string, object>();
		parameters?.Invoke(componentParams);

		var tcs = new TaskCompletionSource<object?>();
		_currentCompletion = tcs;

		var request = new MokaDialogRequest
		{
			Title = title,
			Options = options,
			Type = MokaDialogType.Component,
			ComponentType = typeof(TComponent),
			ComponentParameters = componentParams,
			Completion = tcs
		};

		OnDialogRequested?.Invoke(request);

		return await tcs.Task;
	}

	/// <inheritdoc />
	public void Close(bool result = false)
	{
		_currentCompletion?.TrySetResult(result ? true : null);
		_currentCompletion = null;
		OnDialogClosed?.Invoke();
	}

	/// <inheritdoc />
	public void CloseWithResult(object? result)
	{
		_currentCompletion?.TrySetResult(result);
		_currentCompletion = null;
		OnDialogClosed?.Invoke();
	}
}
