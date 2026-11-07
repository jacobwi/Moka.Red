using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Pagination;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

public class MokaTableShowMoreTests : BunitContext
{
	private static readonly Person[] Five =
	[
		new("Eve", "T", 5, "C"),
		new("Ada", "T", 1, "C"),
		new("Dan", "T", 4, "C"),
		new("Bob", "T", 2, "C"),
		new("Cid", "T", 3, "C")
	];

	public MokaTableShowMoreTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void NumberedPages_StayTheDefault()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, Five)
			.Add(t => t.PageSize, 2)
			.Add(t => t.ChildContent, DefaultColumns));

		Assert.Single(cut.FindComponents<MokaPagination>());
		Assert.Empty(cut.FindAll(".moka-table-foot"));
	}

	[Fact]
	public void TheFirstRows_ComeWithTheFootInsteadOfAPager()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		Assert.Equal(2, DataRows(cut).Count);
		Assert.Empty(cut.FindComponents<MokaPagination>());
		Assert.Equal("Showing 2 of 5", cut.Find(".moka-table-foot-count").TextContent.Trim());
		Assert.Equal(["Show 2 more", "Show all"], FootButtons(cut).Select(b => b.TextContent.Trim()));
	}

	[Fact]
	public async Task ShowMore_AddsAPageSize()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		await FootButtons(cut)[0].ClickAsync(new MouseEventArgs());

		Assert.Equal(4, DataRows(cut).Count);
		Assert.Equal("Showing 4 of 5", cut.Find(".moka-table-foot-count").TextContent.Trim());
	}

	[Fact]
	public async Task TheButton_NeverPromisesMoreThanThereIs()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		await FootButtons(cut)[0].ClickAsync(new MouseEventArgs());

		Assert.Equal("Show 1 more", FootButtons(cut)[0].TextContent.Trim());
	}

	[Fact]
	public async Task TheFoot_GoesWithTheLastRows()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		await FootButtons(cut)[0].ClickAsync(new MouseEventArgs());
		await FootButtons(cut)[0].ClickAsync(new MouseEventArgs());

		Assert.Equal(5, DataRows(cut).Count);
		Assert.Empty(cut.FindAll(".moka-table-foot"));
	}

	[Fact]
	public async Task ShowAll_DrawsEveryRow()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		await FootButtons(cut)[1].ClickAsync(new MouseEventArgs());

		Assert.Equal(5, DataRows(cut).Count);
		Assert.Empty(cut.FindAll(".moka-table-foot"));
	}

	[Fact]
	public void AListThatFits_HasNoFoot()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(pageSize: 5);

		Assert.Empty(cut.FindAll(".moka-table-foot"));
	}

	[Fact]
	public async Task TheFirstRows_FollowTheSortOrder()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		await HeaderCell(cut, "Name").ClickAsync(new MouseEventArgs());

		Assert.Equal(["Ada", "Bob"], ColumnTexts(cut, 0));
	}

	[Fact]
	public async Task ASort_KeepsWhatWasAskedFor()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();
		await FootButtons(cut)[0].ClickAsync(new MouseEventArgs());

		await HeaderCell(cut, "Name").ClickAsync(new MouseEventArgs());

		Assert.Equal(["Ada", "Bob", "Cid", "Dan"], ColumnTexts(cut, 0));
	}

	[Fact]
	public async Task ANewPageSize_StartsOver()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();
		await FootButtons(cut)[1].ClickAsync(new MouseEventArgs());

		cut.Render(p => p.Add(t => t.PageSize, 3));

		Assert.Equal(3, DataRows(cut).Count);
	}

	[Fact]
	public async Task NewRows_KeepWhatWasAskedFor()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();
		await FootButtons(cut)[0].ClickAsync(new MouseEventArgs());

		cut.Render(p => p.Add(t => t.Items, [.. Five, new Person("Fay", "T", 6, "C")]));

		Assert.Equal(4, DataRows(cut).Count);
		Assert.Equal("Showing 4 of 6", cut.Find(".moka-table-foot-count").TextContent.Trim());
	}

	[Fact]
	public void ItWins_OverAHiddenPager()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(extra: p => p.Add(t => t.ShowPagination, false));

		Assert.Equal(2, DataRows(cut).Count);
		Assert.NotEmpty(cut.FindAll(".moka-table-foot"));
	}

	[Fact]
	public void TurningItOnAndOff_LoadsTheRowsAgain()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		cut.Render(p => p.Add(t => t.ShowMore, false));

		Assert.Single(cut.FindComponents<MokaPagination>());
		Assert.Empty(cut.FindAll(".moka-table-foot"));

		cut.Render(p => p.Add(t => t.ShowMore, true));

		Assert.Empty(cut.FindComponents<MokaPagination>());
		Assert.Equal(2, DataRows(cut).Count);
	}

	// The buttons go away with the last rows, and focus would fall to the page.
	[Fact]
	public async Task WhenTheFootGoes_FocusMovesToTheFirstNewRow()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = Table();

		await FootButtons(cut)[1].ClickAsync(new MouseEventArgs());

		JSRuntimeInvocation call = Assert.Single(module.Invocations, i => i.Identifier == "focusCell");
		Assert.Equal(2, call.Arguments[1]);
	}

	[Fact]
	public async Task WhileTheFootStays_FocusStaysOnTheButton()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = Table();

		await FootButtons(cut)[0].ClickAsync(new MouseEventArgs());

		Assert.DoesNotContain(module.Invocations, i => i.Identifier == "focusCell");
	}

	[Fact]
	public void TheBoxInTheHeader_SaysWhatItCovers()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(extra: p => p.Add(t => t.Selectable, true));

		Assert.Equal("Select all rows shown", cut.Find("thead input[type=checkbox]").GetAttribute("aria-label"));
	}

	[Fact]
	public async Task ServerData_IsAskedForPageOneOfTheSizeShown()
	{
		List<MokaTableState> asked = [];
		IRenderedComponent<MokaTable<Person>> cut = ServerTable(asked);
		await cut.WaitForStateAsync(() => DataRows(cut).Count == 2, Wait);

		await FootButtons(cut)[0].ClickAsync(new MouseEventArgs());
		await cut.WaitForStateAsync(() => DataRows(cut).Count == 4, Wait);

		Assert.Equal([(1, 2), (1, 4)], asked.Select(s => (s.Page, s.PageSize)));
	}

	[Fact]
	public async Task ServerData_ShowAll_AsksForTheWholeSet()
	{
		List<MokaTableState> asked = [];
		IRenderedComponent<MokaTable<Person>> cut = ServerTable(asked);
		await cut.WaitForStateAsync(() => DataRows(cut).Count == 2, Wait);

		await FootButtons(cut)[1].ClickAsync(new MouseEventArgs());
		await cut.WaitForStateAsync(() => DataRows(cut).Count == 5, Wait);

		Assert.Equal((1, 5), (asked[^1].Page, asked[^1].PageSize));
	}

	// The rows on screen are what someone is reading while they ask for more.
	[Fact]
	public async Task ServerData_KeepsTheRowsWhileMoreLoad()
	{
		TaskCompletionSource<MokaTableResult<Person>>? pending = null;
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.ShowMore, true)
			.Add(t => t.PageSize, 2)
			.Add(t => t.ServerData, state =>
			{
				if (state.PageSize == 2)
				{
					return Task.FromResult(new MokaTableResult<Person> { Items = Five.Take(2).ToList(), TotalItems = 5 });
				}

				pending = new TaskCompletionSource<MokaTableResult<Person>>();
				return pending.Task;
			})
			.Add(t => t.ChildContent, DefaultColumns));
		await cut.WaitForStateAsync(() => DataRows(cut).Count == 2, Wait);

		Task click = FootButtons(cut)[0].ClickAsync(new MouseEventArgs());
		await cut.WaitForStateAsync(() => pending is not null, Wait);

		Assert.Empty(cut.FindAll(".moka-table-skeleton-row"));
		Assert.Equal(2, DataRows(cut).Count);

		pending!.SetResult(new MokaTableResult<Person> { Items = Five.Take(4).ToList(), TotalItems = 5 });
		await click;
		await cut.WaitForStateAsync(() => DataRows(cut).Count == 4, Wait);
	}

	// A list that refreshes on a timer: the rows stay until the new ones arrive.
	[Fact]
	public async Task RefreshAsync_KeepsTheRowsOnScreen()
	{
		TaskCompletionSource<MokaTableResult<Person>>? pending = null;
		bool first = true;
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.PageSize, 2)
			.Add(t => t.ServerData, state =>
			{
				if (first)
				{
					first = false;
					return Task.FromResult(new MokaTableResult<Person> { Items = Five.Take(2).ToList(), TotalItems = 5 });
				}

				pending = new TaskCompletionSource<MokaTableResult<Person>>();
				return pending.Task;
			})
			.Add(t => t.ChildContent, DefaultColumns));
		await cut.WaitForStateAsync(() => DataRows(cut).Count == 2, Wait);

		Task refresh = cut.InvokeAsync(cut.Instance.RefreshAsync);
		await cut.WaitForStateAsync(() => pending is not null, Wait);
		cut.Render();

		Assert.Empty(cut.FindAll(".moka-table-skeleton-row"));
		Assert.Equal(2, DataRows(cut).Count);

		pending!.SetResult(new MokaTableResult<Person> { Items = Five.Skip(2).Take(2).ToList(), TotalItems = 5 });
		await refresh;
		cut.WaitForAssertion(() => Assert.Equal("Dan", ColumnTexts(cut, 0)[0]), Wait);
	}

	private static TimeSpan Wait => TimeSpan.FromSeconds(10);

	private IRenderedComponent<MokaTable<Person>> Table(
		int pageSize = 2,
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, Five);
			p.Add(t => t.ShowMore, true);
			p.Add(t => t.PageSize, pageSize);
			p.Add(t => t.ChildContent, DefaultColumns);
			extra?.Invoke(p);
		});

	private IRenderedComponent<MokaTable<Person>> ServerTable(List<MokaTableState> asked) =>
		Render<MokaTable<Person>>(p => p
			.Add(t => t.ShowMore, true)
			.Add(t => t.PageSize, 2)
			.Add(t => t.ServerData, state =>
			{
				asked.Add(state);
				return Task.FromResult(new MokaTableResult<Person> { Items = Five.Take(state.PageSize).ToList(), TotalItems = 5 });
			})
			.Add(t => t.ChildContent, DefaultColumns));

	private static IReadOnlyList<IElement> FootButtons(IRenderedComponent<MokaTable<Person>> cut) =>
		cut.FindAll(".moka-table-foot button");
}
