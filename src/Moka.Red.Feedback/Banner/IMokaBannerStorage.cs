namespace Moka.Red.Feedback.Banner;

/// <summary>
///     Remembers which banners people dismissed, for banners with <see cref="MokaBanner.Remember" />. The default keeps
///     them in the browser's local storage; register your own scoped implementation to keep them with the person's
///     account instead. <c>AddMokaFeedback()</c> registers the default only when none is registered.
/// </summary>
public interface IMokaBannerStorage
{
	/// <summary>Whether the banner <paramref name="id" /> was dismissed and remembered.</summary>
	/// <param name="id">The banner's id.</param>
	/// <param name="cancellationToken">Cancels the read.</param>
	/// <returns>True when it was dismissed.</returns>
	ValueTask<bool> IsDismissedAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>Remembers that the banner <paramref name="id" /> was dismissed.</summary>
	/// <param name="id">The banner's id.</param>
	/// <param name="cancellationToken">Cancels the write.</param>
	/// <returns>A task that finishes once it is written.</returns>
	ValueTask RememberDismissedAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>Forgets a dismissal, so the banner shows again the next time the app shows it.</summary>
	/// <param name="id">The banner's id.</param>
	/// <param name="cancellationToken">Cancels the write.</param>
	/// <returns>A task that finishes once it is forgotten.</returns>
	ValueTask ForgetAsync(string id, CancellationToken cancellationToken = default);
}
