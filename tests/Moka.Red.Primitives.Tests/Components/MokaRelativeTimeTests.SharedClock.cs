using Bunit;
using Moka.Red.Primitives.RelativeTime;

namespace Moka.Red.Primitives.Tests.Components;

// Every relative time on a clock shares one timer, set for whichever is due first.
public partial class MokaRelativeTimeTests
{
	[Fact]
	public void ManyOnOneClock_ShareOneTimer_SetForTheSoonest()
	{
		for (int i = 0; i < 30; i++)
		{
			Time(Ago(TimeSpan.FromSeconds(3700 + i * 60)));
		}

		Time(Ago(TimeSpan.FromSeconds(200)));

		AssertWaitsAbout(TimeSpan.FromSeconds(40));
	}

	[Fact]
	public void EachOne_RedrawsOnlyWhenItsOwnTextChanges()
	{
		IRenderedComponent<MokaRelativeTime> minutes = Time(Ago(TimeSpan.FromSeconds(200)));
		IRenderedComponent<MokaRelativeTime> hours = Time(Ago(TimeSpan.FromSeconds(3700)));
		int hourRenders = hours.RenderCount;

		_clock.Advance(TimeSpan.FromSeconds(41));

		AssertText(minutes, "4 minutes ago");
		Assert.Equal(hourRenders, hours.RenderCount);
		Assert.Equal("1 hour ago", TextOf(hours));
		minutes.WaitForAssertion(() => AssertWaitsAbout(TimeSpan.FromSeconds(59)), Plenty);

		_clock.Advance(TimeSpan.FromSeconds(3460));

		AssertText(hours, "2 hours ago");
	}

	[Fact]
	public async Task DisposingOne_LeavesTheOthersTicking()
	{
		IRenderedComponent<MokaRelativeTime> gone = Time(Ago(TimeSpan.FromSeconds(230)));
		IRenderedComponent<MokaRelativeTime> stays = Time(Ago(TimeSpan.FromSeconds(200)));
		AssertWaitsAbout(TimeSpan.FromSeconds(10));

		await gone.Instance.DisposeAsync();
		AssertWaitsAbout(TimeSpan.FromSeconds(40));
		_clock.Advance(TimeSpan.FromSeconds(41));

		AssertText(stays, "4 minutes ago");
		Assert.Equal("3 minutes ago", TextOf(gone));
	}

	[Fact]
	public async Task DisposingTheLast_StopsTheSharedTimer()
	{
		IRenderedComponent<MokaRelativeTime> first = Time(Ago(TimeSpan.FromSeconds(200)));
		IRenderedComponent<MokaRelativeTime> second = Time(Ago(TimeSpan.FromHours(3)));

		await first.Instance.DisposeAsync();
		Assert.False(TheTimer.Disposed);

		await second.Instance.DisposeAsync();
		Assert.True(TheTimer.Disposed);
		Assert.False(RelativeTimeClock.IsRunningFor(_clock));
	}
}
