namespace Moka.Red.Core.Enums;

/// <summary>
///     A soft light drawn over the pattern of a <c>MokaGridBackground</c>, in the accent unless
///     <c>HighlightColor</c> says otherwise.
/// </summary>
public enum MokaGridGlow
{
	/// <summary>An ellipse behind the middle. The original glow that <c>Highlighted</c> turns on.</summary>
	Center,

	/// <summary>Two soft clouds hanging from the top edge.</summary>
	Aurora,

	/// <summary>One glow from the top right corner, leaving the middle on plain grid.</summary>
	Corner,

	/// <summary>A wide, low band of light right behind a headline.</summary>
	Beam,

	/// <summary>The accent and the info color, overlapping in the middle.</summary>
	TwoTone,

	/// <summary>A glow rising from the bottom edge, like light under a door.</summary>
	Horizon
}
