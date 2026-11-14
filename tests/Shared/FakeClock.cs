namespace Moka.Red.Tests.Shared;

// A clock that moves only when a test calls Advance. A timer fires on the caller's thread once the clock reaches its
// due time, so a test can stop a wait just short of running out. Linked into the test projects of the components
// that wait on a registered TimeProvider.
internal sealed class FakeClock : TimeProvider
{
	private readonly List<FakeTimer> _timers = [];
	private DateTimeOffset _now = new(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

	public IReadOnlyList<FakeTimer> Timers => _timers;

	public override DateTimeOffset GetUtcNow() => _now;

	public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
	{
		FakeTimer timer = new(this, callback, state);
		timer.Change(dueTime, period);
		_timers.Add(timer);
		return timer;
	}

	public void Advance(TimeSpan by)
	{
		DateTimeOffset end = _now + by;
		while (_timers.Where(timer => timer.Due <= end).MinBy(timer => timer.Due) is { } next)
		{
			_now = next.Due!.Value;
			next.Fire();
		}

		_now = end;
	}
}

internal sealed class FakeTimer(FakeClock clock, TimerCallback callback, object? state) : ITimer
{
	private TimeSpan _period;

	public DateTimeOffset? Due { get; private set; }

	public bool Disposed { get; private set; }

	public bool Change(TimeSpan dueTime, TimeSpan period)
	{
		if (Disposed)
		{
			return false;
		}

		Due = dueTime == Timeout.InfiniteTimeSpan ? null : clock.GetUtcNow() + dueTime;
		_period = period;
		return true;
	}

	// Disposing a timer doesn't take back a callback that fired just before; this runs it as such a callback would.
	public void InvokeCallback() => callback(state);

	public void Fire()
	{
		Due = _period > TimeSpan.Zero ? Due + _period : null;
		callback(state);
	}

	public void Dispose()
	{
		Disposed = true;
		Due = null;
	}

	public ValueTask DisposeAsync()
	{
		Dispose();
		return ValueTask.CompletedTask;
	}
}
