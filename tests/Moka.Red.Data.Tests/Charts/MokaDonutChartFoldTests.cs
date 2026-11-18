using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     Past <see cref="MokaDonutChart.MaxSegments" /> the smallest segments fold into one muted "Other", so no two
///     segments share a color, while the classic ring draws what it always has.
/// </summary>
public sealed class MokaDonutChartFoldTests : BunitContext
{
	public MokaDonutChartFoldTests() => this.Static();

	private static MokaDonutSegment[] Segments(int count) =>
		[.. Enumerable.Range(1, count).Select(i => new MokaDonutSegment { Name = $"s{i}", Value = 100 - i })];

	[Fact]
	public void PastEight_TheSmallestFoldIntoOther_InTheMutedColor_DrawnLast()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Title, "Traffic")
			.Add(x => x.Segments, Segments(12)));

		IReadOnlyList<IElement> items = cut.FindAll(".moka-donutchart-item");
		Assert.Equal(8, items.Count);
		Assert.Equal(["s1", "s2", "s3", "s4", "s5", "s6", "s7", "Other (5)"],
			items.Select(item => item.QuerySelector(".moka-donutchart-name")!.TextContent));
		Assert.Contains("moka-chart-muted", items[^1].ClassList);
		Assert.Equal("moka-chart-muted", cut.Instance.SeriesInfo[^1].ColorClass);
		Assert.Equal(["moka-chart-c1", "moka-chart-c7"], [cut.Instance.SeriesInfo[0].ColorClass, cut.Instance.SeriesInfo[6].ColorClass]);
	}

	[Fact]
	public void TheFoldedValues_AddUp_InTheTooltipAndTheTable()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Title, "Traffic")
			.Add(x => x.MaxSegments, 3)
			.Add(x => x.Segments, Segments(5)));

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(3, table.RowCount);
		Assert.Equal("Other (3)", table.Display(2, 0));
		Assert.Equal((97 + 96 + 95).ToString(CultureInfo.InvariantCulture), table.Raw(2, 1));
		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 2, null)!;
		Assert.Equal("Other (3)", info.Heading);
		Assert.Equal(-1, info.Context.Points[0].Series);
	}

	[Fact]
	public void TheSegmentsKept_AreTheLargest_InTheOrderGiven()
	{
		MokaDonutSegment[] segments =
		[
			new() { Name = "small", Value = 1 },
			new() { Name = "big", Value = 50 },
			new() { Name = "tiny", Value = 0.5 },
			new() { Name = "mid", Value = 20 }
		];
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Title, "Traffic")
			.Add(x => x.MaxSegments, 3)
			.Add(x => x.Segments, segments));

		Assert.Equal(["big", "mid", "Other (2)"], cut.Instance.SeriesInfo.Select(s => s.Name));
		Assert.Equal(["moka-chart-c2", "moka-chart-c4"], cut.Instance.SeriesInfo.Take(2).Select(s => s.ColorClass));
	}

	[Fact]
	public void Other_CanBeHidden_LikeAnySegment()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Title, "Traffic")
			.Add(x => x.MaxSegments, 3)
			.Add(x => x.Segments, Segments(5)));
		string other = cut.Instance.SeriesInfo[^1].Key;

		cut.Render(p => p.Add(x => x.HiddenSeries, [other]));

		Assert.Equal(2, cut.FindAll(".moka-donutchart-segment").Count);
	}

	[Fact]
	public void ZeroOrLess_NeverFolds()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Title, "Traffic")
			.Add(x => x.MaxSegments, 0)
			.Add(x => x.Segments, Segments(12)));

		Assert.Equal(12, cut.FindAll(".moka-donutchart-item").Count);
	}

	[Fact]
	public void TheClassicRing_NeverFolds()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p.Add(x => x.Segments, Segments(12)));

		Assert.True(cut.Instance.IsClassic);
		Assert.Equal(12, cut.FindAll(".moka-donutchart-segment").Count);
	}

	[Fact]
	public void AMaxSegmentsOfItsOwn_MovesItOntoTheFrame()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.MaxSegments, 6)
			.Add(x => x.Segments, Segments(9)));

		Assert.False(cut.Instance.IsClassic);
		Assert.Equal(6, cut.FindAll(".moka-donutchart-segment").Count);
	}
}
