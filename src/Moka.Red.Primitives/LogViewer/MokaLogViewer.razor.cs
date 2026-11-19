using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.LogViewer;

/// <summary>
///     Streaming log display with level filtering, search, timestamps, and auto-scroll.
///     Designed for dev tools, admin panels, and diagnostic views.
/// </summary>
/// <remarks>
///     <para>
///         With <see cref="Virtualize" /> only the rows in view are drawn, through Blazor's <c>Virtualize</c>, so a log of
///         any length costs the rows on screen. Each row is then one line, <see cref="VirtualRowHeight" /> tall, and an
///         opened row's detail shows in a pane under the list.
///     </para>
///     <para>
///         <see cref="ItemsProvider" /> loads the rows in view on demand, for a log kept on a server. Searching and the
///         level filter are then the provider's job: bind <see cref="SearchText" /> and <see cref="Levels" />, read them
///         in the provider, and the viewer asks it again whenever either changes.
///     </para>
/// </remarks>
public partial class MokaLogViewer : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Primitives/LogViewer/MokaLogViewer.razor.js";

	private static readonly MokaLogLevel[] AllLevels = Enum.GetValues<MokaLogLevel>();

	private readonly string _idPrefix = $"moka-log-viewer-{Guid.NewGuid():N}";

	// Opened rows of the plain list, by the entry itself: two entries with the same text are still two rows.
	private readonly HashSet<MokaLogEntry> _open = new(ReferenceEqualityComparer.Instance);

	private ElementReference _body;
	private bool _following;
	private bool _askProviderAgain;
	private HashSet<MokaLogLevel>? _lastLevels;
	private string? _lastSearchText;
	private HashSet<MokaLogLevel> _levels = [.. AllLevels];
	private bool _maxEntriesGiven;
	private MokaLogEntry? _paneEntry;
	private string _searchText = string.Empty;
	private ICollection<MokaLogEntry> _shown = [];
	private Virtualize<MokaLogEntry>? _virtualize;

	/// <summary>Log entries to display. Set this or <see cref="ItemsProvider" />.</summary>
	[Parameter]
	public IReadOnlyList<MokaLogEntry>? Entries { get; set; }

	/// <summary>
	///     Loads the entries in view on demand, for a log too long to hold in memory, such as one kept on a server. Set
	///     this or <see cref="Entries" />. The rows are drawn through <c>Virtualize</c> whatever <see cref="Virtualize" />
	///     says. The provider filters: read <see cref="SearchText" /> and <see cref="Levels" /> through their bindings, and
	///     use the request's <c>CancellationToken</c>, because a newer search or a scroll cancels the request. Call
	///     <see cref="RefreshDataAsync" /> when the log behind it changes.
	/// </summary>
	[Parameter]
	public ItemsProviderDelegate<MokaLogEntry>? ItemsProvider { get; set; }

	/// <summary>
	///     Maximum entries to keep visible. Oldest entries are trimmed. Default is 500. Under <see cref="Virtualize" />
	///     it applies only when set, so a virtualized log shows every entry unless told otherwise.
	/// </summary>
	[Parameter]
	public int MaxEntries { get; set; } = 500;

	/// <summary>
	///     Draws only the rows in view, through Blazor's <c>Virtualize</c>. Each row is then one line,
	///     <see cref="VirtualRowHeight" /> tall, with a long message cut at the end, and an opened row's detail shows in a
	///     pane under the list. Default false.
	/// </summary>
	[Parameter]
	public bool Virtualize { get; set; }

	/// <summary>
	///     Each row's height in pixels under <see cref="Virtualize" />. It is exact, not an estimate: <c>Virtualize</c>
	///     places the rows by it and the row is drawn this tall. Default 24.
	/// </summary>
	[Parameter]
	public float VirtualRowHeight { get; set; } = 24;

	/// <summary>
	///     The search box's text. Bind it to keep or set the search, or to query a server with
	///     <see cref="ItemsProvider" />. With <see cref="Entries" /> the viewer matches it against each message and source.
	/// </summary>
	[Parameter]
	public string? SearchText { get; set; }

	/// <summary>Raised with the new text whenever the search changes.</summary>
	[Parameter]
	public EventCallback<string> SearchTextChanged { get; set; }

	/// <summary>
	///     The levels shown. Null shows every level. Bind it to keep or set the level filter, or to query a server with
	///     <see cref="ItemsProvider" />.
	/// </summary>
	[Parameter]
	public IReadOnlySet<MokaLogLevel>? Levels { get; set; }

	/// <summary>Raised with a new set of the levels shown whenever a level is turned on or off.</summary>
	[Parameter]
	public EventCallback<IReadOnlySet<MokaLogLevel>> LevelsChanged { get; set; }

	/// <summary>
	///     Draws an opened row's detail in place of its exception and properties. A row can be opened when its entry has
	///     <see cref="MokaLogEntry.HasDetail" />.
	/// </summary>
	[Parameter]
	public RenderFragment<MokaLogEntry>? DetailTemplate { get; set; }

	/// <summary>The words the viewer shows and says. English unless set.</summary>
	[Parameter]
	public MokaLogViewerText? Text { get; set; }

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

	/// <summary>
	///     Keeps the newest entry in view while the list is scrolled to the bottom. Scrolling up stops it, and scrolling
	///     back to the bottom starts it again. It runs in the browser, so scrolling sends nothing to .NET. Default is true.
	/// </summary>
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
		.AddClass("moka-log-viewer--virtual", Virtualizes)
		.AddClass(Class)
		.Build();

	private MokaLogViewerText Words => Text ?? MokaLogViewerText.Default;

	private bool Virtualizes => Virtualize || ItemsProvider is not null;

	private string? BodyStyle => new StyleBuilder()
		.AddStyle("max-height", MaxHeight)
		.AddStyle("--_log-row-height", $"{VirtualRowHeight.ToString(CultureInfo.InvariantCulture)}px", Virtualizes)
		.Build();

	private string? PaneStyle => new StyleBuilder()
		.AddStyle("max-height", MaxHeight)
		.Build();

	private string PaneId => $"{_idPrefix}-pane";

	// The pane's title is spans side by side with no space between them, which a screen reader would run together.
	private static string PaneLabel(MokaLogEntry entry) => entry.Source is null
		? $"{FormatTimestamp(entry.Timestamp)} {LevelToLabel(entry.Level)}"
		: $"{FormatTimestamp(entry.Timestamp)} {LevelToLabel(entry.Level)} {entry.Source}";

	/// <summary>
	///     Asks <see cref="ItemsProvider" /> for the rows in view again and draws them. Call it on the renderer's thread
	///     (<c>InvokeAsync</c>) when the log behind the provider changes. With <see cref="Entries" /> it does nothing: a
	///     new list or a render of the parent is enough.
	/// </summary>
	/// <returns>A task that completes once the rows have been asked for.</returns>
	public async Task RefreshDataAsync()
	{
		if (ItemsProvider is null || _virtualize is null)
		{
			return;
		}

		await _virtualize.RefreshDataAsync();
		StateHasChanged();
	}

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		_maxEntriesGiven = parameters.TryGetValue<int>(nameof(MaxEntries), out _);
		return base.SetParametersAsync(parameters);
	}

	/// <summary>Has internal filter/search state.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (ItemsProvider is not null && Entries is not null)
		{
			throw new InvalidOperationException(
				$"{nameof(MokaLogViewer)} takes {nameof(Entries)} or {nameof(ItemsProvider)}, not both.");
		}

		bool searchChanged = TakeSearchText();
		bool levelsChanged = TakeLevels();
		_askProviderAgain = HasRendered && ItemsProvider is not null && (searchChanged || levelsChanged);

		if (Virtualizes)
		{
			_open.Clear();
		}
		else
		{
			_paneEntry = null;
		}

		Filter();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// A search or levels the parent changed are asked for once the render has put the provider's Virtualize in place.
		if (_askProviderAgain)
		{
			_askProviderAgain = false;
			await RefreshDataAsync();
		}

		if (AutoScroll != _following)
		{
			_following = AutoScroll;
			await SafeModuleInvokeVoidAsync(ModulePath, AutoScroll ? "follow" : "unfollow", _body);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_following)
		{
			_following = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "unfollow", _body);
		}

		await base.DisposeAsyncCore();
	}

	// SearchText seeds the search only when the parent passes a new value, so typing survives a parent that renders
	// without binding it, and the echo of a change reported to the parent asks the provider nothing.
	private bool TakeSearchText()
	{
		if (string.Equals(SearchText, _lastSearchText, StringComparison.Ordinal))
		{
			return false;
		}

		_lastSearchText = SearchText;
		string next = SearchText ?? string.Empty;
		if (string.Equals(next, _searchText, StringComparison.Ordinal))
		{
			return false;
		}

		_searchText = next;
		return true;
	}

	// Compared by content against a copy, so a set the parent builds again on every render changes nothing.
	private bool TakeLevels()
	{
		bool same = Levels is null ? _lastLevels is null : _lastLevels is not null && _lastLevels.SetEquals(Levels);
		if (same)
		{
			return false;
		}

		_lastLevels = Levels is null ? null : [.. Levels];
		HashSet<MokaLogLevel> next = Levels is null ? [.. AllLevels] : [.. Levels];
		if (next.SetEquals(_levels))
		{
			return false;
		}

		_levels = next;
		return true;
	}

	private void Filter()
	{
		if (ItemsProvider is not null || Entries is null)
		{
			_shown = [];
			_open.Clear();
			return;
		}

		int skip = Virtualizes && !_maxEntriesGiven ? 0 : Math.Clamp(Entries.Count - MaxEntries, 0, Entries.Count);
		bool searching = !string.IsNullOrWhiteSpace(_searchText);
		bool everyLevel = _levels.Count == AllLevels.Length;

		if (_open.Count > 0)
		{
			_open.IntersectWith(Entries);
		}

		if (skip == 0 && everyLevel && !searching && Entries is ICollection<MokaLogEntry> whole)
		{
			_shown = whole;
			return;
		}

		List<MokaLogEntry> shown = new(Entries.Count - skip);
		for (int i = skip; i < Entries.Count; i++)
		{
			MokaLogEntry entry = Entries[i];
			if (_levels.Contains(entry.Level) && (!searching || Matches(entry)))
			{
				shown.Add(entry);
			}
		}

		_shown = shown;
	}

	private bool Matches(MokaLogEntry entry) =>
		entry.Message.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
		(entry.Source is not null && entry.Source.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

	private async Task OnSearchChangedAsync(string? typed)
	{
		_searchText = typed ?? string.Empty;
		Filter();
		// The provider reads the bound search, which the parent has taken by the time its callback returns.
		await SearchTextChanged.InvokeAsync(_searchText);
		await RefreshDataAsync();
	}

	private async Task ToggleLevelAsync(MokaLogLevel level)
	{
		if (!_levels.Remove(level))
		{
			_levels.Add(level);
		}

		Filter();
		await LevelsChanged.InvokeAsync(new HashSet<MokaLogLevel>(_levels));
		await RefreshDataAsync();
	}

	private bool IsLevelEnabled(MokaLogLevel level) => _levels.Contains(level);

	// A provider can hand back a new instance of the entry the pane holds, so the pane's row is found by value.
	private bool IsInPane(MokaLogEntry entry) => _paneEntry is not null && _paneEntry.Equals(entry);

	private void ToggleDetail(MokaLogEntry entry)
	{
		if (Virtualizes)
		{
			_paneEntry = IsInPane(entry) ? null : entry;
		}
		else if (!_open.Remove(entry))
		{
			_open.Add(entry);
		}
	}

	// The pane's close button goes with the pane, so focus goes to the list it belongs to.
	private async Task ClosePaneAsync()
	{
		_paneEntry = null;
		await SafeFocusAsync(_body);
	}

	private string DetailId(int index) => $"{_idPrefix}-detail-{index.ToString(CultureInfo.InvariantCulture)}";

	private string ToggleLabel(bool open) => open ? Words.HideDetails : Words.ShowDetails;

	private static string RowClass(MokaLogEntry entry, bool open) => new CssBuilder("moka-log-viewer__row")
		.AddClass($"moka-log-viewer__row--{LevelToCssClass(entry.Level)}")
		.AddClass("moka-log-viewer__row--open", open)
		.Build();

	private static string LevelClass(MokaLogLevel level) => new CssBuilder("moka-log-viewer__level")
		.AddClass($"moka-log-viewer__level--{LevelToCssClass(level)}")
		.Build();

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
