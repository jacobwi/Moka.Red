namespace Moka.Red.Primitives.ColorSwatch;

/// <summary>
///     How <see cref="MokaColorSwatch.AllowCustom" /> asks for a color the palette doesn't have. Each adds the color as
///     a swatch of its own, checked, after the palette.
/// </summary>
public enum MokaSwatchCustomLook
{
	/// <summary>
	///     A dashed circle with a +. It turns into the browser's color button with a check and a cross. The default.
	/// </summary>
	Standard,

	/// <summary>
	///     A swatch-sized circle in every hue with a +, so it reads as "any color". It opens a small panel with the
	///     browser's color button, a hex field and Add.
	/// </summary>
	Rainbow,

	/// <summary>A dashed pill with a rainbow dot and the word Custom, opening the same panel.</summary>
	Pill,

	/// <summary>
	///     A small mono field after the swatches. Six hex digits and Enter add the color, with no panel to open.
	/// </summary>
	Hex
}
