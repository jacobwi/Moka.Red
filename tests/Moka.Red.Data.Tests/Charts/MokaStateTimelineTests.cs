#pragma warning disable CA5394 // Seeded Random keeps the noisy rows repeatable; nothing here is security related
using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using Moka.Red.Icons;

namespace Moka.Red.Data.Tests.Charts;

public sealed class MokaStateTimelineTests : BunitContext
{
	private static readonly DateTimeOffset Midnight = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
	private static readonly DateTimeOffset Six = Midnight.AddHours(6);

	private static readonly MokaStateRow[] Watched =
	[
		new("api", [
			new MokaStateSpan(Midnight, MokaStateTimeline.UpKey),
			new MokaStateSpan(Midnight.AddHours(2), MokaStateTimeline.DownKey) { Detail = "HTTP 503" },
			new MokaStateSpan(Midnight.AddMinutes(150), MokaStateTimeline.UpKey)
		]) { Key = "api-1", Detail = "eu-west" },
		new("db", [
			new MokaStateSpan(Midnight.AddHours(1), MokaStateTimeline.UpKey) { End = Midnight.AddHours(3) },
			new MokaStateSpan(Midnight.AddHours(3), MokaStateTimeline.MaintenanceKey) { End = Midnight.AddHours(4) },
			new MokaStateSpan(Midnight.AddHours(4), MokaStateTimeline.UpKey)
		]),
		new("cache", [])
	];

	public MokaStateTimelineTests()
	{
		this.Static();
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
	}

	[Fact]
	public void DrawsABandPerRow_WithAMarkPerStretch_OnTheSharedClock()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw();

		IReadOnlyList<IElement> lanes = cut.FindAll(".moka-state-timeline__lane");
		Assert.Equal(3, lanes.Count);
		IElement[] api = [.. lanes[0].QuerySelectorAll("span")];
		Assert.Equal(["up", "down", "up"], api.Select(mark => mark.GetAttribute("data-moka-key")));
		Assert.Equal(["0", "1", "2"], api.Select(mark => mark.GetAttribute("data-i")));
		Assert.Equal(0, Place(cut, api[0]).Left, 2);
		Assert.Equal(100d / 3, Place(cut, api[0]).Width, 2);
		Assert.Equal(100d / 3, Place(cut, api[1]).Left, 2);
		Assert.Equal(100d / 12, Place(cut, api[1]).Width, 2);
		Assert.Equal(["api", "db", "cache"], cut.FindAll(".moka-chart-plot__rname").Select(name => name.TextContent));
	}

	[Fact]
	public void EachState_TakesItsTone_AndTheLegendListsThemWithIcons()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw();

		IElement[] marks = [.. cut.FindAll(".moka-state-timeline__lane")[0].QuerySelectorAll("span")];
		Assert.Contains("moka-stl-good", marks[0].ClassList);
		Assert.Contains("moka-stl-bad", marks[1].ClassList);
		Assert.Contains("moka-chart-hatch", marks[1].ClassList);
		Assert.Equal(["up", "down", "maintenance", "unknown"], cut.Instance.SeriesInfo.Select(s => s.Key));
		Assert.Equal(["Up", "Down", "Maintenance", "Unknown"], cut.Instance.SeriesInfo.Select(s => s.Name));
		Assert.Equal(MokaIcons.Status.Error, cut.Instance.SeriesInfo[1].Icon);
		Assert.Equal("moka-chart-tone-error", cut.Instance.SeriesInfo[1].ColorClass);
		Assert.Equal(4, cut.FindAll(".moka-chart-legend__key--icon svg").Count);
	}

	[Fact]
	public void TimeNoSpanCovers_IsUnknown_AndHatched()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw();

		IElement[] db = [.. cut.FindAll(".moka-state-timeline__lane")[1].QuerySelectorAll("span")];
		Assert.Equal(["unknown", "up", "maintenance", "up"], db.Select(mark => mark.GetAttribute("data-moka-key")));
		Assert.Contains("moka-stl-hatched", db[0].ClassList);
		Assert.DoesNotContain("moka-stl-hatched", db[1].ClassList);
		IElement cache = cut.FindAll(".moka-state-timeline__lane")[2].QuerySelector("span")!;
		Assert.Equal("unknown", cache.GetAttribute("data-moka-key"));
		Assert.Equal(100, Place(cut, cache).Width, 2);
	}

	[Fact]
	public void AWideStretchAfterAnotherState_StartsAfterAHairline()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw();

		IElement[] api = [.. cut.FindAll(".moka-state-timeline__lane")[0].QuerySelectorAll("span")];
		Assert.DoesNotContain("moka-stl-gap", api[0].ClassList);
		Assert.Contains("moka-stl-gap", api[1].ClassList);
		Assert.Contains("moka-stl-l", api[0].ClassList);
		Assert.Contains("moka-stl-r", api[2].ClassList);
	}

	[Fact]
	public void TheTooltip_SaysTheState_HowLong_FromWhenToWhen_AndWhy()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw();

		ChartActiveInfo down = cut.Instance.DescribeActive(0, -1, 1, null)!;
		Assert.Equal("api", down.Heading);
		ChartActiveRow row = Assert.Single(down.Rows);
		Assert.Equal(("Down", "30 min"), (row.Name, row.ValueText));
		Assert.Equal(MokaIcons.Status.Error, row.Icon);
		Assert.Contains(" to ", down.Notes[0], StringComparison.Ordinal);
		Assert.Equal(["HTTP 503", "eu-west"], down.Notes.Skip(1));
		Assert.StartsWith("api, Down 30 min, ", down.CellText, StringComparison.Ordinal);
	}

	[Fact]
	public void APoint_CarriesTheSpan_AndTheRowsKey()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw();

		MokaChartPoint point = cut.Instance.DescribeActive(0, -1, 1, null)!.Context.Focus!;

		Assert.Equal(("down", "Down", 0, "api-1", "api"), (point.SeriesKey, point.SeriesName, point.Index, point.RowKey, point.Category));
		Assert.Equal(Midnight.AddHours(2), point.Time);
		Assert.Equal(MokaChartX.FromDateTimeOffset(Midnight.AddHours(2)).Value, point.X);
		Assert.Equal(TimeSpan.FromMinutes(30).TotalMilliseconds, point.Value);
		Assert.Equal(1, cut.Instance.DescribeActive(0, -1, 3, null)!.Context.Focus!.Index);
	}

	[Fact]
	public void AShortOutage_SurvivesBeingThinnerThanAPixel()
	{
		MokaStateRow row = new("link", [
			new MokaStateSpan(Midnight, MokaStateTimeline.UpKey),
			new MokaStateSpan(Midnight.AddHours(3), MokaStateTimeline.DownKey),
			new MokaStateSpan(Midnight.AddHours(3).AddSeconds(4), MokaStateTimeline.UpKey)
		]);
		IRenderedComponent<MokaStateTimeline> cut = Draw(rows: [row]);

		IElement[] marks = [.. cut.FindAll(".moka-state-timeline__lane > span")];
		Assert.Equal(["up", "down", "up"], marks.Select(mark => mark.GetAttribute("data-moka-key")));
		Assert.Contains("moka-stl-thin", marks[1].ClassList);
		Assert.Equal("4 s", cut.Instance.DescribeActive(0, -1, 1, null)!.Rows[0].ValueText);
	}

	[Fact]
	public void SpansThinnerThanAPixel_MergeIntoMarks_ThatShowTheMostSevere_AndSayWhatTheyHold()
	{
		List<MokaStateSpan> spans = [];
		for (int i = 0; i < 2000; i++)
		{
			string state = (i % 3) switch
			{
				0 => MokaStateTimeline.DownKey,
				1 => MokaStateTimeline.DegradedKey,
				_ => MokaStateTimeline.UpKey
			};
			spans.Add(new MokaStateSpan(Midnight.AddSeconds(i * 10.8), state));
		}

		IRenderedComponent<MokaStateTimeline> cut = Draw(rows: [new MokaStateRow("flappy", spans)]);

		Assert.True(cut.Instance.MeasuresWidth);
		IReadOnlyList<IElement> marks = cut.FindAll(".moka-state-timeline__lane > span");
		Assert.InRange(marks.Count, 1, 400);
		Assert.All(marks, mark => Assert.Equal("down", mark.GetAttribute("data-moka-key")));
		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 0, null)!;
		Assert.Equal(["Down", "Degraded", "Up"], info.Rows.Select(row => row.Name));
		Assert.Equal("down", info.Context.Focus!.SeriesKey);
		Assert.Equal(3, info.Context.Points.Count);
	}

	[Fact]
	public void AWiderPlot_DrawsMoreMarks()
	{
		List<MokaStateSpan> spans = Noisy(2000);
		IRenderedComponent<MokaStateTimeline> cut = Draw(rows: [new MokaStateRow("flappy", spans)]);
		long version = cut.Instance.GeometryVersion;
		int narrow = cut.FindAll(".moka-state-timeline__lane > span").Count;

		cut.InvokeAsync(() => cut.Instance.OnPlotWidth(1600));

		Assert.True(cut.Instance.GeometryVersion > version);
		Assert.True(cut.FindAll(".moka-state-timeline__lane > span").Count > narrow);
	}

	[Fact]
	public void AMeasuredWidthCloseToTheOneDrawn_KeepsTheMarks()
	{
		List<MokaStateSpan> spans = Noisy(2000);
		IRenderedComponent<MokaStateTimeline> cut = Draw(rows: [new MokaStateRow("flappy", spans)]);
		string before = cut.Find(".moka-state-timeline__lane").InnerHtml;

		cut.InvokeAsync(() => cut.Instance.OnPlotWidth(960));
		Assert.Equal(before, cut.Find(".moka-state-timeline__lane").InnerHtml);

		cut.InvokeAsync(() => cut.Instance.OnPlotWidth(1088));
		Assert.NotEqual(before, cut.Find(".moka-state-timeline__lane").InnerHtml);
	}

	[Fact]
	public void ShowUptime_EndsEachRow_WithItsShareOfKnownTimeUp()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw(p => p.Add(x => x.ShowUptime, true));

		Assert.Equal(["91.66%", "80.00%"], cut.FindAll(".moka-chart-plot__rfig").Select(figure => figure.TextContent));
		Assert.Equal("Uptime 80.00%, 1 h unknown", cut.Instance.DescribeActive(0, -1, 4, null)!.Notes[^1]);
		Assert.Equal("Uptime 91.66%", cut.Instance.DescribeActive(0, -1, 0, null)!.Notes[^1]);
	}

	[Fact]
	public void Uptime_NeverRoundsUpTo100()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw();

		Assert.Equal("99.99%", cut.Instance.UptimeText(0.99999));
		Assert.Equal("100%", cut.Instance.UptimeText(1));
		Assert.Equal("0.00%", cut.Instance.UptimeText(0));
	}

	[Fact]
	public void UptimeFormatter_WritesTheFigure()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw(p => p
			.Add(x => x.ShowUptime, true)
			.Add(x => x.UptimeFormatter, share => share.ToString("P1", CultureInfo.InvariantCulture)));

		Assert.Equal("80.0 %", cut.FindAll(".moka-chart-plot__rfig")[1].TextContent);
	}

	[Fact]
	public void HidingAState_LeavesItsTimeEmpty_AndKeepsItInTheLegend()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw(p => p.Add(x => x.HiddenSeries, ["up"]));

		IElement[] api = [.. cut.FindAll(".moka-state-timeline__lane")[0].QuerySelectorAll("span")];
		Assert.Equal(["down"], api.Select(mark => mark.GetAttribute("data-moka-key")));
		Assert.True(cut.Instance.SeriesInfo.Single(s => s.Key == "up").Hidden);
	}

	[Fact]
	public void AHighlight_MutesTheOtherStates()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw(p => p.Add(x => x.Highlight, "down"));

		IElement[] api = [.. cut.FindAll(".moka-state-timeline__lane")[0].QuerySelectorAll("span")];
		Assert.Contains("moka-chart-muted", api[0].ClassList);
		Assert.Contains("moka-stl-bad", api[1].ClassList);
	}

	[Fact]
	public void States_NameTheKeys_WithTheirOwnTones_ColorsAndIcons()
	{
		MokaTimelineState[] states =
		[
			new("ok", "Healthy") { Tone = MokaStateTone.Good },
			new("slow", "Slow") { Tone = MokaStateTone.Warn, ColorValue = "#ffaa00", Icon = MokaIcons.Status.Clock },
			new("bad", "Broken") { Tone = MokaStateTone.Bad, ColorValue = "red; top: 0" }
		];
		MokaStateRow row = new("probe", [
			new MokaStateSpan(Midnight, "ok"), new MokaStateSpan(Midnight.AddHours(1), "slow"),
			new MokaStateSpan(Midnight.AddHours(2), "bad"), new MokaStateSpan(Midnight.AddHours(3), "paused")
		]);

		IRenderedComponent<MokaStateTimeline> cut = Draw(p => p.Add(x => x.States, states), [row]);

		IElement[] marks = [.. cut.FindAll(".moka-state-timeline__lane > span")];
		Assert.Equal("#ffaa00", RowChartKit.Var(marks[1], "--moka-chart-color"));
		Assert.Null(RowChartKit.Var(marks[2], "--moka-chart-color"));
		Assert.Contains("moka-stl-off", marks[3].ClassList);
		Assert.Equal(["Healthy", "Slow", "Broken", "paused"], cut.Instance.SeriesInfo.Select(s => s.Name));
		Assert.Equal(MokaIcons.Status.Clock, cut.Instance.SeriesInfo[1].Icon);
	}

	[Fact]
	public void Severity_DecidesWhichOverlappingStateShows()
	{
		MokaTimelineState[] states =
		[
			new("up", "Up"), new("down", "Down") { Tone = MokaStateTone.Bad },
			new("window", "Planned") { Tone = MokaStateTone.Info, Severity = 100 }
		];
		MokaStateRow row = new("db", [
			new MokaStateSpan(Midnight, "up"),
			new MokaStateSpan(Midnight.AddHours(1), "window") { End = Midnight.AddHours(2) },
			new MokaStateSpan(Midnight.AddMinutes(70), "down") { End = Midnight.AddMinutes(80) },
			new MokaStateSpan(Midnight.AddHours(2), "up")
		]);

		IRenderedComponent<MokaStateTimeline> cut = Draw(p => p.Add(x => x.States, states), [row]);

		Assert.Equal(["up", "window", "up"], cut.FindAll(".moka-state-timeline__lane > span").Select(mark => mark.GetAttribute("data-moka-key")));
	}

	[Fact]
	public void TimelineText_RenamesTheBuiltInStates_AndTheUptime()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw(p => p
			.Add(x => x.ShowUptime, true)
			.Add(x => x.TimelineText, MokaStateTimelineText.Default with { Down = "Nede", Uptime = "Oppetid" }));

		Assert.Contains("Nede", cut.Instance.SeriesInfo.Select(s => s.Name));
		Assert.StartsWith("Oppetid ", cut.Instance.DescribeActive(0, -1, 0, null)!.Notes[^1], StringComparison.Ordinal);
	}

	[Fact]
	public void ALineMarksNow_AndItsTimeSitsOverTheNames()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw(p => p.Add(x => x.To, Midnight.AddHours(8)));

		IElement now = cut.Find(".moka-chart-plot__rlines .moka-state-timeline__now");
		Assert.Equal(75, RowChartKit.Percent(RowChartKit.Var(now, "--_st-now")), 2);
		Assert.StartsWith("Now ", cut.Find(".moka-chart-plot__rname-space").TextContent, StringComparison.Ordinal);
		(double left, double width) = Place(cut, cut.FindAll(".moka-state-timeline__lane")[0].QuerySelectorAll("span")[^1]);
		Assert.Equal(75, left + width, 2);

		cut.Render(p => Parameters(p, Watched, q => q.Add(x => x.To, Midnight.AddHours(8)).Add(x => x.ShowNow, false)));
		Assert.Empty(cut.FindAll(".moka-state-timeline__now"));
	}

	[Fact]
	public void TheTable_ListsTheSpans_TimesForPeople_AndISO8601ForCsv()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw();

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal(["Name", "State", "From", "To", "Duration"], table.Headers);
		Assert.Equal(8, table.RowCount);
		Assert.Equal(["api", "Down", "30 min"], new[] { table.Display(1, 0), table.Display(1, 1), table.Display(1, 4) });
		Assert.Equal("down", table.Raw(1, 1));
		Assert.Equal("2026-11-11T02:00:00.0000000+00:00", table.Raw(1, 2));
		Assert.Equal("1800", table.Raw(1, 4));
	}

	[Fact]
	public void OnTheStandardLook_EachMarkCarriesItsOwnTitle()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw();

		IElement down = cut.FindAll(".moka-state-timeline__lane > span")[1];
		Assert.StartsWith("api, Down 30 min", down.GetAttribute("title"), StringComparison.Ordinal);
		Assert.Null(cut.Find(".moka-state-timeline__lane").GetAttribute("title"));
	}

	[Fact]
	public void ADenseRow_IsTitledOnce_ByWhatItHolds()
	{
		List<MokaStateSpan> spans = [.. Enumerable.Range(0, 200).Select(i => new MokaStateSpan(Midnight.AddMinutes(i * 1.8),
			i % 2 == 0 ? MokaStateTimeline.UpKey : MokaStateTimeline.DegradedKey))];

		IRenderedComponent<MokaStateTimeline> cut = Draw(rows: [new MokaStateRow("noisy", spans)]);

		Assert.Equal(200, cut.FindAll(".moka-state-timeline__lane > span").Count);
		Assert.Null(cut.FindAll(".moka-state-timeline__lane > span")[0].GetAttribute("title"));
		Assert.Equal("noisy: Degraded 3 h, Up 3 h", cut.Find(".moka-state-timeline__lane").GetAttribute("title"));
	}

	[Fact]
	public void AWindowThatSlides_MovesTheRoot_AndLeavesTheMarksAlone()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw(p => p.Add(x => x.From, Midnight).Add(x => x.To, Six));
		string?[] before = [.. cut.FindAll(".moka-state-timeline__lane > span").Select(mark => mark.GetAttribute("style"))];
		string? offset = RowChartKit.Var(cut.Find(".moka-state-timeline"), "--_st-o");

		cut.Render(p => Parameters(p, Watched, q => q.Add(x => x.From, Midnight.AddMinutes(1)).Add(x => x.To, Six.AddMinutes(1))));

		string?[] after = [.. cut.FindAll(".moka-state-timeline__lane > span").Select(mark => mark.GetAttribute("style"))];
		Assert.NotEqual(offset, RowChartKit.Var(cut.Find(".moka-state-timeline"), "--_st-o"));
		Assert.NotEqual(before[0], after[0]);
		Assert.Equal(before[1..3], after[1..3]);
		Assert.Equal(before.Length, after.Length);
	}

	[Fact]
	public void TheSameRows_InANewList_DrawNothingNew_ButAChangedSpanDoes()
	{
		List<MokaStateSpan> spans = [new(Midnight, MokaStateTimeline.UpKey), new(Midnight.AddHours(1), MokaStateTimeline.DownKey)];
		IRenderedComponent<MokaStateTimeline> cut = Draw(rows: [new MokaStateRow("a", spans)]);
		long version = cut.Instance.GeometryVersion;

		cut.Render(p => Parameters(p, [new MokaStateRow("a", [.. spans])]));
		Assert.Equal(version, cut.Instance.GeometryVersion);

		spans[1] = spans[1] with { State = MokaStateTimeline.DegradedKey };
		cut.Render(p => Parameters(p, [new MokaStateRow("a", spans)]));
		Assert.True(cut.Instance.GeometryVersion > version);
		Assert.Equal("degraded", cut.FindAll(".moka-state-timeline__lane > span")[1].GetAttribute("data-moka-key"));
	}

	[Fact]
	public void WithADataVersion_TheRowsAreTakenByReference()
	{
		List<MokaStateSpan> spans = [new(Midnight, MokaStateTimeline.UpKey), new(Midnight.AddHours(1), MokaStateTimeline.DownKey)];
		MokaStateRow[] rows = [new MokaStateRow("a", spans)];
		IRenderedComponent<MokaStateTimeline> cut = Draw(p => p.Add(x => x.DataVersion, 1), rows);

		spans[1] = spans[1] with { State = MokaStateTimeline.DegradedKey };
		cut.Render(p => Parameters(p, rows, q => q.Add(x => x.DataVersion, 1)));
		Assert.Equal("down", cut.FindAll(".moka-state-timeline__lane > span")[1].GetAttribute("data-moka-key"));

		cut.Render(p => Parameters(p, rows, q => q.Add(x => x.DataVersion, 2)));
		Assert.Equal("degraded", cut.FindAll(".moka-state-timeline__lane > span")[1].GetAttribute("data-moka-key"));
	}

	[Fact]
	public void AReversedAxis_RunsFromRightToLeft()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw(p => p.Add(x => x.XAxis, new MokaChartAxis { Reverse = true }));

		IElement first = cut.FindAll(".moka-state-timeline__lane")[0].QuerySelector("span")!;
		Assert.Contains("moka-state-timeline--reverse", cut.Find(".moka-state-timeline").ClassList);
		Assert.Equal(0, Place(cut, first).Left, 2);
		Assert.Contains("moka-stl-r", first.ClassList);
	}

	[Fact]
	public void FromAndTo_FallBackToTheXAxisTimes()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw(p => p.Add(x => x.XAxis,
			new MokaChartAxis { MinTime = Midnight.AddHours(-6), MaxTime = Six }));

		Assert.Equal((Midnight.AddHours(-6), Six), cut.Instance.RowTimeRange!.Value);
		IElement first = cut.FindAll(".moka-state-timeline__lane")[0].QuerySelector("span")!;
		Assert.Equal("unknown", first.GetAttribute("data-moka-key"));
		Assert.Equal(50, Place(cut, first).Width, 2);
	}

	[Fact]
	public void ItFillsItsContainer()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw();

		Assert.Contains("moka-fill-width", cut.Find(".moka-state-timeline").ClassList);
		Assert.Equal("img", cut.Find(".moka-state-timeline").GetAttribute("role"));
		Assert.Equal("State timeline", cut.Find(".moka-state-timeline").GetAttribute("aria-label"));
	}

	[Fact]
	public void OnTheOriginalDesign_ItKeepsTheStandardLook()
	{
		IRenderedComponent<MokaStateTimeline> cut = Render<MokaStateTimeline>(p => p
			.AddCascadingValue(MokaTheme.Dark)
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Rows, Watched).Add(x => x.Now, Six).Add(x => x.TimeZone, TimeZoneInfo.Utc));

		Assert.Contains("moka-chart-look-standard", cut.Find(".moka-state-timeline").ClassList);
		Assert.NotNull(cut.FindAll(".moka-state-timeline__lane > span")[0].GetAttribute("title"));
	}

	// Where a mark is drawn, as percentages of the track: its quanta from the origin, less the axis's start, times the
	// width of a quantum. On a reversed axis the left is counted from the right.
	private static (double Left, double Width) Place(IRenderedComponent<MokaStateTimeline> cut, IElement mark)
	{
		IElement root = cut.Find(".moka-state-timeline");
		double offset = Number(RowChartKit.Var(root, "--_st-o"));
		double scale = RowChartKit.Percent(RowChartKit.Var(root, "--_st-k"));
		return ((Number(RowChartKit.Var(mark, "--_s")) - offset) * scale, Number(RowChartKit.Var(mark, "--_w")) * scale);
	}

	private static double Number(string? text) => double.Parse(text ?? "NaN", CultureInfo.InvariantCulture);

	// Spans of random length and state over the six hours, about 2,000 of them: thin and wide stretches mixed.
	private static List<MokaStateSpan> Noisy(int count)
	{
		Random random = new(11);
		double average = Six.Subtract(Midnight).TotalSeconds / count;
		List<MokaStateSpan> spans = [];
		for (double at = 0; at < Six.Subtract(Midnight).TotalSeconds; at += average * (0.2 + (random.NextDouble() * 1.6)))
		{
			double roll = random.NextDouble();
			spans.Add(new MokaStateSpan(Midnight.AddSeconds(at),
				roll < 0.1 ? MokaStateTimeline.DownKey : roll < 0.3 ? MokaStateTimeline.DegradedKey : MokaStateTimeline.UpKey));
		}

		return spans;
	}

	private IRenderedComponent<MokaStateTimeline> Draw(Action<ComponentParameterCollectionBuilder<MokaStateTimeline>>? more = null,
		IReadOnlyList<MokaStateRow>? rows = null) => Render<MokaStateTimeline>(p => Parameters(p, rows ?? Watched, more));

	private static void Parameters(ComponentParameterCollectionBuilder<MokaStateTimeline> p, IReadOnlyList<MokaStateRow> rows,
		Action<ComponentParameterCollectionBuilder<MokaStateTimeline>>? more = null)
	{
		p.Add(x => x.Rows, rows).Add(x => x.Now, Six).Add(x => x.TimeZone, TimeZoneInfo.Utc);
		more?.Invoke(p);
	}
}

// On a page that talks to the chart's script: the surface lets the pointer through to the marks, which carry data-i, and
// the tooltip keys each state with its icon.
public sealed class MokaStateTimelineInteractiveTests : BunitContext
{
	private static readonly DateTimeOffset Midnight = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);

	public MokaStateTimelineInteractiveTests() => this.InteractiveFrame();

	[Fact]
	public void TheSurface_LetsThePointerThroughToTheSpans()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw();

		IElement surface = cut.Find(".moka-chart-surface");
		Assert.Contains("moka-chart-surface--hit", surface.ClassList);
		Assert.Equal("3", surface.GetAttribute("data-moka-count"));
		Assert.Equal("figure", cut.Find(".moka-state-timeline").GetAttribute("role"));
		Assert.Null(cut.FindAll(".moka-state-timeline__lane > span")[0].GetAttribute("title"));
	}

	[Fact]
	public void TheTooltip_KeysTheStateWithItsIcon()
	{
		IRenderedComponent<MokaStateTimeline> cut = Draw();

		cut.InvokeAsync(() => cut.Instance.OnActive(0, -1, 1, false));

		IElement row = cut.Find(".moka-chart-tip .moka-chart-tip__row");
		Assert.NotNull(row.QuerySelector(".moka-chart-tip__icon svg"));
		Assert.Null(row.QuerySelector(".moka-chart-tip__key"));
		Assert.Equal("Down", row.QuerySelector(".moka-chart-tip__name")!.TextContent);
	}

	[Fact]
	public void AClick_ReportsTheSpanAndItsRow()
	{
		MokaChartPoint? clicked = null;
		IRenderedComponent<MokaStateTimeline> cut = Draw(p => p.Add(x => x.OnPointClick, point => clicked = point));

		cut.InvokeAsync(() => cut.Instance.OnSelect(0, -1, 1));

		cut.WaitForAssertion(() => Assert.Equal(("down", "a"), (clicked?.SeriesKey, clicked?.RowKey)));
	}

	private IRenderedComponent<MokaStateTimeline> Draw(Action<ComponentParameterCollectionBuilder<MokaStateTimeline>>? more = null) =>
		Render<MokaStateTimeline>(p =>
		{
			p.Add(x => x.Rows, [
					new MokaStateRow("a", [
						new MokaStateSpan(Midnight, MokaStateTimeline.UpKey),
						new MokaStateSpan(Midnight.AddHours(1), MokaStateTimeline.DownKey),
						new MokaStateSpan(Midnight.AddHours(2), MokaStateTimeline.UpKey)
					])
				])
				.Add(x => x.Now, Midnight.AddHours(3))
				.Add(x => x.TimeZone, TimeZoneInfo.Utc)
				.Add(x => x.Look, MokaChartLook.Quiet)
				.AddCascadingValue(MokaTheme.Red);
			more?.Invoke(p);
		});
}

// A service has to be registered before bUnit hands out the first one, which setting the renderer already does.
public sealed class MokaStateTimelineClockTests : BunitContext
{
	[Fact]
	public void WithoutNow_ARegisteredTimeProviderSaysWhenNowIs()
	{
		DateTimeOffset midnight = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
		Services.AddSingleton<TimeProvider>(new FixedTime(midnight.AddMinutes(90)));
		this.Static();
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		IRenderedComponent<MokaStateTimeline> cut = Render<MokaStateTimeline>(p => p
			.Add(x => x.Rows, [new MokaStateRow("a", [new MokaStateSpan(midnight.AddMinutes(60), MokaStateTimeline.DownKey)])])
			.Add(x => x.TimeZone, TimeZoneInfo.Utc));

		Assert.Equal("30 min", cut.Instance.DescribeActive(0, -1, 0, null)!.Rows[0].ValueText);
	}

	private sealed class FixedTime(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}
}
