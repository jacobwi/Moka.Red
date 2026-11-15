using Moka.Red.Feedback.Notification;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.Feedback.Extensions;

/// <summary>
///     App-wide settings for the Feedback services. Set them through
///     <see cref="ServiceCollectionExtensions.AddMokaFeedback(Microsoft.Extensions.DependencyInjection.IServiceCollection, Action{MokaFeedbackOptions})" />,
///     the meta-package's <c>MokaRedOptions.Feedback</c>, or <c>services.Configure&lt;MokaFeedbackOptions&gt;</c>; several
///     of them add up.
/// </summary>
public sealed class MokaFeedbackOptions
{
	private int? _notificationLimit;

	/// <summary>
	///     Runs on every toast the app shows, after its message and severity are set and before the call's own
	///     <c>configure</c>, so a call still sets what it wants. It reaches <c>Show</c>, the <c>ShowSuccess</c>-style
	///     helpers, the loading toast of <c>Track</c> and <see cref="MokaToast" /> in markup, where a parameter the page
	///     passes wins. A toast that changes later, through <c>Update</c> or a loading toast settling, keeps what it had.
	///     Null, the default, leaves toasts as <see cref="MokaToastOptions" /> makes them.
	/// </summary>
	public Action<MokaToastOptions>? ToastDefaults { get; set; }

	/// <summary>
	///     The most notifications <see cref="IMokaNotificationService" /> keeps. A push that takes the list past it removes
	///     the oldest read notifications first, then the oldest unread ones, never the one just pushed. A push that
	///     replaces an entry by its id or key removes nothing. Null, the default, keeps every notification.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">The value is below 1.</exception>
	public int? NotificationLimit
	{
		get => _notificationLimit;
		set
		{
			if (value is { } limit)
			{
				ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1, nameof(value));
			}

			_notificationLimit = value;
		}
	}
}
