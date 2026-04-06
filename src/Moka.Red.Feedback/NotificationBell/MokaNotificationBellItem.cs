using Moka.Red.Core.Icons;

namespace Moka.Red.Feedback.NotificationBell;

/// <summary>
///     Represents a single notification item displayed in the <see cref="MokaNotificationBell" /> dropdown.
/// </summary>
/// <param name="Id">Unique identifier for the notification.</param>
/// <param name="Title">Short title text.</param>
/// <param name="Message">Longer message body (may be truncated in display).</param>
/// <param name="Timestamp">When the notification was created.</param>
/// <param name="Read">Whether the notification has been read.</param>
/// <param name="Icon">Optional icon override. Falls back to the bell icon when null.</param>
public sealed record MokaNotificationBellItem(
	Guid Id,
	string Title,
	string Message,
	DateTime Timestamp,
	bool Read = false,
	MokaIconDefinition? Icon = null);
