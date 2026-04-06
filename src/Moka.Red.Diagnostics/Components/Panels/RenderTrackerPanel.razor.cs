using Microsoft.AspNetCore.Components;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel displaying component render counts, ShouldRender skip stats, and timing data.
///     Supports sortable column headers and a component summary tree.
///     Auto-refreshes every second via a timer.
/// </summary>
public sealed partial class RenderTrackerPanel : ComponentBase, IDisposable
{
	private bool _disposed;
	private IReadOnlyList<ComponentRenderEntry> _entries = [];

	private ILookup<string, ComponentRenderEntry> _groupedEntries =
		Enumerable.Empty<ComponentRenderEntry>().ToLookup(e => e.ComponentType);

	private Timer? _refreshTimer;
	private string _sortBy = "RenderCount";
	private bool _sortDescending = true;
	private IReadOnlyList<ComponentRenderEntry> _sortedEntries = [];
	private int _totalRenders;
	private int _totalSkipped;
	private HashSet<string> _uniqueTypes = [];

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

		_entries = _diagnosticsService.GetRenderEntries();
		_totalRenders = 0;
		_totalSkipped = 0;

		foreach (ComponentRenderEntry entry in _entries)
		{
			_totalRenders += entry.RenderCount;
			_totalSkipped += entry.ShouldRenderSkipCount;
		}

		ApplySort();

		_uniqueTypes = _entries.Select(e => e.ComponentType).ToHashSet();
		_groupedEntries = _entries.ToLookup(e => e.ComponentType);
	}

	private void ApplySort()
	{
		IEnumerable<ComponentRenderEntry> entries = _entries.AsEnumerable();

		_sortedEntries = (_sortBy switch
		{
			"ComponentType" => _sortDescending
				? entries.OrderByDescending(e => e.ComponentType)
				: entries.OrderBy(e => e.ComponentType),
			"RenderCount" => _sortDescending
				? entries.OrderByDescending(e => e.RenderCount)
				: entries.OrderBy(e => e.RenderCount),
			"SkipCount" => _sortDescending
				? entries.OrderByDescending(e => e.ShouldRenderSkipCount)
				: entries.OrderBy(e => e.ShouldRenderSkipCount),
			"LastRender" => _sortDescending
				? entries.OrderByDescending(e => e.LastRenderTime)
				: entries.OrderBy(e => e.LastRenderTime),
			"Duration" => _sortDescending
				? entries.OrderByDescending(e => e.LastRenderDuration)
				: entries.OrderBy(e => e.LastRenderDuration),
			_ => entries.OrderByDescending(e => e.RenderCount)
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

	private void Clear()
	{
		_diagnosticsService?.ClearRenderData();
		RefreshData();
	}

	private static string FormatRelativeTime(DateTime utcTime)
	{
		if (utcTime == default)
		{
			return "\u2014";
		}

		TimeSpan elapsed = DateTime.UtcNow - utcTime;

		if (elapsed.TotalSeconds < 1)
		{
			return "now";
		}

		if (elapsed.TotalSeconds < 60)
		{
			return $"{(int)elapsed.TotalSeconds}s ago";
		}

		if (elapsed.TotalMinutes < 60)
		{
			return $"{(int)elapsed.TotalMinutes}m ago";
		}

		return $"{(int)elapsed.TotalHours}h ago";
	}
}
