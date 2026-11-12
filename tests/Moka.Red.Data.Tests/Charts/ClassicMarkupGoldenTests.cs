using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using Moka.Red.Data.Heatmap;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     The whole markup of today's line, bar and donut charts and the calendar heatmap, captured before the chart engine
///     moved under them. The classic path has to stay byte for byte what these files hold, apart from the scope
///     attributes, which change whenever a stylesheet moves. Set <c>MOKA_RECORD_GOLDENS=1</c> to write the files again
///     after a change that is meant to show.
/// </summary>
public sealed partial class ClassicMarkupGoldenTests : BunitContext
{
	private const string RecordVariable = "MOKA_RECORD_GOLDENS";

	private static readonly string[] Days = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun", "Mon2"];

	private static string Rate(double bytes) => bytes switch
	{
		>= 1024 * 1024 => string.Create(CultureInfo.InvariantCulture, $"{bytes / (1024 * 1024):0.#} MiB/s"),
		>= 1024 => string.Create(CultureInfo.InvariantCulture, $"{bytes / 1024:0.#} KiB/s"),
		_ => string.Create(CultureInfo.InvariantCulture, $"{bytes:0} B/s")
	};

	private static string Gb(double value) => string.Create(CultureInfo.InvariantCulture, $"{value:0.#} GB");

	[Fact]
	public void LineChart_WithGaps() => Golden("line-gaps", () => Render<MokaLineChart>(p => p
		.Add(x => x.Label, "Gaps")
		.Add(x => x.Labels, Days)
		.Add(x => x.Series,
		[
			new MokaLineSeries { Name = "A", Values = [1, 3, double.NaN, 4, 2, double.NaN, double.NaN, 5] },
			new MokaLineSeries { Name = "B", Values = [2, 2, 3, double.NaN, 1, 1, 2, 3] }
		])));

	[Fact]
	public void LineChart_Smooth() => Golden("line-smooth", () => Render<MokaLineChart>(p => p
		.Add(x => x.Smooth, true)
		.Add(x => x.YTicks, 3)
		.Add(x => x.Labels, ["a", "b", "c", "d", "e"])
		.Add(x => x.Series, [new MokaLineSeries { Name = "Load", Values = [1, 4, 2, 6, 3], Color = MokaColor.Info }])));

	[Fact]
	public void LineChart_LiveSlide()
	{
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Live, true)
			.Add(x => x.ShowPoints, false)
			.Add(x => x.LiveInterval, TimeSpan.FromMilliseconds(750))
			.Add(x => x.Label, "Live")
			.Add(x => x.Series, [new MokaLineSeries { Name = "Down", Values = [double.NaN, double.NaN, 1, 2, 3, 4] }]));

		cut.Render(p => p.Add(x => x.Series,
			[new MokaLineSeries { Name = "Down", Values = [double.NaN, 1, 2, 3, 4, 5] }]));

		Assert.Contains("moka-linechart-slide--a", cut.Markup, StringComparison.Ordinal);
		Golden("line-live", () => cut);
	}

	[Fact]
	public void LineChart_Area() => Golden("line-area", () => Render<MokaLineChart>(p => p
		.Add(x => x.ShowArea, true)
		.Add(x => x.Min, -2d)
		.Add(x => x.Labels, ["one", "", "three", "four"])
		.Add(x => x.Series,
		[
			new MokaLineSeries { Name = "In", Values = [3, 5, double.NaN, 4], Color = MokaColor.Success },
			new MokaLineSeries { Name = "Out", Values = [-1, 2, 2, 1] },
			new MokaLineSeries { Name = "Empty", Values = [] }
		])));

	[Fact]
	public void LineChart_LastPoint() => Golden("line-last", () => Render<MokaLineChart>(p => p
		.Add(x => x.ShowLastPoint, true)
		.Add(x => x.ShowPoints, false)
		.Add(x => x.Formatter, Rate)
		.Add(x => x.TooltipLabel, i => i == 2 ? string.Empty : $"sample {i.ToString(CultureInfo.InvariantCulture)}")
		.Add(x => x.Height, "200px")
		.Add(x => x.AutoScale, true)
		.Add(x => x.ScaleUnits, [1d, 1024, 1024 * 1024])
		.Add(x => x.SuggestedMax, 4096d)
		.Add(x => x.Series,
		[
			new MokaLineSeries { Name = "Down", Values = [2048, 3000, 1024, double.NaN] },
			new MokaLineSeries { Name = "Up", Values = [10, 400, 512, 700] }
		])));

	[Fact]
	public void LineChart_Peak() => Golden("line-peak", () => Render<MokaLineChart>(p => p
		.Add(x => x.Label, "Rate")
		.Add(x => x.ShowPeak, true)
		.Add(x => x.ShowCurrent, true)
		.Add(x => x.PeakLabel, "Max")
		.Add(x => x.Formatter, Rate)
		.Add(x => x.ShowXLabels, false)
		.Add(x => x.Size, MokaSize.Lg)
		.Add(x => x.Class, "mine")
		.Add(x => x.Style, "margin: 2px")
		.Add(x => x.Id, "peak-chart")
		.Add(x => x.Series,
		[
			new MokaLineSeries { Name = "Down", Values = [2048, 1024, double.NaN] },
			new MokaLineSeries { Name = "Up", Values = [10, 4096, 512] }
		])));

	[Fact]
	public void LineChart_ManyLabels() => Golden("line-many-labels", () => Render<MokaLineChart>(p => p
		.Add(x => x.ShowPoints, false)
		.Add(x => x.TickFormat, "0.0")
		.Add(x => x.Labels,
			[.. Enumerable.Range(0, 40).Select(i => i == 7 ? " " : $"{i / 2:00}:{(i % 2) * 30:00}")])
		.Add(x => x.Series,
		[
			new MokaLineSeries
			{
				Name = "Traffic",
				Values = [.. Enumerable.Range(0, 40).Select(i => Math.Round(Math.Sin(i / 4d) * 10 + 12, 2))]
			}
		])));

	[Fact]
	public void LineChart_EmptyWithText() => Golden("line-empty", () => Render<MokaLineChart>(p => p
		.Add(x => x.EmptyText, "Waiting for traffic")
		.Add(x => x.ShowLegend, false)
		.Add(x => x.Series, [new MokaLineSeries { Name = "Down", Values = [double.NaN, double.NaN, double.NaN] }])));

	[Fact]
	public void BarChart_Grouped() => Golden("bar-grouped", () => Render<MokaBarChart>(p => p
		.Add(x => x.Label, "Sales")
		.Add(x => x.Labels, ["Q1", "Q2", "Q3", "Q4"])
		.Add(x => x.Series,
		[
			new MokaBarSeries { Name = "North", Values = [4, 7, 5, 9] },
			new MokaBarSeries { Name = "South", Values = [3, 6, 8, 2], Color = MokaColor.Warning }
		])));

	[Fact]
	public void BarChart_Stacked() => Golden("bar-stacked", () => Render<MokaBarChart>(p => p
		.Add(x => x.Stacked, true)
		.Add(x => x.Max, 20d)
		.Add(x => x.Labels, ["Jan", "Feb", "Mar"])
		.Add(x => x.Series,
		[
			new MokaBarSeries { Name = "Web", Values = [4, 0, 5] },
			new MokaBarSeries { Name = "Mail", Values = [3, 6, -2] },
			new MokaBarSeries { Name = "Files", Values = [2, 2] }
		])));

	[Fact]
	public void BarChart_Negative() => Golden("bar-negative", () => Render<MokaBarChart>(p => p
		.Add(x => x.YTicks, 5)
		.Add(x => x.TickFormat, "0.0")
		.Add(x => x.Height, "180px")
		.Add(x => x.ShowXLabels, false)
		.Add(x => x.ShowLegend, false)
		.Add(x => x.Size, MokaSize.Sm)
		.Add(x => x.Series, [new MokaBarSeries { Name = "Delta", Values = [-3.5, 2, -1, 4.25] }])));

	[Fact]
	public void DonutChart_WithTotal() => Golden("donut-total", () => Render<MokaDonutChart>(p => p
		.Add(x => x.Label, "Storage")
		.Add(x => x.Total, 100d)
		.Add(x => x.Thickness, 30d)
		.Add(x => x.Formatter, Gb)
		.Add(x => x.ChildContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<b>42</b>")))
		.Add(x => x.Segments,
		[
			new MokaDonutSegment { Name = "Photos", Value = 40 },
			new MokaDonutSegment { Name = "Nothing", Value = 0 },
			new MokaDonutSegment { Name = "Music", Value = 25.5, Color = MokaColor.Success },
			new MokaDonutSegment { Name = "Spill", Value = 50 }
		])));

	[Fact]
	public void Heatmap_WithUnknownDays() => Golden("heatmap-unknown", () => Render<MokaHeatmap>(p => p
		.Add(x => x.Label, "Commits")
		.Add(x => x.Weeks, 9)
		.Add(x => x.EndDate, new DateTime(2026, 3, 4))
		.Add(x => x.UnknownText, "Collector down")
		.Add(x => x.Data, new Dictionary<DateTime, int>
		{
			[new DateTime(2026, 3, 4)] = 9,
			[new DateTime(2026, 3, 2, 14, 0, 0)] = 3,
			[new DateTime(2026, 2, 10)] = 1,
			[new DateTime(2026, 1, 20)] = 6
		})
		.Add(x => x.Unknown, [new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), new DateTime(2026, 2, 10)])));

	// MokaGate's presence grid: a week of days by hours, the hours still to come unknown, today's row and hour marked, the
	// key as the grid's last row, and its own words for every cell.
	[Fact]
	public void HeatmapGrid_AsMokaGateDrawsIt() => Golden("heatmap-grid-gate", () => Render<MokaHeatmap>(p => p
		.Add(x => x.Layout, MokaHeatmapLayout.Grid)
		.Add(x => x.GridValues, PresenceWeek())
		.Add(x => x.RowLabels, ["Thu 5", "Fri 6", "Sat 7", "Sun 8", "Mon 9", "Tue 10", "Today"])
		.Add(x => x.RowFigures, ["4 h", "11 h 20 min", "", "0 min", "2 h 5 min", "9 h", "3 h so far"])
		.Add(x => x.ColumnLabels, MokaHeatmap.HourNames)
		.Add(x => x.CellText, (row, hour, minutes) => double.IsNaN(minutes)
			? null
			: hour == 3 ? null : string.Create(CultureInfo.InvariantCulture, $"Day {row}, {hour:00}:00, {minutes:0} min"))
		.Add(x => x.MaxGridValue, 60d)
		.Add(x => x.UnknownText, "later today")
		.Add(x => x.Fill, true)
		.Add(x => x.MarkedRows, [6])
		.Add(x => x.MarkedCells, [new MokaHeatmapCell(6, 14)])
		.Add(x => x.ShowScale, true)
		.Add(x => x.ScalePlacement, MokaHeatmapScalePlacement.GridEnd)
		.Add(x => x.LessText, "A few minutes")
		.Add(x => x.MoreText, "The whole hour")
		.Add(x => x.Label, "On the network 41 h this week")));

	[Fact]
	public void HeatmapGrid_SizedWithTheAccentRamp() => Golden("heatmap-grid-sized", () => Render<MokaHeatmap>(p => p
		.Add(x => x.Layout, MokaHeatmapLayout.Grid)
		.Add(x => x.GridValues,
		[
			[0d, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11],
			[12d, double.NaN, 3.5, 0, -1, 7.25, 1, 2, 3, 4, 5],
			[1d]
		])
		.Add(x => x.RowLabels, ["A row whose name is long", "Short"])
		.Add(x => x.ColumnLabels, ["January", "February", "March", "April", "May", "June", "July", "August", "September",
			"October", "November", "December"])
		.Add(x => x.CellSize, "1.5rem")
		.Add(x => x.Ramp, MokaHeatmapRamp.Accent)
		.Add(x => x.Look, MokaChartLook.Quiet)
		.Add(x => x.ShowScale, true)
		.Add(x => x.ScalePlacement, MokaHeatmapScalePlacement.Start)
		.Add(x => x.Class, "mine")
		.Add(x => x.Style, "max-width: 40rem")
		.Add(x => x.Id, "sized-grid")
		.Add(x => x.Margin, MokaSpacingScale.Md)));

	[Fact]
	public void HeatmapGrid_Plain() => Golden("heatmap-grid-plain", () => Render<MokaHeatmap>(p => p
		.Add(x => x.Layout, MokaHeatmapLayout.Grid)
		.Add(x => x.GridValues, [[0d, 2, 4], [6d, 8, double.NaN]])
		.Add(x => x.CellSize, "var(--app-cell)")
		.Add(x => x.ScalePlacement, MokaHeatmapScalePlacement.Center)
		.Add(x => x.ShowScale, true)));

	[Fact]
	public void Heatmap_CalendarInSteps() => Golden("heatmap-calendar-steps", () => Render<MokaHeatmap>(p => p
		.Add(x => x.Weeks, 4)
		.Add(x => x.EndDate, new DateTime(2026, 3, 4))
		.Add(x => x.Look, MokaChartLook.Quiet)
		.Add(x => x.MaxValue, 10)
		.Add(x => x.ShowMonthLabels, false)
		.Add(x => x.ShowScale, true)
		.Add(x => x.ScalePlacement, MokaHeatmapScalePlacement.Center)
		.Add(x => x.LessText, "Quiet")
		.Add(x => x.MoreText, "Busy")
		.Add(x => x.Data, new Dictionary<DateTime, int>
		{
			[new DateTime(2026, 3, 4)] = 10,
			[new DateTime(2026, 3, 1)] = 4,
			[new DateTime(2026, 2, 12)] = 1
		})));

	private static IReadOnlyList<IReadOnlyList<double>> PresenceWeek() =>
	[
		.. Enumerable.Range(0, 7).Select(day => (IReadOnlyList<double>)
		[
			.. Enumerable.Range(0, 24).Select(hour => day == 6 && hour > 14
				? double.NaN
				: (double)((day * 7 + hour * 11) % 61))
		])
	];

	private static void Golden<TComponent>(string name, Func<IRenderedComponent<TComponent>> render)
		where TComponent : IComponent
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		CultureInfo uiCulture = CultureInfo.CurrentUICulture;
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
		try
		{
			string markup = Normalize(render().Markup);
			string file = Path.Combine(GoldenFolder(), $"{name}.html");

			if (Environment.GetEnvironmentVariable(RecordVariable) == "1")
			{
				File.WriteAllText(file, markup);
				return;
			}

			Assert.True(File.Exists(file), $"No golden for {name}. Run the tests once with {RecordVariable}=1 to write it.");
			Assert.Equal(File.ReadAllText(file).ReplaceLineEndings("\n"), markup);
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = uiCulture;
		}
	}

	private static string Normalize(string markup)
	{
		string withoutScopes = ScopeAttribute().Replace(markup, string.Empty);
		string withoutClipIds = ClipId().Replace(withoutScopes, "moka-linechart-clip-ID");
		return withoutClipIds.Replace("\r\n", "\n", StringComparison.Ordinal);
	}

	private static string GoldenFolder()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				return Path.Combine(folder.FullName, "tests", "Moka.Red.Data.Tests", "Charts", "Goldens");
			}
		}

		Assert.Fail("The tests are not running inside the repository, so there are no goldens to read.");
		return string.Empty;
	}

	[GeneratedRegex(@" b-[a-z0-9]{10}(="""")?")]
	private static partial Regex ScopeAttribute();

	[GeneratedRegex("moka-linechart-clip-[0-9a-f]{32}")]
	private static partial Regex ClipId();
}
