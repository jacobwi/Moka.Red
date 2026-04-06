using Moka.Red.Core.Theming;

namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Service providing diagnostics data for the Moka.Red overlay.
/// </summary>
public interface IMokaDiagnosticsService
{
	/// <summary>
	///     Gets the current diagnostics configuration.
	/// </summary>
	DiagnosticsOptions Options { get; }

	/// <summary>
	///     Gets or sets whether the overlay panel is visible.
	/// </summary>
	bool IsOverlayVisible { get; set; }

	/// <summary>
	///     Number of component instances that have been registered but not yet disposed.
	/// </summary>
	int ActiveComponentCount { get; }

	/// <summary>
	///     Total number of component disposals recorded.
	/// </summary>
	int DisposedComponentCount { get; }

	// ── Pause / Resume ────────────────────────────────────────────

	/// <summary>
	///     Gets or sets whether diagnostics recording is paused.
	///     When <c>true</c>, <see cref="RecordRender" />, <see cref="RecordShouldRenderSkip" />,
	///     <see cref="RecordJsInteropCall" />, and <see cref="RecordDisposal" /> are no-ops.
	/// </summary>
	bool IsPaused { get; set; }

	/// <summary>
	///     Raised when <see cref="IsOverlayVisible" /> changes.
	/// </summary>
	event EventHandler? OnOverlayVisibilityChanged;

	/// <summary>
	///     Extracts all theme tokens from the given theme, grouped by category.
	/// </summary>
	IReadOnlyList<ThemeTokenGroup> GetThemeTokens(MokaTheme theme);

	// ── Render Tracking ────────────────────────────────────────────

	/// <summary>
	///     Records a completed render for a component instance.
	/// </summary>
	/// <param name="componentType">Short type name of the component.</param>
	/// <param name="componentId">Unique instance identifier.</param>
	/// <param name="duration">Time spent rendering.</param>
	void RecordRender(string componentType, string componentId, TimeSpan duration);

	/// <summary>
	///     Records that <c>ShouldRender</c> returned <c>false</c> for a component instance.
	/// </summary>
	/// <param name="componentType">Short type name of the component.</param>
	/// <param name="componentId">Unique instance identifier.</param>
	void RecordShouldRenderSkip(string componentType, string componentId);

	/// <summary>
	///     Returns all tracked component render entries sorted by render count descending.
	/// </summary>
	IReadOnlyList<ComponentRenderEntry> GetRenderEntries();

	/// <summary>
	///     Clears all render tracking data.
	/// </summary>
	void ClearRenderData();

	/// <summary>
	///     Raised when render tracking data changes.
	/// </summary>
	event EventHandler? OnRenderDataChanged;

	// ── JS Interop Tracking ────────────────────────────────────────

	/// <summary>
	///     Records a completed JS interop call.
	/// </summary>
	/// <param name="identifier">The JS function identifier.</param>
	/// <param name="duration">Time spent in the call.</param>
	void RecordJsInteropCall(string identifier, TimeSpan duration);

	/// <summary>
	///     Returns all tracked JS interop entries sorted by call count descending.
	/// </summary>
	IReadOnlyList<JsInteropEntry> GetJsInteropEntries();

	// ── Disposal Tracking ──────────────────────────────────────────

	/// <summary>
	///     Records that a component instance has been disposed.
	/// </summary>
	/// <param name="componentType">Short type name of the component.</param>
	/// <param name="componentId">Unique instance identifier.</param>
	void RecordDisposal(string componentType, string componentId);

	/// <summary>
	///     Clears all performance tracking data (JS interop and disposal).
	/// </summary>
	void ClearPerformanceData();

	/// <summary>
	///     Raised when performance tracking data changes.
	/// </summary>
	event EventHandler? OnPerformanceDataChanged;

	// ── Event Log ─────────────────────────────────────────────────

	/// <summary>
	///     Returns the most recent diagnostics events, newest first.
	/// </summary>
	/// <param name="maxCount">Maximum number of events to return.</param>
	IReadOnlyList<DiagnosticsEvent> GetRecentEvents(int maxCount = 100);

	/// <summary>
	///     Clears all recorded events from the event log.
	/// </summary>
	void ClearEvents();

	/// <summary>
	///     Raised when a new event is logged.
	/// </summary>
	event EventHandler? OnEventLogged;

	/// <summary>
	///     Raised when <see cref="IsPaused" /> changes.
	/// </summary>
	event EventHandler? OnPauseStateChanged;
}
