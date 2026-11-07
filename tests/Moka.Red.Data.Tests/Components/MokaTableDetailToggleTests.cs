using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Data.Table;
using Moka.Red.Tests.Shared;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// Where the detail toggle sits, what it's called, and whether opening one row closes another.
public class MokaTableDetailToggleTests : BunitContext
{
	private const string Css = "MokaTable.razor.css";

	private static readonly RenderFragment<Person> Detail = person => b => b.AddContent(0, $"About {person.Name}");

	public MokaTableDetailToggleTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ByDefault_TheToggleIsTheFirstColumn()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(atEnd: false);

		Assert.Contains("moka-table-cell--expand", DataRows(cut)[0].Children[0].ClassList);
		Assert.Empty(cut.FindAll(".moka-table-cell--more"));
		Assert.Empty(cut.FindAll(".moka-table-more-btn"));
	}

	[Fact]
	public void AtTheEnd_TheToggleIsTheLastCell_AfterTheRowActions()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.RowActions, (RenderFragment<Person>)(_ => b => b.AddContent(0, "act"))));

		foreach (IElement row in DataRows(cut))
		{
			Assert.Contains("moka-table-cell--more", row.Children[^1].ClassList);
			Assert.Contains("moka-table-cell--actions", row.Children[^2].ClassList);
		}

		Assert.Empty(cut.FindAll(".moka-table-cell--expand"));
		IElement header = cut.FindAll("thead tr:first-child th")[^1];
		Assert.Contains("moka-table-cell--more", header.ClassList);
		Assert.Equal("More", header.QuerySelector(".moka-visually-hidden")!.TextContent);
	}

	[Fact]
	public void AtTheEnd_TheButtonTakesTheRowsName_AndHasNoTooltip()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ItemName, x => x.Name));

		IElement button = Toggle(cut, 0);
		Assert.Equal("Show more about Ada", button.GetAttribute("aria-label"));
		Assert.Equal("false", button.GetAttribute("aria-expanded"));
		Assert.Equal("button", button.GetAttribute("type"));
		Assert.Null(button.Closest(".moka-tooltip"));
		Assert.Null(button.GetAttribute("title"));
	}

	[Fact]
	public void AtTheEnd_WithoutItemName_TheNameIsPlain()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		Assert.Equal("Show more", Toggle(cut, 0).GetAttribute("aria-label"));
	}

	[Fact]
	public async Task Opening_ShowsTheDetail_AndRenamesTheButton()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ItemName, x => x.Name));

		await Toggle(cut, 0).ClickAsync(new MouseEventArgs());

		IElement button = Toggle(cut, 0);
		Assert.Equal("true", button.GetAttribute("aria-expanded"));
		Assert.Equal("Hide more about Ada", button.GetAttribute("aria-label"));
		Assert.Equal(["About Ada"], DetailTexts(cut));
	}

	[Fact]
	public async Task ByDefault_RowsOpenOnTheirOwn()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		await Toggle(cut, 0).ClickAsync(new MouseEventArgs());
		await Toggle(cut, 1).ClickAsync(new MouseEventArgs());

		Assert.Equal(["About Ada", "About Grace"], DetailTexts(cut));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SingleExpand_ClosesTheRowThatWasOpen(bool keyed)
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p =>
		{
			p.Add(t => t.SingleExpand, true);
			if (keyed)
			{
				p.Add(t => t.ItemKey, x => x.Name);
			}
		});

		await Toggle(cut, 0).ClickAsync(new MouseEventArgs());
		await Toggle(cut, 1).ClickAsync(new MouseEventArgs());

		Assert.Equal(["About Grace"], DetailTexts(cut));
		Assert.Equal("false", Toggle(cut, 0).GetAttribute("aria-expanded"));
	}

	[Fact]
	public async Task SingleExpand_ClosesTheOpenRow_OnItsOwnToggle()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.SingleExpand, true));

		await Toggle(cut, 0).ClickAsync(new MouseEventArgs());
		await Toggle(cut, 0).ClickAsync(new MouseEventArgs());

		Assert.Empty(DetailTexts(cut));
	}

	[Fact]
	public async Task SingleExpand_WorksWithTheToggleAtTheStart()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.SingleExpand, true), atEnd: false);

		await cut.FindAll(".moka-table-expand-btn")[0].ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-table-expand-btn")[2].ClickAsync(new MouseEventArgs());

		Assert.Equal(["About Alan"], DetailTexts(cut));
	}

	[Fact]
	public void TheToggle_DoesNotClickTheRow()
	{
		int clicks = 0;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.OnRowClick, (Person _) => clicks++));

		Toggle(cut, 0).Click();

		Assert.Equal(0, clicks);
		Assert.Single(DetailTexts(cut));
	}

	// Every row the table draws has a cell for the toggle's column, or the columns stop lining up.
	[Fact]
	public void EveryRow_KeepsACellForTheEndColumn()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ShowFilters, true),
			[Name with { Filterable = true }, Age with { Aggregate = MokaAggregateType.Sum }]);

		Assert.Contains("moka-table-cell--more", cut.Find(".moka-table-filter-row").Children[^1].ClassList);
		Assert.Contains("moka-table-cell--more", cut.Find("tfoot tr").Children[^1].ClassList);

		Toggle(cut, 0).Click();
		Assert.Equal("3", cut.Find(".moka-table-detail-row > td").GetAttribute("colspan"));
	}

	[Fact]
	public void WhileLoading_TheSkeletonRowsKeepTheEndColumn()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Loading, true));

		foreach (IElement row in cut.FindAll(".moka-table-skeleton-row"))
		{
			Assert.Contains("moka-table-cell--more", row.Children[^1].ClassList);
		}
	}

	[Fact]
	public void TheChevron_TurnsWhenTheRowIsOpen()
	{
		Assert.Equal("rotate(90deg)", ScopedCss.Declarations(Css, ".moka-table-more-btn[aria-expanded=\"true\"] ::deep svg")["transform"]);
	}

	// On the phone list it comes last in the row with no column of its own, so it lands at the far right, after a
	// trailing cell.
	[Fact]
	public void OnAPhone_TheToggleSitsAtTheFarRight()
	{
		IReadOnlyDictionary<string, string> cell = ScopedCss.Declarations(Css,
			".moka-table-container > .moka-table--phone > tbody > .moka-table-row--phone > .moka-table-cell--more");

		Assert.Equal("1 / span 2", cell["grid-row"]);
		Assert.DoesNotContain("grid-column", cell.Keys);
	}

	[Fact]
	public void OnAPhone_TheToggleComesAfterTheTrailingCell()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(columns:
		[
			Name with { Phone = MokaColumnPhone.Primary },
			Age with { Phone = MokaColumnPhone.Trailing }
		]);

		IElement row = DataRows(cut)[0];
		Assert.Contains("moka-table-cell--phone-trailing", row.Children[^2].ClassList);
		Assert.Contains("moka-table-cell--more", row.Children[^1].ClassList);
		Assert.Equal("cell", row.Children[^1].GetAttribute("role"));
	}

	private static IElement Toggle(IRenderedComponent<MokaTable<Person>> cut, int row) =>
		DataRows(cut)[row].QuerySelector(".moka-table-more-btn")!;

	private static List<string> DetailTexts(IRenderedComponent<MokaTable<Person>> cut) =>
		cut.FindAll(".moka-table-detail-row").Select(row => row.TextContent.Trim()).ToList();

	private IRenderedComponent<MokaTable<Person>> Table(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null,
		Col[]? columns = null,
		bool atEnd = true) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.Expandable, true);
			p.Add(t => t.DetailTemplate, Detail);
			if (atEnd)
			{
				p.Add(t => t.ExpandTogglePosition, MokaTableTogglePosition.End);
			}

			p.Add(t => t.ChildContent, columns is null ? Columns(Name, Age) : Columns(columns));
			extra?.Invoke(p);
		});
}
