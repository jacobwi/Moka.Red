using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

public sealed class MokaGanttChartTests : BunitContext
{
	private static readonly DateTimeOffset Midnight = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);

	private static readonly MokaGanttRow[] Jobs =
	[
		new("Photos backup", Midnight.AddHours(1)) { End = Midnight.AddMinutes(160) },
		new("Index rebuild", Midnight.AddMinutes(120)) { End = Midnight.AddMinutes(165), Status = MokaGanttStatus.Failed, Detail = "Disk full" },
		new("Offsite sync", Midnight.AddMinutes(170)) { Status = MokaGanttStatus.Running },
		new("Report", Midnight.AddMinutes(318)) { End = Midnight.AddMinutes(330), Status = MokaGanttStatus.Planned }
	];

	public MokaGanttChartTests() => this.Static();

	[Fact]
	public void DrawsABarPerJob_OnTheSharedClock()
	{
		IRenderedComponent<MokaGanttChart> cut = Draw(Midnight.AddMinutes(310));

		IReadOnlyList<IElement> lanes = cut.FindAll(".moka-gantt__lane");
		Assert.Equal(4, lanes.Count);
		Assert.Equal("0%", RowChartKit.Var(lanes[0], "--_ga-from"));
		Assert.Equal(100d / 270 * 100, RowChartKit.Percent(RowChartKit.Var(lanes[0], "--_ga-len")), 2);
		Assert.Equal("100%", RowChartKit.Var(lanes[3], "--_ga-to"));
		Assert.Equal(MokaChartX.FromDateTimeOffset(Midnight.AddHours(1)).Value, cut.Instance.RowTimeRange!.Value.From.ToUnixTimeMilliseconds());
	}

	[Fact]
	public void EachBar_HasItsStatusColor_AnIcon_AndAWord()
	{
		IRenderedComponent<MokaGanttChart> cut = Draw(Midnight.AddMinutes(310));

		IReadOnlyList<IElement> lanes = cut.FindAll(".moka-gantt__lane");
		Assert.Contains("moka-chart-tone-success", lanes[0].ClassList);
		Assert.Contains("moka-chart-tone-error", lanes[1].ClassList);
		Assert.Contains("moka-chart-tone-info", lanes[2].ClassList);
		Assert.Contains("moka-chart-muted", lanes[3].ClassList);
		Assert.Contains("moka-gantt__bar--running", lanes[2].QuerySelector(".moka-gantt__bar")!.ClassList);
		Assert.Equal(["Done", "Failed", "Running", "Planned"], cut.FindAll(".moka-gantt__word-text").Select(word => word.TextContent));
		Assert.Equal(4, cut.FindAll(".moka-gantt__word svg").Count);
		Assert.Equal(["Planned", "Running", "Done", "Failed"], cut.Instance.SeriesInfo.Select(s => s.Name));
		Assert.Equal(["planned", "running", "done", "failed"], cut.Instance.SeriesInfo.Select(s => s.Key));
	}

	[Fact]
	public void AJobWithoutAnEnd_RunsUntilNow_AndALineMarksNow()
	{
		IRenderedComponent<MokaGanttChart> cut = Draw(Midnight.AddMinutes(310));

		IElement now = cut.Find(".moka-chart-plot__rlines .moka-gantt__now");
		Assert.Equal(250d / 270 * 100, RowChartKit.Percent(RowChartKit.Var(now, "--_ga-now")), 2);
		Assert.Equal(250d / 270 * 100, RowChartKit.Percent(RowChartKit.Var(cut.FindAll(".moka-gantt__lane")[2], "--_ga-to")), 2);
		Assert.StartsWith("Now ", cut.Find(".moka-chart-plot__rname-space").TextContent, StringComparison.Ordinal);

		cut.Render(p => p.Add(x => x.ShowNow, false));
		Assert.Empty(cut.FindAll(".moka-gantt__now"));
	}

	[Fact]
	public void TheTooltip_SaysTheSpan_HowLongItTook_AndItsStatus()
	{
		IRenderedComponent<MokaGanttChart> cut = Draw(Midnight.AddMinutes(310));

		ChartActiveInfo index = cut.Instance.DescribeActive(0, -1, 1, null)!;
		Assert.Equal("Index rebuild", index.Heading);
		Assert.Equal("45 min", index.Rows[0].Name);
		Assert.Contains(" to ", index.Rows[0].ValueText, StringComparison.Ordinal);
		Assert.Equal(["Failed: Disk full"], index.Notes);
		Assert.Equal(Midnight.AddMinutes(120), index.Context.Points[0].Time);
		Assert.Equal(TimeSpan.FromMinutes(45).TotalMilliseconds, index.Context.Points[0].Value);
	}

	[Fact]
	public void HidingAStatus_TakesItsJobsAway()
	{
		IRenderedComponent<MokaGanttChart> cut = Draw(Midnight.AddMinutes(310), p => p.Add(x => x.HiddenSeries, ["done", "planned"]));

		Assert.Equal(["Index rebuild", "Offsite sync"], cut.FindAll(".moka-gantt__name").Select(name => name.TextContent));
		Assert.True(cut.Instance.SeriesInfo.Single(s => s.Key == "done").Hidden);
	}

	[Fact]
	public void TheWord_GoesWhereThereIsRoom()
	{
		IRenderedComponent<MokaGanttChart> cut = Draw(Midnight.AddMinutes(310));

		IReadOnlyList<IElement> lanes = cut.FindAll(".moka-gantt__lane");
		Assert.Contains("moka-gantt__lane--after", lanes[0].ClassList);
		Assert.Contains("moka-gantt__lane--before", lanes[3].ClassList);
	}

	[Fact]
	public void StatusText_RenamesEveryStatus()
	{
		IRenderedComponent<MokaGanttChart> cut = Draw(Midnight.AddMinutes(310),
			p => p.Add(x => x.StatusText, status => status == MokaGanttStatus.Done ? "Fertig" : status.ToString()));

		Assert.Equal("Fertig", cut.Find(".moka-gantt__word-text").TextContent);
		Assert.Equal("Fertig", cut.Instance.SeriesInfo.Single(s => s.Key == "done").Name);
	}

	[Fact]
	public void TheTable_WritesTimesForPeople_AndISO8601ForCsv()
	{
		IRenderedComponent<MokaGanttChart> cut = Draw(Midnight.AddMinutes(310));

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(["Name", "From", "To", "Took", "Status"], table.Headers);
		Assert.Equal("2026-11-11T01:00:00.0000000+00:00", table.Raw(0, 1));
		Assert.Equal("", table.Raw(2, 2));
		Assert.Equal("6000", table.Raw(0, 3));
		Assert.Equal("Failed", table.Raw(1, 4));
		Assert.Equal("1 h 40 min", table.Display(0, 3));
	}

	[Fact]
	public void Thresholds_AtATime_RunDownTheRows()
	{
		double deadline = MokaChartX.FromDateTimeOffset(Midnight.AddHours(4)).Value;
		IRenderedComponent<MokaGanttChart> cut = Draw(Midnight.AddMinutes(310),
			p => p.Add(x => x.Annotations, [new MokaChartThreshold(deadline) { Label = "Deadline" }]));

		IElement line = cut.Find(".moka-chart-plot__vthreshold");
		Assert.Equal("Deadline", line.TextContent.Trim());
		Assert.Equal(180d / 270 * 100, RowChartKit.Percent(RowChartKit.Var(line, "--_s")), 2);
	}

	private IRenderedComponent<MokaGanttChart> Draw(DateTimeOffset now,
		Action<ComponentParameterCollectionBuilder<MokaGanttChart>>? more = null) => Render<MokaGanttChart>(p =>
	{
		p.Add(x => x.Rows, Jobs).Add(x => x.Now, now).Add(x => x.TimeZone, TimeZoneInfo.Utc);
		more?.Invoke(p);
	});
}

// A service has to be registered before bUnit hands out the first one, which setting the renderer already does.
public sealed class MokaGanttChartClockTests : BunitContext
{
	[Fact]
	public void WithoutNow_ARegisteredTimeProviderSaysWhenNowIs()
	{
		DateTimeOffset midnight = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
		Services.AddSingleton<TimeProvider>(new FixedTime(midnight.AddMinutes(200)));
		this.Static();
		IRenderedComponent<MokaGanttChart> cut = Render<MokaGanttChart>(p => p
			.Add(x => x.Rows, [new MokaGanttRow("sync", midnight.AddMinutes(170)) { Status = MokaGanttStatus.Running }])
			.Add(x => x.TimeZone, TimeZoneInfo.Utc));

		Assert.Equal("30 min", cut.Instance.DescribeActive(0, -1, 0, null)!.Rows[0].Name);
	}

	private sealed class FixedTime(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}
}
