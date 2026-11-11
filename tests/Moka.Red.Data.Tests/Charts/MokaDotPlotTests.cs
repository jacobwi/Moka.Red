using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Moka.Red.Core.Charts;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

public sealed class MokaDotPlotTests : BunitContext
{
	private static readonly MokaDotPlotRow[] Uptime =
	[
		new("Router", 99.99), new("DNS", 99.97), new("Mail relay", 99.93), new("VPN", 99.82), new("NAS", 99.64),
		new("Media server", 99.21) { Detail = "5 h 41 min down" }
	];

	private static readonly MokaDotPlotRow[] Cache =
	[
		new("/api/feed", 260) { Before = 520 }, new("/api/search", 180) { Before = 420 }, new("/api/login", 150) { Before = 140 }
	];

	public MokaDotPlotTests() => this.Static();

	[Fact]
	public void DrawsADotPerRow_OnTheRowPlot_WithItsValueAtTheEnd()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p.Add(x => x.Rows, Uptime));

		Assert.Equal(6, cut.FindAll(".moka-chart-plot__row").Count);
		Assert.Equal(6, cut.FindAll(".moka-dotplot__dot").Count);
		Assert.Equal(["Router", "DNS", "Mail relay", "VPN", "NAS", "Media server"],
			cut.FindAll(".moka-dotplot__name").Select(name => name.TextContent));
		Assert.Equal("99.99", cut.Find(".moka-dotplot__figure").TextContent);
		Assert.Contains("moka-chart", cut.Find(".moka-dotplot").ClassList);
		Assert.Empty(cut.Instance.SeriesInfo);
	}

	[Fact]
	public void NotZeroBased_TheAxisSpansTheData_OnRoundNumbers()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p.Add(x => x.Rows, Uptime));

		MokaScale scale = cut.Instance.RowPlotModel().Scale!;
		Assert.Equal(99.2, scale.Min, 6);
		Assert.Equal(100, scale.Max, 6);
		Assert.Equal("1.25%", RowChartKit.Var(cut.FindAll(".moka-dotplot__lane")[^1], "--_dp-v"));
		Assert.Equal("98.75%", RowChartKit.Var(cut.FindAll(".moka-dotplot__lane")[0], "--_dp-v"));
	}

	[Fact]
	public void ZeroBased_StartsTheAxisAtZero()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p.Add(x => x.Rows, Uptime).Add(x => x.ZeroBased, true));

		Assert.Equal(0, cut.Instance.RowPlotModel().Scale!.Min);
	}

	[Fact]
	public void Sort_OrdersTheRowsByValue_AndKeepsWhereEachCameFrom()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p
			.Add(x => x.Rows, [new MokaDotPlotRow("b", 2), new("a", 1), new("c", 3), new("gap", double.NaN)])
			.Add(x => x.Sort, MokaChartSort.Descending));

		Assert.Equal(["c", "b", "a", "gap"], cut.FindAll(".moka-dotplot__name").Select(name => name.TextContent));
		Assert.Equal(2, cut.Instance.DescribeActive(0, -1, 0, null)!.Context.Points[0].Index);
	}

	[Fact]
	public void Stems_RunFromTheBaseline_AndTheAxisReachesIt()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p
			.Add(x => x.Rows, [new MokaDotPlotRow("a", 40), new("b", 80)])
			.Add(x => x.Stems, true));

		Assert.Equal(0, cut.Instance.RowPlotModel().Scale!.Min);
		IElement lane = cut.FindAll(".moka-dotplot__lane")[1];
		Assert.NotNull(lane.QuerySelector(".moka-dotplot__link"));
		Assert.Equal("0%", RowChartKit.Var(lane, "--_dp-from"));
		Assert.Equal(RowChartKit.Var(lane, "--_dp-v"), RowChartKit.Var(lane, "--_dp-len"));
	}

	[Fact]
	public void ADumbbell_DrawsBothDots_TheLink_AndNamesThemInTheLegend()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p.Add(x => x.Rows, Cache).Add(x => x.ZeroBased, true));

		Assert.Equal(3, cut.FindAll(".moka-dotplot__dot--before").Count);
		Assert.Equal(6, cut.FindAll(".moka-dotplot__dot").Count);
		Assert.Equal(3, cut.FindAll(".moka-dotplot__link").Count);
		Assert.Equal(["Before", "After"], cut.Instance.SeriesInfo.Select(s => s.Name));
		Assert.Contains("color-mix", cut.Instance.SeriesInfo[0].ColorStyle, StringComparison.Ordinal);
		Assert.Equal("520 → 260", cut.Find(".moka-dotplot__figure").TextContent);
		Assert.Equal(["before", "value"], cut.FindAll(".moka-dotplot__lane")[0].QuerySelectorAll(".moka-dotplot__dot")
			.Select(dot => dot.GetAttribute("data-moka-key")!));
	}

	[Fact]
	public void MarkWorse_DrawsTheRowsThatGotWorseInTheAccent_AndTheRestMuted()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p
			.Add(x => x.Rows, Cache)
			.Add(x => x.UpIsGood, false)
			.Add(x => x.MarkWorse, true));

		IReadOnlyList<IElement> lanes = cut.FindAll(".moka-dotplot__lane");
		Assert.Contains("moka-chart-muted", lanes[0].ClassList);
		Assert.Contains("moka-chart-muted", lanes[1].ClassList);
		Assert.Contains("moka-chart-tone-primary", lanes[2].ClassList);
		Assert.Contains("moka-dotplot__lane--worse", lanes[2].ClassList);
	}

	[Fact]
	public void TheTooltip_SaysBeforeAfter_TheChange_AndWhetherItGotWorse()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p
			.Add(x => x.Rows, Cache)
			.Add(x => x.Unit, MokaChartUnit.Of("ms"))
			.Add(x => x.UpIsGood, false));

		ChartActiveInfo feed = cut.Instance.DescribeActive(0, -1, 0, null)!;
		Assert.Equal("/api/feed", feed.Heading);
		Assert.Equal(["Before", "After"], feed.Rows.Select(row => row.Name));
		Assert.Equal(["520 ms", "260 ms"], feed.Rows.Select(row => row.ValueText));
		Assert.Equal("-260 ms (-50%), better", feed.Notes[0]);
		Assert.Equal("/api/feed, Before 520 ms, After 260 ms, -260 ms (-50%), better", feed.CellText);
		Assert.EndsWith("+10 ms (+7%), worse", cut.Instance.DescribeActive(0, -1, 2, null)!.CellText, StringComparison.Ordinal);
		Assert.Null(cut.Instance.DescribeActive(0, -1, 3, null));
		Assert.Null(cut.Instance.DescribeActive(0, -1, -1, null));
	}

	[Fact]
	public void HidingBefore_LeavesTheValueDots_AndTheAxisRefits()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p
			.Add(x => x.Rows, Cache)
			.Add(x => x.HiddenSeries, [MokaDotPlot.BeforeKey]));

		Assert.Empty(cut.FindAll(".moka-dotplot__dot--before"));
		Assert.Equal(3, cut.FindAll(".moka-dotplot__dot").Count);
		Assert.Equal("260", cut.Find(".moka-dotplot__figure").TextContent);
		Assert.True(cut.Instance.RowPlotModel().Scale!.Max < 520);
	}

	[Fact]
	public void Highlight_MutesTheOtherRows()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p.Add(x => x.Rows, Uptime).Add(x => x.Highlight, "VPN"));

		IReadOnlyList<IElement> lanes = cut.FindAll(".moka-dotplot__lane");
		Assert.Contains("moka-chart-c1", lanes[3].ClassList);
		Assert.All(lanes.Where((_, i) => i != 3), lane => Assert.Contains("moka-chart-muted", lane.ClassList));
	}

	[Fact]
	public void OnTheStandardLook_EachRowCarriesANativeTooltip()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p.Add(x => x.Rows, Cache));

		Assert.Equal("/api/feed: Before 520, After 260", cut.Find(".moka-dotplot__lane").GetAttribute("title"));
		Assert.Equal("img", cut.Find(".moka-dotplot").GetAttribute("role"));
	}

	[Fact]
	public void TheTable_HasBeforeAfterAndTheChange()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p.Add(x => x.Rows, Cache));

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(["Name", "Before", "After", "Change"], table.Headers);
		Assert.Equal("-260 (-50%)", table.Display(0, 3));
		Assert.Equal("-260", table.Raw(0, 3));
		Assert.Equal("520", table.Raw(0, 1));
	}

	[Fact]
	public void NameTemplate_DrawsTheName_AndTheRowStillNamesItself()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p
			.Add(x => x.Rows, Uptime)
			.Add(x => x.NameTemplate, row => builder => builder.AddMarkupContent(0, $"<b>{row.Name}</b>")));

		Assert.Equal("Router", cut.Find(".moka-dotplot__name b").TextContent);
		Assert.Equal("Router", cut.Find(".moka-chart-plot__rname").GetAttribute("title"));
	}

	[Fact]
	public void AListChangedInPlace_DrawsAgain()
	{
		List<MokaDotPlotRow> rows = [new("a", 1), new("b", 2)];
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p.Add(x => x.Rows, rows));
		long before = cut.Instance.GeometryVersion;

		cut.Render(p => p.Add(x => x.Rows, rows));
		Assert.Equal(before, cut.Instance.GeometryVersion);

		rows[1] = new MokaDotPlotRow("b", 5);
		cut.Render(p => p.Add(x => x.Rows, rows));
		Assert.True(cut.Instance.GeometryVersion > before);
		Assert.Equal("5", cut.FindAll(".moka-dotplot__figure")[1].TextContent);
	}

	[Fact]
	public void Interactive_TheSurfaceCoversTheRows_AndEveryPartOfARowCarriesItsIndex()
	{
		this.Interactive();
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p
			.Add(x => x.Rows, Uptime)
			.Add(x => x.Tooltip, MokaChartTooltip.Auto)
			.Add(x => x.Keyboard, true));

		IElement surface = cut.Find(".moka-chart-plot__rbody > .moka-chart-surface");
		Assert.Equal("6", surface.GetAttribute("data-moka-count"));
		Assert.Contains("moka-chart-surface--column", surface.ClassList);
		Assert.Contains("moka-chart-surface--rows", surface.ClassList);
		Assert.Equal(3, cut.FindAll("[data-i=\"2\"]").Count);
		Assert.Equal("figure", cut.Find(".moka-dotplot").GetAttribute("role"));
		Assert.Contains("Up and Down move between rows.", cut.Find(".moka-chart-surface__grid").GetAttribute("aria-label"), StringComparison.Ordinal);
		Assert.Equal("rows", cut.Instance.SurfaceOptions()["orientation"]);
		Assert.Equal("column", cut.Instance.SurfaceOptions()["mode"]);
		Assert.Null(cut.Find(".moka-dotplot__lane").GetAttribute("title"));
	}

	[Fact]
	public void TenThousandRows_GoThroughVirtualize()
	{
		this.Interactive();
		MokaDotPlotRow[] rows = [.. Enumerable.Range(0, 10_000).Select(i => new MokaDotPlotRow($"device-{i.ToString(CultureInfo.InvariantCulture)}", i % 97))];
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p
			.Add(x => x.Rows, rows)
			.Add(x => x.Tooltip, MokaChartTooltip.Auto)
			.Add(x => x.MaxHeight, "400px"));

		Assert.NotNull(cut.FindComponent<Virtualize<int>>());
		Assert.Equal(28f, cut.FindComponent<Virtualize<int>>().Instance.ItemSize);
		Assert.True(cut.FindAll(".moka-chart-plot__row").Count < 10_000);
		Assert.Equal("10000", cut.Find(".moka-chart-surface").GetAttribute("data-moka-count"));
	}

	[Fact]
	public void Thresholds_RunDownTheRows_AndNameThemselvesInTheTooltip()
	{
		IRenderedComponent<MokaDotPlot> cut = Render<MokaDotPlot>(p => p
			.Add(x => x.Rows, [new MokaDotPlotRow("a", 40), new("b", 80)])
			.Add(x => x.ZeroBased, true)
			.Add(x => x.Annotations, [new MokaChartThreshold(50) { Label = "Low space" }]));

		Assert.Equal("Low space", cut.Find(".moka-chart-plot__vthreshold-label").TextContent);
		Assert.Contains(cut.Instance.DescribeActive(0, -1, 1, null)!.Notes, note => note.Contains("over", StringComparison.Ordinal));
	}
}
