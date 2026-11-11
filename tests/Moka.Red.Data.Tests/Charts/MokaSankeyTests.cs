using AngleSharp.Dom;
using Bunit;
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

	private static double Height(IElement node)
	{
		string style = node.GetAttribute("style")!;
		int at = style.IndexOf("height: ", StringComparison.Ordinal) + 8;
		return double.Parse(style[at..style.IndexOf('%', at)], System.Globalization.CultureInfo.InvariantCulture);
	}
}
