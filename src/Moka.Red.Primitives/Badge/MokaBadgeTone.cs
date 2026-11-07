namespace Moka.Red.Primitives.Badge;

/// <summary>What a count means. Each tone keeps one meaning on every theme, so a color never has to be learned twice.</summary>
public enum MokaBadgeTone
{
	/// <summary>Grey. It only counts things.</summary>
	Count,

	/// <summary>Amber. Something is limited right now.</summary>
	Limited,

	/// <summary>Red. Something needs the person to act.</summary>
	NeedsYou,

	/// <summary>Blue on every preset, so it can't be mistaken for <see cref="NeedsYou" /> on a red accent. Something is ready, like an update to install.</summary>
	Ready
}
