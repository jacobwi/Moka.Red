using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Layout.DockLayout;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// The header's toggles were named by title, so the browser's own pale, late hint showed beside the Moka tooltips
// an app draws around its own buttons in the same header.
public class MokaDockPanelTooltipTests : BunitContext
{
	public MokaDockPanelTooltipTests() =>
		JSInterop.SetupModule("./_content/Moka.Red.Core/moka-drag.js").Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TheHeaderToggles_AreNamedByTooltipsBelowTheRow()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderPanel(MokaDockPosition.Left, collapsible: true);

		// Undock is not the last in the row, so it centres; Collapse ends the row and lines up with the panel's edge.
		IconTooltip.AssertNamed(Toggle(cut, "Undock"), "Undock", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(Toggle(cut, "Collapse"), "Collapse", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void UndockEndsTheRow_WhenThePanelCannotCollapse()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderPanel(MokaDockPosition.Left, collapsible: false);

		IconTooltip.AssertNamed(Toggle(cut, "Undock"), "Undock", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void AFloatingPanel_NamesItsDockButton()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderPanel(MokaDockPosition.Left, collapsible: true);

		Toggle(cut, "Undock").Click();

		IconTooltip.AssertNamed(Toggle(cut, "Dock"), "Dock", MokaTooltipPosition.BottomEnd);
	}

	// A collapsed side strip is a column: below the expand button is the title running down the strip, so the
	// tooltip opens beside the strip, on the side away from the window's edge.
	[Theory]
	[InlineData(MokaDockPosition.Left, MokaTooltipPosition.Right)]
	[InlineData(MokaDockPosition.Right, MokaTooltipPosition.Left)]
	[InlineData(MokaDockPosition.Top, MokaTooltipPosition.BottomEnd)]
	[InlineData(MokaDockPosition.Bottom, MokaTooltipPosition.TopEnd)]
	public void ACollapsedStrip_PlacesTheExpandTooltipAwayFromTheEdge(MokaDockPosition dock, MokaTooltipPosition position)
	{
		IRenderedComponent<MokaDockLayout> cut = RenderPanel(dock, collapsible: true);

		Toggle(cut, "Collapse").Click();

		IconTooltip.AssertNamed(Toggle(cut, "Expand"), "Expand", position);
	}

	private static IElement Toggle(IRenderedComponent<MokaDockLayout> cut, string name) =>
		cut.Find($".moka-dock-panel-toggle[aria-label={name}]");

	private IRenderedComponent<MokaDockLayout> RenderPanel(MokaDockPosition dock, bool collapsible) =>
		Render<MokaDockLayout>(p => p.AddChildContent(builder =>
		{
			builder.OpenComponent<MokaDockPanel>(0);
			builder.AddComponentParameter(1, nameof(MokaDockPanel.Dock), dock);
			builder.AddComponentParameter(2, nameof(MokaDockPanel.Title), "Explorer");
			builder.AddComponentParameter(3, nameof(MokaDockPanel.Collapsible), collapsible);
			builder.AddComponentParameter(4, nameof(MokaDockPanel.CollapsedSize), "32px");
			builder.AddComponentParameter(5, nameof(MokaDockPanel.ChildContent), (RenderFragment)(body => body.AddContent(0, "body")));
			builder.CloseComponent();
		}));
}
