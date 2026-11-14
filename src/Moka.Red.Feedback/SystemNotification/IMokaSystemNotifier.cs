namespace Moka.Red.Feedback.SystemNotification;

/// <summary>
///     Shows notifications outside the page through the browser's Notification API, for news worth hearing while the
///     app isn't in sight. Opt-in: register it with <c>AddMokaSystemNotifier()</c>. It never asks for permission on its
///     own; the app asks with <see cref="RequestPermissionAsync" />, from something the person pressed. A host without
///     the API, such as a MAUI web view, is reported as unsupported rather than failing. Call it only from an
///     interactive render, since it talks to the browser.
/// </summary>
public interface IMokaSystemNotifier
{
	/// <summary>Whether the page may show notifications, without asking.</summary>
	/// <param name="cancellationToken">Cancels the call.</param>
	/// <returns>The permission as it stands.</returns>
	ValueTask<MokaSystemNotificationConsent> GetPermissionAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     Asks the person whether the page may show notifications, unless they already answered. Call it from a click:
	///     browsers refuse to ask otherwise, and Safari only asks while the click is recent, which a Blazor Server round
	///     trip on a slow connection can outlast.
	/// </summary>
	/// <param name="cancellationToken">Cancels the call.</param>
	/// <returns>The permission after the person answered.</returns>
	ValueTask<MokaSystemNotificationConsent> RequestPermissionAsync(CancellationToken cancellationToken = default);

	/// <summary>Shows a notification, if the person allowed it and the host can.</summary>
	/// <param name="notification">The notification.</param>
	/// <param name="cancellationToken">Cancels the call.</param>
	/// <returns>Whether it was shown, and why not when it wasn't.</returns>
	ValueTask<MokaSystemNotificationResult> NotifyAsync(MokaSystemNotification notification,
		CancellationToken cancellationToken = default);
}
