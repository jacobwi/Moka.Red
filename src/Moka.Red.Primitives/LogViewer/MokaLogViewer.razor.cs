using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.LogViewer;

/// <summary>
///     Streaming log display with level filtering, search, timestamps, and auto-scroll.
///     Designed for dev tools, admin panels, and diagnostic views.
/// </summary>
public partial class MokaLogViewer : MokaComponentBase
{
	private readonly HashSet<MokaLogLevel> _enabledLevels =
	[
		MokaLogLevel.Trace, MokaLogLevel.Debug, MokaLogLevel.Info,
		MokaLogLevel.Warning, MokaLogLevel.Error, MokaLogLevel.Fatal
	];

	private string _searchText = string.Empty;

	/// <summary>Log entries to display.</summary>
	[Parameter]
	public IReadOnlyList<MokaLogEntry>? Entries { get; set; }

	/// <summary>Maximum entries to keep visible. Oldest entries are trimmed. Default is 500.</summary>
	[Parameter]
	public int MaxEntries { get; set; } = 500;

	/// <summary>Whether to show the timestamp column. Default is true.</summary>
	[Parameter]
	public bool ShowTimestamp { get; set; } = true;

	/// <summary>Whether to show the level badge. Default is true.</summary>
	[Parameter]
	public bool ShowLevel { get; set; } = true;

	/// <summary>Whether to show the search input. Default is true.</summary>
	[Parameter]
	public bool ShowSearch { get; set; } = true;

	/// <summary>Whether to show level filter checkboxes. Default is true.</summary>
	[Parameter]
	public bool ShowLevelFilter { get; set; } = true;

	/// <summary>Whether to auto-scroll to the latest entry. Default is true.</summary>
	[Parameter]
	public bool AutoScroll { get; set; } = true;

	/// <summary>Whether to use monospace font for log messages. Default is true.</summary>
	[Parameter]
	public bool MonoFont { get; set; } = true;

	/// <summary>Maximum height of the log area. Accepts any CSS value. Default is "400px".</summary>
	[Parameter]
	public string MaxHeight { get; set; } = "400px";

	/// <inheritdoc />
	protected override string RootClass => "moka-log-viewer";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-log-viewer--mono", MonoFont)
		.AddClass(Class)
		.Build();

	private IEnumerable<MokaLogEntry> FilteredEntries
	{
		get
		{
			if (Entries is null)
			{
				return [];
			}

			IEnumerable<MokaLogEntry> entries = Entries;

			// Trim to max
			if (Entries.Count > MaxEntries)
			{
				entries = entries.Skip(Entries.Count - MaxEntries);
			}

			// Level filter
			entries = entries.Where(e => _enabledLevels.Contains(e.Level));

			// Search filter
			if (!string.IsNullOrWhiteSpace(_searchText))
			{
				entries = entries.Where(e =>
					e.Message.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					(e.Source is not null && e.Source.Contains(_searchText, StringComparison.OrdinalIgnoreCase)));
			}

			return entries;
		}
	}

	/// <summary>Has internal filter/search state.</summary>
	protected override bool ShouldRender() => true;

	private void OnSearchChanged(ChangeEventArgs e) => _searchText = e.Value?.ToString() ?? string.Empty;

	private void ToggleLevel(MokaLogLevel level)
	{
		if (!_enabledLevels.Remove(level))
		{
			_enabledLevels.Add(level);
		}
	}

	private bool IsLevelEnabled(MokaLogLevel level) => _enabledLevels.Contains(level);

	private static string FormatTimestamp(DateTime timestamp) =>
		timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

	private static string LevelToLabel(MokaLogLevel level) => level switch
	{
		MokaLogLevel.Trace => "TRC",
		MokaLogLevel.Debug => "DBG",
		MokaLogLevel.Info => "INF",
		MokaLogLevel.Warning => "WRN",
		MokaLogLevel.Error => "ERR",
		MokaLogLevel.Fatal => "FTL",
		_ => "???"
	};

	private static string LevelToCssClass(MokaLogLevel level) => level switch
	{
		MokaLogLevel.Trace => "trace",
		MokaLogLevel.Debug => "debug",
		MokaLogLevel.Info => "info",
		MokaLogLevel.Warning => "warning",
		MokaLogLevel.Error => "error",
		MokaLogLevel.Fatal => "fatal",
		_ => "info"
	};
}
