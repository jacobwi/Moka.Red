using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Base;
using Moka.Red.Core.Charts;
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
///         level filter are then the provider's job: bind <see cref="SearchText" />, <see cref="Levels" /> and
///         <see cref="MinimumLevel" />, read them in the provider, and the viewer asks it again whenever one changes.
///     </para>
/// </remarks>
public partial class MokaLogViewer : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Primitives/LogViewer/MokaLogViewer.razor.js";

	private const string DefaultTimestampFormat = "HH:mm:ss.fff";

	// HTML's datetime allows at most three digits of a second, so the round-trip "O" format doesn't qualify.
	private const string MachineFormat = "yyyy-MM-dd'T'HH:mm:ss.fff";

	private static readonly MokaLogLevel[] AllLevels = Enum.GetValues<MokaLogLevel>();

	private readonly string _idPrefix = $"moka-log-viewer-{Guid.NewGuid():N}";

	// Opened rows of the plain list, by the entry itself: two entries with the same text are still two rows.
	private readonly HashSet<MokaLogEntry> _open = new(ReferenceEqualityComparer.Instance);

	private int _aboveCount;
	private string? _aboveToken;
	private bool _askProviderAgain;
	private ElementReference _body;
	private TimeProvider? _clock;
	private MokaLogEntry? _firstShown;
	private HashSet<MokaLogLevel>? _lastLevels;
	private MokaLogLevel? _lastMinimum;
	private string? _lastSearchText;
	private (MokaLogOrder Order, MokaLogLevelFilter Filter)? _lastShape;
	private HashSet<MokaLogLevel> _levels = [.. AllLevels];
	private bool _loadedOlder;
	private bool _loadingOlder;
	private bool _maxEntriesGiven;
	private MokaLogLevel _minimum = MokaLogLevel.Trace;
	private MokaLogEntry? _paneEntry;
	private ItemsProviderDelegate<MokaLogEntry>? _provide;
	private int _providerTotal;
	private bool _quiet;
	private int _search;
	private string _searchText = string.Empty;
	private ITimer? _searchTimer;
	private ICollection<MokaLogEntry> _shown = [];
	private IReadOnlyList<MokaLogEntry> _shownList = [];
	private string _typed = string.Empty;
	private Virtualize<MokaLogEntry>? _virtualize;
	private Watch? _watching;

	// Optional, so looked up rather than injected: [Inject] throws when a service is missing.
	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	/// <summary>Log entries to display, oldest first. Set this or <see cref="ItemsProvider" />.</summary>
	[Parameter]
	public IReadOnlyList<MokaLogEntry>? Entries { get; set; }

	/// <summary>
	///     Loads the entries in view on demand, for a log too long to hold in memory, such as one kept on a server. Set
	///     this or <see cref="Entries" />. The rows are drawn through <c>Virtualize</c> whatever <see cref="Virtualize" />
	///     says. The provider filters: read <see cref="SearchText" />, <see cref="Levels" /> and <see cref="MinimumLevel" />
	///     through their bindings, and use the request's <c>CancellationToken</c>, because a newer search or a scroll
	///     cancels the request. It returns the rows in <see cref="Order" />: under <see cref="MokaLogOrder.NewestFirst" />
	///     row 0 is the newest entry. Call <see cref="RefreshDataAsync" /> when the log behind it changes.
	/// </summary>
	[Parameter]
	public ItemsProviderDelegate<MokaLogEntry>? ItemsProvider { get; set; }

	/// <summary>
	///     Maximum entries to keep visible. Oldest entries are trimmed. Default is 500. Under <see cref="Virtualize" />,
	///     and once <see cref="OnLoadOlder" /> has a listener, it applies only when set, so lines paged in stay.
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
	///     Which end of the list holds the newest entry. Default <see cref="MokaLogOrder.OldestFirst" />, the newest at the
	///     bottom. <see cref="Entries" /> are given oldest first in either order. <see cref="AutoScroll" /> follows the
	///     newest end, and the button <see cref="HasOlder" /> shows sits at the other.
	/// </summary>
	[Parameter]
	public MokaLogOrder Order { get; set; }

	/// <summary>
	///     Shows a button at the old end of the list, the top for <see cref="MokaLogOrder.OldestFirst" /> and the bottom
	///     for <see cref="MokaLogOrder.NewestFirst" />, that raises <see cref="OnLoadOlder" />. Turn it off once the start
	///     of the log is in. Default false.
	/// </summary>
	[Parameter]
	public bool HasOlder { get; set; }

	/// <summary>
	///     Raised by the older-lines button. Add the older entries to the start of <see cref="Entries" />, or let
	///     <see cref="ItemsProvider" /> count them, which the viewer asks again once this completes. The button shows it's
	///     busy until then and ignores more presses. Lines added above the row the reader is on leave it where it is.
	/// </summary>
	[Parameter]
	public EventCallback OnLoadOlder { get; set; }

	/// <summary>
	///     The search box's text. Bind it to keep or set the search, or to query a server with
	///     <see cref="ItemsProvider" />. With <see cref="Entries" /> the viewer matches it against each message and source.
	/// </summary>
	[Parameter]
	public string? SearchText { get; set; }

	/// <summary>
	///     Raised with the text the viewer searches for whenever it changes. Under <see cref="SearchDebounce" /> that is
	///     once typing pauses, and under <see cref="MokaLogSearchMode.OnSubmit" /> once the search is sent, never text
	///     still being typed.
	/// </summary>
	[Parameter]
	public EventCallback<string> SearchTextChanged { get; set; }

	/// <summary>
	///     When the viewer searches for what is typed. Default <see cref="MokaLogSearchMode.Live" />, as it is typed.
	///     <see cref="MokaLogSearchMode.OnSubmit" /> waits for Enter or the search button, for a search that costs a
	///     server round trip.
	/// </summary>
	[Parameter]
	public MokaLogSearchMode SearchMode { get; set; }

	/// <summary>
	///     How long typing has to pause before a <see cref="MokaLogSearchMode.Live" /> search runs. Null, the default,
	///     searches on every key.
	/// </summary>
	[Parameter]
	public TimeSpan? SearchDebounce { get; set; }

	/// <summary>
	///     The levels shown under <see cref="MokaLogLevelFilter.Toggles" />. Null shows every level. Bind it to keep or set
	///     the level filter, or to query a server with <see cref="ItemsProvider" />.
	/// </summary>
	[Parameter]
	public IReadOnlySet<MokaLogLevel>? Levels { get; set; }

	/// <summary>Raised with a new set of the levels shown whenever a level is turned on or off.</summary>
	[Parameter]
	public EventCallback<IReadOnlySet<MokaLogLevel>> LevelsChanged { get; set; }

	/// <summary>
	///     How the reader picks the levels shown. Default <see cref="MokaLogLevelFilter.Toggles" />, a button per level.
	///     <see cref="MokaLogLevelFilter.AtLeast" /> is one list of the lowest level shown, read through
	///     <see cref="MinimumLevel" />, and <see cref="Levels" /> is then left alone.
	/// </summary>
	[Parameter]
	public MokaLogLevelFilter LevelFilter { get; set; }

	/// <summary>
	///     The lowest level shown under <see cref="MokaLogLevelFilter.AtLeast" />: it and every worse level show. Default
	///     <see cref="MokaLogLevel.Trace" />, every level. Bind it to keep or set the threshold, or to query a server with
	///     <see cref="ItemsProvider" />.
	/// </summary>
	[Parameter]
	public MokaLogLevel MinimumLevel { get; set; }

	/// <summary>Raised with the new lowest level whenever the reader picks one.</summary>
	[Parameter]
	public EventCallback<MokaLogLevel> MinimumLevelChanged { get; set; }

	/// <summary>
	///     A .NET date format for the timestamps, such as <c>ddd HH:mm:ss</c> for a week or
	///     <c>yyyy-MM-dd HH:mm:ss</c> for a log of any age, written in the current culture. Null writes
	///     <c>HH:mm:ss.fff</c>.
	/// </summary>
	[Parameter]
	public string? TimestampFormat { get; set; }

	/// <summary>
	///     Writes each timestamp in place of <see cref="TimestampFormat" /> and <see cref="Hour12" />. It's given the
	///     time in <see cref="TimeZone" /> when that is set, else as the entry holds it.
	/// </summary>
	[Parameter]
	public Func<DateTime, string>? TimestampFormatter { get; set; }

	/// <summary>
	///     Whether timestamps are written on a 12-hour clock, as <c>2:30:15.250 PM</c>. Null follows
	///     <see cref="TimestampFormat" />'s own hours, and the 24-hour clock without one. It moves a custom format onto
	///     the clock asked for; a standard one such as <c>T</c> follows the culture.
	/// </summary>
	[Parameter]
	public bool? Hour12 { get; set; }

	/// <summary>
	///     The zone timestamps are written in. A timestamp whose <see cref="DateTime.Kind" /> is <c>Utc</c> or
	///     <c>Local</c> is moved into it, and one whose kind is <c>Unspecified</c> is read as a time there already. Null,
	///     the default, writes every timestamp as the entry holds it. On Blazor Server pass the zone of a scoped
	///     <see cref="TimeProvider" /> set from the browser to write the reader's own times.
	/// </summary>
	[Parameter]
	public TimeZoneInfo? TimeZone { get; set; }

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

	/// <summary>Whether to show the level filter, the buttons or the list <see cref="LevelFilter" /> picks. Default is true.</summary>
	[Parameter]
	public bool ShowLevelFilter { get; set; } = true;

	/// <summary>
	///     Keeps the newest entry in view while the list is scrolled to its newest end, the bottom or, under
	///     <see cref="MokaLogOrder.NewestFirst" />, the top. Scrolling away stops it, and scrolling back to that end starts
	///     it again. It runs in the browser, so scrolling sends nothing to .NET. Default is true.
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
		.AddClass("moka-log-viewer--anchored", KeepsPlace)
		.AddClass(Class)
		.Build();

	private MokaLogViewerText Words => Text ?? MokaLogViewerText.Default;

	private bool Virtualizes => Virtualize || ItemsProvider is not null;

	private bool NewestFirst => Order == MokaLogOrder.NewestFirst;

	// Rows can arrive above the one the reader is on: the newest ones when they are at the top, older ones paged in.
	private bool KeepsPlace => NewestFirst || OnLoadOlder.HasDelegate;

	private bool Thresholds => LevelFilter == MokaLogLevelFilter.AtLeast;

	private bool SubmitsSearch => SearchMode == MokaLogSearchMode.OnSubmit;

	private bool WritesOwnTimes => TimestampFormat is not null || TimestampFormatter is not null || Hour12 is not null || TimeZone is not null;

	// An app can register its own clock. A test does, so no test waits on a real debounce.
	private TimeProvider Clock => _clock ??= Services.GetService<TimeProvider>() ?? TimeProvider.System;

	// One delegate for the life of the viewer, so Virtualize sees the same provider on every render.
	private ItemsProviderDelegate<MokaLogEntry> Provide => _provide ??= ProvideAsync;

	// Under Virtualize the list is a tab stop of its own, so the keyboard can scroll rows that hold no control. Paging
	// can take away the focused button, and focus then goes to the list, which takes it from code alone.
	private string? BodyTabIndex => Virtualizes ? "0" : OnLoadOlder.HasDelegate ? "-1" : null;

	private string? BodyStyle => new StyleBuilder()
		.AddStyle("max-height", MaxHeight)
		.AddStyle("--_log-row-height", $"{VirtualRowHeight.ToString(CultureInfo.InvariantCulture)}px", Virtualizes)
		.Build();

	private string? PaneStyle => new StyleBuilder()
		.AddStyle("max-height", MaxHeight)
		.Build();

	private string PaneId => $"{_idPrefix}-pane";

	private Watch? Wanted => AutoScroll || KeepsPlace ? new Watch(AutoScroll, NewestFirst, Virtualizes ? VirtualRowHeight : 0) : null;

	// The pane's title is spans side by side with no space between them, which a screen reader would run together.
	private string PaneLabel(MokaLogEntry entry) => entry.Source is null
		? $"{WriteTimestamp(entry.Timestamp)} {LevelToLabel(entry.Level)}"
		: $"{WriteTimestamp(entry.Timestamp)} {LevelToLabel(entry.Level)} {entry.Source}";

	/// <summary>
	///     Asks <see cref="ItemsProvider" /> for the rows in view again and draws them. Call it on the renderer's thread
	///     (<c>InvokeAsync</c>) when the log behind the provider changes. Under <see cref="MokaLogOrder.NewestFirst" />
	///     rows it counts beyond the last answer are taken to be new ones at the top, and a reader further down keeps
	///     their place. With <see cref="Entries" /> it does nothing: a new list or a render of the parent is enough.
	/// </summary>
	/// <returns>A task that completes once the rows have been asked for.</returns>
	public Task RefreshDataAsync() => AskProviderAsync(RowsArrive.AtNewestEnd);

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		_maxEntriesGiven = parameters.TryGetValue<int>(nameof(MaxEntries), out _);
		return base.SetParametersAsync(parameters);
	}

	/// <summary>
	///     Has internal filter and search state, so it renders on every change but the keys typed into a search that
	///     waits: Blazor keeps the box's text from the event itself, and the rows don't change until the search runs.
	/// </summary>
	protected override bool ShouldRender()
	{
		if (!_quiet)
		{
			return true;
		}

		_quiet = false;
		return false;
	}

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
		bool minimumChanged = TakeMinimumLevel();
		bool shapeChanged = TakeShape();
		bool filterChanged = searchChanged || levelsChanged || minimumChanged || shapeChanged;
		_askProviderAgain = HasRendered && ItemsProvider is not null && filterChanged;

		if (Virtualizes)
		{
			_open.Clear();
		}
		else
		{
			_paneEntry = null;
		}

		MokaLogEntry? firstBefore = _firstShown;
		Filter();
		if (!filterChanged)
		{
			NoteRowsAbove(firstBefore);
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// A search or levels the parent changed are asked for once the render has put the provider's Virtualize in place.
		if (_askProviderAgain)
		{
			_askProviderAgain = false;
			await AskProviderAsync(RowsArrive.Nowhere);
		}

		if (_loadedOlder)
		{
			_loadedOlder = false;
			if (!HasOlder)
			{
				await SafeFocusAsync(_body);
			}
		}

		Watch? wanted = Wanted;
		if (wanted != _watching)
		{
			_watching = wanted;
			await (wanted is { } watch
				? SafeModuleInvokeVoidAsync(ModulePath, "watch", _body, watch.ToScript())
				: SafeModuleInvokeVoidAsync(ModulePath, "unwatch", _body));
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		DropWaitingSearch();

		if (_watching is not null)
		{
			_watching = null;
			await SafeModuleInvokeVoidAsync(ModulePath, "unwatch", _body);
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

		DropWaitingSearch();
		_searchText = next;
		_typed = next;
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

	// As the search: a parent that passes the same level on every render doesn't undo the reader's pick.
	private bool TakeMinimumLevel()
	{
		if (MinimumLevel == _lastMinimum)
		{
			return false;
		}

		_lastMinimum = MinimumLevel;
		if (MinimumLevel == _minimum)
		{
			return false;
		}

		_minimum = MinimumLevel;
		return true;
	}

	// A new order or kind of level filter shows other rows, and a provider has to be asked for them.
	private bool TakeShape()
	{
		(MokaLogOrder, MokaLogLevelFilter) shape = (Order, LevelFilter);
		bool changed = _lastShape is { } last && last != shape;
		_lastShape = shape;
		return changed;
	}

	private void Filter()
	{
		if (ItemsProvider is not null || Entries is null)
		{
			_shown = [];
			_shownList = [];
			_firstShown = null;
			_open.Clear();
			return;
		}

		bool trimsUnlessTold = Virtualizes || OnLoadOlder.HasDelegate;
		int skip = trimsUnlessTold && !_maxEntriesGiven ? 0 : Math.Clamp(Entries.Count - MaxEntries, 0, Entries.Count);
		bool searching = !string.IsNullOrWhiteSpace(_searchText);
		bool everyLevel = Thresholds ? _minimum == MokaLogLevel.Trace : _levels.Count == AllLevels.Length;

		if (_open.Count > 0)
		{
			_open.IntersectWith(Entries);
		}

		if (skip == 0 && everyLevel && !searching && !NewestFirst && Entries is ICollection<MokaLogEntry> whole)
		{
			_shown = whole;
			_shownList = Entries;
		}
		else
		{
			List<MokaLogEntry> shown = new(Entries.Count - skip);
			for (int step = 0; step < Entries.Count - skip; step++)
			{
				MokaLogEntry entry = Entries[NewestFirst ? Entries.Count - 1 - step : skip + step];
				if (ShowsLevel(entry.Level) && (!searching || Matches(entry)))
				{
					shown.Add(entry);
				}
			}

			_shown = shown;
			_shownList = shown;
		}

		_firstShown = _shownList.Count > 0 ? _shownList[0] : null;
	}

	private bool ShowsLevel(MokaLogLevel level) => Thresholds ? level >= _minimum : _levels.Contains(level);

	private bool Matches(MokaLogEntry entry) =>
		entry.Message.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
		(entry.Source is not null && entry.Source.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

	// The row that was first is found by reference, so the rows before it now are the ones that arrived above it.
	private void NoteRowsAbove(MokaLogEntry? firstBefore)
	{
		if (!KeepsPlace || firstBefore is null)
		{
			return;
		}

		for (int i = 0; i < _shownList.Count; i++)
		{
			if (ReferenceEquals(_shownList[i], firstBefore))
			{
				if (i > 0)
				{
					NoteAbove(i);
				}

				return;
			}
		}
	}

	// Numbered, so the script moves the list once for each arrival however often the same count comes round.
	private void NoteAbove(int count) =>
		_aboveToken = string.Create(CultureInfo.InvariantCulture, $"{++_aboveCount}:{count}");

	private async ValueTask<ItemsProviderResult<MokaLogEntry>> ProvideAsync(ItemsProviderRequest request)
	{
		if (ItemsProvider is not { } provider)
		{
			return new ItemsProviderResult<MokaLogEntry>([], 0);
		}

		ItemsProviderResult<MokaLogEntry> result = await provider(request);
		_providerTotal = result.TotalItemCount;
		return result;
	}

	// Rows a provider counts beyond its last answer are new at the end the change came from; when that end is the top,
	// the reader's place is kept as for Entries. Virtualize draws the new count in the render this ends with.
	private async Task AskProviderAsync(RowsArrive arriving)
	{
		if (ItemsProvider is null || _virtualize is null)
		{
			return;
		}

		int before = _providerTotal;
		await _virtualize.RefreshDataAsync();
		int added = _providerTotal - before;
		bool atTop = arriving == (NewestFirst ? RowsArrive.AtNewestEnd : RowsArrive.AtOldEnd);
		if (KeepsPlace && atTop && before > 0 && added > 0)
		{
			NoteAbove(added);
		}

		StateHasChanged();
	}

	private async Task OnSearchTypedAsync(string? typed)
	{
		_typed = typed ?? string.Empty;
		if (SubmitsSearch)
		{
			_quiet = true;
			return;
		}

		if (SearchDebounce is { } wait && wait > TimeSpan.Zero)
		{
			WaitToSearch(wait);
			_quiet = true;
			return;
		}

		await SearchAsync();
	}

	private Task SubmitSearchAsync() => SearchAsync();

	private async Task SearchAsync()
	{
		DropWaitingSearch();
		_searchText = _typed;
		Filter();
		// The provider reads the bound search, which the parent has taken by the time its callback returns.
		await SearchTextChanged.InvokeAsync(_searchText);
		await AskProviderAsync(RowsArrive.Nowhere);
	}

	private void WaitToSearch(TimeSpan wait)
	{
		int search = DropWaitingSearch();
		_searchTimer = Clock.CreateTimer(_ => RunDetached(() => SearchAfterWaitAsync(search)), null, wait, Timeout.InfiniteTimeSpan);
	}

	// Disposing a timer doesn't stop a callback that already fired and is waiting for the renderer, so each wait carries
	// the number it was started under and only the latest one searches.
	private async Task SearchAfterWaitAsync(int search)
	{
		if (search != _search)
		{
			return;
		}

		await SearchAsync();
		StateHasChanged();
	}

	private int DropWaitingSearch()
	{
		_searchTimer?.Dispose();
		_searchTimer = null;
		return ++_search;
	}

	private async Task ToggleLevelAsync(MokaLogLevel level)
	{
		if (!_levels.Remove(level))
		{
			_levels.Add(level);
		}

		Filter();
		await LevelsChanged.InvokeAsync(new HashSet<MokaLogLevel>(_levels));
		await AskProviderAsync(RowsArrive.Nowhere);
	}

	private async Task PickMinimumAsync(MokaLogLevel level)
	{
		_minimum = level;
		Filter();
		await MinimumLevelChanged.InvokeAsync(level);
		await AskProviderAsync(RowsArrive.Nowhere);
	}

	private async Task LoadOlderAsync()
	{
		if (_loadingOlder)
		{
			return;
		}

		_loadingOlder = true;
		try
		{
			await OnLoadOlder.InvokeAsync();
		}
		finally
		{
			_loadingOlder = false;
		}

		_loadedOlder = true;
		await AskProviderAsync(RowsArrive.AtOldEnd);
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

	// Without a choice of its own the viewer writes what it always has, in the invariant culture. A format of the app's,
	// or the 12-hour clock with its AM and PM, is written in the reader's culture.
	private string WriteTimestamp(DateTime timestamp)
	{
		if (!WritesOwnTimes)
		{
			return timestamp.ToString(DefaultTimestampFormat, CultureInfo.InvariantCulture);
		}

		DateTime clock = Placed(timestamp).Clock;
		if (TimestampFormatter is { } write)
		{
			return write(clock);
		}

		string format = MokaTimeTicks.WithClock(TimestampFormat ?? DefaultTimestampFormat, Hour12);
		CultureInfo culture = TimestampFormat is null && Hour12 is not true ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
		return clock.ToString(format, culture);
	}

	// The moment in full for the time element's datetime: a time with its offset, or with none when the entry gives no
	// zone and the viewer has none to read it in.
	private string MachineTime(DateTime timestamp)
	{
		(DateTime clock, TimeSpan? offset) = Placed(timestamp);
		string time = clock.ToString(MachineFormat, CultureInfo.InvariantCulture);
		return offset is { } shift
			? $"{time}{(shift < TimeSpan.Zero ? '-' : '+')}{shift.Duration().ToString(@"hh\:mm", CultureInfo.InvariantCulture)}"
			: time;
	}

	// TimeZoneInfo's DateTime conversions keep a time near DateTime's ends in range, which DateTimeOffset's don't.
	private (DateTime Clock, TimeSpan? Offset) Placed(DateTime timestamp)
	{
		if (TimeZone is not { } zone)
		{
			return timestamp.Kind switch
			{
				DateTimeKind.Utc => (timestamp, TimeSpan.Zero),
				DateTimeKind.Local => (timestamp, TimeZoneInfo.Local.GetUtcOffset(timestamp)),
				_ => (timestamp, null)
			};
		}

		DateTime clock = timestamp.Kind == DateTimeKind.Unspecified ? timestamp : TimeZoneInfo.ConvertTime(timestamp, zone);
		return (clock, zone.GetUtcOffset(timestamp));
	}

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

	private enum RowsArrive
	{
		Nowhere,
		AtNewestEnd,
		AtOldEnd
	}

	// What the script is asked to do with the list: options for it are a dictionary, since the trim analyzer can't see
	// into an anonymous object.
	private readonly record struct Watch(bool Follow, bool NewestFirst, float RowHeight)
	{
		public Dictionary<string, object?> ToScript() => new()
		{
			["follow"] = Follow,
			["newestFirst"] = NewestFirst,
			["rowHeight"] = RowHeight
		};
	}
}
