#pragma warning disable CA5394 // Seeded Random keeps the dedupe test repeatable; nothing here is security related
using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Charts;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

public sealed class MokaScatterChartTests : BunitContext
{
	private static readonly MokaScatterSeries[] Devices =
	[
		new("Laptops", [new(190, 11.2, null, "laptop-01"), new(220, 14.5, null, "laptop-02"), new(150, 6.4, null, "laptop-03")]),
		new("Phones", [new(80, 3.2, null, "pixel-8"), new(60, 2.1, null, "iphone-anna")]),
		new("IoT", [new(290, 0.9, null, "cam-door"), new(214, 26.4, null, "tv-living-room")])
	];

	private static readonly MokaScatterSeries[] Load =
	[
		new("Load", [new(4200, 0.4, 6, "api"), new(2600, 1.1, 8, "search"), new(300, 0.9, 1, "mail"), new(450, 0.1, null, "billing")])
	];

	public MokaScatterChartTests() => this.Static();

	[Fact]
	public void Dots_AreOnePathPerSeries_OfZeroLengthSegments()
	{
		IRenderedComponent<MokaScatterChart> cut = Render<MokaScatterChart>(p => p.Add(x => x.Series, Devices).Add(x => x.Tooltip, MokaChartTooltip.Auto));

		IReadOnlyList<IElement> paths = cut.FindAll(".moka-scatter__dots");
		Assert.Equal(3, paths.Count);
		Assert.Equal([3, 2, 2], paths.Select(path => Segments(path).Length));
		Assert.Equal(["Laptops", "Phones", "IoT"], paths.Select(path => path.GetAttribute("data-moka-key")!));
		Assert.Contains("moka-chart-c2", paths[1].ClassList);
		Assert.Equal("true", cut.Find(".moka-scatter__svg").GetAttribute("aria-hidden"));
		Assert.Equal(["Laptops", "Phones", "IoT"], cut.Instance.SeriesInfo.Select(s => s.Name));
	}

	[Fact]
	public void APointSitsWhereItsTwoMeasuresPutIt()
	{
		IRenderedComponent<MokaScatterChart> cut = Render<MokaScatterChart>(p => p.Add(x => x.Series, Devices).Add(x => x.Tooltip, MokaChartTooltip.Auto));

		MokaScale y = cut.Instance.ColumnPlotModel(0).Scale;
		ChartXFrame x = cut.Instance.XFrame();
		(double px, double py) = Segments(cut.FindAll(".moka-scatter__dots")[2])[0];
		Assert.Equal(Math.Round(x.Share(214) * 10000), px);
		Assert.Equal(Math.Round((1 - y.Share(26.4)) * 10000), py);
		Assert.Equal(0, y.Min);
	}

	[Fact]
	public void PastTwoThousandPoints_ASeriesKeepsOneDotPerCell()
	{
		Random random = new(4);
		MokaScatterPoint[] points = [.. Enumerable.Range(0, 50_000).Select(_ => new MokaScatterPoint(random.NextDouble() * 10, random.NextDouble() * 10))];
		IRenderedComponent<MokaScatterChart> cut = Render<MokaScatterChart>(p => p
			.Add(x => x.Series, [new MokaScatterSeries("many", points)])
			.Add(x => x.DataVersion, 1)
			.Add(x => x.Tooltip, MokaChartTooltip.Auto));

		int drawn = Segments(cut.Find(".moka-scatter__dots")).Length;
		Assert.InRange(drawn, 2000, 400 * 173);
		Assert.True(drawn < points.Length);
		Assert.Contains("moka-scatter__dots--dense", cut.Find(".moka-scatter__dots").ClassList);
		Assert.True(cut.Instance.MeasuresWidth);
		Assert.Equal(drawn, cut.Instance.DrawnCount(0));
	}

	[Fact]
	public void TheHoverGeometry_IsTheDrawnDots_SortedAcross_EachSeriesItsOwnY()
	{
		IRenderedComponent<MokaScatterChart> cut = Render<MokaScatterChart>(p => p.Add(x => x.Series, Devices).Add(x => x.Tooltip, MokaChartTooltip.Auto));

		ChartHoverData data = cut.Instance.HoverData(0)!;
		float[] x = data.X.ToArray();
		Assert.Equal(7, x.Length);
		Assert.Equal(x.Order(), x);
		Assert.Equal(3, data.Y.Count);
		for (int i = 0; i < x.Length; i++)
		{
			Assert.Equal(1, data.Y.Count(y => float.IsFinite(y.Span[i])));
		}

		byte[] encoded = ChartHoverEncoding.Encode(data, 1);
		Assert.Equal(ChartHoverEncoding.HeaderLength + 2 * 7 * 4, encoded.Length);
	}

	[Fact]
	public void TheTooltip_NamesThePoint_AndSaysEachMeasure()
	{
		IRenderedComponent<MokaScatterChart> cut = Render<MokaScatterChart>(p => p
			.Add(x => x.Series, Devices)
			.Add(x => x.Tooltip, MokaChartTooltip.Auto)
			.Add(x => x.XName, "connections")
			.Add(x => x.YName, "used")
			.Add(x => x.Unit, MokaChartUnit.Of("GB")));

		ChartHoverData data = cut.Instance.HoverData(0)!;
		int tv = Enumerable.Range(0, 7).Single(i => float.IsFinite(data.Y[2].Span[i]) && data.X.Span[i] > 0.5 && data.Y[2].Span[i] > 0.5);
		ChartActiveInfo info = cut.Instance.DescribeActive(0, 2, tv, null)!;
		Assert.Equal("tv-living-room, IoT", info.Heading);
		Assert.Equal(["26.4 GB", "214"], info.Rows.Select(row => row.ValueText));
		Assert.Equal(["used", "connections"], info.Rows.Select(row => row.Name));
		Assert.Equal("tv-living-room, IoT, used 26.4 GB, connections 214", info.CellText);
		Assert.Equal(1, info.Context.Points[0].Index);
		Assert.Equal("--moka-chart-color: transparent", info.Rows[1].ColorStyle);
		Assert.Equal("connections", cut.Find(".moka-chart-plot__xtitle").TextContent);
	}

	[Fact]
	public void Bubbles_AreALineEach_LargestFirst_TheirAreaFollowingTheSize()
	{
		IRenderedComponent<MokaScatterChart> cut = Render<MokaScatterChart>(p => p
			.Add(x => x.Series, Load)
			.Add(x => x.MinSize, 4)
			.Add(x => x.MaxSize, 28));

		IReadOnlyList<IElement> lines = cut.FindAll(".moka-scatter__point--bubble");
		double[] widths = [.. lines.Select(line => double.Parse(RowChartKit.Var(line, "stroke-width")!.TrimEnd('p', 'x'), CultureInfo.InvariantCulture))];
		Assert.Equal(widths.OrderDescending(), widths);
		Assert.Equal(28, widths[0]);
		Assert.Equal(Math.Round(28 * Math.Sqrt(6d / 8), 1), widths[1]);
		Assert.Equal(Math.Max(4, Math.Round(28 * Math.Sqrt(1d / 8), 1)), widths[2]);
		Assert.Equal(4, widths[3]);
		Assert.Contains("moka-scatter--bubbles", cut.Find(".moka-scatter").ClassList);
		Assert.StartsWith("search: ", lines[0].QuerySelector("title")!.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void PastMaxBubbles_TheLargestAreKept_AndTheChartSaysSo()
	{
		IRenderedComponent<MokaScatterChart> cut = Render<MokaScatterChart>(p => p
			.Add(x => x.Series, Load)
			.Add(x => x.MaxBubbles, 2));

		Assert.Equal(2, cut.FindAll(".moka-scatter__point--bubble").Count);
		Assert.Equal("The 2 largest of 4 are drawn", cut.Find(".moka-scatter__note").TextContent);
		Assert.EndsWith("The 2 largest of 4 are drawn", cut.Find(".moka-scatter").GetAttribute("aria-label"), StringComparison.Ordinal);
	}

	[Fact]
	public void HiddenSeries_Leave_AndTheAxesRefit()
	{
		IRenderedComponent<MokaScatterChart> cut = Render<MokaScatterChart>(p => p
			.Add(x => x.Series, Devices)
			.Add(x => x.HiddenSeries, ["IoT"])
			.Add(x => x.Tooltip, MokaChartTooltip.Auto));

		Assert.Equal(2, cut.FindAll(".moka-scatter__dots").Count);
		Assert.True(cut.Instance.ColumnPlotModel(0).Scale.Max < 26.4);
	}

	[Fact]
	public void ALogAxis_LeavesOutWhatItCantPlace()
	{
		IRenderedComponent<MokaScatterChart> cut = Render<MokaScatterChart>(p => p
			.Add(x => x.Series, [new MokaScatterSeries("s", [new(1, 1), new(10, 2), new(0, 3), new(-5, 4), new(1000, 5)])])
			.Add(x => x.XAxis, new MokaChartAxis { Scale = MokaAxisScale.Log })
			.Add(x => x.Tooltip, MokaChartTooltip.Auto));

		Assert.Equal(3, Segments(cut.Find(".moka-scatter__dots")).Length);
	}

	[Fact]
	public void AZoomedRange_DrawsOnlyThePointsInside()
	{
		IRenderedComponent<MokaScatterChart> cut = Render<MokaScatterChart>(p => p
			.Add(x => x.Series, Devices)
			.Add(x => x.Tooltip, MokaChartTooltip.Auto)
			.Add(x => x.Range, new MokaChartRange(150, 230)));

		Assert.Equal(4, cut.Instance.DrawnCount(0));
	}

	[Fact]
	public void OnTheStandardLook_FewDotsCarryNativeTooltips()
	{
		IRenderedComponent<MokaScatterChart> cut = Render<MokaScatterChart>(p => p.Add(x => x.Series, Devices).Add(x => x.YName, "used"));

		Assert.Empty(cut.FindAll(".moka-scatter__dots"));
		Assert.Equal(7, cut.FindAll(".moka-scatter__point").Count);
		Assert.Contains(cut.FindAll(".moka-scatter__point title"), title => title.TextContent.StartsWith("tv-living-room, IoT: 26.4 used", StringComparison.Ordinal));
	}

	[Fact]
	public void TheTable_ListsEveryPoint()
	{
		IRenderedComponent<MokaScatterChart> cut = Render<MokaScatterChart>(p => p.Add(x => x.Series, Devices));

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(7, table.RowCount);
		Assert.Equal(["Name", "Series", "x", "y"], table.Headers);
		Assert.Equal(["pixel-8", "Phones", "80", "3.2"], Enumerable.Range(0, 4).Select(column => table.Raw(3, column)));
		Assert.Equal("tv-living-room", table.Display(6, 0));
	}

	[Fact]
	public void Interactive_ItFindsTheNearestDot_AndStepsAlongASeries()
	{
		this.Interactive();
		IRenderedComponent<MokaScatterChart> cut = Render<MokaScatterChart>(p => p
			.Add(x => x.Series, Devices)
			.Add(x => x.Tooltip, MokaChartTooltip.Auto));

		Dictionary<string, object?> options = cut.Instance.SurfaceOptions();
		Assert.Equal("nearest", options["mode"]);
		Assert.Equal("columns", options["orientation"]);
		Assert.Null(options["sync"]);
		Assert.Equal("7", cut.Find(".moka-chart-surface").GetAttribute("data-moka-count"));
	}

	private static (double X, double Y)[] Segments(IElement element)
	{
		string d = element.GetAttribute("d") ?? "";
		return
		[
			.. d.Split('M', StringSplitOptions.RemoveEmptyEntries).Select(segment =>
			{
				string[] parts = segment.Replace("h0", "", StringComparison.Ordinal).Split(' ');
				return (double.Parse(parts[0], CultureInfo.InvariantCulture), double.Parse(parts[1], CultureInfo.InvariantCulture));
			})
		];
	}
}
