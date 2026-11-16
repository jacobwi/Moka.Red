using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel displaying JS interop call stats, component lifecycle counts,
///     and potential memory leak indicators. Checks every second and renders only when a figure changed.
/// </summary>
public sealed partial class PerformancePanel : ComponentBase, IDisposable
{
	private int _activeCount;
	private bool _disposed;
	private int _disposedCount;
	private int _growthStreak;
	private IReadOnlyList<JsInteropEntry> _jsEntries = [];
	private int _previousActiveCount;
	private (int Active, int Disposed, string Trend, int Functions, int Calls, TimeSpan Time) _shown;
	private PanelRefresh? _refresh;

	private IMokaDiagnosticsService? _diagnosticsService;

	[Inject] private IServiceProvider Services { get; set; } = default!;

	[CascadingParameter(Name = DiagnosticsServiceResolver.CascadeName)]
	private IMokaDiagnosticsService? SharedService { get; set; }

	private string LeakIndicatorClass =>
		_growthStreak >= 5 ? "moka-diag-perf-val--leak" :
		_growthStreak >= 3 ? "moka-diag-perf-val--warn" :
		"";

	private string LeakIndicatorText =>
		_growthStreak >= 5 ? "Growing (possible leak)" :
		_growthStreak >= 3 ? "Growing" :
		"Stable";

	/// <summary>Stops the panel's refresh timer.</summary>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_refresh?.Dispose();
	}

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		_diagnosticsService = DiagnosticsServiceResolver.Resolve(SharedService, Services);
		RefreshData();
		_refresh = new PanelRefresh(Services.GetService<TimeProvider>() ?? TimeProvider.System, TimeSpan.FromSeconds(1),
			InvokeAsync, DispatchExceptionAsync, RefreshData, StateHasChanged);
	}

	private bool RefreshData()
	{
		if (_diagnosticsService is null)
		{
			return false;
		}

		_jsEntries = _diagnosticsService.GetJsInteropEntries();
		_activeCount = _diagnosticsService.ActiveComponentCount;
		_disposedCount = _diagnosticsService.DisposedComponentCount;

		// Track growth trend for leak detection
		if (_activeCount > _previousActiveCount)
		{
			_growthStreak++;
		}
		else if (_activeCount <= _previousActiveCount)
		{
			_growthStreak = 0;
		}

		_previousActiveCount = _activeCount;

		// The entries' totals and longest calls only grow, so their sum moves whenever an average or a maximum shown does.
		var shown = (_activeCount, _disposedCount, LeakIndicatorText, _jsEntries.Count,
			_jsEntries.Sum(entry => entry.CallCount),
			_jsEntries.Aggregate(TimeSpan.Zero, (sum, entry) => sum + entry.TotalDuration + entry.MaxDuration));
		bool changed = shown != _shown;
		_shown = shown;
		return changed;
	}

	private void Clear()
	{
		_diagnosticsService?.ClearPerformanceData();
		_growthStreak = 0;
		_previousActiveCount = 0;
		RefreshData();
	}

	private static string ShortenIdentifier(string identifier)
	{
		// Show last segment of dotted identifiers (e.g., "navigator.clipboard.writeText" -> "writeText")
		int lastDot = identifier.LastIndexOf('.');
		return lastDot >= 0 && lastDot < identifier.Length - 1
			? identifier[(lastDot + 1)..]
			: identifier;
	}
}
