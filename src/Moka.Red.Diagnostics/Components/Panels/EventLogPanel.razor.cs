using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel displaying a chronological log of diagnostics events with type-based filtering.
///     Checks for new events every second and renders only when there are.
/// </summary>
public sealed partial class EventLogPanel : ComponentBase, IDisposable
{
	private readonly RowKeys<DiagnosticsEvent> _rowKeys = new();
	private bool _disposed;
	private IReadOnlyList<DiagnosticsEvent> _events = [];
	private IReadOnlyList<DiagnosticsEvent> _filteredEvents = [];
	private PanelRefresh? _refresh;
	private bool _showDisposal = true;
	private bool _showError = true;
	private bool _showJsInterop = true;
	private bool _showRender = true;
	private bool _showSkip = true;
	private bool _showWarning = true;

	private IMokaDiagnosticsService? _diagnosticsService;

	[Inject] private IServiceProvider Services { get; set; } = default!;

	[CascadingParameter(Name = DiagnosticsServiceResolver.CascadeName)]
	private IMokaDiagnosticsService? SharedService { get; set; }

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

		// Every event the log keeps, so the MaxEventLogEntries setting shows here.
		IReadOnlyList<DiagnosticsEvent> events = _diagnosticsService.GetRecentEvents(int.MaxValue);
		if (RowKeys<DiagnosticsEvent>.SameLog(_events, events))
		{
			return false;
		}

		_events = events;
		ApplyFilter();
		return true;
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
