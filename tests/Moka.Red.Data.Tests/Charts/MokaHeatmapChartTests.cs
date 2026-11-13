using System.Buffers.Binary;
using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using Moka.Red.Data.Heatmap;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Charts;

// The heatmap on the shared chart base: today's drawing until something only a chart has is asked for, then the frame,
// cells that carry data-i, the chart's own tooltip, the keyboard's grid by row and column, the callbacks and the table.
public sealed class MokaHeatmapChartTests : BunitContext
{
	private static readonly DateTime Wednesday = new(2026, 3, 4);

	private static readonly IReadOnlyList<IReadOnlyList<double>> Small =
	[
		[1d, 2, 3],
		[4d, double.NaN, 6]
	];

	public MokaHeatmapChartTests()
	{
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
	}

	public static TheoryData<string> ChartParameters =>
	[
		"Title", "Subtitle", "Tooltip", "TooltipNone", "TooltipTemplate", "Keyboard", "Pinnable", "SyncGroup", "OnPointClick",
		"ActiveChanged", "Controls", "View", "State", "EmptyText"
	];

	[Theory]
	[MemberData(nameof(ChartParameters))]
	public void WhatOnlyAChartHas_MovesItIntoTheFrame(string name)
	{
		this.StaticFrame();

		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Chart(Grid(p), name));

		Assert.False(cut.Instance.IsClassic);
		Assert.Contains("moka-heatmap--chart", cut.Find(".moka-heatmap").ClassList);
		Assert.NotEmpty(cut.FindAll(".moka-chart-frame"));
	}

	// An app may set WithCharts(Quiet) for the whole app: the look has always picked the heatmap's ramp, so it keeps today's
	// drawing, and so do Formatter, Text and DataVersion, which change no markup the old drawing has.
	[Fact]
	public void TheLookAndTheWords_KeepTodaysDrawing()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p)
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Text, MokaChartText.Default with { Value = "Count" })
			.Add(x => x.DataVersion, 3)
			.AddCascadingValue(MokaTheme.Red.WithCharts(MokaChartLook.Quiet)));

		Assert.True(cut.Instance.IsClassic);
		Assert.Empty(cut.FindAll(".moka-chart-frame"));
		Assert.Empty(cut.FindAll("[data-i]"));
		Assert.Equal("Mon 1: 1", cut.FindAll(".moka-heatmap-cell")[0].GetAttribute("title"));
	}

	[Fact]
	public void AFormatter_WritesTheValues_OnTodaysDrawingToo()
	{
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p).Add(x => x.Formatter, Minutes));

		Assert.True(cut.Instance.IsClassic);
		Assert.Equal("Mon 1: 1 min", cut.FindAll(".moka-heatmap-cell")[0].GetAttribute("title"));
	}

	// A listener takes it into the frame and only the default brings it back (gotcha 52).
	[Fact]
	public void TakingTheListenerOff_BringsTodaysDrawingBack()
	{
		this.StaticFrame();
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p).Add(x => x.OnPointClick, _ => { }));
		Assert.False(cut.Instance.IsClassic);

		cut.Render(p => Grid(p).Add(x => x.OnPointClick, default(EventCallback<MokaChartPoint>)));

		Assert.True(cut.Instance.IsClassic);
		Assert.Empty(cut.FindAll(".moka-chart-frame"));
	}

	[Fact]
	public void InTheFrame_GridCellsCarryTheirPlace_RowAfterRow_AndNoTitle()
	{
		this.StaticFrame();

		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p)
			.Add(x => x.Title, "Week")
			.Add(x => x.Tooltip, MokaChartTooltip.Auto));

		IReadOnlyList<IElement> cells = cut.FindAll(".moka-heatmap__matrix .moka-heatmap-cell");
		Assert.Equal(["0", "1", "2", "3", "4", "5"], cells.Select(cell => cell.GetAttribute("data-i")));
		Assert.All(cells, cell => Assert.Null(cell.GetAttribute("title")));
		Assert.Contains("moka-heatmap-cell--unknown", cells[4].ClassList);
		Assert.Contains("moka-heatmap--grid", cut.Find(".moka-heatmap__view").ClassList);
		Assert.Equal("--_hm-rows: 2; --_hm-cols: 3", cut.Find(".moka-heatmap__view").GetAttribute("style"));
	}

	// With the browser's tooltips the cells keep their titles in the frame, as interactive bars do.
	[Fact]
	public void InTheFrame_NativeTooltips_KeepTheTitles()
	{
		this.StaticFrame();

		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p).Add(x => x.Title, "Week"));

		Assert.Equal("Mon 1: 1", cut.FindAll(".moka-heatmap__matrix .moka-heatmap-cell")[0].GetAttribute("title"));
	}

	[Fact]
	public void InTheFrame_TheCalendarsDaysCarryTheirPlace_AndTheDaysToComeNone()
	{
		this.StaticFrame();

		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Calendar(p).Add(x => x.Title, "Commits"));

		IReadOnlyList<IElement> cells = cut.FindAll(".moka-heatmap-grid .moka-heatmap-cell");
		Assert.Equal(14, cells.Count);
		Assert.Equal([.. Enumerable.Range(0, 10).Select(i => i.ToString(CultureInfo.InvariantCulture))],
			cells.Take(10).Select(cell => cell.GetAttribute("data-i")));
		Assert.All(cells.Skip(10), cell => Assert.Null(cell.GetAttribute("data-i")));
		Assert.Equal(10, cut.Instance.DrawnCount(0));
	}

	// The month names sit over their weeks in the frame, so one with no second week before the next name or the end is
	// left out; today's drawing keeps every name.
	[Fact]
	public void InTheFrame_AMonthsNameNeedsTwoWeeks()
	{
		this.StaticFrame();
		IRenderedComponent<MokaHeatmap> chart = Render<MokaHeatmap>(p => p
			.Add(x => x.Weeks, 9)
			.Add(x => x.EndDate, Wednesday)
			.Add(x => x.Title, "Commits"));
		IRenderedComponent<MokaHeatmap> today = Render<MokaHeatmap>(p => p.Add(x => x.Weeks, 9).Add(x => x.EndDate, Wednesday));

		Assert.Equal(["Jan", "", "", "", "Feb", "", "", "", ""], chart.FindAll(".moka-heatmap-month").Select(month => month.TextContent));
		Assert.Equal(["Jan", "", "", "", "Feb", "", "", "", "Mar"], today.FindAll(".moka-heatmap-month").Select(month => month.TextContent));
		Assert.Equal("0", ScopedCss.Declarations("MokaHeatmap.razor.css", ".moka-heatmap__view--calendar .moka-heatmap-month")["width"]);
	}

	[Fact]
	public void TheSurface_TellsTheScriptHowTheCellsAreNumbered()
	{
		this.InteractiveFrame();

		IRenderedComponent<MokaHeatmap> grid = Render<MokaHeatmap>(p => Grid(p).Add(x => x.Keyboard, true));
		IRenderedComponent<MokaHeatmap> calendar = Render<MokaHeatmap>(p => Calendar(p).Add(x => x.Keyboard, true));

		IElement cells = grid.Find(".moka-chart-surface");
		Assert.Equal(("rows", "2", "3", false), (cells.GetAttribute("data-moka-order"), cells.GetAttribute("data-moka-rows"),
			cells.GetAttribute("data-moka-cols"), cells.HasAttribute("data-moka-flow")));
		Assert.Contains("moka-chart-surface--hit", cells.ClassList);
		Assert.Contains("moka-heatmap__view", cells.ParentElement!.ClassList);
		IElement days = calendar.Find(".moka-chart-surface");
		Assert.Equal(("columns", "7", "2", true), (days.GetAttribute("data-moka-order"), days.GetAttribute("data-moka-rows"),
			days.GetAttribute("data-moka-cols"), days.HasAttribute("data-moka-flow")));
	}

	[Fact]
	public void TheKeyboardGrid_SaysWhichRowAndColumnTheCellIsIn()
	{
		this.InteractiveFrame();
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p).Add(x => x.Keyboard, true));

		cut.InvokeAsync(() => cut.Instance.OnActive(0, -1, 5, true));

		IElement grid = cut.Find(".moka-chart-surface__grid");
		Assert.Equal(("2", "3"), (grid.GetAttribute("aria-rowcount"), grid.GetAttribute("aria-colcount")));
		Assert.EndsWith(cut.Instance.Words.CellKeys, grid.GetAttribute("aria-label"), StringComparison.Ordinal);
		IElement cell = cut.Find("[role=gridcell]");
		Assert.Equal("2", cell.ParentElement!.GetAttribute("aria-rowindex"));
		Assert.Equal("3", cell.GetAttribute("aria-colindex"));
		Assert.Equal("Tue 3, 6", cell.TextContent);
	}

	[Fact]
	public void TheTooltip_LeadsWithTheValue_KeyedByTheCellsStep()
	{
		this.InteractiveFrame();
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p).Add(x => x.Tooltip, MokaChartTooltip.Auto));

		cut.InvokeAsync(() => cut.Instance.OnActive(0, -1, 2, false));

		Assert.Equal("Mon 3", cut.Find(".moka-chart-tip__head").TextContent);
		Assert.Equal("3", cut.Find(".moka-chart-tip__value").TextContent);
		Assert.Contains("--moka-chart-seq-3", cut.Find(".moka-chart-tip__key").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void AnUnknownCell_SaysSoInTheTooltip()
	{
		this.InteractiveFrame();
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p)
			.Add(x => x.Tooltip, MokaChartTooltip.Auto)
			.Add(x => x.UnknownText, "Collector down"));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 4, null)!;

		Assert.Equal("Collector down", info.Rows[0].ValueText);
		Assert.Equal("moka-chart-muted", info.Rows[0].ColorClass);
		Assert.True(double.IsNaN(info.Context.Focus!.Value));
		Assert.Contains("moka-heatmap-cell--unknown", cut.FindAll(".moka-heatmap__matrix .moka-heatmap-cell")[4].ClassList);
	}

	[Fact]
	public void CellText_IsANoteUnderTheValue_AndWhatTheKeyboardReads()
	{
		this.StaticFrame();
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p)
			.Add(x => x.Title, "Week")
			.Add(x => x.CellText, (row, column, value) => row == 0 ? $"Hour {column}: {value} min" : null));

		ChartActiveInfo first = cut.Instance.DescribeActive(0, -1, 1, null)!;
		ChartActiveInfo second = cut.Instance.DescribeActive(0, -1, 3, null)!;

		Assert.Equal(["Hour 1: 2 min"], first.Notes);
		Assert.Equal("Hour 1: 2 min", first.CellText);
		Assert.Empty(second.Notes);
		Assert.Equal("Tue 1, 4", second.CellText);
	}

	[Fact]
	public void APin_ShowsEachCellsChangeSinceIt()
	{
		this.StaticFrame();
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p).Add(x => x.Pinnable, true));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 5, 0)!;

		Assert.Equal("+5", info.Rows[0].DeltaText);
		Assert.Equal(5, info.Context.Focus!.SincePin);
	}

	[Fact]
	public void AClick_ReportsTheCellsRowAndColumn()
	{
		MokaChartPoint? clicked = null;
		this.InteractiveFrame();
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p).Add(x => x.OnPointClick, point => clicked = point));

		cut.InvokeAsync(() => cut.Instance.OnSelect(0, -1, 5));

		cut.WaitForAssertion(() => Assert.Equal((1, 2, 6d, "Tue 3"), (clicked?.Row, clicked?.Column, clicked?.Value, clicked?.Category)));
	}

	[Fact]
	public void ACalendarDay_ReportsItsDate_WeekAndWeekday()
	{
		MokaChartPoint? active = null;
		this.InteractiveFrame();
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Calendar(p).Add(x => x.ActiveChanged, point => active = point));

		cut.InvokeAsync(() => cut.Instance.OnActive(0, -1, 9, false));

		cut.WaitForAssertion(() => Assert.NotNull(active));
		Assert.Equal(new DateTimeOffset(Wednesday, TimeSpan.Zero), active!.Time);
		Assert.Equal((2, 1, 9d), (active.Row, active.Column, active.Value));
		Assert.Equal(Wednesday.ToString("D", CultureInfo.InvariantCulture), active.Category);
	}

	[Fact]
	public void AnUnknownDay_SaysSo()
	{
		this.StaticFrame();
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Calendar(p)
			.Add(x => x.Title, "Commits")
			.Add(x => x.Unknown, [Wednesday.AddDays(-1)]));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 8, null)!;

		Assert.Equal("No data", info.Rows[0].ValueText);
		Assert.Contains("moka-heatmap-cell--unknown", cut.FindAll(".moka-heatmap-grid .moka-heatmap-cell")[8].ClassList);
	}

	[Fact]
	public void TheTable_ListsEveryCell_WithItsRowColumnValueAndText()
	{
		this.StaticFrame();
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p)
			.Add(x => x.Title, "Week")
			.Add(x => x.Formatter, Minutes)
			.Add(x => x.CellText, (row, column, _) => $"r{row}c{column}"));

		ChartTableModel table = cut.Instance.TableModel!;

		Assert.Equal(["Row", "Column", "Value", "Detail"], table.Headers);
		Assert.Equal(6, table.RowCount);
		Assert.Equal(["Tue", "2", "No data", "r1c1"], Enumerable.Range(0, 4).Select(column => table.Display(4, column)));
		Assert.Equal(["Tue", "3", "6", "r1c2"], Enumerable.Range(0, 4).Select(column => table.Raw(5, column)));
		Assert.Equal("6 min", table.Display(5, 2));
		Assert.Equal(string.Empty, table.Raw(4, 2));
	}

	[Fact]
	public void TheCalendarsTable_ListsTheDays_WithISODatesForCsv()
	{
		this.StaticFrame();
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Calendar(p).Add(x => x.Title, "Commits"));

		ChartTableModel table = cut.Instance.TableModel!;

		Assert.Equal(["Date", "Value"], table.Headers);
		Assert.Equal(10, table.RowCount);
		Assert.Equal(("2026-03-04", "9"), (table.Raw(9, 0), table.Raw(9, 1)));
		Assert.Equal("03/04/2026", table.Display(9, 0));
	}

	[Fact]
	public void TheTableView_ShowsTheCells()
	{
		this.StaticFrame();
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p).Add(x => x.View, MokaChartView.Table));

		Assert.NotEmpty(cut.FindAll(".moka-chart-frame__table"));
		Assert.Contains("Tue", cut.Find(".moka-chart-frame__table tbody").TextContent, StringComparison.Ordinal);
	}

	// A parent that builds the same grid and lists again every render costs no render.
	[Fact]
	public void TheSameValuesAgain_DrawNothing()
	{
		this.StaticFrame();
		int renders = 0;
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p).Add(x => x.Title, "Week"));
		long version = cut.Instance.GeometryVersion;
		cut.OnMarkupUpdated += (_, _) => renders++;

		cut.Render(p => Grid(p, [[1d, 2, 3], [4d, double.NaN, 6]]).Add(x => x.Title, "Week"));

		Assert.Equal(version, cut.Instance.GeometryVersion);
		Assert.Equal(0, renders);
	}

	[Fact]
	public void AGridChangedInPlace_DrawsAgain()
	{
		this.StaticFrame();
		double[][] values = [[1d, 2], [3d, 4]];
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => p
			.Add(x => x.Layout, MokaHeatmapLayout.Grid)
			.Add(x => x.Title, "Grid")
			.Add(x => x.GridValues, values));
		long version = cut.Instance.GeometryVersion;

		values[1][1] = 40;
		cut.Render(p => p.Add(x => x.GridValues, values));

		Assert.True(cut.Instance.GeometryVersion > version);
	}

	[Fact]
	public void SpacingAndClass_LandOnTheChartsRoot()
	{
		this.StaticFrame();
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p)
			.Add(x => x.Title, "Week")
			.Add(x => x.Margin, MokaSpacingScale.Md)
			.Add(x => x.Class, "mine")
			.Add(x => x.Style, "max-width: 30rem"));

		IElement root = cut.Find(".moka-heatmap");
		Assert.Equal("margin: var(--moka-spacing-md); max-width: 30rem", root.GetAttribute("style"));
		Assert.Contains("mine", root.ClassList);
		Assert.Contains("moka-fill-width", root.ClassList);
		Assert.Equal("Week", cut.Find(".moka-chart-frame__title").TextContent);
	}

	[Fact]
	public void TheChartsLook_OnlyReachesTheNewDesign()
	{
		this.StaticFrame();
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p)
			.Add(x => x.Title, "Week")
			.Add(x => x.Look, MokaChartLook.Bold)
			.AddCascadingValue(MokaTheme.Dark));

		Assert.Contains("moka-chart-look-standard", cut.Find(".moka-heatmap").ClassList);
	}

	[Fact]
	public void TheStylesheet_LiftsTheActiveCell_AndTheTurnedGridSaysSo()
	{
		IReadOnlyDictionary<string, string> on =
			ScopedCss.Declarations("MokaHeatmap.razor.css", ".moka-heatmap__view .moka-heatmap-cell[data-moka-on]");

		Assert.StartsWith("calc(", on["outline"], StringComparison.Ordinal);
		foreach (int width in new[] { 15, 19, 24, 30, 38, 48, 60 })
		{
			Assert.Equal("1", ScopedCss.Declarations("MokaHeatmap.razor.css",
				$".moka-heatmap--flip-w{width.ToString(CultureInfo.InvariantCulture)} .moka-heatmap__matrix")["--moka-chart-transpose"]);
		}
	}

	[Fact]
	public void TheHeatmapsLayout_IsWorkedOutByTheScript_SoNoGeometryIsPulled()
	{
		this.StaticFrame();
		IRenderedComponent<MokaHeatmap> cut = Render<MokaHeatmap>(p => Grid(p).Add(x => x.Keyboard, true));

		Assert.Null(cut.Instance.HoverGeometry(0, 0));
		Assert.Equal((1, 2), cut.Instance.PartLayout(0)!.PlaceOf(5));
	}

	private static string Minutes(double value) => string.Create(CultureInfo.InvariantCulture, $"{value:0} min");

	private static ComponentParameterCollectionBuilder<MokaHeatmap> Grid(ComponentParameterCollectionBuilder<MokaHeatmap> p,
		IReadOnlyList<IReadOnlyList<double>>? values = null) => p
		.Add(x => x.Layout, MokaHeatmapLayout.Grid)
		.Add(x => x.GridValues, values ?? Small)
		.Add(x => x.RowLabels, ["Mon", "Tue"])
		.Add(x => x.ColumnLabels, ["1", "2", "3"]);

	private static ComponentParameterCollectionBuilder<MokaHeatmap> Calendar(ComponentParameterCollectionBuilder<MokaHeatmap> p) => p
		.Add(x => x.Weeks, 2)
		.Add(x => x.EndDate, Wednesday)
		.Add(x => x.Data, new Dictionary<DateTime, int> { [Wednesday] = 9, [Wednesday.AddDays(-2)] = 3 });

	private static ComponentParameterCollectionBuilder<MokaHeatmap> Chart(ComponentParameterCollectionBuilder<MokaHeatmap> p,
		string name) => name switch
	{
		"Title" => p.Add(x => x.Title, "Week"),
		"Subtitle" => p.Add(x => x.Subtitle, "Hours"),
		"Tooltip" => p.Add(x => x.Tooltip, MokaChartTooltip.Auto),
		"TooltipNone" => p.Add(x => x.Tooltip, MokaChartTooltip.None),
		"TooltipTemplate" => p.Add(x => x.TooltipTemplate, (RenderFragment<MokaChartTooltipContext>)(_ => _ => { })),
		"Keyboard" => p.Add(x => x.Keyboard, true),
		"Pinnable" => p.Add(x => x.Pinnable, true),
		"SyncGroup" => p.Add(x => x.SyncGroup, "week"),
		"OnPointClick" => p.Add(x => x.OnPointClick, _ => { }),
		"ActiveChanged" => p.Add(x => x.ActiveChanged, _ => { }),
		"Controls" => p.Add(x => x.Controls, MokaChartControls.View),
		"View" => p.Add(x => x.View, MokaChartView.Table),
		"State" => p.Add(x => x.State, MokaChartState.Loading),
		_ => p.Add(x => x.EmptyText, "Nothing yet")
	};
}
