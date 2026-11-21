using Moka.Red.Core.Icons;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.Feedback.Banner;

/// <summary>
///     An app-wide notice along the top of the page, such as planned maintenance, being offline or a new version, shown
///     through <see cref="IMokaBannerService" /> and drawn by <see cref="MokaBannerHost" />.
/// </summary>
public sealed record MokaBanner
{
	/// <summary>
	///     Names the banner. Showing another with the same id replaces it in place, and a dismissal is remembered by it,
	///     so give a notice that should come back once it changes a new id, such as <c>maintenance-2026-11-20</c>.
	/// </summary>
	public required string Id { get; init; }

	/// <summary>The notice.</summary>
	public required string Message { get; init; }

	/// <summary>Optional words in bold before the message.</summary>
	public string? Title { get; init; }

	/// <summary>Color and icon. <see cref="MokaToastSeverity.Info" /> unless set.</summary>
	public MokaToastSeverity Severity { get; init; } = MokaToastSeverity.Info;

	/// <summary>An icon in place of the severity's own.</summary>
	public MokaIconDefinition? Icon { get; init; }

	/// <summary>Whether the icon shows. On by default.</summary>
	public bool ShowIcon { get; init; } = true;

	/// <summary>Whether people can close it. On by default; off for a notice that lasts as long as its cause, like Offline.</summary>
	public bool Dismissible { get; init; } = true;

	/// <summary>
	///     Whether a dismissal is remembered across visits, through <see cref="IMokaBannerStorage" /> (the browser's local
	///     storage unless the app registered its own). Off by default, so a notice that comes back on its own, such as
	///     Offline, isn't silenced for good. A remembered banner is drawn only once the storage has said it wasn't
	///     dismissed, so it never flashes up and vanishes.
	/// </summary>
	public bool Remember { get; init; }

	/// <summary>The words of a link after the message, such as "Details". Needs <see cref="Href" />.</summary>
	public string? LinkText { get; init; }

	/// <summary>Where the link goes. A <c>javascript:</c>, <c>vbscript:</c> or <c>data:</c> address is dropped.</summary>
	public string? Href { get; init; }

	/// <summary>
	///     Where the link opens, such as <c>_blank</c>. Needs <see cref="Href" />. One that opens another tab gets
	///     <c>rel="noopener noreferrer"</c>, and screen readers hear <see cref="MokaBannerText.NewTab" /> after its words.
	///     <c>_self</c>, <c>_parent</c> and <c>_top</c> render as given and add nothing.
	/// </summary>
	public string? Target { get; init; }

	/// <summary>The words on an action button, such as "Reload". Needs <see cref="OnAction" />.</summary>
	public string? ActionText { get; init; }

	/// <summary>Runs when the action button is pressed. A failure reaches the app's error handling.</summary>
	public Func<Task>? OnAction { get; init; }
}
