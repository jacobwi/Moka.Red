using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using static Moka.Red.Data.Tests.Charts.MokaLineChartTestKit;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>Comparison series and trends on the line chart: how they draw, what the tooltip, table and legend say.</summary>
public sealed class MokaLineChartCompareTests : BunitContext
{
	private static readonly string[] Days = ["Mon", "Tue", "Wed", "Thu"];

	public MokaLineChartCompareTests() => this.Static();

	private static MokaLineSeries[] WeekPair =>
	[
		Series("This week", 110, 120, 90, 100),
		Series("Last week", 100, 100, 100, 0) with { CompareTo = "This week" }
	];

	[Fact]
	public void AComparison_TakesItsSeriesColor_AndDrawsALineAlone()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.Area, MokaChartArea.Flat)
			.Add(x => x.Points, MokaChartPoints.Always)
			.Add(x => x.Series, WeekPair));

		Assert.False(cut.Instance.IsClassic);
		IElement compare = Group(cut.Find(".moka-linechart"), "Last week");
		IElement own = Group(cut.Find(".moka-linechart"), "This week");
		Assert.Contains("moka-linechart__series--compare", compare.ClassList);
		Assert.Contains("moka-chart-c1", compare.ClassList);
		Assert.Contains("moka-chart-c1", own.ClassList);
		Assert.NotNull(PathOf(compare, "moka-linechart__line"));
		Assert.Null(PathOf(compare, "moka-linechart__area"));
		Assert.Null(PathOf(compare, "moka-linechart__dot"));
		Assert.NotNull(PathOf(own, "moka-linechart__area"));
	}

	[Fact]
	public void AComparison_DrawsUnderItsSeries_AndIsNotNamedAtItsEnd()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.DirectLabels, MokaDirectLabels.Names)
			.Add(x => x.Series, WeekPair));

		Assert.Equal(["Last week", "This week"], GroupKeys(cut.Find(".moka-linechart")));
		Assert.Equal(["This week"], cut.FindAll(".moka-linechart__end-name").Select(e => e.TextContent));
	}

	[Fact]
	public void TheTooltip_SaysHowFarTheSeriesIsFromItsComparison()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Days)
			.Add(x => x.Series, WeekPair));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 1, null)!;

		Assert.Equal("+20% vs Last week", info.Rows[0].CompareText);
		Assert.True(info.Rows[1].Dashed);
		Assert.Null(info.Rows[1].CompareText);
		Assert.Equal(100, info.Context.Points[0].Comparison);
		Assert.Contains("120, +20% vs Last week", info.CellText, StringComparison.Ordinal);
		Assert.Equal("-10% vs Last week", cut.Instance.DescribeActive(0, -1, 2, null)!.Rows[0].CompareText);
	}

	[Fact]
	public void FromZero_TheTooltipSaysTheChangeItself()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Days)
			.Add(x => x.Series, WeekPair));

		Assert.Equal("+100 vs Last week", cut.Instance.DescribeActive(0, -1, 3, null)!.Rows[0].CompareText);
	}

	[Fact]
	public void TheWords_ComeFromTheChartText()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Days)
			.Add(x => x.Text, MokaChartText.Default with { Versus = (change, name) => $"{change} gegen {name}" })
			.Add(x => x.Series, WeekPair));

		Assert.Equal("+20% gegen Last week", cut.Instance.DescribeActive(0, -1, 1, null)!.Rows[0].CompareText);
	}

	[Fact]
	public void TheLegend_ListsTheComparison_WithADash()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.Series, WeekPair));

		Assert.Equal(["This week", "Last week"], cut.Instance.LegendItems.Select(item => item.Name));
		Assert.Equal(MokaChartLegendMark.Dash, cut.Instance.LegendItems[1].Mark);
		IElement[] keys = [.. cut.FindAll(".moka-chart-legend__key")];
		Assert.Contains("moka-chart-legend__key--dot", keys[0].ClassList);
		Assert.Contains("moka-chart-legend__key--dash", keys[1].ClassList);
	}

	[Fact]
	public void HidingTheSeries_HidesItsComparison()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.HiddenSeries, ["This week"])
			.Add(x => x.Series, WeekPair));

		Assert.Empty(GroupKeys(cut.Find(".moka-linechart")));
	}

	[Fact]
	public void AComparison_CountsInTheScale_ButNotInThePeak()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.ShowPeak, true)
			.Add(x => x.Series, [Series("Now", 10, 20, 30, 40), Series("Then", 50, 90, 70, 60) with { CompareTo = "Now" }]));

		Assert.Equal(90, cut.Instance.ValueRange(0).Max);
		Assert.Equal("40", cut.Find(".moka-linechart__peak-value").TextContent);
	}

	[Fact]
	public void AComparison_IsNeverStacked()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.Min, 0d)
			.Add(x => x.Max, 100d)
			.Add(x => x.Stacked, true)
			.Add(x => x.Series, [Series("A", 10, 10, 10, 10), Series("B", 20, 20, 20, 20), Series("A then", 10, 10, 10, 10) with { CompareTo = "A" }]));

		IReadOnlyList<(char Command, int X, int Y)> compare = Points(PathOf(Group(cut.Find(".moka-linechart"), "A then"), "moka-linechart__line"));
		Assert.All(compare, point => Assert.Equal(9000, point.Y));
		Assert.Equal("30", cut.Instance.DescribeActive(0, -1, 0, null)!.Rows.Single(row => row.Name == "Total").ValueText);
	}

	[Fact]
	public void AKeyThatNamesNoSeries_LeavesAnOrdinarySeries()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.Series, [Series("A", 1, 2, 3, 4), Series("B", 4, 3, 2, 1) with { CompareTo = "Nobody" }]));

		IElement b = Group(cut.Find(".moka-linechart"), "B");
		Assert.DoesNotContain("moka-linechart__series--compare", b.ClassList);
		Assert.Contains("moka-chart-c2", b.ClassList);
	}

	[Fact]
	public void TheComparison_KeepsTheColorItSetsItself()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.Series, [Series("A", 1, 2, 3, 4), Series("A then", 4, 3, 2, 1) with { CompareTo = "A", ColorValue = "var(--moka-chart-5)" }]));

		Assert.Contains("--moka-chart-color: var(--moka-chart-5)", Group(cut.Find(".moka-linechart"), "A then").GetAttribute("style"),
			StringComparison.Ordinal);
	}

	[Fact]
	public void SmallMultiples_DrawTheComparisonInItsSeriesPanel()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.Split, true)
			.Add(x => x.Series, [.. WeekPair, Series("Other", 5, 6, 7, 8)]));

		IElement[] panels = [.. cut.FindAll(".moka-linechart__panel")];
		Assert.Equal(2, panels.Length);
		Assert.Equal(["Last week", "This week"], panels[0].QuerySelectorAll("g.moka-linechart__series").Select(g => g.GetAttribute("data-moka-key")));
		Assert.Equal(["Other"], panels[1].QuerySelectorAll("g.moka-linechart__series").Select(g => g.GetAttribute("data-moka-key")));
	}

	[Theory]
	[InlineData(MokaChartTrend.MovingAverage, "LAN, 2-point average", "15")]
	[InlineData(MokaChartTrend.Linear, "LAN trend", "20")]
	public void ATrend_IsItsOwnRow_InTheTooltipAndTheTable(MokaChartTrend trend, string name, string atTue)
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Days)
			.Add(x => x.Trend, trend)
			.Add(x => x.TrendWindow, 2)
			.Add(x => x.Series, [Series("LAN", 10, 20, 30, 40)]));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 1, null)!;
		ChartActiveRow row = Assert.Single(info.Rows, r => r.Name == name);
		Assert.Equal(atTue, row.ValueText);
		Assert.True(row.Dashed);
		Assert.Equal(double.Parse(atTue, CultureInfo.InvariantCulture), info.Context.Points[0].Trend);

		ChartTableModel table = cut.Instance.TableModel!;
		int column = table.Headers.ToList().IndexOf(name);
		Assert.True(column > 0);
		Assert.Equal(atTue, table.Display(1, column));
	}

	[Fact]
	public void AMovingAverage_IsADashedPathThroughEachSeries_StartingWhereItsWindowFills()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.Min, 0d)
			.Add(x => x.Max, 40d)
			.Add(x => x.Trend, MokaChartTrend.MovingAverage)
			.Add(x => x.TrendWindow, 2)
			.Add(x => x.Series, [Series("LAN", 10, 20, 30, 40)]));

		IReadOnlyList<(char Command, int X, int Y)> trend = Points(PathOf(Group(cut.Find(".moka-linechart"), "LAN"), "moka-linechart__trend"));
		Assert.Equal([3333, 6667, 10000], trend.Select(point => point.X));
		Assert.Equal([6250, 3750, 1250], trend.Select(point => point.Y));
	}

	[Fact]
	public void ALinearTrend_IsOneStraightSegment_AndTheScaleReachesIt()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.Trend, MokaChartTrend.Linear)
			.Add(x => x.Series, [Series("LAN", 10, 50, 10, 50)]));

		string? trend = PathOf(Group(cut.Find(".moka-linechart"), "LAN"), "moka-linechart__trend");
		Assert.Equal(2, Points(trend).Count);
		Assert.Equal(1, Moves(trend));
	}

	[Fact]
	public void ATrend_LeavesStackedChartsAndComparisonsAlone()
	{
		IRenderedComponent<MokaLineChart> stacked = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.Stacked, true)
			.Add(x => x.Trend, MokaChartTrend.Linear)
			.Add(x => x.Series, [Series("A", 1, 2, 3, 4), Series("B", 1, 2, 3, 4)]));
		IRenderedComponent<MokaLineChart> compared = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.Trend, MokaChartTrend.Linear)
			.Add(x => x.Series, WeekPair));

		Assert.Empty(stacked.FindAll(".moka-linechart__trend"));
		Assert.Single(compared.FindAll(".moka-linechart__trend"));
		Assert.Null(PathOf(Group(compared.Find(".moka-linechart"), "Last week"), "moka-linechart__trend"));
	}

	[Fact]
	public void NeitherOne_LeavesTheClassicChartAsItWas()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Labels, Days)
			.Add(x => x.Series, [Series("LAN", 10, 20, 30, 40)]));

		Assert.True(cut.Instance.IsClassic);
		cut.Render(p => p.Add(x => x.Trend, MokaChartTrend.MovingAverage));
		Assert.False(cut.Instance.IsClassic);
	}
}
