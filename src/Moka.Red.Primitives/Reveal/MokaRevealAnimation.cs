namespace Moka.Red.Primitives.Reveal;

/// <summary>
///     Animation type for the <see cref="MokaReveal" /> scroll-triggered animation wrapper.
/// </summary>
public enum MokaRevealAnimation
{
	/// <summary>Simple fade in.</summary>
	FadeIn,

	/// <summary>Fade in while sliding up.</summary>
	FadeUp,

	/// <summary>Fade in while sliding down.</summary>
	FadeDown,

	/// <summary>Fade in while sliding from the left.</summary>
	FadeLeft,

	/// <summary>Fade in while sliding from the right.</summary>
	FadeRight,

	/// <summary>Fade in with scale from smaller to full size.</summary>
	ScaleIn,

	/// <summary>Slide up from below without fade.</summary>
	SlideUp
}
