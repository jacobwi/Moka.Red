namespace Moka.Red.Primitives.RelativeTime;

// One timer for every relative time on a TimeProvider, set for whichever is due first. Each component redraws through
// its own InvokeAsync, so circuits on Blazor Server, which run in parallel, can share it.
internal sealed class RelativeTimeClock
{
	// A timer throws on a wait of 49.7 days or more. A moment due later wakes the clock at this, and it waits again.
	internal static readonly TimeSpan LongestWait = TimeSpan.FromDays(30);

	private static readonly Dictionary<TimeProvider, RelativeTimeClock> Clocks = new(ReferenceEqualityComparer.Instance);
	private static readonly Lock ClocksGate = new();

	private static readonly Comparer<Subscription> WakeOrder = Comparer<Subscription>.Create((x, y) =>
	{
		int byMoment = Nullable.Compare(x.Moment, y.Moment);
		return byMoment != 0 ? byMoment : x.Order.CompareTo(y.Order);
	});

	private readonly Lock _gate = new();
	private readonly TimeProvider _provider;
	private readonly SortedSet<Subscription> _waiting = new(WakeOrder);
	private DateTimeOffset? _armedFor;
	private bool _closed;
	private int _members;
	private long _nextOrder;
	private ITimer? _timer;

	private RelativeTimeClock(TimeProvider provider) => _provider = provider;

	internal static Subscription Join(TimeProvider provider, Action wake)
	{
		RelativeTimeClock? clock;
		lock (ClocksGate)
		{
			if (!Clocks.TryGetValue(provider, out clock))
			{
				clock = new RelativeTimeClock(provider);
				Clocks.Add(provider, clock);
			}

			clock._members++;
		}

		return new Subscription(clock, wake);
	}

	internal static bool IsRunningFor(TimeProvider provider)
	{
		lock (ClocksGate)
		{
			return Clocks.ContainsKey(provider);
		}
	}

	private void Schedule(Subscription subscription, DateTimeOffset moment)
	{
		lock (_gate)
		{
			ObjectDisposedException.ThrowIf(subscription.Left, subscription);
			if (subscription.Moment is not null)
			{
				_waiting.Remove(subscription);
			}

			subscription.Moment = moment;
			subscription.Order = _nextOrder++;
			_waiting.Add(subscription);
			Arm();
		}
	}

	private void Leave(Subscription subscription)
	{
		lock (_gate)
		{
			if (subscription.Left)
			{
				return;
			}

			subscription.Left = true;
			if (subscription.Moment is not null)
			{
				_waiting.Remove(subscription);
				subscription.Moment = null;
				Arm();
			}
		}

		bool last;
		lock (ClocksGate)
		{
			last = --_members == 0;
			if (last)
			{
				Clocks.Remove(_provider);
			}
		}

		if (last)
		{
			Close();
		}
	}

	private void Close()
	{
		ITimer? timer;
		lock (_gate)
		{
			_closed = true;
			timer = _timer;
			_timer = null;
		}

		timer?.Dispose();
	}

	// Called with _gate held. With nothing waiting the timer is left as it is: it fires, finds nothing due and stops.
	private void Arm()
	{
		if (_closed || _waiting.Min is not { Moment: { } target } || target == _armedFor)
		{
			return;
		}

		_armedFor = target;
		TimeSpan untilDue = target - _provider.GetUtcNow();
		TimeSpan wait = untilDue <= TimeSpan.Zero ? TimeSpan.Zero : untilDue < LongestWait ? untilDue : LongestWait;
		if (_timer is not null)
		{
			_timer.Change(wait, Timeout.InfiniteTimeSpan);
			return;
		}

		// A timer shared by every circuit must not hold on to the first one's async locals, as dotnet/runtime's own
		// shared timers don't (NonCapturingTimer).
		using (ExecutionContext.SuppressFlow())
		{
			_timer = _provider.CreateTimer(static state => ((RelativeTimeClock)state!).Fire(), this, wait,
				Timeout.InfiniteTimeSpan);
		}
	}

	private void Fire()
	{
		List<Subscription>? due = null;
		lock (_gate)
		{
			if (_closed)
			{
				return;
			}

			_armedFor = null;
			DateTimeOffset now = _provider.GetUtcNow();
			while (_waiting.Min is { } first && first.Moment <= now)
			{
				_waiting.Remove(first);
				first.Moment = null;
				(due ??= []).Add(first);
			}

			Arm();
		}

		if (due is null)
		{
			return;
		}

		List<Exception>? failures = null;
		foreach (Subscription subscription in due)
		{
			try
			{
				subscription.Wake();
			}
#pragma warning disable CA1031 // A component's wake hands its work to its renderer and never throws, so one that does is a bug: the rest still wake, then it surfaces.
			catch (Exception ex)
#pragma warning restore CA1031
			{
				(failures ??= []).Add(ex);
			}
		}

		if (failures is not null)
		{
			throw new AggregateException(failures);
		}
	}

	internal sealed class Subscription : IDisposable
	{
		private readonly RelativeTimeClock _clock;

		internal Subscription(RelativeTimeClock clock, Action wake)
		{
			_clock = clock;
			Wake = wake;
		}

		internal Action Wake { get; }

		internal bool Left { get; set; }

		internal DateTimeOffset? Moment { get; set; }

		internal long Order { get; set; }

		public void Dispose() => _clock.Leave(this);

		internal void WakeAt(DateTimeOffset moment) => _clock.Schedule(this, moment);
	}
}
