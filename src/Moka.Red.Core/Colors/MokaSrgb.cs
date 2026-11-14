using System.Globalization;

namespace Moka.Red.Core.Colors;

/// <summary>
///     A color in sRGB, the space CSS hex and <c>rgb()</c> colors live in, with gamma-encoded channels from 0 to 1 and
///     an alpha from 0 (see-through) to 1 (solid). A channel below 0 or above 1 holds a color sRGB can't show, such as
///     one converted from OKLCH before gamut mapping.
/// </summary>
/// <param name="R">Red, 0 to 1.</param>
/// <param name="G">Green, 0 to 1.</param>
/// <param name="B">Blue, 0 to 1.</param>
/// <param name="Alpha">Alpha, 0 to 1.</param>
public readonly record struct MokaSrgb(double R, double G, double B, double Alpha = 1)
{
	/// <summary>How far past 0 or 1 a channel may sit and still count as inside the gamut, so rounding noise at an edge doesn't push a color out.</summary>
	public const double GamutTolerance = 1e-4;

	/// <summary>Solid black.</summary>
	public static MokaSrgb Black { get; } = new(0, 0, 0);

	/// <summary>Solid white.</summary>
	public static MokaSrgb White { get; } = new(1, 1, 1);

	/// <summary>Transparent black, what CSS's <c>transparent</c> keyword is.</summary>
	public static MokaSrgb Transparent { get; } = new(0, 0, 0, 0);

	/// <summary>Whether every channel is within sRGB, give or take <see cref="GamutTolerance" />.</summary>
	public bool IsInGamut => InUnit(R) && InUnit(G) && InUnit(B);

	/// <summary>Whether the color is solid.</summary>
	public bool IsOpaque => Alpha >= 1;

	/// <summary>The same color, solid.</summary>
	public MokaSrgb Opaque => this with { Alpha = 1 };

	/// <summary>
	///     The WCAG 2 relative luminance, 0 for black to 1 for white, with the channels clipped into sRGB. Alpha is ignored;
	///     composite a translucent color with <see cref="Over" /> first.
	/// </summary>
	public double RelativeLuminance => ToLinear(Clip()).Luminance;

	/// <summary>A color from 8-bit channels, 0 to 255.</summary>
	/// <param name="red">Red, 0 to 255.</param>
	/// <param name="green">Green, 0 to 255.</param>
	/// <param name="blue">Blue, 0 to 255.</param>
	/// <param name="alpha">Alpha, 0 to 1.</param>
	/// <returns>The color.</returns>
	public static MokaSrgb FromBytes(int red, int green, int blue, double alpha = 1) =>
		new(red / 255.0, green / 255.0, blue / 255.0, alpha);

	/// <summary>
	///     A color from hue, saturation and lightness (CSS <c>hsl()</c>). Hue is in degrees from 0 to 360, saturation and
	///     lightness from 0 to 1.
	/// </summary>
	/// <param name="hue">The hue in degrees, 0 to 360.</param>
	/// <param name="saturation">The saturation, 0 to 1.</param>
	/// <param name="lightness">The lightness, 0 to 1.</param>
	/// <param name="alpha">Alpha, 0 to 1.</param>
	/// <returns>The color.</returns>
	public static MokaSrgb FromHsl(double hue, double saturation, double lightness, double alpha = 1)
	{
		double chroma = (1 - Math.Abs((2 * lightness) - 1)) * saturation;
		double x = chroma * (1 - Math.Abs((hue / 60 % 2) - 1));
		double m = lightness - (chroma / 2);
		(double r, double g, double b) = (hue / 60) switch
		{
			< 1 => (chroma, x, 0.0),
			< 2 => (x, chroma, 0.0),
			< 3 => (0.0, chroma, x),
			< 4 => (0.0, x, chroma),
			< 5 => (x, 0.0, chroma),
			_ => (chroma, 0.0, x)
		};

		return new MokaSrgb(Math.Clamp(r + m, 0, 1), Math.Clamp(g + m, 0, 1), Math.Clamp(b + m, 0, 1), alpha);
	}

	/// <summary>A color from hue, saturation and value (HSV, also called HSB), the model of most color pickers.</summary>
	/// <param name="hsv">The hue, saturation and value.</param>
	/// <returns>The color.</returns>
	public static MokaSrgb FromHsv(MokaHsv hsv)
	{
		double h = MokaHsl.WrapHue(hsv.Hue);
		double s = Math.Clamp(hsv.Saturation, 0, 1);
		double v = Math.Clamp(hsv.Value, 0, 1);

		double Channel(int n)
		{
			double k = (n + (h / 60)) % 6;
			return v - (v * s * Math.Max(0, Math.Min(k, Math.Min(4 - k, 1))));
		}

		return new MokaSrgb(Channel(5), Channel(3), Channel(1), Math.Clamp(hsv.Alpha, 0, 1));
	}

	/// <summary>The channels as 8-bit values, clipped into sRGB and rounded half away from zero, as browsers do.</summary>
	/// <returns>Red, green and blue, 0 to 255.</returns>
	public (int R, int G, int B) ToBytes() => ToBytes(MidpointRounding.AwayFromZero);

	/// <summary>
	///     The channels as 8-bit values, clipped into sRGB and rounded with <paramref name="rounding" />, for matching values
	///     another tool wrote, such as one that rounds half to even.
	/// </summary>
	/// <param name="rounding">How a channel exactly halfway between two steps rounds.</param>
	/// <returns>Red, green and blue, 0 to 255.</returns>
	public (int R, int G, int B) ToBytes(MidpointRounding rounding) => (Byte(R, rounding), Byte(G, rounding), Byte(B, rounding));

	/// <summary>The color with every channel clipped into sRGB. Alpha is kept.</summary>
	/// <returns>The clipped color.</returns>
	public MokaSrgb Clip() => new(Math.Clamp(R, 0, 1), Math.Clamp(G, 0, 1), Math.Clamp(B, 0, 1), Math.Clamp(Alpha, 0, 1));

	/// <summary>The color in linear-light sRGB.</summary>
	/// <returns>The linear color.</returns>
	public MokaLinearRgb ToLinear() => ToLinear(this);

	/// <summary>The color in OKLab.</summary>
	/// <returns>The OKLab color.</returns>
	public MokaOklab ToOklab() => ToLinear().ToOklab();

	/// <summary>The color in OKLCH.</summary>
	/// <returns>The OKLCH color.</returns>
	public MokaOklch ToOklch() => ToOklab().ToOklch();

	/// <summary>
	///     Hue, saturation and lightness (CSS <c>hsl()</c>), straight from the channels. A grey has hue 0 and saturation 0.
	///     The same color as <see cref="ToHsv" /> followed by <see cref="MokaHsv.ToHsl" />, to the last bit of a double or
	///     two.
	/// </summary>
	/// <returns>The HSL color.</returns>
	public MokaHsl ToHsl()
	{
		double max = Math.Max(R, Math.Max(G, B));
		double min = Math.Min(R, Math.Min(G, B));
		double delta = max - min;
		double lightness = (max + min) / 2;

		if (delta == 0)
		{
			return new MokaHsl(0, 0, lightness, Alpha);
		}

		double saturation = delta / (1 - Math.Abs((2 * lightness) - 1));
		double hue = max == R ? ((60 * ((G - B) / delta)) + 360) % 360
			: max == G ? 60 * (((B - R) / delta) + 2)
			: 60 * (((R - G) / delta) + 4);

		return new MokaHsl(hue, Math.Clamp(saturation, 0, 1), lightness, Alpha);
	}

	/// <summary>Hue, saturation and value (HSV). A grey has hue 0.</summary>
	/// <returns>The HSV color.</returns>
	public MokaHsv ToHsv()
	{
		double max = Math.Max(R, Math.Max(G, B));
		double min = Math.Min(R, Math.Min(G, B));
		double delta = max - min;
		double hue = 0;
		if (delta != 0)
		{
			double sector = max == R ? ((G - B) / delta) % 6
				: max == G ? ((B - R) / delta) + 2
				: ((R - G) / delta) + 4;
			hue = MokaHsl.WrapHue(sector * 60);
		}

		return new MokaHsv(hue, max == 0 ? 0 : delta / max, max, Alpha);
	}

	/// <summary>
	///     This color drawn over <paramref name="background" />, as the eye sees it. A solid color comes back as it is, and a
	///     solid background gives a solid result.
	/// </summary>
	/// <param name="background">What the color is drawn over.</param>
	/// <returns>The composited color.</returns>
	public MokaSrgb Over(MokaSrgb background)
	{
		double top = Math.Clamp(Alpha, 0, 1);
		double under = Math.Clamp(background.Alpha, 0, 1) * (1 - top);
		double alpha = top + under;
		if (alpha <= 0)
		{
			return Transparent;
		}

		double Channel(double over, double below) => ((over * top) + (below * under)) / alpha;
		return new MokaSrgb(Channel(R, background.R), Channel(G, background.G), Channel(B, background.B), alpha);
	}

	/// <summary>
	///     The color as <c>#rrggbb</c>, clipped into sRGB, with the alpha as a fourth byte (<c>#rrggbbaa</c>) when asked
	///     for and below 1.
	/// </summary>
	/// <param name="uppercase">Whether the digits are uppercase.</param>
	/// <param name="withAlpha">Whether a translucent color keeps its alpha.</param>
	/// <returns>The hex color.</returns>
	public string ToHex(bool uppercase = false, bool withAlpha = false)
	{
		(int r, int g, int b) = ToBytes();
		string hex = uppercase
			? string.Create(CultureInfo.InvariantCulture, $"#{r:X2}{g:X2}{b:X2}")
			: string.Create(CultureInfo.InvariantCulture, $"#{r:x2}{g:x2}{b:x2}");
		if (!withAlpha || Alpha >= 1)
		{
			return hex;
		}

		int alpha = Byte(Alpha);
		return hex + alpha.ToString(uppercase ? "X2" : "x2", CultureInfo.InvariantCulture);
	}

	/// <summary>The color in CSS: hex when solid, <c>rgb()</c> with the alpha otherwise.</summary>
	/// <returns>A CSS color.</returns>
	public override string ToString() => MokaCssColor.Format(this, IsOpaque ? MokaCssFormat.Hex : MokaCssFormat.Rgb);

	/// <summary>Linear light from a gamma-encoded sRGB channel, carried on past 0 and 1 the way CSS Color 4 extends it.</summary>
	/// <param name="channel">The encoded channel.</param>
	/// <returns>The linear channel.</returns>
	public static double Linearize(double channel)
	{
		double magnitude = Math.Abs(channel);
		double linear = magnitude <= 0.04045 ? magnitude / 12.92 : Math.Pow((magnitude + 0.055) / 1.055, 2.4);
		return channel < 0 ? -linear : linear;
	}

	/// <summary>A gamma-encoded sRGB channel from linear light, carried on past 0 and 1 the way CSS Color 4 extends it.</summary>
	/// <param name="linear">The linear channel.</param>
	/// <returns>The encoded channel.</returns>
	public static double Encode(double linear)
	{
		double magnitude = Math.Abs(linear);
		double encoded = magnitude <= 0.0031308 ? 12.92 * magnitude : (1.055 * Math.Pow(magnitude, 1 / 2.4)) - 0.055;
		return linear < 0 ? -encoded : encoded;
	}

	private static MokaLinearRgb ToLinear(MokaSrgb color) =>
		new(Linearize(color.R), Linearize(color.G), Linearize(color.B), color.Alpha);

	private static bool InUnit(double channel) => channel is >= -GamutTolerance and <= 1 + GamutTolerance;

	private static int Byte(double unit, MidpointRounding rounding = MidpointRounding.AwayFromZero) =>
		(int)Math.Round(Math.Clamp(unit, 0, 1) * 255, rounding);
}
