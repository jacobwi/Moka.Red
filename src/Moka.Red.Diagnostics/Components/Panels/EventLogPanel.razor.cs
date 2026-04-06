using Microsoft.AspNetCore.Components;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel displaying a chronological log of diagnostics events with type-based filtering.
///     Auto-refreshes every second via a timer.
/// </summary>
public sealed partial class EventLogPanel : ComponentBase, IDisposable
{
	private bool _disposed;
	private IReadOnlyList<DiagnosticsEvent> _events = [];
	private IReadOnlyList<DiagnosticsEvent> _filteredEvents = [];
	private Timer? _refreshTimer;
	private bool _showDisposal = true;
	private bool _showError = true;
	private bool _showJsInterop = true;
	private bool _showRender = true;
	private bool _showSkip = true;
	private bool _showWarning = true;

	[Inject] private IMokaDiagnosticsService? _diagnosticsService { get; set; }

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

		_events = _diagnosticsService.GetRecentEvents();
		ApplyFilter();
	}

	private void ApplyFilter()
	{
		_filteredEvents = _events.Where(e => e.Type switch
		{
			DiagnosticsEventType.Render => _showRender,
			DiagnosticsEventType.RenderSkip => _showSkip,
			DiagnosticsEventType.Disposal => _showDisposal,
			DiagnosticsEventType.JsInterop => _showJsInterop,
			DiagnosticsEventType.Warning => _showWarning,
			DiagnosticsEventType.Error => _showError,
			_ => true
		}).ToList();
	}

	private void Clear()
	{
		_diagnosticsService?.ClearEvents();
		RefreshData();
	}

	private static string FormatType(DiagnosticsEventType type) => type switch
	{
		DiagnosticsEventType.Render => "RND",
		DiagnosticsEventType.RenderSkip => "SKP",
		DiagnosticsEventType.Disposal => "DSP",
		DiagnosticsEventType.JsInterop => "JS",
		DiagnosticsEventType.Warning => "WRN",
		DiagnosticsEventType.Error => "ERR",
		_ => "?"
	};
}
