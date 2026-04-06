using Microsoft.AspNetCore.Components;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel displaying JS interop call stats, component lifecycle counts,
///     and potential memory leak indicators. Auto-refreshes every second.
/// </summary>
public sealed partial class PerformancePanel : ComponentBase, IDisposable
{
	private int _activeCount;
	private bool _disposed;
	private int _disposedCount;
	private int _growthStreak;
	private IReadOnlyList<JsInteropEntry> _jsEntries = [];
	private int _previousActiveCount;
	private Timer? _refreshTimer;

	[Inject] private IMokaDiagnosticsService? _diagnosticsService { get; set; }

	private string LeakIndicatorClass =>
		_growthStreak >= 5 ? "moka-diag-perf-val--leak" :
		_growthStreak >= 3 ? "moka-diag-perf-val--warn" :
		"";

	private string LeakIndicatorText =>
		_growthStreak >= 5 ? "Growing (possible leak)" :
		_growthStreak >= 3 ? "Growing" :
		"Stable";

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_refreshTimer?.Dispose();
	}

	protected override void OnInitialized()
	{
		RefreshData();
		_refreshTimer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
	}

	private void OnTimerTick(object? state)
	{
		if (_disposed)
		{
			return;
		}

		try
		{
			InvokeAsync(() =>
			{
				if (_disposed)
				{
					return;
				}

				RefreshData();
				StateHasChanged();
			});
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private void RefreshData()
	{
		if (_diagnosticsService is null)
		{
			return;
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
