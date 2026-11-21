using System.Globalization;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.Internal;

namespace Moka.Red.Feedback.Notification;

/// <summary>
///     Every word a <see cref="MokaNotificationCenter" /> writes around the notifications. Set the ones to change, or all
///     of them to translate it. Anything holding a name, a count or a time is a function, so the words can go around it.
/// </summary>
public sealed record MokaNotificationCenterText
{
	/// <summary>The panel's heading.</summary>
	public string Title { get; init; } = "Notifications";

	/// <summary>The header button that marks every notification read.</summary>
	public string MarkAllRead { get; init; } = "Mark all read";

	/// <summary>The header button that takes every notification away.</summary>
	public string ClearAll { get; init; } = "Clear all";

	/// <summary>Shown in an empty panel.</summary>
	public string Empty { get; init; } = "No notifications";

	/// <summary>What a screen reader hears after an unread notification.</summary>
	public string Unread { get; init; } = "Unread";

	/// <summary>
	///     What a screen reader hears after a notification whose link opens another tab (<see cref="MokaNotification.Target" />),
	///     after a comma.
	/// </summary>
	public string NewTab { get; init; } = "opens in a new tab";

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

	/// <summary>Names a notification's dismiss button, from its title.</summary>
	public Func<string, string> Dismiss { get; init; } = title => $"Dismiss {title}";

	/// <summary>
	///     A notification's age, from its UTC timestamp and the clock's now in UTC, such as "5m ago".
	/// </summary>
	public Func<DateTime, DateTime, string> Time { get; init; } = MokaFeedbackFormat.RelativeTime;
}
