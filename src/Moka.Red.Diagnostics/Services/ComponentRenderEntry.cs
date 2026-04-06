namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Tracks render statistics for a single component instance.
/// </summary>
public sealed class ComponentRenderEntry
{
	/// <summary>
	///     Short type name of the component (e.g., "MokaButton").
	/// </summary>
	public required string ComponentType { get; init; }

	/// <summary>
	///     Unique identifier for this component instance.
	/// </summary>
	public required string ComponentId { get; init; }

	/// <summary>
	///     Total number of times the component has rendered.
	/// </summary>
	public int RenderCount { get; set; }

	/// <summary>
	///     Number of times <c>ShouldRender</c> returned <c>false</c>, skipping a render.
	/// </summary>
	public int ShouldRenderSkipCount { get; set; }

	/// <summary>
	///     Timestamp of the most recent render.
	/// </summary>
	public DateTime LastRenderTime { get; set; }

	/// <summary>
	///     Duration of the most recent render cycle.
	/// </summary>
	public TimeSpan LastRenderDuration { get; set; }

	/// <summary>
	///     Whether this component has been disposed.
	/// </summary>
	public bool IsDisposed { get; set; }
}
