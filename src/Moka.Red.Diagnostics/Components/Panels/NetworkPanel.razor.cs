using Microsoft.AspNetCore.Components;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel displaying JS interop call statistics with sortable columns.
///     Shows method name, call count, total time, average time, and max time.
///     Auto-refreshes every second via a timer.
/// </summary>
public sealed partial class NetworkPanel : ComponentBase, IDisposable
{
	private bool _disposed;
	private IReadOnlyList<JsInteropEntry> _entries = [];
	private Timer? _refreshTimer;
	private string _sortBy = "TotalDuration";
	private bool _sortDescending = true;
	private IReadOnlyList<JsInteropEntry> _sortedEntries = [];
	private int _totalCalls;
	private double _totalTime;

	[Inject] private IMokaDiagnosticsService? _diagnosticsService { get; set; }

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

		_entries = _diagnosticsService.GetJsInteropEntries();
		_totalCalls = 0;
		_totalTime = 0;

		foreach (JsInteropEntry entry in _entries)
		{
			_totalCalls += entry.CallCount;
			_totalTime += entry.TotalDuration.TotalMilliseconds;
		}

		ApplySort();
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
