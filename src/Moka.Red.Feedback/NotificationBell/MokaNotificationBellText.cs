using System.Globalization;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.Internal;
using Moka.Red.Feedback.Notification;

namespace Moka.Red.Feedback.NotificationBell;

/// <summary>
///     Every word a <see cref="MokaNotificationBell" /> writes around the notifications. Set the ones to change, or all of
///     them to translate it. Anything holding a count or a time is a function, so the words can go around it.
/// </summary>
public sealed record MokaNotificationBellText
{
	/// <summary>The panel's heading.</summary>
	public string Title { get; init; } = "Notifications";

	/// <summary>The header button that marks every notification read.</summary>
	public string MarkAllRead { get; init; } = "Mark all read";

	/// <summary>The header button that takes every notification away.</summary>
	public string Clear { get; init; } = "Clear";

	/// <summary>Shown in an empty panel.</summary>
	public string Empty { get; init; } = "No notifications";

	/// <summary>What a screen reader hears after an unread notification.</summary>
	public string Unread { get; init; } = "Unread";

	/// <summary>
	///     The heading over the notifications from today, under <see cref="MokaNotificationLook.Grouped" />. Today is the
	///     local date of the registered <see cref="TimeProvider" />, else the system clock's.
	/// </summary>
	public string Today { get; init; } = "Today";

	/// <summary>The heading over the older notifications, under <see cref="MokaNotificationLook.Grouped" />.</summary>
	public string Earlier { get; init; } = "Earlier";

	/// <summary>
	///     The header's count of unread notifications, in the looks that show one (<see cref="MokaNotificationLook.Inbox" />,
	///     <see cref="MokaNotificationLook.Grouped" /> and <see cref="MokaNotificationLook.Terminal" />), such as "2 new".
	/// </summary>
	public Func<int, string> NewCount { get; init; } = MokaNotificationLooks.DefaultNewCount;

	/// <summary>Names the bell, in its tooltip and for screen readers, from the unread count.</summary>
	public Func<int, string> Bell { get; init; } = unread => unread > 0
		? string.Create(CultureInfo.CurrentCulture, $"Notifications, {(unread > 99 ? "99+" : unread.ToString(CultureInfo.CurrentCulture))} unread")
		: "Notifications";

	/// <summary>The line under the list saying how many notifications didn't fit, such as "+3 more".</summary>
	public Func<int, string> More { get; init; } = count => string.Create(CultureInfo.CurrentCulture, $"+{count} more");

	/// <summary>
	///     A notification's age, from its UTC timestamp and the clock's now in UTC, such as "5m ago".
	/// </summary>
	public Func<DateTime, DateTime, string> Time { get; init; } = MokaFeedbackFormat.RelativeTime;

	/// <summary>
	///     The tag in front of an unread notification under <see cref="MokaNotificationLook.Terminal" />. Keep it as wide
	///     as <see cref="ReadTag" />, so the titles line up.
	/// </summary>
	public string UnreadTag { get; init; } = "[NEW]";

	/// <summary>The tag in front of a read notification under <see cref="MokaNotificationLook.Terminal" />.</summary>
	public string ReadTag { get; init; } = "[---]";
}
