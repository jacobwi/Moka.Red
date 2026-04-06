namespace Moka.Red.Primitives.Motion;

/// <summary>
///     Direction from which content fades in when using <see cref="MokaFadeIn" />.
/// </summary>
public enum MokaFadeDirection
{
	/// <summary>Fade in place with no directional movement.</summary>
	None,

	/// <summary>Fade in while sliding upward.</summary>
	Up,

	/// <summary>Fade in while sliding downward.</summary>
	Down,

	/// <summary>Fade in while sliding from the left.</summary>
	Left,

	/// <summary>Fade in while sliding from the right.</summary>
	Right
}
