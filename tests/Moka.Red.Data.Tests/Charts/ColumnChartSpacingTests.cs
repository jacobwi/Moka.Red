using Bunit;
using Bunit.Rendering;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Charts;

// Margin, Padding and Rounded on the column charts: the root takes all three, the marks none.
public sealed class ColumnChartSpacingTests : BunitContext
{
	private static readonly DateTimeOffset Day = new(2026, 3, 9, 12, 0, 0, TimeSpan.Zero);

	private static readonly Dictionary<string, SpacingCase> Cases = new()
	{
		["BarChart"] = new(typeof(MokaBarChart),
			"--_plot-max: 14rem | --_s: 0% | --_s: 25% | --_s: 50% | --_s: 75% | --_s: 100% | --_s: 25% | --_s: 50% | --_s: 75% | --_s: 100% | --_s: 0% | --_cs: 0%; --_cw: 50%; --_gw: 60% | --_lo: 0%; --_hi: 50% | --_cs: 50%; --_cw: 50%; --_gw: 60% | --_lo: 0%; --_hi: 100% | --_s: 25%; --_hw: 0.33em | --_s: 75%; --_hw: 0.33em",
			("Look", (MokaChartLook?)MokaChartLook.Quiet),
			("Legend", (MokaChartLegendPosition?)MokaChartLegendPosition.None),
			("ValueLabels", (MokaValueLabels?)MokaValueLabels.None),
			("Series", new[] { new MokaBarSeries { Name = "A", Values = [1, 2] } })),
		["Histogram"] = new(typeof(MokaHistogram),
			"--_plot-max: 14rem | --_s: 0% | --_s: 25% | --_s: 50% | --_s: 75% | --_s: 100% | --_s: 25% | --_s: 50% | --_s: 75% | --_s: 100% | --_s: 0% | --_cs: 0%; --_cw: 33.333% | --_lo: 0%; --_hi: 50% | --_cs: 33.333%; --_cw: 33.333% | --_lo: 0%; --_hi: 100% | --_cs: 66.667%; --_cw: 33.333% | --_lo: 0%; --_hi: 50% | --_s: 0%; --_hw: 0.33em | --_s: 33.333%; --_hw: 0.33em | --_s: 66.667%; --_hw: 0.33em | --_s: 100%; --_hw: 0.33em",
			("Values", new[] { 1d, 2, 2, 3 }),
			("BucketSize", (double?)1)),
		["CandlestickChart"] = new(typeof(MokaCandlestickChart),
			"--_plot-max: 14rem | --_s: 0% | --_s: 20% | --_s: 40% | --_s: 60% | --_s: 80% | --_s: 100% | --_s: 20% | --_s: 40% | --_s: 60% | --_s: 80% | --_s: 100% | --_s: 0% | --_cs: 0%; --_cw: 100% | --_lo: 20%; --_hi: 80% | --_lo: 40%; --_hi: 60%",
			("Legend", (MokaChartLegendPosition?)MokaChartLegendPosition.None),
			("ShowXLabels", false),
			("Candles", new[] { new MokaCandle(Day, 1, 2, 0.5, 1.5) }))
	};

	public ColumnChartSpacingTests() => this.Static();

	public static TheoryData<string> Names => [.. Cases.Keys];

	[Theory]
	[MemberData(nameof(Names))]
	public void SpacingParameters_LandOnTheChartsRoot(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: true));

		spacing.AssertSpacing(cut);
	}

	[Theory]
	[MemberData(nameof(Names))]
	public void Styles_AreUnchanged_WithoutSpacingParameters(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: false));

		Assert.Equal(spacing.DefaultStyles, SpacingCase.Styles(cut));
	}
}
