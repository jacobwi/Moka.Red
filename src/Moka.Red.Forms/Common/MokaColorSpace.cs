using System.Globalization;
using Moka.Red.Core.Colors;
using Moka.Red.Forms.ColorPicker;

namespace Moka.Red.Forms.Common;

/// <summary>
///     The color math the color inputs share, on Core's color library: reading hex and <c>rgb()</c>, HSV, HSL and OKLCH,
///     WCAG contrast, and writing a color in each format. Hue is in degrees from 0 to 360; saturation, value, lightness
///     and alpha run from 0 to 1.
/// </summary>
internal static class MokaColorSpace
{
	/// <summary>
	///     Reads a hex color (<c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c>, <c>#rrggbbaa</c>) or an <c>rgb()</c> or
	///     <c>rgba()</c> color with commas or spaces and an optional alpha as a number or a percentage.
	/// </summary>
	internal static bool TryParse(string? text, out MokaRgba color)
	{
		color = default;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		string value = text.Trim();
		MokaSrgb parsed = default;
		bool read = value[0] == '#'
			? MokaCssColor.TryParseHex(value, out parsed)
			: IsRgbFunction(value) && MokaCssColor.TryParse(value, out parsed);
		if (read)
		{
			color = From(parsed);
		}

		return read;
	}

	/// <summary>The color as lowercase <c>#rrggbb</c>, with the alpha as a fourth byte when asked and below 1.</summary>
	internal static string ToHex(MokaRgba color, bool withAlpha = false) => To(color).ToHex(withAlpha: withAlpha);

	/// <summary>The color from hue, saturation and value.</summary>
	internal static MokaRgba FromHsv(double hue, double saturation, double value, double alpha = 1) =>
		From(MokaSrgb.FromHsv(new MokaHsv(hue, saturation, value, alpha)));

	/// <summary>Hue, saturation and value. A grey has hue 0.</summary>
	internal static (double Hue, double Saturation, double Value) ToHsv(MokaRgba color)
	{
		MokaHsv hsv = To(color).ToHsv();
		return (hsv.Hue, hsv.Saturation, hsv.Value);
	}

	/// <summary>The color from hue, saturation and lightness.</summary>
	internal static MokaRgba FromHsl(double hue, double saturation, double lightness, double alpha = 1) =>
		From(MokaSrgb.FromHsv(new MokaHsl(hue, saturation, lightness, alpha).ToHsv()));

	/// <summary>Hue, saturation and lightness. A grey has hue 0.</summary>
	internal static (double Hue, double Saturation, double Lightness) ToHsl(MokaRgba color)
	{
		MokaHsl hsl = To(color).ToHsv().ToHsl();
		return (hsl.Hue, hsl.Saturation, hsl.Lightness);
	}

	/// <summary>Hue, saturation and value as hue, saturation and lightness.</summary>
	internal static (double Hue, double Saturation, double Lightness) HsvToHsl(double hue, double saturation, double value)
	{
		MokaHsl hsl = new MokaHsv(hue, saturation, value).ToHsl();
		return (hsl.Hue, hsl.Saturation, hsl.Lightness);
	}

	/// <summary>Hue, saturation and lightness as hue, saturation and value.</summary>
	internal static (double Hue, double Saturation, double Value) HslToHsv(double hue, double saturation, double lightness)
	{
		MokaHsv hsv = new MokaHsl(hue, saturation, lightness).ToHsv();
		return (hsv.Hue, hsv.Saturation, hsv.Value);
	}

	/// <summary>OKLCH lightness (0 to 1), chroma and hue in degrees. A color with almost no chroma has hue 0.</summary>
	internal static (double Lightness, double Chroma, double Hue) ToOklch(MokaRgba color)
	{
		MokaOklch oklch = To(color).ToOklch();
		return (oklch.L, oklch.C, oklch.C < MokaCssColor.AchromaticChroma ? 0 : oklch.H);
	}

	/// <summary>The sRGB color for an OKLCH color, clipped into sRGB.</summary>
	internal static MokaRgba FromOklch(double lightness, double chroma, double hue, double alpha = 1) =>
		From(new MokaOklch(lightness, chroma, hue, Math.Clamp(alpha, 0, 1)).ToSrgb());

	/// <summary>WCAG 2 relative luminance, 0 for black to 1 for white. Alpha is ignored.</summary>
	internal static double Luminance(MokaRgba color) => To(color).RelativeLuminance;

	/// <summary>The WCAG 2 contrast ratio between two solid colors, from 1 to 21.</summary>
	internal static double ContrastRatio(MokaRgba first, MokaRgba second) => MokaContrast.Ratio(Luminance(first), Luminance(second));

	/// <summary>
	///     The level a ratio meets. WCAG compares the ratio as it is, so 4.499 fails AA even though it would round to
	///     4.5.
	/// </summary>
	internal static MokaContrastLevel LevelOf(double ratio) => ratio switch
	{
		>= MokaContrast.EnhancedTextRatio => MokaContrastLevel.Aaa,
		>= MokaContrast.TextRatio => MokaContrastLevel.Aa,
		>= MokaContrast.LargeTextRatio => MokaContrastLevel.AaLarge,
		_ => MokaContrastLevel.Fails
	};

	/// <summary>A ratio as text, cut (not rounded) to two decimals, so a ratio just under a level never reads as it.</summary>
	internal static string RatioText(double ratio) =>
		string.Create(CultureInfo.InvariantCulture, $"{Math.Floor(ratio * 100) / 100:0.00}:1");

	/// <summary><paramref name="top" /> drawn over a solid <paramref name="bottom" />, as the eye sees it.</summary>
	/// <remarks>
	///     Worked in 8-bit steps, as the picker has always shown it. <see cref="MokaSrgb.Over" /> composites in fractions and
	///     lands a step away on some exact halves, which would move a shown ratio.
	/// </remarks>
	internal static MokaRgba Over(MokaRgba top, MokaRgba bottom)
	{
		double a = Math.Clamp(top.A, 0, 1);
		int Mix(int over, int under) => (int)Math.Round((over * a) + (under * (1 - a)), MidpointRounding.AwayFromZero);
		return new MokaRgba(Mix(top.R, bottom.R), Mix(top.G, bottom.G), Mix(top.B, bottom.B));
	}

	/// <summary>The color written in <paramref name="format" />, with its alpha when it isn't solid.</summary>
	internal static string Format(MokaRgba color, MokaColorFormat format) => MokaCssColor.Format(To(color), format switch
	{
		MokaColorFormat.Rgb => MokaCssFormat.Rgb,
		MokaColorFormat.Hsl => MokaCssFormat.Hsl,
		MokaColorFormat.Oklch => MokaCssFormat.Oklch,
		_ => MokaCssFormat.Hex
	});

	private static bool IsRgbFunction(string value)
	{
		int open = value.IndexOf('(', StringComparison.Ordinal);
		if (open < 0)
		{
			return false;
		}

		ReadOnlySpan<char> name = value.AsSpan(0, open).Trim();
		return name.Equals("rgb", StringComparison.OrdinalIgnoreCase) || name.Equals("rgba", StringComparison.OrdinalIgnoreCase);
	}

	private static MokaSrgb To(MokaRgba color) => MokaSrgb.FromBytes(color.R, color.G, color.B, color.A);

	private static MokaRgba From(MokaSrgb color)
	{
		(int r, int g, int b) = color.ToBytes();
		return new MokaRgba(r, g, b, Math.Clamp(color.Alpha, 0, 1));
	}
}
