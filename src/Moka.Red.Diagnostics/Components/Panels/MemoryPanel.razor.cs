using System.Runtime;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel displaying GC generation collection counts, the bytes allocated now, the managed heap
///     size at the last collection, and GC configuration info.
///     Auto-refreshes every 2 seconds. Includes a "Force GC" button.
/// </summary>
public sealed partial class MemoryPanel : ComponentBase, IDisposable
{
	private long _allocatedBytes;
	private bool _disposed;
	private long _finalizationPending;
	private int _gen0;
	private int _gen1;
	private int _gen2;
	private long _heapSizeBytes;
	private bool _isServerGc;
	private DateTime _lastUpdated;
	private string _latencyMode = "";
	private int _maxCollections;
	private PanelRefresh? _refresh;
	private bool _shown;

	[Inject] private IServiceProvider Services { get; set; } = default!;

	/// <inheritdoc />
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
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		// Force GC belongs to the whole process, so outside the Development environment, unless the app opted in, the
		// panel shows nothing.
		_shown = DiagnosticsServiceResolver.IsOn(Services);
		if (!_shown)
		{
			return;
		}

		RefreshData();
		_refresh = new PanelRefresh(Services.GetService<TimeProvider>() ?? TimeProvider.System, TimeSpan.FromSeconds(2),
			InvokeAsync, DispatchExceptionAsync, RefreshData, StateHasChanged);
	}

	private bool RefreshData()
	{
		_gen0 = GC.CollectionCount(0);
		_gen1 = GC.CollectionCount(1);
		_gen2 = GC.CollectionCount(2);
		_allocatedBytes = GC.GetTotalMemory(forceFullCollection: false);
		_isServerGc = GCSettings.IsServerGC;
		_latencyMode = GCSettings.LatencyMode.ToString();
		_lastUpdated = DateTime.Now;
		_maxCollections = Math.Max(_gen0, Math.Max(_gen1, _gen2));

		// Describes the heap as the last collection left it, so it moves only when a GC runs.
		GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();
		_heapSizeBytes = memoryInfo.HeapSizeBytes;
		_finalizationPending = memoryInfo.FinalizationPendingCount;
		return true;
	}

	private void ForceGc()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		RefreshData();
	}

	private string BarWidth(int count)
	{
		if (_maxCollections <= 0)
		{
			return "0%";
		}

		double percentage = (double)count / _maxCollections * 100;
		return $"{percentage:F0}%";
	}

	private static string FormatBytes(long bytes)
	{
		return bytes switch
		{
			>= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
			>= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
			>= 1_024 => $"{bytes / 1_024.0:F0} KB",
			_ => $"{bytes} B"
		};
	}
}
