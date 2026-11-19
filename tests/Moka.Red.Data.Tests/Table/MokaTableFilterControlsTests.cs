using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Data.Table;
using Moka.Red.Navigation.Tabs;
using Moka.Red.Primitives.SegmentedControl;
using Moka.Red.Tests.Shared;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Table;

// Every filter control: the typed row, the heading menus, the chips, the quick filters, the facets and the builder, and
// the typed values they hand a server.
public sealed class MokaTableFilterControlsTests : BunitContext
{
	private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
	private readonly FakeClock _clock = new();
	private readonly BunitJSModuleInterop _module;

	public MokaTableFilterControlsTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(TableModule);
		_module.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton<TimeProvider>(_clock);
	}

	private static Col Senior => new("Senior", static x => x.Age > 40) { Filterable = true };

	// ── The typed row ──

	[Fact]
	public void TheRow_DrawsAnInputThatFitsEachColumn()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(columns:
			[Name with { Filterable = true }, Team with { Filterable = true, FilterType = MokaColumnFilterType.Select }, Age with { Filterable = true }, Senior]);

		Assert.Equal("text", cut.Find("input[aria-label='Filter by Name']").GetAttribute("type"));
		Assert.Equal(["number", "number"], cut.FindAll(".moka-table-filter-range input").Select(i => i.GetAttribute("type")));
		Assert.Equal(["", "Engineering", "Research"], cut.Find("select[aria-label='Filter by Team']").QuerySelectorAll("option").Select(o => o.GetAttribute("value")));
		Assert.Equal(["", "true", "false"], cut.Find("select[aria-label='Filter by Senior']").QuerySelectorAll("option").Select(o => o.GetAttribute("value")));
	}

	[Fact]
	public void TheRow_IsNotDrawn_WhileNoShownColumnTakesAFilter() =>
		Assert.Empty(Table().FindAll(".moka-table-filter-row"));

	[Fact]
	public async Task ANumberRange_FiltersBetweenItsEnds_AfterTheWait()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.StateChanged, s => raised = s), Name, Age with { Filterable = true });

		await cut.Find("input[aria-label='Age from']").InputAsync(new ChangeEventArgs { Value = "37" });
		await cut.Find("input[aria-label='Age to']").InputAsync(new ChangeEventArgs { Value = "44" });
		Assert.Equal(People.Length, DataRows(cut).Count);

		_clock.Advance(TimeSpan.FromMilliseconds(300));

		cut.WaitForAssertion(() => Assert.Equal(["Alan"], ColumnTexts(cut, 0)), Wait);
		Assert.Equal(new MokaTableFilter("Age", MokaFilterOperator.Between, ["37", "44"]), Assert.Single(raised!.Filters));
	}

	[Fact]
	public async Task OneEndOfARange_IsAtLeastOrAtMost()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.StateChanged, s => raised = s), Name, Age with { Filterable = true });

		await cut.Find("input[aria-label='Age to']").InputAsync(new ChangeEventArgs { Value = "40" });
		_clock.Advance(TimeSpan.FromMilliseconds(300));

		cut.WaitForAssertion(() => Assert.Equal(["Ada", "Barbara"], ColumnTexts(cut, 0)), Wait);
		Assert.Equal(new MokaTableFilter("Age", MokaFilterOperator.AtMost, "40"), Assert.Single(raised!.Filters));
	}

	[Fact]
	public void AListOrYesAndNo_AppliesAtOnce()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(columns: [Name, Senior]);

		cut.Find("select[aria-label='Filter by Senior']").Change("true");

		Assert.Equal(["Grace", "Alan"], ColumnTexts(cut, 0));
	}

	[Fact]
	public async Task AListOfSeveral_OpensThePopupFromTheRow()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(columns: [Name, Team with { Filterable = true, FilterType = MokaColumnFilterType.MultiSelect }]);
		IElement button = cut.Find(".moka-table-filter-choices");
		Assert.Equal("All", button.TextContent.Trim());

		await button.ClickAsync(new MouseEventArgs());

		Assert.Equal("true", cut.Find(".moka-table-filter-choices").GetAttribute("aria-expanded"));
		Assert.Equal(2, cut.FindAll(".moka-table-filter-popup input[type='checkbox']").Count);
	}

	// ── Heading menus ──

	[Fact]
	public void Menus_DrawANamedFilterButton_InEachFilterableHeading()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.FilterControls, MokaTableFilterControls.Menus),
			Name with { Filterable = true }, Team);

		IElement button = Assert.Single(cut.FindAll(".moka-table-filter-btn"));
		Assert.Equal("Filter by Name", button.GetAttribute("aria-label"));
		Assert.Equal("dialog", button.GetAttribute("aria-haspopup"));
		Assert.Equal("false", button.GetAttribute("aria-expanded"));
		Assert.NotNull(button.Closest(".moka-tooltip-trigger"));
	}

	[Fact]
	public async Task AFilterButton_OpensThePopupInTheTopLayer_AgainstItself()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.FilterControls, MokaTableFilterControls.Menus), Name with { Filterable = true });

		await cut.Find(".moka-table-filter-btn").ClickAsync(new MouseEventArgs());

		IElement popup = cut.Find(".moka-table-filter-popup");
		Assert.Equal("dialog", popup.GetAttribute("role"));
		Assert.Equal("manual", popup.GetAttribute("popover"));
		JSRuntimeInvocation open = Assert.Single(_module.Invocations["openPopup"]);
		Assert.StartsWith("[data-moka-filter-for=", (string)open.Arguments[4]!, StringComparison.Ordinal);
		Assert.Equal(false, open.Arguments[7]);
	}

	[Fact]
	public async Task ThePopup_AppliesAnOperatorAndAValue_AndGivesFocusBack()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.FilterControls, MokaTableFilterControls.Menus), Name with { Filterable = true });
		await cut.Find(".moka-table-filter-btn").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-table-filter-popup select").ChangeAsync(new ChangeEventArgs { Value = nameof(MokaFilterOperator.StartsWith) });
		await cut.Find(".moka-table-filter-popup input").ChangeAsync(new ChangeEventArgs { Value = "A" });
		await cut.Find(".moka-table-filter-form").SubmitAsync();

		Assert.Equal(["Ada", "Alan"], ColumnTexts(cut, 0));
		Assert.Empty(cut.FindAll(".moka-table-filter-popup"));
		Assert.Contains("moka-table-filter-btn--on", cut.Find(".moka-table-filter-btn").ClassList);
		cut.WaitForAssertion(() => Assert.Contains(_module.Invocations["focusFirst"],
			call => ((string[])call.Arguments[1]!)[0].StartsWith("[data-moka-filter-for=", StringComparison.Ordinal)));
	}

	[Fact]
	public async Task ThePopupForAList_PicksAnyOfTheTicked()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
				.Add(t => t.FilterControls, MokaTableFilterControls.Menus)
				.Add(t => t.StateChanged, s => raised = s),
			Name, Team with { Filterable = true, FilterType = MokaColumnFilterType.Select });
		await cut.Find(".moka-table-filter-btn").ClickAsync(new MouseEventArgs());

		await cut.FindAll(".moka-table-filter-popup input[type='checkbox']")[1].ChangeAsync(new ChangeEventArgs { Value = true });
		await cut.Find(".moka-table-filter-form").SubmitAsync();

		Assert.Equal(["Grace", "Barbara"], ColumnTexts(cut, 0));
		Assert.Equal(new MokaTableFilter("Team", MokaFilterOperator.IsAnyOf, ["Research"]), Assert.Single(raised!.Filters));
	}

	[Fact]
	public async Task Escape_ClosesThePopup_AndClearRemovesTheFilter()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
				.Add(t => t.FilterControls, MokaTableFilterControls.Menus)
				.Add(t => t.State, new MokaTableState { Filters = [new MokaTableFilter("Name", MokaFilterOperator.Contains, "a")] }),
			Name with { Filterable = true });
		await cut.Find(".moka-table-filter-btn").ClickAsync(new MouseEventArgs());

		await cut.InvokeAsync(() => cut.FindComponent<MokaTableFilterPopup<Person>>().Instance.Dismiss());
		Assert.Empty(cut.FindAll(".moka-table-filter-popup"));

		await cut.Find(".moka-table-filter-btn").ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-table-filter-actions button")[0].ClickAsync(new MouseEventArgs());

		Assert.Equal(People.Length, DataRows(cut).Count);
	}

	// ── Chips ──

	[Fact]
	public async Task Chips_ShowEachFilter_AndTakeOneAway()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.FilterControls, MokaTableFilterControls.Chips)
			.Add(t => t.State, new MokaTableState
			{
				Filters = [new MokaTableFilter("Team", MokaFilterOperator.Is, "Research"), new MokaTableFilter("Age", MokaFilterOperator.AtLeast, "40")]
			}));
		Assert.Equal(["Grace"], ColumnTexts(cut, 0));
		Assert.Equal(["Team is Research", "Age is at least 40"], cut.FindAll(".moka-table-chip-text").Select(e => e.TextContent));

		IElement remove = cut.FindAll(".moka-table-chip-remove")[1];
		Assert.Equal("Remove the filter Age is at least 40", remove.GetAttribute("aria-label"));
		await remove.ClickAsync(new MouseEventArgs());

		Assert.Equal(["Grace", "Barbara"], ColumnTexts(cut, 0));
		Assert.Single(cut.FindAll(".moka-table-chip"));
	}

	[Fact]
	public async Task ClearAll_TakesEveryFilterAway()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.FilterControls, MokaTableFilterControls.Chips)
			.Add(t => t.State, new MokaTableState { Filters = [new MokaTableFilter("Team", MokaFilterOperator.Is, "Research")] }));

		await cut.FindAll(".moka-table-chips button")[^1].ClickAsync(new MouseEventArgs());

		Assert.Equal(People.Length, DataRows(cut).Count);
		Assert.Empty(cut.FindAll(".moka-table-chips"));
	}

	// The default row draws nothing while no column takes a filter, so a filter the state names stays.
	[Fact]
	public void AFilterFromTheState_StaysWhileTheRowDrawsNothing()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ShowColumnToggle, true)
			.Add(t => t.State, new MokaTableState { Filters = [new MokaTableFilter("Team", MokaFilterOperator.Is, "Research")] }));

		cut.Render(p => p.Add(t => t.ChildContent, Columns(Name, Team, Age with { Visible = false }, City)));

		Assert.Equal(["Grace", "Barbara"], ColumnTexts(cut, 0));
	}

	// ── Quick filters ──

	private static readonly IReadOnlyList<MokaTableQuickFilter> Quick =
	[
		new("Engineering", [new MokaTableFilter("Team", MokaFilterOperator.Is, "Engineering")]),
		new("Over 40", [new MokaTableFilter("Age", MokaFilterOperator.GreaterThan, "40")])
	];

	[Fact]
	public void QuickFilters_AreSegments_WithAnAllChoiceAndCounts()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.QuickFilters, Quick));

		IReadOnlyList<IRenderedComponent<MokaSegment<string>>> segments = cut.FindComponents<MokaSegment<string>>();
		Assert.Equal(["All", "Engineering", "Over 40"], segments.Select(s => s.Instance.Text));
		Assert.Equal([4, 2, 2], segments.Select(s => s.Instance.Count ?? -1));
	}

	[Fact]
	public async Task PickingAQuickFilter_NarrowsTheRows_AndGoesInTheState()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.QuickFilters, Quick).Add(t => t.StateChanged, s => raised = s));

		await cut.InvokeAsync(() => cut.FindComponent<MokaSegmentedControl<string>>().Instance.ValueChanged.InvokeAsync("Over 40"));

		Assert.Equal(["Grace", "Alan"], ColumnTexts(cut, 0));
		Assert.Equal("Over 40", raised!.QuickFilter);
		Assert.Empty(raised.Filters);
	}

	[Fact]
	public void AQuickFilter_NarrowsOnTop_EvenWhenFiltersJoinByAny()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.QuickFilters, Quick)
			.Add(t => t.State, new MokaTableState
			{
				QuickFilter = "Engineering",
				FilterJoin = MokaFilterJoin.Any,
				Filters = [new MokaTableFilter("Name", MokaFilterOperator.Is, "Ada"), new MokaTableFilter("Name", MokaFilterOperator.Is, "Grace")]
			}));

		Assert.Equal(["Ada"], ColumnTexts(cut, 0));
	}

	[Fact]
	public void TheTabsLook_DrawsTabs() =>
		Assert.Equal(["All", "Engineering", "Over 40"], Table(p => p.Add(t => t.QuickFilters, Quick).Add(t => t.QuickFilterLook, MokaQuickFilterLook.Tabs))
			.FindComponent<MokaTabs>().Instance.Items.Select(i => i.Text));

	// ── Facets ──

	[Fact]
	public void Facets_ListTheListColumnsValues_WithCounts()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.FilterControls, MokaTableFilterControls.Facets),
			Name with { Filterable = true }, Team with { Filterable = true, FilterType = MokaColumnFilterType.Select }, Senior);

		Assert.Equal(["Team", "Senior"], cut.FindAll(".moka-table-facet-title").Select(e => e.TextContent));
		Assert.Equal(["Engineering", "Research", "Yes", "No"], cut.FindAll(".moka-table-facet-text").Select(e => e.TextContent));
		Assert.Equal(["2", "2", "2", "2"], cut.FindAll(".moka-table-facet-count").Select(e => e.TextContent));
	}

	// A facet counts with every filter but its own, so its other values still say what they would give.
	[Fact]
	public async Task TickingAValue_FiltersTheRows_AndCountsTheOtherFacetsAgain()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.FilterControls, MokaTableFilterControls.Facets),
			Name, Team with { Filterable = true, FilterType = MokaColumnFilterType.Select }, Senior);

		await cut.FindAll(".moka-table-facet input")[1].ChangeAsync(new ChangeEventArgs { Value = true });

		Assert.Equal(["Grace", "Barbara"], ColumnTexts(cut, 0));
		Assert.Equal(["2", "2", "1", "1"], cut.FindAll(".moka-table-facet-count").Select(e => e.TextContent));
	}

	[Fact]
	public void FacetsFromAServer_AreAskedFor_AndShowItsCounts()
	{
		List<MokaTableRequest> requests = [];
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.FilterControls, MokaTableFilterControls.Facets)
			.Add(t => t.FacetsFromServer, true)
			.Add(t => t.QuickFilters, Quick)
			.Add(t => t.ChildContent, Columns(Name, Team with { Filterable = true, FilterType = MokaColumnFilterType.Select }))
			.Add(t => t.ServerData, request =>
			{
				requests.Add(request);
				return Task.FromResult(new MokaTableResult<Person>
				{
					Items = People,
					TotalItems = 40,
					Facets = new Dictionary<string, IReadOnlyList<MokaFacetValue>> { ["Team"] = [new MokaFacetValue("Engineering", "Engineering", 25), new MokaFacetValue("Sales", "Sales", 15)] },
					QuickFilterCounts = new Dictionary<string, int> { ["Engineering"] = 25, ["Over 40"] = 9 }
				});
			}));

		cut.WaitForAssertion(() => Assert.Equal(["25", "15"], cut.FindAll(".moka-table-facet-count").Select(e => e.TextContent)));
		Assert.Equal(["Team"], requests[0].Facets);
		Assert.Equal(Quick, requests[0].QuickFilterCounts);
		Assert.Equal([40, 25, 9], cut.FindComponents<MokaSegment<string>>().Select(s => s.Instance.Count ?? -1));
	}

	[Fact]
	public void WithoutFacetsFromTheServer_TheValuesComeFromTheRows_Uncounted()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.FilterControls, MokaTableFilterControls.Facets)
			.Add(t => t.ChildContent, Columns(Name, Team with { Filterable = true, FilterType = MokaColumnFilterType.Select }))
			.Add(t => t.ServerData, request => Task.FromResult(new MokaTableResult<Person> { Items = People, TotalItems = 40 })));

		cut.WaitForAssertion(() => Assert.Equal(["Engineering", "Research"], cut.FindAll(".moka-table-facet-text").Select(e => e.TextContent)));
		Assert.Empty(cut.FindAll(".moka-table-facet-count"));
	}

	// ── The builder ──

	[Fact]
	public async Task TheBuilder_OpensFromTheToolbar_AndAppliesWholeConditions()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.FilterControls, MokaTableFilterControls.Builder),
			Name with { Filterable = true }, Age with { Filterable = true });
		IElement toggle = cut.Find(".moka-table-builder-toggle");
		Assert.Equal("false", toggle.GetAttribute("aria-expanded"));

		await toggle.ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-table-builder-foot button")[0].ClickAsync(new MouseEventArgs());
		IElement row = cut.Find(".moka-table-builder-row");
		await row.QuerySelectorAll("select")[0].ChangeAsync(new ChangeEventArgs { Value = "Age" });
		await cut.Find(".moka-table-builder-row").QuerySelectorAll("select")[1].ChangeAsync(new ChangeEventArgs { Value = nameof(MokaFilterOperator.AtLeast) });
		Assert.Equal(People.Length, DataRows(cut).Count);

		await cut.Find(".moka-table-builder-row input").ChangeAsync(new ChangeEventArgs { Value = "40" });

		Assert.Equal(["Grace", "Alan"], ColumnTexts(cut, 0));
		Assert.Equal("Filters (1)", cut.Find(".moka-table-builder-toggle").TextContent.Trim());
	}

	[Fact]
	public async Task TheBuilder_JoinsByAny_AndTheServerHearsIt()
	{
		List<MokaTableRequest> requests = [];
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.FilterControls, MokaTableFilterControls.Builder)
			.Add(t => t.State, new MokaTableState
			{
				Filters = [new MokaTableFilter("Name", MokaFilterOperator.Is, "Ada"), new MokaTableFilter("Age", MokaFilterOperator.Between, ["40", "50"])]
			})
			.Add(t => t.ChildContent, Columns(Name with { Filterable = true }, Age with { Filterable = true }))
			.Add(t => t.ServerData, request =>
			{
				requests.Add(request);
				return Task.FromResult(new MokaTableResult<Person> { Items = People, TotalItems = 4 });
			}));
		cut.WaitForAssertion(() => Assert.NotEmpty(requests));
		await cut.Find(".moka-table-builder-toggle").ClickAsync(new MouseEventArgs());
		Assert.Equal(2, cut.FindAll(".moka-table-builder-row").Count);

		await cut.FindAll(".moka-table-builder-join input")[1].ChangeAsync(new ChangeEventArgs { Value = "on" });

		Assert.Equal(MokaFilterJoin.Any, requests[^1].FilterJoin);
		Assert.Equal(["40", "50"], requests[^1].Filters[1].Values);
	}

	[Fact]
	public void TypedValues_ReachTheServer_InTheInvariantCulture()
	{
		List<MokaTableRequest> requests = [];
		Render<MokaTable<Person>>(p => p
			.Add(t => t.State, new MokaTableState { Filters = [new MokaTableFilter("Age", MokaFilterOperator.Between, ["37.5", "44"])] })
			.Add(t => t.ChildContent, Columns(Name, Age with { Filterable = true }))
			.Add(t => t.ServerData, request =>
			{
				requests.Add(request);
				return Task.FromResult(new MokaTableResult<Person> { Items = People, TotalItems = 4 });
			}));

		WaitForAssertion(() => Assert.NotEmpty(requests));
		Assert.Equal(new MokaTableFilter("Age", MokaFilterOperator.Between, ["37.5", "44"]), Assert.Single(requests[0].Filters));
	}

	// ── Dates are compared by the day ──

	[Fact]
	public void ADayAlone_ComparesATimeByItsDay()
	{
		DateTime afternoon = new(2026, 11, 14, 15, 30, 0, DateTimeKind.Utc);
		var filter = new MokaTableFilter("At", MokaFilterOperator.Between, ["2026-11-01", "2026-11-14"]);

		Assert.True(TableFilterMatch.Matches(filter, afternoon, afternoon.ToString("O", System.Globalization.CultureInfo.InvariantCulture)));
		Assert.True(TableFilterMatch.Matches(new MokaTableFilter("At", MokaFilterOperator.Is, "2026-11-14"), new DateTimeOffset(afternoon), string.Empty));
		Assert.False(TableFilterMatch.Matches(new MokaTableFilter("At", MokaFilterOperator.LessThan, "2026-11-14"), afternoon, string.Empty));
	}

	private static void WaitForAssertion(Action assertion) =>
		Assert.True(SpinWait.SpinUntil(() =>
		{
			try
			{
				assertion();
				return true;
			}
			catch (Xunit.Sdk.XunitException)
			{
				return false;
			}
		}, Wait));

	private IRenderedComponent<MokaTable<Person>> Table(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null,
		params Col[] columns) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.ChildContent, columns.Length > 0 ? Columns(columns) : DefaultColumns);
			extra?.Invoke(p);
		});
}
