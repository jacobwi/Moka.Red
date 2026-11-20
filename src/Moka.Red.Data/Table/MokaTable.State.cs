using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Base;
using Moka.Red.Core.Theming;

namespace Moka.Red.Data.Table;

// The one state record: what the table is showing, built from its fields, put back from a record, raised and kept.
public partial class MokaTable<TItem>
{
	private const string StorePrefix = "moka-table:";

	private readonly CancellationTokenSource _storageCancellation = new();
	private readonly List<string> _groups = [];
	private readonly HashSet<string> _collapsedGroups = new(StringComparer.Ordinal);

	// Row expand tracking. By the key's invariant text under an ItemKey, so the state can name the rows; by the item
	// itself otherwise, where struct items compare by value instead of by a freshly boxed object on every call.
	private readonly HashSet<string> _expandedKeys = new(StringComparer.Ordinal);
	private readonly HashSet<TItem> _expandedItems = new(EqualityComparer<TItem>.Default);

	private MokaTableDensity _density;
	private MokaTableDensity? _lastDensityParameter;
	private bool _densityFromParameter = true;
	private string? _lastGroupByParameter;
	private bool _groupsFromParameter = true;
	private IReadOnlyCollection<string>? _lastInitiallyCollapsed;
	private int _lastRaisedPage = 1;
	private bool _restorePending;

	// The last state, page and search the parent passed, so only a new one seeds the table, and the search last raised.
	private MokaTableState? _lastStateParameter;
	private int? _lastPageParameter;
	private string? _lastSearchParameter;
	private string? _lastRaisedSearch;

	// Whether the parent passed the page and the search in this parameter set: a page left at its default must not undo
	// the page a state names.
	private bool _pageGiven;
	private bool _searchGiven;

	// The state as far as anyone was told: the last one the parent passed or the table raised. A bound parent handing it
	// back is no news. A state the table puts in force itself (a reset, a view, a stored state) is news, so only the
	// parent's paths set it.
	private MokaTableState? _stateInForce;

	/// <summary>
	///     What the table shows: its sort, filters, search, page, page size, density, columns, grouping and open rows.
	///     Two-way bindable with <c>@bind-State</c>. The table applies a state only when the parent passes one that says
	///     something new, compared by content, so an inline <c>new MokaTableState { ... }</c> built on every render
	///     doesn't undo what someone changed: the state seeds the table, and <see cref="StateChanged" /> reports what
	///     changed. A property left null keeps the table's default for it.
	/// </summary>
	[Parameter]
	public MokaTableState? State { get; set; }

	/// <summary>Raised with the whole state whenever someone changes any part of it.</summary>
	[Parameter]
	public EventCallback<MokaTableState> StateChanged { get; set; }

	/// <summary>
	///     Keeps the state from visit to visit under this key: in the browser's local storage, or in the registered
	///     <see cref="IMokaTableStateStore" />. The kept state is put back once the table is interactive, over whatever it
	///     started with, and raised through <see cref="StateChanged" />. Null, the default, keeps nothing.
	/// </summary>
	[Parameter]
	public string? StorageKey { get; set; }

	/// <summary>
	///     The page shown, from 1. Two-way bindable with <c>@bind-Page</c>; it is the state's page, and like the state a new
	///     value seeds it.
	/// </summary>
	[Parameter]
	public int Page { get; set; } = 1;

	/// <summary>Raised when the page changes.</summary>
	[Parameter]
	public EventCallback<int> PageChanged { get; set; }

	/// <summary>
	///     How tightly the rows are spaced when the state doesn't say. Null, the default, takes the theme's
	///     <c>TableDensity</c> (<c>MokaTheme.WithTableDensity</c>), else the look's own:
	///     <see cref="MokaTableDensity.Comfortable" /> for <see cref="MokaTableLook.Airy" />, else
	///     <see cref="MokaTableDensity.Compact" />, the table as it has always looked.
	/// </summary>
	[Parameter]
	public MokaTableDensity? Density { get; set; }

	/// <summary>
	///     The title of the column the rows are grouped by when the state doesn't say. Rows showing the same text in that
	///     column (or the same <see cref="MokaColumn{TItem}.GroupKey" />) are shown together under a heading, in the order
	///     each first appears, so the sort still decides the order. The column can be hidden. Null, the default, leaves the
	///     rows ungrouped. Rows in memory don't combine grouping and <see cref="Virtualize" />: grouping wins, because a
	///     virtualized list of rows has no room for the headings between them. A <see cref="ServerData" /> source with
	///     <see cref="Virtualize" /> windows the headings and rows together when it sends every group's totals.
	/// </summary>
	[Parameter]
	public string? GroupBy { get; set; }

	/// <summary>
	///     Content of a group's heading row, in place of the key and the count. The whole heading row is still
	///     the table's, so the toggle and the spanning cell stay.
	/// </summary>
	[Parameter]
	public RenderFragment<MokaTableGroup<TItem>>? GroupHeaderTemplate { get; set; }

	/// <summary>Whether a group's heading collapses it. Default true.</summary>
	[Parameter]
	public bool GroupsCollapsible { get; set; } = true;

	/// <summary>
	///     Groups that start collapsed, by key. The user's own collapsing is kept apart from this, so a new value
	///     here does not undo what they folded away; it is the starting state.
	/// </summary>
	[Parameter]
	public IReadOnlyCollection<string>? InitiallyCollapsedGroups { get; set; }

	/// <summary>
	///     Raised when a group is collapsed or expanded, with the group's id and the new state. A group of the first level's
	///     id is its key; a nested group's is its keys joined by <see cref="MokaTableGroup{TItem}.IdSeparator" />.
	/// </summary>
	[Parameter]
	public EventCallback<(string Key, bool Collapsed)> OnGroupToggled { get; set; }

	private MokaTableDensity DefaultDensity =>
		Density
		?? (OnNewDesign ? CascadedTheme?.TableDensity : null)
		?? (ResolvedLook == MokaTableLook.Airy ? MokaTableDensity.Comfortable : MokaTableDensity.Compact);

	/// <summary>The state the table shows now, with every property filled in.</summary>
	/// <returns>A new state record.</returns>
	public MokaTableState CurrentState() => BuildState();

	/// <summary>
	///     Puts the table back as its parameters set it: the columns' default sorts, no filters or search, page 1, the
	///     default page size, density, columns and grouping, nothing opened. Raises <see cref="StateChanged" />.
	/// </summary>
	/// <returns>A task that completes once the rows are loaded.</returns>
	public async Task ResetStateAsync()
	{
		ApplyState(DefaultState());
		await LoadDataAsync();
		StateHasChanged();
		await NotifyStateChangedAsync();
	}

	/// <summary>The state the table starts with and resets to.</summary>
	private MokaTableState DefaultState() => new()
	{
		Sorts = DefaultSorts(),
		PageSize = PageSize,
		Density = DefaultDensity,
		Groups = GroupBy is null ? [] : [GroupBy],
		CollapsedGroups = InitiallyCollapsedGroups?.ToList() ?? []
	};

	// A state with the parts it leaves to the table filled in, so it compares with one the table built.
	private MokaTableState Resolve(MokaTableState state) => state with
	{
		Sorts = state.Sorts ?? DefaultSorts(),
		PageSize = state.PageSize ?? PageSize,
		Density = state.Density ?? DefaultDensity,
		Groups = state.Groups ?? (GroupBy is null ? [] : [GroupBy])
	};

	private List<MokaTableSort> DefaultSorts() =>
		_columns
			.Where(static col => col is { DefaultSort: not MokaSortDirection.None, Title: not null })
			.Select(static col => new MokaTableSort(col.Title!, col.DefaultSort))
			.ToList();

	private MokaTableState BuildState() => new()
	{
		Sorts = _sorts.ToList(),
		Filters = _filters.ToList(),
		FilterJoin = _filterJoin,
		QuickFilter = _quickFilter,
		Search = string.IsNullOrWhiteSpace(_searchTerm) ? null : _searchTerm,
		Page = _currentPage,
		PageSize = _pageSize,
		Density = _density,
		Columns = BuildColumnStates(),
		Groups = _groups.ToList(),
		CollapsedGroups = _collapsedGroups.Order(StringComparer.Ordinal).ToList(),
		Expanded = _expandedKeys.Order(StringComparer.Ordinal).ToList()
	};

	private void ApplyState(MokaTableState state)
	{
		_sorts.Clear();
		_sortsFromDefault = state.Sorts is null;
		_sorts.AddRange(state.Sorts ?? DefaultSorts());

		_filters.Clear();
		_filters.AddRange(state.Filters);
		_filterJoin = state.FilterJoin;
		_quickFilter = state.QuickFilter;
		_rowDrafts.Clear();
		ResetFilterBoxes();

		SetSearchTerm(state.Search);

		_pageSize = Math.Max(state.PageSize ?? PageSize, 1);
		_currentPage = Math.Max(state.Page, 1);
		_shownRows = null;
		_cursor = null;

		_densityFromParameter = state.Density is null;
		_density = state.Density ?? DefaultDensity;

		_groupsFromParameter = state.Groups is null;
		_groups.Clear();
		_groups.AddRange(state.Groups ?? (GroupBy is null ? [] : [GroupBy]));

		_collapsedGroups.Clear();
		_collapsedGroups.UnionWith(state.CollapsedGroups);

		_expandedKeys.Clear();
		_expandedKeys.UnionWith(state.Expanded);
		_expandedItems.Clear();
		_expandVersion++;
		ForgetDetailPlaces();

		_arrangementInForce = state.Columns;
		ApplyArrangement(state.Columns);
	}

	// Whether the state or a quick filter names a column by this title, so its arrival changes the rows or the counts.
	private bool StateNames(string? title) =>
		title is not null
		&& (_sorts.Exists(sort => sort.Column == title)
		    || _filters.Exists(filter => filter.Column == title)
		    || _groups.Contains(title)
		    || (QuickFilters?.Any(quick => quick.Filters.Any(filter => filter.Column == title)) ?? false));

	// A parameter only seeds its part of the state when the parent passes a new value, so an unbound parent re-render
	// cannot revert what someone did. A new value matching what is in force is a bound parent handing back what the
	// table reported: nothing to apply, nothing to load again.
	private void SyncStateFromParameters()
	{
		if (!Equals(State, _lastStateParameter))
		{
			_lastStateParameter = State;
			if (State is not null && !Resolve(State).Equals(_stateInForce))
			{
				ApplyState(State);
				_stateInForce = Resolve(State);
				_lastRaisedSearch = _searchTerm;
				_parameterReloadPending = true;
			}
		}

		if (_pageGiven && Page != _lastPageParameter)
		{
			_lastPageParameter = Page;
			if (Page != _currentPage && Page >= 1)
			{
				_currentPage = Page;
				_parameterReloadPending = true;
			}
		}

		if (_searchGiven && Search != _lastSearchParameter)
		{
			_lastSearchParameter = Search;
			string? wanted = string.IsNullOrWhiteSpace(Search) ? null : Search;
			if (wanted != _searchTerm)
			{
				SetSearchTerm(wanted);
				_lastRaisedSearch = wanted;
				_currentPage = 1;
				_parameterReloadPending = true;
			}
		}
	}

	private void SyncDensityFromParameters()
	{
		MokaTableDensity wanted = DefaultDensity;
		if (wanted == _lastDensityParameter)
		{
			return;
		}

		_lastDensityParameter = wanted;
		if (_densityFromParameter)
		{
			_density = wanted;
		}
	}

	private void SyncGroupByFromParameters()
	{
		if (GroupBy == _lastGroupByParameter)
		{
			return;
		}

		_lastGroupByParameter = GroupBy;
		if (!_groupsFromParameter)
		{
			return;
		}

		_groups.Clear();
		if (GroupBy is not null)
		{
			_groups.Add(GroupBy);
		}

		_parameterReloadPending = true;
	}

	// The starting state is applied once per value the parent passes, so the user's own collapsing is not
	// undone by an unrelated re-render (gotcha 9).
	private void SyncInitiallyCollapsed()
	{
		if (ReferenceEquals(_lastInitiallyCollapsed, InitiallyCollapsedGroups))
		{
			return;
		}

		_lastInitiallyCollapsed = InitiallyCollapsedGroups;
		_collapsedGroups.Clear();
		if (InitiallyCollapsedGroups is not null)
		{
			_collapsedGroups.UnionWith(InitiallyCollapsedGroups);
		}
	}

	// Raises the state after someone changed it, and keeps it under the storage key. Built only when someone can hear
	// it: a table nobody binds, keeps or saves views of builds nothing.
	private async Task NotifyStateChangedAsync()
	{
		bool pageHeard = PageChanged.HasDelegate && _currentPage != _lastRaisedPage;
		string? search = string.IsNullOrWhiteSpace(_searchTerm) ? null : _searchTerm;
		if (SearchChanged.HasDelegate && search != _lastRaisedSearch)
		{
			_lastRaisedSearch = search;
			await SearchChanged.InvokeAsync(search);
		}

		if (!StateChanged.HasDelegate && StorageKey is null && !pageHeard)
		{
			return;
		}

		MokaTableState state = BuildState();
		if (state.Equals(_stateInForce))
		{
			return;
		}

		_stateInForce = state;
		if (pageHeard)
		{
			_lastRaisedPage = _currentPage;
			await PageChanged.InvokeAsync(_currentPage);
		}

		await StateChanged.InvokeAsync(state);
		if (StorageKey is { } key)
		{
			await SaveStateAsync(key, state);
		}
	}

	private IMokaTableStateStore? Store => Services.GetService<IMokaTableStateStore>();

	private async Task RestoreStoredStateAsync()
	{
		_restorePending = false;
		MokaTableState? stored = null;
		if (StorageKey is { } key)
		{
			stored = Store is { } store
				? await store.LoadAsync(key, _storageCancellation.Token)
				: MokaTableState.FromJson(await SafeModuleInvokeAsync<string?>(ModulePath, "readState", StorePrefix + key));
		}

		if (stored is not null)
		{
			ApplyState(stored);
		}

		await LoadDataAsync();
		StateHasChanged();
		if (stored is not null)
		{
			await NotifyStateChangedAsync();
		}
	}

	private async Task SaveStateAsync(string key, MokaTableState state)
	{
		if (Store is { } store)
		{
			await store.SaveAsync(key, state, _storageCancellation.Token);
			return;
		}

		await InvokeTableJsAsync("writeState", StorePrefix + key, state.ToJson());
	}

	// ── Expanded rows ──

	private static string KeyText(object key) => Convert.ToString(key, CultureInfo.InvariantCulture) ?? string.Empty;

	private bool IsExpanded(TItem item) =>
		ItemKey is not null ? _expandedKeys.Contains(KeyText(ItemKey(item))) : _expandedItems.Contains(item);

	private void ToggleExpand(TItem item)
	{
		_expandVersion++;
		bool wasOpen = IsExpanded(item);
		if (SingleExpand)
		{
			_expandedKeys.Clear();
			_expandedItems.Clear();
			ForgetDetailPlaces();
		}

		if (ItemKey is not null)
		{
			string key = KeyText(ItemKey(item));
			if (wasOpen)
			{
				_expandedKeys.Remove(key);
			}
			else
			{
				_expandedKeys.Add(key);
			}
		}
		else if (wasOpen)
		{
			_expandedItems.Remove(item);
		}
		else
		{
			_expandedItems.Add(item);
		}

		RunDetached(NotifyStateChangedAsync);
	}

	/// <summary>Opens the detail of every row shown. Raises <see cref="StateChanged" />.</summary>
	/// <returns>A task that completes once the table has drawn the change.</returns>
	public async Task ExpandAllAsync()
	{
		_expandVersion++;
		foreach (MokaTableRow<TItem> row in _displayRows)
		{
			if (ItemKey is not null)
			{
				_expandedKeys.Add(KeyText(ItemKey(row.Item)));
			}
			else
			{
				_expandedItems.Add(row.Item);
			}
		}

		StateHasChanged();
		await NotifyStateChangedAsync();
	}

	/// <summary>Closes the detail of every row. Raises <see cref="StateChanged" />.</summary>
	/// <returns>A task that completes once the table has drawn the change.</returns>
	public async Task CollapseAllAsync()
	{
		_expandVersion++;
		_expandedKeys.Clear();
		_expandedItems.Clear();
		ForgetDetailPlaces();
		StateHasChanged();
		await NotifyStateChangedAsync();
	}

	private bool DrawExpandAll => ShowExpandAll && !SingleExpand;

	private bool AllShownExpanded => _displayRows.Count > 0 && _displayRows.TrueForAll(row => IsExpanded(row.Item));

	private Task ToggleExpandAllAsync() => AllShownExpanded ? CollapseAllAsync() : ExpandAllAsync();

	private string ExpandAllLabel => AllShownExpanded ? Words.CollapseAllRows : Words.ExpandAllRows;

	// ── Groups ──

	private bool IsGroupCollapsed(string id) => _collapsedGroups.Contains(id);
}
