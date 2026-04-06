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

	/// <summary>Notification title.</summary>
	public required string Title { get; init; }

	/// <summary>Notification message body.</summary>
	public required string Message { get; init; }

	/// <summary>Severity controlling color and default icon.</summary>
	public MokaToastSeverity Severity { get; init; } = MokaToastSeverity.Info;

	/// <summary>UTC timestamp when the notification was created.</summary>
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;

	/// <summary>Whether the notification has been read.</summary>
	public bool Read { get; set; }

	/// <summary>Custom icon override. When null, the severity-mapped icon is used.</summary>
	public MokaIconDefinition? Icon { get; init; }

	/// <summary>Optional click action.</summary>
	public Action? OnClick { get; init; }
}
