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

	private async void HandleToastAdded(MokaToastMessage toast)
	{
		if (_disposed)
		{
			return;
		}

		try
		{
			Add(toast);
			await InvokeAsync(StateHasChanged);
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private async void HandleToastRemoved(Guid id)
	{
		if (_disposed)
		{
			return;
		}

		try
		{
			_toasts.RemoveAll(t => t.Id == id);
			await InvokeAsync(StateHasChanged);
		}
		catch (ObjectDisposedException)
		{
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
