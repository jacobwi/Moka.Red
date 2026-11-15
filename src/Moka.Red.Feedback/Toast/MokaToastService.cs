using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.Notification;

namespace Moka.Red.Feedback.Toast;

/// <summary>
///     The default <see cref="IMokaToastService" />. It keeps no timers: a toast's countdown is the animation its host
///     draws, which waits while the toasts are pointed at, focused or out of sight, and the host reports when it ends.
/// </summary>
public sealed class MokaToastService : IMokaToastService
{
	private const double EstimateAfterShare = 0.05;
	private static readonly TimeSpan EstimateAfterTime = TimeSpan.FromSeconds(3);

	private readonly IMokaNotificationService? _center;
	private readonly TimeProvider _clock;
	private readonly Action<MokaToastOptions>? _defaults;
	private readonly Lock _lock = new();
	private ReadOnlyCollection<MokaToastMessage> _snapshot = ReadOnlyCollection<MokaToastMessage>.Empty;
	private MokaToastMessage[] _toasts = [];

	/// <summary>A toast service.</summary>
	/// <param name="notifications">
	///     Where <see cref="MokaToastOptions.AlsoInCenter" /> toasts go too. Null leaves that option without effect.
	/// </param>
	/// <param name="clock">Gives toasts their times. Null takes the system clock.</param>
	/// <param name="options">The app's <see cref="MokaFeedbackOptions.ToastDefaults" />. Null leaves toasts as they come.</param>
	public MokaToastService(IMokaNotificationService? notifications = null, TimeProvider? clock = null,
		IOptions<MokaFeedbackOptions>? options = null)
	{
		_center = notifications;
		_clock = clock ?? TimeProvider.System;
		_defaults = options?.Value.ToastDefaults;
	}

	/// <inheritdoc />
	public IReadOnlyList<MokaToastMessage> Toasts => Volatile.Read(ref _snapshot);

	/// <inheritdoc />
	public event Action? Changed;

	/// <inheritdoc />
	public MokaToastHandle Show(string message, MokaToastSeverity severity = MokaToastSeverity.Info,
		Action<MokaToastOptions>? configure = null)
	{
		ArgumentNullException.ThrowIfNull(message);

		MokaToastOptions options = new() { Message = message, Severity = severity };
		_defaults?.Invoke(options);
		configure?.Invoke(options);
		return ShowCore(options);
	}

	/// <inheritdoc />
	public MokaToastHandle ShowSuccess(string message, Action<MokaToastOptions>? configure = null)
		=> Show(message, MokaToastSeverity.Success, configure);

	/// <inheritdoc />
	public MokaToastHandle ShowError(string message, Action<MokaToastOptions>? configure = null)
		=> Show(message, MokaToastSeverity.Error, configure);

	/// <inheritdoc />
	public MokaToastHandle ShowWarning(string message, Action<MokaToastOptions>? configure = null)
		=> Show(message, MokaToastSeverity.Warning, configure);

	/// <inheritdoc />
	public MokaToastHandle ShowInfo(string message, Action<MokaToastOptions>? configure = null)
		=> Show(message, MokaToastSeverity.Info, configure);

	/// <inheritdoc />
	public bool Update(Guid id, Action<MokaToastOptions> change)
	{
		ArgumentNullException.ThrowIfNull(change);

		// The change is the app's code, so it runs outside the lock. Should another thread replace the toast meanwhile,
		// the change runs again on what that thread left, so neither update is lost.
		while (true)
		{
			MokaToastMessage? current = Find(Volatile.Read(ref _toasts), id);
			if (current is null)
			{
				return false;
			}

			MokaToastOptions options = MokaToastOptions.From(current);
			change(options);
			MokaToastMessage next = Next(current, options, _clock.GetUtcNow(), current.Count);

			lock (_lock)
			{
				int index = IndexOf(_toasts, id);
				if (index < 0)
				{
					return false;
				}

				if (!ReferenceEquals(_toasts[index], current))
				{
					continue;
				}

				Replace(index, next);
			}

			Changed?.Invoke();
			Mirror(current, next);
			return true;
		}
	}

	/// <inheritdoc />
	public void Dismiss(Guid id, MokaToastDismissReason reason = MokaToastDismissReason.Dismissed)
	{
		MokaToastMessage? removed;
		lock (_lock)
		{
			int index = IndexOf(_toasts, id);
			if (index < 0)
			{
				return;
			}

			removed = _toasts[index];
			RemoveAt(index);
		}

		Changed?.Invoke();
		removed.OnDismissed?.Invoke(reason);
	}

	/// <inheritdoc />
	public bool Expire(Guid id, int run)
	{
		MokaToastMessage? removed;
		lock (_lock)
		{
			int index = IndexOf(_toasts, id);
			if (index < 0 || _toasts[index].Run != run || !_toasts[index].CountsDown)
			{
				return false;
			}

			removed = _toasts[index];
			RemoveAt(index);
		}

		Changed?.Invoke();
		removed.OnDismissed?.Invoke(MokaToastDismissReason.Expired);
		return true;
	}

	/// <inheritdoc />
	[SuppressMessage("Design", "CA1031:Do not catch general exception types",
		Justification = "Every callback runs; the failures are thrown together as one AggregateException.")]
	public void Clear()
	{
		MokaToastMessage[] removed;
		lock (_lock)
		{
			removed = _toasts;
			if (removed.Length == 0)
			{
				return;
			}

			Publish([]);
		}

		Changed?.Invoke();

		// Every toast hears it went, even when an earlier one's callback fails; the failures are thrown together.
		List<Exception>? failures = null;
		foreach (MokaToastMessage toast in removed)
		{
			try
			{
				toast.OnDismissed?.Invoke(MokaToastDismissReason.Dismissed);
			}
			catch (Exception ex)
			{
				(failures ??= []).Add(ex);
			}
		}

		if (failures is not null)
		{
			throw new AggregateException(failures);
		}
	}

	/// <inheritdoc />
	public Task Track(Task task, string loading, string success, Func<Exception, string> failure,
		string? cancelled = null, Action<MokaToastOptions>? configure = null)
	{
		ArgumentNullException.ThrowIfNull(task);
		ArgumentNullException.ThrowIfNull(success);
		ArgumentNullException.ThrowIfNull(failure);

		MokaToastHandle handle = ShowLoading(loading, configure);
		return SettleAsync(task, handle, success, failure, cancelled);
	}

	/// <inheritdoc />
	public Task<T> Track<T>(Task<T> task, string loading, Func<T, string> success, Func<Exception, string> failure,
		string? cancelled = null, Action<MokaToastOptions>? configure = null)
	{
		ArgumentNullException.ThrowIfNull(task);
		ArgumentNullException.ThrowIfNull(success);
		ArgumentNullException.ThrowIfNull(failure);

		MokaToastHandle handle = ShowLoading(loading, configure);
		return SettleAsync(task, handle, success, failure, cancelled);
	}

	private MokaToastHandle ShowLoading(string loading, Action<MokaToastOptions>? configure)
	{
		ArgumentNullException.ThrowIfNull(loading);

		MokaToastOptions options = new() { Message = loading };
		_defaults?.Invoke(options);
		configure?.Invoke(options);
		options.Message = loading;
		options.Progress ??= MokaToastProgress.Indeterminate;
		return ShowCore(options);
	}

	private static async Task SettleAsync(Task task, MokaToastHandle handle, string success,
		Func<Exception, string> failure, string? cancelled)
	{
		try
		{
			await task;
		}
		catch (OperationCanceledException) when (task.IsCanceled)
		{
			Cancel(handle, cancelled);
			throw;
		}
		catch (Exception ex)
		{
			Finish(handle, failure(ex), MokaToastSeverity.Error);
			throw;
		}

		Finish(handle, success, MokaToastSeverity.Success);
	}

	private static async Task<T> SettleAsync<T>(Task<T> task, MokaToastHandle handle, Func<T, string> success,
		Func<Exception, string> failure, string? cancelled)
	{
		T value;
		try
		{
			value = await task;
		}
		catch (OperationCanceledException) when (task.IsCanceled)
		{
			Cancel(handle, cancelled);
			throw;
		}
		catch (Exception ex)
		{
			Finish(handle, failure(ex), MokaToastSeverity.Error);
			throw;
		}

		Finish(handle, success(value), MokaToastSeverity.Success);
		return value;
	}

	private static void Finish(MokaToastHandle handle, string message, MokaToastSeverity severity) =>
		handle.Update(options =>
		{
			options.Message = message;
			options.Severity = severity;
			options.Progress = null;
		});

	private static void Cancel(MokaToastHandle handle, string? cancelled)
	{
		if (cancelled is null)
		{
			handle.Dismiss();
		}
		else
		{
			Finish(handle, cancelled, MokaToastSeverity.Info);
		}
	}

	private MokaToastHandle ShowCore(MokaToastOptions options)
	{
		DateTimeOffset now = _clock.GetUtcNow();
		MokaToastMessage? before;
		MokaToastMessage shown;

		lock (_lock)
		{
			int index = RepeatIndex(options);
			if (index >= 0)
			{
				before = _toasts[index];
				int count = options.Repeats == MokaToastRepeats.Count ? before.Count + 1 : 1;
				shown = Next(before, options, now, count, restart: true);
				Replace(index, shown);
			}
			else
			{
				before = null;
				shown = Next(null, options, now, 1);
				MokaToastMessage[] next = new MokaToastMessage[_toasts.Length + 1];
				_toasts.CopyTo(next, 0);
				next[^1] = shown;
				Publish(next);
			}
		}

		Changed?.Invoke();
		Mirror(before, shown);
		return new MokaToastHandle(this, shown.Id);
	}

	// A key keeps one toast per key whatever the repeat setting. Without one only Count and Replace look for a toast
	// to repeat, matching the words it shows.
	private int RepeatIndex(MokaToastOptions options)
	{
		for (int i = 0; i < _toasts.Length; i++)
		{
			MokaToastMessage toast = _toasts[i];
			bool repeats = options.Key is not null
				? string.Equals(toast.Key, options.Key, StringComparison.Ordinal)
				: options.Repeats != MokaToastRepeats.Stack && toast.Key is null && SameWords(toast, options);
			if (repeats)
			{
				return i;
			}
		}

		return -1;
	}

	private static bool SameWords(MokaToastMessage toast, MokaToastOptions options) =>
		toast.Severity == options.Severity &&
		string.Equals(toast.Title, options.Title, StringComparison.Ordinal) &&
		string.Equals(toast.Message, options.Message, StringComparison.Ordinal);

	private static MokaToastMessage Next(MokaToastMessage? before, MokaToastOptions options, DateTimeOffset now,
		int count, bool restart = false)
	{
		bool wordsChanged = before is null ||
		                    before.Severity != options.Severity ||
		                    !string.Equals(before.Title, options.Title, StringComparison.Ordinal) ||
		                    !string.Equals(before.Message, options.Message, StringComparison.Ordinal);
		bool progressEnded = before?.Progress is not null && options.Progress is null;
		bool settled = options.Progress is null;
		bool announce = before is not null && (progressEnded || (wordsChanged && settled));
		bool newCountdown = before is not null &&
		                    (restart || progressEnded || (wordsChanged && settled) || before.Duration != options.Duration);

		(DateTimeOffset? startedAt, double startValue, TimeSpan? estimate) = Estimate(before, options.Progress, now);

		return new MokaToastMessage
		{
			Id = before?.Id ?? Guid.NewGuid(),
			Message = options.Message,
			Severity = options.Severity,
			Title = options.Title,
			Duration = options.Duration,
			ShowCloseButton = options.ShowCloseButton,
			ShowIcon = options.ShowIcon,
			Icon = options.Icon,
			OnClick = options.OnClick,
			OnClickAsync = options.OnClickAsync,
			ActionText = options.ActionText,
			OnAction = options.OnAction,
			OnActionAsync = options.OnActionAsync,
			CloseOnAction = options.CloseOnAction,
			Key = options.Key,
			Repeats = options.Repeats,
			Progress = options.Progress,
			ProgressLook = options.ProgressLook,
			AlsoInCenter = options.AlsoInCenter,
			Content = options.Content,
			OnDismissed = options.OnDismissed,
			Count = count,
			Run = before is null ? 0 : before.Run + (newCountdown ? 1 : 0),
			Announcement = before is null ? 0 : before.Announcement + (announce ? 1 : 0),
			CreatedAt = before?.CreatedAt ?? now,
			UpdatedAt = now,
			ProgressStartedAt = startedAt,
			ProgressStartValue = startValue,
			EstimatedRemaining = estimate
		};
	}

	// The rest of the work is guessed from the average pace since the share first had a value, and only once the share
	// has moved far enough for a long enough time that a stall or a jump at the start doesn't decide it.
	private static (DateTimeOffset? StartedAt, double StartValue, TimeSpan? Estimate) Estimate(
		MokaToastMessage? before, MokaToastProgress? progress, DateTimeOffset now)
	{
		if (progress?.Value is not { } value)
		{
			return (null, 0, null);
		}

		if (before?.ProgressStartedAt is not { } startedAt || before.Progress?.Value is null)
		{
			return (now, value, null);
		}

		TimeSpan elapsed = now - startedAt;
		double moved = value - before.ProgressStartValue;
		TimeSpan? estimate = elapsed >= EstimateAfterTime && moved >= EstimateAfterShare && value < 1
			? elapsed * ((1 - value) / moved)
			: null;
		return (startedAt, before.ProgressStartValue, estimate);
	}

	// A toast that also goes to the center is put there when it's shown and again whenever its words are read anew,
	// under one key, so the center keeps one entry that ends up holding the result.
	private void Mirror(MokaToastMessage? before, MokaToastMessage after)
	{
		if (_center is null || !after.AlsoInCenter ||
		    (before is not null && before.Announcement == after.Announcement && before.AlsoInCenter))
		{
			return;
		}

		Func<Task>? action = after.OnActionAsync ?? Wrap(after.OnAction);
		_center.Push(new MokaNotification
		{
			Key = after.Key ?? $"moka-toast-{after.Id:N}",
			Title = after.Title ?? after.Message,
			Message = after.Title is null ? "" : after.Message,
			Severity = after.Severity,
			Icon = after.Icon,
			Timestamp = after.UpdatedAt.UtcDateTime,
			Actions = action is null || after.ActionText is null ? [] : [new MokaNotificationAction(after.ActionText, action)]
		});
	}

	private static Func<Task>? Wrap(Action? action) => action is null
		? null
		: () =>
		{
			action();
			return Task.CompletedTask;
		};

	private static MokaToastMessage? Find(MokaToastMessage[] toasts, Guid id)
	{
		int index = IndexOf(toasts, id);
		return index < 0 ? null : toasts[index];
	}

	private static int IndexOf(MokaToastMessage[] toasts, Guid id)
	{
		for (int i = 0; i < toasts.Length; i++)
		{
			if (toasts[i].Id == id)
			{
				return i;
			}
		}

		return -1;
	}

	private void Replace(int index, MokaToastMessage toast)
	{
		MokaToastMessage[] next = (MokaToastMessage[])_toasts.Clone();
		next[index] = toast;
		Publish(next);
	}

	private void RemoveAt(int index)
	{
		MokaToastMessage[] next = new MokaToastMessage[_toasts.Length - 1];
		Array.Copy(_toasts, 0, next, 0, index);
		Array.Copy(_toasts, index + 1, next, index, _toasts.Length - index - 1);
		Publish(next);
	}

	private void Publish(MokaToastMessage[] toasts)
	{
		Volatile.Write(ref _toasts, toasts);
		Volatile.Write(ref _snapshot, toasts.Length == 0
			? ReadOnlyCollection<MokaToastMessage>.Empty
			: Array.AsReadOnly(toasts));
	}
}
