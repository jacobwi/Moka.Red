namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Tracks call statistics for a single JS interop function.
/// </summary>
public sealed class JsInteropEntry
{
	/// <summary>
	///     The JS function identifier (e.g., "eval", "import").
	/// </summary>
	public required string Identifier { get; init; }

	/// <summary>
	///     Total number of calls to this function.
	/// </summary>
	public int CallCount { get; set; }

	/// <summary>
	///     Sum of all call durations.
	/// </summary>
	public TimeSpan TotalDuration { get; set; }

	/// <summary>
	///     Average call duration.
	/// </summary>
	public TimeSpan AverageDuration => CallCount > 0 ? TotalDuration / CallCount : TimeSpan.Zero;

	/// <summary>
	///     Maximum single-call duration observed.
	/// </summary>
	public TimeSpan MaxDuration { get; set; }
}
