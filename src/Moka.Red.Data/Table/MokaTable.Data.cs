namespace Moka.Red.Data.Table;

// Where the rows come from: Items through the pipeline, or a server-side source, and what is drawn of them.
public partial class MokaTable<TItem>
{
	private IReadOnlyList<TItem> _displayItems = [];
	private List<MokaTableRow<TItem>> _displayRows = [];

	// Full client-side result set after search, filters, the quick filter and sort, in group order while grouped. Empty
	// in server mode.
	private List<TItem> _filteredItems = [];

	// Position of the first displayed row in the whole result set, so row reordering can report
	// absolute indexes for the rows that are actually on screen.
	private int _firstRowIndex;
	private bool _isLoading;
#pragma warning disable CA2213 // Each load owns and disposes its own. This only points at the one running.
	private CancellationTokenSource? _loadCancellation;
#pragma warning restore CA2213
	private int _totalItems;

	// What the pager counts: rows, or under grouped rows in memory the lines, headings included. The units shown of them.
	private int _pageTotal;
	private int _shownUnits;

	// Whether the source counted its rows. Without a count the table knows only whether a full page came back.
	private bool _totalKnown = true;
	private bool _hasMore;

	// Totals over the whole result set a server sent, by column title.
	private IReadOnlyDictionary<string, double>? _serverTotals;

	// The first load from a server waits for the columns, so their default sorts reach the first request.
	private bool _firstLoadPending;

	// Aggregates and distinct filter values can only see loaded rows. In server mode that is the
	// current page, or every row while the pager is hidden; in client mode it is the full filtered set.
	private IReadOnlyList<TItem> LoadedItems => ServerData is not null ? _displayItems : _filteredItems;

	private IReadOnlyList<ITableField<TItem>> Fields => _columns;

	private async Task LoadDataAsync(bool keepRows = false)
	{
		_ = _loadCancellation?.CancelAsync();
		_loadCancellation = null;
		_firstLoadPending = false;
		_columnSamples.Clear();

		_keepRowsWhileLoading = keepRows;
		_isLoading = !keepRows;
		try
		{
			if (RemoteRows)
			{
				await ReloadRemoteRowsAsync();
			}
			else if (ServerData is not null)
			{
				await LoadServerDataAsync(ServerData);
			}
			else if (Items is not null || _streamedItems is not null)
			{
				// A broken stream's failure stays until Retry or another stream.
				if (_stream is null)
				{
					_loadError = null;
				}

				LoadClientData();
			}
			else
			{
				_loadError = null;
				_filteredItems = [];
				_totalItems = 0;
				_pageTotal = 0;
				_currentPage = 1;
				_groupTree = [];
				_displayLines = [];
				SetDisplayItems([], 0);
			}
		}
		finally
		{
			// A newer load may have started while this one waited, and it's still going. A stream loads until its first
			// rows arrive.
			_isLoading = (_loadCancellation is not null && !_keepRowsWhileLoading) || (_streaming && _streamedItems is { Count: 0 });
		}
	}

	// Answers can land in any order, and a slow old page used to overwrite a newer one, so each load checks it's
	// still the latest after every wait. What a replaced request does after that, failing included, no longer matters.
	// A failure of the latest one shows in the table with a way to try again.
	private async Task LoadServerDataAsync(Func<MokaTableRequest, Task<MokaTableResult<TItem>>> source)
	{
		_filteredItems = [];

		using CancellationTokenSource cancellation = new();
		_loadCancellation = cancellation;
		try
		{
			if (IsShowMoreLook && _shownRows != int.MaxValue)
			{
				_currentPage = 1;
				MokaTableResult<TItem> shown = await source(BuildRequest(1, ShownRowCount, cancellation.Token));
				if (IsLatestLoad(cancellation))
				{
					TakeServerResult(shown, 0, counted: true);
				}

				return;
			}

			// With the pager hidden no other page can be reached, so the table asks for every row.
			if (!ShowPagination || IsShowMoreLook)
			{
				_currentPage = 1;
				MokaTableResult<TItem> all = await FetchAllServerRowsAsync(source, cancellation);
				if (IsLatestLoad(cancellation))
				{
					TakeServerResult(all, 0, counted: true);
				}

				return;
			}

			if (IsCursorLook)
			{
				_currentPage = 1;
				MokaTableResult<TItem> slice = await source(BuildRequest(1, _pageSize, cancellation.Token) with { Cursor = _cursor });
				if (IsLatestLoad(cancellation))
				{
					_nextCursor = slice.NextCursor;
					_previousCursor = slice.PreviousCursor;
					TakeServerResult(slice, 0, counted: true);
				}

				return;
			}

			MokaTableResult<TItem> result = await source(BuildRequest(_currentPage, _pageSize, cancellation.Token));
			if (!IsLatestLoad(cancellation))
			{
				return;
			}

			// The first page asked for without a cursor is the cursor pager's first page too, so a cursor pager that
			// arrives once the rows are in has its cursors.
			_nextCursor = result.NextCursor;
			_previousCursor = result.PreviousCursor;

			// The requested page can be past the end after a filter narrows the result set, or past the last page of a
			// source that doesn't count, which then comes back empty.
			// An empty page past the end of a source that doesn't count says where the end is.
			int? endFound = null;
			if (PageIsPastTheEnd(result))
			{
				if (result.TotalItems is { } total)
				{
					_currentPage = MokaTablePipeline.ClampPage(_currentPage, total, _pageSize);
				}
				else
				{
					endFound = PageStartIndex;
					_currentPage = Math.Max(1, _currentPage - 1);
				}

				result = await source(BuildRequest(_currentPage, _pageSize, cancellation.Token));
				if (!IsLatestLoad(cancellation))
				{
					return;
				}
			}

			TakeServerResult(result, PageStartIndex, counted: false);
			if (endFound is { } end && result.TotalItems is null)
			{
				_totalKnown = true;
				_hasMore = false;
				_totalItems = _pageTotal = Math.Max(end, PageStartIndex + result.Items.Count);
			}
		}
#pragma warning disable CA1031 // A replaced load's outcome no longer matters; the latest one's failure is shown, with Retry.
		catch (Exception) when (!IsLatestLoad(cancellation))
		{
		}
		catch (Exception ex)
#pragma warning restore CA1031
		{
			_loadCancellation = null;
			ShowLoadFailure(ex);
			await OnLoadFailed.InvokeAsync(ex);
		}
		finally
		{
			if (IsLatestLoad(cancellation))
			{
				_loadCancellation = null;
			}
		}
	}

	private bool PageIsPastTheEnd(MokaTableResult<TItem> result) =>
		_currentPage > 1 && (result.TotalItems is { } total
			? MokaTablePipeline.ClampPage(_currentPage, total, _pageSize) != _currentPage
			: result.Items.Count == 0);

	// What a server sent. A paged answer without a count says only whether a full page came back, which is whether the
	// pager offers another.
	private void TakeServerResult(MokaTableResult<TItem> result, int firstRowIndex, bool counted)
	{
		_loadError = null;

		// A short page tells the total too: there is nothing after it.
		_totalKnown = counted || result.TotalItems is not null || result.Items.Count < _pageSize;
		_hasMore = !_totalKnown;
		_totalItems = result.TotalItems ?? firstRowIndex + result.Items.Count;
		_pageTotal = _totalItems;
		_serverTotals = result.Totals;
		_serverGroupTotals = result.GroupTotals;
		TakeServerValues(result);
		SetDisplayItems(result.Items, firstRowIndex);
		_shownUnits = _displayRows.Count;
		GroupServerRows();
	}

	private bool IsLatestLoad(CancellationTokenSource cancellation) => ReferenceEquals(_loadCancellation, cancellation);

	// Page 1, sized from the last TotalItems the source reported. There is none before the first
	// load and it is out of date once the result set grows, so an answer reporting more rows than
	// were asked for gets one follow-up request sized to the new total. A source that caps its page
	// size below that still comes back short, and the table shows what it got.
	private async Task<MokaTableResult<TItem>> FetchAllServerRowsAsync(
		Func<MokaTableRequest, Task<MokaTableResult<TItem>>> source, CancellationTokenSource cancellation)
	{
		int pageSize = FullSetPageSize;
		MokaTableResult<TItem> result = await source(BuildRequest(1, pageSize, cancellation.Token));
		if (IsLatestLoad(cancellation) && result.TotalItems is { } total && total > pageSize && result.Items.Count < total)
		{
			result = await source(BuildRequest(1, total, cancellation.Token));
		}

		return result;
	}

	private void LoadClientData()
	{
		_totalKnown = true;
		_hasMore = false;
		_serverTotals = null;
		_serverGroupTotals = null;
		_filteredItems = BuildFilteredItems();
		_totalItems = _filteredItems.Count;
		BuildTree(_filteredItems);
		if (IsGrouped)
		{
			_filteredItems = MokaTablePipeline.InGroupOrder(_groupTree);
		}

		PageClientRows();
	}

	// The page of rows in memory, or under grouping the page of lines. A table that hides its pager shows every row.
	private void PageClientRows()
	{
		if (IsGrouped)
		{
			List<FullLine> lines = FlattenTree();
			_pageTotal = lines.Count;
			if (IsShowMoreLook)
			{
				_currentPage = 1;
				ShowLines(lines, 0, ShownRowCount);
			}
			else if (!ShowPagination)
			{
				_currentPage = 1;
				ShowLines(lines, 0, lines.Count);
			}
			else
			{
				ClampCurrentPage();
				ShowLines(lines, PageStartIndex, _pageSize);
			}

			return;
		}

		_displayLines = [];
		_pageTotal = _totalItems;
		if (IsShowMoreLook)
		{
			_currentPage = 1;
			SetDisplayItems(_filteredItems.Take(ShownRowCount).ToList(), 0);
		}
		else if (!ShowPagination)
		{
			_currentPage = 1;
			SetDisplayItems(_filteredItems, 0);
		}
		else
		{
			ClampCurrentPage();
			SetDisplayItems(MokaTablePipeline.Page(_filteredItems, _currentPage, _pageSize), PageStartIndex);
		}

		_shownUnits = _displayRows.Count;
	}

	private List<TItem> BuildFilteredItems()
	{
		IEnumerable<TItem> items = ClientItems;
		MokaTableQuickFilter? quick = ActiveQuickFilter;
		bool counting = CountedColumns().Count > 0 || QuickFilters is { Count: > 0 };
		if (counting)
		{
			List<TItem> searched = MokaTablePipeline.Search(items, Fields, _searchTerm).ToList();
			List<TItem> filtered = _filters.Count > 0 ? MokaTablePipeline.Filter(searched, Fields, _filters, _filterJoin).ToList() : searched;
			CountClientValues(searched, filtered);
			items = filtered;
		}
		else
		{
			_facets = new Dictionary<string, IReadOnlyList<MokaFacetValue>>(StringComparer.Ordinal);
			_quickCounts = null;
			FacetsVersion++;
			items = MokaTablePipeline.Search(items, Fields, _searchTerm);
			if (_filters.Count > 0)
			{
				items = MokaTablePipeline.Filter(items, Fields, _filters, _filterJoin);
			}
		}

		if (quick is not null)
		{
			items = MokaTablePipeline.Filter(items, Fields, quick.Filters);
		}

		if (_sorts.Count > 0)
		{
			items = MokaTablePipeline.Sort(items, Fields, _sorts);
		}

		return items.ToList();
	}

	// The lists are copied because the table changes its own in place, which would rewrite a request the source kept.
	private MokaTableRequest BuildRequest(int page, int pageSize, CancellationToken cancellationToken = default) => new()
	{
		CancellationToken = cancellationToken,
		Page = page,
		PageSize = pageSize,
		StartIndex = (Math.Max(page, 1) - 1) * Math.Max(pageSize, 0),
		Search = string.IsNullOrWhiteSpace(_searchTerm) ? null : _searchTerm,
		Sorts = _sorts.ToList(),
		Filters = _filters.ToList(),
		FilterJoin = _filterJoin,
		QuickFilter = ActiveQuickFilter,
		Groups = GroupColumns().Select(static col => col.Title!).ToList(),
		Aggregates = _columns
			.Where(static col => col is { Aggregate: not MokaAggregateType.None, Title: not null })
			.Select(static col => new MokaTableAggregate(col.Title!, col.Aggregate))
			.ToList(),
		Facets = FacetsFromServer ? CountedColumns().Select(static col => col.Title!).ToList() : [],
		QuickFilterCounts = FacetsFromServer && QuickFilters is { } quickFilters ? quickFilters.ToList() : []
	};

	private void SetDisplayItems(IReadOnlyList<TItem> items, int firstRowIndex)
	{
		CarrySelection(items);
		_displayItems = items;
		_firstRowIndex = firstRowIndex;
		_rowIndexBase = 0;
		_displayRows = BuildRows(items);
		_indeterminateState = null;
		ClampFocusedCell();
	}

	// Blazor throws when two sibling rows carry the same @key, and a key that several rows share
	// cannot tell them apart anyway. Those rows lose the key and Blazor matches them by position, as
	// it would without ItemKey; mixing keyed and unkeyed siblings is supported. Unique keys stay, so
	// those rows are still moved rather than rebuilt. The set uses the default equality, which is
	// what Blazor compares keys with.
	private List<MokaTableRow<TItem>> BuildRows(IReadOnlyList<TItem> items)
	{
		var keys = new object?[items.Count];
		var seen = new HashSet<object>();
		HashSet<object>? repeated = null;
		for (int i = 0; i < items.Count; i++)
		{
			object? key = RowKey(items[i]);
			keys[i] = key;
			if (key is not null && !seen.Add(key))
			{
				repeated ??= [];
				repeated.Add(key);
			}
		}

		var rows = new List<MokaTableRow<TItem>>(items.Count);
		for (int i = 0; i < items.Count; i++)
		{
			object? key = keys[i];
			if (key is not null && repeated is not null && repeated.Contains(key))
			{
				key = null;
			}

			rows.Add(new MokaTableRow<TItem>(items[i], i, key));
		}

		return rows;
	}

	private object? RowKey(TItem item) => ItemKey?.Invoke(item) ?? item;

	// Detail rows are siblings of the data rows, so their key is wrapped in a type that no ItemKey
	// can return. An unkeyed row's detail row is unkeyed too: its key would be just as ambiguous.
	private static DetailRowIdentity? DetailRowKey(MokaTableRow<TItem> row) =>
		row.Key is null ? null : new DetailRowIdentity(row.Key);
}
