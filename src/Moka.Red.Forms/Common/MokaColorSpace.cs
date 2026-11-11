using System.Globalization;
using Moka.Red.Forms.ColorPicker;

namespace Moka.Red.Forms.Common;

/// <summary>
///     The color math the color inputs share: reading hex and <c>rgb()</c>, HSV, HSL and OKLCH, WCAG contrast, and
///     writing a color in each format. Hue is in degrees from 0 to 360; saturation, value, lightness and alpha run
///     from 0 to 1.
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

		ReadOnlySpan<char> value = text.AsSpan().Trim();
		return value[0] == '#' ? TryParseHex(value[1..], out color) : TryParseRgbFunction(value, out color);
	}

	/// <summary>The color as lowercase <c>#rrggbb</c>, with the alpha as a fourth byte when asked and below 1.</summary>
	internal static string ToHex(MokaRgba color, bool withAlpha = false)
	{
		string hex = string.Create(CultureInfo.InvariantCulture, $"#{color.R:x2}{color.G:x2}{color.B:x2}");
		return withAlpha && color.A < 1 ? hex + AlphaByte(color.A).ToString("x2", CultureInfo.InvariantCulture) : hex;
	}

	/// <summary>The color from hue, saturation and value.</summary>
	internal static MokaRgba FromHsv(double hue, double saturation, double value, double alpha = 1)
	{
		double h = Wrap(hue);
		double s = Math.Clamp(saturation, 0, 1);
		double v = Math.Clamp(value, 0, 1);

		double Channel(int n)
		{
			double k = (n + (h / 60)) % 6;
			return v - (v * s * Math.Max(0, Math.Min(k, Math.Min(4 - k, 1))));
		}

		return new MokaRgba(Byte(Channel(5)), Byte(Channel(3)), Byte(Channel(1)), Math.Clamp(alpha, 0, 1));
	}

	/// <summary>Hue, saturation and value. A grey has hue 0.</summary>
	internal static (double Hue, double Saturation, double Value) ToHsv(MokaRgba color)
	{
		(double r, double g, double b) = Unit(color);
		double max = Math.Max(r, Math.Max(g, b));
		double min = Math.Min(r, Math.Min(g, b));
		double delta = max - min;
		return (Hue(r, g, b, max, delta), max == 0 ? 0 : delta / max, max);
	}

	/// <summary>The color from hue, saturation and lightness.</summary>
	internal static MokaRgba FromHsl(double hue, double saturation, double lightness, double alpha = 1)
	{
		(double h, double s, double v) = HslToHsv(hue, saturation, lightness);
		return FromHsv(h, s, v, alpha);
	}

	/// <summary>Hue, saturation and lightness. A grey has hue 0.</summary>
	internal static (double Hue, double Saturation, double Lightness) ToHsl(MokaRgba color)
	{
		(double h, double s, double v) = ToHsv(color);
		return HsvToHsl(h, s, v);
	}

	/// <summary>Hue, saturation and value as hue, saturation and lightness.</summary>
	internal static (double Hue, double Saturation, double Lightness) HsvToHsl(double hue, double saturation, double value)
	{
		double l = value * (1 - (saturation / 2));
		double s = l is <= 0 or >= 1 ? 0 : (value - l) / Math.Min(l, 1 - l);
		return (hue, Math.Clamp(s, 0, 1), Math.Clamp(l, 0, 1));
	}

	/// <summary>Hue, saturation and lightness as hue, saturation and value.</summary>
	internal static (double Hue, double Saturation, double Value) HslToHsv(double hue, double saturation, double lightness)
	{
		double s = Math.Clamp(saturation, 0, 1);
		double l = Math.Clamp(lightness, 0, 1);
		double v = l + (s * Math.Min(l, 1 - l));
		return (hue, v == 0 ? 0 : 2 * (1 - (l / v)), v);
	}

	/// <summary>
	///     OKLCH lightness (0 to 1), chroma and hue in degrees, through Björn Ottosson's OKLab. A color with almost no
	///     chroma has hue 0.
	/// </summary>
	internal static (double Lightness, double Chroma, double Hue) ToOklch(MokaRgba color)
	{
		double r = Linear(color.R), g = Linear(color.G), b = Linear(color.B);

		double l = Math.Cbrt((0.4122214708 * r) + (0.5363325363 * g) + (0.0514459929 * b));
		double m = Math.Cbrt((0.2119034982 * r) + (0.6806995451 * g) + (0.1073969566 * b));
		double s = Math.Cbrt((0.0883024619 * r) + (0.2817188376 * g) + (0.6299787005 * b));

		double lightness = (0.2104542553 * l) + (0.7936177850 * m) - (0.0040720468 * s);
		double a = (1.9779984951 * l) - (2.4285922050 * m) + (0.4505937099 * s);
		double bb = (0.0259040371 * l) + (0.7827717662 * m) - (0.8086757660 * s);

		double chroma = Math.Sqrt((a * a) + (bb * bb));
		double hue = chroma < 0.0005 ? 0 : Wrap(Math.Atan2(bb, a) * 180 / Math.PI);
		return (lightness, chroma, hue);
	}

	/// <summary>The sRGB color for an OKLCH color, clipped into sRGB.</summary>
	internal static MokaRgba FromOklch(double lightness, double chroma, double hue, double alpha = 1)
	{
		double radians = hue * Math.PI / 180;
		double a = chroma * Math.Cos(radians);
		double b = chroma * Math.Sin(radians);

		double l = lightness + (0.3963377774 * a) + (0.2158037573 * b);
		double m = lightness - (0.1055613458 * a) - (0.0638541728 * b);
		double s = lightness - (0.0894841775 * a) - (1.2914855480 * b);
		l = l * l * l;
		m = m * m * m;
		s = s * s * s;

		double red = (4.0767416621 * l) - (3.3077115913 * m) + (0.2309699292 * s);
		double green = (-1.2684380046 * l) + (2.6097574011 * m) - (0.3413193965 * s);
		double blue = (-0.0041960863 * l) - (0.7034186147 * m) + (1.7076147010 * s);

		return new MokaRgba(Byte(Gamma(red)), Byte(Gamma(green)), Byte(Gamma(blue)), Math.Clamp(alpha, 0, 1));
	}

	/// <summary>WCAG 2 relative luminance, 0 for black to 1 for white. Alpha is ignored.</summary>
	internal static double Luminance(MokaRgba color) =>
		(0.2126 * Linear(color.R)) + (0.7152 * Linear(color.G)) + (0.0722 * Linear(color.B));

	/// <summary>The WCAG 2 contrast ratio between two solid colors, from 1 to 21.</summary>
	internal static double ContrastRatio(MokaRgba first, MokaRgba second)
	{
		double a = Luminance(first);
		double b = Luminance(second);
		return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
	}

	/// <summary>
	///     The level a ratio meets. WCAG compares the ratio as it is, so 4.499 fails AA even though it would round to
	///     4.5.
	/// </summary>
	internal static MokaContrastLevel LevelOf(double ratio) => ratio switch
	{
		>= 7 => MokaContrastLevel.Aaa,
		>= 4.5 => MokaContrastLevel.Aa,
		>= 3 => MokaContrastLevel.AaLarge,
		_ => MokaContrastLevel.Fails
	};

	/// <summary>A ratio as text, cut (not rounded) to two decimals, so a ratio just under a level never reads as it.</summary>
	internal static string RatioText(double ratio) =>
		string.Create(CultureInfo.InvariantCulture, $"{Math.Floor(ratio * 100) / 100:0.00}:1");

	/// <summary><paramref name="top" /> drawn over a solid <paramref name="bottom" />, as the eye sees it.</summary>
	internal static MokaRgba Over(MokaRgba top, MokaRgba bottom)
	{
		double a = Math.Clamp(top.A, 0, 1);
		int Mix(int over, int under) => (int)Math.Round((over * a) + (under * (1 - a)), MidpointRounding.AwayFromZero);
		return new MokaRgba(Mix(top.R, bottom.R), Mix(top.G, bottom.G), Mix(top.B, bottom.B));
	}

	/// <summary>The color written in <paramref name="format" />, with its alpha when it isn't solid.</summary>
	internal static string Format(MokaRgba color, MokaColorFormat format)
	{
		bool translucent = color.A < 1;
		string alpha = translucent ? string.Create(CultureInfo.InvariantCulture, $" / {Math.Round(color.A * 100, MidpointRounding.AwayFromZero)}%") : "";

		switch (format)
		{
			case MokaColorFormat.Rgb:
				return string.Create(CultureInfo.InvariantCulture, $"rgb({color.R} {color.G} {color.B}{alpha})");

			case MokaColorFormat.Hsl:
			{
				(double h, double s, double l) = ToHsl(color);
				return string.Create(CultureInfo.InvariantCulture,
					$"hsl({Round(h) % 360} {Round(s * 100)}% {Round(l * 100)}%{alpha})");
			}

			case MokaColorFormat.Oklch:
			{
				(double l, double c, double h) = ToOklch(color);
				return string.Create(CultureInfo.InvariantCulture,
					$"oklch({l * 100:0.0}% {c:0.000} {(c < 0.0005 ? 0 : h):0.0}{alpha})");
			}

			default:
				return ToHex(color, translucent).ToUpperInvariant();
		}
	}

	// sRGB channel (0 to 255) to linear light. 0.04045 is the threshold IEC 61966-2-1 gives; the 0.03928 in older
	// WCAG text decides no 8-bit value differently.
	private static double Linear(int channel)
	{
		double c = channel / 255.0;
		return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
	}

	private static double Gamma(double linear)
	{
		double c = Math.Clamp(linear, 0, 1);
		return c <= 0.0031308 ? 12.92 * c : (1.055 * Math.Pow(c, 1 / 2.4)) - 0.055;
	}

	private static (double R, double G, double B) Unit(MokaRgba color) => (color.R / 255.0, color.G / 255.0, color.B / 255.0);

	private static double Hue(double r, double g, double b, double max, double delta)
	{
		if (delta == 0)
		{
			return 0;
		}

		double h = max == r ? ((g - b) / delta) % 6
			: max == g ? ((b - r) / delta) + 2
			: ((r - g) / delta) + 4;
		return Wrap(h * 60);
	}

	private static double Wrap(double degrees)
	{
		double h = degrees % 360;
		return h < 0 ? h + 360 : h;
	}

	private static int Byte(double unit) => (int)Math.Round(Math.Clamp(unit, 0, 1) * 255, MidpointRounding.AwayFromZero);

	private static int AlphaByte(double alpha) => (int)Math.Round(Math.Clamp(alpha, 0, 1) * 255, MidpointRounding.AwayFromZero);

	private static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

	private static bool TryParseHex(ReadOnlySpan<char> digits, out MokaRgba color)
	{
		color = default;
		if (digits.Length is not (3 or 4 or 6 or 8))
		{
			return false;
		}

		foreach (char c in digits)
		{
			if (!char.IsAsciiHexDigit(c))
			{
				return false;
			}
		}

		int width = digits.Length <= 4 ? 1 : 2;
		Span<int> channels = stackalloc int[4];
		for (int i = 0; i < digits.Length / width; i++)
		{
			int high = Nibble(digits[i * width]);
			channels[i] = width == 1 ? (high * 16) + high : (high * 16) + Nibble(digits[(i * 2) + 1]);
		}

		bool hasAlpha = digits.Length is 4 or 8;
		color = new MokaRgba(channels[0], channels[1], channels[2], hasAlpha ? channels[3] / 255.0 : 1);
		return true;
	}

	private static int Nibble(char c) => char.IsAsciiDigit(c) ? c - '0' : char.ToLowerInvariant(c) - 'a' + 10;

	// rgb(239, 83, 80), rgba(239, 83, 80, 0.5), rgb(239 83 80 / 50%). Channels as 0 to 255 or as percentages.
	private static bool TryParseRgbFunction(ReadOnlySpan<char> value, out MokaRgba color)
	{
		color = default;
		int open = value.IndexOf('(');
		if (open < 0 || value[^1] != ')')
		{
			return false;
		}

		ReadOnlySpan<char> name = value[..open].Trim();
		if (!name.Equals("rgb", StringComparison.OrdinalIgnoreCase) && !name.Equals("rgba", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		string[] parts = value[(open + 1)..^1].ToString()
			.Replace("/", " ", StringComparison.Ordinal)
			.Split([',', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length is not (3 or 4))
		{
			return false;
		}

		int[] channels = new int[3];
		for (int i = 0; i < 3; i++)
		{
			if (!TryReadNumber(parts[i], 255, out double channel))
			{
				return false;
			}

			channels[i] = (int)Math.Round(Math.Clamp(channel, 0, 255), MidpointRounding.AwayFromZero);
		}

		double alpha = 1;
		if (parts.Length == 4 && !TryReadNumber(parts[3], 1, out alpha))
		{
			return false;
		}

		color = new MokaRgba(channels[0], channels[1], channels[2], Math.Clamp(alpha, 0, 1));
		return true;
	}

	// A number, or a percentage of 'full'.
	private static bool TryReadNumber(string text, double full, out double number)
	{
		bool percent = text.EndsWith('%');
		string digits = percent ? text[..^1] : text;
		if (!double.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out number) || !double.IsFinite(number))
		{
			return false;
		}

		if (percent)
		{
			number = number / 100 * full;
		}

		return true;
	}
}
