namespace Moka.Red.Feedback.SystemNotification;

/// <summary>What became of a <see cref="IMokaSystemNotifier.NotifyAsync" /> call.</summary>
public enum MokaSystemNotificationResult
{
	/// <summary>The system shows it.</summary>
	Shown,

	/// <summary>Not shown, since <see cref="MokaSystemNotification.OnlyWhenHidden" /> was set and the page is in sight.</summary>
	PageVisible,

	/// <summary>Not shown, since the person hasn't allowed notifications (not asked yet, or refused).</summary>
	NotAllowed,

	/// <summary>Not shown, since the host can't show them. See <see cref="MokaSystemNotificationConsent.Unsupported" />.</summary>
	Unsupported
}
