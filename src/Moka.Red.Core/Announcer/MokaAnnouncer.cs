namespace Moka.Red.Core.Announcer;

/// <summary>The default <see cref="IMokaAnnouncer" />. Thread safe; registered scoped by <c>AddMokaAnnouncer</c>.</summary>
public sealed class MokaAnnouncer : IMokaAnnouncer
{
	private const int KeptPerPoliteness = 3;

	private readonly List<object> _hosts = [];
	private readonly Lock _lock = new();
	private long _number;
	private MokaAnnouncement[] _recent = [];

	/// <inheritdoc />
	public IReadOnlyList<MokaAnnouncement> Recent => Volatile.Read(ref _recent);

	/// <inheritdoc />
	public object? Speaker
	{
		get
		{
			lock (_lock)
			{
				return _hosts.Count > 0 ? _hosts[0] : null;
			}
		}
	}

	/// <inheritdoc />
	public event Action? Changed;

	/// <inheritdoc />
	public void Announce(string text, MokaPoliteness politeness = MokaPoliteness.Polite)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(text);

		lock (_lock)
		{
			MokaAnnouncement[] current = _recent;
			MokaAnnouncement added = new(++_number, text, politeness);

			int samePoliteness = 0;
			foreach (MokaAnnouncement announcement in current)
			{
				if (announcement.Politeness == politeness)
				{
					samePoliteness++;
				}
			}

			int drop = Math.Max(0, samePoliteness + 1 - KeptPerPoliteness);
			List<MokaAnnouncement> next = new(current.Length + 1);
			foreach (MokaAnnouncement announcement in current)
			{
				if (drop > 0 && announcement.Politeness == politeness)
				{
					drop--;
					continue;
				}

				next.Add(announcement);
			}

			next.Add(added);
			Volatile.Write(ref _recent, next.ToArray());
		}

		Changed?.Invoke();
	}

	/// <inheritdoc />
	public void Join(object host)
	{
		ArgumentNullException.ThrowIfNull(host);

		bool speakerChanged;
		lock (_lock)
		{
			if (_hosts.Contains(host))
			{
				return;
			}

			_hosts.Add(host);
			speakerChanged = _hosts.Count == 1;
		}

		if (speakerChanged)
		{
			Changed?.Invoke();
		}
	}

	/// <inheritdoc />
	public void Leave(object host)
	{
		ArgumentNullException.ThrowIfNull(host);

		bool speakerChanged;
		lock (_lock)
		{
			int index = _hosts.IndexOf(host);
			if (index < 0)
			{
				return;
			}

			_hosts.RemoveAt(index);
			speakerChanged = index == 0;
		}

		if (speakerChanged)
		{
			Changed?.Invoke();
		}
	}
}
