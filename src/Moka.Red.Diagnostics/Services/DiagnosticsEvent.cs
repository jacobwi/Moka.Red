namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Represents a single diagnostics event in the chronological event log.
/// </summary>
public sealed record DiagnosticsEvent
{
	/// <summary>
	///     UTC timestamp when the event occurred.
	/// </summary>
	public required DateTime Timestamp { get; init; }

	/// <summary>
	///     The type/category of the event.
	/// </summary>
	public required DiagnosticsEventType Type { get; init; }

	/// <summary>
	///     Human-readable description of the event.
	/// </summary>
	public required string Message { get; init; }

	/// <summary>
	///     Short type name of the component involved, if applicable.
	/// </summary>
	public string? ComponentType { get; init; }

	/// <summary>
	///     Unique instance identifier of the component involved, if applicable.
	/// </summary>
	public string? ComponentId { get; init; }
}

/// <summary>
///     Categorizes a diagnostics event for filtering and display.
/// </summary>
public enum DiagnosticsEventType
{
	/// <summary>A component completed a render cycle.</summary>
	Render,

	/// <summary>A component's ShouldRender returned false, skipping a render.</summary>
	RenderSkip,

	/// <summary>A component was disposed.</summary>
	Disposal,

	/// <summary>A JS interop call was made.</summary>
	JsInterop,

	/// <summary>A warning-level diagnostics event.</summary>
	Warning,

	/// <summary>An error-level diagnostics event.</summary>
	Error
}
