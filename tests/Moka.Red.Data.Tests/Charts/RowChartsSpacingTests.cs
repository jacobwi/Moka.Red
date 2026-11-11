using Bunit;
using Bunit.Rendering;
using Moka.Red.Data.Chart;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Charts;

// Margin, Padding and Rounded on the charts drawn in rows and points: they land on the chart's root, once.
public sealed class RowChartsSpacingTests : BunitContext
{
	private static readonly DateTimeOffset Start = new(2026, 11, 11, 1, 0, 0, TimeSpan.Zero);

	private static readonly Dictionary<string, SpacingCase> Cases = new()
	{
		["DotPlot"] = new(typeof(MokaDotPlot),
			"--_row-h: 28px | --_s: 0%; --_hw: 0.33em | --_s: 25%; --_hw: 1.32em | --_s: 50%; --_hw: 0.99em | --_s: 75%; --_hw: 1.32em | --_s: 100%; --_hw: 0.33em | --_s: 25% | --_s: 50% | --_s: 75% | --_s: 100% | --_s: 0% | --_dp-v: 0% | --_dp-v: 100%",
			("Rows", new[] { new MokaDotPlotRow("a", 1), new MokaDotPlotRow("b", 2) })),
		["BoxPlot"] = new(typeof(MokaBoxPlot),
			"--_row-h: 32px | --_s: 0%; --_hw: 0.33em | --_s: 25%; --_hw: 1.32em | --_s: 50%; --_hw: 1.32em | --_s: 75%; --_hw: 1.32em | --_s: 100%; --_hw: 1.32em | --_s: 25% | --_s: 50% | --_s: 75% | --_s: 100% | --_s: 0% | --_bx-lo: 29.87%; --_bx-q1: 45.455%; --_bx-md: 64.935%; --_bx-q3: 84.416%; --_bx-hi: 100%",
			("Groups", new[] { new MokaBoxPlotGroup("a", [1, 2, 3, 4]) })),
		["Scatter"] = new(typeof(MokaScatterChart),
			"--_plot-max: 14rem | --_s: 0% | --_s: 25% | --_s: 50% | --_s: 75% | --_s: 100% | --_s: 25% | --_s: 50% | --_s: 75% | --_s: 100% | --_s: 0% | --_s: 0%; --_hw: 0.99em | --_s: 20%; --_hw: 0.99em | --_s: 40%; --_hw: 0.99em | --_s: 60%; --_hw: 0.99em | --_s: 80%; --_hw: 0.99em | --_s: 100%; --_hw: 0.99em",
			("Series", new[] { new MokaScatterSeries("a", [new MokaScatterPoint(1, 2)]) })),
		["Gantt"] = new(typeof(MokaGanttChart),
			"--_row-h: 28px | --_s: 0%; --_hw: 1.32em | --_s: 16.667%; --_hw: 2.31em | --_s: 33.333%; --_hw: 2.31em | --_s: 50%; --_hw: 2.31em | --_s: 66.667%; --_hw: 2.31em | --_s: 83.333%; --_hw: 2.31em | --_s: 100%; --_hw: 1.32em | --_s: 0% | --_s: 3.333% | --_s: 6.667% | --_s: 10% | --_s: 13.333% | --_s: 16.667% | --_s: 20% | --_s: 23.333% | --_s: 26.667% | --_s: 30% | --_s: 33.333% | --_s: 36.667% | --_s: 40% | --_s: 43.333% | --_s: 46.667% | --_s: 50% | --_s: 53.333% | --_s: 56.667% | --_s: 60% | --_s: 63.333% | --_s: 66.667% | --_s: 70% | --_s: 73.333% | --_s: 76.667% | --_s: 80% | --_s: 83.333% | --_s: 86.667% | --_s: 90% | --_s: 93.333% | --_s: 96.667% | --_s: 100% | --_ga-from: 0%; --_ga-to: 100%; --_ga-len: 100% | display:inline-block;vertical-align:middle;flex-shrink:0;width: 1.1em; height: 1.1em",
			("Rows", new[] { new MokaGanttRow("a", Start) { End = Start.AddHours(1) } }), ("Now", Start.AddHours(2)),
			("TimeZone", TimeZoneInfo.Utc)),
		["Funnel"] = new(typeof(MokaFunnel),
			"--_row-h: 39px | --_s: 0%; --_hw: 0.33em | --_s: 25%; --_hw: 0.99em | --_s: 50%; --_hw: 0.33em | --_s: 75%; --_hw: 0.99em | --_s: 100%; --_hw: 0.66em | --_s: 25% | --_s: 50% | --_s: 75% | --_s: 100% | --_s: 0% | --moka-chart-color: var(--moka-chart-ord-1); --_fn-from: 0%; --_fn-len: 100% | --moka-chart-color: var(--moka-chart-ord-2); --_fn-from: 0%; --_fn-len: 50%",
			("Steps", new[] { new MokaFunnelStep("a", 10), new MokaFunnelStep("b", 5) }))
	};

	public RowChartsSpacingTests() => this.Static();

	public static TheoryData<string> Names => [.. Cases.Keys];

	[Theory]
	[MemberData(nameof(Names))]
	public void SpacingParameters_LandOnTheComponent(string name)
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

public sealed class ChartRowsTextTests
{
	[Theory]
	[InlineData(45, "45 s")]
	[InlineData(41 * 60, "41 min")]
	[InlineData(100 * 60, "1 h 40 min")]
	[InlineData(3 * 3600, "3 h")]
	[InlineData(76 * 3600, "3 d 4 h")]
	[InlineData(48 * 3600, "2 d")]
	public void Durations_ReadAsPeopleWriteThem(int seconds, string expected) =>
		Assert.Equal(expected, ChartRows.Duration(TimeSpan.FromSeconds(seconds)));

	[Fact]
	public void Order_SortsByValue_WithGapsLast()
	{
		double[] values = [2, double.NaN, 3, 1];
		Assert.Equal([2, 0, 3, 1], ChartRows.Order(4, i => values[i], MokaChartSort.Descending));
		Assert.Equal([3, 0, 2, 1], ChartRows.Order(4, i => values[i], MokaChartSort.Ascending));
		Assert.Equal([0, 1, 2, 3], ChartRows.Order(4, i => values[i], MokaChartSort.None));
	}

	[Fact]
	public void BaseColor_FollowsTheClassThatColorsARow()
	{
		Assert.Equal("var(--moka-chart-3)", ChartRows.BaseColor("moka-chart-c3", null));
		Assert.Equal("var(--moka-color-success)", ChartRows.BaseColor("moka-chart-tone-success", null));
		Assert.Equal("var(--moka-chart-muted)", ChartRows.BaseColor("moka-chart-muted", null));
		Assert.Equal("#123456", ChartRows.BaseColor("moka-chart-c1", "--moka-chart-color: #123456"));
	}
}
