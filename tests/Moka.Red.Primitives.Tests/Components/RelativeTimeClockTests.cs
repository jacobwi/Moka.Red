using Moka.Red.Primitives.RelativeTime;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class RelativeTimeClockTests
{
	private readonly FakeClock _clock = new();

	private DateTimeOffset In(TimeSpan wait) => _clock.GetUtcNow() + wait;

	private FakeTimer TheTimer => Assert.Single(_clock.Timers);

	[Fact]
	public void ManySubscribers_ArmOneTimer_ForTheSoonest()
	{
		List<RelativeTimeClock.Subscription> subscriptions = [];
		for (int i = 0; i < 100; i++)
		{
			RelativeTimeClock.Subscription subscription = RelativeTimeClock.Join(_clock, () => { });
			subscription.WakeAt(In(TimeSpan.FromSeconds(500 - i)));
			subscriptions.Add(subscription);
		}

		Assert.Equal(In(TimeSpan.FromSeconds(401)), TheTimer.Due);
		subscriptions.ForEach(subscription => subscription.Dispose());
	}

	[Fact]
	public void AFire_WakesOnlyTheDue_AndArmsForTheNext()
	{
		int first = 0, second = 0, later = 0;
		using RelativeTimeClock.Subscription a = RelativeTimeClock.Join(_clock, () => first++);
		using RelativeTimeClock.Subscription b = RelativeTimeClock.Join(_clock, () => second++);
		using RelativeTimeClock.Subscription c = RelativeTimeClock.Join(_clock, () => later++);
		a.WakeAt(In(TimeSpan.FromSeconds(10)));
		b.WakeAt(In(TimeSpan.FromSeconds(10)));
		c.WakeAt(In(TimeSpan.FromSeconds(20)));

		_clock.Advance(TimeSpan.FromSeconds(10));

		Assert.Equal((1, 1, 0), (first, second, later));
		Assert.Equal(In(TimeSpan.FromSeconds(10)), TheTimer.Due);
	}

	[Fact]
	public void AWokenSubscriber_WaitsUntilItAsksAgain()
	{
		int wakes = 0;
		using RelativeTimeClock.Subscription subscription = RelativeTimeClock.Join(_clock, () => wakes++);
		subscription.WakeAt(In(TimeSpan.FromSeconds(5)));

		_clock.Advance(TimeSpan.FromHours(1));

		Assert.Equal(1, wakes);
		Assert.Null(TheTimer.Due);
	}

	[Fact]
	public void AskingAgain_MovesTheTimer_EitherWay()
	{
		using RelativeTimeClock.Subscription subscription = RelativeTimeClock.Join(_clock, () => { });
		subscription.WakeAt(In(TimeSpan.FromMinutes(5)));

		subscription.WakeAt(In(TimeSpan.FromMinutes(1)));
		Assert.Equal(In(TimeSpan.FromMinutes(1)), TheTimer.Due);

		subscription.WakeAt(In(TimeSpan.FromHours(2)));
		Assert.Equal(In(TimeSpan.FromHours(2)), TheTimer.Due);
	}

	[Fact]
	public void TheSoonestLeaving_ArmsForTheNext()
	{
		using RelativeTimeClock.Subscription stays = RelativeTimeClock.Join(_clock, () => { });
		RelativeTimeClock.Subscription leaves = RelativeTimeClock.Join(_clock, () => { });
		stays.WakeAt(In(TimeSpan.FromMinutes(3)));
		leaves.WakeAt(In(TimeSpan.FromMinutes(1)));

		leaves.Dispose();

		Assert.Equal(In(TimeSpan.FromMinutes(3)), TheTimer.Due);
	}

	[Fact]
	public void TheLastLeaving_StopsTheTimer_AndForgetsTheClock()
	{
		RelativeTimeClock.Subscription a = RelativeTimeClock.Join(_clock, () => { });
		RelativeTimeClock.Subscription b = RelativeTimeClock.Join(_clock, () => { });
		a.WakeAt(In(TimeSpan.FromMinutes(1)));
		b.WakeAt(In(TimeSpan.FromMinutes(2)));

		a.Dispose();
		Assert.False(TheTimer.Disposed);
		Assert.True(RelativeTimeClock.IsRunningFor(_clock));

		b.Dispose();
		Assert.True(TheTimer.Disposed);
		Assert.False(RelativeTimeClock.IsRunningFor(_clock));
	}

	[Fact]
	public void JoiningAfterTheLastLeft_StartsAFreshTimer()
	{
		RelativeTimeClock.Subscription gone = RelativeTimeClock.Join(_clock, () => { });
		gone.WakeAt(In(TimeSpan.FromMinutes(1)));
		gone.Dispose();
		int wakes = 0;

		using RelativeTimeClock.Subscription fresh = RelativeTimeClock.Join(_clock, () => wakes++);
		fresh.WakeAt(In(TimeSpan.FromMinutes(1)));
		_clock.Advance(TimeSpan.FromMinutes(1));

		Assert.Equal(2, _clock.Timers.Count);
		Assert.Equal(1, wakes);
	}

	[Fact]
	public void ALeftSubscriber_IsNotWoken()
	{
		int wakes = 0;
		using RelativeTimeClock.Subscription stays = RelativeTimeClock.Join(_clock, () => { });
		RelativeTimeClock.Subscription leaves = RelativeTimeClock.Join(_clock, () => wakes++);
		stays.WakeAt(In(TimeSpan.FromMinutes(1)));
		leaves.WakeAt(In(TimeSpan.FromMinutes(1)));

		leaves.Dispose();
		_clock.Advance(TimeSpan.FromMinutes(1));

		Assert.Equal(0, wakes);
		Assert.Throws<ObjectDisposedException>(() => leaves.WakeAt(In(TimeSpan.FromMinutes(1))));
	}

	[Fact]
	public void TwoProviders_GetTwoClocks()
	{
		FakeClock other = new();
		int mine = 0, theirs = 0;
		using RelativeTimeClock.Subscription a = RelativeTimeClock.Join(_clock, () => mine++);
		using RelativeTimeClock.Subscription b = RelativeTimeClock.Join(other, () => theirs++);
		a.WakeAt(In(TimeSpan.FromMinutes(1)));
		b.WakeAt(other.GetUtcNow() + TimeSpan.FromMinutes(1));

		_clock.Advance(TimeSpan.FromMinutes(1));

		Assert.Single(_clock.Timers);
		Assert.Single(other.Timers);
		Assert.Equal((1, 0), (mine, theirs));
	}

	[Fact]
	public void AThrowingWake_DoesNotStopTheRest()
	{
		InvalidOperationException broken = new("broken");
		int before = 0, after = 0;
		using RelativeTimeClock.Subscription a = RelativeTimeClock.Join(_clock, () => before++);
		using RelativeTimeClock.Subscription b = RelativeTimeClock.Join(_clock, () => throw broken);
		using RelativeTimeClock.Subscription c = RelativeTimeClock.Join(_clock, () => after++);
		using RelativeTimeClock.Subscription later = RelativeTimeClock.Join(_clock, () => { });
		a.WakeAt(In(TimeSpan.FromSeconds(10)));
		b.WakeAt(In(TimeSpan.FromSeconds(10)));
		c.WakeAt(In(TimeSpan.FromSeconds(10)));
		later.WakeAt(In(TimeSpan.FromSeconds(30)));

		AggregateException thrown = Assert.Throws<AggregateException>(() => _clock.Advance(TimeSpan.FromSeconds(10)));

		Assert.Same(broken, Assert.Single(thrown.InnerExceptions));
		Assert.Equal((1, 1), (before, after));
		Assert.Equal(_clock.GetUtcNow() + TimeSpan.FromSeconds(20), TheTimer.Due);
	}

	// A real timer throws on a wait of 49.7 days or more.
	[Fact]
	public void AFarMoment_WaitsInStepsATimerAllows_AndWakesOnce()
	{
		int wakes = 0;
		using RelativeTimeClock.Subscription subscription = RelativeTimeClock.Join(_clock, () => wakes++);
		subscription.WakeAt(In(TimeSpan.FromDays(100)));
		Assert.Equal(In(RelativeTimeClock.LongestWait), TheTimer.Due);

		_clock.Advance(TimeSpan.FromDays(99));
		Assert.Equal(0, wakes);

		_clock.Advance(TimeSpan.FromDays(1));
		Assert.Equal(1, wakes);
	}

	[Fact]
	public void AMomentAlreadyPast_WakesAtOnce()
	{
		int wakes = 0;
		using RelativeTimeClock.Subscription subscription = RelativeTimeClock.Join(_clock, () => wakes++);
		subscription.WakeAt(In(TimeSpan.FromSeconds(-5)));

		Assert.Equal(_clock.GetUtcNow(), TheTimer.Due);
		_clock.Advance(TimeSpan.Zero);
		Assert.Equal(1, wakes);
	}

	[Fact]
	public void SubscribersJoiningAndLeavingAtOnce_LeaveNothingRunning()
	{
		Parallel.For(0, 2000, i =>
		{
			using RelativeTimeClock.Subscription subscription = RelativeTimeClock.Join(_clock, () => { });
			subscription.WakeAt(In(TimeSpan.FromSeconds(i % 50 + 1)));
		});

		Assert.False(RelativeTimeClock.IsRunningFor(_clock));
		Assert.All(_clock.Timers, timer => Assert.True(timer.Disposed));
	}
}
