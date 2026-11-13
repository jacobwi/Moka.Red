using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Components;

// Used space against a quota: values in bytes, which the tooltips wrote as 3221225472, and segments that
// are shares of the quota rather than of what's used, with the free part left as the ring's track.
public class MokaDonutChartTotalTests : BunitContext
{
	private const double GiB = 1024d * 1024 * 1024;

	// The ring's radius is (200 - 20) / 2, so its circumference is 2 * pi * 90.
	private const double Circumference = 2 * Math.PI * 90;

	private static string Gib(double bytes) => $"{(bytes / GiB).ToString("0.#", CultureInfo.InvariantCulture)} GiB";

	[Fact]
	public void Formatter_WritesTheValues_InTheTooltipsAndTheLegend()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Formatter, Gib)
			.Add(x => x.Segments,
			[
				new MokaDonutSegment { Name = "Photos", Value = 3 * GiB },
				new MokaDonutSegment { Name = "Music", Value = 1 * GiB }
			]));

		Assert.Equal(["Photos: 3 GiB (75%)", "Music: 1 GiB (25%)"],
			cut.FindAll(".moka-donutchart-segment title").Select(t => t.TextContent));
		Assert.Equal(["Photos · 3 GiB · 75%", "Music · 1 GiB · 25%"],
			cut.FindAll(".moka-donutchart-legend-item").Select(i => i.TextContent.Trim()));
		Assert.DoesNotContain("3221225472", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void WithoutFormatter_TheTextIsAsItWas()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Segments,
			[
				new MokaDonutSegment { Name = "Direct", Value = 75 },
				new MokaDonutSegment { Name = "Referral", Value = 25 }
			]));

		Assert.Equal(["Direct: 75 (75%)", "Referral: 25 (25%)"],
			cut.FindAll(".moka-donutchart-segment title").Select(t => t.TextContent));
		Assert.Equal(["Direct · 75%", "Referral · 25%"],
			cut.FindAll(".moka-donutchart-legend-item").Select(i => i.TextContent.Trim()));
		Assert.Equal([Dash(0.75), Dash(0.25)], Arcs(cut).Select(a => a.GetAttribute("stroke-dasharray")));
		Assert.Equal(["-0.00", Num(-0.75 * Circumference)], Arcs(cut).Select(a => a.GetAttribute("stroke-dashoffset")));
	}

	[Fact]
	public void Total_MakesEachSegmentAShareOfIt_AndLeavesTheRestAsTrack()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Total, 100d)
			.Add(x => x.Segments,
			[
				new MokaDonutSegment { Name = "Photos", Value = 25 },
				new MokaDonutSegment { Name = "Music", Value = 15 }
			]));

		Assert.Equal([Dash(0.25), Dash(0.15)], Arcs(cut).Select(a => a.GetAttribute("stroke-dasharray")));
		Assert.Equal(["-0.00", Num(-0.25 * Circumference)], Arcs(cut).Select(a => a.GetAttribute("stroke-dashoffset")));
		Assert.Equal(["Photos · 25%", "Music · 15%"], cut.FindAll(".moka-donutchart-legend-item").Select(i => i.TextContent.Trim()));
		Assert.Single(cut.FindAll(".moka-donutchart-track"));
	}

	[Fact]
	public void ASumAboveTotal_FillsTheRing_AndCutsOffWhatRunsPastIt()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Total, 100d)
			.Add(x => x.Segments,
			[
				new MokaDonutSegment { Name = "Photos", Value = 80 },
				new MokaDonutSegment { Name = "Music", Value = 40 },
				new MokaDonutSegment { Name = "Video", Value = 10 }
			]));

		IReadOnlyList<IElement> arcs = Arcs(cut);
		Assert.Equal([Dash(0.8), Dash(0.2)], arcs.Select(a => a.GetAttribute("stroke-dasharray")));
		Assert.Equal(["Photos · 80%", "Music · 40%", "Video · 10%"],
			cut.FindAll(".moka-donutchart-legend-item").Select(i => i.TextContent.Trim()));
		Assert.DoesNotContain("-", arcs[1].GetAttribute("stroke-dasharray"), StringComparison.Ordinal);
		Assert.DoesNotContain("NaN", cut.Markup, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(0d)]
	[InlineData(-5d)]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	public void ATotalThatIsntAWhole_UsesTheSum(double total)
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
			.Add(x => x.Total, total)
			.Add(x => x.Segments,
			[
				new MokaDonutSegment { Name = "Direct", Value = 75 },
				new MokaDonutSegment { Name = "Referral", Value = 25 }
			]));

		Assert.Equal([Dash(0.75), Dash(0.25)], Arcs(cut).Select(a => a.GetAttribute("stroke-dasharray")));
	}

	[Fact]
	public void Total_WithoutSegments_ShowsOnlyTheTrack()
	{
		IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p.Add(x => x.Total, 100d));

		Assert.Single(cut.FindAll(".moka-donutchart-track"));
		Assert.Empty(Arcs(cut));
		Assert.Empty(cut.FindAll(".moka-donutchart-legend"));
	}

	[Fact]
	public void TheArcs_StayInvariant_UnderADecimalCommaCulture()
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("de-DE");
		try
		{
			IRenderedComponent<MokaDonutChart> cut = Render<MokaDonutChart>(p => p
				.Add(x => x.Total, 3d)
				.Add(x => x.Segments, [new MokaDonutSegment { Name = "Used", Value = 1 }]));

			Assert.DoesNotContain(',', Arcs(cut)[0].GetAttribute("stroke-dasharray")!);
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	private static IReadOnlyList<IElement> Arcs(IRenderedComponent<MokaDonutChart> cut) =>
		cut.FindAll(".moka-donutchart-segment");

	private static string Dash(double share) =>
		$"{Num(share * Circumference)} {Num(Circumference - share * Circumference)}";

	private static string Num(double value) => value.ToString("F2", CultureInfo.InvariantCulture);
}
