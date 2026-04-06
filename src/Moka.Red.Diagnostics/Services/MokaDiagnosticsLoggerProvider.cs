using Microsoft.Extensions.Logging;

namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Logger provider that captures log messages for the diagnostics console.
///     Registered as a singleton and forwards messages to <see cref="MokaDiagnosticsConsoleBuffer" />.
/// </summary>
public sealed class MokaDiagnosticsLoggerProvider : ILoggerProvider
{
	private readonly MokaDiagnosticsConsoleBuffer _buffer;

	/// <summary>
	///     Initializes a new instance of <see cref="MokaDiagnosticsLoggerProvider" />.
	/// </summary>
	public MokaDiagnosticsLoggerProvider(MokaDiagnosticsConsoleBuffer buffer)
	{
		_buffer = buffer;
	}

	/// <inheritdoc />
	public ILogger CreateLogger(string categoryName) => new MokaDiagnosticsLogger(categoryName, _buffer);

	/// <inheritdoc />
	public void Dispose()
	{
	}
}
