using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel displaying the component lifecycle: active and disposed counts, the average render time, and a trend that
///     points at a possible memory leak. JS interop calls are in <see cref="NetworkPanel" /> and GC figures in
///     <see cref="MemoryPanel" />. Checks every second and renders only when a figure changed.
/// </summary>
public sealed partial class PerformancePanel : ComponentBase, IDisposable
{
	private int _activeCount;
	private double _avgRenderMs;
	private bool _disposed;
	private int _disposedCount;
	private int _growthStreak;
	private int _previousActiveCount;
	private (int Active, int Disposed, string Trend, double AvgRender) _shown;
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

		_activeCount = _diagnosticsService.ActiveComponentCount;
		_disposedCount = _diagnosticsService.DisposedComponentCount;
		_avgRenderMs = AverageRenderMs(_diagnosticsService.GetRenderEntries());

		// A streak of rising active counts points at components that are made and never disposed.
		if (_activeCount > _previousActiveCount)
		{
			_growthStreak++;
		}
		else
		{
			_growthStreak = 0;
		}

		_previousActiveCount = _activeCount;

		var shown = (_activeCount, _disposedCount, LeakIndicatorText, Math.Round(_avgRenderMs, 1));
		bool changed = shown != _shown;
		_shown = shown;
		return changed;
	}

	// The mean of each rendered component's latest render.
	private static double AverageRenderMs(IReadOnlyList<ComponentRenderEntry> entries)
	{
		double totalMs = 0;
		int rendered = 0;
		foreach (ComponentRenderEntry entry in entries)
		{
			if (entry.RenderCount > 0)
			{
				totalMs += entry.LastRenderDuration.TotalMilliseconds;
				rendered++;
			}
		}

		return rendered > 0 ? totalMs / rendered : 0;
	}
}
