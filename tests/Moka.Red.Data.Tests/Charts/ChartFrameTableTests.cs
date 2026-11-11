using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Data.Chart;
using Moka.Red.Data.Table;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     The table view: the chart's values through <c>MokaTable</c>, one row per point and a page at a time, so a long
///     series costs one page of DOM. The chart keeps its place underneath, so switching never moves the page.
/// </summary>
public sealed class ChartFrameTableTests : BunitContext
{
	private static readonly IReadOnlyList<string> Keys = ["LAN", "Wi-Fi"];

	[Fact]
	public void TheTableView_ShowsTheValues_OverTheChartsPlace()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(Rows(3), p => p.Add(x => x.Label, "Traffic"));

		IElement region = cut.Find(".moka-chart-frame__table");
		Assert.Equal("region", region.GetAttribute("role"));
		Assert.Equal("Traffic, Table", region.GetAttribute("aria-label"));
		Assert.Contains("moka-chart-frame__chart--hidden", cut.Find(".moka-chart-frame__chart").ClassName, StringComparison.Ordinal);
		Assert.Equal(["Point", "LAN", "Wi-Fi"], cut.FindAll("thead th").Select(th => th.TextContent.Trim()).Where(text => text.Length > 0));
		Assert.Equal(3, cut.FindAll("tbody tr").Count);
		Assert.Equal("Traffic", cut.Find("table").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheTable_PagesFiftyRowsAtATime()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(Rows(120));

		Assert.Equal(50, cut.FindAll("tbody tr").Count);
		Assert.Equal(50, cut.FindComponent<MokaTable<int>>().Instance.PageSize);
	}

	[Fact]
	public void NumbersRead_RightAligned_InMono()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(Rows(2));

		IReadOnlyList<MokaColumn<int>> columns = [.. cut.FindComponents<MokaColumn<int>>().Select(column => column.Instance)];
		Assert.True(columns[1].Mono);
		Assert.Equal(Moka.Red.Core.Enums.MokaTextAlign.Right, columns[1].Align);
	}

	[Fact]
	public async Task Sorting_OrdersEveryRow_NotJustThePage()
	{
		this.StaticFrame();
		double[] lan = [.. Enumerable.Range(0, 120).Select(i => (double)((i * 37) % 120))];
		IRenderedComponent<FrameChart> cut = Render<FrameChart>(p => p
			.Add(x => x.Keys, ["LAN"])
			.Add(x => x.Data, [lan])
			.Add(x => x.View, MokaChartView.Table));

		MokaTable<int> table = cut.FindComponent<MokaTable<int>>().Instance;
		Func<MokaTableState, Task<MokaTableResult<int>>> load = table.ServerData!;
		MokaTableResult<int> descending = await cut.InvokeAsync(() => load(new MokaTableState
		{
			Page = 1, PageSize = 50, SortColumn = "LAN", SortDirection = MokaSortDirection.Descending
		}));

		Assert.Equal(120, descending.TotalItems);
		Assert.Equal(119, lan[descending.Items[0]]);
		Assert.Equal(118, lan[descending.Items[1]]);
	}

	[Fact]
	public void TheViewParameter_ShowsTheTable_AndEscapeComesBack()
	{
		this.StaticFrame();
		MokaChartView? told = null;
		IRenderedComponent<FrameChart> cut = Chart(Rows(2), p => p.Add(x => x.ViewChanged, view => told = view));

		cut.Find(".moka-chart-frame__main").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

		Assert.Equal(MokaChartView.Chart, told);
		Assert.Empty(cut.FindAll(".moka-chart-frame__table"));
	}

	[Fact]
	public void TheChartAlone_TakesNoKeys()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Render<FrameChart>(p => p.Add(x => x.Keys, Keys).Add(x => x.Data, Rows(2)));

		Assert.False(cut.Find(".moka-chart-frame__main").HasAttribute("blazor:onkeydown"));
	}

	[Fact]
	public void NewData_ReachesAnOpenTable()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Chart(Rows(2));

		cut.Render(p => p.Add(x => x.Keys, Keys).Add(x => x.Data, Rows(4)).Add(x => x.View, MokaChartView.Table));

		cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll("tbody tr").Count));
	}

	[Fact]
	public void AChartWithoutATable_StaysAChart_WhateverTheView()
	{
		this.StaticFrame();
		IRenderedComponent<FrameChart> cut = Render<FrameChart>(p => p
			.Add(x => x.Keys, Keys)
			.Add(x => x.Data, Rows(2))
			.Add(x => x.NoTable, true)
			.Add(x => x.View, MokaChartView.Table));

		Assert.Empty(cut.FindAll(".moka-chart-frame__table"));
		Assert.DoesNotContain("--hidden", cut.Find(".moka-chart-frame__chart").ClassName, StringComparison.Ordinal);
	}

	private static IReadOnlyList<IReadOnlyList<double>> Rows(int count) =>
	[
		[.. Enumerable.Range(0, count).Select(i => (double)i)],
		[.. Enumerable.Range(0, count).Select(i => i * 2d)]
	];

	private IRenderedComponent<FrameChart> Chart(IReadOnlyList<IReadOnlyList<double>> data,
		Action<ComponentParameterCollectionBuilder<FrameChart>>? more = null) =>
		Render<FrameChart>(p =>
		{
			p.Add(x => x.Keys, Keys).Add(x => x.Data, data).Add(x => x.View, MokaChartView.Table);
			more?.Invoke(p);
		});
}
