using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// The grid has one tab stop: the cell that last had focus. When the rows or the columns change
// under it, that cell can be gone, and then no cell had tabindex 0 and Tab skipped the table.
public class MokaTableTabStopTests : BunitContext
{
	private static readonly Person[] Rows = Enumerable.Range(1, 25)
		.Select(i => new Person(
			string.Create(CultureInfo.InvariantCulture, $"Person {i:00}"), "Engineering", 20 + i, "London"))
		.ToArray();

	public MokaTableTabStopTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// Virtualize renders only the rows in view, so the remembered cell's row can scroll out of the
	// rendered ones, and the first row is gone as soon as the user scrolls down. moka-table.js then
	// keeps a rendered cell in the tab order.
	[Fact]
	public void VirtualizedTable_HasTheScriptKeepATabStop()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.Virtualize, true)
			.Add(t => t.Height, "300px")
			.Add(t => t.ShowPagination, false));

		module.VerifyInvoke("watchTabStop");

		cut.Render(p => p.Add(t => t.Virtualize, false));
		module.VerifyInvoke("unwatchTabStop");
	}

	[Fact]
	public void TableThatRendersEveryRow_NeedsNoStandInTabStop()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;

		RenderTable();

		Assert.DoesNotContain(module.Invocations, i => i.Identifier == "watchTabStop");
	}

	[Fact]
	public async Task PageWithFewerRows_KeepsATabStop()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();
		await CellAt(cut, 9, 3).FocusInAsync(new FocusEventArgs());

		await PageButton(cut, "3").ClickAsync(new MouseEventArgs());

		IElement stop = Assert.Single(TabStops(cut));
		Assert.Equal("4", stop.ParentElement!.GetAttribute("data-row-index"));
		Assert.Equal("3", stop.GetAttribute("data-col-index"));
	}

	[Fact]
	public async Task FilterLeavingFewerRows_KeepsATabStop()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.ShowFilters, true));
		await CellAt(cut, 9, 0).FocusInAsync(new FocusEventArgs());

		await cut.Find("input[aria-label='Filter by Name']").InputAsync(new ChangeEventArgs { Value = "Person 0" });

		cut.WaitForAssertion(() => Assert.Equal(9, DataRows(cut).Count), TimeSpan.FromSeconds(5));
		Assert.Single(TabStops(cut));
	}

	[Fact]
	public async Task HidingTheFocusedColumn_KeepsATabStop()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.ShowColumnToggle, true));
		await CellAt(cut, 0, 3).FocusInAsync(new FocusEventArgs());

		await cut.Find(".moka-table-column-toggle-btn").ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-table-column-menu-item")
			.First(item => string.Equals(item.TextContent.Trim(), "City", StringComparison.Ordinal))
			.QuerySelector("input")!
			.ChangeAsync(new ChangeEventArgs());

		IElement stop = Assert.Single(TabStops(cut));
		Assert.Equal("2", stop.GetAttribute("data-col-index"));
	}

	private IRenderedComponent<MokaTable<Person>> RenderTable(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, Rows);
			p.Add(t => t.ChildContent, Columns(Name with { Filterable = true }, Team, Age, City));
			extra?.Invoke(p);
		});

	private static IElement CellAt(IRenderedComponent<MokaTable<Person>> cut, int row, int col) =>
		cut.Find(string.Create(CultureInfo.InvariantCulture,
			$"tbody tr[data-row-index='{row}'] td[data-col-index='{col}']"));

	private static IElement PageButton(IRenderedComponent<MokaTable<Person>> cut, string page) =>
		cut.FindAll(".moka-pagination-page")
			.First(b => string.Equals(b.TextContent.Trim(), page, StringComparison.Ordinal));
}
