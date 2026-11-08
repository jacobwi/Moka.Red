using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.RelativeTime;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// The clock is the test's own, so nothing here waits on a real minute.
public partial class MokaRelativeTimeTests : BunitContext
{
	private static readonly DateTimeOffset Start = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
	private static readonly TimeSpan Plenty = TimeSpan.FromSeconds(10);

	private readonly ManualClock _clock = new(Start);

	public MokaRelativeTimeTests()
	{
		Services.AddSingleton<TimeProvider>(_clock);
		SetRendererInfo(new RendererInfo("Server", true));
	}

	private IRenderedComponent<MokaRelativeTime> Time(
		DateTimeOffset? value,
		Action<ComponentParameterCollectionBuilder<MokaRelativeTime>>? more = null) =>
		Render<MokaRelativeTime>(p =>
		{
			p.Add(x => x.Value, value);
			more?.Invoke(p);
		});

	private static DateTimeOffset Ago(TimeSpan age) => Start - age;

	private static string TextOf(IRenderedComponent<MokaRelativeTime> cut) => cut.Find(".moka-relative-time").TextContent;

	private static void AssertText(IRenderedComponent<MokaRelativeTime> cut, string text) =>
		cut.WaitForAssertion(() => Assert.Equal(text, TextOf(cut)), Plenty);

	private ManualTimer TheTimer => Assert.Single(_clock.Timers);

	private void AssertWaitsAbout(TimeSpan wait) =>
		Assert.InRange(TheTimer.Wait!.Value, wait, wait + TimeSpan.FromSeconds(1));

	private static void InCulture(string culture, Action test)
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo(culture);
		try
		{
			test();
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	[Theory]
	[InlineData(60, "1 minute ago")]
	[InlineData(119, "1 minute ago")]
	[InlineData(120, "2 minutes ago")]
	[InlineData(3599, "59 minutes ago")]
	[InlineData(3600, "1 hour ago")]
	[InlineData(7200, "2 hours ago")]
	[InlineData(86399, "23 hours ago")]
	[InlineData(86400, "1 day ago")]
	[InlineData(518400, "6 days ago")]
	[InlineData(604800, "1 week ago")]
	[InlineData(1814400, "3 weeks ago")]
	[InlineData(2505600, "4 weeks ago")]
	[InlineData(2592000, "1 month ago")]
	[InlineData(5184000, "2 months ago")]
	[InlineData(31449600, "12 months ago")]
	[InlineData(31536000, "1 year ago")]
	[InlineData(94608000, "3 years ago")]
	public void ThePast_ReadsInTheLargestUnitThatFits(int seconds, string words) =>
		Assert.Equal(words, TextOf(Time(Ago(TimeSpan.FromSeconds(seconds)))));

	[Theory]
	[InlineData(60, "in 1 minute")]
	[InlineData(120, "in 2 minutes")]
	[InlineData(3600, "in 1 hour")]
	[InlineData(7200, "in 2 hours")]
	[InlineData(86400, "in 1 day")]
	[InlineData(1209600, "in 2 weeks")]
	[InlineData(2592000, "in 1 month")]
	[InlineData(63072000, "in 2 years")]
	public void TheFuture_ReadsFromNow(int seconds, string words) =>
		Assert.Equal(words, TextOf(Time(Start + TimeSpan.FromSeconds(seconds))));

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(59)]
	[InlineData(-1)]
	[InlineData(-59)]
	public void UnderAMinute_ReadsJustNow(int secondsAgo) =>
		Assert.Equal("just now", TextOf(Time(Start - TimeSpan.FromSeconds(secondsAgo))));

	[Fact]
	public void ItIsATimeElement_WithAMachineReadableDate()
	{
		IElement time = Time(new DateTimeOffset(2026, 11, 8, 11, 57, 3, 250, TimeSpan.FromHours(-5))).Find("time");

		Assert.Equal("2026-11-08T11:57:03.250-05:00", time.GetAttribute("datetime"));
		Assert.True(time.ClassList.Contains("moka-relative-time"));
	}

	[Theory]
	[InlineData("sv-SE")]
	[InlineData("ar-SA")]
	[InlineData("th-TH")]
	public void TheMachineReadableDate_IgnoresTheCulture(string culture) => InCulture(culture, () =>
		Assert.Equal("2026-11-08T12:00:00.000+00:00", Time(Start).Find("time").GetAttribute("datetime")));

	// HTML's global date and time string takes one to three digits of a fraction, so "O" with its seven would be invalid.
	[Fact]
	public void TheMachineReadableDate_IsValidForHtml() =>
		Assert.Matches(HtmlDateTime(), Time(Start.AddTicks(1234567)).Find("time").GetAttribute("datetime")!);

	[GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{1,3}[+-]\d{2}:\d{2}$")]
	private static partial Regex HtmlDateTime();

	[Fact]
	public void TheTooltip_ShowsTheFullDateAndTime()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromMinutes(3)));
		string absolute = Ago(TimeSpan.FromMinutes(3)).ToString("G", CultureInfo.CurrentCulture);

		Assert.Equal(absolute, cut.Find("[role=tooltip]").TextContent);
		Assert.Equal(absolute, cut.Find("time").GetAttribute("aria-description"));
		Assert.Null(cut.Find("time").GetAttribute("title"));
	}

	[Fact]
	public void TooltipFormat_ChangesWhatTheTooltipSays()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Start, p => p.Add(x => x.TooltipFormat, "yyyy"));

		Assert.Equal("2026", cut.Find("[role=tooltip]").TextContent);
	}

	[Fact]
	public void ShowTooltipOff_DrawsNoTooltip()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Start, p => p.Add(x => x.ShowTooltip, false));

		Assert.Empty(cut.FindAll("[role=tooltip]"));
		Assert.Null(cut.Find("time").GetAttribute("aria-description"));
	}

	[Fact]
	public void ADateThatAlreadySaysItAll_GetsNoTooltip()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromDays(10)), p => p
			.Add(x => x.AbsoluteAfter, TimeSpan.FromDays(7))
			.Add(x => x.Format, "G"));

		Assert.Empty(cut.FindAll("[role=tooltip]"));
	}

	[Fact]
	public void AbsoluteAfter_ShowsADateOnceItIsThatOld()
	{
		DateTimeOffset value = Ago(TimeSpan.FromDays(8));
		IRenderedComponent<MokaRelativeTime> cut = Time(value, p => p.Add(x => x.AbsoluteAfter, TimeSpan.FromDays(7)));

		Assert.Equal(value.ToString("d", CultureInfo.CurrentCulture), TextOf(cut));
	}

	[Fact]
	public void AbsoluteAfter_LeavesTheWordsWhileItIsYounger() =>
		Assert.Equal("6 days ago", TextOf(Time(Ago(TimeSpan.FromDays(6)), p => p.Add(x => x.AbsoluteAfter, TimeSpan.FromDays(7)))));

	[Fact]
	public void AbsoluteAfter_ReadsTheSameForAValueToCome()
	{
		DateTimeOffset value = Start + TimeSpan.FromDays(8);
		IRenderedComponent<MokaRelativeTime> cut = Time(value, p => p.Add(x => x.AbsoluteAfter, TimeSpan.FromDays(7)));

		Assert.Equal(value.ToString("d", CultureInfo.CurrentCulture), TextOf(cut));
	}

	[Fact]
	public void Format_ChangesTheDate()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromDays(8)), p => p
			.Add(x => x.AbsoluteAfter, TimeSpan.FromDays(7))
			.Add(x => x.Format, "yyyy/MM"));

		Assert.Equal("2026/10", TextOf(cut).Replace('-', '/'));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-5)]
	public void AnAbsoluteAfterOfNothing_IsAlwaysADate_AndNeedsNoTimer(int days)
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromMinutes(5)), p => p
			.Add(x => x.AbsoluteAfter, TimeSpan.FromDays(days))
			.Add(x => x.Format, "yyyy"));

		Assert.Equal("2026", TextOf(cut));
		Assert.Empty(_clock.Timers);
	}

	[Fact]
	public void Formatter_ReplacesTheWords_AndGetsTheAge()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromMinutes(3)), p => p
			.Add(x => x.Formatter, age => $"för {age.TotalMinutes.ToString(CultureInfo.InvariantCulture)} minuter sedan"));

		Assert.Equal("för 3 minuter sedan", TextOf(cut));
	}

	[Fact]
	public void Formatter_ReplacesJustNow_Too() =>
		Assert.Equal("nu", TextOf(Time(Start, p => p.Add(x => x.Formatter, _ => "nu"))));

	[Fact]
	public void Formatter_GetsANegativeAge_ForAValueToCome()
	{
		TimeSpan? seen = null;
		Time(Start + TimeSpan.FromHours(2), p => p.Add(x => x.Formatter, age =>
		{
			seen = age;
			return "later";
		}));

		Assert.Equal(TimeSpan.FromHours(-2), seen);
	}

	[Fact]
	public void Formatter_YieldsToAbsoluteAfter()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromDays(8)), p => p
			.Add(x => x.AbsoluteAfter, TimeSpan.FromDays(7))
			.Add(x => x.Format, "yyyy")
			.Add(x => x.Formatter, _ => "words"));

		Assert.Equal("2026", TextOf(cut));
	}

	[Fact]
	public void Null_ShowsAHyphen_OutsideATimeElement()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(null);

		Assert.Equal("-", TextOf(cut));
		Assert.Empty(cut.FindAll("time"));
		Assert.Empty(cut.FindAll("[role=tooltip]"));
		Assert.Empty(_clock.Timers);
	}

	[Fact]
	public void EmptyText_ReplacesTheHyphen() =>
		Assert.Equal("Never", TextOf(Time(null, p => p.Add(x => x.EmptyText, "Never"))));

	[Fact]
	public void TheText_MovesOn_WhenTheClockPassesTheNextMinute()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromSeconds(200)));
		Assert.Equal("3 minutes ago", TextOf(cut));

		_clock.Advance(TimeSpan.FromSeconds(39));
		Assert.Equal("3 minutes ago", TextOf(cut));

		_clock.Advance(TimeSpan.FromSeconds(2));
		AssertText(cut, "4 minutes ago");
	}

	[Fact]
	public void AValueToCome_CountsDown()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Start + TimeSpan.FromSeconds(150));
		Assert.Equal("in 2 minutes", TextOf(cut));
		AssertWaitsAbout(TimeSpan.FromSeconds(30));

		_clock.Advance(TimeSpan.FromSeconds(31));
		AssertText(cut, "in 1 minute");
	}

	[Fact]
	public void AValueToCome_PassesThroughJustNow_AndBecomesThePast()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Start + TimeSpan.FromSeconds(61));
		Assert.Equal("in 1 minute", TextOf(cut));

		_clock.Advance(TimeSpan.FromSeconds(2));
		AssertText(cut, "just now");
		cut.WaitForAssertion(() => AssertWaitsAbout(TimeSpan.FromSeconds(119)), Plenty);

		_clock.Advance(TimeSpan.FromSeconds(120));
		AssertText(cut, "1 minute ago");
	}

	[Fact]
	public void ExactlyOnAWholeUnitToCome_ItMovesOnStraightAway()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Start + TimeSpan.FromMinutes(5));
		Assert.Equal("in 5 minutes", TextOf(cut));
		AssertWaitsAbout(TimeSpan.Zero);

		_clock.Advance(TimeSpan.FromSeconds(1));
		AssertText(cut, "in 4 minutes");
	}

	[Fact]
	public void JustNow_DoesNotTick_ItWaitsForTheFirstMinute()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Start);
		int renders = cut.RenderCount;
		AssertWaitsAbout(TimeSpan.FromMinutes(1));

		_clock.Advance(TimeSpan.FromSeconds(59));

		Assert.Equal(renders, cut.RenderCount);
		Assert.Equal("just now", TextOf(cut));
		Assert.Equal(0, TheTimer.Fired);
	}

	[Theory]
	[InlineData(200, 40)]
	[InlineData(3700, 3500)]
	[InlineData(90000, 82800)]
	[InlineData(8 * 86400, 6 * 86400)]
	public void EachValue_WakesWhenItsOwnTextIsDue(int ageSeconds, int waitSeconds)
	{
		Time(Ago(TimeSpan.FromSeconds(ageSeconds)));

		AssertWaitsAbout(TimeSpan.FromSeconds(waitSeconds));
	}

	// A count of weeks or months would run on past the larger unit: 28 days counted up to 35 and skipped "1 month ago".
	[Theory]
	[InlineData(28, 2, "1 month ago")]
	[InlineData(360, 5, "1 year ago")]
	public void TheNextChange_StopsAtTheNextUnit(int ageDays, int waitDays, string then)
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromDays(ageDays)));
		AssertWaitsAbout(TimeSpan.FromDays(waitDays));

		_clock.Advance(TimeSpan.FromDays(waitDays) + TimeSpan.FromSeconds(1));

		AssertText(cut, then);
	}

	// A real timer throws on a wait of 49.7 days or more, which the fake one checks.
	[Fact]
	public void AnOldValue_WaitsWithinWhatATimerAllows_AndWakesWithoutARender()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromDays(400)));
		Assert.Equal("1 year ago", TextOf(cut));
		TimeSpan firstWait = TheTimer.Wait!.Value;
		Assert.InRange(firstWait, TimeSpan.FromDays(1), TimeSpan.FromDays(49));
		int renders = cut.RenderCount;

		_clock.Advance(firstWait + TimeSpan.FromSeconds(1));

		cut.WaitForAssertion(() => Assert.NotNull(TheTimer.Wait), Plenty);
		Assert.Equal(renders, cut.RenderCount);
		Assert.Equal("1 year ago", TextOf(cut));
	}

	[Fact]
	public void TheLargestDates_DoNotThrow()
	{
		Assert.EndsWith("years ago", TextOf(Time(DateTimeOffset.MinValue)), StringComparison.Ordinal);
		Assert.StartsWith("in ", TextOf(Time(DateTimeOffset.MaxValue)), StringComparison.Ordinal);
	}

	[Fact]
	public void PassingAbsoluteAfter_TurnsTheWordsIntoADate_AndStopsTheTimer()
	{
		DateTimeOffset value = Ago(TimeSpan.FromDays(6) + TimeSpan.FromHours(23));
		IRenderedComponent<MokaRelativeTime> cut = Time(value, p => p.Add(x => x.AbsoluteAfter, TimeSpan.FromDays(7)));
		Assert.Equal("6 days ago", TextOf(cut));
		AssertWaitsAbout(TimeSpan.FromHours(1));

		_clock.Advance(TimeSpan.FromHours(1) + TimeSpan.FromSeconds(1));

		AssertText(cut, value.ToString("d", CultureInfo.CurrentCulture));
		cut.WaitForAssertion(() => Assert.True(TheTimer.Disposed), Plenty);
	}

	[Fact]
	public void AFarOffDate_TurnsIntoWordsAsItComesWithinAbsoluteAfter()
	{
		DateTimeOffset value = Start + TimeSpan.FromDays(7) + TimeSpan.FromMinutes(10);
		IRenderedComponent<MokaRelativeTime> cut = Time(value, p => p.Add(x => x.AbsoluteAfter, TimeSpan.FromDays(7)));
		Assert.Equal(value.ToString("d", CultureInfo.CurrentCulture), TextOf(cut));
		AssertWaitsAbout(TimeSpan.FromMinutes(10));

		_clock.Advance(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(1));

		AssertText(cut, "in 6 days");
	}

	[Fact]
	public void AnOldDate_NeedsNoTimer()
	{
		Time(Ago(TimeSpan.FromDays(8)), p => p.Add(x => x.AbsoluteAfter, TimeSpan.FromDays(7)));

		Assert.Empty(_clock.Timers);
	}

	[Fact]
	public void TheFormatter_IsAskedAgain_WhenTheEnglishWouldHaveChanged()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromSeconds(200)), p => p
			.Add(x => x.Formatter, age => ((int)age.TotalMinutes).ToString(CultureInfo.InvariantCulture)));
		Assert.Equal("3", TextOf(cut));

		_clock.Advance(TimeSpan.FromSeconds(41));

		AssertText(cut, "4");
	}

	[Fact]
	public void LiveOff_StartsNoTimer()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromMinutes(3)), p => p.Add(x => x.Live, false));

		_clock.Advance(TimeSpan.FromHours(1));

		Assert.Empty(_clock.Timers);
		Assert.Equal("3 minutes ago", TextOf(cut));
	}

	[Fact]
	public void LiveOff_StillTakesTheNewTimeFromAParentRender()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromMinutes(3)), p => p.Add(x => x.Live, false));
		_clock.Advance(TimeSpan.FromMinutes(5));

		cut.Render(p => p.Add(x => x.Live, false));

		Assert.Equal("8 minutes ago", TextOf(cut));
	}

	[Fact]
	public void TurningLiveOff_StopsTheTimer()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromMinutes(3)));
		ManualTimer timer = TheTimer;

		cut.Render(p => p.Add(x => x.Live, false));

		Assert.True(timer.Disposed);
	}

	[Fact]
	public void AStaticRender_StartsNoTimer()
	{
		SetRendererInfo(new RendererInfo("Static", false));

		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromMinutes(3)));

		Assert.Equal("3 minutes ago", TextOf(cut));
		Assert.Empty(_clock.Timers);
	}

	[Fact]
	public async Task Disposing_StopsTheTimer()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromSeconds(200)));
		ManualTimer timer = TheTimer;

		await cut.Instance.DisposeAsync();

		Assert.True(timer.Disposed);
		_clock.Advance(TimeSpan.FromHours(1));
		Assert.Equal(0, timer.Fired);
		Assert.Equal("3 minutes ago", TextOf(cut));
	}

	[Fact]
	public void ANewValue_SchedulesAnew()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromSeconds(200)));
		AssertWaitsAbout(TimeSpan.FromSeconds(40));

		cut.Render(p => p.Add(x => x.Value, Ago(TimeSpan.FromHours(3))));

		Assert.Equal("3 hours ago", TextOf(cut));
		AssertWaitsAbout(TimeSpan.FromHours(1));
	}

	[Fact]
	public void TheValueGoingNull_StopsTheTimer()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromMinutes(3)));
		ManualTimer timer = TheTimer;

		cut.Render(p => p.Add(x => x.Value, (DateTimeOffset?)null));

		Assert.True(timer.Disposed);
		Assert.Equal("-", TextOf(cut));
	}

	[Fact]
	public void AParentRender_WithTheSameValue_LeavesTheTimerAlone()
	{
		DateTimeOffset value = Ago(TimeSpan.FromSeconds(200));
		IRenderedComponent<MokaRelativeTime> cut = Time(value);
		int changes = TheTimer.Changes;

		_clock.Advance(TimeSpan.FromSeconds(5));
		cut.Render(p => p.Add(x => x.Value, value));

		Assert.Equal(changes, TheTimer.Changes);
	}

	[Fact]
	public void ItIsNotALiveRegion()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Ago(TimeSpan.FromMinutes(3)));

		Assert.Empty(cut.FindAll("[aria-live], [role=status], [role=alert], [role=log], [role=timer]"));
	}

	[Fact]
	public void WithNoClockRegistered_ItUsesTheSystems()
	{
		using BunitContext context = new();
		context.SetRendererInfo(new RendererInfo("Server", true));

		IRenderedComponent<MokaRelativeTime> cut = context.Render<MokaRelativeTime>(p => p
			.Add(x => x.Value, DateTimeOffset.UtcNow - TimeSpan.FromMinutes(3) - TimeSpan.FromSeconds(10))
			.Add(x => x.Live, false));

		Assert.Equal("3 minutes ago", cut.Find("time").TextContent);
	}

	// The one test on the real clock, a moment short of the first minute, because the fake timer can't prove a real one accepts the wait.
	[Fact]
	public void WithNoClockRegistered_ARealTimerMovesTheTextOn()
	{
		using BunitContext context = new();
		context.SetRendererInfo(new RendererInfo("Server", true));

		IRenderedComponent<MokaRelativeTime> cut = context.Render<MokaRelativeTime>(p => p
			.Add(x => x.Value, DateTimeOffset.UtcNow - TimeSpan.FromSeconds(59.8)));

		cut.WaitForAssertion(() => Assert.Equal("1 minute ago", cut.Find("time").TextContent), Plenty);
	}

	[Fact]
	public void ClassIdAndOtherAttributes_LandOnTheTime()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Start, p => p
			.Add(x => x.Class, "mine")
			.Add(x => x.Id, "when")
			.AddUnmatched("data-when", "yes"));

		IElement root = cut.Find("time");
		Assert.True(root.ClassList.Contains("mine"));
		Assert.Equal("when", root.Id);
		Assert.Equal("yes", root.GetAttribute("data-when"));
	}

	[Fact]
	public void WithNothingSet_ItTakesTheSizeAndColorOfTheTextAroundIt() =>
		Assert.Null(Time(Start).Find("time").GetAttribute("style"));

	[Fact]
	public void ColorAndSize_ReachTheText()
	{
		IRenderedComponent<MokaRelativeTime> cut = Time(Start, p => p.Add(x => x.Color, MokaColor.Info).Add(x => x.Size, MokaSize.Sm));

		Assert.Equal("font-size: var(--moka-font-size-sm); color: var(--moka-color-info)", cut.Find("time").GetAttribute("style"));
	}

	[Fact]
	public void Digits_AreTabular() =>
		Assert.Equal("tabular-nums", ScopedCss.Declarations("MokaRelativeTime.razor.css", ".moka-relative-time")["font-variant-numeric"]);

	private sealed class ManualClock(DateTimeOffset start) : TimeProvider
	{
		private readonly Lock _gate = new();
		private DateTimeOffset _now = start;

		public List<ManualTimer> Timers { get; } = [];

		public override DateTimeOffset GetUtcNow()
		{
			lock (_gate)
			{
				return _now;
			}
		}

		public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
		{
			ManualTimer timer = new(this, callback, state);
			timer.Change(dueTime, period);
			timer.Changes = 0;
			Timers.Add(timer);
			return timer;
		}

		public void Advance(TimeSpan by)
		{
			DateTimeOffset now;
			lock (_gate)
			{
				_now += by;
				now = _now;
			}

			foreach (ManualTimer timer in Timers.ToArray())
			{
				timer.FireIfDue(now);
			}
		}
	}

	private sealed class ManualTimer(ManualClock clock, TimerCallback callback, object? state) : ITimer
	{
		private const double LongestSystemWaitMilliseconds = uint.MaxValue - 1;

		private DateTimeOffset? _due;

		public bool Disposed { get; private set; }

		public int Fired { get; private set; }

		public int Changes { get; set; }

		public TimeSpan? Wait { get; private set; }

		public void FireIfDue(DateTimeOffset now)
		{
			if (Disposed || _due is not { } due || due > now)
			{
				return;
			}

			_due = null;
			Wait = null;
			Fired++;
			callback(state);
		}

		public bool Change(TimeSpan dueTime, TimeSpan period)
		{
			ArgumentOutOfRangeException.ThrowIfGreaterThan(dueTime.TotalMilliseconds, LongestSystemWaitMilliseconds);
			Assert.Equal(Timeout.InfiniteTimeSpan, period);
			Changes++;
			Wait = dueTime;
			_due = clock.GetUtcNow() + dueTime;
			return true;
		}

		public void Dispose() => Disposed = true;

		public ValueTask DisposeAsync()
		{
			Disposed = true;
			return ValueTask.CompletedTask;
		}
	}
}
