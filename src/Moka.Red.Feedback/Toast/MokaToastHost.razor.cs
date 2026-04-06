using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Icons;

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
		ToastService.OnToastAdded += HandleToastAdded;
		ToastService.OnToastRemoved += HandleToastRemoved;
	}

	private async void HandleToastAdded(MokaToastMessage toast)
	{
		if (_disposed)
		{
			return;
		}

		try
		{
			_toasts.Add(toast);
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

	private static void HandleAction(MokaToastMessage toast) => toast.Options.OnAction?.Invoke();

	private static void HandleClick(MokaToastMessage toast) => toast.Options.OnClick?.Invoke();

	/// <summary>Gets the severity icon for a toast.</summary>
	internal static MokaIconDefinition GetSeverityIcon(MokaToastMessage toast)
	{
		if (toast.Options.CustomIcon is not null)
		{
			return toast.Options.CustomIcon.Value;
		}

		return toast.Severity switch
		{
			MokaToastSeverity.Success => MokaIcons.Status.CheckCircle,
			MokaToastSeverity.Warning => MokaIcons.Status.Warning,
			MokaToastSeverity.Error => MokaIcons.Status.Error,
			_ => MokaIcons.Status.Info
		};
	}
}
