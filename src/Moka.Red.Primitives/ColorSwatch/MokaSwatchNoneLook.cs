namespace Moka.Red.Primitives.ColorSwatch;

/// <summary>How <see cref="MokaColorSwatch.ShowNone" /> draws the swatch that chooses no color.</summary>
public enum MokaSwatchNoneLook
{
	/// <summary>A swatch with a red slash through it, as office apps draw "no fill". The default.</summary>
	Standard,

	/// <summary>
	///     A small pill that says <see cref="MokaColorSwatchText.NoneChip" />, filled in the text color while chosen. A
	///     screen reader and an eye read the same word.
	/// </summary>
	Chip,

	/// <summary>A dashed outline where a color would be. Quiet, and it keeps the row all circles.</summary>
	Empty,

	/// <summary>The checkerboard design tools use for transparent, for a fill or a highlight.</summary>
	Checkerboard
}
