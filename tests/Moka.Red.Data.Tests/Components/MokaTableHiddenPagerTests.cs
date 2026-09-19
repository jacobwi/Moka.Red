using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;

namespace Moka.Red.Data.Tests.Components;

/// <summary>
///     ShowPagination="false" used to hide only the pager. Rows were still cut at PageSize, and with
///     no pager and no public page parameter the rest could not be reached at all.
/// </summary>
public class MokaTableHiddenPagerTests : BunitContext
{
	private const int RowCount = 25;
	private const int DefaultPageSize = 10;

	// 15 people in Engineering, so a search for it still matches more than one page.
	private static readonly Person[] People = Enumerable.Range(1, RowCount)
		.Select(i => new Person(
			string.Create(CultureInfo.InvariantCulture, $"Person {i:00}"),
			i <= 15 ? "Engineering" : "Research",
			20 + i))
		.ToArray();

	public MokaTableHiddenPagerTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void HiddenPager_RendersEveryRow()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.ShowPagination, false));

		Assert.Equal(RowCount, DataRows(cut).Count);
		Assert.Empty(cut.FindAll(".moka-pagination"));
	}

	[Fact]
	public async Task HiddenPager_KeepsEveryRow_AfterSorting()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.ShowPagination, false));

		await AgeHeader(cut).ClickAsync(new MouseEventArgs());
		await AgeHeader(cut).ClickAsync(new MouseEventArgs());

		Assert.Equal(RowCount, DataRows(cut).Count);
		Assert.Equal("Person 25", FirstName(cut));
	}

	[Fact]
	public async Task HiddenPager_KeepsEveryMatch_AfterSearching()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.ShowPagination, false)
			.Add(t => t.Searchable, true));

		await cut.Find(".moka-table-search input").InputAsync(new ChangeEventArgs { Value = "Engineering" });

		// The search is debounced, so the reload lands a moment later.
		cut.WaitForAssertion(() => Assert.Equal(15, DataRows(cut).Count), TimeSpan.FromSeconds(5));
	}

	[Fact]
	public void HiddenPager_VirtualizedTable_GetsEveryRow()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.ShowPagination, false)
			.Add(t => t.Virtualize, true)
			.Add(t => t.Height, "400px"));

		// bUnit has no layout, so Virtualize renders every item it is handed.
		Assert.Equal(RowCount, DataRows(cut).Count);
	}

	[Fact]
	public async Task HiddenPager_ServerData_IsAskedForEveryRow()
	{
		var requests = new List<MokaTableState>();
		IRenderedComponent<MokaTable<Person>> cut = RenderServerTable(requests, p => p.Add(t => t.ShowPagination, false));

		Assert.Equal(RowCount, DataRows(cut).Count);
		Assert.All(requests, r => Assert.Equal(1, r.Page));
		Assert.True(requests[^1].PageSize >= RowCount);

		// The first load has no total yet: one call to learn it, one sized to it.
		Assert.InRange(requests.Count, 1, 2);

		requests.Clear();
		await AgeHeader(cut).ClickAsync(new MouseEventArgs());

		// With the total known, a reload is a single call for everything.
		MokaTableState sorted = Assert.Single(requests);
		Assert.Equal(1, sorted.Page);
		Assert.True(sorted.PageSize >= RowCount);
		Assert.Equal(RowCount, DataRows(cut).Count);
	}

	[Fact]
	public void HiddenPager_ServerDataAggregates_CoverEveryRow()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderServerTable([], p => p.Add(t => t.ShowPagination, false));

		string expected = People.Sum(x => x.Age).ToString(CultureInfo.CurrentCulture);
		Assert.Equal(expected, cut.Find(".moka-table-aggregate-value").TextContent.Trim());
	}

	[Fact]
	public async Task HiddenPager_SelectAll_SelectsEveryRow()
	{
		HashSet<Person>? selected = null;
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.ShowPagination, false)
			.Add(t => t.Selectable, true)
			.Add(t => t.SelectedItemsChanged, s => selected = s));

		IElement selectAll = cut.Find("thead input[type='checkbox']");
		Assert.Equal("Select all rows", selectAll.GetAttribute("aria-label"));

		await selectAll.ChangeAsync(new ChangeEventArgs { Value = true });

		Assert.NotNull(selected);
		Assert.Equal(RowCount, selected.Count);
	}

	[Fact]
	public async Task HiddenPager_ArrowDown_MovesPastTheFirstPageOfRows()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.ShowPagination, false));

		await Cell(cut, DefaultPageSize - 1, 0).KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

		Assert.Equal("0", Cell(cut, DefaultPageSize, 0).GetAttribute("tabindex"));
	}

	[Fact]
	public async Task HiddenPager_RowReorder_ReportsRowsPastTheFirstPage()
	{
		(Person Item, int OldIndex, int NewIndex)? moved = null;
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.ShowPagination, false)
			.Add(t => t.RowReorderable, true)
			.Add(t => t.OnRowReordered, e => moved = e));

		await cut.Find("tbody tr[data-row-index='12'] td.moka-table-cell--reorder").DragStartAsync(new DragEventArgs());
		await cut.Find("tbody tr[data-row-index='2']").DropAsync(new DragEventArgs());

		Assert.NotNull(moved);
		Assert.Equal(People[12], moved.Value.Item);
		Assert.Equal(12, moved.Value.OldIndex);
		Assert.Equal(2, moved.Value.NewIndex);
	}

	[Fact]
	public async Task ShowPagination_ChangedAtRuntime_ReloadsTheRows()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();
		await cut.Find(".moka-pagination button[title='Next page']").ClickAsync(new MouseEventArgs());
		Assert.Equal("Person 11", FirstName(cut));

		cut.Render(p => p.Add(t => t.ShowPagination, false));

		Assert.Equal(RowCount, DataRows(cut).Count);
		Assert.Equal("Person 01", FirstName(cut));
		Assert.Empty(cut.FindAll(".moka-pagination"));

		cut.Render(p => p.Add(t => t.ShowPagination, true));

		Assert.Equal(DefaultPageSize, DataRows(cut).Count);
		Assert.Equal("Person 01", FirstName(cut));
		Assert.NotEmpty(cut.FindAll(".moka-pagination"));
	}

	private IRenderedComponent<MokaTable<Person>> RenderTable(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.ChildContent, Columns);
			extra?.Invoke(p);
		});

	// A well-behaved source: it pages exactly as asked and reports the full total.
	private IRenderedComponent<MokaTable<Person>> RenderServerTable(
		List<MokaTableState> requests,
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.ServerData, state =>
			{
				requests.Add(state);
				return Task.FromResult(new MokaTableResult<Person>
				{
					Items = People.Skip((state.Page - 1) * state.PageSize).Take(state.PageSize).ToList(),
					TotalItems = People.Length
				});
			});
			p.Add(t => t.ChildContent, Columns);
			extra?.Invoke(p);
		});

	private static IReadOnlyList<IElement> DataRows(IRenderedComponent<MokaTable<Person>> cut) =>
		cut.FindAll("tbody tr[data-row-index]");

	private static IElement Cell(IRenderedComponent<MokaTable<Person>> cut, int row, int col) =>
		cut.Find(string.Create(CultureInfo.InvariantCulture,
			$"tbody tr[data-row-index='{row}'] td[data-col-index='{col}']"));

	private static IElement AgeHeader(IRenderedComponent<MokaTable<Person>> cut) => cut.FindAll("thead th")[2];

	private static string FirstName(IRenderedComponent<MokaTable<Person>> cut) =>
		Cell(cut, 0, 0).TextContent.Trim();

	private static void Columns(RenderTreeBuilder builder)
	{
		builder.OpenComponent<MokaColumn<Person>>(0);
		builder.AddAttribute(1, nameof(MokaColumn<Person>.Title), "Name");
		builder.AddAttribute(2, nameof(MokaColumn<Person>.Field), (Func<Person, object?>)(static x => x.Name));
		builder.CloseComponent();

		builder.OpenComponent<MokaColumn<Person>>(10);
		builder.AddAttribute(11, nameof(MokaColumn<Person>.Title), "Team");
		builder.AddAttribute(12, nameof(MokaColumn<Person>.Field), (Func<Person, object?>)(static x => x.Team));
		builder.CloseComponent();

		builder.OpenComponent<MokaColumn<Person>>(20);
		builder.AddAttribute(21, nameof(MokaColumn<Person>.Title), "Age");
		builder.AddAttribute(22, nameof(MokaColumn<Person>.Field), (Func<Person, object?>)(static x => x.Age));
		builder.AddAttribute(23, nameof(MokaColumn<Person>.Aggregate), MokaAggregateType.Sum);
		builder.AddAttribute(24, nameof(MokaColumn<Person>.AggregateFormat), "0");
		builder.CloseComponent();
	}

	private sealed record Person(string Name, string Team, int Age);
}
