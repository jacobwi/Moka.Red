using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Layout.DockLayout;
using Moka.Red.Primitives.Tooltip;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// The header's buttons show only icons, so each is named by aria-label and a MokaTooltip, never a title: below the
// row, the last one lined up with the group's edge.
public class MokaDockPanelTooltipTests : BunitContext
{
	public MokaDockPanelTooltipTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TheHeaderButtons_AreNamedByTooltipsBelowTheRow()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderPanel(MokaDockPosition.Left, MokaDockControls.Default);

		IconTooltip.AssertNamed(Button(cut, "Maximize"), "Maximize", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(Button(cut, "Undock"), "Undock", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(Button(cut, "Panel options"), "Panel options", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(Button(cut, "Collapse"), "Collapse", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void TheLastButton_EndsTheRow()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderPanel(MokaDockPosition.Left, MokaDockControls.Floating);

		IconTooltip.AssertNamed(Button(cut, "Undock"), "Undock", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void AFloatingGroup_NamesItsDockButton()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderPanel(MokaDockPosition.Left, MokaDockControls.Floating);

		Button(cut, "Undock").Click();

		IconTooltip.AssertNamed(Button(cut, "Dock"), "Dock", MokaTooltipPosition.BottomEnd);
	}

	// A collapsed side strip is a column: the tooltip opens beside it, away from the window's edge.
	[Theory]
	[InlineData(MokaDockPosition.Left, MokaTooltipPosition.Right)]
	[InlineData(MokaDockPosition.Right, MokaTooltipPosition.Left)]
	[InlineData(MokaDockPosition.Top, MokaTooltipPosition.BottomEnd)]
	[InlineData(MokaDockPosition.Bottom, MokaTooltipPosition.TopEnd)]
	public void ACollapsedStrip_PlacesTheExpandTooltipAwayFromTheEdge(MokaDockPosition dock, MokaTooltipPosition position)
	{
		IRenderedComponent<MokaDockLayout> cut = RenderPanel(dock, MokaDockControls.Default);

		Button(cut, "Collapse").Click();

		IconTooltip.AssertNamed(Button(cut, "Expand"), "Expand", position);
	}

	[Fact]
	public void EditorTabs_NameTheirCloseButtons()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p
			.Add(x => x.TabStyle, MokaDockTabStyle.Editor)
			.AddChildContent(DockKit.Group("docs", null,
				DockKit.Panel("a", "Alpha", extra: new Dictionary<string, object?> { [nameof(MokaDockPanel.Closable)] = true }))));

		IconTooltip.AssertNamed(cut.Find(".moka-dock-tab__close"), "Close Alpha", MokaTooltipPosition.Bottom);
	}

	private static IElement Button(IRenderedComponent<MokaDockLayout> cut, string name) =>
		cut.Find($".moka-dock-panel-header button[aria-label='{name}']");

	private IRenderedComponent<MokaDockLayout> RenderPanel(MokaDockPosition dock, MokaDockControls controls) =>
		Render<MokaDockLayout>(p => p
			.Add(x => x.Controls, controls)
			.AddChildContent(DockKit.Parts(
				DockKit.Panel("explorer", "Explorer", extra: new Dictionary<string, object?>
				{
					[nameof(MokaDockPanel.Dock)] = dock,
					[nameof(MokaDockPanel.Collapsible)] = true,
					[nameof(MokaDockPanel.CollapsedSize)] = "32px"
				}),
				DockKit.Content())));
}
