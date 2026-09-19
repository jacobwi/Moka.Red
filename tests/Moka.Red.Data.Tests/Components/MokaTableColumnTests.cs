using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// Moving, hiding and showing columns, and the cells each row renders for a column.
public class MokaTableColumnTests : BunitContext
{
	private static readonly TimeSpan Debounce = TimeSpan.FromSeconds(5);

	public MokaTableColumnTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// The drop handler read on-screen positions as indexes into the order the columns registered
	// in. The two agree until the first move, so the second drag moved the wrong column.
	[Fact]
	public async Task SecondColumnDrag_MovesTheDraggedColumn()
	{
		var moves = new List<(int OldIndex, int NewIndex)>();
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.ColumnReorderable, true)
			.Add(t => t.OnColumnReordered, e => moves.Add(e)));

		await DragHeaderAsync(cut, "Name", "Age");
		Assert.Equal(["Team", "Age", "Name", "City"], HeaderTitles(cut));

		await DragHeaderAsync(cut, "City", "Team");
		Assert.Equal(["City", "Team", "Age", "Name"], HeaderTitles(cut));
		Assert.Equal(["London", "Engineering", "36", "Ada"], FirstRowTexts(cut));
		Assert.Equal([(0, 2), (3, 0)], moves);
	}

	// A hidden column shifts every position after it, so the same misreading hit the first drag.
	[Fact]
	public async Task ColumnDrag_WithAHiddenColumnInFront_MovesTheDraggedColumn()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(
			p => p.Add(t => t.ColumnReorderable, true),
			Name with { Visible = false }, Team, Age, City);

		await DragHeaderAsync(cut, "City", "Team");

		Assert.Equal(["City", "Team", "Age"], HeaderTitles(cut));
	}

	// Every header had drag handlers, so a row or a file dragged across the header went to .NET
	// on every dragover and could mark a header, with column reordering off.
	[Fact]
	public async Task Headers_HaveNoDragHandlers_WhenColumnsCannotBeReordered()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();
		IElement name = HeaderCell(cut, "Name");

		await Assert.ThrowsAsync<MissingEventHandlerException>(() => name.DragStartAsync(new DragEventArgs()));
		await Assert.ThrowsAsync<MissingEventHandlerException>(() => name.DragOverAsync(new DragEventArgs()));
		await Assert.ThrowsAsync<MissingEventHandlerException>(() => name.DropAsync(new DragEventArgs()));
	}

	// dragover fires many times a second. moka-table.js marks the header under the pointer.
	[Fact]
	public async Task DragOverAHeader_DoesNotCallDotNet()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.ColumnReorderable, true));

		await Assert.ThrowsAsync<MissingEventHandlerException>(
			() => HeaderCell(cut, "Age").DragOverAsync(new DragEventArgs()));
	}

	// With no dragend handler a cancelled drag stayed in progress, and a later drop on a header,
	// from any drag, moved the column of the cancelled one.
	[Fact]
	public async Task CancelledColumnDrag_MovesNothingOnALaterDrop()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.ColumnReorderable, true));

		await HeaderCell(cut, "Name").DragStartAsync(new DragEventArgs());
		await HeaderCell(cut, "Name").DragEndAsync(new DragEventArgs());
		await HeaderCell(cut, "Age").DropAsync(new DragEventArgs());

		Assert.Equal(["Name", "Team", "Age", "City"], HeaderTitles(cut));
		Assert.Empty(cut.FindAll(".moka-table-header--drag-over"));
	}

	[Fact]
	public void DropMarkerScript_IsBoundOnlyWhileSomethingCanBeDragged()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();
		Assert.DoesNotContain(module.Invocations, i => i.Identifier == "initReorderDrag");

		cut.Render(p => p.Add(t => t.ColumnReorderable, true));
		module.VerifyInvoke("initReorderDrag");

		cut.Render(p => p.Add(t => t.ColumnReorderable, false));
		module.VerifyInvoke("disposeReorderDrag");
	}

	// The toggle kept a list of titles it had hidden, and Visible="false" was a second switch it
	// could not reach, so a column that started hidden stayed hidden.
	[Fact]
	public async Task ColumnToggle_ShowsAColumnThatStartedHidden()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(
			p => p.Add(t => t.ShowColumnToggle, true),
			Name, Team, Age, City with { Visible = false });
		Assert.DoesNotContain("City", HeaderTitles(cut));

		await ToggleColumnAsync(cut, "City");

		Assert.Contains("City", HeaderTitles(cut));
		Assert.NotNull(MenuItem(cut, "City").QuerySelector("input:checked"));
	}

	// Columns without a title all went under the key "", so hiding one hid all of them.
	[Fact]
	public async Task ColumnToggle_HidesOnlyTheUntitledColumnItWasUsedOn()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(
			p => p.Add(t => t.ShowColumnToggle, true),
			Name,
			new Col(null, static x => x.Team) { HeaderTemplate = Marker("team-header") },
			new Col(null, static x => x.City) { HeaderTemplate = Marker("city-header") });

		await OpenColumnMenuAsync(cut);
		IElement firstUntitled = cut.FindAll(".moka-table-column-menu-item")
			.First(item => string.Equals(item.TextContent.Trim(), "Unnamed", StringComparison.Ordinal));
		await firstUntitled.QuerySelector("input")!.ChangeAsync(new ChangeEventArgs());

		Assert.Empty(cut.FindAll(".team-header"));
		Assert.NotEmpty(cut.FindAll(".city-header"));
		Assert.Equal(2, cut.FindAll("thead tr:first-child th").Count);
	}

	[Fact]
	public async Task NewVisibleValueFromTheParent_ReplacesTheToggleChoice()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.ShowColumnToggle, true));
		await ToggleColumnAsync(cut, "City");
		Assert.DoesNotContain("City", HeaderTitles(cut));

		cut.Render(p => p.Add(t => t.ChildContent, Columns(Name, Team, Age, City with { Visible = false })));
		cut.Render(p => p.Add(t => t.ChildContent, Columns(Name, Team, Age, City)));

		Assert.Contains("City", HeaderTitles(cut));
	}

	// The table renders before its columns get their parameters, so it used to show a new Visible
	// value one render late.
	[Fact]
	public void ColumnHiddenByTheParent_GoesInTheSameRender()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();

		cut.Render(p => p.Add(t => t.ChildContent, Columns(Name, Team, Age, City with { Visible = false })));

		Assert.Equal(["Name", "Team", "Age"], HeaderTitles(cut));
	}

	// A hidden column has no filter control, yet its filter went on hiding rows.
	[Fact]
	public async Task HidingAFilteredColumn_DropsItsFilter()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(
			p => p.Add(t => t.ShowFilters, true).Add(t => t.ShowColumnToggle, true),
			Name, Team with { Filterable = true }, Age, City);
		await FilterInput(cut, "Team").InputAsync(new ChangeEventArgs { Value = "Research" });
		cut.WaitForAssertion(() => Assert.Equal(2, DataRows(cut).Count), Debounce);

		await ToggleColumnAsync(cut, "Team");

		Assert.Equal(People.Length, DataRows(cut).Count);
	}

	// Hiding the filter row left its filters on, with no control left to clear them.
	[Fact]
	public async Task HidingTheFilterRow_DropsTheFilters()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(
			p => p.Add(t => t.ShowFilters, true),
			Name, Team with { Filterable = true }, Age, City);
		await FilterInput(cut, "Team").InputAsync(new ChangeEventArgs { Value = "Research" });
		cut.WaitForAssertion(() => Assert.Equal(2, DataRows(cut).Count), Debounce);

		cut.Render(p => p.Add(t => t.ShowFilters, false));

		cut.WaitForAssertion(() => Assert.Equal(People.Length, DataRows(cut).Count), Debounce);
		cut.Render(p => p.Add(t => t.ShowFilters, true));
		Assert.Equal("", FilterInput(cut, "Team").GetAttribute("value") ?? "");
	}

	// A column whose title went away kept sorting the rows under a title no column had.
	[Fact]
	public void ColumnLosingItsTitle_TakesItsSortAlong()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.SortColumn, "Age")
			.Add(t => t.SortDirection, MokaSortDirection.Descending));
		Assert.Equal(["45", "41", "36", "29"], ColumnTexts(cut, 2));

		cut.Render(p => p.Add(t => t.ChildContent, Columns(Name, Team, Age with { Title = null }, City)));

		cut.WaitForAssertion(() => Assert.Equal(["36", "45", "41", "29"], ColumnTexts(cut, 2)));
	}

	[Fact]
	public async Task ColumnHiddenByTheParent_StopsFilteringServerData()
	{
		var requests = new List<MokaTableState>();
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.ServerData, state =>
			{
				requests.Add(state);
				return Task.FromResult(new MokaTableResult<Person> { Items = People, TotalItems = People.Length });
			})
			.Add(t => t.ShowFilters, true)
			.Add(t => t.ChildContent, Columns(Name, Team with { Filterable = true })));
		await FilterInput(cut, "Team").InputAsync(new ChangeEventArgs { Value = "Research" });
		cut.WaitForAssertion(() => Assert.Contains("Team", requests[^1].ColumnFilters.Keys), Debounce);

		cut.Render(p => p.Add(t => t.ChildContent, Columns(Name, Team with { Filterable = true, Visible = false })));

		cut.WaitForAssertion(() => Assert.Empty(requests[^1].ColumnFilters), Debounce);
	}

	// Only the header and data cells got these classes, so on a narrow screen the filter and
	// aggregate rows kept a cell the other rows had dropped, and a sticky column slid off.
	[Fact]
	public void StickyAndHideOnMobile_ReachTheFilterAndAggregateCells()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(
			p => p.Add(t => t.ShowFilters, true),
			Name with { Sticky = true, Filterable = true, Aggregate = MokaAggregateType.Count },
			Team with { HideOnMobile = true, Filterable = true, Aggregate = MokaAggregateType.Count });

		IReadOnlyList<IElement> filterCells = cut.FindAll(".moka-table-filter-row td");
		AssertColumnClasses(filterCells[0], sticky: true, hideOnMobile: false);
		AssertColumnClasses(filterCells[1], sticky: false, hideOnMobile: true);

		IReadOnlyList<IElement> aggregateCells = cut.FindAll("tfoot td");
		AssertColumnClasses(aggregateCells[0], sticky: true, hideOnMobile: false);
		AssertColumnClasses(aggregateCells[1], sticky: false, hideOnMobile: true);
	}

	[Fact]
	public void StickyAndHideOnMobile_ReachTheSkeletonCells()
	{
		var pending = new TaskCompletionSource<MokaTableResult<Person>>();
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.ServerData, _ => pending.Task)
			.Add(t => t.ChildContent, Columns(Name with { Sticky = true }, Team with { HideOnMobile = true })));

		IElement[] cells = cut.FindAll(".moka-table-skeleton-row")[0].QuerySelectorAll("td").ToArray();
		AssertColumnClasses(cells[0], sticky: true, hideOnMobile: false);
		AssertColumnClasses(cells[1], sticky: false, hideOnMobile: true);
	}

	private IRenderedComponent<MokaTable<Person>> RenderTable(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null,
		params Col[] columns) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.ChildContent, columns.Length > 0 ? Columns(columns) : DefaultColumns);
			extra?.Invoke(p);
		});

	private static async Task DragHeaderAsync(IRenderedComponent<MokaTable<Person>> cut, string from, string onto)
	{
		await HeaderCell(cut, from).DragStartAsync(new DragEventArgs());
		await HeaderCell(cut, onto).DropAsync(new DragEventArgs());
	}

	private static async Task OpenColumnMenuAsync(IRenderedComponent<MokaTable<Person>> cut)
	{
		if (cut.FindAll(".moka-table-column-menu").Count == 0)
		{
			await cut.Find(".moka-table-column-toggle-btn").ClickAsync(new MouseEventArgs());
		}
	}

	private static async Task ToggleColumnAsync(IRenderedComponent<MokaTable<Person>> cut, string title)
	{
		await OpenColumnMenuAsync(cut);
		await MenuItem(cut, title).QuerySelector("input")!.ChangeAsync(new ChangeEventArgs());
	}

	private static IElement MenuItem(IRenderedComponent<MokaTable<Person>> cut, string title) =>
		cut.FindAll(".moka-table-column-menu-item")
			.First(item => string.Equals(item.TextContent.Trim(), title, StringComparison.Ordinal));

	private static IElement FilterInput(IRenderedComponent<MokaTable<Person>> cut, string title) =>
		cut.Find($"input[aria-label='Filter by {title}']");

	private static RenderFragment Marker(string cssClass) => builder =>
	{
		builder.OpenElement(0, "span");
		builder.AddAttribute(1, "class", cssClass);
		builder.CloseElement();
	};

	private static void AssertColumnClasses(IElement cell, bool sticky, bool hideOnMobile)
	{
		Assert.Equal(sticky, cell.ClassList.Contains("moka-table-cell--sticky"));
		Assert.Equal(hideOnMobile, cell.ClassList.Contains("moka-table-cell--hide-mobile"));
	}
}
