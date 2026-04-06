namespace Moka.Red.Primitives.Motion;

/// <summary>
///     Animation style applied to each staggered item in <see cref="MokaStagger{TItem}" />.
/// </summary>
public enum MokaStaggerAnimation
{
	/// <summary>Items fade in from transparent.</summary>
	FadeIn,

	/// <summary>Items fade in while sliding upward.</summary>
	SlideUp,

	/// <summary>Items fade in while sliding from the left.</summary>
	SlideLeft,

	/// <summary>Items scale in from a smaller size.</summary>
	ScaleIn
}
