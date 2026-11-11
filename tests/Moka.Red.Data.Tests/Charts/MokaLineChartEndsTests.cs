using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using static Moka.Red.Data.Tests.Charts.MokaLineChartTestKit;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     Names and values at the lines' ends (S4C): written in the plot's gutter in text colors, placed by the stylesheet
///     so ends that crowd push apart with a leader, and hidden together when the plot can't hold them.
/// </summary>
public sealed class MokaLineChartEndsTests : BunitContext
{
	private static readonly string[] Hours = ["00:00", "01:00", "02:00"];

	public MokaLineChartEndsTests() => this.Static();

	[Fact]
	public void TheQuietLook_NamesUpToFourLinesAtTheirEnds_TopFirst()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("Low", 1, 1, 1), Series("High", 9, 9, 9), Series("Mid", 5, 5, 5)]));

		Assert.Equal(["High", "Mid", "Low"], Names(cut));
		Assert.Empty(cut.FindAll(".moka-linechart__end-value"));
		Assert.NotNull(cut.Find(".moka-chart-plot__gutter .moka-linechart__ends-sizer"));
	}

	[Fact]
	public void TheQuietLook_LeavesFiveLinesToTheLegend()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [.. Enumerable.Range(1, 5).Select(i => Series($"S{i}", i, i, i))]));

		Assert.Empty(cut.FindAll(".moka-linechart__end"));
		Assert.Empty(cut.FindAll(".moka-chart-plot__gutter"));
	}

	[Fact]
	public void DirectLabels_NamesEveryLine_UpToEight()
	{
		IRenderedComponent<MokaLineChart> six = Render<MokaLineChart>(p => p
			.Add(x => x.DirectLabels, MokaDirectLabels.Both)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [.. Enumerable.Range(1, 6).Select(i => Series($"S{i}", i, i, i))]));
		IRenderedComponent<MokaLineChart> nine = Render<MokaLineChart>(p => p
			.Add(x => x.DirectLabels, MokaDirectLabels.Names)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [.. Enumerable.Range(1, 9).Select(i => Series($"S{i}", i, i, i))]));

		Assert.Equal(6, six.FindAll(".moka-linechart__end").Count);
		Assert.Equal(["6", "5", "4", "3", "2", "1"], six.FindAll(".moka-linechart__end-value").Select(v => v.TextContent));
		Assert.Empty(nine.FindAll(".moka-linechart__end"));
	}

	[Fact]
	public void TheBoldLook_WritesValues()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Bold)
			.Add(x => x.Unit, MokaChartUnit.Of("Mbps"))
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("LAN", 1, 2, 412)]));

		Assert.Equal(["412 Mbps"], cut.FindAll(".moka-linechart__end-value").Select(v => v.TextContent));
		Assert.Empty(cut.FindAll(".moka-linechart__end-name"));
	}

	[Fact]
	public void AnIndexedLine_SaysHowMuchItGrew()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.IndexTo, 100d)
			.Add(x => x.DirectLabels, MokaDirectLabels.Values)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("Sessions", 18400, 25000, 31832), Series("Users", 1240, 1100, 1116)]));

		Assert.Equal(["+73%", "-10%"], cut.FindAll(".moka-linechart__end-value").Select(v => v.TextContent));
	}

	[Fact]
	public void EachLabel_IsPlacedInThePlotsOwnHeight_PushedClearOfTheOthers()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.DirectLabels, MokaDirectLabels.Names)
			.Add(x => x.Min, 0d)
			.Add(x => x.Max, 100d)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("A", 50, 50, 50), Series("B", 51, 51, 51)]));

		string box = cut.Find(".moka-linechart__ends").GetAttribute("style")!;
		Assert.Contains("--_f0: max(0.65em, calc(49cqh + 0em))", box, StringComparison.Ordinal);
		Assert.Contains("--_f1: max(1.95em, calc(49cqh + 1.3em), calc(50cqh + 0em))", box, StringComparison.Ordinal);
		IElement first = cut.FindAll(".moka-linechart__end")[0];
		Assert.Equal("--_y: 49cqh; --_b: min(calc(100cqh - 1.95em), calc(var(--_f0) - 0em), calc(var(--_f1) - 1.3em))",
			first.GetAttribute("style"));
		Assert.Equal(3, first.QuerySelectorAll(".moka-linechart__lead").Length);
	}

	[Theory]
	[InlineData(1, "moka-linechart__ends--h3")]
	[InlineData(3, "moka-linechart__ends--h4")]
	[InlineData(6, "moka-linechart__ends--h8")]
	public void LabelsHide_InAPlotTooShortForThem(int lines, string heightClass)
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.DirectLabels, MokaDirectLabels.Names)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [.. Enumerable.Range(1, lines).Select(i => Series($"S{i}", i, i, i))]));

		Assert.Contains(heightClass, cut.Find(".moka-linechart__ends").ClassList);
	}

	[Fact]
	public void SmallMultiples_NameTheirPanelsInstead()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Split, true)
			.Add(x => x.DirectLabels, MokaDirectLabels.Names)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("A", 1, 2, 3), Series("B", 3, 2, 1)]));

		Assert.Empty(cut.FindAll(".moka-linechart__end"));
	}

	[Fact]
	public void ALabel_DimsWithItsLine()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.DirectLabels, MokaDirectLabels.Names)
			.Add(x => x.Labels, Hours)
			.Add(x => x.Series, [Series("A", 1, 2, 3) with { Key = "a" }]));

		Assert.Equal("a", cut.Find(".moka-linechart__end").GetAttribute("data-moka-key"));
	}

	private static IReadOnlyList<string> Names(IRenderedComponent<MokaLineChart> cut) =>
		[.. cut.FindAll(".moka-linechart__end-name").Select(name => name.TextContent)];
}
