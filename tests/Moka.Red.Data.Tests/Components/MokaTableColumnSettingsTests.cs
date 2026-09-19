using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// A MokaColumn gets its parameters after the table has rendered, so a new Title, Width, Align and so
// on from the parent showed only on the table's next render. Only Visible told the table.
public class MokaTableColumnSettingsTests : BunitContext
{
	private static readonly TimeSpan Debounce = TimeSpan.FromSeconds(5);

	public MokaTableColumnSettingsTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void NewTitle_ShowsInTheSameRender()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(columns: [Name, Team]);

		cut.Render(p => p.Add(t => t.ChildContent, Columns(Name with { Title = "Full name" }, Team)));

		Assert.Equal(["Full name", "Team"], HeaderTitles(cut));
	}

	[Fact]
	public void NewWidthAndAlign_ShowInTheSameRender()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(columns: [Name, Team]);

		cut.Render(p => p.Add(t => t.ChildContent,
			Columns(Name with { Width = "320px", Align = MokaTextAlign.Right }, Team)));

		IElement header = cut.FindAll("thead tr:first-child th")[0];
		Assert.Contains("width: 320px", header.GetAttribute("style") ?? "", StringComparison.Ordinal);
		Assert.Contains("moka-table-cell--right", header.ClassList);
		Assert.All(DataRows(cut),
			row => Assert.Contains("moka-table-cell--right", row.QuerySelector("td[data-col-index='0']")!.ClassList));
	}

	// A parent render passes new delegates and templates every time. Counting those as changes
	// would render every table twice on every parent render.
	[Fact]
	public void UnchangedColumns_CostNoSecondRender()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(columns: [NameWithTemplate(), Team]);
		int renders = cut.RenderCount;

		cut.Render(p => p.Add(t => t.ChildContent, Columns(NameWithTemplate(), Team)));

		Assert.Equal(renders + 1, cut.RenderCount);
	}

	// Sorts and filters name a column by its title, so a renamed column lost both: the filter went on
	// under the old title with no input showing it, and the sort lost its header.
	[Fact]
	public async Task RenamedColumn_KeepsItsFilterAndSort()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(
			p => p.Add(t => t.ShowFilters, true),
			[Name, Team with { Filterable = true }, Age]);
		await cut.Find("input[aria-label='Filter by Team']").InputAsync(new ChangeEventArgs { Value = "Research" });
		cut.WaitForAssertion(() => Assert.Equal(2, DataRows(cut).Count), Debounce);
		await HeaderCell(cut, "Team").ClickAsync(new MouseEventArgs());

		cut.Render(p => p.Add(t => t.ChildContent, Columns(Name, Team with { Title = "Group", Filterable = true }, Age)));

		Assert.Equal("Research", cut.Find("input[aria-label='Filter by Group']").GetAttribute("value"));
		Assert.Equal("ascending", HeaderCell(cut, "Group").GetAttribute("aria-sort"));
		await cut.InvokeAsync(() => cut.Instance.ReloadAsync());
		Assert.Equal(2, DataRows(cut).Count);
	}

	// Its input goes with Filterable, and a filter without an input on screen would go on hiding rows.
	[Fact]
	public async Task ColumnMadeUnfilterable_DropsItsFilter()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(
			p => p.Add(t => t.ShowFilters, true),
			[Name, Team with { Filterable = true }]);
		await cut.Find("input[aria-label='Filter by Team']").InputAsync(new ChangeEventArgs { Value = "Research" });
		cut.WaitForAssertion(() => Assert.Equal(2, DataRows(cut).Count), Debounce);

		cut.Render(p => p.Add(t => t.ChildContent, Columns(Name, Team)));

		cut.WaitForAssertion(() => Assert.Equal(People.Length, DataRows(cut).Count), Debounce);
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

	// A new template delegate every call, the way a parent render passes one.
	private static Col NameWithTemplate() =>
		Name with { CellTemplate = person => builder => builder.AddContent(0, person.Name) };
}
