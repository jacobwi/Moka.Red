using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

public sealed class MokaBarChartWaterfallTests : BunitContext
{
	private static readonly string[] Books = ["Start", "Sales", "Refunds", "Gross", "Interest"];

	// A start balance, two changes, a subtotal left to the chart, one more change, and the end the chart adds.
	private static readonly MokaBarSeries[] Budget = [new() { Name = "Cash", Values = [1000, 200, -300, double.NaN, 50] }];

	private static readonly int[] StartAndGross = [0, 3];

	private static readonly MokaChartAxis Fixed = new() { Min = 0, Max = 1500, Nice = false };

	public MokaBarChartWaterfallTests() => this.Static();

	[Fact]
	public void EachChange_FloatsFromTheRunningTotalBeforeIt_ToTheOneAfterIt()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Steps(p => p
			.Add(x => x.Series, [new MokaBarSeries { Name = "Cash", Values = [100, 50, -30, 20] }])
			.Add(x => x.YAxis, new MokaChartAxis { Min = 0, Max = 200, Nice = false }));

		Assert.False(cut.Instance.IsClassic);
		IReadOnlyList<IElement> marks = cut.FindAll(".moka-chart-mark");
		Assert.Equal(["--_lo: 0%; --_hi: 50%", "--_lo: 50%; --_hi: 75%", "--_lo: 60%; --_hi: 75%", "--_lo: 60%; --_hi: 70%"],
			marks.Select(mark => mark.GetAttribute("style")));
		Assert.Equal(["positive", "positive", "negative", "positive"], marks.Select(mark => mark.GetAttribute("data-moka-key")));
		Assert.Contains("moka-chart-mark--up", marks[1].ClassList);
		Assert.Contains("moka-chart-mark--down", marks[2].ClassList);
		Assert.All(marks, mark => Assert.Contains("moka-chart-hatch", mark.ClassList));
	}

	[Fact]
	public void AChange_IsSquareAtBothEnds_WhileATotal_RoundsTheEndItReaches()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted();

		IReadOnlyList<IElement> marks = cut.FindAll(".moka-chart-mark");
		Assert.Contains("moka-chart-mark--round-hi", marks[0].ClassList);
		Assert.DoesNotContain(marks, mark => mark.GetAttribute("data-moka-key") != "total"
		                                     && (mark.ClassList.Contains("moka-chart-mark--round-hi") || mark.ClassList.Contains("moka-chart-mark--round-lo")));
	}

	[Fact]
	public void Totals_StandOnZero_AStartAsGiven_ASubtotalAddedUp_AndTheEndAdded()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted();

		IReadOnlyList<IElement> marks = cut.FindAll(".moka-chart-mark");
		Assert.Equal(6, marks.Count);
		Assert.Equal(
		[
			"--_lo: 0%; --_hi: 66.667%", "--_lo: 66.667%; --_hi: 80%", "--_lo: 60%; --_hi: 80%", "--_lo: 0%; --_hi: 60%",
			"--_lo: 60%; --_hi: 63.333%", "--_lo: 0%; --_hi: 63.333%"
		], marks.Select(mark => mark.GetAttribute("style")));
		Assert.Equal(["total", "positive", "negative", "total", "positive", "total"], marks.Select(mark => mark.GetAttribute("data-moka-key")));
		Assert.All(marks.Where(mark => mark.GetAttribute("data-moka-key") == "total"),
			mark => Assert.Contains("moka-chart-mark--total", mark.ClassList));
		Assert.Equal(["Start", "Sales", "Refunds", "Gross", "Interest", "End"],
			cut.FindAll(".moka-chart-plot__xtick").Select(tick => tick.TextContent.Trim()));
	}

	[Fact]
	public void ATotalWithAValue_StandsAtIt_AndTheChangesAfterCountOnFromIt()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Steps(p => p
			.Add(x => x.Series, [new MokaBarSeries { Name = "Cash", Values = [100, 20, 500, -10] }])
			.Add(x => x.TotalsAt, [2])
			.Add(x => x.ShowTotal, true)
			.Add(x => x.YAxis, new MokaChartAxis { Min = 0, Max = 1000, Nice = false }));

		Assert.Equal(["--_lo: 0%; --_hi: 50%", "--_lo: 49%; --_hi: 50%", "--_lo: 0%; --_hi: 49%"],
			cut.FindAll(".moka-chart-mark").Skip(2).Select(mark => mark.GetAttribute("style")));
		Assert.Equal(490, cut.Instance.DescribeActive(0, -1, 4, null)!.Context.Points[0].Total);
	}

	[Fact]
	public void UpIsGoodFalse_SwapsTheColors_AndTheLegendNamesWhatTheBarsAre()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted(p => p.Add(x => x.UpIsGood, false));

		IReadOnlyList<IElement> marks = cut.FindAll(".moka-chart-mark");
		Assert.Contains("moka-chart-mark--down", marks[1].ClassList);
		Assert.Contains("moka-chart-mark--up", marks[2].ClassList);
		Assert.Equal(["Increase", "Decrease", "Total"], cut.Instance.SeriesInfo.Select(s => s.Name));
		Assert.Equal(["positive", "negative", "total"], cut.Instance.SeriesInfo.Select(s => s.Key));
		Assert.Contains("var(--moka-chart-down)", cut.Instance.SeriesInfo[0].ColorStyle, StringComparison.Ordinal);
		Assert.Contains("var(--moka-color-on-surface-tertiary)", cut.Instance.SeriesInfo[2].ColorStyle, StringComparison.Ordinal);
	}

	[Fact]
	public void TheDirections_TakeTheChartsWords_OrItsOwn()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Steps(p => p
			.Add(x => x.Series, Budget)
			.Add(x => x.ShowTotal, true)
			.Add(x => x.Text, MokaChartText.Default with { Increase = "Up", Total = "Balance" })
			.Add(x => x.NegativeText, "Spending"));

		Assert.Equal(["Up", "Spending", "Balance"], cut.Instance.SeriesInfo.Select(s => s.Name));
		Assert.Equal("Balance", cut.FindAll(".moka-chart-plot__xtick")[^1].TextContent.Trim());
		Assert.Equal(("Spending", "-300"), (cut.Instance.DescribeActive(0, -1, 2, null)!.Rows[0].Name, cut.Instance.DescribeActive(0, -1, 2, null)!.Rows[0].ValueText));
	}

	[Fact]
	public void WithoutTotals_TheLegendHasOnlyTheDirections()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Steps(p => p.Add(x => x.Series, [new MokaBarSeries { Name = "Cash", Values = [5, -2] }]));

		Assert.Equal(["positive", "negative"], cut.Instance.SeriesInfo.Select(s => s.Key));
	}

	[Fact]
	public void TheTooltip_SaysTheChangeInWords_ThenTheRunningTotal()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted();

		ChartActiveInfo sales = cut.Instance.DescribeActive(0, -1, 1, null)!;
		Assert.Equal("Sales", sales.Heading);
		ChartActiveRow row = Assert.Single(sales.Rows);
		Assert.Equal(("Increase", "+200"), (row.Name, row.ValueText));
		Assert.Null(row.DeltaText);
		Assert.Equal(["Total 1,200"], sales.Notes);
		Assert.Equal("Sales, Increase +200, Total 1,200", sales.CellText);

		MokaChartPoint point = Assert.Single(sales.Context.Points);
		Assert.Equal(("positive", "Increase", 0, 200d, 1200d, 1d), (point.SeriesKey, point.SeriesName, point.Series, point.Value, point.Total!.Value, point.X!.Value));

		ChartActiveInfo refunds = cut.Instance.DescribeActive(0, -1, 2, null)!;
		Assert.Equal(("Decrease", "-300"), (refunds.Rows[0].Name, refunds.Rows[0].ValueText));
		Assert.Equal(["Total 900"], refunds.Notes);
	}

	[Fact]
	public void ATotalsTooltip_SaysItsLevel_WithNoSecondTotal()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted();

		ChartActiveInfo end = cut.Instance.DescribeActive(0, -1, 5, null)!;
		Assert.Equal("End", end.Heading);
		Assert.Equal(("Total", "950"), (end.Rows[0].Name, end.Rows[0].ValueText));
		Assert.Empty(end.Notes);
		Assert.Equal("End, Total 950", end.CellText);
		Assert.Null(cut.Instance.DescribeActive(0, -1, 6, null));
	}

	[Fact]
	public void APin_SaysHowFarTheRunningTotalHasMovedSince()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted();

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 4, 1)!;
		Assert.Equal("-250", info.Rows[0].DeltaText);
		Assert.Equal(-250, info.Context.Points[0].SincePin);
	}

	[Fact]
	public void Values_PrintAtEachTip_ChangesWithTheirSign()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted(labels: null, fixedAxis: false);

		IReadOnlyList<IElement> values = cut.FindAll(".moka-chart-value");
		Assert.Equal(["1,000", "+200", "-300", "900", "+50", "950"], values.Select(value => value.TextContent));
		Assert.Contains("moka-chart-value--hi", values[1].ClassList);
		Assert.Contains("moka-chart-value--lo", values[2].ClassList);
	}

	[Fact]
	public void Inside_PrintsEachValueInItsBar()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted(labels: MokaValueLabels.Inside);

		Assert.Equal(["1,000", "+200", "-300", "900", "+50", "950"], cut.FindAll(".moka-chart-mark__label").Select(l => l.TextContent));
		Assert.Empty(cut.FindAll(".moka-chart-value"));
	}

	[Fact]
	public void Hairlines_JoinEachBarToTheNext_AtTheRunningTotal()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted();

		IReadOnlyList<IElement> columns = cut.FindAll(".moka-chart-cols > .moka-chart-col");
		Assert.Equal(6, columns.Count);
		Assert.Equal([(false, true), (true, true), (true, true), (true, true), (true, true), (true, false)],
			columns.Select(column => (column.QuerySelector(".moka-chart-link--in") is not null, column.QuerySelector(".moka-chart-link--out") is not null)));
		Assert.Equal("--_at: 66.667%", columns[0].QuerySelector(".moka-chart-link--out")!.GetAttribute("style"));
		Assert.Equal("--_at: 66.667%", columns[1].QuerySelector(".moka-chart-link--in")!.GetAttribute("style"));
		Assert.Equal("--_at: 60%", columns[2].QuerySelector(".moka-chart-link--out")!.GetAttribute("style"));
		Assert.Equal("--_at: 60%", columns[3].QuerySelector(".moka-chart-link--in")!.GetAttribute("style"));
		Assert.Equal("moka-chart-link moka-chart-link--in", columns[1].Children[0].ClassName);
	}

	[Fact]
	public void AValueThatIsntANumber_DrawsNothing_AndBreaksTheJoin()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Steps(p => p
			.Add(x => x.Series, [new MokaBarSeries { Name = "Cash", Values = [10, double.NaN, 5] }]));

		IReadOnlyList<IElement> columns = cut.FindAll(".moka-chart-cols > .moka-chart-col");
		Assert.Empty(columns[1].QuerySelectorAll(".moka-chart-mark, .moka-chart-link"));
		Assert.Null(columns[0].QuerySelector(".moka-chart-link"));
		Assert.Null(columns[2].QuerySelector(".moka-chart-link"));
		Assert.Equal(15, cut.Instance.DescribeActive(0, -1, 2, null)!.Context.Points[0].Total);
		Assert.Empty(cut.Instance.DescribeActive(0, -1, 1, null)!.Rows);
	}

	[Fact]
	public void TheValueAxis_ReachesZero_AndTheLowestAndHighestRunningTotals()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Steps(p => p
			.Add(x => x.ValueLabels, MokaValueLabels.None)
			.Add(x => x.Series, [new MokaBarSeries { Name = "Cash", Values = [-50, -100, 30] }]));

		Assert.Equal((-150, 0), cut.Instance.ValueRange(0));

		cut.Render(p => p.Add(x => x.Series, [new MokaBarSeries { Name = "Cash", Values = [400, 300, -100] }]));
		Assert.Equal((0, 700), cut.Instance.ValueRange(0));
	}

	[Fact]
	public void TheTable_SaysWhatEachBarIs_ItsChange_AndTheRunningTotal()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted();

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(["Name", "Type", "Change", "Total"], table.Headers);
		Assert.Equal(6, table.RowCount);
		Assert.Equal(["Start", "Total", "", "1,000"], Enumerable.Range(0, 4).Select(c => table.Display(0, c)));
		Assert.Equal(["Refunds", "Decrease", "-300", "900"], Enumerable.Range(0, 4).Select(c => table.Display(2, c)));
		Assert.Equal(["End", "Total", "", "950"], Enumerable.Range(0, 4).Select(c => table.Display(5, c)));
		Assert.Equal("-300", table.Raw(2, 2));
		Assert.Equal("1200", table.Raw(1, 3));
	}

	[Fact]
	public void AStaticChart_GivesEachColumnItsWordsAsANativeTooltip()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted();

		IReadOnlyList<IElement> columns = cut.FindAll(".moka-chart-cols > .moka-chart-col");
		Assert.Equal("Sales · Increase +200\nTotal 1,200", columns[1].GetAttribute("title"));
		Assert.Equal("Start · Total 1,000", columns[0].GetAttribute("title"));
	}

	[Fact]
	public void PastMaxColumns_ChangesMergeBetweenTotals_AndNeverAcrossOne()
	{
		using CultureScope culture = new();
		double[] values = [.. Enumerable.Range(0, 1000).Select(i => i % 7 == 0 ? -3d : 2d)];
		values[0] = 500;
		int[] totals = [0, 250, 251, 600];
		IRenderedComponent<MokaBarChart> cut = Steps(p => p
			.Add(x => x.MaxColumns, 50)
			.Add(x => x.ColumnAggregate, MokaChartAggregate.Max)
			.Add(x => x.TotalsAt, totals)
			.Add(x => x.ShowTotal, true)
			.Add(x => x.Series, [new MokaBarSeries { Name = "Cash", Values = values }]));

		int count = cut.Instance.CategoryCount;
		Assert.InRange(count, 50, 60);
		List<ChartActiveInfo> drawn = [.. Enumerable.Range(0, count).Select(i => cut.Instance.DescribeActive(0, -1, i, null)!)];
		Assert.Equal(5, drawn.Count(info => info.Rows[0].Name == "Total"));
		Assert.All(drawn.Where(info => info.Rows[0].Name == "Total"), info => Assert.DoesNotContain(" to ", info.Heading, StringComparison.Ordinal));

		double running = 0;
		for (int i = 0; i < values.Length; i++)
		{
			running = totals.Contains(i) ? values[i] : running + values[i];
		}

		Assert.Equal(running, drawn[^1].Context.Points[0].Total!.Value, 6);

		ChartActiveInfo merged = drawn[1];
		Assert.Equal("1 to 21", merged.Heading);
		double sum = values.Skip(1).Take(21).Sum();
		Assert.Equal(sum, merged.Context.Points[0].Value, 6);
		Assert.Equal(500 + sum, merged.Context.Points[0].Total!.Value, 6);
	}

	[Fact]
	public void ColumnsTooNarrowToShowThem_SendNoValuesAndNoJoins_UnlessValuesAreAskedFor()
	{
		using CultureScope culture = new();
		MokaBarSeries[] days = [new() { Name = "Cash", Values = [.. Enumerable.Range(0, 2000).Select(i => i % 3 == 0 ? -4d : 5d)] }];
		IRenderedComponent<MokaBarChart> cut = Steps(p => p.Add(x => x.Series, days));

		Assert.InRange(cut.Instance.CategoryCount, 300, 400);
		Assert.Empty(cut.FindAll(".moka-chart-value"));
		Assert.Empty(cut.FindAll(".moka-chart-link"));

		cut.Render(p =>
		{
			StepParameters(p);
			p.Add(x => x.Series, days);
			p.Add(x => x.ValueLabels, MokaValueLabels.Tip);
		});
		Assert.NotEmpty(cut.FindAll(".moka-chart-value"));
		Assert.Empty(cut.FindAll(".moka-chart-link"));
	}

	[Fact]
	public async Task TheWidthTheScriptReports_DecidesWhetherColumnsAreTooNarrow()
	{
		using CultureScope culture = new();
		this.Interactive();
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Waterfall, true)
			.Add(x => x.MaxColumns, 60)
			.Add(x => x.Series, [new MokaBarSeries { Name = "Cash", Values = [.. Enumerable.Repeat(1d, 120)] }]));
		Assert.Equal(60, cut.Instance.CategoryCount);
		Assert.NotEmpty(cut.FindAll(".moka-chart-link"));

		await cut.InvokeAsync(() => cut.Instance.OnPlotWidth(320));

		Assert.Equal(60, cut.Instance.CategoryCount);
		Assert.Empty(cut.FindAll(".moka-chart-link"));
		Assert.Empty(cut.FindAll(".moka-chart-value"));
	}

	[Fact]
	public void Waterfall_WinsOverStackedPercentDivergingAndSort()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> plain = Budgeted();
		IRenderedComponent<MokaBarChart> busy = Budgeted(p => p
				.Add(x => x.Stacked, true)
				.Add(x => x.Percent, true)
				.Add(x => x.Diverging, true)
				.Add(x => x.Sort, MokaChartSort.Descending),
			[Budget[0], new MokaBarSeries { Name = "Ignored", Values = [9, 9, 9, 9, 9] }]);

		Assert.Equal(plain.FindAll(".moka-chart-cols").Single().InnerHtml, busy.FindAll(".moka-chart-cols").Single().InnerHtml);
		Assert.Null(busy.Instance.ValueAxisUnit);
	}

	[Fact]
	public void HorizontalSteps_RunDownInTheOrderGiven_JoinedRowToRow()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted(p => p.Add(x => x.Horizontal, true));

		Assert.True(cut.Instance.DrawsRows);
		Assert.Equal(["Start", "Sales", "Refunds", "Gross", "Interest", "End"],
			cut.FindAll(".moka-chart-plot__rname").Select(name => name.TextContent.Trim()));
		IReadOnlyList<IElement> rows = cut.FindAll(".moka-chart-col--row");
		Assert.Equal(6, rows.Count);
		Assert.Equal(["moka-chart-link moka-chart-link--in", "moka-chart-link moka-chart-link--out"],
			rows[1].Children.Where(child => child.ClassList.Contains("moka-chart-link")).Select(child => child.ClassName));
		Assert.Contains("--_gh: 46%", rows[0].GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void AHiddenKind_LeavesItsBars_ButNotTheJoins()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted(p => p.Add(x => x.HiddenSeries, ["negative"]));

		Assert.DoesNotContain(cut.FindAll(".moka-chart-mark"), mark => mark.GetAttribute("data-moka-key") == "negative");
		Assert.Equal(2, cut.FindAll(".moka-chart-cols > .moka-chart-col")[2].QuerySelectorAll(".moka-chart-link").Length);
		Assert.True(cut.Instance.SeriesInfo[1].Hidden);
	}

	[Fact]
	public void Totals_TakeTheSeriesColor_WhenItNamesOne()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted(series: [Budget[0] with { ColorValue = "var(--moka-color-info)" }]);

		IElement total = cut.FindAll(".moka-chart-mark")[0];
		Assert.DoesNotContain("moka-chart-mark--total", total.ClassList);
		Assert.Contains("--moka-chart-color: var(--moka-color-info)", total.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Contains("var(--moka-color-info)", cut.Instance.SeriesInfo[2].ColorStyle, StringComparison.Ordinal);
	}

	[Fact]
	public void TheChartIsNamedAfterItsSeries()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted();

		Assert.Equal("Waterfall chart: Cash", cut.Instance.DefaultLabel);
		Assert.Equal("Waterfall chart: Cash", cut.Find(".moka-barchart").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheSameTotalsInANewList_BuildNothingAgain()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Budgeted();
		long version = cut.Instance.GeometryVersion;

		cut.Render(p =>
		{
			StepParameters(p);
			BudgetParameters(p, totals: [0, 3]);
		});
		Assert.Equal(version, cut.Instance.GeometryVersion);

		cut.Render(p =>
		{
			StepParameters(p);
			BudgetParameters(p, totals: [0]);
		});
		Assert.NotEqual(version, cut.Instance.GeometryVersion);
	}

	[Fact]
	public void OnTheOriginalDesign_AWaterfallIsDrawnOnTheStandardLook()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.AddCascadingValue(MokaTheme.Dark.WithCharts(MokaChartLook.Bold))
			.Add(x => x.Waterfall, true)
			.Add(x => x.Series, Budget)
			.Add(x => x.Labels, Books)
			.Add(x => x.TotalsAt, StartAndGross));

		Assert.False(cut.Instance.IsClassic);
		Assert.Contains("moka-chart-look-standard", cut.Find(".moka-barchart").ClassList);
		Assert.Equal(4, cut.FindAll(".moka-chart-link--out").Count);
	}

	[Fact]
	public void OnTheStandardLook_TheAxisIsNice_AndLeavesRoomForTheHighestValue()
	{
		using CultureScope culture = new();
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.AddCascadingValue(MokaTheme.Dark)
			.Add(x => x.Waterfall, true)
			.Add(x => x.Series, [new MokaBarSeries { Name = "Cash", Values = [400, 300, -100] }]));

		Assert.Contains("moka-chart-look-standard", cut.Find(".moka-barchart").ClassList);
		Assert.True(cut.Instance.ValueRange(0).Max > 700);
		Assert.Equal("+300", cut.FindAll(".moka-chart-value")[1].TextContent);
		IHtmlCollection<IElement> ticks = cut.Find(".moka-chart-plot__yticks").Children;
		Assert.Equal("800", ticks[ticks.Length - 1].TextContent.Trim());
	}

	[Fact]
	public void AnInteractiveWaterfall_FindsItsColumnsBySlot()
	{
		using CultureScope culture = new();
		this.Interactive();
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Waterfall, true)
			.Add(x => x.Series, Budget)
			.Add(x => x.Labels, Books)
			.Add(x => x.TotalsAt, StartAndGross));

		Assert.Equal("column", cut.Instance.SurfaceOptions()["mode"]);
		Assert.False(cut.Instance.PlacesColumns);
		Assert.Equal("5", cut.Find(".moka-chart-surface").GetAttribute("data-moka-count"));
		Assert.Null(cut.FindAll(".moka-chart-cols > .moka-chart-col")[0].GetAttribute("title"));
	}

	private IRenderedComponent<MokaBarChart> Budgeted(Action<ComponentParameterCollectionBuilder<MokaBarChart>>? more = null,
		IReadOnlyList<MokaBarSeries>? series = null, MokaValueLabels? labels = MokaValueLabels.None, bool fixedAxis = true) =>
		Steps(p => BudgetParameters(p, more, series, labels, fixedAxis));

	private static void BudgetParameters(ComponentParameterCollectionBuilder<MokaBarChart> p,
		Action<ComponentParameterCollectionBuilder<MokaBarChart>>? more = null, IReadOnlyList<MokaBarSeries>? series = null,
		MokaValueLabels? labels = MokaValueLabels.None, bool fixedAxis = true, IReadOnlyList<int>? totals = null)
	{
		p.Add(x => x.Series, series ?? Budget);
		p.Add(x => x.Labels, Books);
		p.Add(x => x.TotalsAt, totals ?? StartAndGross);
		p.Add(x => x.ShowTotal, true);
		p.Add(x => x.TotalLabel, "End");
		p.Add(x => x.YAxis, fixedAxis ? Fixed : null);
		p.Add(x => x.ValueLabels, labels);
		more?.Invoke(p);
	}

	private static void StepParameters(ComponentParameterCollectionBuilder<MokaBarChart> p)
	{
		p.Add(x => x.Look, MokaChartLook.Quiet);
		p.Add(x => x.Legend, MokaChartLegendPosition.None);
		p.Add(x => x.Waterfall, true);
	}

	private IRenderedComponent<MokaBarChart> Steps(Action<ComponentParameterCollectionBuilder<MokaBarChart>> parameters) =>
		Render<MokaBarChart>(p =>
		{
			StepParameters(p);
			parameters(p);
		});

	private sealed class CultureScope : IDisposable
	{
		private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

		public CultureScope() => CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

		public void Dispose() => CultureInfo.CurrentCulture = _previous;
	}
}
