using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// OnRowReordered reports positions in Items. Under a sort, a search or a filter the rows on
// screen are not in the order of Items, so the positions of the rows there moved the wrong item
// when applied to Items. Reordering is off in those states.
public class MokaTableRowReorderTests : BunitContext
{
	private static readonly TimeSpan Debounce = TimeSpan.FromSeconds(5);

	private readonly List<(Person Item, int OldIndex, int NewIndex)> _moves = [];

	public MokaTableRowReorderTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task RowHandles_AreOff_WhileSorted()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();

		await HeaderCell(cut, "Age").ClickAsync(new MouseEventArgs());

		AssertHandlesOff(cut);
		await DragRowAsync(cut, 2, 0);
		Assert.Empty(_moves);
	}

	[Fact]
	public async Task RowHandles_AreOff_WhileASearchIsActive()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.Searchable, true));

		await cut.Find(".moka-table-search input").InputAsync(new ChangeEventArgs { Value = "Research" });
		cut.WaitForAssertion(() => Assert.Equal(2, DataRows(cut).Count), Debounce);

		AssertHandlesOff(cut);
		await DragRowAsync(cut, 1, 0);
		Assert.Empty(_moves);
	}

	[Fact]
	public async Task RowHandles_AreOff_WhileAColumnFilterIsActive()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.ShowFilters, true));

		await cut.Find("input[aria-label='Filter by Team']").InputAsync(new ChangeEventArgs { Value = "Research" });
		cut.WaitForAssertion(() => Assert.Equal(2, DataRows(cut).Count), Debounce);

		AssertHandlesOff(cut);
		await DragRowAsync(cut, 1, 0);
		Assert.Empty(_moves);
	}

	[Fact]
	public async Task RowHandles_ComeBack_WhenTheSortIsCleared()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();

		// Ascending, descending, then back to the original order.
		for (int i = 0; i < 3; i++)
		{
			await HeaderCell(cut, "Age").ClickAsync(new MouseEventArgs());
		}

		Assert.All(Handles(cut), h => Assert.Equal("true", h.GetAttribute("draggable")));
		await DragRowAsync(cut, 2, 0);
		Assert.Equal((People[2], 2, 0), Assert.Single(_moves));
	}

	// The positions are Items positions, page offset included, so RemoveAt(OldIndex) followed by
	// Insert(NewIndex, Item) applies the move to Items.
	[Fact]
	public async Task RowMove_OnALaterPage_ReportsPositionsInItems()
	{
		Person[] rows = Enumerable.Range(1, 25)
			.Select(i => new Person(
				string.Create(CultureInfo.InvariantCulture, $"Person {i:00}"), "Engineering", 20 + i, "London"))
			.ToArray();
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(items: rows);
		await cut.Find(".moka-pagination button[title='Next page']").ClickAsync(new MouseEventArgs());

		await DragRowAsync(cut, 3, 0);

		Assert.Equal((rows[13], 13, 10), Assert.Single(_moves));
	}

	private IRenderedComponent<MokaTable<Person>> RenderTable(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null,
		Person[]? items = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, items ?? People);
			p.Add(t => t.RowReorderable, true);
			p.Add(t => t.OnRowReordered, e => _moves.Add(e));
			p.Add(t => t.ChildContent, Columns(Name, Team with { Filterable = true }, Age, City));
			extra?.Invoke(p);
		});

	private static IReadOnlyList<IElement> Handles(IRenderedComponent<MokaTable<Person>> cut) =>
		cut.FindAll("tbody td.moka-table-cell--reorder");

	private static void AssertHandlesOff(IRenderedComponent<MokaTable<Person>> cut) =>
		Assert.All(Handles(cut), h =>
		{
			Assert.Equal("false", h.GetAttribute("draggable"));
			Assert.Contains("moka-table-cell--reorder-off", h.ClassList);
			Assert.False(string.IsNullOrEmpty(h.GetAttribute("title")));
		});

	private static async Task DragRowAsync(IRenderedComponent<MokaTable<Person>> cut, int from, int onto)
	{
		await cut.Find(string.Create(CultureInfo.InvariantCulture,
				$"tbody tr[data-row-index='{from}'] td.moka-table-cell--reorder"))
			.DragStartAsync(new DragEventArgs());
		await cut.Find(string.Create(CultureInfo.InvariantCulture, $"tbody tr[data-row-index='{onto}']"))
			.DropAsync(new DragEventArgs());
	}
}
