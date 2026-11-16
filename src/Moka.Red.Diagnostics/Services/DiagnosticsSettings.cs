namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     The settings one window's diagnostics run with: a circuit on Blazor Server, the app instance on WebAssembly or in
///     a Blazor Hybrid app. Each starts from the matching <see cref="DiagnosticsOptions" /> value, and a change applies to
///     this window only, until it reloads. <see cref="IMokaDiagnosticsService.Settings" /> holds the window's instance.
/// </summary>
/// <remarks>
///     What the app's logger captures (<see cref="DiagnosticsOptions.MinConsoleLogLevel" />) isn't here: the console
///     keeps one log for the whole app, so that setting is shared by every window.
/// </remarks>
public sealed class DiagnosticsSettings
{
	private bool _jsInteropTrackingEnabled;
	private int _maxEventLogEntries;
	private int _panelWidth;
	private OverlayPosition _position;
	private bool _renderTrackingEnabled;

	/// <summary>Starts every setting at the value <paramref name="options" /> gives it.</summary>
	/// <param name="options">The app's diagnostics options.</param>
	public DiagnosticsSettings(DiagnosticsOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		_position = options.Position;
		_panelWidth = options.PanelWidth;
		_maxEventLogEntries = options.MaxEventLogEntries;
		_renderTrackingEnabled = options.RenderTrackingEnabled;
		_jsInteropTrackingEnabled = options.JsInteropTrackingEnabled;
	}

	/// <summary>Screen corner where this window's overlay badge and panel appear.</summary>
	public OverlayPosition Position
	{
		get => _position;
		set => Set(ref _position, value);
	}

	/// <summary>Width of this window's diagnostics panel in pixels.</summary>
	public int PanelWidth
	{
		get => _panelWidth;
		set => Set(ref _panelWidth, value);
	}

	/// <summary>Maximum number of events this window's event log retains. A lower limit drops the oldest at once.</summary>
	public int MaxEventLogEntries
	{
		get => _maxEventLogEntries;
		set => Set(ref _maxEventLogEntries, value);
	}

	/// <summary>Whether this window records renders and skipped renders.</summary>
	public bool RenderTrackingEnabled
	{
		get => _renderTrackingEnabled;
		set => Set(ref _renderTrackingEnabled, value);
	}

	/// <summary>Whether this window records JS interop calls.</summary>
	public bool JsInteropTrackingEnabled
	{
		get => _jsInteropTrackingEnabled;
		set => Set(ref _jsInteropTrackingEnabled, value);
	}

	/// <summary>
	///     Raised when a setting changes. The popup page changes its opener's settings from its own circuit, so this can
	///     come on another circuit's thread.
	/// </summary>
	public event EventHandler? Changed;

	private void Set<T>(ref T field, T value)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return;
		}

		field = value;
		Changed?.Invoke(this, EventArgs.Empty);
	}
}
