using System.Diagnostics.CodeAnalysis;

namespace Moka.Red.Core.Announcer;

/// <summary>
///     Says things to screen readers that have no place on the screen: "Saved", "3 results", "Connection lost". Words go
///     to two live regions, one polite and one assertive, that a <see cref="MokaAnnouncerHost" /> keeps in the page.
///     Toasts and banners speak through it too, and a <c>MokaToastHost</c> carries a host of its own, so an app with one
///     needs nothing more. Safe to call from any thread.
/// </summary>
[SuppressMessage("Design", "CA1003:Use generic event handler instances",
	Justification = "A parameterless Action is simpler for this lightweight state-changed event.")]
public interface IMokaAnnouncer
{
	/// <summary>
	///     The latest announcements, oldest first: up to three of each politeness. Hosts draw these, so a message that
	///     arrives while another is being spoken is still heard.
	/// </summary>
	IReadOnlyList<MokaAnnouncement> Recent { get; }

	/// <summary>
	///     The host that speaks, or null when none is in the page. Every host keeps empty regions in the page, so another
	///     can take over at once when this one goes; only the speaker fills its regions, so nothing is heard twice.
	/// </summary>
	object? Speaker { get; }

	/// <summary>Raised when <see cref="Recent" /> or <see cref="Speaker" /> changes, on the thread that changed it.</summary>
	event Action? Changed;

	/// <summary>Says <paramref name="text" /> to screen readers.</summary>
	/// <param name="text">The words. Short and complete: a screen reader speaks them once, out of context.</param>
	/// <param name="politeness">
	///     <see cref="MokaPoliteness.Polite" /> waits its turn. <see cref="MokaPoliteness.Assertive" /> interrupts, so keep
	///     it for errors and anything else that can't wait.
	/// </param>
	void Announce(string text, MokaPoliteness politeness = MokaPoliteness.Polite);

	/// <summary>For hosts: joins the queue of hosts. The first one joined speaks until it leaves.</summary>
	/// <param name="host">The host, usually the component itself.</param>
	void Join(object host);

	/// <summary>For hosts: leaves the queue. The next host in it speaks.</summary>
	/// <param name="host">The host given to <see cref="Join" />.</param>
	void Leave(object host);
}
