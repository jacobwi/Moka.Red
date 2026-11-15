using System.Collections.ObjectModel;
using Microsoft.Extensions.Options;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.Feedback.Notification;

/// <summary>
///     Default implementation of <see cref="IMokaNotificationService" />.
///     Thread-safe notification management.
/// </summary>
public sealed class MokaNotificationService : IMokaNotificationService
{
	private readonly TimeProvider _clock;
	private readonly int? _limit;
	private readonly object _lock = new();
	private readonly List<MokaNotification> _notifications = [];

	// Sorting and copying on every property read was the hot path: the notification
	// center touches Notifications several times per render. The snapshot is rebuilt
	// only after a mutation.
	private ReadOnlyCollection<MokaNotification>? _snapshot;
	private int _unreadCount;

	/// <summary>A notification service.</summary>
	/// <param name="clock">Stamps the notifications pushed from a title and message. Null takes the system clock.</param>
	/// <param name="options">The app's <see cref="MokaFeedbackOptions.NotificationLimit" />. Null keeps every notification.</param>
	public MokaNotificationService(TimeProvider? clock = null, IOptions<MokaFeedbackOptions>? options = null)
	{
		_clock = clock ?? TimeProvider.System;
		_limit = options?.Value.NotificationLimit;
	}

	/// <inheritdoc />
	public IReadOnlyList<MokaNotification> Notifications
	{
		get
		{
			lock (_lock)
			{
				return EnsureSnapshot();
			}
		}
	}

	/// <inheritdoc />
	public int UnreadCount
	{
		get
		{
			lock (_lock)
			{
				EnsureSnapshot();
				return _unreadCount;
			}
		}
	}

	/// <inheritdoc />
	public event Action? OnChanged;

	/// <inheritdoc />
	public void Push(string title, string message, MokaToastSeverity severity = MokaToastSeverity.Info)
		=> Push(new MokaNotification
		{
			Title = title,
			Message = message,
			Severity = severity,
			Timestamp = _clock.GetUtcNow().UtcDateTime
		});

	/// <inheritdoc />
	public void Push(MokaNotification notification)
	{
		ArgumentNullException.ThrowIfNull(notification);

		lock (_lock)
		{
			// One entry per id, so MarkAsRead and Remove always mean one notification. A repeated id
			// updates the entry, the way a status notification is usually re-pushed. A repeated key replaces
			// the entry it names, whatever its id.
			if (notification.Key is not null)
			{
				_notifications.RemoveAll(n =>
					n.Id != notification.Id && string.Equals(n.Key, notification.Key, StringComparison.Ordinal));
			}

			int index = _notifications.FindIndex(n => n.Id == notification.Id);
			if (index >= 0)
			{
				_notifications[index] = notification;
			}
			else
			{
				_notifications.Add(notification);
				if (_limit is { } limit)
				{
					TrimTo(limit, notification.Id);
				}
			}

			Invalidate();
		}

		OnChanged?.Invoke();
	}

	/// <inheritdoc />
	public void MarkAsRead(Guid id)
	{
		lock (_lock)
		{
			int index = _notifications.FindIndex(n => n.Id == id);
			if (index < 0 || _notifications[index].Read)
			{
				return;
			}

			// Replaced rather than mutated so snapshots already handed out stay as they were.
			_notifications[index] = _notifications[index].AsRead();
			Invalidate();
		}

		OnChanged?.Invoke();
	}

	/// <inheritdoc />
	public void MarkAllAsRead()
	{
		lock (_lock)
		{
			bool changed = false;

			for (int i = 0; i < _notifications.Count; i++)
			{
				if (!_notifications[i].Read)
				{
					_notifications[i] = _notifications[i].AsRead();
					changed = true;
				}
			}

			if (!changed)
			{
				return;
			}

			Invalidate();
		}

		OnChanged?.Invoke();
	}

	/// <inheritdoc />
	public void Remove(Guid id)
	{
		lock (_lock)
		{
			if (_notifications.RemoveAll(n => n.Id == id) == 0)
			{
				return;
			}

			Invalidate();
		}

		OnChanged?.Invoke();
	}

	/// <inheritdoc />
	public void Clear()
	{
		lock (_lock)
		{
			if (_notifications.Count == 0)
			{
				return;
			}

			_notifications.Clear();
			Invalidate();
		}

		OnChanged?.Invoke();
	}

	private void Invalidate() => _snapshot = null;

	// Read notifications go first, oldest first, then the oldest unread ones. The one just pushed always stays, so a
	// push never removes itself, and with a limit of at least 1 there is always another to take.
	private void TrimTo(int limit, Guid pushed)
	{
		while (_notifications.Count > limit)
		{
			int oldest = OldestIndex(readOnly: true, pushed);
			_notifications.RemoveAt(oldest >= 0 ? oldest : OldestIndex(readOnly: false, pushed));
		}
	}

	private int OldestIndex(bool readOnly, Guid keep)
	{
		int oldest = -1;
		for (int i = 0; i < _notifications.Count; i++)
		{
			MokaNotification candidate = _notifications[i];
			if (candidate.Id == keep || (readOnly && !candidate.Read))
			{
				continue;
			}

			if (oldest < 0 || candidate.Timestamp < _notifications[oldest].Timestamp)
			{
				oldest = i;
			}
		}

		return oldest;
	}

	private ReadOnlyCollection<MokaNotification> EnsureSnapshot()
	{
		if (_snapshot is not null)
		{
			return _snapshot;
		}

		List<MokaNotification> sorted = _notifications.OrderByDescending(n => n.Timestamp).ToList();

		int unread = 0;
		foreach (MokaNotification notification in sorted)
		{
			if (!notification.Read)
			{
				unread++;
			}
		}

		_unreadCount = unread;
		_snapshot = sorted.AsReadOnly();
		return _snapshot;
	}
}
