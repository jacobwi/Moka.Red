namespace Moka.Red.Primitives.Chat;

/// <summary>
///     Represents a single message in a <see cref="MokaChat" /> component.
/// </summary>
public sealed class MokaChatMessage
{
	/// <summary>Unique identifier for the message.</summary>
	public required string Id { get; init; }

	/// <summary>Message text content.</summary>
	public required string Text { get; init; }

	/// <summary>Display name of the message author.</summary>
	public required string Author { get; init; }

	/// <summary>Optional avatar image URI for the author.</summary>
	public Uri? AvatarUri { get; init; }

	/// <summary>Optional initials fallback for the avatar.</summary>
	public string? AvatarInitials { get; init; }

	/// <summary>When the message was sent. Defaults to <see cref="DateTime.UtcNow" />.</summary>
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;

	/// <summary>Whether the message was sent by the current user.</summary>
	public bool IsOwn { get; init; }

	/// <summary>Whether this is a system message (e.g., "Chat started").</summary>
	public bool IsSystem { get; init; }

	/// <summary>Delivery status of the message.</summary>
	public MokaChatMessageStatus Status { get; init; } = MokaChatMessageStatus.Sent;
}

/// <summary>
///     Delivery status for a chat message.
/// </summary>
public enum MokaChatMessageStatus
{
	/// <summary>Message is being sent.</summary>
	Sending,

	/// <summary>Message has been sent to the server.</summary>
	Sent,

	/// <summary>Message has been delivered to the recipient.</summary>
	Delivered,

	/// <summary>Message has been read by the recipient.</summary>
	Read,

	/// <summary>Message failed to send.</summary>
	Error
}
