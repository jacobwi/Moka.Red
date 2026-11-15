using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// A toggle group's items were --moka-segment-height at every size. Under MokaTheme.WithControlHeights(Matched) the group
// is as tall as a field of its size; otherwise the variable is invalid and the items keep the segment height.
public class MokaToggleGroupHeightTests
{
	[Theory]
	[InlineData(".moka-gate .moka-toggle-group--xs", "calc(var(--moka-matched-height-xs) - 2 * var(--moka-border-width))")]
	[InlineData(".moka-gate .moka-toggle-group--sm", "calc(var(--moka-matched-height-sm) - 2 * var(--moka-border-width))")]
	[InlineData(".moka-gate .moka-toggle-group--md", "calc(var(--moka-matched-height) - 2 * var(--moka-border-width))")]
	[InlineData(".moka-gate .moka-toggle-group--lg", "calc(var(--moka-matched-height-lg) - 2 * var(--moka-border-width))")]
	public void EachSize_LeavesItsItemsWhatItsBorderLeaves(string selector, string height) =>
		Assert.Equal(height, ScopedCss.Declarations("MokaToggleGroup.razor.css", selector)["--_toggle-height"]);

	[Fact]
	public void TheItems_FallBackToTheSegmentHeight() =>
		Assert.Equal("var(--_toggle-height, var(--moka-segment-height))",
			ScopedCss.Declarations("MokaToggleGroupItem.razor.css", ".moka-gate .moka-toggle-group-item")["min-height"]);
}
