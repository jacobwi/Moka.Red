using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// Segments grow equally from a zero basis, so where an engine took a segment's content minimum as less than its label, a
// row of segments came out equal and the longer labels were cut ("D...", "Li...", "Both"). The wrapping track now gives
// every segment its label's width as a floor, and only the one-row grid, which cuts labels on purpose, opts out.
public class MokaSegmentedControlLabelWidthTests
{
	private const string Css = "MokaSegmentedControl.razor.css";
	private const string WrappingSegment = ".moka-segmented:not(.moka-segmented--one-row) ::deep .moka-segment";

	[Fact]
	public void InAWrappingTrack_ASegmentIsNeverNarrowerThanItsLabel() =>
		Assert.Equal("max-content", ScopedCss.Declarations(Css, WrappingSegment)["min-width"]);

	[Fact]
	public void TheSegments_StillShareTheRow()
	{
		Assert.Equal("1", ScopedCss.Declarations(Css, ".moka-segmented ::deep .moka-segment")["flex"]);
		Assert.Equal("wrap", ScopedCss.Declarations(Css, ".moka-segmented")["flex-wrap"]);
	}

	[Fact]
	public void TheOneRowGrid_KeepsLettingLabelsGive() =>
		Assert.Equal("0", ScopedCss.Declarations(Css, ".moka-segmented--one-row ::deep .moka-segment")["min-width"]);

	// StackBelow sets only the basis, so a stacked segment keeps the floor; nothing else in the stylesheet sets a minimum
	// width on a segment that would undo it.
	[Fact]
	public void NoOtherRule_SetsASegmentsMinimumWidth()
	{
		string[] others =
		[
			".moka-segmented ::deep .moka-segment",
			".moka-segmented--stack ::deep .moka-segment",
			".moka-gate .moka-segmented ::deep .moka-segment",
			".moka-gate .moka-segmented--xs ::deep .moka-segment",
			".moka-gate .moka-segmented--sm ::deep .moka-segment",
			".moka-gate .moka-segmented.moka-segmented--control-height ::deep .moka-segment"
		];

		Assert.All(others, selector => Assert.DoesNotContain("min-width", ScopedCss.Declarations(Css, selector).Keys));
	}
}
