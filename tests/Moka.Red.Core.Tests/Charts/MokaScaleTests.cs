using System.Globalization;
using Moka.Red.Core.Charts;

namespace Moka.Red.Core.Tests.Charts;

public class MokaScaleTests
{
	private static string[] Texts(MokaScale scale) =>
		[.. scale.Ticks.Select(tick => scale.Format(tick, culture: CultureInfo.InvariantCulture))];

	[Theory]
	[InlineData(1, 1)]
	[InlineData(1.1, 2)]
	[InlineData(2.2, 2.5)]
	[InlineData(3, 4)]
	[InlineData(4.5, 5)]
	[InlineData(7, 10)]
	[InlineData(0.07, 0.1)]
	[InlineData(0.3, 0.4)]
	[InlineData(2950, 4000)]
	[InlineData(26, 40)]
	public void NiceStep_RoundsUpToARoundStep(double raw, double expected) =>
		Assert.Equal(expected, MokaNiceScale.NiceStep(raw));

	[Theory]
	[InlineData(12, 15)]
	[InlineData(3, 5)]
	[InlineData(20, 30)]
	[InlineData(0.3, 0.4)]
	public void NiceStep_UsesTheUnitsMultiples_WhenTheStepFallsAmongThem(double raw, double expected) =>
		Assert.Equal(expected, MokaNiceScale.NiceStep(raw, [1, 2, 5, 10, 15, 30]));

	[Fact]
	public void NiceStep_OfNothing_IsOne()
	{
		Assert.Equal(1, MokaNiceScale.NiceStep(0));
		Assert.Equal(1, MokaNiceScale.NiceStep(double.NaN));
	}

	// The board's S3A card: data up to 11,800 reads 4K, 8K and 12K.
	[Fact]
	public void TheBoardsHairlines_ReadFourEightAndTwelveK()
	{
		MokaScale scale = MokaNiceScale.Linear(0, 11_800, new MokaScaleOptions { Unit = MokaChartUnit.Compact });

		Assert.Equal([0d, 4_000, 8_000, 12_000], scale.Ticks);
		Assert.Equal(["0", "4K", "8K", "12K"], Texts(scale));
		Assert.Equal(4_000, scale.Step);
	}

	[Fact]
	public void WithoutAUnit_TicksAreGrouped() =>
		Assert.Equal(["0", "4,000", "8,000", "12,000"], Texts(MokaNiceScale.Linear(0, 11_800, new MokaScaleOptions())));

	// Rounded in KB the top reaches 1,000 KB, which is MB, so the step is rounded again in MB.
	[Fact]
	public void AStepThatCarriesTheTopIntoTheNextUnit_IsRoundedAgainInIt()
	{
		MokaScale scale = MokaNiceScale.Linear(0, 990_000, new MokaScaleOptions { Unit = MokaChartUnit.Bytes });

		Assert.Equal("MB", scale.Unit.Suffix);
		Assert.Equal(["0.00", "0.25 MB", "0.50 MB", "0.75 MB", "1.00 MB"], Texts(scale));
		Assert.Equal(2, scale.Decimals);
	}

	// A tick that lands on a rounder number keeps the step's decimals, so 1.080 doesn't read 1.08 between 1.076 and 1.084.
	[Fact]
	public void TicksOnOneAxis_ShareTheStepsDecimals()
	{
		MokaScale scale = MokaNiceScale.Linear(1.0761, 1.0839, new MokaScaleOptions { IncludeZero = false });

		string[] texts = Texts(scale);
		Assert.Contains("1.080", texts);
		Assert.All(texts, text => Assert.Equal(3, text.Length - text.IndexOf('.', StringComparison.Ordinal) - 1));
	}

	[Fact]
	public void NotNice_DividesTheSpanEvenly_AsTodaysChartsDo()
	{
		MokaScale scale = MokaNiceScale.Linear(0, 11_800, new MokaScaleOptions { Nice = false });

		Assert.Equal([0d, 2_950, 5_900, 8_850, 11_800], scale.Ticks);
		Assert.Null(scale.Step);
	}

	[Fact]
	public void AFixedEnd_StaysAsSet_AndStillGetsATick()
	{
		MokaScale scale = MokaNiceScale.Linear(0, 11_800, new MokaScaleOptions { Max = 11_000 });

		Assert.Equal(11_000, scale.Max);
		Assert.Equal([0d, 4_000, 8_000, 11_000], scale.Ticks);
	}

	[Fact]
	public void WithoutZero_TheAxisHugsTheData()
	{
		MokaScale scale = MokaNiceScale.Linear(52, 88, new MokaScaleOptions { IncludeZero = false });

		Assert.Equal([50d, 60, 70, 80, 90], scale.Ticks);
	}

	[Fact]
	public void AllPositive_StartsAtZero_AndAllNegative_EndsAtZero()
	{
		Assert.Equal(0, MokaNiceScale.Linear(52, 88, new MokaScaleOptions()).Min);
		Assert.Equal(0, MokaNiceScale.Linear(-88, -52, new MokaScaleOptions()).Max);
	}

	[Fact]
	public void TheAxis_ReachesThresholdsAndSuggestedEnds()
	{
		Assert.True(MokaNiceScale.Linear(0, 100, new MokaScaleOptions { Reach = [200] }).Max >= 200);
		Assert.True(MokaNiceScale.Linear(0, 1, new MokaScaleOptions { SuggestedMax = 50 }).Max >= 50);
		Assert.True(MokaNiceScale.Linear(10, 20, new MokaScaleOptions { IncludeZero = false, SuggestedMin = -5 }).Min <= -5);
	}

	[Fact]
	public void AFlatRange_IsWidenedByOneEachWay()
	{
		MokaScale scale = MokaNiceScale.Linear(5, 5, new MokaScaleOptions { IncludeZero = false, Nice = false });

		Assert.Equal(4, scale.Min);
		Assert.Equal(6, scale.Max);
	}

	[Fact]
	public void ValuesThatArentNumbers_DontCount()
	{
		MokaScale scale = MokaNiceScale.Linear([double.NaN, 1, double.PositiveInfinity, 3], new MokaScaleOptions());

		Assert.Equal(0, scale.Min);
		Assert.True(scale.Max is >= 3 and < 5);
	}

	[Fact]
	public void ExplicitTicks_Win_AndTheAxisSpansThem()
	{
		MokaScale scale = MokaNiceScale.Linear(0, 10, new MokaScaleOptions { TickValues = [0, 15, 7] });

		Assert.Equal([0d, 7, 15], scale.Ticks);
		Assert.Equal(15, scale.Max);
	}

	[Fact]
	public void Share_IsZeroAtTheBottom_AndOneAtTheTop()
	{
		MokaScale scale = MokaNiceScale.Linear(0, 11_800, new MokaScaleOptions());

		Assert.Equal(0, scale.Share(0));
		Assert.Equal(0.5, scale.Share(6_000));
		Assert.Equal(1, scale.Share(12_000));
		Assert.True(double.IsNaN(scale.Share(double.NaN)));
	}

	[Fact]
	public void Percent_KeepsItsSignOnZero() =>
		Assert.Equal("0%", MokaNiceScale.Linear(0, 80, new MokaScaleOptions { Unit = MokaChartUnit.Percent })
			.Format(0, culture: CultureInfo.InvariantCulture));

	[Fact]
	public void Log_SpansWholeDecades_WithoutMinorsPastThree()
	{
		MokaScale scale = MokaLogScale.Create(3, 4_500, new MokaScaleOptions());

		Assert.Equal(MokaScaleKind.Log, scale.Kind);
		Assert.Equal([1d, 10, 100, 1_000, 10_000], scale.Ticks);
		Assert.Equal(0.5, scale.Share(100), 9);
		Assert.True(double.IsNaN(scale.Share(0)));
	}

	[Fact]
	public void Log_AddsMinorsAtTwoAndFive_OverThreeDecadesOrFewer()
	{
		MokaScale scale = MokaLogScale.Create(2, 800, new MokaScaleOptions());

		Assert.Equal([1d, 2, 5, 10, 20, 50, 100, 200, 500, 1_000], scale.Ticks);
	}

	[Fact]
	public void Log_BelowOne_KeepsItsDecimals()
	{
		MokaScale scale = MokaLogScale.Create(0.02, 3, new MokaScaleOptions());

		Assert.Equal(0.01, scale.Min);
		Assert.Equal(["0.01", "0.02", "0.05", "0.1", "0.2", "0.5", "1", "2", "5", "10"], Texts(scale));
	}

	[Fact]
	public void Log_WritesEachTickInItsOwnStep()
	{
		MokaScale scale = MokaLogScale.Create(1, 9_000, new MokaScaleOptions { Unit = MokaChartUnit.Milliseconds });

		Assert.Equal(["1 ms", "10 ms", "100 ms", "1 s", "10 s"], Texts(scale));
	}
}
