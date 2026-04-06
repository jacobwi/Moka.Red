namespace Moka.Red.Feedback.Toast;

/// <summary>
///     Default implementation of <see cref="IMokaToastService" />.
///     Thread-safe; auto-removes toasts after their configured duration.
/// </summary>
public sealed class MokaToastService : IMokaToastService, IDisposable
{
	private readonly object _lock = new();
	private readonly Dictionary<Guid, Timer> _timers = [];
	private readonly List<MokaToastMessage> _toasts = [];

	/// <summary>Disposes all active timers.</summary>
	public void Dispose()
	{
		lock (_lock)
		{
			foreach (Timer timer in _timers.Values)
			{
				timer.Dispose();
			}

			_timers.Clear();
			_toasts.Clear();
		}
	}

	/// <inheritdoc />
	public event Action<MokaToastMessage>? OnToastAdded;

	/// <inheritdoc />
	public event Action<Guid>? OnToastRemoved;

	/// <inheritdoc />
	public void Show(string message, MokaToastSeverity severity = MokaToastSeverity.Info,
		Action<MokaToastOptions>? configure = null)
	{
		var options = new MokaToastOptions();
		configure?.Invoke(options);

		var toast = new MokaToastMessage
		{
			Id = Guid.NewGuid(),
			Message = message,
			Severity = severity,
			Options = options,
			CreatedAt = DateTime.UtcNow
		};

		lock (_lock)
		{
			_toasts.Add(toast);

			if (options.DurationMs > 0)
			{
				var timer = new Timer(_ => Remove(toast.Id), null, options.DurationMs, Timeout.Infinite);
				_timers[toast.Id] = timer;
			}
		}

		OnToastAdded?.Invoke(toast);
	}

	/// <inheritdoc />
	public void ShowSuccess(string message, Action<MokaToastOptions>? configure = null)
		=> Show(message, MokaToastSeverity.Success, configure);

	/// <inheritdoc />
	public void ShowError(string message, Action<MokaToastOptions>? configure = null)
		=> Show(message, MokaToastSeverity.Error, configure);

	/// <inheritdoc />
	public void ShowWarning(string message, Action<MokaToastOptions>? configure = null)
		=> Show(message, MokaToastSeverity.Warning, configure);

	/// <inheritdoc />
	public void ShowInfo(string message, Action<MokaToastOptions>? configure = null)
		=> Show(message, MokaToastSeverity.Info, configure);

	/// <inheritdoc />
	public void Remove(Guid id)
	{
		lock (_lock)
		{
			int index = _toasts.FindIndex(t => t.Id == id);
			if (index < 0)
			{
				return;
			}

			_toasts.RemoveAt(index);

			if (_timers.Remove(id, out Timer? timer))
			{
				timer.Dispose();
			}
		}

		OnToastRemoved?.Invoke(id);
	}

	/// <inheritdoc />
	public void Clear()
	{
		List<Guid> ids;
		lock (_lock)
		{
			ids = _toasts.Select(t => t.Id).ToList();
			_toasts.Clear();

			foreach (Timer timer in _timers.Values)
			{
				timer.Dispose();
			}

			_timers.Clear();
		}

		foreach (Guid id in ids)
		{
			OnToastRemoved?.Invoke(id);
		}
	}
}
