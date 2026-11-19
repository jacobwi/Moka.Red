using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Utilities;
using Moka.Red.Data.Pagination;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Data.Table;

// Paging: the pager part, its seven looks, the page and page size, load more, endless scroll and cursors.
public partial class MokaTable<TItem>
{
	private MokaTablePager? _pager;
	private MokaPagerLook _pagerLook;
	private int _currentPage = 1;
	private int _pageSize = 10;
	private int? _previousPageSize;

	// The page size the parent set. The pager stays while the rows need more than one page at this size, so someone
	// who picked a size that fits every row can still pick a smaller one.
	private int _parentPageSize = 10;

	// How many rows load more and endless scroll draw, once someone asked for more. int.MaxValue is every row.
	private int? _shownRows;

	// The cursor pager's place: the cursor asked for, and the ones the last answer handed out.
	private string? _cursor;
	private string? _nextCursor;
	private string? _previousCursor;

	// Endless scroll: the row count the end of the rows was last watched at, -1 while it isn't watched.
	private int _endlessArmedAt = -1;
	private ElementReference _endlessRef;

	/// <summary>
	///     Rows per page when the state doesn't say. Default 10. Does not limit the rows while
	///     <see cref="ShowPagination" /> is false: the table then shows every row. The page size someone picks is part of
	///     the <see cref="State" />.
	/// </summary>
	[Parameter]
	public int PageSize { get; set; } = 10;

	/// <summary>
	///     Whether the table pages its rows and shows the pager. Default true. When false every row
	///     renders: <see cref="Items" /> is not cut to <see cref="PageSize" />, and
	///     <see cref="ServerData" /> is asked for page 1 with a page size that covers the whole result
	///     set. Pair it with <see cref="Virtualize" /> for long lists. A <see cref="MokaTablePager" /> among the columns
	///     says how the pager looks.
	/// </summary>
	[Parameter]
	public bool ShowPagination { get; set; } = true;

	private PagerSettings Pager => _pager?.Settings ?? PagerSettings.Default;

	private MokaPagerLook PagerLook => Pager.Look;

	private bool IsShowMoreLook => PagerLook is MokaPagerLook.LoadMore or MokaPagerLook.Endless;

	private bool IsCursorLook => PagerLook == MokaPagerLook.Cursor && ServerData is not null;

	private MokaPaginationText PagerWords => Words.Pager;

	// A source that doesn't count has as many pages as the one shown, and one more while a full page came back.
	private int PageCount => _totalKnown
		? MokaTablePipeline.PageCount(_pageTotal, _pageSize)
		: _currentPage + (_hasMore ? 1 : 0);

	// What the pager is told: null while the count is whole, else whether there is more.
	private bool? PagerHasMore => _totalKnown ? null : _hasMore;

	private int PageStartIndex => (_currentPage - 1) * Math.Max(_pageSize, 0);

	// A page size that holds the whole result set, as far as the last load reported it.
	private int FullSetPageSize => Math.Max(_totalItems, Math.Max(_pageSize, 1));

	// One rule for every numbered pager. Hidden while everything fits on one page, but kept while the user is past
	// page 1, and while the rows would need more than one page at the parent's page size: someone who picked a size
	// that fits every row needs the size menu to pick a smaller one again.
	private bool ShowPaginationBar => ShowPagination
	                                  && !RemoteRows
	                                  && !IsShowMoreLook
	                                  && (IsCursorLook
		                                  ? _nextCursor is not null || _previousCursor is not null
		                                  : _pageTotal > _pageSize || _currentPage > 1 || _pageTotal > _parentPageSize || _hasMore);

	// The entries pager has a part above the rows and a part below, wherever the position says.
	private bool ShowTopPagination => ShowPaginationBar
	                                  && (PagerLook == MokaPagerLook.Entries
	                                      || Pager.Position is MokaTablePaginationPosition.Top or MokaTablePaginationPosition.Both);

	private bool ShowBottomPagination => ShowPaginationBar
	                                     && (PagerLook == MokaPagerLook.Entries
	                                         || Pager.Position is MokaTablePaginationPosition.Bottom or MokaTablePaginationPosition.Both);

	private int PageFirstRow => _pageTotal == 0 ? 0 : PageStartIndex + 1;

	private int PageLastRow => Math.Min(PageStartIndex + _pageSize, _pageTotal);

	private string EntriesText => _hasMore
		? PagerWords.ShowingEntriesOpen(PageFirstRow, PageLastRow)
		: PagerWords.ShowingEntries(PageFirstRow, PageLastRow, _pageTotal);

	private string JumpPagesText => _totalKnown ? PagerWords.OfPages(PageCount) : PagerWords.OfMorePages;

	private string PagerClass(bool top) => new CssBuilder("moka-table-pager")
		.AddClass(PagerLook switch
		{
			MokaPagerLook.Entries => "moka-table-pager--entries",
			MokaPagerLook.Jump => "moka-table-pager--jump",
			MokaPagerLook.Cursor => "moka-table-pager--cursor",
			_ => null
		})
		.AddClass("moka-table-pager--top", top)
		.AddClass("moka-table-pager--sticky", Pager.Sticky)
		.Build();

	// A pager under the rows names its buttons above itself, over the rows; one above the rows below itself.
	private static MokaTooltipPosition PagerTooltip(bool top) => top ? MokaTooltipPosition.Bottom : MokaTooltipPosition.Top;

	private bool CanGoNewer => IsCursorLook ? _previousCursor is not null : _currentPage > 1;

	private bool CanGoOlder => IsCursorLook ? _nextCursor is not null : _currentPage < PageCount;

	private void SyncPageSizeFromParameters()
	{
		if (PageSize == _previousPageSize)
		{
			return;
		}

		_previousPageSize = PageSize;
		_parentPageSize = PageSize;
		if (PageSize != _pageSize)
		{
			_pageSize = Math.Max(PageSize, 1);
			_shownRows = null;
			_parameterReloadPending = true;
		}
	}

	// A new sort, search or filter starts from the first page.
	private void ResetPaging()
	{
		_currentPage = 1;
		_cursor = null;
	}

	private bool ClampCurrentPage()
	{
		int clamped = MokaTablePipeline.ClampPage(_currentPage, _pageTotal, _pageSize);
		if (clamped == _currentPage)
		{
			return false;
		}

		_currentPage = clamped;
		return true;
	}

	void IMokaTablePartHost.AddPager(MokaTablePager pager)
	{
		_pager = pager;
		PagerLookChanged();
		_parts.Render();
	}

	void IMokaTablePartHost.RemovePager(MokaTablePager pager)
	{
		if (!ReferenceEquals(_pager, pager))
		{
			return;
		}

		_pager = null;
		PagerLookChanged();
		_parts.Render();
	}

	void IMokaTablePartHost.OnPagerChanged(MokaTablePager pager)
	{
		PagerLookChanged();
		_parts.Render();
	}

	// Every paged look starts from the same first page, so the look the pager part names when it arrives needs no new
	// load. A table that loaded every row with the pager hidden does need one for load more and endless scroll, which
	// win over it, and a change between looks after someone paged goes back to the start.
	private void PagerLookChanged()
	{
		if (PagerLook == _pagerLook)
		{
			return;
		}

		bool showMoreBefore = _pagerLook is MokaPagerLook.LoadMore or MokaPagerLook.Endless;
		_pagerLook = PagerLook;
		bool samePage = _currentPage == 1 && _shownRows is null && _cursor is null;
		if (samePage && (ShowPagination || showMoreBefore == IsShowMoreLook))
		{
			return;
		}

		_currentPage = 1;
		_shownRows = null;
		_cursor = null;
		if (_hasLoaded)
		{
			_columnReloadPending = true;
		}
	}

	private async Task GoToPageAsync(int page)
	{
		int target = Math.Clamp(page, 1, PageCount);

		// The pager reports page 1 right after a new page size, which has loaded page 1 already.
		if (target == _currentPage)
		{
			return;
		}

		_currentPage = target;
		await LoadDataAsync();
		await NotifyStateChangedAsync();
	}

	private Task HandlePageChange(int page) => GoToPageAsync(page);

	private async Task HandlePageSizeChange(int pageSize)
	{
		_pageSize = Math.Max(pageSize, 1);
		_currentPage = 1;
		_shownRows = null;
		_cursor = null;
		await LoadDataAsync();
		await NotifyStateChangedAsync();
	}

	private Task HandlePageSizePick(ChangeEventArgs e) =>
		int.TryParse(e.Value?.ToString(), out int size) && size > 0 ? HandlePageSizeChange(size) : Task.CompletedTask;

	private Task PreviousPageAsync() => GoToPageAsync(_currentPage - 1);

	private Task NextPageAsync() => GoToPageAsync(_currentPage + 1);

	// A page typed in the jump box goes there, inside the pages there are. Anything else puts the box back, through
	// @bind, which tells Blazor what the box holds.
	private async Task JumpToPageAsync(int page)
	{
		if (Math.Clamp(page, 1, PageCount) != _currentPage)
		{
			await GoToPageAsync(page);
		}
	}

	private async Task NewerAsync()
	{
		if (!CanGoNewer)
		{
			return;
		}

		if (!IsCursorLook)
		{
			await GoToPageAsync(_currentPage - 1);
			return;
		}

		_cursor = _previousCursor;
		await LoadDataAsync();
	}

	private async Task OlderAsync()
	{
		if (!CanGoOlder)
		{
			return;
		}

		if (!IsCursorLook)
		{
			await GoToPageAsync(_currentPage + 1);
			return;
		}

		_cursor = _nextCursor;
		await LoadDataAsync();
	}

	// ── Load more and endless ──

	private int ShownRowCount => _shownRows ?? Math.Max(_pageSize, 1);

	private bool ShowMoreFoot => IsShowMoreLook && !Loading && !_isLoading && !FailureReplacesRows && _shownUnits < _pageTotal;

	private string ShowMoreCountText => Words.ShowingCount(_shownUnits, _pageTotal, ItemsName);

	private string ShowMoreButtonText => Words.ShowMoreRows(Math.Min(Math.Max(_pageSize, 1), _pageTotal - _shownUnits));

	private string ShowAllButtonText => Words.ShowAll(_pageTotal, ItemsName);

	/// <summary>
	///     Loads the rows again keeping those on screen, like <see cref="RefreshAsync" />, and under the load more and
	///     endless pagers draws at least <paramref name="rows" /> of them. For a page that adds rows because someone asked
	///     for more, such as older log entries added to the same list, so the person sees what they asked for. A list
	///     changed in place is read again too, which the table can't notice by itself.
	/// </summary>
	/// <param name="rows">How many rows to draw at least.</param>
	/// <returns>A task that completes once the rows are loaded.</returns>
	public async Task ShowAtLeastAsync(int rows)
	{
		if (IsShowMoreLook && rows > ShownRowCount)
		{
			_shownRows = rows;
		}

		await LoadDataAsync(keepRows: _displayRows.Count > 0);
		StateHasChanged();
	}

	private async Task ShowMoreRowsAsync()
	{
		int firstNew = _displayRows.Count;
		_shownRows = _shownUnits + Math.Max(_pageSize, 1);
		await LoadDataAsync(keepRows: true);
		FocusFirstNewRow(firstNew);
	}

	private async Task ShowAllRowsAsync()
	{
		int firstNew = _displayRows.Count;
		_shownRows = int.MaxValue;
		await LoadDataAsync(keepRows: true);
		FocusFirstNewRow(firstNew);
	}

	// The buttons go away with the last rows, and focus would fall to the page.
	private void FocusFirstNewRow(int firstNew)
	{
		if (_shownUnits >= _pageTotal && firstNew < _displayRows.Count)
		{
			_focusedCell = (firstNew, _firstTabColumn);
			_cellFocusPending = true;
		}
	}

	/// <summary>
	///     Called from JS once the end of the rows scrolls into view under the endless pager. The script stops watching
	///     until the table watches again, after rows arrive.
	/// </summary>
	[JSInvokable]
	public void OnEndReached() => RunDetached(async () =>
	{
		if (PagerLook != MokaPagerLook.Endless || !ShowMoreFoot)
		{
			return;
		}

		_shownRows = _shownUnits + Math.Max(_pageSize, 1);
		await LoadDataAsync(keepRows: true);
		StateHasChanged();
	});

	// One shared observer in the script watches the end of the rows. It is watched again only when rows arrived, so a
	// load that adds nothing can't ask over and over; the button under the rows still loads more.
	private async Task SyncEndlessAsync()
	{
		bool wanted = PagerLook == MokaPagerLook.Endless && ShowMoreFoot;
		if (wanted && _shownUnits > _endlessArmedAt)
		{
			_endlessArmedAt = _shownUnits;
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await InvokeTableJsAsync("observeEnd", _endlessRef, _dotNetRef);
		}
		else if (!wanted && _endlessArmedAt >= 0)
		{
			_endlessArmedAt = -1;
			await InvokeTableJsAsync("unobserveEnd", _wrapperRef);
		}
	}
}
