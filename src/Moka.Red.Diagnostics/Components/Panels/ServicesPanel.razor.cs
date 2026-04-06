using Microsoft.AspNetCore.Components;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel displaying memory usage, component statistics, and registered Moka service status.
///     Auto-refreshes every second via a timer.
/// </summary>
public sealed partial class ServicesPanel : ComponentBase, IDisposable
{
	private int _activeComponents;
	private double _avgRenderMs;
	private bool _disposed;
	private int _disposedComponents;
	private int _gen0Collections;
	private int _gen1Collections;
	private int _gen2Collections;
	private long _managedMemoryBytes;
	private Timer? _refreshTimer;
	private List<ServiceCheck> _serviceChecks = [];
	private bool _servicesCached;
	private int _totalTracked;

	[Inject] private IMokaDiagnosticsService? _diagnosticsService { get; set; }

	[Inject] private IServiceProvider _serviceProvider { get; set; } = default!;

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

		InvokeAsync(() =>
		{
			RefreshData();
			StateHasChanged();
		});
	}

	private void RefreshData()
	{
		_managedMemoryBytes = GC.GetTotalMemory(false);
		_gen0Collections = GC.CollectionCount(0);
		_gen1Collections = GC.CollectionCount(1);
		_gen2Collections = GC.CollectionCount(2);

		if (!_servicesCached)
		{
			DetectRegisteredServices();
			_servicesCached = true;
		}

		if (_diagnosticsService is not null)
		{
			_activeComponents = _diagnosticsService.ActiveComponentCount;
			_disposedComponents = _diagnosticsService.DisposedComponentCount;

			IReadOnlyList<ComponentRenderEntry> entries = _diagnosticsService.GetRenderEntries();
			_totalTracked = entries.Count;

			if (entries.Count > 0)
			{
				double totalMs = 0.0;
				int count = 0;

				foreach (ComponentRenderEntry entry in entries)
				{
					if (entry.RenderCount > 0)
					{
						totalMs += entry.LastRenderDuration.TotalMilliseconds;
						count++;
					}
				}

				_avgRenderMs = count > 0 ? totalMs / count : 0;
			}
			else
			{
				_avgRenderMs = 0;
			}
		}
	}

	private void DetectRegisteredServices()
	{
		_serviceChecks =
		[
			new ServiceCheck("DiagnosticsService", true),
			new ServiceCheck("ToastService", CheckServiceByName("Moka.Red.Feedback.Toast.IMokaToastService")),
			new ServiceCheck("DialogService", CheckServiceByName("Moka.Red.Feedback.Dialog.IMokaDialogService"))
		];
	}

	private bool CheckServiceByName(string fullTypeName)
	{
		Type? type = AppDomain.CurrentDomain.GetAssemblies()
			.Select(a => a.GetType(fullTypeName))
			.FirstOrDefault(t => t is not null);

		return type is not null && _serviceProvider.GetService(type) is not null;
	}

	private static string FormatBytes(long bytes)
	{
		const double mb = 1024.0 * 1024.0;
		return $"{bytes / mb:F1} MB";
	}

	private sealed record ServiceCheck(string Name, bool IsRegistered);
}
