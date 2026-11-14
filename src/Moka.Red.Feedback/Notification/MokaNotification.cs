using Moka.Red.Core.Icons;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.Feedback.Notification;

/// <summary>
///     Model representing a single notification in the notification center.
/// </summary>
public sealed class MokaNotification
{
	/// <summary>Unique identifier.</summary>
	public Guid Id { get; init; } = Guid.NewGuid();

	/// <summary>
	///     Names what the notification is about, so a later one with the same key takes its place rather than standing
	///     beside it: a build's "Running" replaced by its result. The replacement is new, so it's unread and at the top.
	/// </summary>
	public string? Key { get; init; }

	/// <summary>Notification title.</summary>
	public required string Title { get; init; }

	/// <summary>Notification message body.</summary>
	public required string Message { get; init; }

	/// <summary>Severity controlling color and default icon.</summary>
	public MokaToastSeverity Severity { get; init; } = MokaToastSeverity.Info;

	/// <summary>UTC timestamp when the notification was created.</summary>
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;

	/// <summary>
	///     Whether the notification has been read. Set at construction only: the service
	///     marks a notification read by replacing it, so snapshots already handed out to
	///     callers never change under them.
	/// </summary>
	public bool Read { get; init; }

	/// <summary>Custom icon override. When null, the severity-mapped icon is used.</summary>
	public MokaIconDefinition? Icon { get; init; }

	/// <summary>Optional click action.</summary>
	public Action? OnClick { get; init; }

	/// <summary>
	///     Where the notification leads. The notification becomes a link there; a <c>javascript:</c>, <c>vbscript:</c>
	///     or <c>data:</c> address is dropped. <see cref="OnClick" /> still runs when it's followed.
	/// </summary>
	public string? Href { get; init; }

	/// <summary>Buttons under the notification, such as "Retry" or "View log".</summary>
	public IReadOnlyList<MokaNotificationAction> Actions { get; init; } = [];

	internal MokaNotification AsRead() => new()
	{
		Id = Id,
		Key = Key,
		Title = Title,
		Message = Message,
		Severity = Severity,
		Timestamp = Timestamp,
		Icon = Icon,
		OnClick = OnClick,
		Href = Href,
		Actions = Actions,
		Read = true
	};
}
