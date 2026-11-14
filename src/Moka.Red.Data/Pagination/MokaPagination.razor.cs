using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Data.Pagination;

/// <summary>
///     Standalone pagination component. Usable independently of MokaTable.
///     Provides page navigation, page size selection, and page info display.
/// </summary>
public partial class MokaPagination : MokaComponentBase
{
	private int _cachedCurrentPage;
	private int _cachedTotalPages;
	private readonly MokaInputValue<int> _pageSize;
	private readonly MokaInputValue<int> _page;
	private List<int> _visiblePages = [];
	private bool _cachedOpen;

	/// <summary>Creates the pager.</summary>
	public MokaPagination()
	{
		_pageSize = new MokaInputValue<int>(ShowParentValue, DispatchExceptionAsync, 10);
		_page = new MokaInputValue<int>(ShowParentValue, DispatchExceptionAsync, 1);
	}

	// The page and the page size are the parent's when it passes them and listens: a page it refuses, such as one past
	// the end of a source that doesn't count, goes back on screen once its handler has run.
	private int CurrentPageShown => _page.Shown;

	/// <summary>Total number of items across all pages, or the items known so far while <see cref="HasMore" /> is set.</summary>
	[Parameter]
	public int TotalItems { get; set; }

	/// <summary>
	///     For a source that doesn't count its items: whether there is a page after the last one counted. Null, the
	///     default, means <see cref="TotalItems" /> is the whole count. Set, the pager offers the next page while it is
	///     true, draws no last page and says "21-30 of more" instead of a total.
	/// </summary>
	[Parameter]
	public bool? HasMore { get; set; }

	/// <summary>Items per page. Default 10. Two-way bindable.</summary>
	[Parameter]
	public int PageSize { get; set; } = 10;

	/// <summary>Callback when page size changes.</summary>
	[Parameter]
	public EventCallback<int> PageSizeChanged { get; set; }

	/// <summary>Current page number (1-indexed). Default 1. Two-way bindable.</summary>
	[Parameter]
	public int CurrentPage { get; set; } = 1;

	/// <summary>Callback when current page changes.</summary>
	[Parameter]
	public EventCallback<int> CurrentPageChanged { get; set; }

	/// <summary>Dropdown options for page size. Default [10, 25, 50, 100].</summary>
	[Parameter]
	public IReadOnlyList<int> PageSizeOptions { get; set; } = [10, 25, 50, 100];

	/// <summary>Whether to show the page size dropdown. Default true.</summary>
	[Parameter]
	public bool ShowPageSizeSelector { get; set; } = true;

	/// <summary>Whether to show first/last page buttons. Default true.</summary>
	[Parameter]
	public bool ShowFirstLast { get; set; } = true;

	/// <summary>Whether to show "1-10 of 100" info text. Default true.</summary>
	[Parameter]
	public bool ShowPageInfo { get; set; } = true;

	/// <summary>Maximum page number buttons shown. Default 5.</summary>
	[Parameter]
	public int MaxVisiblePages { get; set; } = 5;

	/// <summary>Compact mode - shows only "prev 1/25 next". Default false.</summary>
	[Parameter]
	public bool Compact { get; set; }

	/// <summary>
	///     Pins the bar to the top or bottom edge via <c>position: sticky</c> as content scrolls.
	///     The consumer owns the scroll container: place this inside an element that scrolls
	///     (or let the page scroll) and it stays visible. Default false.
	/// </summary>
	[Parameter]
	public bool Sticky { get; set; }

	/// <summary>Which edge the bar pins to when <see cref="Sticky" /> is true. Default Bottom.</summary>
	[Parameter]
	public MokaPaginationStickyPosition StickyPosition { get; set; } = MokaPaginationStickyPosition.Bottom;

	/// <summary>Every word the pager writes. Null, the default, is <see cref="MokaPaginationText.Default" />, in English.</summary>
	[Parameter]
	public MokaPaginationText? Text { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-pagination";

	private MokaPaginationText Words => Text ?? MokaPaginationText.Default;

	// The bar sits under the list it pages, so the arrows' names open above it, over the list, unless it is
	// pinned to the top. Where there is no room the tooltip moves to the side that has some.
	private MokaTooltipPosition TooltipPosition =>
		Sticky && StickyPosition == MokaPaginationStickyPosition.Top ? MokaTooltipPosition.Bottom : MokaTooltipPosition.Top;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-pagination--compact", Compact)
		.AddClass("moka-pagination--sticky", Sticky)
		.AddClass("moka-pagination--sticky-top", Sticky && StickyPosition == MokaPaginationStickyPosition.Top)
		.AddClass("moka-pagination--sticky-bottom", Sticky && StickyPosition == MokaPaginationStickyPosition.Bottom)
		.AddClass(Class)
		.Build();

	private int PageSizeShown => _pageSize.Shown;

	private int CountedPages => PageSizeShown > 0 ? (int)Math.Ceiling((double)TotalItems / PageSizeShown) : 0;

	// With an open count the pages run to the one shown, and one more while the source says there is more.
	private int TotalPages => HasMore is { } more ? Math.Max(CountedPages, CurrentPageShown + (more ? 1 : 0)) : CountedPages;

	private bool OpenCount => HasMore is not null;

	private bool ShowLastPage => !OpenCount;

	private int StartItem => TotalItems == 0 ? 0 : (CurrentPageShown - 1) * PageSizeShown + 1;
	private int EndItem => Math.Min(CurrentPageShown * PageSizeShown, TotalItems);

	private string RangeText => HasMore == true
		? Words.RangeOpen(StartItem, EndItem)
		: Words.Range(StartItem, EndItem, TotalItems);

	private string CompactText => OpenCount ? Words.PageOpen(CurrentPageShown) : Words.PageOfPages(CurrentPageShown, TotalPages);

	private bool IsFirstPage => CurrentPageShown <= 1;
	private bool IsLastPage => CurrentPageShown >= TotalPages;

	private bool ShowStartEllipsis { get; set; }

	private bool ShowEndEllipsis { get; set; }

	/// <summary>Pagination has internal page state that changes on click.</summary>
	protected override bool ShouldRender() => true;

	private string PageCssClass(int page) => new CssBuilder("moka-pagination-btn")
		.AddClass("moka-pagination-page")
		.AddClass("moka-pagination-page--active", page == CurrentPageShown)
		.Build();

	// The first and last page buttons and the ellipses stay in the layout while hidden, so the bar
	// keeps its width as the visible range moves.
	private static string EdgePageCssClass(bool visible) => new CssBuilder("moka-pagination-btn")
		.AddClass("moka-pagination-page")
		.AddClass("moka-pagination--hidden", !visible)
		.Build();

	private static string EllipsisCssClass(bool visible) => new CssBuilder("moka-pagination-ellipsis")
		.AddClass("moka-pagination--hidden", !visible)
		.Build();

	private static int EdgePageTabIndex(bool visible) => visible ? 0 : -1;

	// Bug 3: Cache visible pages to avoid recomputing 3 times per render
	private void UpdateVisiblePages()
	{
		int totalPages = TotalPages;
		if (_cachedCurrentPage == CurrentPageShown && _cachedTotalPages == totalPages && _cachedOpen == OpenCount)
		{
			return;
		}

		_cachedCurrentPage = CurrentPageShown;
		_cachedTotalPages = totalPages;
		_cachedOpen = OpenCount;

		var pages = new List<int>();
		if (totalPages <= MaxVisiblePages)
		{
			for (int i = 1; i <= totalPages; i++)
			{
				pages.Add(i);
			}
		}
		else
		{
			int half = MaxVisiblePages / 2;
			int start = Math.Max(1, CurrentPageShown - half);
			int end = Math.Min(totalPages, start + MaxVisiblePages - 1);

			if (end - start + 1 < MaxVisiblePages)
			{
				start = Math.Max(1, end - MaxVisiblePages + 1);
			}

			for (int i = start; i <= end; i++)
			{
				pages.Add(i);
			}
		}

		_visiblePages = pages;
		ShowStartEllipsis = TotalPages > MaxVisiblePages && _visiblePages.Count > 0 && _visiblePages[0] > 1;
		ShowEndEllipsis = ShowLastPage && TotalPages > MaxVisiblePages && _visiblePages.Count > 0 && _visiblePages[^1] < TotalPages;
	}

	private void GoToPage(int page)
	{
		if (page < 1 || page > TotalPages || page == CurrentPageShown)
		{
			return;
		}

		_page.Pick(page, CurrentPageChanged);
		UpdateVisiblePages();
	}

	// The parent owns the size when it passes one and listens: a size it refuses goes back on screen once its handler
	// has run, through @bind on the select.
	private void PickPageSize(int size)
	{
		if (size <= 0)
		{
			return;
		}

		_pageSize.Pick(size, PageSizeChanged);
		_page.Pick(1, CurrentPageChanged);
		UpdateVisiblePages();
	}

	private void ShowParentValue()
	{
		UpdateVisiblePages();
		StateHasChanged();
	}

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		_pageSize.Take(parameters, nameof(PageSize));
		_page.Take(parameters, nameof(CurrentPage));
		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);
		_pageSize.AfterRender();
		_page.AfterRender();
	}

	/// <inheritdoc />
	protected override ValueTask DisposeAsyncCore()
	{
		_pageSize.Stop();
		_page.Stop();
		return base.DisposeAsyncCore();
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		UpdateVisiblePages();
	}
}
