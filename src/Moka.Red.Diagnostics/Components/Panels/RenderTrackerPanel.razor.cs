using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel displaying component render counts, ShouldRender skip stats, and timing data.
///     Supports sortable column headers and a component summary tree.
///     Refreshes every second while it lists anything, since the time since each render moves.
/// </summary>
public sealed partial class RenderTrackerPanel : ComponentBase, IDisposable
{
	private bool _disposed;
	private IReadOnlyList<ComponentRenderEntry> _entries = [];

	private ILookup<string, ComponentRenderEntry> _groupedEntries =
		Enumerable.Empty<ComponentRenderEntry>().ToLookup(e => e.ComponentType);

	private PanelRefresh? _refresh;
	private string _sortBy = "RenderCount";
	private bool _sortDescending = true;
	private IReadOnlyList<ComponentRenderEntry> _sortedEntries = [];
	private int _totalRenders;
	private int _totalSkipped;
	private HashSet<string> _uniqueTypes = [];

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

		bool wasEmpty = _entries.Count == 0;

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
		return !wasEmpty || _entries.Count > 0;
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

	// The arrow is hidden from screen readers, which hear the sort from the column header instead.
	private string? AriaSort(string column)
	{
		if (_sortBy != column)
		{
			return null;
		}

		return _sortDescending ? "descending" : "ascending";
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
			return "never";
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
