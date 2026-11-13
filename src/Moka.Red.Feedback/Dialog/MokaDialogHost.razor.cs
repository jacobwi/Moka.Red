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

	// Requests whose dialogs are playing their exit animation. They left the service's list, but
	// stay mounted with Open=false until the dialog reports itself gone, so closing one fades or
	// slides out instead of blinking off with the host's re-render. Reference-compared for the
	// same reason as _contexts.
	private readonly List<MokaDialogRequest> _exiting = [];
	private IReadOnlyList<MokaDialogRequest> _open = [];

	// The confirms whose OnConfirmAsync has run at least once: the attempt running and the last failure. By reference,
	// like _contexts.
	private readonly Dictionary<MokaDialogRequest, ConfirmWork> _work = new(ReferenceEqualityComparer.Instance);
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

		// The work goes on without a dialog to show it; asking it to stop is all that is left to do.
		foreach (ConfirmWork work in _work.Values)
		{
			work.Stop();
		}

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
		IReadOnlyList<MokaDialogRequest> open = DialogService.OpenDialogs;

		// Whichever of the dialogs just rendered left the service's list has been closed; mounted
		// here with Open=false, its dialog animates out on its own.
		foreach (MokaDialogRequest request in _open)
		{
			if (!open.Contains(request) && !_exiting.Contains(request))
			{
				_exiting.Add(request);
			}
		}

		_open = open;

		foreach (MokaDialogRequest closed in _contexts.Keys
			         .Where(r => !_open.Contains(r) && !_exiting.Contains(r))
			         .ToList())
		{
			_contexts.Remove(closed);
		}

		// A confirm closed through the service (Close, CloseAll) while its work runs asks the work to stop; its call has
		// already returned. The state stays while the dialog exits, so a failure shown in it doesn't vanish mid-fade.
		foreach (MokaDialogRequest closed in _work.Keys.Where(r => !_open.Contains(r)).ToList())
		{
			_work[closed].Stop();
			if (!_exiting.Contains(closed))
			{
				_work.Remove(closed);
			}
		}
	}

	// The open stack, then the dialogs on their way out - a closing dialog stays above the one
	// beneath it while it goes.
	private IReadOnlyList<MokaDialogRequest> RenderedRequests => [.. _open, .. _exiting];

	// Exiting dialogs render with Open=false, which starts the exit in the component itself for
	// a close that came from the service rather than the dialog's own controls.
	private bool OpenFor(MokaDialogRequest request) => !_exiting.Contains(request);

	// The dialog's markup is gone; the request can stop being rendered and its context released.
	private void Exited(MokaDialogRequest request)
	{
		if (_exiting.RemoveAll(r => ReferenceEquals(r, request)) > 0)
		{
			_contexts.Remove(request);
			_work.Remove(request);
			StateHasChanged();
		}
	}

	// One context per dialog for its whole life, so the cascaded value keeps its identity and a
	// component inside closes its own dialog, not whichever one is on top.
	// A component the trimmer can follow through ComponentType's annotation, which it cannot through
	// DynamicComponent's Type parameter.
	private static RenderFragment ComponentContent(MokaDialogRequest request) => builder =>
	{
		builder.OpenComponent(0, request.ComponentType!);
		if (request.ComponentParameters is not null)
		{
			foreach (KeyValuePair<string, object> parameter in request.ComponentParameters)
			{
				builder.AddComponentParameter(1, parameter.Key, parameter.Value);
			}
		}

		builder.CloseComponent();
	};

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

	private const string DefaultFailedText = "Something went wrong.";

	private bool IsWorking(MokaDialogRequest request) => _work.TryGetValue(request, out ConfirmWork? work) && work.Running;

	private bool IsCancelling(MokaDialogRequest request) => _work.TryGetValue(request, out ConfirmWork? work) && work.Cancelling;

	private string? FailureOf(MokaDialogRequest request) => _work.TryGetValue(request, out ConfirmWork? work) ? work.Failure : null;

	// Closing goes through the service: it owns each dialog's completion and raises the event that
	// re-renders this host without the closed dialog. A confirm with work closes once the work has
	// succeeded, and stays open showing why when it fails.
	private async Task ConfirmAsync(MokaDialogRequest request)
	{
		Func<CancellationToken, Task>? run = request.Type == MokaDialogType.Confirm ? request.Options.OnConfirmAsync : null;
		if (run is null)
		{
			DialogService.Close(request, true);
			return;
		}

		if (!_work.TryGetValue(request, out ConfirmWork? work))
		{
			work = new ConfirmWork();
			_work[request] = work;
		}

		if (work.Running)
		{
			return;
		}

		using CancellationTokenSource attempt = new();
		CancellationToken token = work.Begin(attempt);
		bool confirmed = true;
		try
		{
			await run(token);
		}
#pragma warning disable CA1031 // Whatever the work throws is what the person reads, and the dialog stays open for another try.
		catch (Exception ex)
#pragma warning restore CA1031
		{
			if (!token.IsCancellationRequested)
			{
				work.Failure = request.Options.FailedText
				               ?? (string.IsNullOrWhiteSpace(ex.Message) ? DefaultFailedText : ex.Message);
				return;
			}

			confirmed = false;
		}
		finally
		{
			work.End();
		}

		// Already closed when the service closed it while the work ran, and then this does nothing.
		DialogService.Close(request, confirmed);
	}

	// While a confirm's work runs, Cancel asks it to stop and the dialog closes once it has.
	private void Cancel(MokaDialogRequest request)
	{
		if (_work.TryGetValue(request, out ConfirmWork? work) && work.Running)
		{
			work.Stop();
			return;
		}

		DialogService.Close(request, false);
	}

	// One confirm's work: the attempt running, if any, and why the last one failed. Only touched on the renderer's
	// thread, so the attempt is never cancelled after its owner disposed it.
	private sealed class ConfirmWork
	{
		private CancellationTokenSource? _attempt;

		public bool Running => _attempt is not null;

		public bool Cancelling { get; private set; }

		public string? Failure { get; set; }

		public CancellationToken Begin(CancellationTokenSource attempt)
		{
			_attempt = attempt;
			Cancelling = false;
			Failure = null;
			return attempt.Token;
		}

		public void End()
		{
			_attempt = null;
			Cancelling = false;
		}

		public void Stop()
		{
			if (_attempt is { IsCancellationRequested: false } attempt)
			{
				Cancelling = true;
				attempt.Cancel();
			}
		}
	}
}
