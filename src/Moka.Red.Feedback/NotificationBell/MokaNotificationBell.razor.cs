using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Popover;

namespace Moka.Red.Feedback.NotificationBell;

/// <summary>
///     Lightweight bell icon trigger with unread badge count and a dropdown notification list.
///     Unlike <see cref="Notification.MokaNotificationCenter" />, this is a standalone trigger
///     that works with a simple item list rather than a service.
/// </summary>
public partial class MokaNotificationBell : MokaVisualComponentBase
{
	private bool _isOpen;

	/// <summary>Notifications to display in the dropdown.</summary>
	[Parameter]
	public IReadOnlyList<MokaNotificationBellItem>? Notifications { get; set; }

	/// <summary>Number of unread notifications to show on the badge. 0 hides the badge.</summary>
	[Parameter]
	public int UnreadCount { get; set; }

	/// <summary>Raised when a notification item is clicked.</summary>
	[Parameter]
	public EventCallback<MokaNotificationBellItem> OnNotificationClick { get; set; }

	/// <summary>Raised when the "Mark all read" action is clicked.</summary>
	[Parameter]
	public EventCallback OnMarkAllRead { get; set; }

	/// <summary>Raised when the "Clear" action is clicked.</summary>
	[Parameter]
	public EventCallback OnClear { get; set; }

	/// <summary>Maximum number of notifications visible in the dropdown before scrolling.</summary>
	[Parameter]
	public int MaxVisible { get; set; } = 5;

	/// <summary>Position of the dropdown relative to the bell icon.</summary>
	[Parameter]
	public MokaPopoverPosition Position { get; set; } = MokaPopoverPosition.BottomEnd;

	/// <inheritdoc />
	protected override string RootClass => "moka-notification-bell";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-notification-bell--open", _isOpen)
		.AddClass($"moka-notification-bell--{MokaEnumHelpers.ToCssClass(Size)}")
		.AddClass(Class)
		.Build();

	private IReadOnlyList<MokaNotificationBellItem> VisibleNotifications =>
		Notifications is null
			? []
			: Notifications.Count <= MaxVisible
				? Notifications
				: Notifications.Take(MaxVisible).ToList();

	private int RemainingCount =>
		Notifications is null ? 0 : Math.Max(0, Notifications.Count - MaxVisible);

	/// <summary>Has internal open/close state.</summary>
	protected override bool ShouldRender() => true;

	private void ToggleDropdown() => _isOpen = !_isOpen;

	private async Task HandleItemClick(MokaNotificationBellItem item) => await OnNotificationClick.InvokeAsync(item);

	private async Task HandleMarkAllRead() => await OnMarkAllRead.InvokeAsync();

	private async Task HandleClear()
	{
		await OnClear.InvokeAsync();
		_isOpen = false;
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
}
