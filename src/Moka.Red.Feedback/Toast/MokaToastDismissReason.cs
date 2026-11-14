namespace Moka.Red.Feedback.Toast;

/// <summary>Why a toast went away. See <see cref="MokaToastOptions.OnDismissed" />.</summary>
public enum MokaToastDismissReason
{
	/// <summary>Its countdown ran out.</summary>
	Expired,

	/// <summary>Someone pressed its close button.</summary>
	Closed,

	/// <summary>Someone swiped it away on a touch screen.</summary>
	Swiped,

	/// <summary>Someone pressed its action button, with <see cref="MokaToastOptions.CloseOnAction" /> on.</summary>
	Action,

	/// <summary>
	///     The app took it away: <see cref="IMokaToastService.Dismiss" />, <see cref="IMokaToastService.Clear" />, a
	///     handle, or a <see cref="MokaToast" /> closed by its page.
	/// </summary>
	Dismissed
}
