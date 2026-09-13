using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Internal;

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

	private IReadOnlyList<MokaNotification> Notifications => NotificationService.Notifications;

	private int UnreadCount => NotificationService.UnreadCount;

	private string BadgeText => UnreadCount > 99 ? "99+" : UnreadCount.ToString(CultureInfo.InvariantCulture);

	/// <summary>Has internal open/close state.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
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
		=> notification.Icon ?? MokaFeedbackFormat.SeverityIcon(notification.Severity);

	private static string FormatTime(DateTime timestamp) => MokaFeedbackFormat.RelativeTime(timestamp);

	private static string ItemCss(MokaNotification notification) => new CssBuilder("moka-notification-item")
		.AddClass("moka-notification-item--unread", !notification.Read)
		.AddClass($"moka-notification-item--{MokaEnumHelpers.ToCssClass(notification.Severity)}")
		.Build();

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		NotificationService.OnChanged -= HandleChanged;
		await base.DisposeAsyncCore();
	}
}
