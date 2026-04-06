using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel displaying captured ILogger output with level filtering, category filtering,
///     and expandable exception stack traces. Auto-refreshes every second via a timer.
/// </summary>
public sealed partial class ConsolePanel : ComponentBase, IDisposable
{
	private readonly HashSet<string> _expandedExceptions = [];
	private string _categoryFilter = "";
	private bool _disposed;
	private IReadOnlyList<ConsoleLogEntry> _filteredMessages = [];
	private IReadOnlyList<ConsoleLogEntry> _messages = [];
	private Timer? _refreshTimer;
	private bool _showCritical = true;

	private bool _showDebug = true;
	private bool _showError = true;
	private bool _showInfo = true;
	private bool _showWarning = true;

	[Inject] private MokaDiagnosticsConsoleBuffer? _buffer { get; set; }

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
		if (_buffer is null)
		{
			return;
		}

		_messages = _buffer.GetMessages();
		ApplyFilter();
	}

	private void ApplyFilter()
	{
		_filteredMessages = _messages.Where(m =>
		{
			bool levelMatch = m.Level switch
			{
				LogLevel.Debug or LogLevel.Trace => _showDebug,
				LogLevel.Information => _showInfo,
				LogLevel.Warning => _showWarning,
				LogLevel.Error => _showError,
				LogLevel.Critical => _showCritical,
				_ => true
			};

			if (!levelMatch)
			{
				return false;
			}

			if (!string.IsNullOrWhiteSpace(_categoryFilter) &&
			    !m.Category.Contains(_categoryFilter, StringComparison.OrdinalIgnoreCase) &&
			    !m.Message.Contains(_categoryFilter, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			return true;
		}).ToList();
	}

	private void HandleCategoryFilter(ChangeEventArgs e)
	{
		_categoryFilter = e.Value?.ToString() ?? "";
		ApplyFilter();
	}

	private void Clear()
	{
		_buffer?.Clear();
		_expandedExceptions.Clear();
		RefreshData();
	}

	private void ToggleException(string key)
	{
		if (!_expandedExceptions.Remove(key))
		{
			_expandedExceptions.Add(key);
		}
	}

	private static string GetExceptionKey(ConsoleLogEntry entry) =>
		$"{entry.Timestamp.Ticks}-{entry.Category}";

	private static string LevelBadgeClass(LogLevel level) => level switch
	{
		LogLevel.Debug or LogLevel.Trace => "moka-diag-console-badge--debug",
		LogLevel.Information => "moka-diag-console-badge--info",
		LogLevel.Warning => "moka-diag-console-badge--warning",
		LogLevel.Error => "moka-diag-console-badge--error",
		LogLevel.Critical => "moka-diag-console-badge--critical",
		_ => ""
	};

	private static string LevelText(LogLevel level) => level switch
	{
		LogLevel.Trace => "TRC",
		LogLevel.Debug => "DBG",
		LogLevel.Information => "INF",
		LogLevel.Warning => "WRN",
		LogLevel.Error => "ERR",
		LogLevel.Critical => "CRT",
		_ => "?"
	};
}
