using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Pagination;

namespace Moka.Red.Data.Tests.Components;

// The pager used to write the user's page and page size into its own parameters. Blazor passes
// every parameter again whenever the parent renders, so with one-way values the parent's next
// render sent the pager back to the page it started on.
public class MokaPaginationTests : BunitContext
{
	[Fact]
	public async Task StaysOnThePage_WhenTheParentPassesTheSamePage()
	{
		IRenderedComponent<MokaPagination> cut = RenderPager();

		await PageButton(cut, "3").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.CurrentPage, 1));

		Assert.Equal("3", ActivePage(cut));
		Assert.Equal("21-30 of 100", cut.Find(".moka-pagination-info").TextContent);
	}

	[Fact]
	public async Task FollowsANewPageFromTheParent()
	{
		IRenderedComponent<MokaPagination> cut = RenderPager();

		await PageButton(cut, "3").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.CurrentPage, 5));

		Assert.Equal("5", ActivePage(cut));
	}

	[Fact]
	public async Task KeepsThePageSize_WhenTheParentPassesTheSameSize()
	{
		IRenderedComponent<MokaPagination> cut = RenderPager();

		await cut.Find("select").ChangeAsync(new ChangeEventArgs { Value = "25" });
		cut.Render(p => p.Add(x => x.PageSize, 10));

		Assert.Equal("1-25 of 100", cut.Find(".moka-pagination-info").TextContent);
	}

	[Fact]
	public async Task FollowsANewPageSizeFromTheParent()
	{
		IRenderedComponent<MokaPagination> cut = RenderPager();

		await cut.Find("select").ChangeAsync(new ChangeEventArgs { Value = "25" });
		cut.Render(p => p.Add(x => x.PageSize, 50));

		Assert.Equal("1-50 of 100", cut.Find(".moka-pagination-info").TextContent);
	}

	// The first and last page buttons and the ellipses stay in the layout and are hidden with a
	// class, so the bar keeps its width. The classes were spliced in with ternaries in the markup,
	// which left a stray space in the class list of every shown one.
	[Fact]
	public async Task TheEdgePagesAndEllipses_AreHidden_OnlyWhereTheRangeReachesTheEnd()
	{
		IRenderedComponent<MokaPagination> cut = RenderPager();

		AssertEdges(cut, startShown: false, endShown: true);

		await cut.Find("button[title='Next page']").ClickAsync(new MouseEventArgs());
		await cut.Find("button[title='Next page']").ClickAsync(new MouseEventArgs());
		await cut.Find("button[title='Next page']").ClickAsync(new MouseEventArgs());
		await cut.Find("button[title='Next page']").ClickAsync(new MouseEventArgs());

		Assert.Equal("5", ActivePage(cut));
		AssertEdges(cut, startShown: true, endShown: true);

		await cut.Find("button[title='Last page']").ClickAsync(new MouseEventArgs());

		AssertEdges(cut, startShown: true, endShown: false);
	}

	private static void AssertEdges(IRenderedComponent<MokaPagination> cut, bool startShown, bool endShown)
	{
		IReadOnlyList<IElement> edges = cut.FindAll(".moka-pagination-nav button[tabindex]");
		IReadOnlyList<IElement> ellipses = cut.FindAll(".moka-pagination-ellipsis");

		Assert.Equal(2, edges.Count);
		Assert.Equal(2, ellipses.Count);
		AssertShown(edges[0], "moka-pagination-btn moka-pagination-page", startShown);
		AssertShown(ellipses[0], "moka-pagination-ellipsis", startShown);
		AssertShown(ellipses[1], "moka-pagination-ellipsis", endShown);
		AssertShown(edges[1], "moka-pagination-btn moka-pagination-page", endShown);
		Assert.Equal(startShown ? "0" : "-1", edges[0].GetAttribute("tabindex"));
		Assert.Equal(endShown ? "0" : "-1", edges[1].GetAttribute("tabindex"));
	}

	// The buttons had no type, so inside a form they submitted it, and the compact pager's arrows
	// had no name at all.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void EveryButton_IsAPlainNamedButton(bool compact)
	{
		IRenderedComponent<MokaPagination> cut = Render<MokaPagination>(p => p
			.Add(x => x.TotalItems, 100)
			.Add(x => x.PageSize, 10)
			.Add(x => x.CurrentPage, 2)
			.Add(x => x.Compact, compact));

		Assert.All(cut.FindAll("button"), button =>
		{
			Assert.Equal("button", button.GetAttribute("type"));
			Assert.False(string.IsNullOrWhiteSpace(button.GetAttribute("aria-label") ?? button.TextContent));
		});
		Assert.Equal("Pagination", cut.Find(".moka-pagination").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheCurrentPage_IsMarked_AndThePageSizeSelectIsNamed()
	{
		IRenderedComponent<MokaPagination> cut = RenderPager();

		Assert.Equal("page", PageButton(cut, "1").GetAttribute("aria-current"));
		Assert.False(PageButton(cut, "2").HasAttribute("aria-current"));
		Assert.Equal("Rows per page", cut.Find("select").GetAttribute("aria-label"));
	}

	private static void AssertShown(IElement element, string classes, bool shown) =>
		Assert.Equal(shown ? classes : $"{classes} moka-pagination--hidden", element.GetAttribute("class"));

	private IRenderedComponent<MokaPagination> RenderPager() => Render<MokaPagination>(p => p
		.Add(x => x.TotalItems, 100)
		.Add(x => x.PageSize, 10)
		.Add(x => x.CurrentPage, 1));

	private static IElement PageButton(IRenderedComponent<MokaPagination> cut, string page) =>
		cut.FindAll(".moka-pagination-nav .moka-pagination-page").First(b => b.TextContent.Trim() == page);

	private static string ActivePage(IRenderedComponent<MokaPagination> cut) =>
		cut.Find(".moka-pagination-page--active").TextContent.Trim();
}
