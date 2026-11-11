using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using Moka.Red.Data.Heatmap;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Charts;

public sealed class MokaHeatmapGridTests : BunitContext
{
	private const string Css = "MokaHeatmap.razor.css";

	private static readonly IReadOnlyList<IReadOnlyList<double>> Week =
		[.. Enumerable.Range(0, 7).Select(d => (IReadOnlyList<double>)[.. Enumerable.Range(0, 24).Select(h => (double)(d * 24 + h))])];

	[Fact]
	public void HourByWeekday_CountsByDayAndHour_InTheZoneGiven()
	{
		TimeZoneInfo plusTwo = TimeZoneInfo.CreateCustomTimeZone("plus-two", TimeSpan.FromHours(2), "plus-two", "plus-two");
		DateTimeOffset sundayLate = new(2026, 11, 8, 23, 30, 0, TimeSpan.Zero);

		IReadOnlyList<IReadOnlyList<double>> grid = MokaHeatmap.HourByWeekday([sundayLate, sundayLate.AddMinutes(10)], plusTwo);

		Assert.Equal(7, grid.Count);
		Assert.All(grid, row => Assert.Equal(24, row.Count));
		Assert.Equal(2, grid[0][1]);
		Assert.Equal(2, grid.Sum(row => row.Sum()));
	}

	[Fact]
	public void TheNameHelpers_StartOnMonday_AndWriteTwoDigitHours()
	{
		Assert.Equal(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"], MokaHeatmap.WeekdayNames(CultureInfo.InvariantCulture));
		Assert.Equal("00", MokaHeatmap.HourNames[0]);
		Assert.Equal("23", MokaHeatmap.HourNames[^1]);
	}

	[Fact]
	public void Grid_DrawsACellPerValue_InFiveSteps()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.GridValues, Week)
			.Add(x => x.RowLabels, MokaHeatmap.WeekdayNames(CultureInfo.InvariantCulture))
			.Add(x => x.ColumnLabels, MokaHeatmap.HourNames));

		IReadOnlyList<IElement> cells = cut.FindAll(".moka-heatmap-cell");
		Assert.Equal(168, cells.Count);
		Assert.Equal("moka-heatmap-cell moka-heatmap-cell--s0", cells[0].ClassName);
		Assert.Equal("moka-heatmap-cell moka-heatmap-cell--s5", cells[^1].ClassName);
		Assert.Equal("--r: 0; --c: 1", cells[1].GetAttribute("style"));
		Assert.Equal("Mon 01: 1", cells[1].GetAttribute("title"));
		Assert.Contains("moka-heatmap--grid", cut.Find(".moka-heatmap").ClassList);
	}

	[Fact]
	public void Grid_CellText_WritesTheTooltip_AndNaNIsUnknown()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.GridValues, [[1d, double.NaN]])
			.Add(x => x.CellText, (row, column, value) => $"{row}/{column}={value}"));

		IReadOnlyList<IElement> cells = cut.FindAll(".moka-heatmap-cell");
		Assert.Equal("0/0=1", cells[0].GetAttribute("title"));
		Assert.Contains("moka-heatmap-cell--unknown", cells[1].ClassList);
	}

	[Fact]
	public void Grid_TheAccentRamp_KeepsTodaysFourSteps()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.Ramp, MokaHeatmapRamp.Accent)
			.Add(x => x.GridValues, [[1d, 2, 3, 4]]));

		Assert.Equal(["l1", "l2", "l3", "l4"], cut.FindAll(".moka-heatmap-cell").Select(c => c.ClassList[1]!.Replace("moka-heatmap-cell--", "", StringComparison.Ordinal)));
	}

	[Fact]
	public void Grid_WiderThanTall_TurnsOnItsSideBelowTheWidthItNeeds()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.GridValues, Week)
			.Add(x => x.RowLabels, MokaHeatmap.WeekdayNames(CultureInfo.InvariantCulture)));

		Assert.Contains("moka-heatmap--flip-w24", cut.Find(".moka-heatmap").ClassList);
		Assert.Contains("--_hm-cols: 24", cut.Find(".moka-heatmap").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void Grid_TallerThanWide_NeverTurns()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.GridValues, [[1d], [2d], [3d]]));

		Assert.DoesNotContain(cut.Find(".moka-heatmap").ClassList, c => c.StartsWith("moka-heatmap--flip", StringComparison.Ordinal));
	}

	[Fact]
	public void Grid_HourNames_KeepEveryThirdHour_EvenlySpaced()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.GridValues, Week)
			.Add(x => x.ColumnLabels, MokaHeatmap.HourNames));

		IReadOnlyList<IElement> columns = cut.FindAll(".moka-heatmap__col");
		Assert.Equal("moka-heatmap__col", columns[0].ClassName);
		Assert.Equal("moka-heatmap__col moka-heatmap__col--hide moka-heatmap__col--minor", columns[1].ClassName);
		Assert.Equal(columns[1].ClassName, columns[2].ClassName);
		Assert.Equal(["00", "03", "06", "09", "12", "15", "18", "21"], columns.Where(c => !c.ClassList.Contains("moka-heatmap__col--minor")).Select(c => c.TextContent));
	}

	[Fact]
	public void Grid_LongColumnNames_ThinByContainerQuery()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.GridValues, [[.. Enumerable.Range(0, 12).Select(i => (double)i)]])
			.Add(x => x.ColumnLabels, ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]));

		IReadOnlyList<IElement> columns = cut.FindAll(".moka-heatmap__col");
		Assert.Contains(columns[1].ClassList, c => c.StartsWith("moka-heatmap__col--w", StringComparison.Ordinal) || c == "moka-heatmap__col--hide");
		Assert.DoesNotContain(columns[0].ClassList, c => c.StartsWith("moka-heatmap__col--w", StringComparison.Ordinal));
	}

	[Fact]
	public void ShowScale_DrawsAKeyFromLessToMore()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.ShowScale, true)
			.Add(x => x.GridValues, Week));

		IElement scale = cut.Find(".moka-heatmap__scale");
		Assert.Equal(5, scale.QuerySelectorAll(".moka-heatmap-cell").Length);
		Assert.StartsWith("Less", scale.TextContent.Trim(), StringComparison.Ordinal);
		Assert.EndsWith("More", scale.TextContent.Trim(), StringComparison.Ordinal);
	}

	[Fact]
	public void Calendar_UnderANewLook_StepsThroughFive()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Weeks, 2)
			.Add(x => x.EndDate, new DateTime(2026, 3, 4))
			.Add(x => x.Data, new Dictionary<DateTime, int> { [new DateTime(2026, 3, 4)] = 5, [new DateTime(2026, 3, 3)] = 1 }));

		Assert.Contains(cut.FindAll(".moka-heatmap-cell"), c => c.ClassList.Contains("moka-heatmap-cell--s5"));
		Assert.Contains(cut.FindAll(".moka-heatmap-cell"), c => c.ClassList.Contains("moka-heatmap-cell--s1"));
	}

	[Fact]
	public void Calendar_OnTheOriginalDesign_KeepsTheAccentRamp()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.AddCascadingValue(MokaTheme.Dark)
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Weeks, 2)
			.Add(x => x.EndDate, new DateTime(2026, 3, 4))
			.Add(x => x.Data, new Dictionary<DateTime, int> { [new DateTime(2026, 3, 4)] = 5 }));

		Assert.Contains(cut.FindAll(".moka-heatmap-cell"), c => c.ClassList.Contains("moka-heatmap-cell--l4"));
	}

	[Fact]
	public void TheStepColors_ReadTheChartTokens_WithTheirMatrixValuesBehind()
	{
		Assert.Equal("var(--moka-chart-seq-5, var(--moka-color-primary))",
			ScopedCss.Declarations(Css, ".moka-heatmap-cell--s5")["background"]);
		Assert.Equal("var(--moka-chart-zero, var(--moka-color-outline-variant))",
			ScopedCss.Declarations(Css, ".moka-heatmap-cell--s0")["background"]);
	}
}

public sealed class SmallChartsSpacingTests : BunitContext
{
	private static readonly Dictionary<string, SpacingCase> Cases = new()
	{
		["Treemap"] = new(typeof(MokaTreemap), "left: 0%; top: 0%; width: 100%; height: 100%",
			("Nodes", new[] { new MokaTreemapNode("A", 1) })),
		["Sankey"] = new(typeof(MokaSankey),
			"--_sk-rows: 1; --_sk-start: 2.3em; --_sk-end: 2.3em | left: 0%; top: 0%; height: 100% | left: 100%; top: 0%; height: 100% | --_sk-x: 0%; top: 50% | --_sk-x: 100%; top: 50%",
			("Links", new[] { new MokaSankeyLink("a", "b", 1) })),
		["HeatmapGrid"] = new(typeof(MokaHeatmap), "--_hm-rows: 1; --_hm-cols: 1 | --c: 0 | --r: 0 | --r: 0; --c: 0",
			("Layout", MokaHeatmapLayout.Grid), ("GridValues", new IReadOnlyList<double>[] { new[] { 1d } })),
		["DonutBar"] = new(typeof(MokaDonutChart), "flex-grow: 1",
			("Shape", MokaDonutShape.Bar), ("Segments", new[] { new MokaDonutSegment { Name = "A", Value = 1 } }))
	};

	public SmallChartsSpacingTests() => this.Static();

	public static TheoryData<string> Names => [.. Cases.Keys];

	[Theory]
	[MemberData(nameof(Names))]
	public void SpacingParameters_LandOnTheComponent(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: true));

		spacing.AssertSpacing(cut);
	}

	[Theory]
	[MemberData(nameof(Names))]
	public void Styles_AreUnchanged_WithoutSpacingParameters(string name)
	{
		SpacingCase spacing = Cases[name];
		IRenderedComponent<ContainerFragment> cut = Render(spacing.Render(withSpacing: false));

		Assert.Equal(spacing.DefaultStyles, SpacingCase.Styles(cut));
	}

	[Fact]
	public void MokaSparkline_IsForwardedFromData_ToPrimitives()
	{
		Type? forwarded = Type.GetType("Moka.Red.Data.Chart.MokaSparkline, Moka.Red.Data");

		Assert.Same(typeof(MokaSparkline), forwarded);
		Assert.Equal("Moka.Red.Primitives", typeof(MokaSparkline).Assembly.GetName().Name);
	}
}
