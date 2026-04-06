namespace Moka.Red.Feedback.Toast;

/// <summary>
///     Immutable record representing a single toast notification in the queue.
/// </summary>
public sealed record MokaToastMessage
{
	/// <summary>Unique identifier for this toast.</summary>
	public required Guid Id { get; init; }

	/// <summary>The message body displayed in the toast.</summary>
	public required string Message { get; init; }

	/// <summary>Severity level controlling color and icon.</summary>
	public required MokaToastSeverity Severity { get; init; }

	/// <summary>Configuration options for this toast.</summary>
	public required MokaToastOptions Options { get; init; }

	/// <summary>UTC timestamp when the toast was created.</summary>
	public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
