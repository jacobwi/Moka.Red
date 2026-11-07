namespace Moka.Red.Feedback.BootScreen;

/// <summary>How a <see cref="MokaBootScreen" /> draws.</summary>
public enum MokaBootVariant
{
	/// <summary>
	///     The brand in the title font, a thin line under it and one sentence. The default on the new design when no
	///     variant is named.
	/// </summary>
	Mark,

	/// <summary>One mono line per step with its time and state word, for apps whose people like to see the machinery.</summary>
	Log,

	/// <summary>The brand on the left and a checklist of steps on the right, for a startup with known steps.</summary>
	Steps,

	/// <summary>
	///     The original boot screen: the brand in the accent with a glow, a pulsing status, chips, a faint grid, a
	///     scanline and a bar. The default on <c>MokaTheme.Dark</c> and <c>Light</c> when no variant is named.
	/// </summary>
	Classic,

	/// <summary>A glowing mono wordmark over a terminal boot sequence, with a blinking block cursor.</summary>
	Terminal,

	/// <summary>Rings breathing out from a glowing core while dots orbit it, over a glowing wordmark.</summary>
	Pulse,

	/// <summary>A glowing wordmark over a grid plane that runs toward a lit horizon.</summary>
	Horizon
}
