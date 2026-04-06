using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Toast;
using Moka.Red.Icons;

namespace Moka.Red.Feedback.Notification;

/// <summary>
///     Persistent notification panel with a bell icon button, unread count badge,
///     and a dropdown panel listing all notifications.
/// </summary>
public partial class MokaNotificationCenter
{
	private bool _isOpen;

	[Inject] private IMokaNotificationService NotificationService { get; set; } = default!;

	/// <inheritdoc />
	protected override string RootClass => "moka-notification-center";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-notification-center--open", _isOpen)
		.AddClass(Class)
		.Build();

	/// <summary>Has internal open/close state.</summary>
	protected override bool ShouldRender() => true;

	protected override void OnInitialized() => NotificationService.OnChanged += HandleChanged;

	private void TogglePanel() => _isOpen = !_isOpen;

	private void HandleChanged() => InvokeAsync(StateHasChanged);

	private void HandleMarkAllRead() => NotificationService.MarkAllAsRead();

	private void HandleClear()
	{
		NotificationService.Clear();
		_isOpen = false;
	}

	private void HandleRemove(Guid id) => NotificationService.Remove(id);

	private void HandleItemClick(MokaNotification notification)
	{
		if (!notification.Read)
		{
			NotificationService.MarkAsRead(notification.Id);
		}

		notification.OnClick?.Invoke();
	}

	private static MokaIconDefinition GetIcon(MokaNotification notification)
	{
		if (notification.Icon is not null)
		{
			return notification.Icon.Value;
		}

		return notification.Severity switch
		{
			MokaToastSeverity.Success => MokaIcons.Status.CheckCircle,
			MokaToastSeverity.Warning => MokaIcons.Status.Warning,
			MokaToastSeverity.Error => MokaIcons.Status.Error,
			_ => MokaIcons.Status.Info
		};
	}

	private static string FormatTime(DateTime timestamp)
	{
		TimeSpan diff = DateTime.UtcNow - timestamp;
		if (diff.TotalMinutes < 1)
		{
			return "just now";
		}

		if (diff.TotalMinutes < 60)
		{
			return $"{(int)diff.TotalMinutes}m ago";
		}

		if (diff.TotalHours < 24)
		{
			return $"{(int)diff.TotalHours}h ago";
		}

		if (diff.TotalDays < 7)
		{
			return $"{(int)diff.TotalDays}d ago";
		}

		return timestamp.ToString("MMM d", CultureInfo.InvariantCulture);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		NotificationService.OnChanged -= HandleChanged;
		await base.DisposeAsyncCore();
	}
}
