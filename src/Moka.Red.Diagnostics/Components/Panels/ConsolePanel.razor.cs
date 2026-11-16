using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Components.Panels;

/// <summary>
///     Panel displaying captured ILogger output with level filtering, category filtering,
///     and expandable exception stack traces. Checks for new messages every second and renders only when there are.
/// </summary>
public sealed partial class ConsolePanel : ComponentBase, IDisposable
{
	private readonly RowKeys<ConsoleLogEntry> _exceptionKeys = new();
	private readonly HashSet<ConsoleLogEntry> _expandedExceptions = new(ReferenceEqualityComparer.Instance);
	private readonly RowKeys<ConsoleLogEntry> _rowKeys = new();
	private string _categoryFilter = "";
	private bool _disposed;
	private IReadOnlyList<ConsoleLogEntry> _filteredMessages = [];
	private IReadOnlyList<ConsoleLogEntry> _messages = [];
	private PanelRefresh? _refresh;
	private bool _showCritical = true;

	private bool _showDebug = true;
	private bool _showError = true;
	private bool _showInfo = true;
	private bool _showWarning = true;

	private MokaDiagnosticsConsoleBuffer? _buffer;

	// Looked up rather than injected: [Inject] throws for a service that is not registered, even on a
	// nullable property.
	[Inject] private IServiceProvider Services { get; set; } = default!;

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
		_buffer = Services.GetService<MokaDiagnosticsConsoleBuffer>();
		RefreshData();
		_refresh = new PanelRefresh(Services.GetService<TimeProvider>() ?? TimeProvider.System, TimeSpan.FromSeconds(1),
			InvokeAsync, DispatchExceptionAsync, RefreshData, StateHasChanged);
	}

	private bool RefreshData()
	{
		if (_buffer is null)
		{
			return false;
		}

		IReadOnlyList<ConsoleLogEntry> messages = _buffer.GetMessages();
		if (RowKeys<ConsoleLogEntry>.SameLog(_messages, messages))
		{
			return false;
		}

		_messages = messages;
		_expandedExceptions.IntersectWith(messages);
		ApplyFilter();
		return true;
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

	private void HandleCategoryFilter(string? typed)
	{
		_categoryFilter = typed ?? "";
		ApplyFilter();
	}

	private void Clear()
	{
		_buffer?.Clear();
		_expandedExceptions.Clear();
		RefreshData();
	}

	private void ToggleException(ConsoleLogEntry entry)
	{
		if (!_expandedExceptions.Remove(entry))
		{
			_expandedExceptions.Add(entry);
		}
	}

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
