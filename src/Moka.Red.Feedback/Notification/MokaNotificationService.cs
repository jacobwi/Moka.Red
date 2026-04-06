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

	/// <inheritdoc />
	public IReadOnlyList<MokaNotification> Notifications
	{
		get
		{
			lock (_lock)
			{
				return _notifications.OrderByDescending(n => n.Timestamp).ToList().AsReadOnly();
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
				return _notifications.Count(n => !n.Read);
			}
		}
	}

	/// <inheritdoc />
	public event Action? OnChanged;

	/// <inheritdoc />
	public void Push(string title, string message, MokaToastSeverity severity = MokaToastSeverity.Info)
	{
		var notification = new MokaNotification
		{
			Title = title,
			Message = message,
			Severity = severity
		};

		lock (_lock)
		{
			_notifications.Add(notification);
		}

		OnChanged?.Invoke();
	}

	/// <inheritdoc />
	public void MarkAsRead(Guid id)
	{
		lock (_lock)
		{
			MokaNotification? notification = _notifications.Find(n => n.Id == id);
			if (notification is not null)
			{
				notification.Read = true;
			}
		}

		OnChanged?.Invoke();
	}

	/// <inheritdoc />
	public void MarkAllAsRead()
	{
		lock (_lock)
		{
			foreach (MokaNotification notification in _notifications)
			{
				notification.Read = true;
			}
		}

		OnChanged?.Invoke();
	}

	/// <inheritdoc />
	public void Remove(Guid id)
	{
		lock (_lock)
		{
			_notifications.RemoveAll(n => n.Id == id);
		}

		OnChanged?.Invoke();
	}

	/// <inheritdoc />
	public void Clear()
	{
		lock (_lock)
		{
			_notifications.Clear();
		}

		OnChanged?.Invoke();
	}
}
