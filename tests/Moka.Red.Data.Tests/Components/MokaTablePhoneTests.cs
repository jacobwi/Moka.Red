using AngleSharp.Dom;
using Bunit;
using Moka.Red.Data.Table;
using Moka.Red.Tests.Shared;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// The list below 720 px. The table stays a table in the DOM, so bUnit reads the slots and roles off the markup, and
// the layout itself off the stylesheet.
public class MokaTablePhoneTests : BunitContext
{
	private const string Css = "MokaTable.razor.css";
	private const string Row = ".moka-table-container > .moka-table--phone > tbody > .moka-table-row--phone";

	public MokaTablePhoneTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ATableWithNoSlots_RendersAsItAlwaysDid()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(Name, Team);

		Assert.Empty(cut.FindAll("[role]"));
		Assert.Empty(cut.FindAll(".moka-table--phone, .moka-table-wrapper--phone, .moka-table-row--phone"));
		Assert.Empty(cut.FindAll("[class*='moka-table-cell--phone']"));
		Assert.False(cut.Find("table").HasAttribute("style"));
	}

	// display: grid takes away what a table's elements mean to a screen reader, so the roles have to say it.
	[Fact]
	public void ASlot_TurnsOnTheRolesAScreenReaderNeeds()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(Name with { Phone = MokaColumnPhone.Primary }, Team);

		Assert.Equal("table", cut.Find("table").GetAttribute("role"));
		Assert.Equal("rowgroup", cut.Find("thead").GetAttribute("role"));
		Assert.Equal("rowgroup", cut.Find("tbody").GetAttribute("role"));
		Assert.All(cut.FindAll("tr"), row => Assert.Equal("row", row.GetAttribute("role")));
		Assert.All(cut.FindAll("thead th"), cell => Assert.Equal("columnheader", cell.GetAttribute("role")));
		Assert.All(cut.FindAll("tbody td"), cell => Assert.Equal("cell", cell.GetAttribute("role")));
	}

	[Fact]
	public void TheHeader_KeepsItsNamesForAScreenReader()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(Name with { Phone = MokaColumnPhone.Primary }, Team);

		Assert.Equal(["Name", "Team"], HeaderTitles(cut));
		IReadOnlyDictionary<string, string> head = ScopedCss.Declarations(
			Css, ".moka-table-container > .moka-table--phone > thead > tr:first-child");
		Assert.DoesNotContain("display", head.Keys);
		Assert.Equal("inset(50%)", head["clip-path"]);
		Assert.Equal("hidden", ScopedCss.Declarations(
			Css, ".moka-table-container > .moka-table--phone > thead > tr:first-child .moka-table-sort-button")["visibility"]);
		Assert.Equal("visible", ScopedCss.Declarations(
			Css, ".moka-table-container > .moka-table--phone > thead > tr:first-child .moka-table-header-text")["visibility"]);
	}

	[Fact]
	public void EachSlot_LandsOnItsCells()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(
			Name with { Phone = MokaColumnPhone.Primary },
			Team with { Phone = MokaColumnPhone.Secondary },
			Age with { Phone = MokaColumnPhone.Trailing },
			City with { Phone = MokaColumnPhone.Hidden });

		IHtmlCollection<IElement> cells = DataRows(cut)[0].QuerySelectorAll("td");

		Assert.Contains("moka-table-cell--phone-primary", cells[0].ClassList);
		Assert.Contains("moka-table-cell--phone-secondary", cells[1].ClassList);
		Assert.Contains("moka-table-cell--phone-trailing", cells[2].ClassList);
		Assert.Contains("moka-table-cell--phone-hidden", cells[3].ClassList);
	}

	[Fact]
	public void TheHeaderCells_TakeTheSlotsToo_SoCellsStillLineUpWithTheirNames()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(
			Name with { Phone = MokaColumnPhone.Primary }, City with { Phone = MokaColumnPhone.Hidden });

		Assert.Contains("moka-table-cell--phone-hidden", HeaderCell(cut, "City").ClassList);
		Assert.Equal("none", ScopedCss.Declarations(
			Css, ".moka-table-container > .moka-table--phone > thead > tr:first-child > .moka-table-cell--phone-hidden")["display"]);
	}

	[Fact]
	public void AColumnWithoutASlot_IsHiddenInTheList()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(Name with { Phone = MokaColumnPhone.Primary }, Team);

		Assert.Contains("moka-table-cell--phone-hidden", DataRows(cut)[0].QuerySelectorAll("td")[1].ClassList);
	}

	[Fact]
	public void TheFirstColumn_IsThePrimaryLine_WhenNoColumnClaimsIt()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(Name, Team with { Phone = MokaColumnPhone.Secondary });

		Assert.Contains("moka-table-cell--phone-primary", DataRows(cut)[0].QuerySelectorAll("td")[0].ClassList);
	}

	[Fact]
	public void TheFirstColumn_KeepsTheSlotItNamed()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(Name with { Phone = MokaColumnPhone.Trailing }, Team);

		Assert.Contains("moka-table-cell--phone-trailing", DataRows(cut)[0].QuerySelectorAll("td")[0].ClassList);
	}

	[Fact]
	public void TheGrid_HasATrackPerSecondaryCell()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(
			Name with { Phone = MokaColumnPhone.Primary },
			Team with { Phone = MokaColumnPhone.Secondary },
			City with { Phone = MokaColumnPhone.Secondary });

		Assert.Equal("--_phone-secondary: 2", cut.Find("table").GetAttribute("style")!.TrimEnd(';').Trim());
	}

	// repeat() takes no zero: the whole grid declaration would be thrown away.
	[Fact]
	public void TheGrid_KeepsOneTrack_WithNoSecondaryCells()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(Name with { Phone = MokaColumnPhone.Primary });

		Assert.Equal("--_phone-secondary: 1", cut.Find("table").GetAttribute("style")!.TrimEnd(';').Trim());
	}

	[Fact]
	public void SecondaryCellsAreJoinedInCss_WithNoCopyOfTheirContent()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(
			Name with { Phone = MokaColumnPhone.Primary },
			Team with { Phone = MokaColumnPhone.Secondary },
			City with { Phone = MokaColumnPhone.Secondary });

		Assert.Equal(1, cut.FindAll("tbody tr:first-child td").Count(td => td.TextContent.Contains("Engineering", StringComparison.Ordinal)));
		Assert.Contains("\"\\00b7\"", ScopedCss.Declarations(
			Css, Row + " > .moka-table-cell--phone-secondary ~ .moka-table-cell--phone-secondary::before")["content"], StringComparison.Ordinal);
	}

	// A phone lays rows out by slot whatever the columns above 720 px show, and someone who hid the primary column on
	// a desktop would otherwise get empty rows.
	[Fact]
	public void AHiddenColumnWithASlot_StillDrawsItsCell_OutsideTheGridsKeys()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(
			Name with { Phone = MokaColumnPhone.Primary, Visible = false }, Team, Age);

		IElement cell = DataRows(cut)[0].QuerySelector("td.moka-table-cell--phone-only")!;

		Assert.Equal("Ada", cell.TextContent.Trim());
		Assert.False(cell.HasAttribute("data-col-index"));
		Assert.False(cell.HasAttribute("tabindex"));
		Assert.Single(cut.FindAll("thead th.moka-table-cell--phone-only"));
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-table-cell--phone-only")["display"]);
		Assert.Equal(["0", "1"], DataRows(cut)[0].QuerySelectorAll("td[data-col-index]").Select(td => td.GetAttribute("data-col-index")!));
	}

	[Fact]
	public void AHiddenColumnWithoutASlot_DrawsNoCell()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(
			Name with { Phone = MokaColumnPhone.Primary }, Team with { Visible = false });

		Assert.Single(DataRows(cut)[0].QuerySelectorAll("td"));
	}

	// Tab has to reach a cell a phone shows, or the table can't be reached from the keyboard there.
	[Fact]
	public void TheTabStop_StartsOnThePrimaryCell()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(
			Team with { Phone = MokaColumnPhone.Hidden }, Name with { Phone = MokaColumnPhone.Primary });

		IElement stop = Assert.Single(TabStops(cut));
		Assert.Equal("1", stop.GetAttribute("data-col-index"));
	}

	[Fact]
	public void TheTabStop_StaysOnTheFirstCell_WithNoSlots()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(Team, Name);

		Assert.Equal("0", Assert.Single(TabStops(cut)).GetAttribute("data-col-index"));
	}

	[Fact]
	public void DataRowsTakeTheGrid_AndOtherRowsDoNot()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render(Name with { Phone = MokaColumnPhone.Primary });

		Assert.All(DataRows(cut), row => Assert.Contains("moka-table-row--phone", row.ClassList));
		IReadOnlyDictionary<string, string> grid = ScopedCss.Declarations(Css, Row);
		Assert.Equal("grid", grid["display"]);
		Assert.Contains("repeat(var(--_phone-secondary)", grid["grid-template-columns"], StringComparison.Ordinal);
		Assert.Equal("row dense", grid["grid-auto-flow"]);
		Assert.Equal("0.6875rem 0.875rem", grid["padding"]);
		Assert.Equal("block", ScopedCss.Declarations(Css, ".moka-table-container > .moka-table--phone > tbody > tr")["display"]);
	}

	[Fact]
	public void TheSlots_HaveTheSizesOfTheMockup()
	{
		Assert.Equal("var(--moka-font-size-md)", ScopedCss.Declarations(Css, Row + " > .moka-table-cell--phone-primary")["font-size"]);
		Assert.Equal("var(--moka-font-weight-semibold)", ScopedCss.Declarations(Css, Row + " > .moka-table-cell--phone-primary")["font-weight"]);
		Assert.Equal("var(--moka-font-size-sm)", ScopedCss.Declarations(Css, Row + " > .moka-table-cell--phone-secondary")["font-size"]);
		Assert.Equal("var(--moka-color-on-surface-variant)", ScopedCss.Declarations(Css, Row + " > .moka-table-cell--phone-secondary")["color"]);
		Assert.Equal("var(--moka-selected-bar)", ScopedCss.Declarations(Css, Row + ".moka-table-row--selected")["box-shadow"]);
	}

	[Fact]
	public void AWidthOnAColumn_DoesNotReachTheList()
	{
		Assert.Equal("auto !important", ScopedCss.Declarations(Css, Row + " > td")["width"]);
		Assert.Equal("0 !important", ScopedCss.Declarations(Css, Row + " > td")["min-width"]);
	}

	[Fact]
	public void SkeletonRows_TakeTheGridAndTheSlots()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.Loading, true)
			.Add(t => t.ChildContent, Columns(Name with { Phone = MokaColumnPhone.Primary }, Team with { Phone = MokaColumnPhone.Trailing })));

		IElement row = cut.Find(".moka-table-skeleton-row");

		Assert.Contains("moka-table-row--phone", row.ClassList);
		Assert.Contains("moka-table-cell--phone-primary", row.QuerySelectorAll("td")[0].ClassList);
		Assert.Contains("moka-table-cell--phone-trailing", row.QuerySelectorAll("td")[1].ClassList);
	}

	[Fact]
	public void TheChooser_IsLeftOutOfTheList()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.ShowColumnChooser, true)
			.Add(t => t.ChildContent, Columns(Name with { Phone = MokaColumnPhone.Primary })));

		Assert.Contains("moka-table-wrapper--phone", cut.Find(".moka-table-wrapper").ClassList);
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-table-wrapper--phone .moka-table-chooser")["display"]);
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-table-wrapper--phone ::deep .moka-table-chooser-toggle")["display"]);
	}

	private IRenderedComponent<MokaTable<Person>> Render(params Col[] columns) =>
		Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.ChildContent, Columns(columns)));
}
