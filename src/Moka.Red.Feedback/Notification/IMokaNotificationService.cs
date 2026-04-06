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
	/// <summary>All current notifications, newest first.</summary>
	IReadOnlyList<MokaNotification> Notifications { get; }

	/// <summary>Count of unread notifications.</summary>
	int UnreadCount { get; }

	/// <summary>Pushes a new notification.</summary>
	/// <param name="title">Notification title.</param>
	/// <param name="message">Notification message body.</param>
	/// <param name="severity">Severity level. Defaults to <see cref="MokaToastSeverity.Info" />.</param>
	void Push(string title, string message, MokaToastSeverity severity = MokaToastSeverity.Info);

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
