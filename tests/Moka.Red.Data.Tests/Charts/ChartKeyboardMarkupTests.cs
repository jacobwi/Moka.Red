using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     The keyboard grid (I4): one tab stop over the plot, a grid named by the chart and how its keys work, one row per
///     series, and only the active cell, whose id changes with the point so a screen reader reads each move.
/// </summary>
public sealed class ChartKeyboardMarkupTests : BunitContext
{
	private static readonly SurfaceSeries[] Two = [new("LAN", [10, 20, 30, 40]), new("WAN", [5, 6, 7, 8])];
	private static readonly string[] Times = ["00:00", "06:00", "12:00", "18:00"];

	[Fact]
	public void TheGrid_IsOneTabStop_NamedByTheChartAndItsKeys()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart();

		IElement grid = Assert.Single(cut.FindAll("[role=grid]"));
		Assert.Equal("0", grid.GetAttribute("tabindex"));
		Assert.Equal("Traffic today. Left and Right move along a line, Up and Down change line.", grid.GetAttribute("aria-label"));
		Assert.Equal("2", grid.GetAttribute("aria-rowcount"));
		Assert.Equal("4", grid.GetAttribute("aria-colcount"));
		Assert.False(grid.HasAttribute("aria-activedescendant"));
		Assert.Equal(["1", "2"], grid.QuerySelectorAll("[role=row]").Select(r => r.GetAttribute("aria-rowindex")));
		Assert.Empty(grid.QuerySelectorAll("[role=gridcell]"));
		Assert.Single(cut.FindAll("[tabindex='0']"));
	}

	[Fact]
	public async Task TheActiveDescendant_IsTheOneCell_InItsSeriesRow()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart();

		await cut.InvokeAsync(() => cut.Instance.OnActive(0, 1, 2, true));

		IElement grid = cut.Find("[role=grid]");
		IElement cell = Assert.Single(grid.QuerySelectorAll("[role=gridcell]"));
		Assert.Equal(cell.Id, grid.GetAttribute("aria-activedescendant"));
		Assert.Equal("2", cell.ParentElement!.GetAttribute("aria-rowindex"));
		Assert.Equal("3", cell.GetAttribute("aria-colindex"));
		Assert.Equal("WAN, 12:00, 7", cell.TextContent);
		Assert.Equal(cut.Instance.Base.GridId(0), grid.Id);
	}

	[Fact]
	public async Task TheCellId_ChangesWithThePoint()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart();

		await cut.InvokeAsync(() => cut.Instance.OnActive(0, 0, 2, true));
		string first = cut.Find("[role=gridcell]").Id!;
		await cut.InvokeAsync(() => cut.Instance.OnActive(0, 0, 3, true));
		string next = cut.Find("[role=gridcell]").Id!;
		await cut.InvokeAsync(() => cut.Instance.OnActive(0, 1, 3, true));
		string otherLine = cut.Find("[role=gridcell]").Id!;

		Assert.Equal(3, new[] { first, next, otherLine }.Distinct().Count());
	}

	[Fact]
	public void TwoCharts_NeverShareAnId()
	{
		this.Interactive();
		string a = Chart().Find("[role=grid]").Id!;
		string b = Chart().Find("[role=grid]").Id!;

		Assert.NotEqual(a, b);
	}

	[Fact]
	public async Task Rows_HaveOneRowAtTheActiveIndex_AndSayTheRowKeys()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart(p => p
			.Add(x => x.Rows, true)
			.Add(x => x.Tooltip, MokaChartTooltip.Column));

		await cut.InvokeAsync(() => cut.Instance.OnActive(0, -1, 3, true));

		IElement grid = cut.Find("[role=grid]");
		Assert.EndsWith("Up and Down move between rows.", grid.GetAttribute("aria-label"), StringComparison.Ordinal);
		Assert.Equal("1", grid.GetAttribute("aria-colcount"));
		IElement row = Assert.Single(grid.QuerySelectorAll("[role=row]"));
		Assert.Equal("4", row.GetAttribute("aria-rowindex"));
		Assert.Equal("18:00, LAN 40, WAN 8", row.QuerySelector("[role=gridcell]")!.TextContent);
		Assert.Equal("rows", Assert.IsType<Dictionary<string, object?>>(
			Assert.Single(module.Invocations, i => i.Identifier == "bind").Arguments[2])["orientation"]);
	}

	[Fact]
	public async Task Columns_ReadTheWholeColumn_FromOneRow()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart(p => p.Add(x => x.Tooltip, MokaChartTooltip.Column));

		await cut.InvokeAsync(() => cut.Instance.OnActive(0, -1, 0, true));

		IElement grid = cut.Find("[role=grid]");
		Assert.Equal("1", grid.GetAttribute("aria-rowcount"));
		Assert.Equal("00:00, LAN 10, WAN 5", grid.QuerySelector("[role=gridcell]")!.TextContent);
	}

	[Fact]
	public async Task WithoutKeys_ThereIsNoGrid_ButTheTooltipStays()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart(p => p.Add(x => x.Keyboard, false));

		await cut.InvokeAsync(() => cut.Instance.OnActive(0, 0, 1, false));

		Assert.Empty(cut.FindAll("[role=grid]"));
		Assert.Empty(cut.FindAll("[tabindex]"));
		Assert.Equal("06:00", cut.Find(".moka-chart-tip__head").TextContent);
	}

	[Fact]
	public void KeysWithoutATooltip_StillGiveTheGrid_AndDrawNoTip()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Chart(p => p
			.Add(x => x.Tooltip, MokaChartTooltip.Native)
			.Add(x => x.Keyboard, true));

		Assert.Single(cut.FindAll("[role=grid]"));
		Assert.Empty(cut.FindAll(".moka-chart-tip"));
		Assert.Contains("moka-chart-surface--none", cut.Find(".moka-chart-surface").ClassList);
	}

	private IRenderedComponent<ChartSurfaceTestChart> Chart(
		Action<ComponentParameterCollectionBuilder<ChartSurfaceTestChart>>? more = null) =>
		Render<ChartSurfaceTestChart>(p =>
		{
			p.Add(x => x.Series, Two)
				.Add(x => x.Labels, Times)
				.Add(x => x.Look, MokaChartLook.Quiet)
				.Add(x => x.Label, "Traffic today");
			more?.Invoke(p);
		});
}
