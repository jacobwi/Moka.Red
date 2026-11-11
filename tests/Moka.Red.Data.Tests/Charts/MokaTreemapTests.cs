#pragma warning disable CA5394 // Seeded Random keeps the property tests repeatable; nothing here is security related
using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

public sealed class MokaTreemapTests : BunitContext
{
	private static readonly MokaTreemapNode[] Disk =
	[
		new("Media", 0) { Children = [new("Photos", 412), new("Videos", 388), new("Music", 64)] },
		new("Backups", 0) { Children = [new("Laptop", 236), new("Server", 142), new("Phone", 98)] },
		new("Work", 0) { Children = [new("Projects", 121), new("Mail", 38), new("Documents", 26)] },
		new("System", 95)
	];

	public MokaTreemapTests() => this.Static();

	[Fact]
	public void Squarify_GivesEachValueItsShareOfTheArea()
	{
		Random random = new(3);
		for (int run = 0; run < 50; run++)
		{
			double[] values = [.. Enumerable.Range(0, random.Next(1, 40)).Select(_ => 1 + random.NextDouble() * 500)
				.OrderDescending()];
			TreemapRect box = new(0, 0, 600, 400);
			TreemapRect[] rects = TreemapLayout.Squarify(values, box);

			double total = values.Sum();
			for (int i = 0; i < values.Length; i++)
			{
				double area = rects[i].Width * rects[i].Height;
				Assert.InRange(area / (600 * 400), values[i] / total * 0.99, values[i] / total * 1.01);
				Assert.InRange(rects[i].X, -0.001, 600.001);
				Assert.InRange(rects[i].X + rects[i].Width, -0.001, 600.001);
				Assert.InRange(rects[i].Y + rects[i].Height, -0.001, 400.001);
			}
		}
	}

	[Fact]
	public void Squarify_KeepsTilesNearSquare()
	{
		TreemapRect[] rects = TreemapLayout.Squarify([6d, 6, 4, 3, 2, 2, 1], new TreemapRect(0, 0, 600, 400));

		Assert.All(rects, rect => Assert.InRange(Math.Max(rect.Width / rect.Height, rect.Height / rect.Width), 1, 4));
	}

	[Fact]
	public void DrawsALeafTilePerPart_TwoLevelsDeep()
	{
		IRenderedComponent<MokaTreemap> cut = Render<MokaTreemap>(p => p.Add(x => x.Nodes, Disk));

		IReadOnlyList<IElement> tiles = cut.FindAll(".moka-treemap__tile");
		Assert.Equal(10, tiles.Count);
		Assert.Equal(["Photos", "Videos", "Music"], tiles.Take(3).Select(t => t.QuerySelector(".moka-treemap__name")!.TextContent));
		Assert.Contains("moka-chart-c1", tiles[0].ClassList);
		Assert.Contains("moka-treemap__tile--d2", tiles[0].ClassList);
		Assert.Contains("moka-treemap__tile--d1", tiles[^1].ClassList);
		Assert.Equal(["Media", "Backups", "Work", "System"], cut.Instance.SeriesInfo.Select(s => s.Name));
	}

	[Fact]
	public void DepthOne_DrawsOnlyTheTopLevel()
	{
		IRenderedComponent<MokaTreemap> cut = Render<MokaTreemap>(p => p
			.Add(x => x.Depth, 1)
			.Add(x => x.Nodes, Disk));

		Assert.Equal(4, cut.FindAll(".moka-treemap__tile").Count);
		Assert.Equal("864", cut.Find(".moka-treemap__value").TextContent);
	}

	[Fact]
	public void TheTooltip_SaysTheShareOfItsParentAndOfAll()
	{
		IRenderedComponent<MokaTreemap> cut = Render<MokaTreemap>(p => p.Add(x => x.Nodes, Disk));

		Assert.Equal("Media / Photos: 412, 48% of Media, 25% of all", cut.Find(".moka-treemap__tile").GetAttribute("title"));
	}

	[Fact]
	public void TheLabel_CarriesTheNarrowestTileItsNameFits()
	{
		IRenderedComponent<MokaTreemap> cut = Render<MokaTreemap>(p => p.Add(x => x.Nodes, Disk));

		Assert.Contains("moka-treemap__label--w5", cut.Find(".moka-treemap__label").ClassList);
	}

	[Fact]
	public void PastMaxTiles_TheSmallestFoldIntoOther()
	{
		MokaTreemapNode[] many =
			[new("Root", 0) { Children = [.. Enumerable.Range(1, 20).Select(i => new MokaTreemapNode($"f{i}", i))] }];
		IRenderedComponent<MokaTreemap> cut = Render<MokaTreemap>(p => p
			.Add(x => x.MaxTiles, 10)
			.Add(x => x.Nodes, many));

		IReadOnlyList<IElement> tiles = cut.FindAll(".moka-treemap__tile");
		Assert.Equal(10, tiles.Count);
		IElement other = Assert.Single(cut.FindAll(".moka-treemap__tile--other"));
		Assert.Equal("Other (11)", other.QuerySelector(".moka-treemap__name")!.TextContent);
		Assert.Equal(((1 + 11) * 11 / 2).ToString(CultureInfo.InvariantCulture), other.QuerySelector(".moka-treemap__value")!.TextContent);
	}

	[Fact]
	public void PastMaxTiles_AtTheTopLevel_FoldsThereToo()
	{
		IRenderedComponent<MokaTreemap> cut = Render<MokaTreemap>(p => p
			.Add(x => x.MaxTiles, 5)
			.Add(x => x.Nodes, [.. Enumerable.Range(1, 9).Select(i => new MokaTreemapNode($"n{i}", i))]));

		Assert.Equal(5, cut.FindAll(".moka-treemap__tile").Count);
		Assert.Single(cut.FindAll(".moka-treemap__tile--other"));
	}

	[Fact]
	public void HiddenTopParts_Leave()
	{
		IRenderedComponent<MokaTreemap> cut = Render<MokaTreemap>(p => p
			.Add(x => x.HiddenSeries, ["Media"])
			.Add(x => x.Nodes, Disk));

		Assert.Equal(7, cut.FindAll(".moka-treemap__tile").Count);
		Assert.True(cut.Instance.SeriesInfo[0].Hidden);
	}

	[Fact]
	public void WithOnNodeClick_TilesAreButtons_AndAClickRaisesItsNode()
	{
		MokaTreemapNode? clicked = null;
		IRenderedComponent<MokaTreemap> cut = Render<MokaTreemap>(p => p
			.Add(x => x.Nodes, Disk)
			.Add(x => x.OnNodeClick, node => clicked = node));

		IElement first = cut.Find("button.moka-treemap__tile");
		Assert.Equal("Media / Photos: 412, 48% of Media, 25% of all", first.GetAttribute("aria-label"));
		first.Click();

		Assert.Equal("Photos", clicked!.Name);
		Assert.Equal("figure", cut.Find(".moka-treemap").GetAttribute("role"));
	}

	[Fact]
	public void TheTable_ListsEveryTile()
	{
		IRenderedComponent<MokaTreemap> cut = Render<MokaTreemap>(p => p.Add(x => x.Nodes, Disk));

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(10, table.RowCount);
		Assert.Equal("Media / Photos", table.Display(0, 0));
	}

	[Fact]
	public void APartChangedDeepInTheTree_DrawsAgain()
	{
		List<MokaTreemapNode> kids = [new("A", 1), new("B", 1)];
		MokaTreemapNode[] nodes = [new("Root", 0) { Children = kids }];
		IRenderedComponent<MokaTreemap> cut = Render<MokaTreemap>(p => p.Add(x => x.Nodes, nodes));

		kids[0] = new MokaTreemapNode("A", 3);
		cut.Render(p => p.Add(x => x.Nodes, nodes));

		Assert.Equal("Root / A: 3, 75% of Root, 75% of all", cut.Find(".moka-treemap__tile").GetAttribute("title"));
	}
}
