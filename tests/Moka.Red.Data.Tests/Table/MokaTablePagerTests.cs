using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Data.Pagination;
using Moka.Red.Data.Table;
using Moka.Red.Tests.Shared;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Table;

// Every pager look the pager part names: its markup, its keys and buttons, and the endless pager's one observer.
public sealed class MokaTablePagerTests : BunitContext
{
	private static readonly Person[] Twelve = Enumerable.Range(1, 12)
		.Select(static i => new Person($"Person {i:00}", i % 2 == 0 ? "Even" : "Odd", 20 + i, "City"))
		.ToArray();

	public MokaTablePagerTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void WithoutAPagerPart_TheNumberedPagerIsDrawn()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(pager: null);

		Assert.Single(cut.FindAll(".moka-pagination"));
		Assert.Empty(cut.FindAll(".moka-table-pager"));
	}

	[Fact]
	public void TheNumberedPager_TakesItsOptions()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(new Pager { ShowFirstLast = false, ShowPageInfo = false, PageSizeOptions = [5, 50] });

		Assert.Empty(cut.FindAll("button[aria-label='First page']"));
		Assert.Empty(cut.FindAll(".moka-pagination-info"));
		Assert.Equal(["5", "50"], cut.FindAll(".moka-pagination-size-select option").Select(static o => o.TextContent));
	}

	[Fact]
	public void TheEntriesPager_PutsTheSizeAbove_AndTheCountWithPreviousAndNextBelow()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(new Pager { Look = MokaPagerLook.Entries });

		Assert.Contains("Show", cut.Find(".moka-table-pager--top .moka-table-pager-size").TextContent, StringComparison.Ordinal);
		Assert.Equal("Showing 1 to 5 of 12 entries", cut.Find(".moka-table-pager--entries .moka-table-pager-info").TextContent);

		cut.FindAll(".moka-table-pager-buttons button")[1].Click();
		Assert.Equal("Showing 6 to 10 of 12 entries", cut.Find(".moka-table-pager--entries .moka-table-pager-info").TextContent);
		Assert.Equal("Person 06", ColumnTexts(cut, 0)[0]);

		cut.Find(".moka-table-pager-select").Change("10");
		Assert.Equal("Showing 1 to 10 of 12 entries", cut.Find(".moka-table-pager--entries .moka-table-pager-info").TextContent);
	}

	[Fact]
	public void TheCompactPager_ShowsOnlyThePages()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(new Pager { Look = MokaPagerLook.Compact });

		Assert.Equal("1 / 3", cut.Find(".moka-pagination-compact-info").TextContent);
		Assert.Empty(cut.FindAll(".moka-pagination-size-select"));
	}

	[Fact]
	public void TheJumpPager_GoesToTheTypedPage_AndPutsBackAnythingElse()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(new Pager { Look = MokaPagerLook.Jump });
		Assert.Equal("of 3", cut.Find(".moka-table-pager-jump span:last-child").TextContent);

		cut.Find(".moka-table-pager-jump-input").Change("3");
		Assert.Equal(["Person 11", "Person 12"], ColumnTexts(cut, 0));

		cut.Find(".moka-table-pager-jump-input").Change("nine");
		Assert.Equal("3", cut.Find(".moka-table-pager-jump-input").GetAttribute("value"));
		IconTooltip.AssertNamed(cut.Find(".moka-table-pager--jump .moka-table-pager-btn"), "Previous page", Moka.Red.Primitives.Tooltip.MokaTooltipPosition.Top);
	}

	[Fact]
	public void TheCursorPager_StepsThroughPages_WithItems()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(new Pager { Look = MokaPagerLook.Cursor });
		IReadOnlyList<AngleSharp.Dom.IElement> buttons = cut.FindAll(".moka-table-pager--cursor button");
		Assert.True(buttons[0].HasAttribute("disabled"));

		buttons[1].Click();
		Assert.Equal("Person 06", ColumnTexts(cut, 0)[0]);
	}

	[Fact]
	public void TheCursorPager_HandsTheServerItsCursors()
	{
		List<string?> asked = [];
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(x => x.PageSize, 5)
			.Add(x => x.ChildContent, DefaultColumnsWithPager(new Pager { Look = MokaPagerLook.Cursor }))
			.Add(x => x.ServerData, request =>
			{
				asked.Add(request.Cursor);
				int start = request.Cursor is null ? 0 : int.Parse(request.Cursor, System.Globalization.CultureInfo.InvariantCulture);
				return Task.FromResult(new MokaTableResult<Person>
				{
					Items = Twelve.Skip(start).Take(request.PageSize).ToList(),
					NextCursor = start + request.PageSize < Twelve.Length ? (start + request.PageSize).ToString(System.Globalization.CultureInfo.InvariantCulture) : null,
					PreviousCursor = start > 0 ? Math.Max(0, start - request.PageSize).ToString(System.Globalization.CultureInfo.InvariantCulture) : null
				});
			}));
		cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".moka-table-pager--cursor button")));

		cut.FindAll(".moka-table-pager--cursor button")[1].Click();
		Assert.Equal("5", asked[^1]);
		Assert.Equal("Person 06", ColumnTexts(cut, 0)[0]);

		cut.FindAll(".moka-table-pager--cursor button")[0].Click();
		Assert.Equal("0", asked[^1]);
		Assert.Equal("Person 01", ColumnTexts(cut, 0)[0]);
	}

	[Fact]
	public void TheEndlessPager_WatchesTheEndOfTheRows_AndLoadsMoreWhenItIsReached()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.SetupVoid("observeEnd", _ => true);
		IRenderedComponent<MokaTable<Person>> cut = Table(new Pager { Look = MokaPagerLook.Endless });

		Assert.Single(module.Invocations["observeEnd"]);
		Assert.Single(cut.FindAll(".moka-table-foot--endless[data-moka-endless]"));
		Assert.Equal(5, DataRows(cut).Count);

		cut.InvokeAsync(cut.Instance.OnEndReached);
		cut.WaitForAssertion(() => Assert.Equal(10, DataRows(cut).Count));
		Assert.Equal(2, module.Invocations["observeEnd"].Count);

		cut.Find(".moka-table-foot--endless button").Click();
		Assert.Equal(12, DataRows(cut).Count);
		Assert.Empty(cut.FindAll("[data-moka-endless]"));
		Assert.Single(module.Invocations["unobserveEnd"]);
	}

	[Fact]
	public void ATableWithNoEndlessPager_WatchesNothing()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		Table(new Pager { Look = MokaPagerLook.LoadMore });

		Assert.Empty(module.Invocations["observeEnd"]);
	}

	[Fact]
	public void ThePagersWords_AreReplaceable()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(x => x.Items, Twelve)
			.Add(x => x.PageSize, 5)
			.Add(x => x.Text, MokaTableText.Default with
			{
				Pager = MokaPaginationText.Default with { Range = static (a, b, t) => $"{a} bis {b} von {t}", NextPage = "Weiter" }
			})
			.Add(x => x.ChildContent, DefaultColumns));

		Assert.Equal("1 bis 5 von 12", cut.Find(".moka-pagination-info").TextContent);
		Assert.Single(cut.FindAll("button[aria-label='Weiter']"));
	}

	[Fact]
	public void APagerPartArrivingAfterAPage_StartsAgainFromTheFirst()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(new Pager());
		cut.FindAll(".moka-pagination-page").First(b => b.TextContent.Trim() == "2").Click();

		cut.Render(p => p.Add(x => x.ChildContent, DefaultColumnsWithPager(LoadMore)));

		cut.WaitForAssertion(() => Assert.Equal("Person 01", ColumnTexts(cut, 0)[0]));
	}

	private IRenderedComponent<MokaTable<Person>> Table(Pager? pager) =>
		Render<MokaTable<Person>>(p => p
			.Add(x => x.Items, Twelve)
			.Add(x => x.PageSize, 5)
			.Add(x => x.ChildContent, pager is null ? DefaultColumns : DefaultColumnsWithPager(pager)));
}
