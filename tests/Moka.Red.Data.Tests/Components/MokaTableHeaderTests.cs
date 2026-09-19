using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// Sorting from the header. It took a click on the header cell, which the keyboard cannot reach, and
// no header said how the table was sorted. A sortable header now holds a button, and the sorted
// column's header carries aria-sort.
public class MokaTableHeaderTests : BunitContext
{
	public MokaTableHeaderTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void SortableHeader_HoldsAButtonNamedByTheTitle()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();

		IElement button = SortButton(cut, "Name");
		Assert.Equal("button", button.GetAttribute("type"));
		Assert.Equal("Name", button.QuerySelector(".moka-table-header-text")?.TextContent.Trim());
		Assert.False(button.HasAttribute("aria-label"));
	}

	[Fact]
	public void HeaderThatCannotSort_HasNoButton()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(columns: [Name, Team with { Sortable = false }]);

		Assert.Empty(HeaderCell(cut, "Team").QuerySelectorAll("button"));
	}

	// Enter and Space on the button click it, and the click reaches the header's handler.
	[Fact]
	public async Task SortButton_Sorts_AndTheHeaderSaysHow()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();
		Assert.False(HeaderCell(cut, "Age").HasAttribute("aria-sort"));

		await SortButton(cut, "Age").ClickAsync(new MouseEventArgs());
		Assert.Equal("ascending", HeaderCell(cut, "Age").GetAttribute("aria-sort"));
		Assert.Equal(["Barbara", "Ada", "Alan", "Grace"], ColumnTexts(cut, 0));

		await SortButton(cut, "Age").ClickAsync(new MouseEventArgs());
		Assert.Equal("descending", HeaderCell(cut, "Age").GetAttribute("aria-sort"));

		await SortButton(cut, "Age").ClickAsync(new MouseEventArgs());
		Assert.False(HeaderCell(cut, "Age").HasAttribute("aria-sort"));
		Assert.All(cut.FindAll("thead th"), th => Assert.False(th.HasAttribute("aria-sort")));
	}

	// ARIA asks for aria-sort on one header at a time, so a multi-column sort names its first column.
	[Fact]
	public async Task MultiColumnSort_PutsAriaSortOnTheFirstColumn()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.MultiSort, true));

		await SortButton(cut, "Team").ClickAsync(new MouseEventArgs { ShiftKey = true });
		await SortButton(cut, "Age").ClickAsync(new MouseEventArgs { ShiftKey = true });

		Assert.Equal("ascending", HeaderCell(cut, "Team").GetAttribute("aria-sort"));
		Assert.False(HeaderCell(cut, "Age").HasAttribute("aria-sort"));
		Assert.Equal(["Ada", "Alan", "Barbara", "Grace"], ColumnTexts(cut, 0));
	}

	[Fact]
	public void SortFromTheParent_SetsAriaSort()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.SortColumn, "City")
			.Add(t => t.SortDirection, MokaSortDirection.Descending));

		Assert.Equal("descending", HeaderCell(cut, "City").GetAttribute("aria-sort"));
	}

	// A HeaderTemplate can hold its own controls, and a button cannot hold another control, so the
	// template stays outside and the button holds the sort icon under a label of its own.
	[Fact]
	public void HeaderTemplate_StaysOutsideTheSortButton()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(columns:
		[
			Name,
			City with
			{
				HeaderTemplate = b =>
				{
					b.OpenElement(0, "span");
					b.AddAttribute(1, "class", "city-header");
					b.AddContent(2, "Where");
					b.CloseElement();
				}
			}
		]);

		IElement header = cut.FindAll("thead tr:first-child th")[1];
		IElement button = header.QuerySelector("button.moka-table-sort-button")!;
		Assert.Contains(header.Children, child => child.ClassList.Contains("city-header"));
		Assert.Null(button.QuerySelector(".city-header"));
		Assert.Equal("Sort by City", button.GetAttribute("aria-label"));
	}

	private IRenderedComponent<MokaTable<Person>> RenderTable(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null,
		Col[]? columns = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.ChildContent, columns is null ? DefaultColumns : Columns(columns));
			extra?.Invoke(p);
		});

	private static IElement SortButton(IRenderedComponent<MokaTable<Person>> cut, string title) =>
		HeaderCell(cut, title).QuerySelector("button.moka-table-sort-button")
		?? throw new InvalidOperationException($"The {title} header has no sort button.");
}
