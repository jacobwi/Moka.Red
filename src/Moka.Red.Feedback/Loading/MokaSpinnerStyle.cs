namespace Moka.Red.Feedback.Loading;

/// <summary>Visual style of the spinner animation.</summary>
public enum MokaSpinnerStyle
{
	/// <summary>Classic spinning circle using stroke-dasharray.</summary>
	Circular,

	/// <summary>Three bouncing dots.</summary>
	Dots,

	/// <summary>Pulsing circle that grows and shrinks.</summary>
	Pulse,

	/// <summary>Three vertical bars that animate height.</summary>
	Bars,

	/// <summary>Spinning ring with gradient fade.</summary>
	Ring
}
