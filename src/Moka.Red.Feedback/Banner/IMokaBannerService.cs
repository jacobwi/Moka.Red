using System.Diagnostics.CodeAnalysis;

namespace Moka.Red.Feedback.Banner;

/// <summary>
///     Shows app-wide notices along the top of the page. Registered scoped by <c>AddMokaFeedback()</c>; one
///     <see cref="MokaBannerHost" /> in the layout draws them. Safe to call from any thread.
/// </summary>
[SuppressMessage("Design", "CA1003:Use generic event handler instances",
	Justification = "A parameterless Action is simpler for this lightweight state-changed event.")]
public interface IMokaBannerService
{
	/// <summary>The banners shown, oldest first. An immutable snapshot: a change replaces the list.</summary>
	IReadOnlyList<MokaBanner> Banners { get; }

	/// <summary>Raised after every change to <see cref="Banners" />, on the thread that made it.</summary>
	event Action? Changed;

	/// <summary>Shows a banner, or replaces the one with its id in place.</summary>
	/// <param name="banner">The banner.</param>
	void Show(MokaBanner banner);

	/// <summary>Takes a banner away, if it's shown. Nothing is remembered: it shows again when the app shows it.</summary>
	/// <param name="id">The banner's id.</param>
	void Dismiss(string id);

	/// <summary>Takes every banner away.</summary>
	void Clear();
}
