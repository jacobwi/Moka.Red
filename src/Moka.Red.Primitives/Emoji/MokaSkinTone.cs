namespace Moka.Red.Primitives.Emoji;

/// <summary>The skin tone applied to every emoji that takes one, from Unicode's five Fitzpatrick modifiers.</summary>
public enum MokaSkinTone
{
	/// <summary>No modifier: the yellow the emoji font draws.</summary>
	Default,

	/// <summary>Light skin tone, U+1F3FB.</summary>
	Light,

	/// <summary>Medium-light skin tone, U+1F3FC.</summary>
	MediumLight,

	/// <summary>Medium skin tone, U+1F3FD.</summary>
	Medium,

	/// <summary>Medium-dark skin tone, U+1F3FE.</summary>
	MediumDark,

	/// <summary>Dark skin tone, U+1F3FF.</summary>
	Dark
}
