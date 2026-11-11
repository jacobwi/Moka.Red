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
}
