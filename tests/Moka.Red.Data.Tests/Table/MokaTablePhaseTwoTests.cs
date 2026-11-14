using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Pagination;
using Moka.Red.Data.Table;
using Moka.Red.Icons;
using Moka.Red.Tests.Shared;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Table;

// Search, pinned columns, the column menu, heading groups, rows, nested grouping, server totals, the states, the theme
// choice and the fixes to phase 1's weak spots.
public sealed class MokaTablePhaseTwoTests : BunitContext
{
	private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
	private readonly FakeClock _clock = new();
	private readonly BunitJSModuleInterop _module;

	public MokaTablePhaseTwoTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(TableModule);
		_module.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton<TimeProvider>(_clock);
	}

	// ── Search ──

	[Fact]
	public async Task TheSearch_BindsBothWays()
	{
		string? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Searchable, true)
			.Add(t => t.Search, "ada")
			.Add(t => t.SearchChanged, s => raised = s));
		Assert.Equal(["Ada"], ColumnTexts(cut, 0));

		await cut.Find(".moka-table-search input").InputAsync(new ChangeEventArgs { Value = "gr" });
		_clock.Advance(TimeSpan.FromMilliseconds(300));

		cut.WaitForAssertion(() => Assert.Equal("gr", raised), Wait);
		Assert.Equal(["Grace"], ColumnTexts(cut, 0));
	}

	[Fact]
	public async Task TheDebounce_IsTheTables_OnTheRegisteredClock()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Searchable, true).Add(t => t.Debounce, TimeSpan.FromSeconds(1)));

		await cut.Find(".moka-table-search input").InputAsync(new ChangeEventArgs { Value = "grace" });
		_clock.Advance(TimeSpan.FromMilliseconds(900));
		Assert.Equal(People.Length, DataRows(cut).Count);

		_clock.Advance(TimeSpan.FromMilliseconds(100));
		cut.WaitForAssertion(() => Assert.Equal(["Grace"], ColumnTexts(cut, 0)), Wait);
	}

	[Fact]
	public void HighlightSearch_MarksWhatTheSearchFound_InTheTextCells()
	{
		IRenderedComponent<MokaTable<Person>> marked = Table(p => p.Add(t => t.Searchable, true).Add(t => t.Search, "an").Add(t => t.HighlightSearch, true));
		IElement mark = Assert.Single(marked.FindAll("tbody mark.moka-table-mark"));
		Assert.Equal("an", mark.TextContent);
		Assert.Equal("Alan", mark.ParentElement!.TextContent);

		Assert.Empty(Table(p => p.Add(t => t.Searchable, true).Add(t => t.Search, "an")).FindAll("mark"));
	}

	// ── Pinned columns ──

	[Fact]
	public void PinnedColumns_MoveToTheirEdges_InTheOrderTheyStand()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(columns: [Name with { Pin = MokaColumnPin.End }, Team, Age, City with { Pin = MokaColumnPin.Start }]);

		Assert.Equal(["City", "Team", "Age", "Name"], HeaderTitles(cut));
	}

	[Fact]
	public void PinnedCells_SitAtTheMeasuredOffsets_AndMarkTheEdge()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(columns: [Name with { Pin = MokaColumnPin.End }, Team, Age, City with { Pin = MokaColumnPin.Start }]);

		IElement city = HeaderCell(cut, "City");
		Assert.Contains("moka-table-cell--pin-start", city.ClassList);
		Assert.Contains("moka-table-cell--pin-edge", city.ClassList);
		Assert.Equal("0", city.GetAttribute("data-pin-at"));
		Assert.Contains("inset-inline-start: var(--_pin-0, 0px)", city.GetAttribute("style"), StringComparison.Ordinal);
		IElement name = HeaderCell(cut, "Name");
		Assert.Contains("inset-inline-end: var(--_pin-3, 0px)", name.GetAttribute("style"), StringComparison.Ordinal);
		IElement cell = DataRows(cut)[0].QuerySelector("td[data-col-index='3']")!;
		Assert.Contains("moka-table-cell--pin-end", cell.ClassList);
		Assert.Contains("inset-inline-end: var(--_pin-3, 0px)", cell.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Null(HeaderCell(cut, "Team").GetAttribute("data-pin-at"));
	}

	[Fact]
	public void PinnedOffsets_AreMeasuredOncePerLayoutChange()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(columns: [Name with { Pin = MokaColumnPin.Start }, Team, Age, City]);
		cut.WaitForAssertion(() => Assert.Single(_module.Invocations["measurePins"]));

		cut.Render();
		Assert.Single(_module.Invocations["measurePins"]);

		cut.Render(p => p.Add(t => t.ChildContent, Columns(Name, Team, Age, City)));
		cut.WaitForAssertion(() => Assert.Single(_module.Invocations["releasePins"]));
	}

	[Fact]
	public void ATableWithNoPin_CallsNoPinScript()
	{
		Table();
		Assert.Empty(_module.Invocations["measurePins"]);
		Assert.Empty(_module.Invocations["releasePins"]);
	}

	[Fact]
	public void APinFromTheState_ReplacesTheColumns()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.State, new MokaTableState
		{
			Columns = [new MokaTableColumnState("Name") { Pin = MokaColumnPin.End }]
		}));

		Assert.Equal(["Team", "Age", "City", "Name"], HeaderTitles(cut));
	}

	// ── The column menu ──

	[Fact]
	public void TheColumnMenu_HasANamedButtonInEachHeading()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ColumnMenu, true));

		IElement button = cut.FindAll(".moka-table-menu-btn")[0];
		Assert.Equal(4, cut.FindAll(".moka-table-menu-btn").Count);
		Assert.Equal("Name column options", button.GetAttribute("aria-label"));
		Assert.Equal("menu", button.GetAttribute("aria-haspopup"));
		Assert.NotNull(button.Closest(".moka-tooltip-trigger"));
	}

	[Fact]
	public async Task OpeningTheMenu_ShowsItsItems_InTheTopLayer()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ColumnMenu, true));

		await cut.FindAll(".moka-table-menu-btn")[0].ClickAsync(new MouseEventArgs());

		IElement menu = cut.Find(".moka-table-column-menu-panel");
		Assert.Equal("menu", menu.GetAttribute("role"));
		Assert.Equal(["Sort ascending", "Sort descending", "Pin to start", "Pin to end", "Group by this column", "Hide column", "Fit to content"],
			menu.QuerySelectorAll("button").Select(b => b.TextContent.Trim()));
		Assert.All(menu.QuerySelectorAll("button"), item => Assert.Equal("-1", item.GetAttribute("tabindex")));
		Assert.Equal(true, Assert.Single(_module.Invocations["openPopup"]).Arguments[7]);
	}

	[Fact]
	public async Task TheMenu_SortsPinsHidesAndGroups()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ColumnMenu, true).Add(t => t.StateChanged, s => raised = s));

		await MenuItem(cut, 0, "Sort descending");
		Assert.Equal(["Grace", "Barbara", "Alan", "Ada"], ColumnTexts(cut, 0));

		await MenuItem(cut, 0, "Pin to end");
		Assert.Equal(["Team", "Age", "City", "Name"], HeaderTitles(cut));
		Assert.Equal(MokaColumnPin.End, raised!.Columns.Single(c => c.Title == "Name").Pin);

		await MenuItem(cut, 0, "Hide column");
		Assert.Equal(["Age", "City", "Name"], HeaderTitles(cut));

		await MenuItem(cut, 0, "Group by this column");
		Assert.Equal(["Age"], raised.Groups!);
		Assert.Equal("Age", cut.Find(".moka-table-group-bar .moka-table-chip-text").TextContent);
	}

	[Fact]
	public async Task FitToContent_TakesTheWidthTheScriptMeasures()
	{
		_module.Setup<double>("fitColumn", _ => true).SetResult(180.4);
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ColumnMenu, true));

		await MenuItem(cut, 0, "Fit to content");

		Assert.Contains("width: 181px", HeaderCell(cut, "Name").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task Escape_ClosesTheMenu_AndGivesFocusBackToItsButton()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ColumnMenu, true));
		await cut.FindAll(".moka-table-menu-btn")[1].ClickAsync(new MouseEventArgs());

		await cut.InvokeAsync(() => cut.FindComponent<MokaTableColumnMenu<Person>>().Instance.Dismiss());

		Assert.Empty(cut.FindAll(".moka-table-column-menu-panel"));
		cut.WaitForAssertion(() => Assert.Contains(_module.Invocations["focusFirst"],
			call => ((string[])call.Arguments[1]!)[0].StartsWith("[data-moka-menu-for=", StringComparison.Ordinal)));
	}

	private static async Task MenuItem(IRenderedComponent<MokaTable<Person>> cut, int heading, string item)
	{
		await cut.FindAll(".moka-table-menu-btn")[heading].ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-table-column-menu-panel button").First(b => b.TextContent.Trim() == item).ClickAsync(new MouseEventArgs());
	}

	// ── Heading groups ──

	[Fact]
	public void AGroup_DrawsOneHeadingOverItsColumns()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(Grouped());

		IReadOnlyList<IElement> headings = cut.FindAll(".moka-table-group-heading-row th");
		Assert.Equal(["Name", "Team", "Age", "City"], Headings(cut));
		Assert.Equal(["Person", ""], headings.Select(th => th.TextContent));
		Assert.Equal(["2", "2"], headings.Select(th => th.GetAttribute("colspan")));
		Assert.Equal("colgroup", headings[0].GetAttribute("scope"));
		Assert.Equal("true", headings[1].GetAttribute("aria-hidden"));
	}

	[Fact]
	public void AGroup_FollowsItsColumns_ThroughHidingAndPinning()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(Grouped(), p => p.Add(t => t.State, new MokaTableState
		{
			Columns = [new MokaTableColumnState("Name"), new MokaTableColumnState("Team") { Pin = MokaColumnPin.End }, new MokaTableColumnState("Age") { Hidden = true }, new MokaTableColumnState("City")]
		}));

		Assert.Equal(["Person", "", "Person"], cut.FindAll(".moka-table-group-heading-row th").Select(th => th.TextContent));
		Assert.Equal(["1", "1", "1"], cut.FindAll(".moka-table-group-heading-row th").Select(th => th.GetAttribute("colspan")));
		Assert.Contains("moka-table-cell--pin-end", cut.FindAll(".moka-table-group-heading-row th")[2].ClassList);
	}

	[Fact]
	public void NoGroup_DrawsNoHeadingRow() => Assert.Empty(Table().FindAll(".moka-table-group-heading-row"));

	[Fact]
	public async Task AGroupThatKeepsItsColumnsTogether_RefusesAColumnFromOutside()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(Grouped(keepTogether: true), p => p.Add(t => t.ColumnReorderable, true));

		await Heading(cut, "Age").TriggerEventAsync("ondragstart", new DragEventArgs());
		await Heading(cut, "Name").TriggerEventAsync("ondrop", new DragEventArgs());
		Assert.Equal(["Name", "Team", "Age", "City"], Headings(cut));

		await Heading(cut, "Team").TriggerEventAsync("ondragstart", new DragEventArgs());
		await Heading(cut, "Name").TriggerEventAsync("ondrop", new DragEventArgs());
		Assert.Equal(["Team", "Name", "Age", "City"], Headings(cut));
	}

	// With heading groups the columns' headings are the second row.
	private static List<string> Headings(IRenderedComponent<MokaTable<Person>> cut) =>
		cut.FindAll("thead tr:not(.moka-table-group-heading-row) th .moka-table-header-text").Select(e => e.TextContent.Trim()).ToList();

	private static IElement Heading(IRenderedComponent<MokaTable<Person>> cut, string title) =>
		cut.FindAll("thead tr:not(.moka-table-group-heading-row) th")
			.First(th => th.QuerySelector(".moka-table-header-text")?.TextContent.Trim() == title);

	// Name and Team under a heading group, then Age and City.
	private static RenderFragment Grouped(bool keepTogether = false) => builder =>
	{
		builder.OpenComponent<MokaColumnGroup>(300);
		builder.AddComponentParameter(301, nameof(MokaColumnGroup.Title), "Person");
		builder.AddComponentParameter(302, nameof(MokaColumnGroup.KeepTogether), keepTogether);
		builder.AddComponentParameter(303, nameof(MokaColumnGroup.ChildContent), Columns(Name, Team));
		builder.CloseComponent();
		builder.AddContent(304, Columns(Age, City));
	};

	// ── Rows ──

	[Fact]
	public async Task SelectOnRowClick_PicksARow_CtrlAddsOne_ShiftAddsTheRun()
	{
		HashSet<Person>? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Selectable, true)
			.Add(t => t.SelectOnRowClick, true)
			.Add(t => t.SelectedItemsChanged, s => raised = s));

		await DataRows(cut)[0].ClickAsync(new MouseEventArgs());
		Assert.Equal(["Ada"], raised!.Select(x => x.Name));

		await DataRows(cut)[2].ClickAsync(new MouseEventArgs { CtrlKey = true });
		Assert.Equal(["Ada", "Alan"], raised!.Select(x => x.Name).Order());

		await DataRows(cut)[1].ClickAsync(new MouseEventArgs());
		Assert.Equal(["Grace"], raised!.Select(x => x.Name));

		await DataRows(cut)[3].ClickAsync(new MouseEventArgs { ShiftKey = true });
		Assert.Equal(["Alan", "Barbara", "Grace"], raised!.Select(x => x.Name).Order());
	}

	// A parent that passes the selection and doesn't take the new set back owns it, and the box goes back unticked.
	[Fact]
	public async Task ASelectionTheParentRefuses_GoesBackOnScreen()
	{
		HashSet<Person> mine = [];
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Selectable, true)
			.Add(t => t.SelectedItems, mine)
			.Add(t => t.SelectedItemsChanged, _ => { }));

		await DataRows(cut)[0].QuerySelector("input[type='checkbox']")!.ChangeAsync(new ChangeEventArgs { Value = true });

		cut.WaitForAssertion(() => Assert.False(DataRows(cut)[0].QuerySelector("input[type='checkbox']")!.HasAttribute("checked")));
		Assert.DoesNotContain("moka-table-row--selected", DataRows(cut)[0].ClassList);
	}

	[Fact]
	public void ARowDoubleClick_IsRaisedWithItsItem_AndListensOnlyWhenHeard()
	{
		Person? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.OnRowDoubleClick, item => raised = item));

		DataRows(cut)[1].DoubleClick();

		Assert.Equal("Grace", raised!.Name);
		Assert.False(DataRows(Table())[0].HasAttribute("blazor:ondblclick"));
	}

	[Fact]
	public async Task ExpandAll_OpensEveryRowShown_AndClosesThemAgain()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Expandable, true)
			.Add(t => t.ShowExpandAll, true)
			.Add(t => t.ItemKey, x => x.Name)
			.Add(t => t.DetailTemplate, item => b => b.AddContent(0, item.City))
			.Add(t => t.StateChanged, s => raised = s));
		IElement button = cut.Find(".moka-table-expand-all");
		Assert.Equal("Show detail for every row", button.GetAttribute("aria-label"));

		await button.ClickAsync(new MouseEventArgs());
		Assert.Equal(4, cut.FindAll(".moka-table-detail-row").Count);
		Assert.Equal(["Ada", "Alan", "Barbara", "Grace"], raised!.Expanded);

		await cut.Find(".moka-table-expand-all").ClickAsync(new MouseEventArgs());
		Assert.Empty(cut.FindAll(".moka-table-detail-row"));
	}

	[Fact]
	public void PinnedRows_StandOutsideTheSortFiltersAndPages()
	{
		Person total = new("Total", "", 151, "");
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.PageSize, 2)
			.Add(t => t.PinnedTop, [People[3]])
			.Add(t => t.PinnedBottom, [total])
			.Add(t => t.State, KitSorted()));

		IElement top = cut.Find(".moka-table-pinned-body--top tr");
		Assert.Equal("t0", top.GetAttribute("data-pinned-row"));
		Assert.Null(top.GetAttribute("data-row-index"));
		Assert.Equal("Barbara", top.QuerySelector("td")!.TextContent.Trim());
		Assert.Equal("Total", cut.Find(".moka-table-pinned-body--bottom tr td").TextContent.Trim());
		Assert.Equal(2, DataRows(cut).Count);
	}

	private static MokaTableState KitSorted() => new() { Sorts = [new MokaTableSort("Age", MokaSortDirection.Descending)] };

	// ── Nested grouping ──

	[Fact]
	public void NestedGroups_ShowWholeGroupCounts_AndACutPageShowsItsGroupsFirst()
	{
		MokaTableState nested = new() { Groups = ["Team", "City"] };
		IRenderedComponent<MokaTable<Person>> first = Table(p => p.Add(t => t.PageSize, 3).Add(t => t.State, nested));
		Assert.Equal(["Engineering", "London"], first.FindAll(".moka-table-group-key").Select(e => e.TextContent));
		Assert.Equal(["2", "1"], first.FindAll(".moka-table-group-count").Select(e => e.TextContent));
		Assert.Contains("moka-table-group-row--nested", first.FindAll(".moka-table-group-row")[1].ClassList);

		IRenderedComponent<MokaTable<Person>> second = Table(p => p.Add(t => t.PageSize, 3).Add(t => t.State, nested with { Page = 2 }));
		Assert.Equal(["Engineering", "Berlin", "Research"], second.FindAll(".moka-table-group-key").Select(e => e.TextContent));
	}

	[Fact]
	public void AGroupsHeading_TotalsEachAggregatedColumn_OverTheWholeGroup()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.GroupBy, "Team").Add(t => t.PageSize, 2),
			Name, Team, Age with { Aggregate = MokaAggregateType.Sum, AggregateFormat = "0" }, City);

		Assert.Equal(["77"], cut.FindAll(".moka-table-group-total").Select(e => e.TextContent));
		Assert.Equal("2", cut.Find(".moka-table-group-cell").GetAttribute("colspan"));
	}

	[Fact]
	public async Task EveryGroup_CollapsesAndExpandsAtOnce()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.State, new MokaTableState { Groups = ["Team", "City"] }));

		await cut.InvokeAsync(() => cut.Instance.CollapseAllGroupsAsync());
		Assert.Empty(DataRows(cut));
		Assert.Equal(["Engineering", "Research"], cut.FindAll(".moka-table-group-key").Select(e => e.TextContent));

		await cut.InvokeAsync(() => cut.Instance.ExpandAllGroupsAsync());
		Assert.Equal(4, DataRows(cut).Count);
	}

	[Fact]
	public void AServer_HearsTheGroupsAndTotals_AndSendsWholeGroupsAndTheFooter()
	{
		List<MokaTableRequest> requests = [];
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.GroupBy, "Team")
			.Add(t => t.ChildContent, Columns(Name, Team, Age with { Aggregate = MokaAggregateType.Sum, AggregateFormat = "0" }, City))
			.Add(t => t.ServerData, request =>
			{
				requests.Add(request);
				return Task.FromResult(new MokaTableResult<Person>
				{
					Items = [People[0], People[2], People[1], People[3]],
					TotalItems = 40,
					Totals = new Dictionary<string, double> { ["Age"] = 1500 },
					GroupTotals = [new MokaTableGroupTotals(["Engineering"], 30) { Totals = new Dictionary<string, double> { ["Age"] = 1100 } }]
				});
			}));

		cut.WaitForAssertion(() => Assert.Equal(["30", "2"], cut.FindAll(".moka-table-group-count").Select(e => e.TextContent)), Wait);
		Assert.Equal(["Team"], requests[0].Groups);
		Assert.Equal(new MokaTableAggregate("Age", MokaAggregateType.Sum), Assert.Single(requests[0].Aggregates));
		Assert.Equal(["1100", "74"], cut.FindAll(".moka-table-group-total").Select(e => e.TextContent));
		Assert.Equal("1500", cut.Find(".moka-table-aggregate-value").TextContent);
	}

	// ── States ──

	[Fact]
	public async Task NoResults_SaysWhatWasSearched_AndClearsTheSearchAndFilters()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Searchable, true).Add(t => t.Search, "zzz"));

		Assert.Equal("No results for “zzz”.", cut.Find(".moka-table-no-results-text").TextContent);
		Assert.Equal("status", cut.Find(".moka-table-no-results").GetAttribute("role"));
		Assert.Empty(cut.FindAll(".moka-table-empty"));

		await cut.Find(".moka-table-no-results button").ClickAsync(new MouseEventArgs());

		Assert.Equal(People.Length, DataRows(cut).Count);
	}

	[Fact]
	public void NoResults_TakesItsOwnContent_AndAnEmptyTableStaysEmpty()
	{
		IRenderedComponent<MokaTable<Person>> own = Table(p => p
			.Add(t => t.Search, "zzz")
			.Add(t => t.NoResults, b => b.AddMarkupContent(0, "<p class=\"own\">Nothing</p>")));
		Assert.Equal("Nothing", own.Find(".own").TextContent);

		IRenderedComponent<MokaTable<Person>> empty = Render<MokaTable<Person>>(p => p.Add(t => t.Items, Array.Empty<Person>()).Add(t => t.ChildContent, DefaultColumns));
		Assert.NotNull(empty.Find(".moka-table-empty"));
		Assert.Empty(empty.FindAll(".moka-table-no-results"));
	}

	// ── Phase 1's weak spots ──

	[Fact]
	public void AColumnsDefaultSort_ReachesTheFirstRequest()
	{
		List<MokaTableRequest> requests = [];
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.ChildContent, Columns(Name, Age with { DefaultSort = MokaSortDirection.Descending }))
			.Add(t => t.ServerData, request =>
			{
				requests.Add(request);
				return Task.FromResult(new MokaTableResult<Person> { Items = People, TotalItems = 4 });
			}));

		cut.WaitForAssertion(() => Assert.Equal(People.Length, DataRows(cut).Count), Wait);
		MokaTableRequest first = Assert.Single(requests);
		Assert.Equal(new MokaTableSort("Age", MokaSortDirection.Descending), Assert.Single(first.Sorts));
	}

	[Fact]
	public void APagerPart_IsTheOnlyPagerDrawn()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, Enumerable.Range(1, 30).Select(i => new Person($"P{i}", "T", i, "C")).ToArray())
			.Add(t => t.ChildContent, DefaultColumnsWithPager(new Pager { Look = MokaPagerLook.Jump })));

		Assert.Empty(cut.FindComponents<MokaPagination>());
		Assert.NotEmpty(cut.FindAll(".moka-table-pager--jump"));
	}

	[Fact]
	public async Task ASourceThatDoesntCount_IsPagedHonestly()
	{
		Person[] rows = Enumerable.Range(1, 4).Select(i => new Person($"P{i}", "T", i, "C")).ToArray();
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.PageSize, 2)
			.Add(t => t.ChildContent, DefaultColumns)
			.Add(t => t.ServerData, request => Task.FromResult(new MokaTableResult<Person>
			{
				Items = rows.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList()
			})));
		cut.WaitForAssertion(() => Assert.Equal("1-2 of more", cut.Find(".moka-pagination-info").TextContent), Wait);
		Assert.Empty(cut.FindAll("button[aria-label='Last page']"));

		await cut.Find("button[aria-label='Next page']").ClickAsync(new MouseEventArgs());
		cut.WaitForAssertion(() => Assert.Equal("3-4 of more", cut.Find(".moka-pagination-info").TextContent), Wait);

		await cut.Find("button[aria-label='Next page']").ClickAsync(new MouseEventArgs());
		cut.WaitForAssertion(() => Assert.Equal("3-4 of 4", cut.Find(".moka-pagination-info").TextContent), Wait);
		Assert.True(cut.Find("button[aria-label='Next page']").HasAttribute("disabled"));
	}

	[Fact]
	public async Task ACellsControl_InAPinnedRow_ActsOnThatRow()
	{
		List<string> ran = [];
		RenderFragment columns = b =>
		{
			Name.Write(b);
			b.OpenComponent<MokaActionsColumn<Person>>(200);
			b.AddComponentParameter(201, nameof(MokaActionsColumn<Person>.Actions), (IReadOnlyList<MokaTableAction<Person>>)
			[
				new MokaTableAction<Person>("Open", MokaIcons.Action.Edit, item =>
				{
					ran.Add(item.Name);
					return Task.CompletedTask;
				})
			]);
			b.CloseComponent();
		};
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.PinnedTop, [new Person("Pinned", "", 0, "")])
			.Add(t => t.ChildContent, columns));
		IElement button = cut.Find(".moka-table-pinned-body--top button[data-moka-act]");

		await cut.InvokeAsync(() => cut.Instance.OnCellAction("t0", button.GetAttribute("data-moka-act")!, 0, 0, 0));

		cut.WaitForAssertion(() => Assert.Equal(["Pinned"], ran));
	}

	// ── Reset: a reset is a change from what was in force before it ──

	[Fact]
	public async Task Reset_RaisesTheDefaultState_ToABoundParent()
	{
		IRenderedComponent<BoundStateHost> host = Render<BoundStateHost>();
		IRenderedComponent<MokaTable<Person>> cut = host.FindComponent<MokaTable<Person>>();
		await HeaderCell(cut, "Age").ClickAsync(new MouseEventArgs());
		Assert.Single(host.Instance.State!.Sorts!);

		await cut.InvokeAsync(() => cut.Instance.ResetStateAsync());

		Assert.Empty(host.Instance.State!.Sorts!);
		Assert.Equal(["Ada", "Grace", "Alan", "Barbara"], ColumnTexts(cut, 0));
	}

	[Fact]
	public async Task Reset_KeepsTheDefaultState_InTheStore()
	{
		MemoryStore store = new();
		Services.AddSingleton<IMokaTableStateStore>(store);
		SetRendererInfo(new RendererInfo("Server", true));
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.StorageKey, "people"));
		cut.WaitForAssertion(() => Assert.Equal(People.Length, DataRows(cut).Count), Wait);
		await HeaderCell(cut, "Age").ClickAsync(new MouseEventArgs());
		Assert.Single(store.States["people"].Sorts!);

		await cut.InvokeAsync(() => cut.Instance.ResetStateAsync());

		Assert.Empty(store.States["people"].Sorts!);
	}

	// ── The theme's table choice ──

	[Fact]
	public void TheThemesLook_IsTheTablesUnlessItHasItsOwn()
	{
		MokaTheme theme = MokaTheme.Red.WithTables(MokaTableLook.SoftStripes);

		Assert.Contains("moka-table-wrapper--look-soft-stripes", Themed(theme).Find(".moka-table-wrapper").ClassList);
		Assert.Contains("moka-table-wrapper--look-cards", Themed(theme, p => p.Add(t => t.Look, MokaTableLook.Cards)).Find(".moka-table-wrapper").ClassList);
		Assert.DoesNotContain("moka-table-wrapper--look-soft-stripes", Themed(MokaTheme.Dark.WithTables(MokaTableLook.SoftStripes)).Find(".moka-table-wrapper").ClassList);
	}

	[Fact]
	public void TheThemesDensity_IsTheTablesUnlessItHasItsOwn()
	{
		MokaTheme theme = MokaTheme.Red.WithTableDensity(MokaTableDensity.Comfortable);

		Assert.Contains("moka-table--comfortable", Themed(theme).Find("table").ClassList);
		Assert.Contains("moka-table--standard", Themed(theme, p => p.Add(t => t.Density, MokaTableDensity.Standard)).Find("table").ClassList);
	}

	private IRenderedComponent<MokaTable<Person>> Themed(MokaTheme theme, Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.AddCascadingValue(theme);
			p.Add(t => t.Items, People);
			p.Add(t => t.ChildContent, DefaultColumns);
			extra?.Invoke(p);
		});

	private IRenderedComponent<MokaTable<Person>> Table(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null,
		params Col[] columns) =>
		Table(columns.Length > 0 ? Columns(columns) : DefaultColumns, extra);

	private IRenderedComponent<MokaTable<Person>> Table(
		RenderFragment columns, Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.ChildContent, columns);
			extra?.Invoke(p);
		});

	private sealed class BoundStateHost : ComponentBase
	{
		public MokaTableState? State { get; private set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaTable<Person>>(0);
			builder.AddComponentParameter(1, nameof(MokaTable<Person>.Items), People);
			builder.AddComponentParameter(2, nameof(MokaTable<Person>.State), State);
			builder.AddComponentParameter(3, nameof(MokaTable<Person>.StateChanged),
				EventCallback.Factory.Create<MokaTableState>(this, state => State = state));
			builder.AddComponentParameter(4, nameof(MokaTable<Person>.ChildContent), DefaultColumns);
			builder.CloseComponent();
		}
	}

	private sealed class MemoryStore : IMokaTableStateStore
	{
		public Dictionary<string, MokaTableState> States { get; } = [];

		public ValueTask<MokaTableState?> LoadAsync(string key, CancellationToken cancellationToken) =>
			new(States.GetValueOrDefault(key));

		public ValueTask SaveAsync(string key, MokaTableState state, CancellationToken cancellationToken)
		{
			States[key] = state;
			return ValueTask.CompletedTask;
		}
	}
}
