using Microsoft.Extensions.Logging;

namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Logger provider that captures log messages for the diagnostics console.
///     Registered as a singleton and forwards messages to <see cref="MokaDiagnosticsConsoleBuffer" />.
/// </summary>
public sealed class MokaDiagnosticsLoggerProvider : ILoggerProvider
{
	private readonly MokaDiagnosticsConsoleBuffer _buffer;
	private readonly DiagnosticsOptions _options;

	/// <summary>
	///     Initializes a new instance of <see cref="MokaDiagnosticsLoggerProvider" /> whose loggers keep
	///     Debug and above.
	/// </summary>
	public MokaDiagnosticsLoggerProvider(MokaDiagnosticsConsoleBuffer buffer)
		: this(buffer, new DiagnosticsOptions())
	{
	}

	/// <summary>
	///     Initializes a new instance of <see cref="MokaDiagnosticsLoggerProvider" /> whose loggers follow
	///     <see cref="DiagnosticsOptions.MinConsoleLogLevel" />, including changes made while the app runs.
	/// </summary>
	public MokaDiagnosticsLoggerProvider(MokaDiagnosticsConsoleBuffer buffer, DiagnosticsOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		_buffer = buffer;
		_options = options;
	}

	/// <inheritdoc />
	public ILogger CreateLogger(string categoryName) => new MokaDiagnosticsLogger(categoryName, _buffer, _options);

	/// <inheritdoc />
	public void Dispose()
	{
	}
}
