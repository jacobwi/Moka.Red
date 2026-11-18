using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Conventions;

// Virtualize sizes its items by the bounding box of a range from one spacer to the other, and a range counts every text
// node inside it. A scroll container neither clips nor scrolls an absolutely positioned descendant whose containing
// block lies outside it, so a visually hidden span in an item stayed where it sat with nothing scrolled, and the items
// measured as tall as the scroll offset. Under a table's remote windows every request then cancelled the last.
public sealed class VirtualizedScrollerTests
{
	[Theory]
	[InlineData("MokaTable.razor.css", ".moka-table-container")]
	[InlineData("MokaVirtualList.razor.css", ".moka-virtual-list")]
	[InlineData("MokaFileBrowser.razor.css", ".moka-file-browser__list")]
	[InlineData("MokaChartPlot.razor.css", ".moka-chart-plot__rbody")]
	public void WhatVirtualizedItemsScrollIn_HoldsWhatTheyPositionAbsolutely(string file, string selector) =>
		Assert.Equal("relative", ScopedCss.Declarations(file, selector)["position"]);
}
