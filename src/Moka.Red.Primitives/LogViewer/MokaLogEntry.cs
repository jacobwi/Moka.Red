namespace Moka.Red.Primitives.LogViewer;

/// <summary>
///     Represents a single log entry displayed in the <see cref="MokaLogViewer" />.
/// </summary>
/// <param name="Timestamp">When the log entry was created.</param>
/// <param name="Level">Severity level of the entry.</param>
/// <param name="Message">Log message text.</param>
/// <param name="Source">Optional source/category identifier (e.g., class name, module).</param>
/// <remarks>
///     <see cref="Exception" /> and <see cref="Properties" /> carry what a structured logger records beside the message:
///     <c>Microsoft.Extensions.Logging</c> passes an exception and the message's named values, OpenTelemetry a log
///     record's exception and attributes, Serilog an exception and properties. A row whose entry has either can be opened
///     to show them.
/// </remarks>
public sealed record MokaLogEntry(
	DateTime Timestamp,
	MokaLogLevel Level,
	string Message,
	string? Source = null)
{
	/// <summary>
	///     The exception logged with the entry, as text, usually <c>exception.ToString()</c> with its stack trace. Text
	///     rather than an <see cref="System.Exception" />, so an entry read back from a log store or sent to a browser
	///     carries it too.
	/// </summary>
	public string? Exception { get; init; }

	/// <summary>
	///     The entry's named values, such as a message template's arguments, an event id, a trace id or a scope's values,
	///     shown in the order given. Values are text: format them in the culture the reader expects.
	/// </summary>
	public IReadOnlyCollection<KeyValuePair<string, string>>? Properties { get; init; }

	/// <summary>Whether the entry has an <see cref="Exception" /> or <see cref="Properties" />, so its row can be opened.</summary>
	public bool HasDetail => !string.IsNullOrEmpty(Exception) || Properties is { Count: > 0 };
}
