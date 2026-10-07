using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// The splitter was 1px wide and reached for a 12px grab zone with a ::before centred on its edge. The panel
// sets overflow: hidden, so it clipped the outer half of that zone and left about a pixel to aim at: hovering
// the panel edge showed no resize cursor. The grab zone is the splitter's own width now, inside the panel.
public class MokaDockPanelSplitterTests
{
	private const string Css = "MokaDockPanel.razor.css";

	[Theory]
	[InlineData(".moka-dock-panel-splitter--left")]
	[InlineData(".moka-dock-panel-splitter--right")]
	public void AVerticalSplitterHasAGrabZone(string selector)
	{
		IReadOnlyDictionary<string, string> rules = ScopedCss.Declarations(Css, selector);

		Assert.Equal("6px", rules["width"]);
		Assert.Equal("col-resize", rules["cursor"]);
	}

	[Theory]
	[InlineData(".moka-dock-panel-splitter--top")]
	[InlineData(".moka-dock-panel-splitter--bottom")]
	public void AHorizontalSplitterHasAGrabZone(string selector)
	{
		IReadOnlyDictionary<string, string> rules = ScopedCss.Declarations(Css, selector);

		Assert.Equal("6px", rules["height"]);
		Assert.Equal("row-resize", rules["cursor"]);
	}

	// A pseudo-element reaching outside the splitter is what the panel clipped, so it must not come back.
	[Theory]
	[InlineData(".moka-dock-panel-splitter--left::before")]
	[InlineData(".moka-dock-panel-splitter--right::before")]
	[InlineData(".moka-dock-panel-splitter--top::before")]
	[InlineData(".moka-dock-panel-splitter--bottom::before")]
	public void NoHitAreaReachesOutsideThePanel(string selector) =>
		Assert.Empty(ScopedCss.Declarations(Css, selector));

	// The panel clips its overflow, which is why the grab zone has to sit inside it.
	[Fact]
	public void ThePanelStillClipsItsOverflow() =>
		Assert.Equal("hidden", ScopedCss.Declarations(Css, ".moka-dock-panel")["overflow"]);
}
