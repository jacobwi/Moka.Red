#pragma warning disable CA5394 // Seeded Random keeps the property tests repeatable; nothing here is security related
using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

public sealed class MokaDonutChartShapeTests : BunitContext
{
	private static readonly MokaDonutSegment[] Storage =
	[
		new() { Name = "Media", Value = 864 },
		new() { Name = "Backups", Value = 476 },
		new() { Name = "Work", Value = 185 },
		new() { Name = "System", Value = 95 }
	];

	public MokaDonutChartShapeTests() => this.Static();

	[Fact]
	public void ByDefault_TheRingStaysClassic()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p.Add(x => x.Segments, Storage));

		Assert.Equal("moka-donutchart moka-donutchart--md", cut.Find(".moka-donutchart").ClassName);
		Assert.Empty(cut.FindComponents<MokaChartFrame>());
	}

	[Fact]
	public void OnTheOriginalDesign_ALookStillDrawsTheClassicRing()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.AddCascadingValue(MokaTheme.Dark)
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Segments, Storage));

		Assert.Equal("moka-donutchart moka-donutchart--md", cut.Find(".moka-donutchart").ClassName);
	}

	[Fact]
	public void ATitle_MovesItOntoTheFrame_WithTheShareListBesideTheRing()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Title, "Storage")
			.Add(x => x.Segments, Storage));

		IElement root = cut.Find(".moka-donutchart");
		Assert.Contains("moka-chart", root.ClassList);
		Assert.Contains("moka-donutchart--ring", root.ClassList);
		Assert.Single(cut.FindComponents<MokaChartFrame>());
		Assert.Contains("Storage", root.TextContent, StringComparison.Ordinal);
		Assert.Equal(4, cut.FindAll(".moka-donutchart-item").Count);
		Assert.Equal(["53%", "29%", "11%", "6%"], cut.FindAll(".moka-donutchart-pct").Select(e => e.TextContent));
		Assert.Equal(["Media", "Backups", "Work", "System"], cut.Instance.SeriesInfo.Select(s => s.Name));
		Assert.Empty(cut.FindAll(".moka-chart-legend"));
		Assert.Equal(["Media", "Backups", "Work", "System"],
			cut.FindAll(".moka-donutchart-item").Select(e => e.GetAttribute("data-moka-legend-key")));
		Assert.Equal("Media", cut.Find(".moka-donutchart-arc").GetAttribute("data-moka-key"));
	}

	[Fact]
	public void TheModernLabel_SummarisesTheShares()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Shape, MokaDonutShape.Bar)
			.Add(x => x.Segments, Storage));

		Assert.Equal("Media 53%, Backups 29%, Work 11%, System 6%", cut.Find(".moka-donutchart").GetAttribute("aria-label"));
	}

	[Fact]
	public void Bar_DrawsOneSegmentPerShare_SizedByItsShare_WithTheShareInside()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Shape, MokaDonutShape.Bar)
			.Add(x => x.Segments, Storage));

		IReadOnlyList<IElement> segments = cut.FindAll(".moka-donutchart-seg");
		Assert.Equal(4, segments.Count);
		Assert.StartsWith("flex-grow: 0.53333", segments[0].GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("53%", segments[0].QuerySelector(".moka-donutchart-seg-share")!.TextContent);
		Assert.Equal("0", segments[0].GetAttribute("data-i"));
		Assert.Contains("moka-chart-c1", segments[0].ClassList);
		Assert.Contains("moka-chart-c4", segments[3].ClassList);
		Assert.Equal(["Media", "Backups", "Work", "System"], cut.Instance.SeriesInfo.Select(s => s.Name));
	}

	[Fact]
	public void Bar_WithATotal_LeavesTheRestAsTrack_AndCanDropTheShares()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Shape, MokaDonutShape.Bar)
			.Add(x => x.ShowShares, false)
			.Add(x => x.Total, 2000d)
			.Add(x => x.Segments, Storage));

		Assert.Equal("flex-grow: 0.19", cut.Find(".moka-donutchart-seg--rest").GetAttribute("style"));
		Assert.Empty(cut.FindAll(".moka-donutchart-seg-share"));
	}

	[Fact]
	public void Waffle_DrawsAHundredSquares_SplitByLargestRemainder()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Shape, MokaDonutShape.Waffle)
			.Add(x => x.Segments,
			[
				new MokaDonutSegment { Name = "A", Value = 1 },
				new MokaDonutSegment { Name = "B", Value = 1 },
				new MokaDonutSegment { Name = "C", Value = 1 }
			]));

		Assert.Equal(100, cut.FindAll(".moka-donutchart-cell").Count);
		Assert.Equal([34, 33, 33], cut.FindAll(".moka-donutchart-group").Select(g => g.QuerySelectorAll(".moka-donutchart-cell").Length));
		Assert.Empty(cut.FindAll(".moka-donutchart-cell--empty"));
	}

	[Fact]
	public void Waffle_WithATotal_LeavesTheRestEmpty()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Shape, MokaDonutShape.Waffle)
			.Add(x => x.Total, 200d)
			.Add(x => x.Segments, [new MokaDonutSegment { Name = "A", Value = 62 }, new MokaDonutSegment { Name = "B", Value = 24 }]));

		Assert.Equal(31, cut.FindAll(".moka-donutchart-group")[0].QuerySelectorAll(".moka-donutchart-cell").Length);
		Assert.Equal(12, cut.FindAll(".moka-donutchart-group")[1].QuerySelectorAll(".moka-donutchart-cell").Length);
		Assert.Equal(57, cut.FindAll(".moka-donutchart-cell--empty").Count);
	}

	[Fact]
	public void Waffle_NativeTooltip_IsOnTheWholeShare()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Shape, MokaDonutShape.Waffle)
			.Add(x => x.Segments, [new MokaDonutSegment { Name = "HTTPS", Value = 62 }, new MokaDonutSegment { Name = "DNS", Value = 38 }]));

		Assert.Equal("HTTPS: 62 (62%)", cut.FindAll(".moka-donutchart-group")[0].GetAttribute("title"));
	}

	[Theory]
	[InlineData(MokaChartLook.Quiet, "14.00", "butt")]
	[InlineData(MokaChartLook.Dense, "10.00", "butt")]
	[InlineData(MokaChartLook.Soft, "22.00", "round")]
	[InlineData(MokaChartLook.Bold, "30.00", "butt")]
	public void RingLooks_SetTheThicknessAndTheEnds(MokaChartLook look, string thickness, string cap)
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Look, look)
			.Add(x => x.Segments, Storage));

		IElement arc = cut.Find(".moka-donutchart-segment");
		Assert.Equal(thickness, arc.GetAttribute("stroke-width"));
		Assert.Equal(cap, arc.GetAttribute("stroke-linecap"));
		Assert.Empty(cut.FindAll(".moka-donutchart-track"));
	}

	[Fact]
	public void QuietRing_LeavesAGapBetweenSegments()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Segments, [new MokaDonutSegment { Name = "A", Value = 1 }, new MokaDonutSegment { Name = "B", Value = 1 }]));

		double radius = (200 - 14) / 2d;
		double half = Math.PI * radius;
		IElement first = cut.FindAll(".moka-donutchart-segment")[0];
		Assert.Equal((half - 2).ToString("F2", CultureInfo.InvariantCulture), first.GetAttribute("stroke-dasharray")!.Split(' ')[0]);
		Assert.Equal("-1.00", first.GetAttribute("stroke-dashoffset"));
	}

	[Fact]
	public void ASetThickness_WinsOverTheLook()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Look, MokaChartLook.Bold)
			.Add(x => x.Thickness, 12d)
			.Add(x => x.Segments, Storage));

		Assert.Equal("12.00", cut.Find(".moka-donutchart-segment").GetAttribute("stroke-width"));
	}

	[Fact]
	public void Texture_HatchesEachArc_WithAPatternInsideItsOwnGroup()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Texture, true)
			.Add(x => x.Segments, Storage));

		IReadOnlyList<IElement> groups = cut.FindAll(".moka-donutchart-arc");
		Assert.Equal(4, groups.Count);
		IElement pattern = groups[1].QuerySelector("pattern")!;
		Assert.Equal("rotate(135)", pattern.GetAttribute("patternTransform"));
		Assert.Equal($"stroke: url(#{pattern.Id})", groups[1].QuerySelector(".moka-donutchart-segment")!.GetAttribute("style"));
		Assert.Equal("9", groups[2].QuerySelector("pattern")!.GetAttribute("width"));
	}

	[Fact]
	public void HiddenSegments_LeaveTheShares_AndKeepTheirColors()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Shape, MokaDonutShape.Bar)
			.Add(x => x.HiddenSeries, ["Backups"])
			.Add(x => x.Segments, Storage));

		IReadOnlyList<IElement> segments = cut.FindAll(".moka-donutchart-seg");
		Assert.Equal(3, segments.Count);
		Assert.Contains("moka-chart-c3", segments[1].ClassList);
		Assert.Equal("76%", segments[0].TextContent.Trim());
	}

	[Fact]
	public void AHighlight_MutesTheOtherSegments()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Shape, MokaDonutShape.Bar)
			.Add(x => x.Highlight, "Work")
			.Add(x => x.Segments, Storage));

		IReadOnlyList<IElement> segments = cut.FindAll(".moka-donutchart-seg");
		Assert.Contains("moka-chart-muted", segments[0].ClassList);
		Assert.Contains("moka-chart-c3", segments[2].ClassList);
	}

	[Fact]
	public void ASegmentsOwnColor_IsWrittenInline()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Shape, MokaDonutShape.Bar)
			.Add(x => x.Segments, [new MokaDonutSegment { Name = "A", Value = 1, ColorValue = "var(--moka-color-info)" }]));

		Assert.Equal("flex-grow: 1; --moka-chart-color: var(--moka-color-info)", cut.Find(".moka-donutchart-seg").GetAttribute("style"));
	}

	[Fact]
	public void TheActivePoint_ReadsTheSegmentItsValueAndItsShare()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Shape, MokaDonutShape.Bar)
			.Add(x => x.Formatter, v => $"{v.ToString(CultureInfo.InvariantCulture)} GB")
			.Add(x => x.Segments, Storage));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, 0, 1, null)!;
		Assert.Equal("Backups, 476 GB, 29%", info.CellText);
		Assert.Equal(476, info.Context.Focus!.Value);
		Assert.Null(cut.Instance.DescribeActive(0, 0, 9, null));
	}

	[Fact]
	public void TheTable_ListsEverySegment()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Shape, MokaDonutShape.Bar)
			.Add(x => x.Segments, Storage));

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(4, table.RowCount);
		Assert.Equal("Media", table.Display(0, 0));
		Assert.Equal("0.5333333333333333", table.Raw(0, 2));
	}

	[Fact]
	public void ASegmentChangedInPlace_DrawsAgain()
	{
		MokaDonutSegment[] segments = [new() { Name = "A", Value = 1 }, new() { Name = "B", Value = 3 }];
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Shape, MokaDonutShape.Bar)
			.Add(x => x.Segments, segments));

		segments[0] = segments[0] with { Value = 3 };
		cut.Render(p => p.Add(x => x.Segments, segments));

		Assert.Equal("50%", cut.Find(".moka-donutchart-seg-share").TextContent);
	}
}

public sealed class ChartWholesTests
{
	[Theory]
	[InlineData(new[] { 1d, 1, 1 }, 100, new[] { 34, 33, 33 })]
	[InlineData(new[] { 62d, 24, 4, 10 }, 100, new[] { 62, 24, 4, 10 })]
	[InlineData(new[] { 0.5d, 0.5, 99 }, 100, new[] { 1, 0, 99 })]
	[InlineData(new[] { 7d, 7, 7, 7, 7, 7 }, 100, new[] { 17, 17, 17, 17, 16, 16 })]
	public void LargestRemainder_AddsUpExactly(double[] values, int cells, int[] expected)
	{
		int[] counts = ChartWholes.LargestRemainder(values, values.Sum(), cells);

		Assert.Equal(expected, counts);
		Assert.Equal(cells, counts.Sum());
	}

	[Fact]
	public void LargestRemainder_AlwaysSumsToTheWhole_ForRandomShares()
	{
		Random random = new(7);
		for (int run = 0; run < 200; run++)
		{
			double[] values = [.. Enumerable.Range(0, random.Next(1, 12)).Select(_ => random.NextDouble() * 1000)];
			Assert.Equal(100, ChartWholes.LargestRemainder(values, values.Sum(), 100).Sum());
		}
	}

	[Fact]
	public void LargestRemainder_OfAWholeLargerThanTheValues_FillsOnlyTheirPart()
	{
		Assert.Equal(43, ChartWholes.LargestRemainder([30d, 12.5], 100, 100).Sum());
	}
}
