using Moka.Red.Core.Base;
using Moka.Red.Feedback.JobProgress;

namespace Moka.Red.Feedback.Tests.Components;

// Code compiled against a package from before the move still looks for the enum in Feedback.
public class MokaByteUnitsForwardTests
{
	[Fact]
	public void TheEnum_LivesInCore_UnderTheNamespaceItAlwaysHad()
	{
		Assert.Same(typeof(MokaComponentBase).Assembly, typeof(MokaByteUnits).Assembly);
		Assert.Equal("Moka.Red.Feedback.JobProgress", typeof(MokaByteUnits).Namespace);
	}

	[Fact]
	public void TheFeedbackAssembly_ForwardsIt() =>
		Assert.Contains(typeof(MokaByteUnits), typeof(MokaJobProgress).Assembly.GetForwardedTypes());
}
