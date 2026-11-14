namespace Moka.Red.Core.Colors;

/// <summary>A color as hue, saturation and lightness, the CSS <c>hsl()</c> model.</summary>
/// <param name="Hue">The hue in degrees, 0 to 360.</param>
/// <param name="Saturation">The saturation, 0 to 1.</param>
/// <param name="Lightness">The lightness, 0 to 1.</param>
/// <param name="Alpha">Alpha, 0 to 1.</param>
public readonly record struct MokaHsl(double Hue, double Saturation, double Lightness, double Alpha = 1)
{
	/// <summary>The color in sRGB.</summary>
	/// <returns>The sRGB color.</returns>
	public MokaSrgb ToSrgb() => MokaSrgb.FromHsl(Hue, Saturation, Lightness, Alpha);

	/// <summary>The same color as hue, saturation and value.</summary>
	/// <returns>The HSV color.</returns>
	public MokaHsv ToHsv()
	{
		double s = Math.Clamp(Saturation, 0, 1);
		double l = Math.Clamp(Lightness, 0, 1);
		double v = l + (s * Math.Min(l, 1 - l));
		return new MokaHsv(Hue, v == 0 ? 0 : 2 * (1 - (l / v)), v, Alpha);
	}

	/// <summary>A hue in degrees brought into 0 to 360.</summary>
	/// <param name="degrees">Any angle in degrees.</param>
	/// <returns>The same hue, 0 to 360.</returns>
	public static double WrapHue(double degrees)
	{
		double hue = degrees % 360;
		return hue < 0 ? hue + 360 : hue;
	}
}

/// <summary>A color as hue, saturation and value (also called HSB), the model of most color pickers.</summary>
/// <param name="Hue">The hue in degrees, 0 to 360.</param>
/// <param name="Saturation">The saturation, 0 to 1.</param>
/// <param name="Value">The value (brightness), 0 to 1.</param>
/// <param name="Alpha">Alpha, 0 to 1.</param>
public readonly record struct MokaHsv(double Hue, double Saturation, double Value, double Alpha = 1)
{
	/// <summary>The color in sRGB.</summary>
	/// <returns>The sRGB color.</returns>
	public MokaSrgb ToSrgb() => MokaSrgb.FromHsv(this);

	/// <summary>The same color as hue, saturation and lightness.</summary>
	/// <returns>The HSL color.</returns>
	public MokaHsl ToHsl()
	{
		double l = Value * (1 - (Saturation / 2));
		double s = l is <= 0 or >= 1 ? 0 : (Value - l) / Math.Min(l, 1 - l);
		return new MokaHsl(Hue, Math.Clamp(s, 0, 1), Math.Clamp(l, 0, 1), Alpha);
	}
}
