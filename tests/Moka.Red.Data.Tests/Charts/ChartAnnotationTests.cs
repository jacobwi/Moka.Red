using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Enums;
using Moka.Red.Data.Chart;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     Thresholds, bands, the target band, event markers and windows: what the plot draws for each, that the value axis
///     always reaches them, and the notes a tooltip adds for a point.
/// </summary>
public sealed class ChartAnnotationTests : BunitContext
{
	private static readonly string[] Hours = [.. Enumerable.Range(0, 12).Select(i => $"{i * 2:00}:00")];
	private static readonly DateTimeOffset Start = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);

	public ChartAnnotationTests() => this.Static();

	[Fact]
	public void AThreshold_IsADashedLineLabelledAtItsEnd_AndTheAxisReachesIt()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart([10, 40], p => p.Add(x => x.Annotations,
			[new MokaChartThreshold(200) { Label = "Limit 200 ms", Tone = MokaColor.Error }]));

		IElement line = cut.Find(".moka-chart-plot__threshold");
		Assert.Contains("moka-chart-plot__tone--error", line.ClassName, StringComparison.Ordinal);
		Assert.Equal("Limit 200 ms", line.TextContent.Trim());
		Assert.Equal("200", cut.FindAll(".moka-chart-plot__ytick")[^1].TextContent);
		Assert.Equal(100, ChartAxisPlotTests.ShareOf(line), 3);
	}

	[Fact]
	public void AThresholdWithoutALabel_ReadsItsValueAsTheAxisWritesIt()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart([10, 400], p => p
			.Add(x => x.Unit, MokaChartUnit.Milliseconds)
			.Add(x => x.Annotations, [new MokaChartThreshold(200)]));

		Assert.Equal("200 ms", cut.Find(".moka-chart-plot__threshold").TextContent.Trim());
	}

	[Fact]
	public void BandsAndTheTarget_AreSoftBands_TheAxisReaches()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart([10, 40], p => p
			.Add(x => x.TargetLow, 20)
			.Add(x => x.TargetHigh, 60)
			.Add(x => x.Annotations, [new MokaChartBand(5, 15) { Label = "Quiet hours" }]));

		IReadOnlyList<IElement> bands = cut.FindAll(".moka-chart-plot__band");
		Assert.Equal(2, bands.Count);
		Assert.Equal("Quiet hours", bands[1].TextContent.Trim());
		Assert.Equal("60", cut.FindAll(".moka-chart-plot__ytick")[^1].TextContent);
		Assert.Contains("--_s: 33.333%", bands[0].GetAttribute("style"), StringComparison.Ordinal);
		Assert.Contains("--_w: 66.667%", bands[0].GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void Notes_SayHowFarOverAThreshold_AndWhereABandIs()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart([10, 400], p => p
			.Add(x => x.Unit, MokaChartUnit.Milliseconds)
			.Add(x => x.TargetLow, 100)
			.Add(x => x.TargetHigh, 250)
			.Add(x => x.Annotations, [new MokaChartThreshold(200) { Label = "200 ms" }]));
		MokaCartesianChartBase chart = cut.Instance.Base;

		Assert.Equal(["Inside target"], chart.AnnotationNotes(150));
		Assert.Equal(["12 ms above target", "62 ms over the 200 ms line"], chart.AnnotationNotes(262));
		Assert.Equal(["40 ms below target"], chart.AnnotationNotes(60));
	}

	[Fact]
	public void Notes_UseTheChartsWords()
	{
		MokaChartText words = MokaChartText.Default with { InsideTarget = "Im Ziel" };
		IRenderedComponent<ChartAxisTestChart> cut = Chart([10, 40], p => p
			.Add(x => x.Text, words)
			.Add(x => x.Annotations, [new MokaChartBand(0, 50)]));

		Assert.Equal(["Im Ziel"], cut.Instance.Base.AnnotationNotes(20));
	}

	[Fact]
	public void EventMarkers_AreNamedButtons_InATooltip_UnderThePlot()
	{
		IRenderedComponent<ChartAxisTestChart> cut = TimeChart(
			new MokaChartEvent(Start.AddHours(9)) { Label = "Deploy 0.3.23", Detail = "Rolled back the cache", Tone = MokaColor.Info },
			new MokaChartEvent(Start.AddHours(12)) { Label = "Incident", To = Start.AddHours(13), Shape = MokaChartMarker.Triangle });

		IReadOnlyList<IElement> markers = cut.FindAll("button.moka-chart-plot__marker");
		Assert.Equal(["Deploy 0.3.23, 09:00", "Incident, 12:00 to 13:00"], markers.Select(m => m.GetAttribute("aria-label")));
		Assert.All(markers, m => Assert.Equal("button", m.GetAttribute("type")));
		IReadOnlyList<IRenderedComponent<MokaTooltip>> tips = cut.FindComponents<MokaTooltip>();
		Assert.Equal(2, tips.Count);
		Assert.Contains("Rolled back the cache", tips[0].Markup, StringComparison.Ordinal);
		Assert.Equal(MokaTooltipPosition.Top, tips[0].Instance.Position);
		Assert.Single(cut.FindAll(".moka-chart-plot__span"));
		Assert.Equal(2, cut.FindAll(".moka-chart-plot__guide").Count);
		Assert.Single(cut.FindAll(".moka-chart-plot__lane"));
	}

	[Fact]
	public void TheGuideAndSpan_ShowOnTheMarkersHoverOrFocus_ByCssAlone()
	{
		string css = ChartAxisPlotTests.ScopedCssText();

		Assert.Contains(".moka-chart-plot__event:has(.moka-chart-plot__marker:is(:hover, :focus-visible)) > .moka-chart-plot__guide", css,
			StringComparison.Ordinal);
		Assert.Contains("pointer-events: none;", css, StringComparison.Ordinal);
	}

	[Fact]
	public void EachShape_IsItsOwnFilledIcon()
	{
		MokaChartMarker[] shapes = [MokaChartMarker.Diamond, MokaChartMarker.Triangle, MokaChartMarker.Circle, MokaChartMarker.Square];

		Assert.Equal(4, shapes.Select(ChartAnnotations.IconFor).Select(icon => icon.Name).Distinct().Count());
		Assert.All(shapes.Select(ChartAnnotations.IconFor), icon => Assert.True(icon.Filled));
	}

	[Fact]
	public void AnEventOutsideTheAxis_IsNotDrawn()
	{
		IRenderedComponent<ChartAxisTestChart> cut = TimeChart(new MokaChartEvent(Start.AddDays(3)) { Label = "Later" });

		Assert.Empty(cut.FindAll(".moka-chart-plot__marker"));
	}

	[Fact]
	public void AWindow_IsHatched_LabelledAbove_AndBreaksTheLine()
	{
		IRenderedComponent<ChartAxisTestChart> cut = TimeChart(new MokaChartWindow(Start.AddHours(2), Start.AddHours(4)) { Label = "Maintenance" });

		IElement window = cut.Find(".moka-chart-plot__window");
		Assert.Contains("--_s: 11.111%", window.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("Maintenance", cut.Find(".moka-chart-plot__above .moka-chart-plot__wlabel").TextContent);
		MokaCartesianChartBase chart = cut.Instance.Base;
		Assert.True(chart.BreaksLineAt(MokaChartX.FromDateTimeOffset(Start.AddHours(3)).Value));
		Assert.False(chart.BreaksLineAt(MokaChartX.FromDateTimeOffset(Start.AddHours(5)).Value));
		Assert.Equal(["Maintenance"], chart.AnnotationNotes(10, MokaChartX.FromDateTimeOffset(Start.AddHours(3)).Value));
	}

	[Fact]
	public void AWindowThatKeepsTheLine_DoesntBreakIt()
	{
		IRenderedComponent<ChartAxisTestChart> cut = TimeChart(new MokaChartWindow(Start.AddHours(2), Start.AddHours(4)) { BreakLine = false });

		Assert.False(cut.Instance.Base.BreaksLineAt(MokaChartX.FromDateTimeOffset(Start.AddHours(3)).Value));
		Assert.Empty(cut.FindAll(".moka-chart-plot__above"));
	}

	[Fact]
	public void Notes_NameAnEvent_NearAPoint_OrOverItsSpan()
	{
		IRenderedComponent<ChartAxisTestChart> cut = TimeChart(
			new MokaChartEvent(Start.AddHours(9)) { Label = "Deploy" },
			new MokaChartEvent(Start.AddHours(12)) { Label = "Incident", To = Start.AddHours(13) });
		MokaCartesianChartBase chart = cut.Instance.Base;
		double quarter = TimeSpan.FromMinutes(15).TotalMilliseconds;

		Assert.Equal(["Deploy"], chart.AnnotationNotes(1, MokaChartX.FromDateTimeOffset(Start.AddHours(9).AddMinutes(10)).Value, quarter));
		Assert.Empty(chart.AnnotationNotes(1, MokaChartX.FromDateTimeOffset(Start.AddHours(9).AddMinutes(20)).Value, quarter));
		Assert.Equal(["Incident"], chart.AnnotationNotes(1, MokaChartX.FromDateTimeOffset(Start.AddHours(12.5)).Value));
	}

	[Fact]
	public void TheSameAnnotations_InANewList_DrawNothingAgain()
	{
		double[] values = [10, 40];
		IRenderedComponent<ChartAxisTestChart> cut = Chart(values, p => p.Add(x => x.Annotations, [new MokaChartThreshold(20) { Label = "Limit" }]));
		int rebuilds = cut.Instance.Rebuilds;

		cut.Render(p => p.Add(x => x.Values, values).Add(x => x.Labels, Hours)
			.Add(x => x.Annotations, [new MokaChartThreshold(20) { Label = "Limit" }]));
		Assert.Equal(rebuilds, cut.Instance.Rebuilds);

		cut.Render(p => p.Add(x => x.Values, values).Add(x => x.Labels, Hours)
			.Add(x => x.Annotations, [new MokaChartThreshold(30) { Label = "Limit" }]));
		Assert.Equal(rebuilds + 1, cut.Instance.Rebuilds);
	}

	[Fact]
	public void RepeatedKeys_DontBreakTheRender()
	{
		IRenderedComponent<ChartAxisTestChart> cut = Chart([10, 40], p => p.Add(x => x.Annotations,
			[new MokaChartThreshold(20) { Key = "a" }, new MokaChartThreshold(30) { Key = "a" }]));

		Assert.Equal(2, cut.FindAll(".moka-chart-plot__threshold").Count);
	}

	private IRenderedComponent<ChartAxisTestChart> Chart(double[] values,
		Action<ComponentParameterCollectionBuilder<ChartAxisTestChart>> set)
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
		try
		{
			return Render<ChartAxisTestChart>(p =>
			{
				p.Add(x => x.Values, values);
				p.Add(x => x.Labels, Hours);
				set(p);
			});
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	private IRenderedComponent<ChartAxisTestChart> TimeChart(params MokaChartAnnotation[] annotations)
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
		try
		{
			return Render<ChartAxisTestChart>(p => p
				.Add(x => x.Values, [.. Enumerable.Range(0, 73).Select(i => (double)i)])
				.Add(x => x.Times, ChartAxisPreviewTests.TimesOver(TimeSpan.FromHours(18), 73, Start))
				.Add(x => x.TimeZone, TimeZoneInfo.Utc)
				.Add(x => x.Annotations, annotations));
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}
}
