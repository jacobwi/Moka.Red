using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Tests.Shared;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// The toolbar's icon buttons were named by title, so the browser's own pale, late hint showed instead of Moka's.
public class MokaTableTooltipTests : BunitContext
{
	public MokaTableTooltipTests()
	{
		JSInterop.SetupModule(TableModule).Mode = JSRuntimeMode.Loose;
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	// A row along the top of the table: names open below it, and the last button lines up with the table's edge.
	[Fact]
	public void TheToolbarButtons_AreNamedByTooltipsBelowTheToolbar()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.ShowColumnToggle, true)
			.Add(t => t.Exportable, true)
			.Add(t => t.ShowRefresh, true)
			.Add(t => t.ShowDensityToggle, true));

		IconTooltip.AssertNamed(cut.Find(".moka-table-column-toggle-btn"), "Toggle columns", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(cut.Find("button[aria-label=Export]"), "Export", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(cut.Find("button[aria-label=Refresh]"), "Refresh", MokaTooltipPosition.Bottom);
		// A table starts dense, so the button offers the other density.
		IconTooltip.AssertNamed(cut.Find("button[aria-label=Comfortable]"), "Comfortable", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void TheLastButton_LeavesTheEdgeToTheAppsOwnToolbarContent()
	{
		RenderFragment content = builder => builder.AddContent(0, "app");
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.ShowRefresh, true)
			.Add(t => t.ToolbarContent, content));

		IconTooltip.AssertNamed(cut.Find("button[aria-label=Refresh]"), "Refresh", MokaTooltipPosition.Bottom);
	}

	[Fact]
	public async Task TheDensityButton_NamesWhatItSwitchesTo()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.ShowDensityToggle, true));

		await cut.Find("button[aria-label=Comfortable]").ClickAsync(new MouseEventArgs());

		IconTooltip.AssertNamed(cut.Find("button[aria-label=Dense]"), "Dense", MokaTooltipPosition.BottomEnd);
	}

	// Open, the menu sits right under the button, where the tooltip would cover it.
	[Fact]
	public async Task TheColumnToggle_ShowsNoTooltipWhileItsMenuIsOpen()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.ShowColumnToggle, true));

		await cut.Find(".moka-table-column-toggle-btn").ClickAsync(new MouseEventArgs());

		IElement button = cut.Find(".moka-table-column-toggle-btn");
		Assert.Equal("Toggle columns", button.GetAttribute("aria-label"));
		Assert.Equal("true", button.GetAttribute("aria-expanded"));
		Assert.Empty(cut.FindAll(".moka-table-column-toggle .moka-tooltip-popup"));
	}

	[Fact]
	public void TheSearchClearButton_IsNamedByATooltip()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.Searchable, true));

		cut.Find(".moka-table-search input").Input("ada");

		// The search waits for typing to stop before it runs, and the clear button comes with the term.
		IElement clear = cut.WaitForElement(".moka-table-search-clear", TimeSpan.FromSeconds(5));
		IconTooltip.AssertNamed(clear, "Clear search", MokaTooltipPosition.Bottom);
	}

	// The toggles make a column down the table, so the name opens beside it.
	[Fact]
	public void TheDetailToggle_IsNamedByATooltipBesideTheColumn()
	{
		RenderFragment<Person> detail = person => builder => builder.AddContent(0, person.City);
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.Expandable, true)
			.Add(t => t.DetailTemplate, detail));

		IconTooltip.AssertNamed(cut.Find(".moka-table-expand-btn"), "Toggle detail", MokaTooltipPosition.Right);
	}

	// A tooltip shown as a drag starts would ride along in the drag image, so a handle that can drag has none.
	[Fact]
	public void AHandleThatCanDrag_HasNoTooltip()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p.Add(t => t.RowReorderable, true));

		IElement handle = cut.Find("tbody td.moka-table-cell--reorder");
		Assert.Equal("true", handle.GetAttribute("draggable"));
		Assert.Null(handle.QuerySelector(".moka-tooltip-trigger"));
		Assert.False(handle.HasAttribute("aria-description"));
	}

	private IRenderedComponent<MokaTable<Person>> RenderTable(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>> configure) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People).Add(t => t.ChildContent, DefaultColumns);
			configure(p);
		});
}
