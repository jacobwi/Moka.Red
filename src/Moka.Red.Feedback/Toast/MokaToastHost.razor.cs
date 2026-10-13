using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Internal;
using Moka.Red.Feedback.Tooltip;

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

	/// <summary>
	///     A CSS length to keep clear along the top of the window, such as <c>48px</c> for an app's own title bar.
	///     Applies to the top positions, on top of the host's padding. Written as <c>--moka-toast-offset-top</c>,
	///     which an app can also set from its own CSS on the host or any element around it. Null leaves it at 0.
	/// </summary>
	[Parameter]
	public string? OffsetTop { get; set; }

	/// <summary>
	///     A CSS length to keep clear along the right edge of the window. Applies to the right and centred positions,
	///     on top of the host's padding, as <c>--moka-toast-offset-right</c>. Null leaves it at 0.
	/// </summary>
	[Parameter]
	public string? OffsetRight { get; set; }

	/// <summary>
	///     A CSS length to keep clear along the bottom of the window, such as <c>24px</c> for a status bar. Applies to
	///     the bottom positions, on top of the host's padding, as <c>--moka-toast-offset-bottom</c>. Null leaves it
	///     at 0.
	/// </summary>
	[Parameter]
	public string? OffsetBottom { get; set; }

	/// <summary>
	///     A CSS length to keep clear along the left edge of the window, such as a side rail's width. Applies to the
	///     left and centred positions, on top of the host's padding, as <c>--moka-toast-offset-left</c>. Null leaves
	///     it at 0.
	/// </summary>
	[Parameter]
	public string? OffsetLeft { get; set; }

	/// <summary>Additional CSS classes on the host, after its own.</summary>
	[Parameter]
	public string? Class { get; set; }

	/// <summary>Inline styles on the host, after the offsets, so a variable set here wins over its parameter.</summary>
	[Parameter]
	public string? Style { get; set; }

	private string HostCss => new CssBuilder("moka-toast-host")
		.AddClass($"moka-toast-host--{MokaEnumHelpers.ToCssClass(Position)}")
		.AddClass(Class)
		.Build();

	// Only the offsets that were set are written, so one an app sets in its own CSS, on the host or around it, is not
	// overridden by an empty parameter. The stylesheet reads each with a fallback of 0.
	private string? HostStyle => new StyleBuilder()
		.AddStyle("--moka-toast-offset-top", OffsetTop, !string.IsNullOrWhiteSpace(OffsetTop))
		.AddStyle("--moka-toast-offset-right", OffsetRight, !string.IsNullOrWhiteSpace(OffsetRight))
		.AddStyle("--moka-toast-offset-bottom", OffsetBottom, !string.IsNullOrWhiteSpace(OffsetBottom))
		.AddStyle("--moka-toast-offset-left", OffsetLeft, !string.IsNullOrWhiteSpace(OffsetLeft))
		.AddStyle(Style)
		.Build();

	// The close button ends the toast's top row. Toasts along the bottom of the window open its name above.
	private MokaTooltipPosition CloseTooltipPosition =>
		Position is MokaToastPosition.BottomRight or MokaToastPosition.BottomLeft or MokaToastPosition.BottomCenter
			? MokaTooltipPosition.TopEnd
			: MokaTooltipPosition.BottomEnd;

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
