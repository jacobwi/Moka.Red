using System.Text.RegularExpressions;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Table;

// The seven looks, the three densities and every word: their markup on both designs, and the rules they read.
public sealed partial class MokaTableLooksAndDensityTests : BunitContext
{
	public MokaTableLooksAndDensityTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<MokaTableLook, string> Looks => new()
	{
		{ MokaTableLook.SoftStripes, "moka-table-wrapper--look-soft-stripes" },
		{ MokaTableLook.Cards, "moka-table-wrapper--look-cards" },
		{ MokaTableLook.Sheet, "moka-table-wrapper--look-sheet" },
		{ MokaTableLook.Airy, "moka-table-wrapper--look-airy" },
		{ MokaTableLook.Framed, "moka-table-wrapper--look-framed" },
		{ MokaTableLook.List, "moka-table-wrapper--look-list" }
	};

	[Fact]
	public void TheDefaultLook_WritesNoLookClass()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(MokaTheme.Default, null);

		Assert.DoesNotContain("--look-", cut.Find(".moka-table-wrapper").ClassName, StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(Looks))]
	public void ALook_IsAClassOnTheWrapper_OnTheNewDesign(MokaTableLook look, string expected)
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(MokaTheme.Default, look);

		Assert.Contains(expected, cut.Find(".moka-table-wrapper").ClassList);
	}

	[Theory]
	[MemberData(nameof(Looks))]
	public void ALook_HasRulesOfItsOwn(MokaTableLook look, string expected)
	{
		string css = Stylesheet();

		// The list's rules key off the table's class, since the phone list's rules draw it below 720 px.
		string owner = look == MokaTableLook.List ? "moka-table--list" : expected;
		Assert.Matches(new Regex($@"\.{Regex.Escape(owner)}[\s>.:\[]"), css);
	}

	// The original design draws the lined table whatever is asked, except the list, which is markup and works on both.
	[Theory]
	[MemberData(nameof(Looks))]
	public void TheOriginalDesign_DrawsLined_ButForTheList(MokaTableLook look, string expected)
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(MokaTheme.Dark, look);

		if (look == MokaTableLook.List)
		{
			Assert.Contains(expected, cut.Find(".moka-table-wrapper").ClassList);
		}
		else
		{
			Assert.DoesNotContain("--look-", cut.Find(".moka-table-wrapper").ClassName, StringComparison.Ordinal);
			Assert.Empty(cut.FindAll(".moka-table-cell--rownum"));
		}
	}

	[Fact]
	public void TheSheet_NumbersTheRows_FromThePagesFirst()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.AddCascadingValue(MokaTheme.Default)
			.Add(x => x.Items, People)
			.Add(x => x.PageSize, 2)
			.Add(x => x.Look, MokaTableLook.Sheet)
			.Add(x => x.ChildContent, DefaultColumns));

		cut.FindAll(".moka-pagination-page").First(b => b.TextContent.Trim() == "2").Click();

		Assert.Equal(["3", "4"], cut.FindAll("tbody td.moka-table-cell--rownum").Select(static td => td.TextContent));
		Assert.Single(cut.FindAll("thead th.moka-table-cell--rownum[aria-hidden='true']"));
	}

	[Fact]
	public void TheList_DrawsNoHeading_AndTwoLinesFromTheColumns()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(MokaTheme.Dark, MokaTableLook.List);

		Assert.Empty(cut.FindAll("thead"));
		Assert.Contains("moka-table--list", cut.Find("table").ClassList);
		IReadOnlyList<AngleSharp.Dom.IElement> cells = DataRows(cut)[0].QuerySelectorAll("td");
		Assert.Contains("moka-table-cell--phone-primary", cells[0].ClassList);
		Assert.All(cells.Skip(1), static cell => Assert.Contains("moka-table-cell--phone-secondary", cell.ClassList));
	}

	[Theory]
	[InlineData(MokaTableDensity.Compact, "moka-table--dense")]
	[InlineData(MokaTableDensity.Standard, "moka-table--standard")]
	[InlineData(MokaTableDensity.Comfortable, "moka-table--comfortable")]
	public void EachDensity_IsAClassOnTheTable(MokaTableDensity density, string expected)
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(x => x.Items, People)
			.Add(x => x.Density, density)
			.Add(x => x.ChildContent, DefaultColumns));

		Assert.Contains(expected, cut.Find("table").ClassList);
	}

	[Fact]
	public void TheAiryLook_IsComfortableUnlessADensityIsSet()
	{
		Assert.Contains("moka-table--comfortable", Table(MokaTheme.Default, MokaTableLook.Airy).Find("table").ClassList);
	}

	[Fact]
	public void TheDensityMenu_OffersAllThree_AndAPickIsInTheState()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(x => x.Items, People)
			.Add(x => x.ShowDensityToggle, true)
			.Add(x => x.StateChanged, (MokaTableState s) => raised = s)
			.Add(x => x.ChildContent, DefaultColumns));

		cut.Find(".moka-table-density .moka-table-toolbar-btn").Click();
		Assert.Equal(["Compact", "Standard", "Comfortable"],
			cut.FindAll(".moka-table-density-item").Select(static l => l.TextContent.Trim()));

		cut.FindAll(".moka-table-density-item input")[2].Change(true);

		Assert.Contains("moka-table--comfortable", cut.Find("table").ClassList);
		Assert.Equal(MokaTableDensity.Comfortable, raised?.Density);
		Assert.Empty(cut.FindAll(".moka-table-density-menu"));
	}

	[Fact]
	public void TheDensities_HaveRulesOnBothDesigns()
	{
		string css = Stylesheet();

		Assert.Contains(".moka-table--comfortable td", css, StringComparison.Ordinal);
		Assert.Contains(".moka-gate .moka-table--standard td", css, StringComparison.Ordinal);
		Assert.Contains(".moka-gate .moka-table--comfortable td", css, StringComparison.Ordinal);
	}

	[Fact]
	public void EveryWord_IsReplaceable()
	{
		MokaTableText text = MokaTableText.Default with
		{
			SearchPlaceholder = "Suchen",
			Actions = "Aktionen",
			NoData = "Nichts",
			ToggleColumns = "Spalten",
			Density = "Dichte"
		};

		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(x => x.Items, Array.Empty<Person>())
			.Add(x => x.Text, text)
			.Add(x => x.Searchable, true)
			.Add(x => x.ShowColumnToggle, true)
			.Add(x => x.ShowDensityToggle, true)
			.Add(x => x.RowActions, (Microsoft.AspNetCore.Components.RenderFragment<Person>)(_ => _ => { }))
			.Add(x => x.ChildContent, DefaultColumns));

		Assert.Equal("Suchen", cut.Find(".moka-table-search-input").GetAttribute("placeholder"));
		Assert.Equal("Aktionen", cut.Find("th.moka-table-cell--actions").TextContent);
		Assert.Equal("Nichts", cut.Find(".moka-table-empty").TextContent);
		Assert.Single(cut.FindAll("button[aria-label='Spalten']"));
		Assert.Single(cut.FindAll("button[aria-label='Dichte']"));
	}

	private IRenderedComponent<MokaTable<Person>> Table(MokaTheme theme, MokaTableLook? look) =>
		Render<MokaTable<Person>>(p => p
			.AddCascadingValue(theme)
			.Add(x => x.Items, People)
			.Add(x => x.Look, look)
			.Add(x => x.ChildContent, DefaultColumns));

	private static string Stylesheet() =>
		File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaTable.razor.css")).ReplaceLineEndings("\n");
}
