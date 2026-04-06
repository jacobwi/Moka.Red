using System.Collections.Concurrent;

namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Singleton buffer for console log messages. Shared across all circuits.
///     Thread-safe via <see cref="ConcurrentQueue{T}" />.
/// </summary>
public sealed class MokaDiagnosticsConsoleBuffer
{
	private const int MaxMessages = 500;
	private readonly ConcurrentQueue<ConsoleLogEntry> _messages = new();

	/// <summary>
	///     Raised when a new message is added to the buffer.
	/// </summary>
	public event EventHandler? OnMessageAdded;

	/// <summary>
	///     Adds a log entry to the buffer, trimming oldest entries when capacity is exceeded.
	/// </summary>
	public void Add(ConsoleLogEntry entry)
	{
		_messages.Enqueue(entry);
		while (_messages.Count > MaxMessages)
		{
			_messages.TryDequeue(out _);
		}

		OnMessageAdded?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	///     Returns the most recent messages, newest first.
	/// </summary>
	public IReadOnlyList<ConsoleLogEntry> GetMessages(int maxCount = 200) =>
		_messages.Reverse().Take(maxCount).ToList();

	/// <summary>
	///     Clears all messages from the buffer.
	/// </summary>
	public void Clear()
	{
		while (_messages.TryDequeue(out _))
		{
		}
	}
}
