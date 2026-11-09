using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Data.Table;
using Moka.Red.Feedback.EmptyState;
using Moka.Red.Feedback.Loading;
using Moka.Red.Icons;
using Moka.Red.Tests.Shared;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// Framed and Bleed, the loading and empty states, and the sizes that live only in the stylesheet.
public class MokaTableLookTests : BunitContext
{
	private const string Css = "MokaTable.razor.css";

	public MokaTableLookTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ByDefault_TheWrapperIsAsItWas()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		IElement wrapper = cut.Find(".moka-table-wrapper");
		Assert.Equal("moka-table-wrapper", wrapper.ClassName);
		Assert.Empty(cut.FindAll(".moka-table-frame"));
		Assert.Equal("moka-table-container", wrapper.Children[0].ClassName!.Split(' ')[0]);
	}

	[Fact]
	public void TheHeaderRow_ShowsByDefault()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		Assert.NotEmpty(cut.FindAll("thead tr"));
	}

	[Fact]
	public void ShowHeaderOff_LeavesOutTheHeaderRow_AndKeepsEveryRow()
	{
		int rows = Table().FindAll("tbody tr").Count;

		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ShowHeader, false));

		Assert.Empty(cut.FindAll("thead"));
		Assert.Equal(rows, cut.FindAll("tbody tr").Count);
	}

	[Fact]
	public void Framed_PutsTheRowsInACard_AndTheToolbarAboveIt()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Framed, true)
			.Add(t => t.Title, "People"));

		IElement wrapper = cut.Find(".moka-table-wrapper");
		Assert.Contains("moka-table-wrapper--framed", wrapper.ClassList);
		Assert.Contains("moka-table-toolbar", wrapper.Children[0].ClassList);
		Assert.Contains("moka-table-frame", wrapper.Children[1].ClassList);
		Assert.NotEmpty(wrapper.Children[1].QuerySelectorAll("table"));
		Assert.Empty(wrapper.Children[1].QuerySelectorAll(".moka-table-toolbar"));
	}

	[Fact]
	public void TheFrame_HasTheSpecsSizes()
	{
		IReadOnlyDictionary<string, string> frame = ScopedCss.Declarations(Css, ".moka-table-frame");

		Assert.Equal("var(--moka-color-surface)", frame["background"]);
		Assert.Equal("1px solid var(--moka-color-outline)", frame["border"]);
		Assert.Equal("var(--moka-radius-xl)", frame["border-radius"]);
		Assert.Equal("clip", frame["overflow"]);
	}

	[Fact]
	public void Bleed_AddsItsClass_AndNoElement()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Bleed, true));

		Assert.Contains("moka-table-wrapper--bleed", cut.Find(".moka-table-wrapper").ClassList);
		Assert.Empty(cut.FindAll(".moka-table-frame"));
	}

	// The card's padding is one value on the new look and two on the original one, so calc() can read it only there.
	[Fact]
	public void Bleed_ReachesPastTheCardsPadding_OnBothDesigns()
	{
		Assert.Equal(
			"0 calc(-1 * var(--moka-spacing-lg)) calc(-1 * var(--moka-spacing-md))",
			ScopedCss.Declarations(Css, ".moka-table-wrapper--bleed")["margin"]);
		Assert.Equal(
			"0 calc(-1 * var(--_table-bleed)) calc(-1 * var(--_table-bleed))",
			ScopedCss.Declarations(Css, ".moka-gate .moka-table-wrapper--bleed")["margin"]);
	}

	// An Inner card pads less than a card does, so bleeding by the card's padding cut off the last row.
	[Fact]
	public void Bleed_ReadsThePaddingOfTheCardItIsIn()
	{
		Assert.Equal("var(--moka-card-inset, var(--moka-card-padding))",
			ScopedCss.Declarations(Css, ".moka-gate .moka-table-wrapper--bleed")["--_table-bleed"]);
		Assert.Equal("var(--_table-bleed)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-table-wrapper--bleed > .moka-table-toolbar")["padding-inline"]);
		Assert.Equal("var(--_table-bleed) !important",
			ScopedCss.Declarations(Css, ".moka-gate .moka-table-wrapper--bleed > .moka-table-container > .moka-table > tbody > tr > td:first-child")["padding-inline-start"]);
	}

	[Fact]
	public void Mono_MarksTheCells()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(columns: [Name, City with { Mono = true }]);

		IHtmlCollection<IElement> cells = DataRows(cut)[0].QuerySelectorAll("td");
		Assert.DoesNotContain("moka-table-cell--mono", cells[0].ClassList);
		Assert.Contains("moka-table-cell--mono", cells[1].ClassList);
		Assert.Equal("var(--moka-font-size-mono)", ScopedCss.Declarations(Css, ".moka-table > tbody > tr > td.moka-table-cell--mono")["font-size"]);
	}

	[Fact]
	public void TheBulkBar_HasTheSpecsSizes()
	{
		IReadOnlyDictionary<string, string> bar = ScopedCss.Declarations(Css, ".moka-table-bulk");

		Assert.Equal("sticky", bar["position"]);
		Assert.Equal("0.75rem 1rem", bar["padding"]);
		Assert.Contains("var(--moka-color-primary-row)", bar["background"], StringComparison.Ordinal);
		Assert.Equal("0 1px 0 var(--moka-color-primary-ring)", bar["box-shadow"]);
		Assert.Equal("var(--moka-font-size-md)", ScopedCss.Declarations(Css, ".moka-table-bulk-total")["font-size"]);
		Assert.Equal("var(--moka-color-primary-light)", ScopedCss.Declarations(Css, ".moka-table-bulk-names")["color"]);
		Assert.Equal("ellipsis", ScopedCss.Declarations(Css, ".moka-table-bulk-names")["text-overflow"]);
	}

	[Fact]
	public void TheChips_Are28PixelsTall()
	{
		Assert.Equal("1.75rem", ScopedCss.Declarations(Css, ".moka-table-view")["height"]);
		Assert.Equal("1.75rem", ScopedCss.Declarations(Css, ".moka-table-view-save")["height"]);
	}

	[Fact]
	public void TheFoot_HasTheSpecsSizes()
	{
		IReadOnlyDictionary<string, string> foot = ScopedCss.Declarations(Css, ".moka-table-foot");

		Assert.Equal("0.75rem 1rem", foot["padding"]);
		Assert.Equal("var(--moka-font-size-mono)", foot["font-size"]);
		Assert.Equal("var(--moka-color-on-surface-variant)", foot["color"]);
	}

	[Fact]
	public void TheHeading_IsLitOnlyWhenSorted()
	{
		Assert.Equal("var(--moka-color-on-surface-quaternary)", ScopedCss.Declarations(
			Css, ".moka-gate .moka-table-sort-icon:not(.moka-table-sort-icon--active)")["color"]);
		Assert.Equal("var(--moka-color-primary-light)", ScopedCss.Declarations(Css, ".moka-gate .moka-table-sort-icon--active")["color"]);
		Assert.Equal("sticky", ScopedCss.Declarations(Css, ".moka-gate .moka-table thead")["position"]);
	}

	[Fact]
	public void TheBoxes_HaveTheSpecsSizes()
	{
		IReadOnlyDictionary<string, string> box = ScopedCss.Declarations(Css, ".moka-gate .moka-table-cell--checkbox > input[type=\"checkbox\"]");

		Assert.Equal("none", box["appearance"]);
		Assert.Equal("1.125rem", box["width"]);
		Assert.Equal("1.125rem", box["height"]);
		Assert.Equal("1.5px solid var(--moka-color-on-surface-quaternary)", box["border"]);
		Assert.Equal("var(--moka-radius-sm)", box["border-radius"]);
		Assert.Equal("var(--moka-color-on-primary)", box["color"]);
		Assert.Equal("var(--moka-color-primary)", ScopedCss.Declarations(
			Css, ".moka-gate .moka-table-cell--checkbox > input[type=\"checkbox\"]:indeterminate")["background"]);
	}

	[Fact]
	public void ThePickedRow_ReadsTheBarToken()
	{
		Assert.Equal("var(--moka-selected-bar)", ScopedCss.Declarations(Css, ".moka-gate .moka-table-row--selected > td:first-child")["box-shadow"]);
	}

	[Fact]
	public void NotLoading_DrawsNoSkeleton()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		Assert.Empty(cut.FindComponents<MokaSkeleton>());
		Assert.False(cut.Find("table").HasAttribute("aria-busy"));
		Assert.Empty(cut.FindAll("[role='status']"));
	}

	[Fact]
	public void Loading_DrawsSkeletonRowsInTheRealColumns()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Loading, true));

		IReadOnlyList<IElement> rows = cut.FindAll("tbody tr");
		Assert.Equal(4, rows.Count);
		Assert.All(rows, row =>
		{
			Assert.Contains("moka-table-skeleton-row", row.ClassList);
			Assert.Equal("true", row.GetAttribute("aria-hidden"));
			Assert.Equal(4, row.QuerySelectorAll("td .moka-skeleton").Length);
		});
		Assert.Empty(DataRows(cut));
		Assert.Equal(4 * 4, cut.FindComponents<MokaSkeleton>().Count);
	}

	[Fact]
	public void LoadingRows_SaysHowMany()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Loading, true)
			.Add(t => t.LoadingRows, 2));

		Assert.Equal(2, cut.FindAll("tbody tr").Count);
	}

	[Fact]
	public void Loading_KeepsTheControlColumns()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Loading, true)
			.Add(t => t.Selectable, true)
			.Add(t => t.Expandable, true));

		IHtmlCollection<IElement> cells = cut.Find("tbody tr").QuerySelectorAll("td");
		Assert.Equal(cut.FindAll("thead tr:first-child th").Count, cells.Length);
		Assert.Contains("moka-table-cell--expand", cells[0].ClassList);
		Assert.Contains("moka-table-cell--checkbox", cells[1].ClassList);
	}

	[Fact]
	public void Loading_SaysSoToAScreenReader()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Loading, true));

		Assert.Equal("true", cut.Find("table").GetAttribute("aria-busy"));
		Assert.Equal("Loading rows", cut.Find("[role='status']").TextContent.Trim());
	}

	[Fact]
	public void LoadingContent_TakesTheirPlace()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Loading, true)
			.Add(t => t.LoadingContent, (RenderFragment)(b => b.AddContent(0, "Fetching"))));

		Assert.Empty(cut.FindComponents<MokaSkeleton>());
		Assert.Equal("Fetching", cut.Find(".moka-table-state-cell").TextContent.Trim());
	}

	[Fact]
	public void Loading_HidesTheShowMoreFoot()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Loading, true)
			.Add(t => t.ShowMore, true)
			.Add(t => t.PageSize, 2));

		Assert.Empty(cut.FindAll(".moka-table-foot"));
	}

	[Fact]
	public void AnEmptyTable_SaysWhatItAlwaysSaid()
	{
		IRenderedComponent<MokaTable<Person>> cut = EmptyTable();

		Assert.Empty(cut.FindComponents<MokaEmptyState>());
		Assert.Equal("No data available", cut.Find(".moka-table-empty").TextContent.Trim());
		Assert.Equal("moka-table-state-cell", cut.Find("tbody td").ClassName);
	}

	[Fact]
	public void TheOldEmptyContent_StillWorks()
	{
		IRenderedComponent<MokaTable<Person>> cut = EmptyTable(p => p
			.Add(t => t.EmptyContent, (RenderFragment)(b => b.AddContent(0, "Old"))));

		Assert.Equal("Old", cut.Find("tbody td").TextContent.Trim());
		Assert.Equal("moka-table-state-cell", cut.Find("tbody td").ClassName);
	}

	[Fact]
	public void EmptyTitleAndText_ShowAnEmptyState()
	{
		IRenderedComponent<MokaTable<Person>> cut = EmptyTable(p => p
			.Add(t => t.EmptyIcon, MokaIcons.Action.Search)
			.Add(t => t.EmptyTitle, "Nobody here")
			.Add(t => t.EmptyText, "Try another filter."));

		IRenderedComponent<MokaEmptyState> state = cut.FindComponent<MokaEmptyState>();
		Assert.Equal("Nobody here", state.Find(".moka-empty-state__title").TextContent.Trim());
		Assert.Equal("Try another filter.", state.Find(".moka-empty-state__description").TextContent.Trim());
		Assert.NotEmpty(state.FindAll(".moka-empty-state__icon"));
		Assert.Equal("status", state.Find(".moka-empty-state").GetAttribute("role"));
		Assert.Contains("moka-table-state-cell--plain", cut.Find("tbody td").ClassList);
	}

	[Fact]
	public void AnyOneOfTheThree_IsEnough()
	{
		IRenderedComponent<MokaTable<Person>> cut = EmptyTable(p => p
			.Add(t => t.EmptyText, "Nothing yet."));

		Assert.Single(cut.FindComponents<MokaEmptyState>());
	}

	[Fact]
	public void TheEmptyFragment_WinsOverEverything()
	{
		IRenderedComponent<MokaTable<Person>> cut = EmptyTable(p => p
			.Add(t => t.EmptyTitle, "Nobody here")
			.Add(t => t.EmptyContent, (RenderFragment)(b => b.AddContent(0, "Old")))
			.Add(t => t.Empty, (RenderFragment)(b => b.AddContent(0, "New"))));

		Assert.Equal("New", cut.Find("tbody td").TextContent.Trim());
		Assert.Empty(cut.FindComponents<MokaEmptyState>());
	}

	[Fact]
	public void TheOldEmptyContent_WinsOverTheNewParameters()
	{
		IRenderedComponent<MokaTable<Person>> cut = EmptyTable(p => p
			.Add(t => t.EmptyTitle, "Nobody here")
			.Add(t => t.EmptyContent, (RenderFragment)(b => b.AddContent(0, "Old"))));

		Assert.Equal("Old", cut.Find("tbody td").TextContent.Trim());
	}

	[Fact]
	public void ByDefault_TheTableHasNoName_AndNothingNew()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		Assert.Null(cut.Find("table").GetAttribute("aria-label"));
		Assert.Null(cut.Find("table").GetAttribute("style"));
		Assert.Empty(cut.FindAll(".moka-table-foot-content"));
		Assert.DoesNotContain("moka-table-wrapper--page-header", cut.Find(".moka-table-wrapper").ClassList);
	}

	[Fact]
	public void Label_NamesTheTableElement_NotTheWrapper()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Label, "Devices"));

		Assert.Equal("Devices", cut.Find("table").GetAttribute("aria-label"));
		Assert.Null(cut.Find(".moka-table-wrapper").GetAttribute("aria-label"));
	}

	// A page hides a column at some widths by its class, so every cell of the column has to carry it.
	[Fact]
	public void AColumnsClass_ReachesEveryCellOfIt()
	{
		Col[] columns = [Name with { Filterable = true }, Age with { Class = "hide-narrow", Aggregate = MokaAggregateType.Sum }];

		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ShowFilters, true), columns);

		Assert.Contains("hide-narrow", cut.FindAll("thead tr:first-child th")[1].ClassList);
		Assert.Contains("hide-narrow", cut.Find(".moka-table-filter-row").Children[1].ClassList);
		Assert.All(DataRows(cut), row => Assert.Contains("hide-narrow", row.Children[1].ClassList));
		Assert.Contains("hide-narrow", cut.Find("tfoot tr").Children[1].ClassList);
		Assert.DoesNotContain("hide-narrow", cut.FindAll("thead tr:first-child th")[0].ClassList);
	}

	[Fact]
	public void AColumnsClass_ReachesTheSkeletonCells()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Loading, true), [Name, Age with { Class = "hide-narrow" }]);

		Assert.All(cut.FindAll(".moka-table-skeleton-row"), row => Assert.Contains("hide-narrow", row.Children[1].ClassList));
	}

	[Fact]
	public void AColumnsClass_SitsBesideItsCellClass_OnTheDataCells()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.ChildContent, (RenderFragment)(b =>
			{
				b.OpenComponent<MokaColumn<Person>>(0);
				b.AddComponentParameter(1, nameof(MokaColumn<Person>.Title), "Name");
				b.AddComponentParameter(2, nameof(MokaColumn<Person>.Field), (Func<Person, object?>)(x => x.Name));
				b.AddComponentParameter(3, nameof(MokaColumn<Person>.Class), "col-name");
				b.AddComponentParameter(4, nameof(MokaColumn<Person>.CellClass), "cell-name");
				b.CloseComponent();
			})));

		IElement cell = DataRows(cut)[0].Children[0];
		Assert.Contains("col-name", cell.ClassList);
		Assert.Contains("cell-name", cell.ClassList);
		Assert.DoesNotContain("cell-name", cut.Find("thead th").ClassList);
	}

	[Fact]
	public void ToolbarInFrame_DrawsTheToolbarAsTheCardsFirstRow()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Framed, true)
			.Add(t => t.ToolbarInFrame, true)
			.Add(t => t.Title, "People"));

		IElement wrapper = cut.Find(".moka-table-wrapper");
		IElement frame = wrapper.Children.Single();
		Assert.Contains("moka-table-frame", frame.ClassList);
		Assert.Contains("moka-table-toolbar", frame.Children[0].ClassList);
		Assert.Contains("moka-table-container", frame.Children[1].ClassList);
	}

	[Fact]
	public async Task ToolbarInFrame_PutsTheChooserInTheCardToo()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Framed, true)
			.Add(t => t.ToolbarInFrame, true)
			.Add(t => t.ShowColumnChooser, true));

		await cut.Find(".moka-table-chooser-toggle").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

		Assert.Equal("moka-table-frame", cut.Find(".moka-table-chooser").ParentElement!.ClassName);
		IReadOnlyDictionary<string, string> chooser = ScopedCss.Declarations(Css, ".moka-table-frame > .moka-table-chooser");
		Assert.Equal("0", chooser["border-radius"]);
		Assert.Equal("0 0 1px", chooser["border-width"]);
	}

	[Fact]
	public void ToolbarInFrame_DoesNothingWithoutAFrame()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ToolbarInFrame, true)
			.Add(t => t.Title, "People"));

		Assert.Empty(cut.FindAll(".moka-table-frame"));
		Assert.Contains("moka-table-toolbar", cut.Find(".moka-table-wrapper").Children[0].ClassList);
	}

	[Fact]
	public void FootContent_IsAPlainRowUnderTheRows_InsideTheFrame()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Framed, true)
			.Add(t => t.FootContent, (RenderFragment)(b =>
			{
				b.AddMarkupContent(0, "<span>All 4 people</span><span>Hint</span>");
			})));

		IElement foot = cut.Find(".moka-table-foot-content");
		Assert.Equal("moka-table-frame", foot.ParentElement!.ClassName);
		Assert.Equal("moka-table-foot-content", foot.ParentElement.LastElementChild!.ClassName);
		Assert.Equal(["All 4 people", "Hint"], foot.Children.Select(c => c.TextContent));
		Assert.Empty(cut.FindAll("tfoot"));
	}

	[Fact]
	public void FootContent_ComesAfterTheShowMoreFoot()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ShowMore, true)
			.Add(t => t.PageSize, 2)
			.Add(t => t.FootContent, (RenderFragment)(b => b.AddContent(0, "All people"))));

		IElement foot = cut.Find(".moka-table-foot-content");
		Assert.Contains("moka-table-foot", foot.PreviousElementSibling!.ClassList);
	}

	[Fact]
	public void FootContent_LooksLikeTheFoot()
	{
		IReadOnlyDictionary<string, string> foot = ScopedCss.Declarations(Css, ".moka-table-foot-content");

		Assert.Equal("space-between", foot["justify-content"]);
		Assert.Equal("wrap", foot["flex-wrap"]);
		Assert.Equal("1px solid var(--moka-color-outline-variant)", foot["border-top"]);
		Assert.Equal("var(--moka-color-on-surface-variant)", foot["color"]);
		Assert.Equal("0", ScopedCss.Declarations(Css,
			".moka-table-container:has(+ .moka-table-foot-content) > .moka-table > tbody > tr:last-child > td")["border-bottom"]);
	}

	[Fact]
	public void MinWidth_GoesOnTheTable_AndMakesTheContainerScroll()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Responsive, false)
			.Add(t => t.MinWidth, "960px"));

		Assert.Equal("min-width: 960px", cut.Find("table").GetAttribute("style")!.TrimEnd(';').Trim());
		Assert.Contains("moka-table-container--responsive", cut.Find(".moka-table-container").ClassList);
		Assert.DoesNotContain("moka-table-wrapper--page-header", cut.Find(".moka-table-wrapper").ClassList);
	}

	[Fact]
	public void MinWidth_ThatCouldEndItsDeclaration_IsDropped()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.MinWidth, "1px; color: red"));

		Assert.Null(cut.Find("table").GetAttribute("style"));
	}

	[Fact]
	public void WithNothingScrollingAroundTheRows_TheHeaderSticksToThePage()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Responsive, false));

		Assert.Contains("moka-table-wrapper--page-header", cut.Find(".moka-table-wrapper").ClassList);
		Assert.DoesNotContain("moka-table-container--responsive", cut.Find(".moka-table-container").ClassList);
		Assert.Equal("clip", ScopedCss.Declarations(Css, ".moka-table-wrapper--page-header")["overflow"]);
		Assert.Equal("var(--moka-appbar-space, 0px)", ScopedCss.Declarations(Css,
			".moka-gate .moka-table-wrapper--page-header > .moka-table-frame > .moka-table-container > .moka-table > thead")["top"]);
	}

	[Fact]
	public void AHeightMakesTheTableScrollOnItsOwn_SoTheHeaderStaysInIt()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Responsive, false)
			.Add(t => t.Height, "20rem"));

		Assert.DoesNotContain("moka-table-wrapper--page-header", cut.Find(".moka-table-wrapper").ClassList);
	}

	private IRenderedComponent<MokaTable<Person>> EmptyTable(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, []);
			p.Add(t => t.ChildContent, DefaultColumns);
			extra?.Invoke(p);
		});

	private IRenderedComponent<MokaTable<Person>> Table(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null,
		Col[]? columns = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.ChildContent, columns is null ? DefaultColumns : Columns(columns));
			extra?.Invoke(p);
		});
}
