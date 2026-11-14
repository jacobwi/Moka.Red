using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// The splitter was a band inside the panel, clipped by the panel's overflow (gotcha 26). It is a gutter between two
// cells now: a 1px track whose grab zone reaches over both from a pseudo-element no ancestor clips, above the panel
// hosts that follow it in the document.
public class MokaDockPanelSplitterTests
{
	private const string Css = "MokaDockFrame.razor.css";

	[Theory]
	[InlineData(".moka-dock-split--row > .moka-dock-gutter", "col-resize")]
	[InlineData(".moka-dock-split--column > .moka-dock-gutter", "row-resize")]
	public void AGutter_ShowsTheResizeCursor(string selector, string cursor) =>
		Assert.Equal(cursor, ScopedCss.Declarations(Css, selector)["cursor"]);

	[Fact]
	public void AGutter_SitsAboveThePanelHosts() =>
		Assert.Equal("1", ScopedCss.Declarations(Css, ".moka-dock-split > .moka-dock-gutter")["z-index"]);

	[Theory]
	[InlineData(".moka-dock-split--row > .moka-dock-gutter::before", "inset-inline")]
	[InlineData(".moka-dock-split--column > .moka-dock-gutter::before", "inset-block")]
	public void TheGrabZone_ReachesOverBothNeighbours(string selector, string property) =>
		Assert.Equal("calc(var(--moka-spacing-xs) * -1)", ScopedCss.Declarations(Css, selector)[property]);

	// Splits nest, so a split's rules reach its own gutters and cells with child combinators (gotcha 27).
	[Fact]
	public void SplitRules_UseChildCombinators()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css));

		Assert.DoesNotContain(".moka-dock-split .moka-dock-gutter", css, StringComparison.Ordinal);
		Assert.DoesNotContain(".moka-dock-split--row .moka-dock-gutter", css, StringComparison.Ordinal);
	}
}
