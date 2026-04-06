namespace Moka.Red.Primitives.LogViewer;

/// <summary>
///     Severity level for log entries displayed in the <see cref="MokaLogViewer" />.
/// </summary>
public enum MokaLogLevel
{
	/// <summary>Verbose tracing information.</summary>
	Trace,

	/// <summary>Debug-level diagnostic information.</summary>
	Debug,

	/// <summary>General informational messages.</summary>
	Info,

	/// <summary>Warning conditions that may require attention.</summary>
	Warning,

	/// <summary>Error conditions.</summary>
	Error,

	/// <summary>Critical/fatal errors.</summary>
	Fatal
}
