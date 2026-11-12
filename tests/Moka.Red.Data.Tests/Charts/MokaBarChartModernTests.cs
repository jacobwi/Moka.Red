using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

public sealed class MokaBarChartModernTests : BunitContext
{
	private static readonly string[] Regions = ["EU West", "EU North", "US East"];

	private static readonly MokaBarSeries[] TwoWeeks =
	[
		new() { Name = "This week", Values = [412, 128, 506] },
		new() { Name = "Last week", Values = [388, 141, 470] }
	];

	public MokaBarChartModernTests() => this.Static();

	public static TheoryData<string> ModernParameters =>
	[
		nameof(MokaBarChart.Horizontal), nameof(MokaBarChart.Percent), nameof(MokaBarChart.Diverging),
		nameof(MokaBarChart.Sort), nameof(MokaBarChart.ValueLabels), nameof(MokaBarChart.BarEnds), nameof(MokaBarChart.MaxColumns),
		nameof(MokaBarChart.Title), nameof(MokaBarChart.Look), nameof(MokaBarChart.Tooltip), nameof(MokaBarChart.Keyboard),
		nameof(MokaBarChart.Legend), nameof(MokaBarChart.Highlight), nameof(MokaBarChart.Controls), nameof(MokaBarChart.TargetLow),
		nameof(MokaBarChart.Annotations), nameof(MokaBarChart.Unit), nameof(MokaBarChart.Formatter), "SeriesKey",
		nameof(MokaBarChart.Waterfall), nameof(MokaBarChart.TotalsAt), nameof(MokaBarChart.ShowTotal), nameof(MokaBarChart.TotalLabel)
	];

	[Fact]
	public void WithNothingNew_ItIsTheClassicSvgChart()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Series, TwoWeeks)
			.Add(x => x.Labels, Regions)
			.Add(x => x.Stacked, true)
			.Add(x => x.Height, "200px"));

		Assert.True(cut.Instance.IsClassic);
		Assert.NotEmpty(cut.FindAll("svg.moka-barchart-plot path"));
		Assert.Empty(cut.FindAll(".moka-chart-mark"));
	}

	[Theory]
	[MemberData(nameof(ModernParameters))]
	public void AnyNewParameter_DrawsTheHtmlChart(string name)
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p =>
		{
			p.Add(x => x.Labels, Regions);
			p.Add(x => x.Series, name == "SeriesKey" ? [TwoWeeks[0] with { Key = "now" }, TwoWeeks[1]] : TwoWeeks);
			_ = name switch
			{
				nameof(MokaBarChart.Horizontal) => p.Add(x => x.Horizontal, true),
				nameof(MokaBarChart.Percent) => p.Add(x => x.Percent, true),
				nameof(MokaBarChart.Diverging) => p.Add(x => x.Diverging, true),
				nameof(MokaBarChart.Sort) => p.Add(x => x.Sort, MokaChartSort.Descending),
				nameof(MokaBarChart.ValueLabels) => p.Add(x => x.ValueLabels, MokaValueLabels.Tip),
				nameof(MokaBarChart.BarEnds) => p.Add(x => x.BarEnds, MokaBarEnds.Square),
				nameof(MokaBarChart.MaxColumns) => p.Add(x => x.MaxColumns, 100),
				nameof(MokaBarChart.Title) => p.Add(x => x.Title, "Requests"),
				nameof(MokaBarChart.Look) => p.Add(x => x.Look, MokaChartLook.Quiet),
				nameof(MokaBarChart.Tooltip) => p.Add(x => x.Tooltip, MokaChartTooltip.Column),
				nameof(MokaBarChart.Keyboard) => p.Add(x => x.Keyboard, false),
				nameof(MokaBarChart.Legend) => p.Add(x => x.Legend, MokaChartLegendPosition.Top),
				nameof(MokaBarChart.Highlight) => p.Add(x => x.Highlight, "This week"),
				nameof(MokaBarChart.Controls) => p.Add(x => x.Controls, MokaChartControls.None),
				nameof(MokaBarChart.TargetLow) => p.Add(x => x.TargetLow, 100),
				nameof(MokaBarChart.Annotations) => p.Add(x => x.Annotations, [new MokaChartThreshold(300)]),
				nameof(MokaBarChart.Unit) => p.Add(x => x.Unit, MokaChartUnit.Bytes),
				nameof(MokaBarChart.Formatter) => p.Add(x => x.Formatter, Whole),
				nameof(MokaBarChart.Waterfall) => p.Add(x => x.Waterfall, true),
				nameof(MokaBarChart.TotalsAt) => p.Add(x => x.TotalsAt, [1]),
				nameof(MokaBarChart.ShowTotal) => p.Add(x => x.ShowTotal, true),
				nameof(MokaBarChart.TotalLabel) => p.Add(x => x.TotalLabel, "End"),
				_ => p
			};
		});

		Assert.False(cut.Instance.IsClassic);
		Assert.Empty(cut.FindAll("svg.moka-barchart-plot"));
		Assert.NotEmpty(cut.FindAll(".moka-chart-mark"));
	}

	[Fact]
	public void ATitleSetLater_TakesTheChartOffTheClassicPath()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p.Add(x => x.Series, TwoWeeks).Add(x => x.Labels, Regions));
		Assert.True(cut.Instance.IsClassic);

		cut.Render(p => p.Add(x => x.Title, "Requests by region"));

		Assert.False(cut.Instance.IsClassic);
		Assert.Equal("Requests by region", cut.Find(".moka-chart-frame__title").TextContent);
		Assert.Equal(6, cut.FindAll(".moka-chart-mark").Count);
	}

	[Fact]
	public void TheOriginalDesign_KeepsTheClassicChart_WhateverTheThemeSaysAboutCharts()
	{
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.AddCascadingValue(MokaTheme.Dark.WithCharts(MokaChartLook.Bold))
			.Add(x => x.Series, TwoWeeks));

		Assert.True(cut.Instance.IsClassic);
	}

	[Fact]
	public void GroupedBars_StandSideBySide_OneLanePerSeries_InAColumnPerCategory()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p.Add(x => x.Series, TwoWeeks).Add(x => x.Labels, Regions));

		IReadOnlyList<IElement> columns = cut.FindAll(".moka-chart-cols > .moka-chart-col");
		Assert.Equal(3, columns.Count);
		Assert.All(columns, column => Assert.Equal(2, column.QuerySelectorAll(".moka-chart-col__lane").Length));
		Assert.Contains("--_cs: 33.333%", columns[1].GetAttribute("style"), StringComparison.Ordinal);
		Assert.Contains("--_cw: 33.333%", columns[1].GetAttribute("style"), StringComparison.Ordinal);
		Assert.Contains("--_gw: 72%", columns[1].GetAttribute("style"), StringComparison.Ordinal);

		IElement first = columns[0].QuerySelector(".moka-chart-mark")!;
		Assert.Equal("0", first.GetAttribute("data-i"));
		Assert.Equal("This week", first.GetAttribute("data-moka-key"));
		Assert.Contains("moka-chart-c1", first.ClassList);
		Assert.Contains("moka-chart-hatch", first.ClassList);
		Assert.Contains("moka-chart-mark--round-hi", first.ClassList);
		Assert.Contains("moka-chart-c2", columns[0].QuerySelectorAll(".moka-chart-mark")[1].ClassList);
	}

	[Fact]
	public void TheCategoryLabels_SitUnderTheMiddleOfEachSlot()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p.Add(x => x.Series, TwoWeeks).Add(x => x.Labels, Regions));

		Assert.Equal(1d / 6, cut.Instance.CategoryShare(0, 3), 6);
		Assert.Equal(["EU West", "EU North", "US East"], cut.FindAll(".moka-chart-plot__xtick").Select(t => t.TextContent.Trim()));
	}

	[Fact]
	public void SquareEnds_RoundNothing()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p.Add(x => x.Series, TwoWeeks).Add(x => x.BarEnds, MokaBarEnds.Square));

		Assert.Empty(cut.FindAll(".moka-chart-mark--round-hi, .moka-chart-mark--round-lo"));
	}

	[Fact]
	public void ANegativeBar_RoundsItsLowEnd_AndHangsFromZero()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p
			.Add(x => x.Series, [new MokaBarSeries { Name = "Delta", Values = [-4, 4] }])
			.Add(x => x.YAxis, new MokaChartAxis { Min = -4, Max = 4, Nice = false }));

		IReadOnlyList<IElement> marks = cut.FindAll(".moka-chart-mark");
		Assert.Contains("moka-chart-mark--round-lo", marks[0].ClassList);
		Assert.Equal("--_lo: 0%; --_hi: 50%", marks[0].GetAttribute("style"));
		Assert.Contains("moka-chart-mark--round-hi", marks[1].ClassList);
		Assert.Equal("--_lo: 50%; --_hi: 100%", marks[1].GetAttribute("style"));
	}

	[Fact]
	public void StackedSegments_KeepAGap_AndOnlyEachStacksOuterEndIsRounded()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p
			.Add(x => x.Stacked, true)
			.Add(x => x.Series,
			[
				new MokaBarSeries { Name = "Sales", Values = [40] },
				new MokaBarSeries { Name = "Services", Values = [20] },
				new MokaBarSeries { Name = "Refunds", Values = [-10] },
				new MokaBarSeries { Name = "Fees", Values = [-5] }
			])
			.Add(x => x.YAxis, new MokaChartAxis { Min = -20, Max = 60, Nice = false }));

		IReadOnlyList<IElement> marks = cut.FindAll(".moka-chart-mark");
		Assert.Equal(4, marks.Count);
		Assert.Single(cut.FindAll(".moka-chart-col__lane"));
		Assert.Equal("--_lo: 25%; --_hi: 75%", marks[0].GetAttribute("style"));
		Assert.DoesNotContain("moka-chart-mark--gap-lo", marks[0].ClassList);
		Assert.DoesNotContain("moka-chart-mark--round-hi", marks[0].ClassList);
		Assert.Contains("moka-chart-mark--gap-lo", marks[1].ClassList);
		Assert.Contains("moka-chart-mark--round-hi", marks[1].ClassList);
		Assert.Equal("--_lo: 12.5%; --_hi: 25%", marks[2].GetAttribute("style"));
		Assert.DoesNotContain("moka-chart-mark--round-lo", marks[2].ClassList);
		Assert.Contains("moka-chart-mark--gap-hi", marks[3].ClassList);
		Assert.Contains("moka-chart-mark--round-lo", marks[3].ClassList);
	}

	[Fact]
	public void PercentBars_SplitEachCategoryIntoShares_PrintedInsideWhereTheyFit()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p
			.Add(x => x.Percent, true)
			.Add(x => x.Labels, ["LAN", "Guest"])
			.Add(x => x.Series,
			[
				new MokaBarSeries { Name = "HTTPS", Values = [62, 30] },
				new MokaBarSeries { Name = "QUIC", Values = [38, 10] }
			]));

		Assert.Equal((0, 100), cut.Instance.ValueRange(0));
		Assert.Equal(["62%", "38%", "75%", "25%"], cut.FindAll(".moka-chart-mark__label").Select(l => l.TextContent));
		Assert.Contains("100%", cut.FindAll(".moka-chart-plot__ytick").Select(t => t.TextContent.Trim()));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 1, null)!;
		Assert.Equal("Guest", info.Heading);
		Assert.Equal(["75%", "25%"], info.Rows.Select(r => r.ValueText));
		Assert.Equal(["30", "10"], info.Rows.Select(r => r.DeltaText));
	}

	[Fact]
	public void DivergingBars_TakeTheUpAndDownColorsBySign_AndTheLegendNamesTheDirections()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p
			.Add(x => x.Diverging, true)
			.Add(x => x.UpIsGood, false)
			.Add(x => x.PositiveText, "Slower")
			.Add(x => x.NegativeText, "Faster")
			.Add(x => x.Labels, ["/api/sync", "/api/search"])
			.Add(x => x.Series, [new MokaBarSeries { Name = "Change", Values = [71, -64] }]));

		IReadOnlyList<IElement> marks = cut.FindAll(".moka-chart-mark");
		Assert.Contains("moka-chart-mark--down", marks[0].ClassList);
		Assert.Equal("positive", marks[0].GetAttribute("data-moka-key"));
		Assert.Contains("moka-chart-mark--up", marks[1].ClassList);
		Assert.Equal(["Slower", "Faster"], cut.Instance.SeriesInfo.Select(s => s.Name));
		Assert.Equal("Faster", cut.Instance.DescribeActive(0, -1, 1, null)!.Rows[0].Name);
		(double min, double max) = cut.Instance.ValueRange(0);
		Assert.Equal(-71 * 1.08, min, 6);
		Assert.Equal(71 * 1.08, max, 6);
	}

	[Fact]
	public void HorizontalBars_AreRows_LargestFirst_WithTheValueAtTheTip()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p
			.Add(x => x.Horizontal, true)
			.Add(x => x.Labels, ["b.example", "a.example", "c.example"])
			.Add(x => x.Series, [new MokaBarSeries { Name = "Queries", Values = [500, 2000, 1000] }]));

		Assert.True(cut.Instance.DrawsRows);
		Assert.Equal(["a.example", "c.example", "b.example"], cut.FindAll(".moka-chart-plot__rname").Select(n => n.TextContent.Trim()));
		IReadOnlyList<IElement> rows = cut.FindAll(".moka-chart-col--row");
		Assert.Equal(3, rows.Count);
		Assert.Contains("--_gh: 46%", rows[0].GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("0", rows[0].QuerySelector(".moka-chart-mark")!.GetAttribute("data-i"));
		Assert.Contains("2,000", cut.FindAll(".moka-chart-value").Select(v => v.TextContent));
	}

	[Fact]
	public void SortAscending_PutsTheSmallestFirst_AndNoneKeepsTheOrderGiven()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p
			.Add(x => x.Horizontal, true)
			.Add(x => x.Sort, MokaChartSort.Ascending)
			.Add(x => x.Labels, ["b", "a", "c"])
			.Add(x => x.Series, [new MokaBarSeries { Name = "Q", Values = [500, 2000, 1000] }]));
		Assert.Equal(["b", "c", "a"], cut.FindAll(".moka-chart-plot__rname").Select(n => n.TextContent.Trim()));

		cut.Render(p => p.Add(x => x.Sort, MokaChartSort.None));
		Assert.Equal(["b", "a", "c"], cut.FindAll(".moka-chart-plot__rname").Select(n => n.TextContent.Trim()));
	}

	[Fact]
	public void PastMaxColumns_CategoriesMerge_AndTheTooltipNamesTheSpan()
	{
		string[] days = [.. Enumerable.Range(1, 1000).Select(i => $"D{i}")];
		IRenderedComponent<MokaBarChart> cut = Modern(p => p
			.Add(x => x.MaxColumns, 100)
			.Add(x => x.Labels, days)
			.Add(x => x.Series, [new MokaBarSeries { Name = "Requests", Values = [.. Enumerable.Repeat(1d, 1000)] }]));

		Assert.Equal(100, cut.FindAll(".moka-chart-cols > .moka-chart-col").Count);
		Assert.Equal(100, cut.Instance.CategoryCount);
		Assert.Equal("D11", cut.Instance.CategoryText(1));
		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 1, null)!;
		Assert.Equal("D11 to D20", info.Heading);
		Assert.Equal("10", info.Rows[0].ValueText);
	}

	[Fact]
	public void MergedColumns_TakeTheAggregateAsked()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p
			.Add(x => x.MaxColumns, 2)
			.Add(x => x.ColumnAggregate, MokaChartAggregate.Max)
			.Add(x => x.Series, [new MokaBarSeries { Name = "R", Values = [1, 5, 2, 3] }]));

		Assert.Equal(["5", "3"], Enumerable.Range(0, 2).Select(i => cut.Instance.DescribeActive(0, -1, i, null)!.Rows[0].ValueText));
	}

	[Fact]
	public void ScrollKeepsEveryCategory_HoweverMany()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p
			.Add(x => x.MaxColumns, 10)
			.Add(x => x.LabelFit, MokaAxisLabelFit.Scroll)
			.Add(x => x.Series, [new MokaBarSeries { Name = "R", Values = [.. Enumerable.Repeat(1d, 50)] }]));

		Assert.Equal(50, cut.FindAll(".moka-chart-cols > .moka-chart-col").Count);
		Assert.NotNull(cut.Find(".moka-chart-plot__scroll .moka-chart-cols"));
	}

	[Fact]
	public void RowsAreNeverMerged()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p
			.Add(x => x.Horizontal, true)
			.Add(x => x.MaxColumns, 10)
			.Add(x => x.Series, [new MokaBarSeries { Name = "R", Values = [.. Enumerable.Range(1, 50).Select(i => (double)i)] }]));

		Assert.Equal(50, cut.Instance.RowCount);
		Assert.Equal(50, cut.FindAll(".moka-chart-col--row").Count);
	}

	[Fact]
	public void TheTooltip_ListsEverySeriesInTheColumn_ValueFirst_WithItsShareOfTheColumn()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p.Add(x => x.Series, TwoWeeks).Add(x => x.Labels, Regions));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 2, null)!;
		Assert.Equal("US East", info.Heading);
		Assert.Equal(["506", "470"], info.Rows.Select(r => r.ValueText));
		Assert.Equal(["52%", "48%"], info.Rows.Select(r => r.DeltaText));
		Assert.Equal("US East, This week 506, Last week 470", info.CellText);
		Assert.Equal(2, info.Context.Points.Count);
		Assert.Equal(976, info.Context.Points[0].Total);
		Assert.Null(cut.Instance.DescribeActive(0, -1, 3, null));
		Assert.Null(cut.Instance.DescribeActive(0, -1, -1, null));
	}

	[Fact]
	public void WithOneSeries_TheTooltipSaysTheShareOfTheWhole()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p
			.Add(x => x.Labels, ["LAN", "Guest", "IoT", "VPN"])
			.Add(x => x.Series, [new MokaBarSeries { Name = "Devices online", Values = [38, 21, 34, 11] }])
			.Add(x => x.ShareText, (share, total) => $"{share} of the {total} online"));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 0, null)!;
		Assert.Equal(["37% of the 104 online"], info.Notes);
		Assert.Equal("LAN, Devices online 38, 37% of the 104 online", info.CellText);
	}

	[Fact]
	public void APin_ReplacesTheShareWithTheChangeSinceThePin()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p.Add(x => x.Series, TwoWeeks).Add(x => x.Labels, Regions));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 2, 0)!;
		Assert.Equal(["+94", "+82"], info.Rows.Select(r => r.DeltaText));
	}

	[Fact]
	public void AStackedColumn_SaysItsTotal_AndTheTargetBand()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p
			.Add(x => x.Stacked, true)
			.Add(x => x.TargetLow, 100)
			.Add(x => x.TargetHigh, 900)
			.Add(x => x.Series, TwoWeeks));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 2, null)!;
		Assert.Equal("Total 976", info.Notes[0]);
		Assert.Contains(info.Notes, note => note.Contains("above target", StringComparison.Ordinal));
	}

	[Fact]
	public void Largest_LabelsOnlyTheBusiestBar()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p
			.Add(x => x.ValueLabels, MokaValueLabels.Largest)
			.Add(x => x.Series, TwoWeeks));

		IElement value = Assert.Single(cut.FindAll(".moka-chart-value"));
		Assert.Equal("506", value.TextContent);
		Assert.Contains("moka-chart-value--hi", value.ClassList);
		Assert.Same(cut.FindAll(".moka-chart-cols > .moka-chart-col")[2].QuerySelector(".moka-chart-col__lane"), value.ParentElement);
	}

	[Fact]
	public void WithATargetBand_OnlyTheBarsOutsideItArePrinted()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p
			.Add(x => x.TargetLow, 20)
			.Add(x => x.TargetHigh, 40)
			.Add(x => x.Series, [new MokaBarSeries { Name = "Backup", Values = [28, 44, 31, 12] }]));

		Assert.Equal(["44", "12"], cut.FindAll(".moka-chart-value").Select(v => v.TextContent));
		Assert.Contains("moka-chart-value--w1", cut.FindAll(".moka-chart-value")[0].ClassList);
		Assert.Contains(cut.FindAll(".moka-chart-plot__band"), band => band is not null);
	}

	[Fact]
	public void Highlight_MutesTheOtherSeries()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p.Add(x => x.Series, TwoWeeks).Add(x => x.Highlight, "Last week"));

		IReadOnlyList<IElement> first = cut.FindAll(".moka-chart-cols > .moka-chart-col")[0].QuerySelectorAll(".moka-chart-mark");
		Assert.Contains("moka-chart-muted", first[0].ClassList);
		Assert.Contains("moka-chart-c2", first[1].ClassList);
	}

	[Fact]
	public void AHiddenSeries_LeavesTheBarsAndTheScale()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p.Add(x => x.Series, TwoWeeks).Add(x => x.HiddenSeries, ["This week"]));

		Assert.Equal(3, cut.FindAll(".moka-chart-mark").Count);
		Assert.Equal(470 * 1.08, cut.Instance.ValueRange(0).Max, 6);
		Assert.True(cut.Instance.SeriesInfo[0].Hidden);
	}

	[Fact]
	public void ASeriesKeepsItsPinnedSlotAndOwnColor()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p.Add(x => x.Series,
		[
			new MokaBarSeries { Name = "A", Values = [1], Slot = 5 },
			new MokaBarSeries { Name = "B", Values = [2], ColorValue = "var(--moka-color-info)" }
		]));

		IReadOnlyList<IElement> marks = cut.FindAll(".moka-chart-mark");
		Assert.Contains("moka-chart-c5", marks[0].ClassList);
		Assert.Contains("--moka-chart-color: var(--moka-color-info)", marks[1].GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void AStaticChart_UsesNativeTooltipsOnItsColumns()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p.Add(x => x.Series, TwoWeeks).Add(x => x.Labels, Regions));

		Assert.Equal("EU West\nThis week: 412\nLast week: 388", cut.FindAll(".moka-chart-cols > .moka-chart-col")[0].GetAttribute("title"));
	}

	[Fact]
	public void TheTable_ListsEveryCategory_WithEachSeries()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p.Add(x => x.Series, TwoWeeks).Add(x => x.Labels, Regions));

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(["Name", "This week", "Last week"], table.Headers);
		Assert.Equal(3, table.RowCount);
		Assert.Equal("EU North", table.Display(1, 0));
		Assert.Equal("141", table.Raw(1, 2));
	}

	[Fact]
	public void PercentTable_GivesEachSeriesItsValueAndItsShare()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p
			.Add(x => x.Percent, true)
			.Add(x => x.Series, [new MokaBarSeries { Name = "A", Values = [3] }, new MokaBarSeries { Name = "B", Values = [1] }]));

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(["Name", "A", "A Share", "B", "B Share"], table.Headers);
		Assert.Equal("3", table.Display(0, 1));
		Assert.Equal("75%", table.Display(0, 2));
		Assert.Equal("0.25", table.Raw(0, 4));
	}

	[Fact]
	public void AnInteractiveChart_IsAFigureWithAGridOfColumns()
	{
		BunitJSModuleInterop module = this.Interactive();
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Series, TwoWeeks)
			.Add(x => x.Labels, Regions));

		Assert.Equal("figure", cut.Find(".moka-barchart").GetAttribute("role"));
		IElement surface = cut.Find(".moka-chart-surface");
		Assert.Equal("3", surface.GetAttribute("data-moka-count"));
		Assert.Contains("Left and Right move between bars.", cut.Find(".moka-chart-surface__grid").GetAttribute("aria-label"),
			StringComparison.Ordinal);
		Assert.Null(cut.FindAll(".moka-chart-cols > .moka-chart-col")[0].GetAttribute("title"));
		Assert.Equal("column", cut.Instance.SurfaceOptions()["mode"]);
		Assert.Contains(module.Invocations, call => call.Identifier == "bind");
	}

	[Fact]
	public void AnInteractiveHorizontalChart_PutsItsSurfaceOverTheRows()
	{
		this.Interactive();
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Horizontal, true)
			.Add(x => x.Series, TwoWeeks)
			.Add(x => x.Labels, Regions));

		Assert.NotNull(cut.Find(".moka-chart-plot__rbody > .moka-chart-surface"));
		Assert.Equal("rows", cut.Instance.SurfaceOptions()["orientation"]);
		Assert.Contains("Up and Down move between rows.", cut.Find(".moka-chart-surface__grid").GetAttribute("aria-label"),
			StringComparison.Ordinal);
	}

	[Fact]
	public void PastTwoHundredRows_AnInteractiveChartDrawsOnlyTheRowsInView()
	{
		this.Interactive();
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Horizontal, true)
			.Add(x => x.MaxHeight, "320px")
			.Add(x => x.Series, [new MokaBarSeries { Name = "Q", Values = [.. Enumerable.Range(0, 2000).Select(i => (double)i)] }]));

		Assert.True(cut.Instance.RowPlotModel().Virtualize);
		Assert.InRange(cut.FindAll(".moka-chart-col--row").Count, 1, 200);
		Assert.Contains("--_row-h: 28px", cut.Find(".moka-chart-plot--rows").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void TheWidthTheScriptReports_MergesColumnsToThreePixelsEach()
	{
		this.Interactive();
		IRenderedComponent<MokaBarChart> cut = Render<MokaBarChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Series, [new MokaBarSeries { Name = "R", Values = [.. Enumerable.Repeat(1d, 300)] }]));
		Assert.True(cut.Instance.MeasuresWidth);
		Assert.Equal(300, cut.Instance.CategoryCount);

		cut.InvokeAsync(() => cut.Instance.OnPlotWidth(320));

		Assert.Equal(100, cut.Instance.CategoryCount);
	}

	[Fact]
	public void UnchangedValues_InNewLists_BuildNothingAgain()
	{
		IRenderedComponent<MokaBarChart> cut = Modern(p => p.Add(x => x.Series, TwoWeeks).Add(x => x.Labels, Regions));
		long version = cut.Instance.GeometryVersion;

		// A parent hands every parameter over again on each render.
		cut.Render(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Legend, MokaChartLegendPosition.None)
			.Add(x => x.Series, [.. TwoWeeks])
			.Add(x => x.Labels, [.. Regions]));
		Assert.Equal(version, cut.Instance.GeometryVersion);

		cut.Render(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Legend, MokaChartLegendPosition.None)
			.Add(x => x.Series, [TwoWeeks[0] with { Values = [1, 2, 3] }, TwoWeeks[1]])
			.Add(x => x.Labels, [.. Regions]));
		Assert.NotEqual(version, cut.Instance.GeometryVersion);
	}

	private static string Whole(double value) => value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

	private IRenderedComponent<MokaBarChart> Modern(Action<ComponentParameterCollectionBuilder<MokaBarChart>> parameters) =>
		Render<MokaBarChart>(p =>
		{
			p.Add(x => x.Look, MokaChartLook.Quiet);
			p.Add(x => x.Legend, MokaChartLegendPosition.None);
			parameters(p);
		});
}
