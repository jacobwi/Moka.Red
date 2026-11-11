namespace Moka.Red.Forms.ColorPicker;

/// <summary>How <see cref="MokaColorPicker" /> lays out its panel.</summary>
public enum MokaColorPickerLayout
{
	/// <summary>A square, a hue bar, hex and R, G, B fields, presets and recent colors. The original panel.</summary>
	Standard,

	/// <summary>Spectrum and Swatches tabs over one hex row. Swatches holds the theme's colors and the palette.</summary>
	Tabbed,

	/// <summary>A hue ring around a saturation square, with the hex, a copy button and an H, S, B readout.</summary>
	Wheel,

	/// <summary>Hue, saturation and lightness sliders whose tracks show where each goes, each with a number field.</summary>
	Sliders,

	/// <summary>The palette and recent colors on top, and a small spectrum under More colors.</summary>
	PaletteFirst,

	/// <summary>A narrow panel: the square, one hue bar, and the hex with an eyedropper.</summary>
	Compact
}
