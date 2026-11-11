using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

public sealed class MokaSankeyTests : BunitContext
{
	private static readonly MokaSankeyLink[] Requests =
	[
		new("web", "cache", 3.4), new("web", "db", 1.9), new("web", "storage", 0.5),
		new("api", "cache", 1.1), new("api", "db", 1.5), new("api", "storage", 0.5),
		new("jobs", "cache", 0.1), new("jobs", "db", 0.4), new("jobs", "storage", 0.6)
	];

	private static readonly MokaSankeyNode[] Names =
	[
		new("web", "Web"), new("api", "API"), new("jobs", "Jobs"),
		new("cache", "Cache"), new("db", "Database"), new("storage", "Storage")
	];

	public MokaSankeyTests() => this.Static();

	[Fact]
	public void Acyclic_DropsTheFlowThatClosesACycle()
	{
		(List<SankeyFlow> flows, List<string> keys, int dropped) = SankeyLayout.Acyclic(
			[new SankeyFlow("a", "b", 1), new SankeyFlow("b", "c", 1), new SankeyFlow("c", "a", 1), new SankeyFlow("a", "a", 1)]);

		Assert.Equal(1, dropped);
		Assert.Equal(["a", "b", "c"], keys);
		Assert.DoesNotContain(flows, f => f.From == "c" && f.To == "a");
	}

	[Fact]
	public void Acyclic_MergesRepeatedFlows()
	{
		(List<SankeyFlow> flows, _, _) = SankeyLayout.Acyclic([new SankeyFlow("a", "b", 1), new SankeyFlow("a", "b", 2.5)]);

		Assert.Equal(3.5, Assert.Single(flows).Value);
	}

	[Fact]
	public void Columns_FollowTheLongestPath_WithSinksAtTheEnd()
	{
		List<SankeyFlow> flows = [new("wan", "router", 5), new("router", "lan", 3), new("router", "wifi", 2), new("wan", "vpn", 1)];
		Dictionary<string, int> columns = SankeyLayout.Columns(["wan", "router", "lan", "wifi", "vpn"], flows, _ => null);

		Assert.Equal(0, columns["wan"]);
		Assert.Equal(1, columns["router"]);
		Assert.Equal(2, columns["lan"]);
		Assert.Equal(2, columns["vpn"]);
	}

	[Fact]
	public void Columns_AFixedColumnWins()
	{
		Dictionary<string, int> columns = SankeyLayout.Columns(["a", "b"], [new SankeyFlow("a", "b", 1)], key => key == "b" ? 3 : null);

		Assert.Equal(3, columns["b"]);
	}

	[Fact]
	public void DrawsARibbonPerFlow_AndANodePerEnd()
	{
		IRenderedComponent<MokaSankey> cut = Render<MokaSankey>(p => p
			.Add(x => x.Links, Requests)
			.Add(x => x.Nodes, Names));

		Assert.Equal(9, cut.FindAll(".moka-sankey__ribbon").Count);
		Assert.Equal(6, cut.FindAll(".moka-sankey__node").Count);
		Assert.Equal(3, cut.FindAll(".moka-sankey__label--start").Count);
		Assert.Equal(3, cut.FindAll(".moka-sankey__label--end").Count);
		Assert.Equal(["Web", "API", "Jobs"], cut.Instance.SeriesInfo.Select(s => s.Name));
	}

	[Fact]
	public void SourcesTakeThePalette_AndTheirFlowsFollow_TheEndsStayMuted()
	{
		IRenderedComponent<MokaSankey> cut = Render<MokaSankey>(p => p
			.Add(x => x.Links, Requests)
			.Add(x => x.Nodes, Names));

		IReadOnlyList<IElement> nodes = cut.FindAll(".moka-sankey__node");
		Assert.Contains("moka-chart-c1", nodes[0].ClassList);
		Assert.Contains("moka-chart-muted", nodes[3].ClassList);
		Assert.Contains("moka-chart-c1", cut.Find(".moka-sankey__ribbon").ClassList);
	}

	[Fact]
	public void TheLargestNodeSitsOnTop_AndNodesAreAsTallAsTheirFlow()
	{
		IRenderedComponent<MokaSankey> cut = Render<MokaSankey>(p => p
			.Add(x => x.Links, Requests)
			.Add(x => x.Nodes, Names));

		IReadOnlyList<IElement> labels = cut.FindAll(".moka-sankey__label--start .moka-sankey__name");
		Assert.Equal(["Web", "API", "Jobs"], labels.Select(l => l.TextContent));
		IReadOnlyList<IElement> nodes = cut.FindAll(".moka-sankey__node");
		Assert.True(Height(nodes[0]) > Height(nodes[1]));
		Assert.Equal(Height(nodes[0]) / 5.8, Height(nodes[2]) / 1.1, 2);
	}

	[Fact]
	public void TheTooltip_SaysTheFlowAndItsShareOfTheSource()
	{
		IRenderedComponent<MokaSankey> cut = Render<MokaSankey>(p => p
			.Add(x => x.Links, Requests)
			.Add(x => x.Nodes, Names));

		Assert.Equal("Web to Cache: 3.4, 59% of Web", cut.Find(".moka-sankey__ribbon title").TextContent);
	}

	[Fact]
	public void PastMaxNodesPerSide_TheSmallestFoldIntoOther()
	{
		MokaSankeyLink[] fan = [.. Enumerable.Range(1, 8).Select(i => new MokaSankeyLink("wan", $"d{i}", i))];
		IRenderedComponent<MokaSankey> cut = Render<MokaSankey>(p => p
			.Add(x => x.MaxNodesPerSide, 4)
			.Add(x => x.Links, fan));

		IReadOnlyList<IElement> ends = cut.FindAll(".moka-sankey__label--end .moka-sankey__name");
		Assert.Equal(["d8", "d7", "d6", "Other (5)"], ends.Select(e => e.TextContent));
		Assert.Equal(4, cut.FindAll(".moka-sankey__ribbon").Count);
	}

	[Fact]
	public void AHiddenSource_TakesItsFlowsAway()
	{
		IRenderedComponent<MokaSankey> cut = Render<MokaSankey>(p => p
			.Add(x => x.HiddenSeries, ["jobs"])
			.Add(x => x.Links, Requests)
			.Add(x => x.Nodes, Names));

		Assert.Equal(6, cut.FindAll(".moka-sankey__ribbon").Count);
		Assert.True(cut.Instance.SeriesInfo[2].Hidden);
	}

	[Fact]
	public void ThreeColumns_PutTheMiddleNamesOverTheFlows()
	{
		IRenderedComponent<MokaSankey> cut = Render<MokaSankey>(p => p
			.Add(x => x.Links, [new MokaSankeyLink("wan", "router", 5), new MokaSankeyLink("router", "lan", 3), new MokaSankeyLink("router", "wifi", 2)]));

		Assert.Single(cut.FindAll(".moka-sankey__label--mid"));
		Assert.Contains("--_sk-x: 50%", cut.Find(".moka-sankey__label--mid").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void TheActiveRibbonAndNode_ReadTheirValues()
	{
		IRenderedComponent<MokaSankey> cut = Render<MokaSankey>(p => p
			.Add(x => x.Links, Requests)
			.Add(x => x.Nodes, Names));

		Assert.Equal("Web to Cache, 3.4, 59% of Web", cut.Instance.DescribeActive(0, 0, 0, null)!.CellText);
		Assert.Equal("Web, 5.8", cut.Instance.DescribeActive(0, 0, 9, null)!.CellText);
	}

	[Fact]
	public void TheTable_ListsEveryFlow()
	{
		IRenderedComponent<MokaSankey> cut = Render<MokaSankey>(p => p
			.Add(x => x.Links, Requests)
			.Add(x => x.Nodes, Names));

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(9, table.RowCount);
		Assert.Equal("Web", table.Display(0, 0));
		Assert.Equal("Cache", table.Display(0, 1));
	}

	[Fact]
	public void ALinksOwnColor_PaintsItsFlow_AndTheRestKeepTheirSources()
	{
		MokaSankeyLink[] links =
		[
			new("web", "cache", 3.4) { Color = MokaColor.Error },
			new("web", "db", 1.9) { ColorValue = " var(--odd) " },
			new("web", "storage", 0.5)
		];
		IRenderedComponent<MokaSankey> cut = Render<MokaSankey>(p => p.Add(x => x.Links, links));

		IReadOnlyList<IElement> ribbons = cut.FindAll(".moka-sankey__ribbon");
		Assert.Equal("moka-sankey__ribbon moka-chart-tone-error", ribbons[0].ClassName);
		Assert.Null(ribbons[0].GetAttribute("style"));
		Assert.Equal("--moka-chart-color: var(--odd)", ribbons[1].GetAttribute("style"));
		Assert.Equal("moka-sankey__ribbon moka-chart-c1", ribbons[2].ClassName);
		Assert.Contains("moka-chart-c1", cut.Find(".moka-sankey__node").ClassList);
	}

	[Fact]
	public void AColorValue_WinsOverColor_AndOneThatIsNoColorIsIgnored()
	{
		MokaSankeyLink[] links =
		[
			new("a", "b", 2) { Color = MokaColor.Error, ColorValue = "#ffab40" },
			new("a", "c", 1) { ColorValue = "red; background: url(x)" }
		];
		IRenderedComponent<MokaSankey> cut = Render<MokaSankey>(p => p.Add(x => x.Links, links));

		IReadOnlyList<IElement> ribbons = cut.FindAll(".moka-sankey__ribbon");
		Assert.Equal("--moka-chart-color: #ffab40", ribbons[0].GetAttribute("style"));
		Assert.Equal("moka-sankey__ribbon moka-chart-c1", ribbons[1].ClassName);
		Assert.Null(ribbons[1].GetAttribute("style"));
	}

	[Fact]
	public void AColoredFlow_IsMuted_WithTheSourceAHighlightMutes()
	{
		MokaSankeyLink[] links = [new("a", "x", 2) { Color = MokaColor.Error }, new("b", "x", 1) { Color = MokaColor.Error }];
		IRenderedComponent<MokaSankey> cut = Render<MokaSankey>(p => p
			.Add(x => x.Links, links)
			.Add(x => x.Highlight, "a"));

		IReadOnlyList<IElement> ribbons = cut.FindAll(".moka-sankey__ribbon");
		Assert.Contains("moka-chart-tone-error", ribbons[0].ClassList);
		Assert.Contains("moka-chart-muted", ribbons[1].ClassList);
	}

	[Fact]
	public void MergedFlows_KeepAColor_OnlyWhenEveryOneGaveTheSame()
	{
		SankeyTint red = new(MokaColor.Error, null);
		(List<SankeyFlow> flows, _, _) = SankeyLayout.Acyclic(
		[
			new SankeyFlow("a", "b", 1, red), new SankeyFlow("a", "b", 2, red),
			new SankeyFlow("a", "c", 1, red), new SankeyFlow("a", "c", 1),
			new SankeyFlow("a", "d", 1, red), new SankeyFlow("a", "d", 1, new SankeyTint(null, "#fff"))
		]);

		Assert.Equal(red, flows.Single(f => f.To == "b").Tint);
		Assert.Null(flows.Single(f => f.To == "c").Tint);
		Assert.Null(flows.Single(f => f.To == "d").Tint);
	}

	[Fact]
	public void FoldedFlows_KeepAColor_OnlyWhenEveryOneGaveTheSame()
	{
		SankeyTint red = new(MokaColor.Error, null);
		List<SankeyFlow> flows =
		[
			new("wan", "d1", 9, red), new("wan", "d2", 8), new("wan", "d3", 2, red), new("wan", "d4", 1, red),
			new("lan", "d3", 1, red), new("lan", "d4", 1)
		];
		Dictionary<string, int> column = new(StringComparer.Ordinal) { ["wan"] = 0, ["lan"] = 0, ["d1"] = 1, ["d2"] = 1, ["d3"] = 1, ["d4"] = 1 };
		Dictionary<string, double> value = new(StringComparer.Ordinal) { ["wan"] = 20, ["lan"] = 2, ["d1"] = 9, ["d2"] = 8, ["d3"] = 3, ["d4"] = 2 };

		(List<SankeyFlow> folded, _) = SankeyLayout.Fold(flows, column, key => value[key], 3);

		string other = SankeyLayout.OtherKey(1);
		Assert.Equal(red, folded.Single(f => f.From == "wan" && f.To == other).Tint);
		Assert.Null(folded.Single(f => f.From == "lan" && f.To == other).Tint);
		Assert.Equal(red, folded.Single(f => f.To == "d1").Tint);
	}

	// MokaGate read a flow's far end back out of the tooltip heading, "Laptop to Websites", through a table of its own.
	[Fact]
	public void AFlowsPoint_NamesBothEnds_AndANodesPointOnlyItself()
	{
		IRenderedComponent<MokaSankey> cut = Render<MokaSankey>(p => p
			.Add(x => x.Links, Requests)
			.Add(x => x.Nodes, Names));

		MokaChartPoint flow = cut.Instance.DescribeActive(0, 0, 0, null)!.Context.Focus!;
		Assert.Equal(("web", "Web", "cache", "Cache", "Web to Cache"), (flow.SeriesKey, flow.SeriesName, flow.ToKey, flow.ToName, flow.Category));
		MokaChartPoint node = cut.Instance.DescribeActive(0, 0, 9, null)!.Context.Focus!;
		Assert.Equal(("web", null, null), (node.SeriesKey, node.ToKey, node.ToName));
	}

	[Fact]
	public async Task ClickingAFlow_ReportsItsTarget_AndClickingANodeDoesNot()
	{
		this.Interactive();
		List<MokaChartPoint> clicked = [];
		IRenderedComponent<MokaSankey> cut = Render<MokaSankey>(p => p
			.Add(x => x.Links, Requests)
			.Add(x => x.Nodes, Names)
			.Add(x => x.OnPointClick, (MokaChartPoint point) => clicked.Add(point)));

		await cut.InvokeAsync(() => cut.Instance.OnSelect(0, 0, 4));
		await cut.InvokeAsync(() => cut.Instance.OnSelect(0, 0, 12));

		cut.WaitForAssertion(() => Assert.Equal(2, clicked.Count));
		Assert.Equal(("api", "db"), (clicked[0].SeriesKey, clicked[0].ToKey));
		Assert.Equal(("cache", null), (clicked[1].SeriesKey, clicked[1].ToKey));
	}

	private static double Height(IElement node)
	{
		string style = node.GetAttribute("style")!;
		int at = style.IndexOf("height: ", StringComparison.Ordinal) + 8;
		return double.Parse(style[at..style.IndexOf('%', at)], System.Globalization.CultureInfo.InvariantCulture);
	}
}
