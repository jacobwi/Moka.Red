using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     The radar chart: a spoke, a wedge and a label per axis, a shape per series with a dot per value, shared and per
///     axis scales, reversed axes, gaps, the rings, what pointing at an axis reads, the table, and its name.
/// </summary>
public sealed class MokaRadarChartTests : BunitContext
{
	private static readonly MokaRadarAxis[] Four = [new("Speed"), new("Range"), new("Comfort"), new("Price")];

	private static readonly MokaRadarSeries[] Two =
	[
		new("Model A", [10, 5, 10, 5]),
		new("Model B", [5, 10, 5, 10])
	];

	public MokaRadarChartTests() => this.Static();

	[Fact]
	public void EachAxis_HasASpokeAWedgeAndALabel_CarryingItsPlace()
	{
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p.Add(x => x.Axes, Four).Add(x => x.Series, Two));

		Assert.Equal(["0", "1", "2", "3"], cut.FindAll(".moka-radarchart__spoke").Select(e => e.GetAttribute("data-i")));
		Assert.Equal(["0", "1", "2", "3"], cut.FindAll(".moka-radarchart__wedge").Select(e => e.GetAttribute("data-i")));
		Assert.Equal(["Speed", "Range", "Comfort", "Price"], cut.FindAll(".moka-radarchart__label").Select(e => e.TextContent));
		Assert.Equal(["0", "1", "2", "3"], cut.FindAll(".moka-radarchart__label").Select(e => e.GetAttribute("data-i")));
	}

	[Fact]
	public void TheAxes_RunClockwiseFromTwelve_AndTheirLabelsSitOutsideThem()
	{
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p.Add(x => x.Axes, Four).Add(x => x.Series, Two));

		IReadOnlyList<IElement> spokes = cut.FindAll(".moka-radarchart__spoke");
		Assert.Equal(("100", "0"), (spokes[0].GetAttribute("x2"), spokes[0].GetAttribute("y2")));
		Assert.Equal(("200", "100"), (spokes[1].GetAttribute("x2"), spokes[1].GetAttribute("y2")));
		IReadOnlyList<IElement> labels = cut.FindAll(".moka-radarchart__label");
		Assert.Contains("moka-radarchart__label--above", labels[0].ClassList);
		Assert.Contains("moka-radarchart__label--center", labels[0].ClassList);
		Assert.Contains("moka-radarchart__label--start", labels[1].ClassList);
		Assert.Contains("moka-radarchart__label--below", labels[2].ClassList);
		Assert.Contains("moka-radarchart__label--end", labels[3].ClassList);
		Assert.Equal("--_x: 1; --_y: 0", labels[1].GetAttribute("style"));
	}

	[Fact]
	public void OnASharedScale_EachShapeReachesItsValues()
	{
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p.Add(x => x.Axes, Four).Add(x => x.Series, Two));

		IReadOnlyList<IElement> lines = cut.FindAll(".moka-radarchart__line");
		Assert.Equal("M100 0L150 100L100 200L50 100Z", lines[0].GetAttribute("d"));
		Assert.Equal("M100 50L200 100L100 150L0 100Z", lines[1].GetAttribute("d"));
		Assert.Equal(lines[0].GetAttribute("d"), cut.Find(".moka-radarchart__area").GetAttribute("d"));
	}

	[Fact]
	public void EachValue_HasADotThatCarriesItsAxis()
	{
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p.Add(x => x.Axes, Four).Add(x => x.Series, Two));

		IElement first = cut.FindAll(".moka-radarchart__series")[0];
		Assert.Equal(["M100 0h0", "M150 100h0", "M100 200h0", "M50 100h0"],
			first.QuerySelectorAll(".moka-radarchart__dot").Select(e => e.GetAttribute("d")));
		Assert.Equal(["0", "1", "2", "3"], first.QuerySelectorAll(".moka-radarchart__dot").Select(e => e.GetAttribute("data-i")));
		Assert.Equal("Model A", first.GetAttribute("data-moka-key"));
		Assert.Contains("moka-chart-c1", first.ClassList);
		Assert.Contains("moka-chart-c2", cut.FindAll(".moka-radarchart__series")[1].ClassList);
	}

	[Fact]
	public void TheSharedScale_WritesItsValuesUpTheTopAxis_AndRingsFallOnThem()
	{
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p
			.Add(x => x.Axes, Four)
			.Add(x => x.Series, Two)
			.Add(x => x.Max, 10));

		Assert.Equal(["2.5", "5.0", "7.5", "10.0"], cut.FindAll(".moka-radarchart__tick").Select(e => e.TextContent));
		Assert.Equal("--_s: 0.25", cut.Find(".moka-radarchart__tick").GetAttribute("style"));
		Assert.Equal(4, cut.FindAll(".moka-radarchart__ring").Count);
		Assert.Equal("M100 0L200 100L100 200L0 100Z", cut.FindAll(".moka-radarchart__ring")[^1].GetAttribute("d"));
	}

	[Fact]
	public void PerAxis_EachAxisTakesItsOwnScale_AndRingsMarkEqualParts()
	{
		MokaRadarAxis[] axes = [new("Score") { Max = 100 }, new("Latency") { Max = 400 }, new("Cost") { Max = 20 }];
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p
			.Add(x => x.Axes, axes)
			.Add(x => x.Series, [new MokaRadarSeries("A", [50, 400, 10])])
			.Add(x => x.Scale, MokaRadarScale.PerAxis)
			.Add(x => x.GridShape, MokaRadarGridShape.Circle));

		Assert.Equal("M100 50L186.6 150L56.7 125Z", cut.Find(".moka-radarchart__line").GetAttribute("d"));
		Assert.Equal(["25", "50", "75", "100"], cut.FindAll("circle.moka-radarchart__ring").Select(e => e.GetAttribute("r")));
		Assert.Empty(cut.FindAll(".moka-radarchart__tick"));
	}

	[Fact]
	public void AReversedAxis_PutsItsHighestValueAtTheCenter()
	{
		MokaRadarAxis[] axes = [new("Price") { Reversed = true }, new("Range"), new("Comfort")];
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p
			.Add(x => x.Axes, axes)
			.Add(x => x.Series, [new MokaRadarSeries("A", [10, 10, 10]), new MokaRadarSeries("B", [0, 0, 0])])
			.Add(x => x.Max, 10));

		IReadOnlyList<IElement> lines = cut.FindAll(".moka-radarchart__line");
		Assert.StartsWith("M100 100L", lines[0].GetAttribute("d"), StringComparison.Ordinal);
		Assert.StartsWith("M100 0L", lines[1].GetAttribute("d"), StringComparison.Ordinal);
	}

	[Fact]
	public void AMissingValue_BreaksTheOutline_AndTheShapeIsntFilled()
	{
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p
			.Add(x => x.Axes, Four)
			.Add(x => x.Series, [new MokaRadarSeries("A", [10, double.NaN, 10, 5])])
			.Add(x => x.Max, 10));

		Assert.Equal("M100 200L50 100L100 0", cut.Find(".moka-radarchart__line").GetAttribute("d"));
		Assert.Empty(cut.FindAll(".moka-radarchart__area"));
		Assert.Equal(3, cut.FindAll(".moka-radarchart__dot").Count);
	}

	[Fact]
	public void AreaNone_DrawsOutlinesOnly()
	{
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p
			.Add(x => x.Axes, Four)
			.Add(x => x.Series, Two)
			.Add(x => x.Area, MokaChartArea.None));

		Assert.Empty(cut.FindAll(".moka-radarchart__area"));
		Assert.Equal(2, cut.FindAll(".moka-radarchart__line").Count);
	}

	[Fact]
	public void FewerThanThreeAxes_DrawNothing_AndTheFrameSaysSo()
	{
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p
			.Add(x => x.Axes, [new MokaRadarAxis("A"), new MokaRadarAxis("B")])
			.Add(x => x.Series, [new MokaRadarSeries("S", [1, 2])]));

		Assert.False(cut.Instance.HasData);
		Assert.Empty(cut.FindAll(".moka-radarchart__line"));
		Assert.Contains("No data", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void PointingAtAnAxis_ReadsEverySeriesOnIt_InItsOwnFormat()
	{
		MokaRadarAxis[] axes = [new("Speed"), new("Range") { Formatter = Km }, new("Comfort")];
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p
			.Add(x => x.Axes, axes)
			.Add(x => x.Series, [new MokaRadarSeries("A", [1, 420, 3]), new MokaRadarSeries("B", [2, double.NaN, 4])]));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 1, null)!;
		Assert.Equal("Range", info.Heading);
		Assert.Equal(["A 420 km", "B No data"], info.Rows.Select(row => $"{row.Name} {row.ValueText}"));
		Assert.Equal("Range, A 420 km, B No data", info.CellText);
		Assert.Equal(["A", "B"], info.Context.Points.Select(point => point.SeriesName));
		Assert.Equal("Range", info.Context.Points[0].Category);
		Assert.Null(cut.Instance.DescribeActive(0, -1, 3, null));
	}

	[Fact]
	public void TheTable_HasARowPerAxis_AndAColumnPerSeries()
	{
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p
			.Add(x => x.Axes, Four)
			.Add(x => x.Series, [new MokaRadarSeries("A", [1.5, 2, double.NaN, 4])]));

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(["Axis", "A"], table.Headers);
		Assert.Equal(4, table.RowCount);
		Assert.Equal("Speed", table.Display(0, 0));
		Assert.Equal("1.5", table.Raw(0, 1));
		Assert.Equal(string.Empty, table.Raw(2, 1));
	}

	[Fact]
	public void HiddenSeries_Leave_AndTheRestKeepTheirColors()
	{
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p
			.Add(x => x.Axes, Four)
			.Add(x => x.Series, Two)
			.Add(x => x.HiddenSeries, ["Model A"]));

		IElement shown = Assert.Single(cut.FindAll(".moka-radarchart__series"));
		Assert.Contains("moka-chart-c2", shown.ClassList);
		Assert.True(cut.Instance.SeriesInfo[0].Hidden);
	}

	[Fact]
	public void ItsName_ListsTheSeries_InTheChartsOwnWords()
	{
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p
			.Add(x => x.Axes, Four)
			.Add(x => x.Series, Two)
			.Add(x => x.Text, MokaChartText.Default with { RadarChart = "Netzdiagramm" }));

		Assert.Equal("Netzdiagramm: Model A, Model B", cut.Find(".moka-radarchart").GetAttribute("aria-label"));
	}

	[Fact]
	public void OnTheStandardLook_EachAxisCarriesTheBrowsersTooltip_AndEveryDotShows()
	{
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p.Add(x => x.Axes, Four).Add(x => x.Series, Two));

		Assert.Equal("Speed: Model A 10, Model B 5", cut.Find(".moka-radarchart__wedge title").TextContent);
		Assert.Contains("moka-radarchart--points-always", cut.Find(".moka-radarchart").ClassList);
		Assert.Equal("img", cut.Find(".moka-radarchart").GetAttribute("role"));
	}

	[Fact]
	public void OnTheQuietLook_TheChartBindsItsScript_AsAChartOfParts()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p
			.Add(x => x.Axes, Four)
			.Add(x => x.Series, Two)
			.Add(x => x.Look, MokaChartLook.Quiet));

		JSRuntimeInvocation bind = Assert.Single(module.Invocations, call => call.Identifier == "bind");
		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(bind.Arguments[2]);
		Assert.Equal(true, options["hit"]);
		Assert.Equal("column", options["mode"]);
		Assert.Empty(cut.FindAll(".moka-radarchart__wedge title"));
		Assert.DoesNotContain("moka-radarchart--points-always", cut.Find(".moka-radarchart").ClassList);
		Assert.Equal("figure", cut.Find(".moka-radarchart").GetAttribute("role"));
		Assert.Equal("Speed, Range, Comfort, Price".Split(", "), cut.FindAll(".moka-radarchart__label").Select(e => e.TextContent));
		Assert.Contains("Left and Right move between axes.", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void WithTexture_EachFillIsHatchedAtItsOwnAngle()
	{
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p
			.Add(x => x.Axes, Four)
			.Add(x => x.Series, Two)
			.AddCascadingValue(MokaTheme.Gate)
			.Add(x => x.Texture, true));

		IReadOnlyList<IElement> patterns = cut.FindAll("pattern");
		Assert.Equal(["rotate(45)", "rotate(135)"], patterns.Select(p => p.GetAttribute("patternTransform")));
		Assert.StartsWith("fill: url(#", cut.Find(".moka-radarchart__area").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void TheSameValues_InANewList_DrawNothing_AndAValueChangedInPlaceDrawsAgain()
	{
		double[] values = [10, 5, 10, 5];
		MokaRadarSeries[] series = [new("A", values)];
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p.Add(x => x.Axes, Four).Add(x => x.Series, series));
		long version = cut.Instance.GeometryVersion;

		cut.Render(p => p.Add(x => x.Axes, [.. Four]).Add(x => x.Series, [new MokaRadarSeries("A", [10, 5, 10, 5])]));
		Assert.Equal(version, cut.Instance.GeometryVersion);

		values[1] = 10;
		cut.Render(p => p.Add(x => x.Axes, Four).Add(x => x.Series, series));
		Assert.Equal(version + 1, cut.Instance.GeometryVersion);
		Assert.Equal("M100 0L200 100L100 200L50 100Z", cut.Find(".moka-radarchart__line").GetAttribute("d"));
	}

	[Fact]
	public void TheGridStyle_DecidesTheRings()
	{
		IRenderedComponent<MokaRadarChart> baseline = Render<MokaRadarChart>(p => p
			.Add(x => x.Axes, Four)
			.Add(x => x.Series, Two)
			.Add(x => x.Grid, MokaChartGrid.Baseline));
		IRenderedComponent<MokaRadarChart> none = Render<MokaRadarChart>(p => p
			.Add(x => x.Axes, Four)
			.Add(x => x.Series, Two)
			.Add(x => x.Grid, MokaChartGrid.None));

		Assert.Single(baseline.FindAll(".moka-radarchart__ring"));
		Assert.Empty(baseline.FindAll(".moka-radarchart__tick"));
		Assert.Empty(none.FindAll(".moka-radarchart__ring"));
		Assert.Equal(4, none.FindAll(".moka-radarchart__spoke").Count);
	}

	[Fact]
	public void SideLabels_KeepRoomForTheLongestName()
	{
		MokaRadarAxis[] axes = [new("Top"), new("A rather long axis name"), new("Bottom"), new("Left")];
		IRenderedComponent<MokaRadarChart> cut = Render<MokaRadarChart>(p => p
			.Add(x => x.Axes, axes)
			.Add(x => x.Series, [new MokaRadarSeries("S", [1, 2, 3, 4])]));

		Assert.Contains("--_radar-side: 9em", cut.Find(".moka-radarchart").GetAttribute("style"), StringComparison.Ordinal);
	}

	private static string Km(double value) => $"{value:0} km";
}
