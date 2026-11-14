namespace Moka.Red.Feedback.SystemNotification;

/// <summary>Whether the page may show system notifications.</summary>
public enum MokaSystemNotificationConsent
{
	/// <summary>The host has no Notification API, such as a MAUI web view or an old browser, or allows none here.</summary>
	Unsupported,

	/// <summary>The person hasn't been asked yet. <see cref="IMokaSystemNotifier.RequestPermissionAsync" /> asks.</summary>
	NotAsked,

	/// <summary>The person allowed them.</summary>
	Granted,

	/// <summary>The person refused them. Only they can change it, in the browser's site settings.</summary>
	Denied
}
