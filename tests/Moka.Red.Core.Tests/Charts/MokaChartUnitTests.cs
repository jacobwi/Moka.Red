using System.Globalization;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.JobProgress;

namespace Moka.Red.Core.Tests.Charts;

public class MokaChartUnitTests
{
	public static TheoryData<double> ByteCounts =>
	[
		0, 1, 0.5, 9.5, 99.4, 99.96, 100, 512, 999, 999.4, 999.6, 1000, 1023, 1024, 1500, 1536, 10_240, 99_950, 100_000,
		149_999, 999_499, 999_960, 1_000_000, 1_048_576, 1_572_864, 12_345_678, 999_950_000, 1_000_000_000, 1_073_741_824,
		5.5e12, 1.2e15, 9.99e17, 2e18, -1, -1500, -2_500_000, double.NaN, double.PositiveInfinity, 123.456, 7_777_777
	];

	[Theory]
	[MemberData(nameof(ByteCounts))]
	public void Bytes_WriteWhatMokaByteFormatWrites(double bytes)
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		try
		{
			Assert.Equal(MokaByteFormat.Format(bytes), MokaChartUnit.Bytes.Format(bytes));
			Assert.Equal(MokaByteFormat.Format(bytes, MokaByteUnits.Binary), MokaChartUnit.BytesBinary.Format(bytes));
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	[Fact]
	public void TheByteUnits_CoverForty() => Assert.Equal(40, ByteCounts.Count);

	[Theory]
	[InlineData(9_500d, "9500")]
	[InlineData(12_000d, "12K")]
	[InlineData(3_400_000d, "3.4M")]
	[InlineData(1_200_000_000d, "1.2B")]
	public void Compact_GoesShortFromTenThousand(double value, string expected) =>
		Assert.Equal(expected, MokaChartUnit.Compact.Format(value, culture: CultureInfo.InvariantCulture));

	[Fact]
	public void Compact_MovesUpWhenRoundingReachesTheNextStep() =>
		Assert.Equal("10K", MokaChartUnit.Compact.Format(9_999.6, culture: CultureInfo.InvariantCulture));

	[Fact]
	public void Percent_SitsRightAfterTheNumber() =>
		Assert.Equal("12%", MokaChartUnit.Percent.Format(12, culture: CultureInfo.InvariantCulture));

	[Fact]
	public void Of_IsOneStep() => Assert.Equal("5 req/s", MokaChartUnit.Of("req/s").Format(5, culture: CultureInfo.InvariantCulture));

	[Theory]
	[InlineData(500d, "ms")]
	[InlineData(1_500d, "s")]
	[InlineData(90_000d, "min")]
	[InlineData(7_200_000d, "h")]
	public void Milliseconds_StepUpThroughSecondsMinutesAndHours(double top, string suffix) =>
		Assert.Equal(suffix, MokaChartUnit.Milliseconds.StepFor(top).Suffix);

	[Fact]
	public void MinutesAndHours_CarryClockFriendlyMultiples()
	{
		Assert.Equal([1d, 2, 5, 10, 15, 30], MokaChartUnit.Milliseconds.StepFor(120_000).Multiples);
		Assert.Equal([1d, 2, 3, 6, 12], MokaChartUnit.Seconds.StepFor(7_200).Multiples);
	}

	[Fact]
	public void Decimals_AreAlwaysThatMany() =>
		Assert.Equal("1.50 KB", MokaChartUnit.Bytes.Format(1_500, 2, CultureInfo.InvariantCulture));

	[Fact]
	public void BitsPerSecond_UseTheirOwnNames() =>
		Assert.Equal("1.2 Gbps", MokaChartUnit.BitsPerSecond.Format(1.2e9, culture: CultureInfo.InvariantCulture));
}
