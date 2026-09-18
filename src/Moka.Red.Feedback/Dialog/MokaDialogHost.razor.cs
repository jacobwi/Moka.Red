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

	// Bound straight to the request so the service can read the entered text when
	// something other than this host (a consumer calling Close(true)) confirms the prompt.
	private string PromptValue
	{
		get => _activeRequest?.CurrentValue ?? string.Empty;
		set
		{
			if (_activeRequest is not null)
			{
				_activeRequest.CurrentValue = value;
			}
		}
	}

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

	private void HandleDialogRequested(MokaDialogRequest request) => _ = ApplyAsync(() =>
	{
		_activeRequest = request;
		_dialogContext = request.Type == MokaDialogType.Component
			? new MokaDialogContext(DialogService)
			: null;
	});

	private void HandleDialogClosed() => _ = ApplyAsync(() =>
	{
		_activeRequest = null;
		_dialogContext = null;
	});

	// The service raises its events on whatever thread called it. State is only changed on the
	// renderer's thread, and the task is observed here: from an async void, any exception would
	// have been rethrown on the thread pool and taken the whole app down.
	private async Task ApplyAsync(Action change)
	{
		if (_disposed)
		{
			return;
		}

		try
		{
			await InvokeAsync(() =>
			{
				if (_disposed)
				{
					return;
				}

				change();
				StateHasChanged();
			});
		}
		catch (ObjectDisposedException)
		{
			// The renderer went away between the event and the dispatch.
		}
		catch (Exception ex) when (!_disposed)
		{
			// Anything else goes to Blazor's own error handling (error boundaries, the circuit
			// log) the same way an exception from a lifecycle method would.
			await DispatchExceptionAsync(ex);
		}
	}

	// Closing always goes through the service: it owns the completion source, the queue,
	// and the OnDialogClosed notification that clears this host's state.
	private void HandleConfirm() => DialogService.Close(true);

	private void HandleCancel() => DialogService.Close(false);

	private void HandleClose() => HandleCancel();
}
