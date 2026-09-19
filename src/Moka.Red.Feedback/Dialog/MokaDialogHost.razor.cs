using Microsoft.AspNetCore.Components;

namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     Renders service-triggered dialogs. Place once in the application layout.
///     Subscribes to <see cref="IMokaDialogService" /> events and shows every open dialog,
///     stacked in the order they were opened.
/// </summary>
public sealed partial class MokaDialogHost : IDisposable
{
	// By reference: a request is a record, and a prompt's CurrentValue (part of its value) changes
	// with every keystroke.
	private readonly Dictionary<MokaDialogRequest, MokaDialogContext> _contexts = new(ReferenceEqualityComparer.Instance);
	private IReadOnlyList<MokaDialogRequest> _open = [];
	private bool _disposed;

	/// <summary>The dialog service providing requests.</summary>
	[Inject]
	private IMokaDialogService DialogService { get; set; } = default!;

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

		// Dialogs opened before this host mounted still show.
		Refresh();
	}

	private void HandleDialogRequested(MokaDialogRequest request) => _ = ApplyAsync(Refresh);

	private void HandleDialogClosed() => _ = ApplyAsync(Refresh);

	private void Refresh()
	{
		_open = DialogService.OpenDialogs;

		foreach (MokaDialogRequest closed in _contexts.Keys.Where(r => !_open.Contains(r)).ToList())
		{
			_contexts.Remove(closed);
		}
	}

	// One context per dialog for its whole life, so the cascaded value keeps its identity and a
	// component inside closes its own dialog, not whichever one is on top.
	private MokaDialogContext ContextFor(MokaDialogRequest request)
	{
		if (!_contexts.TryGetValue(request, out MokaDialogContext? context))
		{
			context = new MokaDialogContext(DialogService, request);
			_contexts[request] = context;
		}

		return context;
	}

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

	// Closing goes through the service: it owns each dialog's completion and raises the event that
	// re-renders this host without the closed dialog.
	private void Confirm(MokaDialogRequest request) => DialogService.Close(request, true);

	private void Cancel(MokaDialogRequest request) => DialogService.Close(request, false);
}
