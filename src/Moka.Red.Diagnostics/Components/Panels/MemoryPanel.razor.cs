using System.Runtime;
using Microsoft.AspNetCore.Components;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel displaying GC generation collection counts, total memory,
///     managed heap size, and GC configuration info.
///     Auto-refreshes every 2 seconds. Includes a "Force GC" button.
/// </summary>
public sealed partial class MemoryPanel : ComponentBase, IDisposable
{
	private bool _disposed;
	private long _finalizationPending;
	private int _gen0;
	private int _gen1;
	private int _gen2;
	private bool _isServerGc;
	private DateTime _lastUpdated;
	private string _latencyMode = "";
	private long _managedMemory;
	private int _maxCollections;
	private Timer? _refreshTimer;
	private long _totalMemory;

	/// <inheritdoc />
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_refreshTimer?.Dispose();
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		RefreshData();
		_refreshTimer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
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
		_gen0 = GC.CollectionCount(0);
		_gen1 = GC.CollectionCount(1);
		_gen2 = GC.CollectionCount(2);
		_totalMemory = GC.GetTotalMemory(forceFullCollection: false);
		_managedMemory = GC.GetTotalMemory(forceFullCollection: false);
		_isServerGc = GCSettings.IsServerGC;
		_latencyMode = GCSettings.LatencyMode.ToString();
		_lastUpdated = DateTime.Now;
		_maxCollections = Math.Max(_gen0, Math.Max(_gen1, _gen2));

		GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();
		_finalizationPending = memoryInfo.FinalizationPendingCount;
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
