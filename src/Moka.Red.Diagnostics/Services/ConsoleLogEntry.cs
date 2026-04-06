using Microsoft.Extensions.Logging;

namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Represents a single log message captured by the diagnostics console.
/// </summary>
public sealed record ConsoleLogEntry
{
	/// <summary>
	///     UTC timestamp when the message was logged.
	/// </summary>
	public required DateTime Timestamp { get; init; }

	/// <summary>
	///     The log level of the message.
	/// </summary>
	public required LogLevel Level { get; init; }

	/// <summary>
	///     Shortened category name (typically the class name).
	/// </summary>
	public required string Category { get; init; }

	/// <summary>
	///     The formatted log message.
	/// </summary>
	public required string Message { get; init; }

	/// <summary>
	///     Full exception text, if an exception was associated with the log entry.
	/// </summary>
	public string? Exception { get; init; }
}
