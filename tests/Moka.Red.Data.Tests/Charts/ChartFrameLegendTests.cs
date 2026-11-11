using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     The legend inside a chart: where the look or the chart puts it, what its entries do, and hiding through the
///     chart so the parent and the drawing follow.
/// </summary>
public sealed class ChartFrameLegendTests : BunitContext
{
	private static readonly IReadOnlyList<string> Keys = ["LAN", "Wi-Fi", "Guest"];
	private static readonly IReadOnlyList<IReadOnlyList<double>> Data = [[1, 2], [3, 4], [5, 6]];

	[Theory]
	[InlineData(MokaChartLegendPosition.Bottom, "bottom")]
	[InlineData(MokaChartLegendPosition.Top, "top")]
	[InlineData(MokaChartLegendPosition.Right, "right")]
	[InlineData(MokaChartLegendPosition.TitleRow, "title-row")]
	public void TheLegend_SitsWhereTheChartSays(MokaChartLegendPosition position, string place)
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Legend, position));

		Assert.NotNull(cut.Find($".moka-chart-frame__legend--{place} .moka-chart-legend"));
		Assert.Single(cut.FindAll(".moka-chart-legend"));
	}

	[Fact]
	public void BesideThePlot_TheLegendIsAColumn_InTheTitleRowItIsDense()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> right = Chart(p => p.Add(x => x.Legend, MokaChartLegendPosition.Right));
		IRenderedComponent<FrameChart> titleRow = Chart(p => p.Add(x => x.Legend, MokaChartLegendPosition.TitleRow));

		Assert.Contains("moka-chart-legend--column", right.Find(".moka-chart-legend").ClassName, StringComparison.Ordinal);
		Assert.Contains("moka-chart-frame__body--right", right.Find(".moka-chart-frame__body").ClassName, StringComparison.Ordinal);
		Assert.Contains("moka-chart-legend--dense", titleRow.Find(".moka-chart-legend").ClassName, StringComparison.Ordinal);
		Assert.NotNull(titleRow.Find(".moka-chart-frame__header .moka-chart-legend"));
	}

	[Fact]
	public void TheLook_PlacesItAndPicksItsKeysAndActions()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.AddCascadingValue(MokaTheme.Default.WithCharts(MokaChartLook.Quiet)));

		Assert.NotNull(cut.Find(".moka-chart-frame__legend--top"));
		Assert.Equal(3, cut.FindAll("button.moka-chart-legend__item").Count);
		Assert.Equal(["line", "line", "line"], cut.FindAll(".moka-chart-legend__key")
			.Select(k => k.ClassList.First(c => c.StartsWith("moka-chart-legend__key--", StringComparison.Ordinal))["moka-chart-legend__key--".Length..]));
	}

	[Fact]
	public void ShowLegendOff_DrawsNone()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.ShowLegend, false).Add(x => x.Controls, MokaChartControls.Legend));

		Assert.Empty(cut.FindAll(".moka-chart-legend"));
	}

	[Fact]
	public void TheLegendControl_PutsItInTheToolbar_InsteadOfItsPlace()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Controls, MokaChartControls.Legend));

		Assert.NotNull(cut.Find("[data-moka-control='legend'] .moka-chart-legend"));
		Assert.Single(cut.FindAll(".moka-chart-legend"));
	}

	[Fact]
	public void HidingAnEntry_HidesTheSeries_InTheChart_AndTellsTheParent()
	{
		this.StaticFrame();
		IReadOnlyCollection<string>? told = null;
		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.LegendActions, MokaChartLegendActions.Hide)
			.Add(x => x.HiddenSeriesChanged, hidden => told = hidden));

		cut.Find("[data-moka-series='Wi-Fi']").Click(new MouseEventArgs { Detail = 1 });

		Assert.Equal(["Wi-Fi"], told);
		Assert.True(cut.Instance.Base.IsHidden("Wi-Fi"));
		Assert.Equal("false", cut.Find("[data-moka-series='Wi-Fi']").GetAttribute("aria-pressed"));
		Assert.Equal("Wi-Fi hidden. 2 of 3 series shown.", cut.Find(".moka-chart-legend > [role='status']").TextContent);
	}

	[Fact]
	public void ANameOnlyLegend_LeavesAHiddenSeriesOut_AsTheChartDoes()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.HiddenSeries, ["Guest"]));

		Assert.Equal(["LAN", "Wi-Fi"], cut.FindAll(".moka-chart-legend__item").Select(item => item.TextContent.Trim()));
	}

	[Fact]
	public void AToggleLegend_KeepsAHiddenSeries_SoItCanComeBack()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p
			.Add(x => x.HiddenSeries, ["Guest"])
			.Add(x => x.LegendActions, MokaChartLegendActions.Hide));

		Assert.Equal("false", cut.Find("[data-moka-series='Guest']").GetAttribute("aria-pressed"));
	}

	[Fact]
	public void AHighlight_MutesTheOtherEntries_LikeTheirSeries()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Highlight, "LAN"));

		IReadOnlyList<IElement> entries = cut.FindAll(".moka-chart-legend__item");
		Assert.Contains("moka-chart-c1", entries[0].ClassName, StringComparison.Ordinal);
		Assert.Equal("--moka-chart-color: var(--moka-chart-muted)", entries[1].GetAttribute("style"));
	}

	[Fact]
	public void TheChartsColors_ReachTheKeys()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Colors, ["#aa0000"]));

		Assert.All(cut.FindAll(".moka-chart-legend__item"), item => Assert.Equal("--moka-chart-color: #aa0000", item.GetAttribute("style")));
	}

	[Fact]
	public void OnTheOriginalDesign_TheLegendIsTheStandardOne()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(p => p.Add(x => x.Look, MokaChartLook.Quiet).AddCascadingValue(MokaTheme.Dark));

		Assert.NotNull(cut.Find(".moka-chart-frame__legend--bottom"));
		Assert.Empty(cut.FindAll("button.moka-chart-legend__item"));
		Assert.All(cut.FindAll(".moka-chart-legend__key"), key => Assert.Contains("moka-chart-legend__key--dot", key.ClassName, StringComparison.Ordinal));
	}

	private IRenderedComponent<FrameChart> Chart(Action<ComponentParameterCollectionBuilder<FrameChart>> more) =>
		Render<FrameChart>(p =>
		{
			p.Add(x => x.Keys, Keys).Add(x => x.Data, Data);
			more(p);
		});
}
