namespace Moka.Red.Core.Enums;

/// <summary>Visual style for countdown timer display.</summary>
public enum MokaCountdownStyle
{
	/// <summary>Each unit in its own box.</summary>
	Boxes,

	/// <summary>All units in one line (e.g., 02:15:30:45).</summary>
	Inline,

	/// <summary>
	///     Each value on a card that flips in when the value changes (a CSS animation, cut short when the
	///     user asks the OS to reduce motion). The first render does not flip.
	/// </summary>
	Flip
}
