using Moka.Red.Primitives.Callout;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// The track clipped its overflow and the segments never wrapped, so in a narrow row the last options
// were hidden with nothing on screen saying they existed.
public class MokaSegmentedControlWrapTests
{
	[Fact]
	public void TheTrackWrapsInsteadOfHidingOptions() =>
		Assert.Equal(
			"wrap",
			ScopedCss.Declarations("MokaSegmentedControl.razor.css", ".moka-segmented")["flex-wrap"]);
}

// Callers coming from other libraries reach for Info and found only Note.
public class MokaCalloutInfoTests
{
	[Fact]
	public void InfoIsTheSameCalloutAsNote() =>
		Assert.Equal(MokaCalloutType.Note, MokaCalloutType.Info);

	// Declared as an alias, so the types after it keep the values they had.
	[Theory]
	[InlineData(MokaCalloutType.Tip, 1)]
	[InlineData(MokaCalloutType.Important, 2)]
	[InlineData(MokaCalloutType.Warning, 3)]
	[InlineData(MokaCalloutType.Caution, 4)]
	public void TheOtherTypesKeptTheirValues(MokaCalloutType type, int expected) =>
		Assert.Equal(expected, (int)type);
}
