using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// A nested split is the same component with the same CSS-isolation scope, so the direction rules used
// to reach it from the outer split's root: a vertical split inside a horizontal one matched both axes
// and its handle came out a few pixels square. Every rule that styles a split's own parts is a child
// selector now, which is the only thing keeping them apart.
public class MokaSplitPaneNestingTests
{
	private const string Css = "MokaSplitPane.razor.css";

	[Theory]
	[InlineData(".moka-split-pane--horizontal > .moka-split-pane-handle", "width")]
	[InlineData(".moka-split-pane--vertical > .moka-split-pane-handle", "height")]
	public void TheHandleTakesItsAxisFromItsOwnSplit(string selector, string property) =>
		Assert.Equal("var(--moka-spacing-xs)", ScopedCss.Declarations(Css, selector)[property]);

	[Theory]
	[InlineData(".moka-split-pane--horizontal > .moka-split-pane-first", "min-width")]
	[InlineData(".moka-split-pane--horizontal > .moka-split-pane-second", "min-width")]
	[InlineData(".moka-split-pane--vertical > .moka-split-pane-first", "min-height")]
	[InlineData(".moka-split-pane--vertical > .moka-split-pane-second", "min-height")]
	public void EachHalfTakesItsMinimumFromItsOwnSplit(string selector, string property) =>
		Assert.Equal("var(--moka-split-min, 100px)", ScopedCss.Declarations(Css, selector)[property]);

	[Theory]
	[InlineData(".moka-split-pane--horizontal > .moka-split-pane-handle > .moka-split-pane-handle-bar", "width")]
	[InlineData(".moka-split-pane--vertical > .moka-split-pane-handle > .moka-split-pane-handle-bar", "height")]
	public void TheGripTakesItsAxisFromItsOwnSplit(string selector, string property) =>
		Assert.Equal("2px", ScopedCss.Declarations(Css, selector)[property]);

	// The shapes that leaked. None of them may come back as a descendant selector.
	[Theory]
	[InlineData(".moka-split-pane--horizontal .moka-split-pane-handle")]
	[InlineData(".moka-split-pane--vertical .moka-split-pane-handle")]
	[InlineData(".moka-split-pane--horizontal .moka-split-pane-first")]
	[InlineData(".moka-split-pane--vertical .moka-split-pane-second")]
	[InlineData(".moka-split-pane--horizontal .moka-split-pane-handle-bar")]
	[InlineData(".moka-split-pane--vertical .moka-split-pane-handle-bar")]
	[InlineData(".moka-split-pane--dragging .moka-split-pane-handle")]
	public void NoDirectionRuleReachesPastItsOwnSplit(string selector) =>
		Assert.Empty(ScopedCss.Declarations(Css, selector));

	// Only the split being dragged keeps its handle live; everything else inside it stops taking clicks.
	[Fact]
	public void DraggingKeepsOnlyItsOwnHandleClickable()
	{
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-split-pane--dragging *")["pointer-events"]);
		Assert.Equal(
			"auto",
			ScopedCss.Declarations(Css, ".moka-split-pane--dragging > .moka-split-pane-handle")["pointer-events"]);
	}
}
