using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;
using Moka.Red.Navigation.Menu;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// A collapsed menu shows only icons, and each item was named by a title, so the browser's own pale, late hint
// showed instead of Moka's. The collapsed menu is a column, so the name opens beside it.
public class MokaMenuItemTooltipTests : BunitContext
{
	[Fact]
	public void ACollapsedLink_IsNamedByATooltipBesideTheMenu()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(collapsed: true, item => item.Add(x => x.Href, "/reports"));

		IconTooltip.AssertNamed(cut.Find("a.moka-menu-item"), "Reports", MokaTooltipPosition.Right);
	}

	[Fact]
	public void ACollapsedButton_IsNamedByATooltipBesideTheMenu()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(collapsed: true, item => item.Add(x => x.OnClick, _ => { }));

		IconTooltip.AssertNamed(cut.Find("button.moka-menu-item"), "Reports", MokaTooltipPosition.Right);
	}

	[Fact]
	public void ACollapsedParent_IsNamedByATooltipBesideTheMenu()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(collapsed: true, item => item
			.AddChildContent<MokaMenuItem>(child => child.Add(x => x.Text, "Monthly").Add(x => x.Href, "/monthly")));

		IconTooltip.AssertNamed(cut.Find("button.moka-menu-item"), "Reports", MokaTooltipPosition.Right);
	}

	// A div cannot take a name, so a static item keeps its text for screen readers while the tooltip shows it.
	[Fact]
	public void ACollapsedStaticItem_KeepsItsTextForScreenReaders()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(collapsed: true, _ => { });

		IElement item = cut.Find("div.moka-menu-item");
		Assert.False(item.HasAttribute("title"));
		Assert.Equal("Reports", item.QuerySelector(".moka-visually-hidden")?.TextContent);
		Assert.Equal("Reports", item.ParentElement!.QuerySelector(":scope > .moka-tooltip-popup")?.TextContent.Trim());
	}

	// The text shows, so there is nothing to name and no tooltip around the item.
	[Fact]
	public void AnExpandedMenu_DrawsItsItemsWithoutTooltips()
	{
		IRenderedComponent<MokaMenu> cut = RenderMenu(collapsed: false, item => item.Add(x => x.Href, "/reports"));

		IElement link = cut.Find("a.moka-menu-item");
		Assert.False(link.HasAttribute("title"));
		Assert.False(link.HasAttribute("aria-label"));
		Assert.Empty(cut.FindAll(".moka-tooltip-trigger"));
	}

	// The tooltip's element is inline-flex; in a collapsed menu it has to stretch across the menu as the item did.
	[Fact]
	public void TheTooltipAroundACollapsedItem_StretchesAcrossTheMenu() =>
		Assert.Equal(
			"flex",
			ScopedCss.Declarations("MokaMenu.razor.css", ".moka-menu ::deep .moka-menu-item-tooltip")["display"]);

	private IRenderedComponent<MokaMenu> RenderMenu(
		bool collapsed,
		Action<ComponentParameterCollectionBuilder<MokaMenuItem>> item) =>
		Render<MokaMenu>(p => p
			.Add(x => x.Collapsed, collapsed)
			.AddChildContent<MokaMenuItem>(i =>
			{
				i.Add(x => x.Text, "Reports").Add(x => x.Icon, MokaIcons.File.Document);
				item(i);
			}));
}
