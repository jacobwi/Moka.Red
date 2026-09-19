using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Utilities;

// Boxes at width: 100% or height: 100% overflowed their container by their margin. The fill
// utilities in moka.css size them with the margin inside, and fall back to 100% in a browser that
// knows none of the keywords, since it keeps the last value it understands.
public class FillUtilitiesTests
{
	[Fact]
	public void FillWidth_FallsBackFrom_Stretch_ToTheVendorKeywords_To100Percent()
	{
		Assert.Equal(["100%", "-moz-available", "-webkit-fill-available", "stretch"],
			ScopedCss.Values("moka.css", ".moka-fill-width", "width"));
	}

	[Fact]
	public void FillHeight_FallsBackFrom_Stretch_ToTheWebKitKeyword_To100Percent()
	{
		Assert.Equal(["100%", "-webkit-fill-available", "stretch"],
			ScopedCss.Values("moka.css", ".moka-fill-height", "height"));
	}
}
