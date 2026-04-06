using Microsoft.Extensions.Logging;

namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Logger that forwards log messages to the <see cref="MokaDiagnosticsConsoleBuffer" />.
/// </summary>
public sealed class MokaDiagnosticsLogger : ILogger
{
	private readonly MokaDiagnosticsConsoleBuffer _buffer;
	private readonly string _categoryName;

	/// <summary>
	///     Initializes a new instance of <see cref="MokaDiagnosticsLogger" />.
	/// </summary>
	public MokaDiagnosticsLogger(string categoryName, MokaDiagnosticsConsoleBuffer buffer)
	{
		_categoryName = categoryName;
		_buffer = buffer;
	}

	/// <inheritdoc />
	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

	/// <inheritdoc />
	public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

	/// <inheritdoc />
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
		ArgumentNullException.ThrowIfNull(formatter);

		if (!IsEnabled(logLevel))
		{
			return;
		}

		string message = formatter(state, exception);
		_buffer.Add(new ConsoleLogEntry
		{
			Timestamp = DateTime.UtcNow,
			Level = logLevel,
			Category = ShortenCategory(_categoryName),
			Message = message,
			Exception = exception?.ToString()
		});
	}

	private static string ShortenCategory(string category)
	{
		// "Microsoft.AspNetCore.Components.Rendering.Renderer" -> "Renderer"
		int lastDot = category.LastIndexOf('.');
		return lastDot >= 0 ? category[(lastDot + 1)..] : category;
	}
}
