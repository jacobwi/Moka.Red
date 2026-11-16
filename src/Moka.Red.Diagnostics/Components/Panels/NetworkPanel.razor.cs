using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel displaying JS interop call statistics with sortable columns.
///     Shows method name, call count, total time, average time, and max time.
///     Checks every second and renders only when a call was recorded.
/// </summary>
public sealed partial class NetworkPanel : ComponentBase, IDisposable
{
	private bool _disposed;
	private IReadOnlyList<JsInteropEntry> _entries = [];
	private PanelRefresh? _refresh;
	private string _sortBy = "TotalDuration";
	private bool _sortDescending = true;
	private IReadOnlyList<JsInteropEntry> _sortedEntries = [];
	private int _totalCalls;
	private double _totalTime;
	private (int Functions, int Calls, double Time) _shown = (-1, -1, -1);

	private IMokaDiagnosticsService? _diagnosticsService;

	[Inject] private IServiceProvider Services { get; set; } = default!;

	[CascadingParameter(Name = DiagnosticsServiceResolver.CascadeName)]
	private IMokaDiagnosticsService? SharedService { get; set; }

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

		_entries = _diagnosticsService.GetJsInteropEntries();
		_totalCalls = 0;
		_totalTime = 0;

		foreach (JsInteropEntry entry in _entries)
		{
			_totalCalls += entry.CallCount;
			_totalTime += entry.TotalDuration.TotalMilliseconds;
		}

		ApplySort();

		(int, int, double) shown = (_entries.Count, _totalCalls, _totalTime);
		bool changed = shown != _shown;
		_shown = shown;
		return changed;
	}

	private void ApplySort()
	{
		IEnumerable<JsInteropEntry> entries = _entries.AsEnumerable();

		_sortedEntries = (_sortBy switch
		{
			"Identifier" => _sortDescending
				? entries.OrderByDescending(e => e.Identifier)
				: entries.OrderBy(e => e.Identifier),
			"CallCount" => _sortDescending
				? entries.OrderByDescending(e => e.CallCount)
				: entries.OrderBy(e => e.CallCount),
			"TotalDuration" => _sortDescending
				? entries.OrderByDescending(e => e.TotalDuration)
				: entries.OrderBy(e => e.TotalDuration),
			"AverageDuration" => _sortDescending
				? entries.OrderByDescending(e => e.AverageDuration)
				: entries.OrderBy(e => e.AverageDuration),
			"MaxDuration" => _sortDescending
				? entries.OrderByDescending(e => e.MaxDuration)
				: entries.OrderBy(e => e.MaxDuration),
			_ => entries.OrderByDescending(e => e.TotalDuration)
		}).ToList();
	}

	private void SortBy(string column)
	{
		if (_sortBy == column)
		{
			_sortDescending = !_sortDescending;
		}
		else
		{
			_sortBy = column;
			_sortDescending = true;
		}

		ApplySort();
	}

	private string SortIndicator(string column)
	{
		if (_sortBy != column)
		{
			return "";
		}

		return _sortDescending ? "\u25BE" : "\u25B4";
	}

	// The arrow is hidden from screen readers, which hear the sort from the column header instead.
	private string? AriaSort(string column)
	{
		if (_sortBy != column)
		{
			return null;
		}

		return _sortDescending ? "descending" : "ascending";
	}

	private static string RowClass(JsInteropEntry entry)
	{
		if (entry.MaxDuration.TotalMilliseconds > 100)
		{
			return "moka-diag-network-row--slow";
		}

		if (entry.CallCount > 50)
		{
			return "moka-diag-network-row--hot";
		}

		return "";
	}
}
