using System.Collections.ObjectModel;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.Feedback.Notification;

/// <summary>
///     Default implementation of <see cref="IMokaNotificationService" />.
///     Thread-safe notification management.
/// </summary>
public sealed class MokaNotificationService : IMokaNotificationService
{
	private readonly object _lock = new();
	private readonly List<MokaNotification> _notifications = [];

	// Sorting and copying on every property read was the hot path: the notification
	// center touches Notifications several times per render. The snapshot is rebuilt
	// only after a mutation.
	private ReadOnlyCollection<MokaNotification>? _snapshot;
	private int _unreadCount;

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
			Severity = severity
		});

	/// <inheritdoc />
	public void Push(MokaNotification notification)
	{
		ArgumentNullException.ThrowIfNull(notification);

		lock (_lock)
		{
			_notifications.Add(notification);
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
			_notifications[index] = AsRead(_notifications[index]);
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
					_notifications[i] = AsRead(_notifications[i]);
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

	private static MokaNotification AsRead(MokaNotification source) => new()
	{
		Id = source.Id,
		Title = source.Title,
		Message = source.Message,
		Severity = source.Severity,
		Timestamp = source.Timestamp,
		Icon = source.Icon,
		OnClick = source.OnClick,
		Read = true
	};

	private void Invalidate() => _snapshot = null;

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
