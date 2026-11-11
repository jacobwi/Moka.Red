using System.Globalization;
using Moka.Red.Core.Charts;

namespace Moka.Red.Core.Tests.Charts;

public class MokaTimeTicksTests
{
	// Central European rules, built here so the test doesn't depend on the machine's zone database: +1, and +2 from
	// the last Sunday of March at 02:00 to the last Sunday of October at 03:00.
	private static readonly TimeZoneInfo Berlin = TimeZoneInfo.CreateCustomTimeZone("Test/Central", TimeSpan.FromHours(1),
		"Central", "Central", "Central Summer",
	[
		TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(DateTime.MinValue.Date, DateTime.MaxValue.Date, TimeSpan.FromHours(1),
			TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 5, DayOfWeek.Sunday),
			TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 10, 5, DayOfWeek.Sunday))
	]);

	private static MokaTimeTickOptions Options(double axisEm = 120, string culture = "") => new()
	{
		Zone = Berlin,
		Culture = culture.Length == 0 ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(culture),
		MaxAxisEm = axisEm
	};

	private static DateTimeOffset Local(int month, int day, int hour, int minute = 0) =>
		new(new DateTime(2026, month, day, hour, minute, 0), Berlin.GetUtcOffset(new DateTime(2026, month, day, hour, minute, 0)));

	[Theory]
	[InlineData(0.5, 120, 1)]
	[InlineData(0.5, 60, 2)]
	[InlineData(0.5, 20, 10)]
	[InlineData(6, 120, 15)]
	[InlineData(6, 60, 30)]
	[InlineData(6, 20, 120)]
	[InlineData(48, 120, 120)]
	[InlineData(48, 60, 180)]
	[InlineData(48, 20, 720)]
	[InlineData(720, 120, 1440)]
	[InlineData(720, 60, 2880)]
	[InlineData(720, 20, 10080)]
	public void StepFor_TakesTheFinestStepWhoseLabelsFit(double hours, double axisEm, double minutes) =>
		Assert.Equal(TimeSpan.FromMinutes(minutes), MokaTimeTicks.StepFor(TimeSpan.FromHours(hours), axisEm));

	[Fact]
	public void TheHourClocksSkip_HasNoTick_AndTheRestStayOnTheClock()
	{
		IReadOnlyList<MokaTimeTick> ticks = MokaTimeTicks.For(Local(3, 29, 0), Local(3, 30, 0), Options());

		Assert.Equal(24, ticks.Count);
		Assert.DoesNotContain(ticks, tick => tick.At.Hour == 2);
		Assert.All(ticks, tick => Assert.Equal(0, tick.At.Minute));
		Assert.Equal(TimeSpan.FromHours(1), ticks[2].At - ticks[1].At);
		Assert.Equal("01:00", ticks[1].Text);
		Assert.Equal("03:00", ticks[2].Text);
	}

	[Fact]
	public void HalfHours_StayOnTheClock_AcrossTheChange()
	{
		IReadOnlyList<MokaTimeTick> ticks = MokaTimeTicks.For(Local(3, 29, 1), Local(3, 29, 4), Options(20));

		Assert.Equal(["01:00", "01:30", "03:00", "03:30", "04:00"], ticks.Select(tick => tick.Text));
		Assert.Equal(
			[MokaTimeTickLevel.Major, MokaTimeTickLevel.Minor, MokaTimeTickLevel.Major, MokaTimeTickLevel.Minor, MokaTimeTickLevel.Major],
			ticks.Select(tick => tick.Level));
	}

	[Fact]
	public void Midnight_IsADayTick_WithTheDate()
	{
		IReadOnlyList<MokaTimeTick> ticks = MokaTimeTicks.For(Local(3, 2, 12), Local(3, 4, 12), Options(30));

		MokaTimeTick midnight = Assert.Single(ticks, tick => tick.At.Day == 3 && tick.At.Hour == 0);
		Assert.Equal(MokaTimeTickLevel.Day, midnight.Level);
		Assert.Equal("Tue 3", midnight.Text);
	}

	[Fact]
	public void ATickACoarserStepLandsOn_ShowsOnANarrowerAxis()
	{
		IReadOnlyList<MokaTimeTick> ticks = MokaTimeTicks.For(Local(3, 3, 6), Local(3, 3, 12), Options());

		MokaTimeTick noon = Assert.Single(ticks, tick => tick.At.Hour == 12);
		MokaTimeTick quarterPast = Assert.Single(ticks, tick => tick.At is { Hour: 7, Minute: 15 });
		Assert.True(noon.NeedEm < quarterPast.NeedEm);
		Assert.True(quarterPast.NeedEm <= 120);
	}

	[Fact]
	public void WeekSteps_StartOnMonday_AndMarkEachMonth()
	{
		IReadOnlyList<MokaTimeTick> ticks = MokaTimeTicks.For(Local(2, 1, 0), Local(4, 30, 0), Options());

		Assert.All(ticks.Where(tick => tick.Level != MokaTimeTickLevel.Month),
			tick => Assert.Equal(DayOfWeek.Monday, tick.At.DayOfWeek));
		Assert.Equal(13, ticks.Count(tick => tick.Level != MokaTimeTickLevel.Month));
		Assert.Equal(["Feb", "Mar", "Apr"], ticks.Where(tick => tick.Level == MokaTimeTickLevel.Month).Select(tick => tick.Text));
		Assert.Contains(ticks, tick => tick.Text == "Feb 2");
	}

	[Fact]
	public void ATwelveHourCulture_WritesAWholeHourShort()
	{
		IReadOnlyList<MokaTimeTick> ticks = MokaTimeTicks.For(Local(3, 3, 13), Local(3, 3, 18), Options(30, "en-US"));

		// ICU (Linux, macOS) puts a narrow no-break space before the AM/PM designator; Windows puts a plain space.
		Assert.Contains(ticks, tick => tick.Text.Replace(' ', ' ') == "2 PM");
	}

	[Fact]
	public void AFormat_ReplacesTheMinorAndMajorText()
	{
		MokaTimeTickOptions options = Options() with { Format = "HH'h'" };

		IReadOnlyList<MokaTimeTick> ticks = MokaTimeTicks.For(Local(3, 3, 6), Local(3, 3, 12), options);

		Assert.Contains(ticks, tick => tick.Text == "09h");
	}

	[Fact]
	public void AnEmptySpan_HasNoTicks() =>
		Assert.Empty(MokaTimeTicks.For(Local(3, 3, 6), Local(3, 3, 6), Options()));

	[Fact]
	public void TooltipText_SaysAsMuchOfTheDateAsTheSpanNeeds()
	{
		DateTimeOffset at = Local(3, 3, 14, 30);

		Assert.Equal("14:30", MokaTimeTicks.TooltipText(at, TimeSpan.FromHours(6), Options()));
		Assert.Equal("Tue 14:30", MokaTimeTicks.TooltipText(at, TimeSpan.FromDays(3), Options()));
		Assert.Equal("Tue 14:30", MokaTimeTicks.TooltipText(at, TimeSpan.FromDays(6), Options()));
		Assert.Equal("03/03/2026 14:30", MokaTimeTicks.TooltipText(at, TimeSpan.FromDays(30), Options()));
	}

	// Six and a half days can run from a Tuesday evening to the next Tuesday morning, so the weekday alone can't tell
	// the two ends apart.
	[Fact]
	public void TooltipText_PastSixDays_WritesTheDate() =>
		Assert.Equal("03/03/2026 14:30", MokaTimeTicks.TooltipText(Local(3, 3, 14, 30), TimeSpan.FromDays(6.5), Options()));

	// A minute of seconds written HH:mm read "15:35, 15:35, 15:36": the step has to be one the text can tell apart.
	[Fact]
	public void AFormatThatCantTellTheStepApart_TakesACoarserStep()
	{
		MokaTimeTickOptions options = Options() with { Format = "HH:mm" };

		IReadOnlyList<MokaTimeTick> ticks = MokaTimeTicks.For(Local(3, 3, 15, 34), Local(3, 3, 15, 37), options);

		Assert.Equal(["15:34", "15:35", "15:36", "15:37"], ticks.Select(tick => tick.Text));
	}

	[Theory]
	[InlineData(0.5)]
	[InlineData(1)]
	[InlineData(3)]
	[InlineData(30)]
	[InlineData(600)]
	[InlineData(6 * 60)]
	[InlineData(3 * 24 * 60)]
	[InlineData(60 * 24 * 60)]
	[InlineData(800 * 24 * 60)]
	public void NoTwoTicksSideBySide_ReadTheSame_WhateverTheSpanAndFormat(double minutes)
	{
		DateTimeOffset from = Local(3, 3, 15, 34);
		DateTimeOffset to = from + TimeSpan.FromMinutes(minutes);
		foreach (MokaTimeTickOptions options in new[]
		         {
			         Options(), Options(20), Options() with { Format = "HH:mm" }, Options() with { Format = "HH" },
			         Options() with { Format = "MMM d" }, Options() with { Format = "MMM" }, Options() with { Hour12 = true },
			         Options() with { Formatter = time => time.ToString("ddd", CultureInfo.InvariantCulture) }
		         })
		{
			IReadOnlyList<MokaTimeTick> ticks = MokaTimeTicks.For(from, to, options);
			for (int i = 1; i < ticks.Count; i++)
			{
				Assert.NotEqual(ticks[i - 1].Text, ticks[i].Text);
			}
		}
	}

	[Fact]
	public void AFormatter_WritesEveryTick()
	{
		MokaTimeTickOptions options = Options() with { Formatter = time => $"at {time.Hour}" };

		IReadOnlyList<MokaTimeTick> ticks = MokaTimeTicks.For(Local(3, 2, 18), Local(3, 3, 6), options);

		Assert.All(ticks, tick => Assert.StartsWith("at ", tick.Text, StringComparison.Ordinal));
		Assert.Contains(ticks, tick => tick is { Level: MokaTimeTickLevel.Day, Text: "at 0" });
	}

	[Fact]
	public void Hour12_WritesTheTicksOnATwelveHourClock()
	{
		IReadOnlyList<MokaTimeTick> ticks = MokaTimeTicks.For(Local(3, 3, 13), Local(3, 3, 18), Options(60) with { Hour12 = true });

		Assert.Contains(ticks, tick => tick.Text == "2 PM");
		Assert.Contains(ticks, tick => tick.Text == "2:30 PM");
	}

	[Fact]
	public void Hour12False_WritesATwelveHourCultureOnTheTwentyFourHourClock()
	{
		IReadOnlyList<MokaTimeTick> ticks = MokaTimeTicks.For(Local(3, 3, 13), Local(3, 3, 18), Options(30, "en-US") with { Hour12 = false });

		Assert.Contains(ticks, tick => tick.Text == "14:00");
		Assert.DoesNotContain(ticks, tick => tick.Text.Contains('M', StringComparison.Ordinal));
	}

	[Fact]
	public void TooltipText_TakesTheClock_AndTheTooltipFormat()
	{
		DateTimeOffset at = Local(3, 3, 14, 30);

		Assert.Equal("2:30 PM", MokaTimeTicks.TooltipText(at, TimeSpan.FromHours(6), Options() with { Hour12 = true }));
		Assert.Equal("Tue 2:30 PM", MokaTimeTicks.TooltipText(at, TimeSpan.FromDays(3), Options() with { Hour12 = true }));
		Assert.Equal("14:30", MokaTimeTicks.TooltipText(at, TimeSpan.FromHours(6), Options(120, "en-US") with { Hour12 = false }));
		Assert.Equal("3 Mar, 14.30",
			MokaTimeTicks.TooltipText(at, TimeSpan.FromHours(6), Options() with { TooltipFormat = "d MMM, HH.mm", Hour12 = true }));
	}

	[Fact]
	public void TooltipText_KeepsTheTimeOfAMidnightPoint_OnAChartOfDays() =>
		Assert.Equal("03/03/2026 00:00", MokaTimeTicks.TooltipText(Local(3, 3, 0), TimeSpan.FromDays(30), Options() with { Format = "MMM d" }));

	[Theory]
	[InlineData("h:mm tt", true)]
	[InlineData("hh:mm", true)]
	[InlineData("HH:mm", false)]
	[InlineData("H.mm", false)]
	[InlineData("HH 'h' mm", false)]
	[InlineData("'h' MMM d", null)]
	[InlineData("MMM d", null)]
	[InlineData("\\h:mm", null)]
	[InlineData(null, null)]
	public void WritesHour12_ReadsTheClock_OutsideQuotes(string? format, bool? twelve) =>
		Assert.Equal(twelve, MokaTimeTicks.WritesHour12(format));

	[Theory]
	[InlineData("fr-CA")]
	[InlineData("ko-KR")]
	[InlineData("en-US")]
	[InlineData("de-DE")]
	public void Hour12_KeepsACulturesQuotedText_AndItsTimeReadable(string culture)
	{
		ArgumentNullException.ThrowIfNull(culture);
		DateTimeOffset at = Local(3, 3, 14, 30);

		string twelve = MokaTimeTicks.TooltipText(at, TimeSpan.FromHours(6), Options(120, culture) with { Hour12 = true });
		string twentyFour = MokaTimeTicks.TooltipText(at, TimeSpan.FromHours(6), Options(120, culture) with { Hour12 = false });

		Assert.Contains("2", twelve, StringComparison.Ordinal);
		Assert.DoesNotContain("14", twelve, StringComparison.Ordinal);
		Assert.Contains("14", twentyFour, StringComparison.Ordinal);
		Assert.Contains("30", twentyFour, StringComparison.Ordinal);
		Assert.Equal(twentyFour.Trim(), twentyFour);
	}
}
