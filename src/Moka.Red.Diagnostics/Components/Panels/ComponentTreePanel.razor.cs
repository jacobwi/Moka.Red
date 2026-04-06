using Microsoft.AspNetCore.Components;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel displaying registered components in a tree grouped by type,
///     with render count, last render time, and color-coded heat indicators.
///     Green for low render count (&le;5), yellow for medium (6-20), red for hot (&gt;20).
///     Auto-refreshes every second via a timer.
/// </summary>
public sealed partial class ComponentTreePanel : ComponentBase, IDisposable
{
	private bool _disposed;
	private Timer? _refreshTimer;
	private int _totalDisposed;
	private int _totalInstances;
	private IReadOnlyList<ComponentTypeGroup> _typeGroups = [];

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

		IReadOnlyList<ComponentRenderEntry> entries = _diagnosticsService.GetRenderEntries();
		_totalInstances = entries.Count;
		_totalDisposed = entries.Count(e => e.IsDisposed);

		_typeGroups = entries
			.GroupBy(e => e.ComponentType)
			.Select(g => new ComponentTypeGroup
			{
				TypeName = g.Key,
				InstanceCount = g.Count(),
				TotalRenders = g.Sum(e => e.RenderCount),
				Instances = g.OrderByDescending(e => e.RenderCount).ToList()
			})
			.OrderByDescending(g => g.TotalRenders)
			.ToList();
	}

	private static string HeatClass(int renderCount) =>
		renderCount switch
		{
			> 20 => "moka-diag-comptree-heat--hot",
			> 5 => "moka-diag-comptree-heat--warm",
			_ => "moka-diag-comptree-heat--cool"
		};

	private static string RenderCountClass(int renderCount) =>
		renderCount switch
		{
			> 20 => "moka-diag-comptree-val--hot",
			> 5 => "moka-diag-comptree-val--warm",
			_ => ""
		};

	private static string ShortenId(string id)
	{
		// Show last 8 chars of long IDs
		return id.Length > 16 ? $"...{id[^12..]}" : id;
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

	/// <summary>
	///     Groups component instances by their type name for the tree view.
	/// </summary>
	private sealed class ComponentTypeGroup
	{
		/// <summary>Short type name of the component.</summary>
		public required string TypeName { get; init; }

		/// <summary>Number of instances of this type.</summary>
		public int InstanceCount { get; init; }

		/// <summary>Sum of all render counts across instances.</summary>
		public int TotalRenders { get; init; }

		/// <summary>Individual instances ordered by render count descending.</summary>
		public required IReadOnlyList<ComponentRenderEntry> Instances { get; init; }
	}
}
