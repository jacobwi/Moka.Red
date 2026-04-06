namespace Moka.Red.Primitives.LogViewer;

/// <summary>
///     Represents a single log entry displayed in the <see cref="MokaLogViewer" />.
/// </summary>
/// <param name="Timestamp">When the log entry was created.</param>
/// <param name="Level">Severity level of the entry.</param>
/// <param name="Message">Log message text.</param>
/// <param name="Source">Optional source/category identifier (e.g., class name, module).</param>
public sealed record MokaLogEntry(
	DateTime Timestamp,
	MokaLogLevel Level,
	string Message,
	string? Source = null);
