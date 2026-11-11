using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     A time axis: pinned to a range whatever the data, never writing the same tick twice in a row, a tooltip time
///     written apart from the ticks on the clock the app picks, and bars that stand at their times.
/// </summary>
public sealed partial class ChartTimeAxisTests : BunitContext
{
	private static readonly DateTimeOffset Three = new(2026, 11, 3, 15, 0, 0, TimeSpan.Zero);

	public ChartTimeAxisTests() => this.Static();

	[Fact]
	public void APinnedTimeAxis_SpansItsRange_WhateverTheData()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		IRenderedComponent<MokaLineChart> cut = Line(MinuteOfSeconds(), new MokaChartAxis { MinTime = Three, MaxTime = Three.AddHours(1) });

		ChartXFrame frame = cut.Instance.XFrame();
		Assert.Equal(Ms(Three), frame.From);
		Assert.Equal(Ms(Three.AddHours(1)), frame.To);
		IReadOnlyList<PlotLabel> labels = cut.Instance.ColumnPlotModel(0).X.Labels;
		Assert.Equal("15:00", labels[0].Text);
		Assert.Equal("16:00", labels[^1].Text);
		AssertNoRepeats(labels);
	}

	[Fact]
	public void TheSuggestedEnds_OnlyWidenTheAxis()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		MokaChartAxis axis = new() { SuggestedMinTime = Three, SuggestedMaxTime = Three.AddMinutes(50) };

		ChartXFrame inside = Line(MinuteOfSeconds(), axis).Instance.XFrame();
		ChartXFrame past = Line([.. Enumerable.Range(0, 7).Select(i => Three.AddMinutes(-10 + (i * 15)))], axis).Instance.XFrame();

		Assert.Equal((Ms(Three), Ms(Three.AddMinutes(50))), (inside.From, inside.To));
		Assert.Equal((Ms(Three.AddMinutes(-10)), Ms(Three.AddMinutes(80))), (past.From, past.To));
	}

	[Fact]
	public void AMinuteOfData_WrittenAsHoursAndMinutes_NeverRepeatsATick()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		IRenderedComponent<MokaLineChart> cut = Line(MinuteOfSeconds(), new MokaChartAxis { TimeFormat = "HH:mm" });

		IReadOnlyList<PlotLabel> labels = cut.Instance.ColumnPlotModel(0).X.Labels;
		Assert.Equal(["15:35", "15:36"], labels.Select(label => label.Text));
	}

	[Fact]
	public void Hour12_WritesTheTicksAndTheTooltipOnATwelveHourClock()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		IRenderedComponent<MokaLineChart> cut = Line(MinuteOfSeconds(), new MokaChartAxis { Hour12 = true });

		Assert.Equal("3:35:10 PM", Heading(cut, 1));
		Assert.All(cut.Instance.ColumnPlotModel(0).X.Labels, label => Assert.EndsWith("PM", label.Text, StringComparison.Ordinal));
	}

	[Fact]
	public void ATwelveHourTimeFormat_SetsTheTooltipsClockToo()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		IRenderedComponent<MokaLineChart> cut = Line(Hourly(4), new MokaChartAxis { TimeFormat = "h:mm tt" });

		Assert.Equal("4:00 PM", Heading(cut, 1));
	}

	[Fact]
	public void TooltipTimeFormat_WritesTheHeading_WhateverTheFormatterWritesOnTheTicks()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		IRenderedComponent<MokaLineChart> cut = Line(Hourly(4), new MokaChartAxis
		{
			Formatter = _ => "tick",
			TooltipTimeFormat = "ddd h:mm tt"
		});

		Assert.Equal("Tue 4:00 PM", Heading(cut, 1));
		Assert.All(cut.Instance.ColumnPlotModel(0).X.Labels, label => Assert.Equal("tick", label.Text));
	}

	[Fact]
	public void AMidnightPoint_OnAChartOfDays_StillSaysItsTime()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		DateTimeOffset[] days = [.. Enumerable.Range(0, 10).Select(i => new DateTimeOffset(2026, 11, 1 + i, 0, 0, 0, TimeSpan.Zero))];
		IRenderedComponent<MokaLineChart> cut = Line(days, new MokaChartAxis { TimeFormat = "MMM d" });

		Assert.Equal("11/03/2026 00:00", Heading(cut, 2));
	}

	[Fact]
	public void ThePinnedEnds_AreWhereZoomingOutAndTheStripStop()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		JSInterop.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaLineChart> cut = Line(MinuteOfSeconds(), new MokaChartAxis { MinTime = Three, MaxTime = Three.AddHours(1) },
			p => p.Add(x => x.Zoom, MokaChartZoom.Strip));

		Assert.Equal((Ms(Three), Ms(Three.AddHours(1))), cut.Instance.XExtent(0)!.Value);
		Assert.Equal((Ms(Three), Ms(Three.AddHours(1))), (cut.Instance.Strip!.Min, cut.Instance.Strip.Max));
	}

	[Fact]
	public void BarsOnATimeAxis_StandAtTheirTimes_UnderTheirTicks()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		IRenderedComponent<MokaBarChart> cut = Bars(Hourly(6), [5, 8, 3, 9, 4, 7]);

		Assert.True(cut.Instance.PlacesColumns);
		ChartXFrame frame = cut.Instance.XFrame();
		Assert.Equal((Ms(Three.AddMinutes(-30)), Ms(Three.AddHours(5.5))), (frame.From, frame.To));
		IReadOnlyList<PlotLabel> labels = cut.Instance.ColumnPlotModel(0).X.Labels;
		IReadOnlyList<IElement> columns = cut.FindAll(".moka-chart-cols > .moka-chart-col");
		Assert.Equal(6, columns.Count);
		for (int hour = 0; hour < 6; hour++)
		{
			string text = Three.AddHours(hour).ToString("HH:mm", CultureInfo.InvariantCulture);
			PlotLabel label = Assert.Single(labels, l => l.Text == text);
			(double start, double width) = Slot(columns[hour]);
			Assert.Equal(label.Share * 100, start + (width / 2), 2);
			Assert.Equal(100d / 6, width, 2);
		}
	}

	[Fact]
	public void AMissingTime_LeavesAGap_AndNoBarGrowsIntoIt()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		DateTimeOffset[] times = [Three, Three.AddHours(1), Three.AddHours(3), Three.AddHours(4)];
		IRenderedComponent<MokaBarChart> cut = Bars(times, [5, 8, 3, 9]);

		IReadOnlyList<IElement> columns = cut.FindAll(".moka-chart-cols > .moka-chart-col");
		(double second, double width) = Slot(columns[1]);
		(double third, _) = Slot(columns[2]);
		Assert.Equal(100d / 5, width, 2);
		Assert.Equal(2 * width, third - second, 2);
	}

	[Fact]
	public void ABarsTooltip_SaysItsTime_AndItsPointCarriesIt()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		IRenderedComponent<MokaBarChart> cut = Bars(Hourly(6), [5, 8, 3, 9, 4, 7], p => p.Add(x => x.XAxis, new MokaChartAxis { Hour12 = true }));

		ChartActiveInfo info = cut.Instance.DescribeActive(0, -1, 1, null)!;
		Assert.Equal("4:00 PM", info.Heading);
		MokaChartPoint point = Assert.Single(info.Context.Points);
		Assert.Equal(Three.AddHours(1), point.Time);
		Assert.Equal(Ms(Three.AddHours(1)), point.X);
	}

	[Fact]
	public void BarsAtTimes_GiveTheScriptTheirPlaces()
	{
		BunitJSModuleInterop module = this.Interactive();
		JSInterop.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaBarChart> cut = Bars(Hourly(4), [5, 8, 3, 9], p => p.Add(x => x.Look, MokaChartLook.Quiet));

		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(
			Assert.Single(module.Invocations, i => i.Identifier == "bind").Arguments[2]);
		Assert.Equal(true, options["placed"]);
		ChartHoverData data = cut.Instance.HoverData(0)!;
		Assert.Equal([0.125f, 0.375f, 0.625f, 0.875f], data.X.ToArray());
		Assert.NotNull(cut.Instance.HoverGeometry(0, 0));
	}

	[Fact]
	public void SortedBars_KeepTheirSlots()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		IRenderedComponent<MokaBarChart> cut = Bars(Hourly(3), [5, 8, 3], p => p.Add(x => x.Sort, MokaChartSort.Descending));

		Assert.False(cut.Instance.PlacesColumns);
	}

	[Fact]
	public void TheTable_ListsTheBarsTimes()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		IRenderedComponent<MokaBarChart> cut = Bars(Hourly(3), [5, 8, 3]);

		ChartTableModel table = cut.Instance.TableModel!;
		Assert.Equal("Time", table.Headers[0]);
		Assert.Equal("16:00", table.Display(1, 0));
		Assert.Equal("2026-11-03T16:00:00.0000000+00:00", table.Raw(1, 0));
	}

	[Fact]
	public void TheSameTimes_InANewList_DrawTheBarsNothingAgain()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		IRenderedComponent<MokaBarChart> cut = Bars(Hourly(3), [5, 8, 3]);
		long version = cut.Instance.GeometryVersion;

		cut.Render(p => BarParameters(p, Hourly(3), [5, 8, 3]));
		Assert.Equal(version, cut.Instance.GeometryVersion);

		cut.Render(p => BarParameters(p, Hourly(4), [5, 8, 3]));
		Assert.NotEqual(version, cut.Instance.GeometryVersion);
	}

	[Fact]
	public void AnEvent_IsNamedInItsColumnsTooltip()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		IRenderedComponent<MokaBarChart> placed = Bars(Hourly(4), [5, 8, double.NaN, 9], p => p
			.Add(x => x.Annotations, [new MokaChartEvent(Three.AddHours(2).AddMinutes(10)) { Label = "Restart" }]));
		IRenderedComponent<MokaBarChart> categories = Render<MokaBarChart>(p => p
			.Add(x => x.Series, [new MokaBarSeries { Name = "Tries", Values = [5, 8, 3, 9] }])
			.Add(x => x.Labels, ["a", "b", "c", "d"])
			.Add(x => x.Title, "Tries")
			.Add(x => x.Annotations, [new MokaChartEvent(1) { Label = "Called out" }]));

		Assert.Equal(["Restart"], placed.Instance.DescribeActive(0, -1, 2, null)!.Notes);
		Assert.DoesNotContain("Restart", placed.Instance.DescribeActive(0, -1, 1, null)!.Notes);
		Assert.Contains("Called out", categories.Instance.DescribeActive(0, -1, 1, null)!.Notes);
		Assert.DoesNotContain("Called out", categories.Instance.DescribeActive(0, -1, 2, null)!.Notes);
	}

	[Fact]
	public void AnEvent_IsNamedAtALinesGap()
	{
		using CultureScope culture = new(CultureInfo.InvariantCulture);
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Series, [new MokaLineSeries { Name = "LAN", Values = [1, double.NaN, 3, 4] }])
			.Add(x => x.Times, Hourly(4))
			.Add(x => x.TimeZone, TimeZoneInfo.Utc)
			.Add(x => x.Annotations, [new MokaChartEvent(Three.AddHours(1)) { Label = "Outage" }]));

		Assert.Contains("Outage", cut.Instance.DescribeActive(0, -1, 1, null)!.Notes);
	}

	private static double Ms(DateTimeOffset time) => MokaChartX.FromDateTimeOffset(time).Value;

	private static DateTimeOffset[] Hourly(int count) => [.. Enumerable.Range(0, count).Select(i => Three.AddHours(i))];

	// A router started a minute ago: seven samples ten seconds apart.
	private static DateTimeOffset[] MinuteOfSeconds() => [.. Enumerable.Range(0, 7).Select(i => Three.AddMinutes(35).AddSeconds(i * 10))];

	private static string? Heading(IRenderedComponent<MokaLineChart> cut, int index) => cut.Instance.DescribeActive(0, -1, index, null)?.Heading;

	private static void AssertNoRepeats(IReadOnlyList<PlotLabel> labels)
	{
		for (int i = 1; i < labels.Count; i++)
		{
			Assert.NotEqual(labels[i - 1].Text, labels[i].Text);
		}
	}

	private static (double Start, double Width) Slot(IElement column)
	{
		string style = column.GetAttribute("style") ?? string.Empty;
		return (Percent(StartPattern().Match(style)), Percent(WidthPattern().Match(style)));
	}

	private static double Percent(Match match) =>
		double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);

	private IRenderedComponent<MokaLineChart> Line(DateTimeOffset[] times, MokaChartAxis axis,
		Action<ComponentParameterCollectionBuilder<MokaLineChart>>? more = null) =>
		Render<MokaLineChart>(p =>
		{
			p.Add(x => x.Series, [new MokaLineSeries { Name = "LAN", Values = [.. times.Select((_, i) => (double)(i + 1))] }]);
			p.Add(x => x.Times, times);
			p.Add(x => x.TimeZone, TimeZoneInfo.Utc);
			p.Add(x => x.XAxis, axis);
			more?.Invoke(p);
		});

	private IRenderedComponent<MokaBarChart> Bars(DateTimeOffset[] times, double[] values,
		Action<ComponentParameterCollectionBuilder<MokaBarChart>>? more = null) =>
		Render<MokaBarChart>(p =>
		{
			BarParameters(p, times, values);
			more?.Invoke(p);
		});

	private static ComponentParameterCollectionBuilder<MokaBarChart> BarParameters(ComponentParameterCollectionBuilder<MokaBarChart> p,
		DateTimeOffset[] times, double[] values) => p
		.Add(x => x.Series, [new MokaBarSeries { Name = "Traffic", Values = values }])
		.Add(x => x.Times, times)
		.Add(x => x.TimeZone, TimeZoneInfo.Utc);

	[GeneratedRegex(@"--_cs:\s*(-?[0-9.]+)%")]
	private static partial Regex StartPattern();

	[GeneratedRegex(@"--_cw:\s*(-?[0-9.]+)%")]
	private static partial Regex WidthPattern();

	/// <summary>Sets the culture for a test and puts the old one back.</summary>
	private sealed class CultureScope : IDisposable
	{
		private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

		public CultureScope(CultureInfo culture) => CultureInfo.CurrentCulture = culture;

		public void Dispose() => CultureInfo.CurrentCulture = _previous;
	}
}
