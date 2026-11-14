using System.Globalization;

namespace Moka.Red.Data.Pagination;

/// <summary>
///     Every word a pager writes, for <see cref="MokaPagination" /> and the table's pagers. Replace any of them with
///     <c>with</c>: <c>MokaPaginationText.Default with { NextPage = "Weiter" }</c>.
/// </summary>
public sealed record MokaPaginationText
{
	/// <summary>The English words.</summary>
	public static MokaPaginationText Default { get; } = new();

	/// <summary>The name of the pager's navigation region.</summary>
	public string Label { get; init; } = "Pagination";

	/// <summary>The first page button's name.</summary>
	public string FirstPage { get; init; } = "First page";

	/// <summary>The previous page button's name.</summary>
	public string PreviousPage { get; init; } = "Previous page";

	/// <summary>The next page button's name.</summary>
	public string NextPage { get; init; } = "Next page";

	/// <summary>The last page button's name.</summary>
	public string LastPage { get; init; } = "Last page";

	/// <summary>The page size box's name.</summary>
	public string RowsPerPage { get; init; } = "Rows per page";

	/// <summary>The rows on the page, from the first, the last and the total: "1-10 of 200".</summary>
	public Func<int, int, int, string> Range { get; init; } =
		static (first, last, total) => string.Create(CultureInfo.CurrentCulture, $"{first}-{last} of {total}");

	/// <summary>The page and the page count of the compact pager: "3 / 25".</summary>
	public Func<int, int, string> PageOfPages { get; init; } =
		static (page, pages) => string.Create(CultureInfo.CurrentCulture, $"{page} / {pages}");

	/// <summary>The word before the page size box of the entries pager.</summary>
	public string Show { get; init; } = "Show";

	/// <summary>The word after the page size box of the entries pager.</summary>
	public string EntriesWord { get; init; } = "entries";

	/// <summary>The rows shown by the entries pager: "Showing 1 to 10 of 200 entries".</summary>
	public Func<int, int, int, string> ShowingEntries { get; init; } =
		static (first, last, total) => string.Create(CultureInfo.CurrentCulture, $"Showing {first:N0} to {last:N0} of {total:N0} entries");

	/// <summary>The entries pager's previous button.</summary>
	public string Previous { get; init; } = "Previous";

	/// <summary>The entries pager's next button.</summary>
	public string Next { get; init; } = "Next";

	/// <summary>The word before the page box of the jump pager.</summary>
	public string PageWord { get; init; } = "Page";

	/// <summary>The words after the page box of the jump pager: "of 25".</summary>
	public Func<int, string> OfPages { get; init; } = static pages => string.Create(CultureInfo.CurrentCulture, $"of {pages:N0}");

	/// <summary>The rows shown while the source hasn't counted them all and has more: "21-30 of more".</summary>
	public Func<int, int, string> RangeOpen { get; init; } = static (start, end) =>
		string.Create(CultureInfo.CurrentCulture, $"{start:N0}-{end:N0} of more");

	/// <summary>The compact pager's page while the source hasn't counted the pages: "Page 3".</summary>
	public Func<int, string> PageOpen { get; init; } = static page => string.Create(CultureInfo.CurrentCulture, $"Page {page:N0}");

	/// <summary>The entries pager's line while the source hasn't counted the rows and has more.</summary>
	public Func<int, int, string> ShowingEntriesOpen { get; init; } = static (start, end) =>
		string.Create(CultureInfo.CurrentCulture, $"Showing {start:N0} to {end:N0} of more entries");

	/// <summary>The jump pager's count of pages while the source hasn't counted them: "of more".</summary>
	public string OfMorePages { get; init; } = "of more";

	/// <summary>The name of the jump pager's page box.</summary>
	public string GoToPage { get; init; } = "Go to page";

	/// <summary>The cursor pager's button for newer rows.</summary>
	public string Newer { get; init; } = "Newer";

	/// <summary>The cursor pager's button for older rows.</summary>
	public string Older { get; init; } = "Older";
}
