using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Tests.Utilities;

public class MokaByteFormatTests
{
	[Theory]
	[InlineData(148_000_000d, 240_000_000d, "148", "240 MB")]
	[InlineData(900_000d, 2_000_000d, "900 KB", "2 MB")]
	[InlineData(0d, 2_000_000d, "0", "2 MB")]
	public void Pair_SaysTheDoneUnitOnlyWhenItDiffers(double done, double whole, string expectedDone, string expectedWhole) =>
		Assert.Equal((expectedDone, expectedWhole), MokaByteFormat.Pair(done, whole));
}
