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
			.Add(x => x.CellText, (row, column, value) => string.Create(CultureInfo.InvariantCulture, $"{row}/{column}={value}")));

		IReadOnlyList<IElement> cells = cut.FindAll(".moka-heatmap-cell");
		Assert.Equal("0/0=1", cells[0].GetAttribute("title"));
		Assert.Contains("moka-heatmap-cell--unknown", cells[1].ClassList);
	}

	// A presence grid says "later today" for the hours still to come, which only CellText knows how to word.
	[Fact]
	public void Grid_CellText_IsAskedForUnknownCellsToo()
	{
		List<(int Row, int Column, double Value)> asked = [];
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.GridValues, [[1d, double.NaN]])
			.Add(x => x.CellText, (row, column, value) =>
			{
				asked.Add((row, column, value));
				return double.IsNaN(value) ? "Today 3 PM: later today" : null;
			}));

		IReadOnlyList<IElement> cells = cut.FindAll(".moka-heatmap-cell");
		Assert.Contains((0, 1, double.NaN), asked);
		Assert.Equal("Today 3 PM: later today", cells[1].GetAttribute("title"));
		Assert.Equal("1, 1: 1", cells[0].GetAttribute("title"));
	}

	[Fact]
	public void Grid_ANullAnswer_KeepsTheUnknownCellsOwnText()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.GridValues, [[double.NaN]])
			.Add(x => x.RowLabels, ["Mon"])
			.Add(x => x.ColumnLabels, ["09"])
			.Add(x => x.UnknownText, "later today")
			.Add(x => x.CellText, (_, _, _) => null));

		Assert.Equal("Mon 09: later today", cut.Find(".moka-heatmap-cell").GetAttribute("title"));
	}

	[Fact]
	public void Grid_ByDefault_CellsGrowFromTenToEighteenPixels()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.GridValues, Week));

		Assert.Equal("max-content repeat(var(--_hm-cols), minmax(var(--_hm-cell-min, 0.625rem), var(--_hm-cell-max, 1.125rem)))",
			ScopedCss.Declarations(Css, ".moka-heatmap__matrix")["grid-template-columns"]);
		Assert.DoesNotContain("--_hm-cell", cut.Find(".moka-heatmap").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void Fill_SharesTheWholeWidth_FlatAndOnItsSide()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.Fill, true)
			.Add(x => x.CellSize, "2rem")
			.Add(x => x.GridValues, Week));

		Assert.Equal("--_hm-rows: 7; --_hm-cols: 24; --_hm-cell-max: 1fr; --_hm-flip-max: 1fr", cut.Find(".moka-heatmap").GetAttribute("style"));
		Assert.Contains("minmax(1.75rem, var(--_hm-flip-max, 2.5rem))",
			ScopedCss.Declarations(Css, ".moka-heatmap--flip-w24 .moka-heatmap__matrix")["grid-template-columns"], StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("1.5rem", "--_hm-cell-max: 1.5rem; --_hm-cell-min: min(0.625rem, 1.5rem)")]
	[InlineData("24", "--_hm-cell-max: 24px; --_hm-cell-min: min(0.625rem, 24px)")]
	[InlineData("var(--cell)", "--_hm-cell-max: var(--cell); --_hm-cell-min: min(0.625rem, var(--cell))")]
	[InlineData("1rem; color: red", null)]
	[InlineData("calc(100% - 1rem)", null)]
	public void CellSize_SetsTheLargestCell(string size, string? expected)
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.CellSize, size)
			.Add(x => x.GridValues, [[1d]]));

		string style = cut.Find(".moka-heatmap").GetAttribute("style")!;
		Assert.Equal(expected is null ? "--_hm-rows: 1; --_hm-cols: 1" : $"--_hm-rows: 1; --_hm-cols: 1; {expected}", style);
	}

	// A month name is too long for an 18 px cell, so it showed only every third month. Larger cells make room for every
	// one, once the grid is wide enough; a size the component can't measure keeps them as they were.
	[Theory]
	[InlineData(null, false, "moka-heatmap__col--hide")]
	[InlineData("2rem", false, "moka-heatmap__col--w24")]
	[InlineData("32px", false, "moka-heatmap__col--w24")]
	[InlineData("1.5rem", false, "moka-heatmap__col--hide")]
	[InlineData("var(--cell)", false, "moka-heatmap__col--hide")]
	[InlineData(null, true, "moka-heatmap__col--w24")]
	public void ColumnNames_ShowWhereTheCellsCanGrowToFitThem(string? size, bool fill, string expected)
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.CellSize, size)
			.Add(x => x.Fill, fill)
			.Add(x => x.GridValues, [[.. Enumerable.Range(0, 12).Select(i => (double)i)]])
			.Add(x => x.ColumnLabels, ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]));

		Assert.Contains(expected, cut.FindAll(".moka-heatmap__col")[1].ClassList);
	}

	[Fact]
	public void ColumnNames_TooLongForTheWidestQuery_NeverShow()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.Fill, true)
			.Add(x => x.GridValues, [[.. Enumerable.Range(0, 40).Select(i => (double)i)]])
			.Add(x => x.ColumnLabels, [.. Enumerable.Range(0, 40).Select(i => $"A rather long name {i}")]));

		IReadOnlyList<IElement> columns = cut.FindAll(".moka-heatmap__col");
		Assert.Contains("moka-heatmap__col--hide", columns[1].ClassList);
		Assert.Contains("moka-heatmap__col--hide", columns[2].ClassList);
		Assert.Contains("moka-heatmap__col--w95", columns[4].ClassList);
		Assert.Contains("moka-heatmap__col--w60", columns[8].ClassList);
	}

	[Fact]
	public void TheWidthQueries_CoverEveryWidthTheComponentWrites()
	{
		foreach (int width in new[] { 6, 8, 10, 12, 15, 19, 24, 30, 38, 48, 60, 75, 95, 120 })
		{
			Assert.Equal("hidden", ScopedCss.Declarations(Css, $".moka-heatmap__col--w{width.ToString(CultureInfo.InvariantCulture)}")["visibility"]);
		}
	}

	// On its side the row names head columns 1.75rem to 2.5rem wide. They used to stay on one line and run into each
	// other, so an app had to override .moka-heatmap__row to wrap them.
	[Theory]
	[InlineData(15)]
	[InlineData(19)]
	[InlineData(24)]
	[InlineData(30)]
	[InlineData(38)]
	[InlineData(48)]
	[InlineData(60)]
	public void OnItsSide_RowNamesWrapInsideTheirColumn_AndNoLongerStick(int width)
	{
		IReadOnlyDictionary<string, string> row =
			ScopedCss.Declarations(Css, $".moka-heatmap--flip-w{width.ToString(CultureInfo.InvariantCulture)} .moka-heatmap__row");

		Assert.Equal("normal", row["white-space"]);
		Assert.Equal("anywhere", row["overflow-wrap"]);
		Assert.Equal("stretch", row["justify-self"]);
		Assert.Equal("0", row["min-width"]);
		Assert.Equal("static", row["position"]);
	}

	[Fact]
	public void TheTurnBlocks_AreTheSame_ButForTheirWidth()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css)).ReplaceLineEndings("\n");
		List<string> blocks = [];
		foreach (int width in new[] { 15, 19, 24, 30, 38, 48, 60 })
		{
			string name = width.ToString(CultureInfo.InvariantCulture);
			int start = css.IndexOf($"@container moka-heatmap (width < {name}rem) {{\n", StringComparison.Ordinal);
			Assert.True(start >= 0, $"No turn block for {name}rem");
			int end = css.IndexOf("\n}\n", start, StringComparison.Ordinal);
			blocks.Add(css[start..end].Replace($"{name}rem", "Wrem", StringComparison.Ordinal).Replace($"-w{name} ", "-wW ", StringComparison.Ordinal));
		}

		Assert.All(blocks, block => Assert.Equal(blocks[0], block));
	}

	[Fact]
	public void RowFigures_SitBesideTheirRowsName()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.GridValues, Week)
			.Add(x => x.RowLabels, MokaHeatmap.WeekdayNames(CultureInfo.InvariantCulture))
			.Add(x => x.RowFigures, ["3.5 h", "12 h", "0 h", "1 h", "2 h", "4 h", "8 h"]));

		Assert.Contains("moka-heatmap--figures", cut.Find(".moka-heatmap").ClassList);
		IElement monday = cut.FindAll(".moka-heatmap__row")[0];
		Assert.Equal(["moka-heatmap__name", "moka-heatmap__fig"], monday.Children.Select(c => c.ClassName));
		Assert.Equal(("Mon", "3.5 h"), (monday.Children[0].TextContent, monday.Children[1].TextContent));
		Assert.Equal("Mon: 0", cut.Find(".moka-heatmap-cell").GetAttribute("title"));
	}

	[Fact]
	public void WithoutRowFigures_ARowsNameIsItsOnlyText()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.GridValues, Week)
			.Add(x => x.RowLabels, MokaHeatmap.WeekdayNames(CultureInfo.InvariantCulture))
			.Add(x => x.RowFigures, ["", ""]));

		Assert.DoesNotContain("moka-heatmap--figures", cut.Find(".moka-heatmap").ClassList);
		Assert.Empty(cut.FindAll(".moka-heatmap__row")[0].Children);
	}

	[Fact]
	public void RowFigures_CountTowardTheWidthTheGridTurnsAt()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.GridValues, Week)
			.Add(x => x.RowLabels, MokaHeatmap.WeekdayNames(CultureInfo.InvariantCulture))
			.Add(x => x.RowFigures, ["12.5 h"]));

		Assert.Contains("moka-heatmap--flip-w30", cut.Find(".moka-heatmap").ClassList);
		Assert.Equal("column", ScopedCss.Declarations(Css, ".moka-heatmap--flip-w30 .moka-heatmap__matrix")["--_hm-row-flow"]);
	}

	[Fact]
	public void MarkedRowsAndCells_AreMarked()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.GridValues, Week)
			.Add(x => x.MarkedRows, [6])
			.Add(x => x.MarkedCells, [new MokaHeatmapCell(6, 15), new MokaHeatmapCell(9, 0)]));

		IReadOnlyList<IElement> rows = cut.FindAll(".moka-heatmap__row");
		Assert.Equal([6], rows.Select((row, i) => (row, i)).Where(r => r.row.ClassList.Contains("moka-heatmap__row--marked")).Select(r => r.i));
		IElement marked = Assert.Single(cut.FindAll(".moka-heatmap-cell--marked"));
		Assert.Equal("--r: 6; --c: 15", marked.GetAttribute("style"));
		Assert.Contains("var(--moka-color-on-surface)", ScopedCss.Declarations(Css, ".moka-heatmap--grid .moka-heatmap-cell--marked")["box-shadow"], StringComparison.Ordinal);
		Assert.Equal("var(--moka-font-weight-semibold)", ScopedCss.Declarations(Css, ".moka-heatmap__row--marked")["font-weight"]);
	}

	[Theory]
	[InlineData(MokaHeatmapScalePlacement.End, "moka-heatmap__scale")]
	[InlineData(MokaHeatmapScalePlacement.Start, "moka-heatmap__scale moka-heatmap__scale--start")]
	[InlineData(MokaHeatmapScalePlacement.Center, "moka-heatmap__scale moka-heatmap__scale--center")]
	public void ScalePlacement_StartCenterAndEnd_SitUnderTheHeatmap(MokaHeatmapScalePlacement placement, string expected)
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.ShowScale, true)
			.Add(x => x.ScalePlacement, placement)
			.Add(x => x.GridValues, Week));

		IElement scale = cut.Find(".moka-heatmap__scale");
		Assert.Equal(expected, scale.ClassName);
		Assert.Contains("moka-heatmap", scale.ParentElement!.ClassList);
	}

	[Fact]
	public void ScalePlacement_GridEnd_IsARowOfTheGrid_UnderItsCells()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.ShowScale, true)
			.Add(x => x.ScalePlacement, MokaHeatmapScalePlacement.GridEnd)
			.Add(x => x.GridValues, Week));

		IElement scale = cut.Find(".moka-heatmap__scale");
		Assert.Contains("moka-heatmap__matrix", scale.ParentElement!.ClassList);
		Assert.Contains("moka-heatmap__scale--grid", scale.ClassList);
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations(Css, ".moka-heatmap__scale.moka-heatmap__scale--grid");
		Assert.Equal("2 / -1", rule["grid-column"]);
		Assert.Equal("end", rule["justify-self"]);
	}

	[Fact]
	public void ScalePlacement_GridEnd_OnACalendar_IsItsEnd()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.ShowScale, true)
			.Add(x => x.ScalePlacement, MokaHeatmapScalePlacement.GridEnd)
			.Add(x => x.Weeks, 2)
			.Add(x => x.EndDate, new DateTime(2026, 3, 4)));

		Assert.Equal("moka-heatmap__scale", cut.Find(".moka-heatmap__scale").ClassName);
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
