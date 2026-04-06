using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;

namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     Renders service-triggered dialogs. Place once in the application layout.
///     Subscribes to <see cref="IMokaDialogService" /> events and provides
///     the imperative ConfirmAsync, PromptAsync, and ShowAsync APIs.
/// </summary>
public sealed partial class MokaDialogHost : IDisposable
{
	private MokaDialogRequest? _activeRequest;
	private MokaDialogContext? _dialogContext;
	private bool _disposed;
	private string _promptValue = "";

	/// <summary>The dialog service providing requests.</summary>
	[Inject]
	private IMokaDialogService DialogService { get; set; } = default!;

	private bool IsOpen => _activeRequest is not null;

	private MokaDialogSize CurrentSize => _activeRequest?.Options.Size ?? MokaDialogSize.Medium;
	private bool CurrentShowCloseButton => _activeRequest?.Options.ShowCloseButton ?? true;
	private bool CurrentCloseOnBackdrop => _activeRequest?.Options.CloseOnBackdropClick ?? true;
	private bool CurrentCloseOnEscape => _activeRequest?.Options.CloseOnEscape ?? true;
	private bool CurrentPreventScroll => _activeRequest?.Options.PreventScroll ?? true;

	private MokaColor ConfirmButtonColor => _activeRequest?.Options.ConfirmColor ?? MokaColor.Primary;

	/// <inheritdoc />
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		DialogService.OnDialogRequested -= HandleDialogRequested;
		DialogService.OnDialogClosed -= HandleDialogClosed;

		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		DialogService.OnDialogRequested += HandleDialogRequested;
		DialogService.OnDialogClosed += HandleDialogClosed;
	}

	private async void HandleDialogRequested(MokaDialogRequest request)
	{
		if (_disposed)
		{
			return;
		}

		try
		{
			_activeRequest = request;
			_promptValue = request.DefaultValue ?? "";
			_dialogContext = request.Type == MokaDialogType.Component
				? new MokaDialogContext(DialogService)
				: null;
			await InvokeAsync(StateHasChanged);
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private async void HandleDialogClosed()
	{
		if (_disposed)
		{
			return;
		}

		try
		{
			_activeRequest = null;
			_promptValue = "";
			await InvokeAsync(StateHasChanged);
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private void HandleConfirm()
	{
		if (_activeRequest is null)
		{
			return;
		}

		if (_activeRequest.Type == MokaDialogType.Prompt)
		{
			_activeRequest.Completion?.TrySetResult(_promptValue);
		}
		else
		{
			_activeRequest.Completion?.TrySetResult(true);
		}

		_activeRequest = null;
		_promptValue = "";
	}

	private void HandleCancel()
	{
		_activeRequest?.Completion?.TrySetResult(null);
		_activeRequest = null;
		_promptValue = "";
	}

	private void HandleClose() => HandleCancel();
}
