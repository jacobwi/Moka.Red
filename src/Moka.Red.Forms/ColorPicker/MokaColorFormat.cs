namespace Moka.Red.Forms.ColorPicker;

/// <summary>How <see cref="MokaColorPicker" /> writes a color when <see cref="MokaColorPicker.ShowFormats" /> is on.</summary>
public enum MokaColorFormat
{
	/// <summary><c>#EF5350</c>, or <c>#EF5350B8</c> with alpha.</summary>
	Hex,

	/// <summary><c>rgb(239 83 80)</c>, or <c>rgb(239 83 80 / 72%)</c> with alpha.</summary>
	Rgb,

	/// <summary><c>hsl(1 83% 63%)</c>.</summary>
	Hsl,

	/// <summary><c>oklch(63.6% 0.183 25.3)</c>, which keeps lightness even across hues.</summary>
	Oklch
}
