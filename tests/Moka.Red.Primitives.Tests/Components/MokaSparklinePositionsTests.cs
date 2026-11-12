using Bunit;
using Moka.Red.Core.Charts;
using Moka.Red.Data.Chart;

namespace Moka.Red.Primitives.Tests.Components;

// Positions place each value across the box, for values that came at uneven times, such as a chart's overview of a
// time axis. Without them every value keeps its even place, and the goldens hold that.
public class MokaSparklinePositionsTests : BunitContext
{
	private static string Line(IRenderedComponent<MokaSparkline> cut) => cut.Find(".moka-sparkline-line").GetAttribute("d")!;

	[Fact]
	public void Positions_PlaceEachValue()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [1d, 2, 3, 4])
			.Add(x => x.Positions, [0d, 10, 50, 60]));

		Assert.Equal("M 0.00 35.00 L 20.00 23.67 L 100.00 12.33 L 120.00 1.00", Line(cut));
	}

	[Fact]
	public void TheEnds_CanReachPastThePositions()
	{
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [1d, 2])
			.Add(x => x.Positions, [10d, 30])
			.Add(x => x.PositionMin, 0d)
			.Add(x => x.PositionMax, 40d));

		Assert.StartsWith("M 30.00 ", Line(cut), StringComparison.Ordinal);
		Assert.Contains(" L 90.00 ", Line(cut), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(new[] { 0d, 10, 50 })]
	[InlineData(new[] { 5d, 5, 5, 5 })]
	public void PositionsThatDontFit_LeaveTheValuesEvenlySpaced(double[] positions)
	{
		IRenderedComponent<MokaSparkline> plain = Render<MokaSparkline>(p => p.Add(x => x.Values, [1d, 2, 3, 4]));
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, [1d, 2, 3, 4])
			.Add(x => x.Positions, positions));

		Assert.Equal(Line(plain), Line(cut));
	}

	[Fact]
	public void ALongSeries_IsCutDownByPosition()
	{
		double[] values = [.. Enumerable.Range(0, 2_000).Select(i => Math.Sin(i / 40d))];
		double[] positions = [.. Enumerable.Range(0, 2_000).Select(i => i < 1_000 ? (double)i : 1_000 + (i - 1_000) * 9d)];
		IRenderedComponent<MokaSparkline> cut = Render<MokaSparkline>(p => p
			.Add(x => x.Values, values)
			.Add(x => x.Positions, positions)
			.Add(x => x.ShowArea, false));

		int expected = MokaDownsampler.MinMax(positions, values, 240).Length;
		Assert.Equal(expected, Line(cut).Count(c => c is 'M' or 'L'));
	}
}
