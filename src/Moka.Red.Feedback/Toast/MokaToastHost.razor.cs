using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Internal;

namespace Moka.Red.Feedback.Toast;

/// <summary>
///     Renders active toast notifications. Place once in the application layout.
///     Subscribes to <see cref="IMokaToastService" /> events and displays toasts
///     with auto-dismiss, progress bars, and animations.
/// </summary>
public sealed partial class MokaToastHost : IDisposable
{
	private readonly List<MokaToastMessage> _toasts = [];
	private bool _disposed;

	/// <summary>The toast service that provides notifications.</summary>
	[Inject]
	private IMokaToastService ToastService { get; set; } = default!;

	/// <summary>Screen position of the toast container. Defaults to <see cref="MokaToastPosition.TopRight" />.</summary>
	[Parameter]
	public MokaToastPosition Position { get; set; } = MokaToastPosition.TopRight;

	/// <summary>Maximum number of toasts visible at once. Defaults to 5.</summary>
	[Parameter]
	public int MaxVisible { get; set; } = 5;

	private string HostCss => new CssBuilder("moka-toast-host")
		.AddClass($"moka-toast-host--{MokaEnumHelpers.ToCssClass(Position)}")
		.Build();

	private IEnumerable<MokaToastMessage> VisibleToasts =>
		_toasts.Count > MaxVisible ? _toasts.Skip(_toasts.Count - MaxVisible) : _toasts;

	/// <inheritdoc />
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		ToastService.OnToastAdded -= HandleToastAdded;
		ToastService.OnToastRemoved -= HandleToastRemoved;

		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		// Subscribe before seeding so nothing raised in between is missed; Add dedupes
		// by id so a toast caught both ways is only shown once.
		ToastService.OnToastAdded += HandleToastAdded;
		ToastService.OnToastRemoved += HandleToastRemoved;

		foreach (MokaToastMessage toast in ToastService.Toasts)
		{
			Add(toast);
		}
	}

	private void Add(MokaToastMessage toast)
	{
		if (!_toasts.Exists(t => t.Id == toast.Id))
		{
			_toasts.Add(toast);
		}
	}

	private void HandleToastAdded(MokaToastMessage toast) => _ = ApplyAsync(() => Add(toast));

	private void HandleToastRemoved(Guid id) => _ = ApplyAsync(() => _toasts.RemoveAll(t => t.Id == id));

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

	private void HandleClose(Guid id) => ToastService.Remove(id);

	private Task HandleAction(MokaToastMessage toast)
		=> InvokeCallbackAsync(toast.Options.OnActionAsync, toast.Options.OnAction);

	private Task HandleClick(MokaToastMessage toast)
		=> InvokeCallbackAsync(toast.Options.OnClickAsync, toast.Options.OnClick);

	// Consumer callbacks routinely call StateHasChanged on their own component, so they
	// are dispatched onto the renderer's synchronization context rather than invoked raw.
	private Task InvokeCallbackAsync(Func<Task>? asyncCallback, Action? syncCallback)
	{
		if (asyncCallback is not null)
		{
			return InvokeAsync(asyncCallback);
		}

		return syncCallback is not null ? InvokeAsync(syncCallback) : Task.CompletedTask;
	}

	/// <summary>Gets the severity icon for a toast, honouring any custom icon override.</summary>
	internal static MokaIconDefinition GetSeverityIcon(MokaToastMessage toast)
		=> toast.Options.CustomIcon ?? MokaFeedbackFormat.SeverityIcon(toast.Severity);
}
