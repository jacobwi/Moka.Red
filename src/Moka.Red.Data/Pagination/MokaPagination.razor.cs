using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Pagination;

/// <summary>
///     Standalone pagination component. Usable independently of MokaTable.
///     Provides page navigation, page size selection, and page info display.
/// </summary>
public partial class MokaPagination : MokaComponentBase
{
	private int _cachedCurrentPage;
	private int _cachedTotalPages;
	private List<int> _visiblePages = [];

	/// <summary>Total number of items across all pages.</summary>
	[Parameter]
	public int TotalItems { get; set; }

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

	/// <summary>Compact mode — shows only "prev 1/25 next". Default false.</summary>
	[Parameter]
	public bool Compact { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-pagination";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-pagination--compact", Compact)
		.AddClass(Class)
		.Build();

	private int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;
	private int StartItem => TotalItems == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
	private int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);

	private bool IsFirstPage => CurrentPage <= 1;
	private bool IsLastPage => CurrentPage >= TotalPages;

	private bool ShowStartEllipsis { get; set; }

	private bool ShowEndEllipsis { get; set; }

	/// <summary>Pagination has internal page state that changes on click.</summary>
	protected override bool ShouldRender() => true;

	// Bug 3: Cache visible pages to avoid recomputing 3 times per render
	private void UpdateVisiblePages()
	{
		int totalPages = TotalPages;
		if (_cachedCurrentPage == CurrentPage && _cachedTotalPages == totalPages)
		{
			return;
		}

		_cachedCurrentPage = CurrentPage;
		_cachedTotalPages = totalPages;

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
			int start = Math.Max(1, CurrentPage - half);
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
		ShowEndEllipsis = TotalPages > MaxVisiblePages && _visiblePages.Count > 0 && _visiblePages[^1] < TotalPages;
	}

	private async Task GoToPage(int page)
	{
		if (page < 1 || page > TotalPages || page == CurrentPage)
		{
			return;
		}

		CurrentPage = page;
		UpdateVisiblePages();
		if (CurrentPageChanged.HasDelegate)
		{
			await CurrentPageChanged.InvokeAsync(CurrentPage);
		}
	}

	private async Task HandlePageSizeChange(ChangeEventArgs e)
	{
		if (int.TryParse(e.Value?.ToString(), out int size) && size > 0)
		{
			PageSize = size;
			CurrentPage = 1;
			UpdateVisiblePages();
			if (PageSizeChanged.HasDelegate)
			{
				await PageSizeChanged.InvokeAsync(PageSize);
			}

			if (CurrentPageChanged.HasDelegate)
			{
				await CurrentPageChanged.InvokeAsync(CurrentPage);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		UpdateVisiblePages();
	}
}
