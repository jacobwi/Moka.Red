using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Moka.Red.Data.Table;

// The search, the filters and the controls that set them. Typing renders nothing: one debounced wait applies what was
// typed in the search box and the filter row, then loads once.
public partial class MokaTable<TItem>
{
	private static readonly TimeSpan DefaultDebounce = TimeSpan.FromMilliseconds(300);

	private readonly List<MokaTableFilter> _filters = [];
	private MokaFilterJoin _filterJoin;
	private string? _quickFilter;

	private string? _searchTerm;
	private string? _searchBoxStart;
	private int _searchBoxVersion;


	// What the search box and the filter row hold that the debounced wait hasn't applied yet.
	private bool _searchTyped;
	private string? _typedSearch;
	private readonly Dictionary<MokaColumn<TItem>, RowDraft> _rowDrafts = [];
#pragma warning disable CA2213 // Disposed when a newer wait replaces it and in DisposeAsyncCore.
	private ITimer? _typingTimer;
#pragma warning restore CA2213
	private int _typingWait;

	// The filter in force per column when the filter row last started its boxes, which a box shows as its start.
	private Dictionary<string, MokaTableFilter> _filterBoxStart = new(StringComparer.Ordinal);
	private int _filterBoxVersion;
	private MokaTableFilterControls? _previousFilterControls;

	// Values and counts per column title, for the facet panel and the list filters, as the last load found them.
	private Dictionary<string, IReadOnlyList<MokaFacetValue>> _facets = new(StringComparer.Ordinal);
	private bool _facetCountsKnown;
	private Dictionary<string, int>? _quickCounts;
	private readonly Dictionary<MokaColumn<TItem>, ColumnSample> _columnSamples = [];

	/// <summary>
	///     Which filter controls the table draws, combined as needed. Default <see cref="MokaTableFilterControls.Row" />:
	///     a row under the headings with an input that fits each filterable column, drawn once a shown column is
	///     <see cref="MokaColumn{TItem}.Filterable" />. A filter that no control left on screen can show or clear is dropped,
	///     so the rows it hid come back.
	/// </summary>
	[Parameter]
	public MokaTableFilterControls FilterControls { get; set; } = MokaTableFilterControls.Row;

	/// <summary>
	///     Preset filters drawn above the table, with an "All" choice first and how many rows each keeps. The one picked is
	///     part of the <see cref="State" />. Null, the default, draws none.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaTableQuickFilter>? QuickFilters { get; set; }

	/// <summary>How the <see cref="QuickFilters" /> are drawn. Default <see cref="MokaQuickFilterLook.Segments" />.</summary>
	[Parameter]
	public MokaQuickFilterLook QuickFilterLook { get; set; }

	/// <summary>
	///     Asks a <see cref="ServerData" /> source for the values of the list filters and the facets with their counts, and
	///     for the counts of the quick filters, through <see cref="MokaTableRequest.Facets" /> and
	///     <see cref="MokaTableRequest.QuickFilterCounts" />. Default false, where those lists show the values on the rows
	///     loaded, with no counts.
	/// </summary>
	[Parameter]
	public bool FacetsFromServer { get; set; }

	/// <summary>
	///     The text in the search box. Two-way bindable with <c>@bind-Search</c>; it is the state's search, and like the
	///     state a new value seeds it.
	/// </summary>
	[Parameter]
	public string? Search { get; set; }

	/// <summary>Raised when the search changes.</summary>
	[Parameter]
	public EventCallback<string?> SearchChanged { get; set; }

	/// <summary>
	///     How long the table waits after the last key in the search box or a filter box before it loads. Default 300 ms.
	///     Uses a registered <see cref="TimeProvider" />, else the system clock.
	/// </summary>
	[Parameter]
	public TimeSpan Debounce { get; set; } = DefaultDebounce;

	/// <summary>
	///     Marks what the search found in the cells that write their text, in a <c>mark</c> element, with no component per
	///     cell. Cells drawn by a template or a column kind are left as they are. Default false.
	/// </summary>
	[Parameter]
	public bool HighlightSearch { get; set; }

	private TimeProvider Clock => Services.GetService<TimeProvider>() ?? TimeProvider.System;

	private bool HasControl(MokaTableFilterControls control) => (FilterControls & control) != 0;

	// Whether the last render drew a filter input or button for a column.
	private bool _columnFilterControlsDrawn;

	// The row draws while a shown column takes a filter.
	private bool ShowFilterRow(List<(MokaColumn<TItem> Col, int ColIdx)> columns) =>
		HasControl(MokaTableFilterControls.Row) && columns.Exists(static entry => entry.Col is { Filterable: true, Title: not null });

	private bool FiltersActive => _filters.Count > 0 || _quickFilter is not null || !string.IsNullOrWhiteSpace(_searchTerm);

	// ── Search ──

	private void HandleSearchInput(ChangeEventArgs e)
	{
		_typedSearch = e.Value?.ToString();
		_searchTyped = true;
		StartTypingWait();
	}

	private async Task ClearSearch()
	{
		CancelTypingWait(keepRowDrafts: true);
		_searchTyped = false;
		SetSearchTerm(null);
		ResetPaging();
		await LoadDataAsync();
		await NotifyStateChangedAsync();
	}

	/// <summary>Clears the search, the filters and the quick filter, and loads the rows again. Raises <see cref="StateChanged" />.</summary>
	/// <returns>A task that completes once the rows are loaded.</returns>
	public async Task ClearSearchAndFiltersAsync()
	{
		CancelTypingWait(keepRowDrafts: false);
		_searchTyped = false;
		SetSearchTerm(null);
		_filters.Clear();
		_filterJoin = MokaFilterJoin.All;
		_quickFilter = null;
		ResetFilterBoxes();
		ResetPaging();
		await LoadDataAsync();
		StateHasChanged();
		await NotifyStateChangedAsync();
	}

	private void SetSearchTerm(string? term)
	{
		_searchTerm = string.IsNullOrWhiteSpace(term) ? null : term;
		_searchBoxStart = _searchTerm;
		_searchBoxVersion++;
	}


	// ── The debounced wait ──

	// One wait for every box: a newer key replaces the wait, and the wait's number tells a callback already on its way
	// that it is no longer the latest.
	private void StartTypingWait()
	{
		_typingTimer?.Dispose();
		int wait = ++_typingWait;
		_typingTimer = Clock.CreateTimer(_ => RunDetached(() => FinishTypingAsync(wait)), null, Debounce, Timeout.InfiniteTimeSpan);
	}

	private void CancelTypingWait(bool keepRowDrafts)
	{
		_typingTimer?.Dispose();
		_typingTimer = null;
		_typingWait++;
		if (!keepRowDrafts)
		{
			_rowDrafts.Clear();
		}
	}

	private async Task FinishTypingAsync(int wait)
	{
		if (wait != _typingWait)
		{
			return;
		}

		_typingTimer?.Dispose();
		_typingTimer = null;
		if (_searchTyped)
		{
			_searchTyped = false;
			_searchTerm = string.IsNullOrWhiteSpace(_typedSearch) ? null : _typedSearch;
			_searchBoxStart = _searchTerm;
		}

		foreach ((MokaColumn<TItem> col, RowDraft draft) in _rowDrafts)
		{
			ReplaceColumnFilter(col, draft.ToFilter(col.Title!, FilterKindOf(col)));
		}

		_rowDrafts.Clear();
		ResetPaging();
		await LoadDataAsync();
		StateHasChanged();
		await NotifyStateChangedAsync();
	}

	// ── The filter row ──

	private void HandleRowText(MokaColumn<TItem> col, ChangeEventArgs e)
	{
		_rowDrafts[col] = DraftFor(col) with { First = e.Value?.ToString() };
		StartTypingWait();
	}

	private void HandleRowRangeEnd(MokaColumn<TItem> col, ChangeEventArgs e)
	{
		_rowDrafts[col] = DraftFor(col) with { Second = e.Value?.ToString() };
		StartTypingWait();
	}

	// A list or yes and no is a single pick: it applies at once. The list is bound, so a pick the parent's state refuses
	// goes back on screen.
	private async Task SetRowChoiceAsync(MokaColumn<TItem> col, string? value)
	{
		MokaTableFilter? filter = string.IsNullOrEmpty(value) ? null : new MokaTableFilter(col.Title!, MokaFilterOperator.Is, value);
		await SetColumnFilterAsync(col, filter, resetBoxes: false);
	}

	// What a list of several shows in the filter row: the values picked, or All.
	private string RowChoicesText(MokaColumn<TItem> col)
	{
		if (FilterOn(col) is not { } filter)
		{
			return Words.All;
		}

		if (filter.Operator != MokaFilterOperator.IsAnyOf)
		{
			return FilterSummary(filter);
		}

		IReadOnlyList<MokaFacetValue> choices = ChoicesFor(col);
		return string.Join(", ", filter.Values.Select(value => ShownValue(value, choices)));
	}

	private static readonly string[] FocusChips = [".moka-table-chip-remove", ".moka-table-chips button", ".moka-table-sort-button"];

	private bool ShowChips => HasControl(MokaTableFilterControls.Chips) && _filters.Count > 0;

	private bool ShowFacets => HasControl(MokaTableFilterControls.Facets) && FacetColumns().Count > 0;

	private const string AllQuickKey = "\u0001all";

	private Task PickQuickSegmentAsync(string? value) => PickQuickFilterAsync(value == AllQuickKey ? null : value);

	private List<Moka.Red.Navigation.Tabs.MokaTabItem> QuickTabs(IReadOnlyList<MokaTableQuickFilter> quickFilters)
	{
		List<Moka.Red.Navigation.Tabs.MokaTabItem> tabs = [new(AllQuickKey, Words.AllRows, AllRowsCount)];
		tabs.AddRange(quickFilters.Select(quick => new Moka.Red.Navigation.Tabs.MokaTabItem(quick.Name, quick.Name, QuickCountOf(quick.Name))));
		return tabs;
	}

	// ── The builder ──

	private readonly string _builderId = $"moka-table-builder-{Guid.NewGuid():N}";
	private bool _builderOpen;

	private void ToggleBuilder() => _builderOpen = !_builderOpen;

	internal void CloseBuilder()
	{
		_builderOpen = false;
		_focusAfterRender = [".moka-table-builder-toggle"];
		StateHasChanged();
	}

	internal IReadOnlyList<MokaTableFilter> Filters => _filters;

	internal MokaFilterJoin FilterJoin => _filterJoin;

	internal List<MokaColumn<TItem>> FilterableColumns() =>
		_columns.Where(static col => col is { Filterable: true, Title: not null }).ToList();

	private RowDraft DraftFor(MokaColumn<TItem> col)
	{
		if (_rowDrafts.TryGetValue(col, out RowDraft draft))
		{
			return draft;
		}

		MokaTableFilter? current = col.Title is null ? null : _filterBoxStart.GetValueOrDefault(col.Title);
		return RowDraft.From(current);
	}

	// What a box of the filter row starts with: the filter in force on its column when the boxes last started.
	private string? RowBoxStart(MokaColumn<TItem> col, bool second = false)
	{
		MokaTableFilter? current = col.Title is null ? null : _filterBoxStart.GetValueOrDefault(col.Title);
		RowDraft draft = RowDraft.From(current);
		return second ? draft.Second : draft.First;
	}

	private string RowChoiceStart(MokaColumn<TItem> col) =>
		col.Title is not null && _filterBoxStart.GetValueOrDefault(col.Title) is { Operator: MokaFilterOperator.Is, Value: { } value }
			? value
			: string.Empty;

	// The search, filter and view name boxes are never given the typed text back: a render after a slow load would write
	// an older value over keys still on their way. Their listeners don't render (NonRendering), and @bind, which tells
	// Blazor what the browser holds, would hand them a new listener on every render of the table. So a box shows what it
	// started with, and a change from outside (Clear, a menu, a chip, a renamed column, a state) rebuilds it through @key.
	private void ResetFilterBoxes()
	{
		_filterBoxStart = new Dictionary<string, MokaTableFilter>(StringComparer.Ordinal);
		foreach (MokaTableFilter filter in _filters)
		{
			_filterBoxStart.TryAdd(filter.Column, filter);
		}

		_filterBoxVersion++;
	}

	// ── Setting filters ──

	private void ReplaceColumnFilter(MokaColumn<TItem> col, MokaTableFilter? filter)
	{
		if (col.Title is not { } title)
		{
			return;
		}

		_filters.RemoveAll(existing => existing.Column == title);
		if (filter is not null)
		{
			_filters.Add(filter);
		}
	}

	/// <summary>Puts one filter on a column in place of the ones it had, or clears it with null, and loads the rows.</summary>
	internal async Task SetColumnFilterAsync(MokaColumn<TItem> col, MokaTableFilter? filter, bool resetBoxes = true)
	{
		_rowDrafts.Remove(col);
		ReplaceColumnFilter(col, filter);
		if (resetBoxes)
		{
			ResetFilterBoxes();
		}
		else if (col.Title is { } title)
		{
			if (filter is null)
			{
				_filterBoxStart.Remove(title);
			}
			else
			{
				_filterBoxStart[title] = filter;
			}
		}

		ResetPaging();
		await LoadDataAsync();
		StateHasChanged();
		await NotifyStateChangedAsync();
	}

	/// <summary>Puts every filter and the join in place of the ones in force, and loads the rows.</summary>
	internal async Task SetFiltersAsync(IReadOnlyList<MokaTableFilter> filters, MokaFilterJoin join)
	{
		if (join == _filterJoin && filters.SequenceEqual(_filters))
		{
			return;
		}

		_rowDrafts.Clear();
		_filters.Clear();
		_filters.AddRange(filters);
		_filterJoin = join;
		ResetFilterBoxes();
		ResetPaging();
		await LoadDataAsync();
		StateHasChanged();
		await NotifyStateChangedAsync();
	}

	private async Task RemoveFilterAsync(MokaTableFilter filter)
	{
		if (!_filters.Remove(filter))
		{
			return;
		}

		ResetFilterBoxes();
		ResetPaging();
		_focusAfterRender = FocusChips;
		await LoadDataAsync();
		await NotifyStateChangedAsync();
	}

	private async Task ClearFiltersAsync()
	{
		_rowDrafts.Clear();
		_filters.Clear();
		_quickFilter = null;
		ResetFilterBoxes();
		ResetPaging();
		await LoadDataAsync();
		await NotifyStateChangedAsync();
	}

	private async Task PickQuickFilterAsync(string? name)
	{
		string? picked = string.IsNullOrEmpty(name) || QuickFilterNamed(name) is null ? null : name;
		if (picked == _quickFilter)
		{
			return;
		}

		_quickFilter = picked;
		ResetPaging();
		await LoadDataAsync();
		StateHasChanged();
		await NotifyStateChangedAsync();
	}

	private MokaTableQuickFilter? QuickFilterNamed(string? name) =>
		name is null ? null : QuickFilters?.FirstOrDefault(quick => string.Equals(quick.Name, name, StringComparison.Ordinal));

	private MokaTableQuickFilter? ActiveQuickFilter => QuickFilterNamed(_quickFilter);

	// ── What a column's filter takes ──

	internal TableFilterKind FilterKindOf(MokaColumn<TItem> col)
	{
		MokaColumnFilterType type = col.FilterType != MokaColumnFilterType.Auto ? col.FilterType : col.AutoFilterType;
		return type switch
		{
			MokaColumnFilterType.Text => TableFilterKind.Text,
			MokaColumnFilterType.Number => TableFilterKind.Number,
			MokaColumnFilterType.Date => TableFilterKind.Date,
			MokaColumnFilterType.Select => TableFilterKind.Select,
			MokaColumnFilterType.MultiSelect => TableFilterKind.MultiSelect,
			MokaColumnFilterType.YesNo => TableFilterKind.YesNo,
			_ => col.FilterOptions is not null ? TableFilterKind.Select : InferredKind(col)
		};
	}

	// The first value the rows have says what the column holds.
	private TableFilterKind InferredKind(MokaColumn<TItem> col) =>
		SampleOf(col).First is { } value ? KindOfValue(value) : TableFilterKind.Text;

	// A column's first value among the rows, and whether a row leaves it empty, for the filter it gets and the type its
	// editor writes. Kept until the rows are loaded again.
	private ColumnSample SampleOf(MokaColumn<TItem> col)
	{
		if (_columnSamples.TryGetValue(col, out ColumnSample sample))
		{
			return sample;
		}

		object? first = null;
		bool hasEmpty = false;
		if (col.HasValue)
		{
			foreach (TItem item in (IEnumerable<TItem>?)Items ?? _displayItems)
			{
				object? value = col.ValueOf(item);
				first ??= value;
				hasEmpty |= value is null;
				if (first is not null && hasEmpty)
				{
					break;
				}
			}
		}

		sample = new ColumnSample(first, hasEmpty);
		_columnSamples[col] = sample;
		return sample;
	}

	// The type a Field column's editor writes. A boxed value has lost its declared type, int? boxes as int and null as
	// nothing, so it comes from the values: the row's own, else the first among the rows, made nullable when the row or any
	// other leaves the column empty. A column no row gives a value takes the type its Editor edits, else text.
	internal Type FieldValueType(MokaColumn<TItem> col, TItem item)
	{
		object? own = col.ValueOf(item);
		ColumnSample sample = SampleOf(col);
		Type? type = (own ?? sample.First)?.GetType() ?? TableValues.TypeEditedBy(col.Editor);
		if (type is null)
		{
			return typeof(string);
		}

		return own is null || sample.HasEmpty ? TableValues.CanBeEmpty(type) : type;
	}

	private readonly record struct ColumnSample(object? First, bool HasEmpty);

	private static TableFilterKind KindOfValue(object value) => value switch
	{
		bool => TableFilterKind.YesNo,
		DateTime or DateTimeOffset or DateOnly => TableFilterKind.Date,
		Enum => TableFilterKind.Select,
		string => TableFilterKind.Text,
		_ when MokaTablePipeline.AsNumber(value) is not null => TableFilterKind.Number,
		_ => TableFilterKind.Text
	};

	internal static bool IsChoiceKind(TableFilterKind kind) =>
		kind is TableFilterKind.Select or TableFilterKind.MultiSelect or TableFilterKind.YesNo;

	/// <summary>The operators a column's filter offers, in the menu and the builder.</summary>
	internal static IReadOnlyList<MokaFilterOperator> OperatorsFor(TableFilterKind kind) => kind switch
	{
		TableFilterKind.Number or TableFilterKind.Date =>
		[
			MokaFilterOperator.Is, MokaFilterOperator.IsNot, MokaFilterOperator.GreaterThan, MokaFilterOperator.AtLeast,
			MokaFilterOperator.LessThan, MokaFilterOperator.AtMost, MokaFilterOperator.Between, MokaFilterOperator.IsEmpty,
			MokaFilterOperator.IsNotEmpty
		],
		TableFilterKind.Select or TableFilterKind.MultiSelect =>
			[MokaFilterOperator.IsAnyOf, MokaFilterOperator.IsNoneOf, MokaFilterOperator.IsEmpty, MokaFilterOperator.IsNotEmpty],
		TableFilterKind.YesNo => [MokaFilterOperator.Is],
		_ =>
		[
			MokaFilterOperator.Contains, MokaFilterOperator.DoesNotContain, MokaFilterOperator.Is, MokaFilterOperator.IsNot,
			MokaFilterOperator.StartsWith, MokaFilterOperator.EndsWith, MokaFilterOperator.IsEmpty, MokaFilterOperator.IsNotEmpty
		]
	};

	// How many values an operator takes: none for the empty tests, two for between, any for the lists, else one.
	internal static int ValueCountOf(MokaFilterOperator op) => op switch
	{
		MokaFilterOperator.IsEmpty or MokaFilterOperator.IsNotEmpty => 0,
		MokaFilterOperator.Between => 2,
		MokaFilterOperator.IsAnyOf or MokaFilterOperator.IsNoneOf => -1,
		_ => 1
	};

	// ── Choices and counts ──

	/// <summary>The columns a facet panel lists: the filterable list and yes or no columns that are shown.</summary>
	internal List<MokaColumn<TItem>> FacetColumns() =>
		_columns.Where(col => col is { Filterable: true, Title: not null } && IsColumnVisible(col) && IsChoiceKind(FilterKindOf(col)))
			.ToList();

	// The columns whose values the table counts on a load: the facets, and the list filters of the row, the menus and the
	// builder. A table with none counts nothing.
	private List<MokaColumn<TItem>> CountedColumns()
	{
		bool facets = HasControl(MokaTableFilterControls.Facets);
		bool choices = (FilterControls & (MokaTableFilterControls.Row | MokaTableFilterControls.Menus | MokaTableFilterControls.Builder |
		                                  MokaTableFilterControls.Chips)) != 0;
		if (!facets && !choices)
		{
			return [];
		}

		return _columns
			.Where(col => col is { Filterable: true, Title: not null, HasValue: true } && IsChoiceKind(FilterKindOf(col)))
			.ToList();
	}

	/// <summary>
	///     A list filter's choices: the column's <see cref="MokaColumn{TItem}.FilterOptions" /> with the counts the last load
	///     found, else the values the last load found. Yes and no are always the two choices.
	/// </summary>
	internal IReadOnlyList<MokaFacetValue> ChoicesFor(MokaColumn<TItem> col)
	{
		IReadOnlyList<MokaFacetValue> found = col.Title is not null && _facets.TryGetValue(col.Title, out IReadOnlyList<MokaFacetValue>? values)
			? values
			: [];

		if (FilterKindOf(col) == TableFilterKind.YesNo)
		{
			return
			[
				new MokaFacetValue("true", Words.Yes, CountOf(found, "true")),
				new MokaFacetValue("false", Words.No, CountOf(found, "false"))
			];
		}

		if (col.FilterOptions is { } options)
		{
			return options.Select(option => new MokaFacetValue(option.Value, option.Text, CountOf(found, option.Value))).ToList();
		}

		return found;
	}

	private static int CountOf(IReadOnlyList<MokaFacetValue> found, string value)
	{
		foreach (MokaFacetValue facet in found)
		{
			if (string.Equals(facet.Value, value, StringComparison.OrdinalIgnoreCase))
			{
				return facet.Count;
			}
		}

		return 0;
	}

	internal bool FacetCountsKnown => _facetCountsKnown;

	// Changes whenever the values are counted again, after a load, so the facet panel draws only when they can have changed.
	internal int FacetsVersion { get; private set; }

	internal bool IsFacetPicked(MokaColumn<TItem> col, string value) =>
		FilterOn(col) is { Operator: MokaFilterOperator.IsAnyOf or MokaFilterOperator.Is } filter
		&& filter.Values.Contains(value, StringComparer.OrdinalIgnoreCase);

	// A facet's boxes are one filter on the column: the values ticked, any of which a row may have.
	internal Task PickFacetAsync(MokaColumn<TItem> col, string value, bool on)
	{
		List<string> picked = FilterOn(col) is { Operator: MokaFilterOperator.IsAnyOf or MokaFilterOperator.Is } filter
			? filter.Values.ToList()
			: [];
		picked.RemoveAll(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase));
		if (on)
		{
			picked.Add(value);
		}

		return SetColumnFilterAsync(col, picked.Count == 0 ? null : new MokaTableFilter(col.Title!, MokaFilterOperator.IsAnyOf, picked));
	}

	internal int? QuickCountOf(string name) => _quickCounts is { } counts && counts.TryGetValue(name, out int count) ? count : null;

	internal int? AllRowsCount => _quickCounts is null ? null : _quickAllCount;

	private int _quickAllCount;

	// Counts over rows in memory, each column with every filter but its own, and each quick filter with the search and
	// the filters but no other quick filter.
	private void CountClientValues(List<TItem> searched, List<TItem> filtered)
	{
		List<MokaColumn<TItem>> counted = CountedColumns();
		Dictionary<string, IReadOnlyList<MokaFacetValue>> facets = new(StringComparer.Ordinal);
		IReadOnlyList<MokaTableFilter> quick = ActiveQuickFilter?.Filters ?? [];
		foreach (MokaColumn<TItem> col in counted)
		{
			List<MokaTableFilter> others = _filters.Where(filter => filter.Column != col.Title).ToList();
			IEnumerable<TItem> rows = others.Count == _filters.Count ? filtered : MokaTablePipeline.Filter(searched, Fields, others, _filterJoin);
			if (quick.Count > 0)
			{
				rows = MokaTablePipeline.Filter(rows, Fields, quick);
			}

			facets[col.Title!] = MokaTablePipeline.Facet(rows, col);
		}

		_facets = facets;
		_facetCountsKnown = true;
		FacetsVersion++;

		if (QuickFilters is not { Count: > 0 } quickFilters)
		{
			_quickCounts = null;
			return;
		}

		Dictionary<string, int> counts = new(StringComparer.Ordinal);
		foreach (MokaTableQuickFilter quickFilter in quickFilters)
		{
			counts[quickFilter.Name] = MokaTablePipeline.Filter(filtered, Fields, quickFilter.Filters).Count();
		}

		_quickAllCount = filtered.Count;
		_quickCounts = counts;
	}

	// A server sends what it was asked for; without that, the lists show the values on the rows it sent, uncounted. The
	// version moves once the values are in, not when the request goes out, or the panel would draw the last load's.
	private void TakeServerValues(MokaTableResult<TItem> result)
	{
		FacetsVersion++;
		if (FacetsFromServer && result.Facets is { } facets)
		{
			_facets = new Dictionary<string, IReadOnlyList<MokaFacetValue>>(facets, StringComparer.Ordinal);
			_facetCountsKnown = true;
		}
		else
		{
			Dictionary<string, IReadOnlyList<MokaFacetValue>> found = new(StringComparer.Ordinal);
			foreach (MokaColumn<TItem> col in CountedColumns())
			{
				found[col.Title!] = MokaTablePipeline.Facet(result.Items, col);
			}

			_facets = found;
			_facetCountsKnown = false;
		}

		if (FacetsFromServer && result.QuickFilterCounts is { } quickCounts)
		{
			_quickCounts = new Dictionary<string, int>(quickCounts, StringComparer.Ordinal);
			_quickAllCount = result.TotalItems ?? result.Items.Count;
		}
		else
		{
			_quickCounts = null;
		}
	}

	// ── Which filters keep a control ──

	// A filter some control can show or clear stays. Chips and the builder show every filter; the row and the menus show
	// the filters of shown, filterable columns; the facets those of their columns. With none of these, filters come from
	// the state, which is their control.
	private bool DropFiltersWithoutControl()
	{
		if (_filters.Count == 0 || (FilterControls & (MokaTableFilterControls.Chips | MokaTableFilterControls.Builder)) != 0)
		{
			return false;
		}

		// The row and the menus are controls only while a shown column takes a filter, or did when the table last drew:
		// a filter whose input just went away goes with it, and filters from the state on a table whose row never drew stay.
		bool byColumn = (FilterControls & (MokaTableFilterControls.Row | MokaTableFilterControls.Menus)) != 0
		                && (_columnFilterControlsDrawn
		                    || _columns.Exists(col => col is { Filterable: true, Title: not null } && IsColumnVisible(col)));
		bool facets = HasControl(MokaTableFilterControls.Facets);
		if (!byColumn && !facets)
		{
			return false;
		}

		int removed = _filters.RemoveAll(filter => !_columns.Exists(col =>
			col.Title == filter.Column && col.Filterable && IsColumnVisible(col)
			&& (byColumn || IsChoiceKind(FilterKindOf(col)))));
		if (removed > 0)
		{
			ResetFilterBoxes();
		}

		return removed > 0;
	}

	// The controls changing can leave filters nothing shows; they go, and the rows they hid come back. Taking every
	// control away takes the filters they set with them.
	// The quick filters the counts were made for. A new list, or one whose filters changed, has none of its own until the
	// rows are counted again, so it loads them again rather than showing no counts until something else does.
	private IReadOnlyList<MokaTableQuickFilter>? _countedQuickFilters;
	private string? _countedQuickSignature;

	private void SyncQuickFilters()
	{
		if (ReferenceEquals(QuickFilters, _countedQuickFilters))
		{
			return;
		}

		_countedQuickFilters = QuickFilters;
		string? signature = QuickFilters is null ? null : string.Join('\u001E', QuickFilters.Select(static quick =>
			quick.Name + '\u001F' + string.Join('\u001F', quick.Filters.Select(static filter =>
				filter.Column + '\u001D' + filter.Operator + '\u001D' + string.Join('\u001D', filter.Values)))));
		if (signature == _countedQuickSignature)
		{
			return;
		}

		bool first = _countedQuickSignature is null && !_hasLoaded;
		_countedQuickSignature = signature;
		if (!first)
		{
			_parameterReloadPending = true;
		}
	}

	private void SyncFilterControls()
	{
		if (FilterControls == _previousFilterControls)
		{
			return;
		}

		MokaTableFilterControls? before = _previousFilterControls;
		_previousFilterControls = FilterControls;
		if (before is null)
		{
			return;
		}

		bool dropped = FilterControls == MokaTableFilterControls.None && before != MokaTableFilterControls.None && _filters.Count > 0;
		if (dropped)
		{
			_filters.Clear();
			ResetFilterBoxes();
		}

		if (dropped || DropFiltersWithoutControl())
		{
			_rowDrafts.Clear();
			_currentPage = 1;
			_parameterReloadPending = true;
		}
	}

	// ── Words ──

	private string FilterSummary(MokaTableFilter filter)
	{
		MokaColumn<TItem>? col = _columns.Find(c => c.Title == filter.Column);
		TableFilterKind kind = col is null ? TableFilterKind.Text : FilterKindOf(col);
		IReadOnlyList<MokaFacetValue> choices = col is not null && IsChoiceKind(kind) ? ChoicesFor(col) : [];
		List<string> shown = filter.Values.Select(value => ShownValue(value, choices)).ToList();
		return Words.FilterSummary(filter.Column, Words.OperatorName(filter.Operator, kind == TableFilterKind.Date), shown);
	}

	private static string ShownValue(string value, IReadOnlyList<MokaFacetValue> choices)
	{
		foreach (MokaFacetValue choice in choices)
		{
			if (string.Equals(choice.Value, value, StringComparison.OrdinalIgnoreCase))
			{
				return choice.Text;
			}
		}

		return value;
	}

	internal static string CountText(int count) => count.ToString("N0", CultureInfo.CurrentCulture);

	/// <summary>What one column's box or boxes in the filter row hold, until the debounced wait applies it.</summary>
	/// <param name="First">The text, or a range's start.</param>
	/// <param name="Second">A range's end.</param>
	private readonly record struct RowDraft(string? First, string? Second)
	{
		public static RowDraft From(MokaTableFilter? filter) => filter switch
		{
			null => default,
			{ Operator: MokaFilterOperator.Between } when filter.Values.Count >= 2 => new RowDraft(filter.Values[0], filter.Values[1]),
			{ Operator: MokaFilterOperator.AtLeast } => new RowDraft(filter.Value, null),
			{ Operator: MokaFilterOperator.AtMost } => new RowDraft(null, filter.Value),
			_ => new RowDraft(filter.Value, null)
		};

		// A range with both ends is between them, with one end at least or at most it. Text is held anywhere in the cell.
		public MokaTableFilter? ToFilter(string column, TableFilterKind kind)
		{
			string? first = string.IsNullOrWhiteSpace(First) ? null : First.Trim();
			string? second = string.IsNullOrWhiteSpace(Second) ? null : Second.Trim();
			if (kind is TableFilterKind.Number or TableFilterKind.Date)
			{
				return (first, second) switch
				{
					({ } from, { } to) => new MokaTableFilter(column, MokaFilterOperator.Between, [from, to]),
					({ } from, null) => new MokaTableFilter(column, MokaFilterOperator.AtLeast, from),
					(null, { } to) => new MokaTableFilter(column, MokaFilterOperator.AtMost, to),
					_ => null
				};
			}

			return first is null ? null : new MokaTableFilter(column, MokaFilterOperator.Contains, first);
		}
	}
}

/// <summary>What a column's filter controls take, once <see cref="MokaColumnFilterType.Auto" /> is worked out.</summary>
internal enum TableFilterKind
{
	Text,
	Number,
	Date,
	Select,
	MultiSelect,
	YesNo
}
