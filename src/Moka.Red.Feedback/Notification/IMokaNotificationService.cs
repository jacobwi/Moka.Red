using System.Diagnostics.CodeAnalysis;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.Feedback.Notification;

/// <summary>
///     Service for managing persistent notifications in the notification center.
///     Register as scoped and inject into components or other services.
/// </summary>
[SuppressMessage("Design", "CA1003:Use generic event handler instances",
	Justification = "Action delegates are simpler for lightweight service events.")]
public interface IMokaNotificationService
{
	/// <summary>
	///     Immutable snapshot of the current notifications, newest first.
	///     A snapshot handed out here never changes afterwards: marking a notification read
	///     replaces it, so the caller keeps the list exactly as it read it.
	/// </summary>
	IReadOnlyList<MokaNotification> Notifications { get; }

	/// <summary>Count of unread notifications.</summary>
	int UnreadCount { get; }

	/// <summary>Pushes a new notification.</summary>
	/// <param name="title">Notification title.</param>
	/// <param name="message">Notification message body.</param>
	/// <param name="severity">Severity level. Defaults to <see cref="MokaToastSeverity.Info" />.</param>
	void Push(string title, string message, MokaToastSeverity severity = MokaToastSeverity.Info);

	/// <summary>
	///     Pushes a pre-built notification. Use this overload to set
	///     <see cref="MokaNotification.Icon" /> or <see cref="MokaNotification.OnClick" />,
	///     which the title/message overload cannot reach. A notification whose
	///     <see cref="MokaNotification.Id" /> is already in the list replaces that one, in its place.
	/// </summary>
	/// <param name="notification">The notification to add.</param>
	void Push(MokaNotification notification);

	/// <summary>Marks a specific notification as read.</summary>
	void MarkAsRead(Guid id);

	/// <summary>Marks all notifications as read.</summary>
	void MarkAllAsRead();

	/// <summary>Removes a specific notification.</summary>
	void Remove(Guid id);

	/// <summary>Removes all notifications.</summary>
	void Clear();

	/// <summary>Raised when the notification list changes.</summary>
	event Action? OnChanged;
}
