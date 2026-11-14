using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Data.Table;
using Moka.Red.Data.Tests.Components;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Table;

/// <summary>
///     The whole markup of the table in representative configurations, recorded before its internals were split up.
///     A refactor has to leave these byte for byte as they are, apart from scope attributes, element reference ids,
///     event handler ids and generated ids, which change with any edit elsewhere. Set <c>MOKA_RECORD_GOLDENS=1</c> to
///     write them again after a change that is meant to show, and say why in the commit.
/// </summary>
public sealed partial class MokaTableGoldenTests : BunitContext
{
	private const string RecordVariable = "MOKA_RECORD_GOLDENS";

	public MokaTableGoldenTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void Basic() => Golden("basic", () => Table(p => p
		.Add(x => x.Title, "People")
		.Add(x => x.Searchable, true)));

	[Fact]
	public void ServerData() => Golden("server", () =>
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(items: null, configure: p => p
			.Add(x => x.PageSize, 2)
			.Add(x => x.ServerData, state => Task.FromResult(new MokaTableResult<Person>
			{
				Items = People.Skip((state.Page - 1) * state.PageSize).Take(state.PageSize).ToList(),
				TotalItems = People.Length
			})));
		cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("tbody tr[data-row-index]")));
		return cut;
	});

	[Fact]
	public void Selection() => Golden("selection", () => Table(p => p
		.Add(x => x.Selectable, true)
		.Add(x => x.SelectedItems, [People[1]])
		.Add(x => x.SelectionActions, (RenderFragment<HashSet<Person>>)(set => b => b.AddContent(0, $"{set.Count} picked")))));

	[Fact]
	public void Bulk() => Golden("bulk", () => Table(p => p
		.Add(x => x.Selectable, true)
		.Add(x => x.SelectedItems, [People[0], People[2]])
		.Add(x => x.ItemName, static x => x.Name)
		.Add(x => x.ItemsName, "people")
		.Add(x => x.BulkActions, (RenderFragment<IReadOnlyList<Person>>)(rows => b => b.AddContent(0, $"Act on {rows.Count}")))));

	[Fact]
	public void Grouping() => Golden("grouping", () => Table(p => p
		.Add(x => x.GroupBy, "Team")
		.Add(x => x.InitiallyCollapsedGroups, ["Research"])));

	[Fact]
	public void FilterRow() => Golden("filters", () => Table(p => p
		.Add(x => x.FilterControls, MokaTableFilterControls.Row), Columns(
			Name with { Filterable = true },
			Team with { Filterable = true },
			Age,
			City)));

	[Fact]
	public void SelectFilter() => Golden("filters-select", () =>
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(x => x.Items, People)
			.Add(x => x.Label, "People")
			.Add(x => x.FilterControls, MokaTableFilterControls.Row)
			.AddChildContent(b =>
			{
				b.OpenComponent<MokaColumn<Person>>(0);
				b.AddComponentParameter(1, nameof(MokaColumn<Person>.Title), "Team");
				b.AddComponentParameter(2, nameof(MokaColumn<Person>.Field), (Func<Person, object?>)(static x => x.Team));
				b.AddComponentParameter(3, nameof(MokaColumn<Person>.Filterable), true);
				b.AddComponentParameter(4, nameof(MokaColumn<Person>.FilterType), MokaColumnFilterType.Select);
				b.CloseComponent();
			}));
		return cut;
	});

	[Fact]
	public void PaginationTopAndBottom() => Golden("pagination-both", () => Table(p => p
		.Add(x => x.PageSize, 2), DefaultColumnsWithPager(new Pager { Position = MokaTablePaginationPosition.Both, Sticky = true })));

	[Fact]
	public void PaginationSecondPage() => Golden("pagination-page-two", () =>
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(x => x.PageSize, 2));
		cut.FindAll(".moka-pagination-page").First(b => b.TextContent.Trim() == "2").Click();
		return cut;
	});

	[Fact]
	public void PhoneList() => Golden("phone", () => Table(p => p
		.Add(x => x.Selectable, true), Columns(
			Name with { Phone = MokaColumnPhone.Primary },
			Team with { Phone = MokaColumnPhone.Secondary },
			Age with { Phone = MokaColumnPhone.Trailing },
			City with { Phone = MokaColumnPhone.Secondary, Visible = false })));

	[Fact]
	public void Virtualized() => Golden("virtualize", () => Table(p => p
		.Add(x => x.Virtualize, true)
		.Add(x => x.ShowPagination, false)
		.Add(x => x.Height, "300px")));

	[Fact]
	public void Editable() => Golden("editable", () =>
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(static _ => { },
			Columns(Name with { Editable = true }, Team, Age with { Format = "N1" }, City));
		cut.Find("tbody td[data-editable]").DoubleClick();
		return cut;
	});

	[Fact]
	public void DetailRows() => Golden("detail", () =>
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(x => x.Expandable, true)
			.Add(x => x.ItemKey, static x => x.Name)
			.Add(x => x.DetailTemplate, (RenderFragment<Person>)(person => b => b.AddContent(0, $"About {person.Name}"))));
		cut.Find(".moka-table-expand-btn").Click();
		return cut;
	});

	[Fact]
	public void DetailToggleAtTheEnd() => Golden("detail-end", () =>
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(x => x.Expandable, true)
			.Add(x => x.ExpandTogglePosition, MokaTableTogglePosition.End)
			.Add(x => x.ItemName, static x => x.Name)
			.Add(x => x.DetailTemplate, (RenderFragment<Person>)(person => b => b.AddContent(0, $"About {person.Name}"))));
		cut.Find(".moka-table-more-btn").Click();
		return cut;
	});

	[Fact]
	public void FooterAggregates() => Golden("aggregates", () => Table(static _ => { }, Columns(
		Name with { Aggregate = MokaAggregateType.Count },
		Team,
		Age with { Aggregate = MokaAggregateType.Sum },
		City)));

	[Fact]
	public void Toolbar() => Golden("toolbar", () => Table(p => p
		.Add(x => x.Title, "People")
		.Add(x => x.Exportable, true)
		.Add(x => x.ShowRefresh, true)
		.Add(x => x.ShowDensityToggle, true)
		.Add(x => x.ShowColumnToggle, true)
		.Add(x => x.ShowColumnChooser, true)
		.Add(x => x.ToolbarContent, (RenderFragment)(b => b.AddContent(0, "Mine")))));

	[Fact]
	public void FramedWithTheToolbarInside() => Golden("framed", () => Table(p => p
		.Add(x => x.Title, "People")
		.Add(x => x.Framed, true)
		.Add(x => x.ToolbarInFrame, true)
		.Add(x => x.Striped, true)
		.Add(x => x.Bordered, true)
		.Add(x => x.FootContent, (RenderFragment)(b => b.AddContent(0, "All 4 people")))));

	[Fact]
	public void ShowMore() => Golden("show-more", () => Table(p => p
		.Add(x => x.PageSize, 2)
		.Add(x => x.ItemsName, "people"), DefaultColumnsWithPager(LoadMore)));

	[Fact]
	public void Empty() => Golden("empty", () => Table(items: [], configure: p => p
		.Add(x => x.EmptyTitle, "Nobody here")
		.Add(x => x.EmptyText, "Add someone to see them.")));

	[Fact]
	public void EmptyPlain() => Golden("empty-plain", () => Table(items: [], configure: static _ => { }));

	[Fact]
	public void Loading() => Golden("loading", () => Table(p => p
		.Add(x => x.Loading, true)
		.Add(x => x.Selectable, true)));

	[Fact]
	public void Reordering() => Golden("reorder", () => Table(p => p
		.Add(x => x.RowReorderable, true)
		.Add(x => x.ColumnReorderable, true)));

	[Fact]
	public void Sorted() => Golden("sorted", () => Table(p => p
		.Add(x => x.MultiSort, true)
		.Add(x => x.State, SortedBy("Age", MokaSortDirection.Descending))));

	[Fact]
	public void Views() => Golden("views", () => Table(p => p
		.Add(x => x.Views,
		[
			new MokaTableView("Everything", new MokaTableState()),
			new MokaTableView("Engineers", new MokaTableState { Filters = [new MokaTableFilter("Team", MokaFilterOperator.Is, "Engineering")] })
		])
		.Add(x => x.State, new MokaTableState { Filters = [new MokaTableFilter("Team", MokaFilterOperator.Is, "Engineering")] })
		.Add(x => x.ViewSaved, (MokaTableView _) => { })
		.Add(x => x.ViewForgotten, (MokaTableView _) => { })));

	[Fact]
	public void RowActionsAndClicks() => Golden("row-actions", () => Table(p => p
		.Add(x => x.OnRowClick, (Person _) => { })
		.Add(x => x.RowActions, (RenderFragment<Person>)(person => b => b.AddContent(0, $"Edit {person.Name}")))
		.Add(x => x.RowClass, static x => x.Age > 40 ? "senior" : null)));

	private IRenderedComponent<MokaTable<Person>> Table(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>> configure, RenderFragment? columns = null) =>
		Table(People, configure, columns);

	// The columns are an argument rather than ChildContent in the configuration: bUnit adds a second ChildContent
	// after the first, so the table would draw both sets.
	private IRenderedComponent<MokaTable<Person>> Table(
		IEnumerable<Person>? items,
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>> configure,
		RenderFragment? columns = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(x => x.Items, items)
				.Add(x => x.Label, "People")
				.Add(x => x.ChildContent, columns ?? DefaultColumns);
			configure(p);
		});

	private static void Golden(string name, Func<IRenderedComponent<MokaTable<Person>>> render)
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		CultureInfo uiCulture = CultureInfo.CurrentUICulture;
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
		try
		{
			string markup = Normalize(render().Markup);
			string file = Path.Combine(GoldenFolder(), $"{name}.html");

			if (Environment.GetEnvironmentVariable(RecordVariable) == "1")
			{
				File.WriteAllText(file, markup);
				return;
			}

			Assert.True(File.Exists(file), $"No golden for {name}. Run the tests once with {RecordVariable}=1 to write it.");
			Assert.Equal(File.ReadAllText(file).ReplaceLineEndings("\n"), markup);
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = uiCulture;
		}
	}

	internal static string Normalize(string markup)
	{
		string text = ScopeAttribute().Replace(markup, string.Empty);
		text = ElementReference().Replace(text, string.Empty);
		text = HandlerId().Replace(text, "$1");
		text = GeneratedId().Replace(text, "ID");
		return text.Replace("\r\n", "\n", StringComparison.Ordinal);
	}

	private static string GoldenFolder()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				string goldens = Path.Combine(folder.FullName, "tests", "Moka.Red.Data.Tests", "Table", "Goldens");
				Directory.CreateDirectory(goldens);
				return goldens;
			}
		}

		Assert.Fail("The tests are not running inside the repository, so there are no goldens to read.");
		return string.Empty;
	}

	[GeneratedRegex(@" b-[a-z0-9]{10}(="""")?")]
	private static partial Regex ScopeAttribute();

	[GeneratedRegex(@" blazor:elementReference=""[^""]*""")]
	private static partial Regex ElementReference();

	[GeneratedRegex(@"( blazor:on[a-z]+)=""\d+""")]
	private static partial Regex HandlerId();

	[GeneratedRegex("[0-9a-f]{32}")]
	private static partial Regex GeneratedId();
}
