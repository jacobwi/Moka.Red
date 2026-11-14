using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Moka.Red.Core.Colors;

/// <summary>A way of writing a color in CSS. See <see cref="MokaCssColor.Format" />.</summary>
public enum MokaCssFormat
{
	/// <summary><c>#RRGGBB</c> in uppercase, with the alpha as a fourth byte when the color is translucent.</summary>
	Hex,

	/// <summary><c>rgb(239 83 80)</c>, with <c>/ 50%</c> when the color is translucent.</summary>
	Rgb,

	/// <summary><c>hsl(1 83% 63%)</c> in whole numbers, with <c>/ 50%</c> when the color is translucent.</summary>
	Hsl,

	/// <summary><c>oklch(63.4% 0.188 25.5)</c>, with <c>/ 50%</c> when the color is translucent.</summary>
	Oklch,

	/// <summary><c>rgba(239, 83, 80, 0.08)</c>, the comma form the built-in palettes write their translucent shades in.</summary>
	LegacyRgba
}

/// <summary>
///     Reads and writes CSS colors: hex (<c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c>, <c>#rrggbbaa</c>), <c>rgb()</c> and
///     <c>rgba()</c>, <c>hsl()</c> and <c>hsla()</c>, <c>oklch()</c>, <c>oklab()</c>, the named colors and
///     <c>transparent</c>. Functions take commas or spaces, an alpha after a slash or a fourth comma, and numbers or
///     percentages. What depends on where it is used, such as <c>currentColor</c> or <c>var()</c>, isn't read. An
///     <c>oklch()</c> or <c>oklab()</c> color outside sRGB reads as it is, with channels past 0 or 1, so it can be mapped
///     with <see cref="MokaOklch.ToSrgbInGamut" /> or clipped by <see cref="MokaSrgb.ToHex" /> as browsers paint it.
/// </summary>
public static class MokaCssColor
{
	/// <summary>Below this OKLCH chroma a color has no hue worth writing, and its hue is written as 0.</summary>
	public const double AchromaticChroma = 0.0005;

	private const double OklabPercentScale = 0.4;

	private static readonly FrozenDictionary<string, int> Named = BuildNamed();

	/// <summary>Reads a CSS color.</summary>
	/// <param name="text">The CSS color.</param>
	/// <param name="color">The color, when it could be read.</param>
	/// <returns><c>true</c> when <paramref name="text" /> is a color this reads.</returns>
	public static bool TryParse([NotNullWhen(true)] string? text, out MokaSrgb color)
	{
		color = default;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		string value = text.Trim();
		if (value[0] == '#')
		{
			return TryParseHex(value, out color);
		}

		int open = value.IndexOf('(', StringComparison.Ordinal);
		if (open < 0)
		{
			return TryParseName(value, out color);
		}

		if (value[^1] != ')')
		{
			return false;
		}

		string function = value[..open].Trim();
		string[] parts = value[(open + 1)..^1]
			.Split([',', ' ', '\t', '\n', '\r', '/'], StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length is not (3 or 4))
		{
			return false;
		}

		return Function(function, "rgb", "rgba") ? TryReadRgb(parts, out color)
			: Function(function, "hsl", "hsla") ? TryReadHsl(parts, out color)
			: Function(function, "oklch") ? TryReadOklch(parts, out color)
			: Function(function, "oklab") && TryReadOklab(parts, out color);
	}

	/// <summary>Reads a CSS color, or throws.</summary>
	/// <param name="text">The CSS color.</param>
	/// <returns>The color.</returns>
	/// <exception cref="FormatException">Thrown when <paramref name="text" /> isn't a color <see cref="TryParse" /> reads.</exception>
	public static MokaSrgb Parse(string text) =>
		TryParse(text, out MokaSrgb color) ? color : throw new FormatException($"'{text}' is not a CSS color this can read.");

	/// <summary>Reads a hex color only: <c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c> or <c>#rrggbbaa</c>, with the <c>#</c>.</summary>
	/// <param name="text">The hex color.</param>
	/// <param name="color">The color, when it could be read.</param>
	/// <returns><c>true</c> when <paramref name="text" /> is a hex color.</returns>
	public static bool TryParseHex([NotNullWhen(true)] string? text, out MokaSrgb color)
	{
		color = default;
		ReadOnlySpan<char> value = text.AsSpan().Trim();
		if (value.Length < 2 || value[0] != '#')
		{
			return false;
		}

		ReadOnlySpan<char> digits = value[1..];
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
		color = MokaSrgb.FromBytes(channels[0], channels[1], channels[2], hasAlpha ? channels[3] / 255.0 : 1);
		return true;
	}

	/// <summary>Writes a color in one of the CSS forms, clipped into sRGB.</summary>
	/// <param name="color">The color.</param>
	/// <param name="format">How to write it.</param>
	/// <returns>The CSS color.</returns>
	public static string Format(MokaSrgb color, MokaCssFormat format)
	{
		bool translucent = color.Alpha < 1;
		string alpha = translucent
			? string.Create(CultureInfo.InvariantCulture, $" / {Math.Round(Math.Clamp(color.Alpha, 0, 1) * 100, MidpointRounding.AwayFromZero)}%")
			: "";

		switch (format)
		{
			case MokaCssFormat.Rgb:
			{
				(int r, int g, int b) = color.ToBytes();
				return string.Create(CultureInfo.InvariantCulture, $"rgb({r} {g} {b}{alpha})");
			}

			case MokaCssFormat.LegacyRgba:
			{
				(int r, int g, int b) = color.ToBytes();
				return string.Create(CultureInfo.InvariantCulture, $"rgba({r}, {g}, {b}, {Math.Clamp(color.Alpha, 0, 1):0.00})");
			}

			case MokaCssFormat.Hsl:
			{
				(int r, int g, int b) = color.ToBytes();
				MokaHsl hsl = MokaSrgb.FromBytes(r, g, b).ToHsv().ToHsl();
				return string.Create(CultureInfo.InvariantCulture,
					$"hsl({Round(hsl.Hue) % 360} {Round(hsl.Saturation * 100)}% {Round(hsl.Lightness * 100)}%{alpha})");
			}

			case MokaCssFormat.Oklch:
			{
				(int r, int g, int b) = color.ToBytes();
				MokaOklch oklch = MokaSrgb.FromBytes(r, g, b).ToOklch();
				double hue = oklch.C < AchromaticChroma ? 0 : oklch.H;
				return string.Create(CultureInfo.InvariantCulture, $"oklch({oklch.L * 100:0.0}% {oklch.C:0.000} {hue:0.0}{alpha})");
			}

			default:
				return color.ToHex(uppercase: true, withAlpha: true);
		}
	}

	private static bool TryParseName(string value, out MokaSrgb color)
	{
		color = default;
		if (value.Equals("transparent", StringComparison.OrdinalIgnoreCase))
		{
			color = MokaSrgb.Transparent;
			return true;
		}

		if (!Named.TryGetValue(value, out int rgb))
		{
			return false;
		}

		color = MokaSrgb.FromBytes((rgb >> 16) & 0xff, (rgb >> 8) & 0xff, rgb & 0xff);
		return true;
	}

	private static bool TryReadRgb(string[] parts, out MokaSrgb color)
	{
		color = default;
		Span<double> channels = stackalloc double[3];
		for (int i = 0; i < 3; i++)
		{
			if (!TryReadNumber(parts[i], 255, out double channel))
			{
				return false;
			}

			channels[i] = Math.Round(Math.Clamp(channel, 0, 255), MidpointRounding.AwayFromZero) / 255;
		}

		if (!TryReadAlpha(parts, out double alpha))
		{
			return false;
		}

		color = new MokaSrgb(channels[0], channels[1], channels[2], alpha);
		return true;
	}

	private static bool TryReadHsl(string[] parts, out MokaSrgb color)
	{
		color = default;
		if (!TryReadAngle(parts[0], out double hue)
		    || !TryReadNumber(parts[1], 100, out double saturation)
		    || !TryReadNumber(parts[2], 100, out double lightness)
		    || !TryReadAlpha(parts, out double alpha))
		{
			return false;
		}

		color = MokaSrgb.FromHsl(MokaHsl.WrapHue(hue), Math.Clamp(saturation / 100, 0, 1), Math.Clamp(lightness / 100, 0, 1), alpha);
		return true;
	}

	private static bool TryReadOklch(string[] parts, out MokaSrgb color)
	{
		color = default;
		if (!TryReadNumber(parts[0], 1, out double lightness)
		    || !TryReadNumber(parts[1], OklabPercentScale, out double chroma)
		    || !TryReadAngle(parts[2], out double hue)
		    || !TryReadAlpha(parts, out double alpha))
		{
			return false;
		}

		color = new MokaOklch(Math.Clamp(lightness, 0, 1), Math.Max(chroma, 0), MokaHsl.WrapHue(hue), alpha).ToSrgb();
		return true;
	}

	private static bool TryReadOklab(string[] parts, out MokaSrgb color)
	{
		color = default;
		if (!TryReadNumber(parts[0], 1, out double lightness)
		    || !TryReadNumber(parts[1], OklabPercentScale, out double a)
		    || !TryReadNumber(parts[2], OklabPercentScale, out double b)
		    || !TryReadAlpha(parts, out double alpha))
		{
			return false;
		}

		color = new MokaOklab(Math.Clamp(lightness, 0, 1), a, b, alpha).ToSrgb();
		return true;
	}

	private static bool TryReadAlpha(string[] parts, out double alpha)
	{
		alpha = 1;
		if (parts.Length < 4)
		{
			return true;
		}

		if (!TryReadNumber(parts[3], 1, out alpha))
		{
			return false;
		}

		alpha = Math.Clamp(alpha, 0, 1);
		return true;
	}

	// A number, a percentage of 'full', or none (zero).
	private static bool TryReadNumber(string text, double full, out double number)
	{
		if (text.Equals("none", StringComparison.OrdinalIgnoreCase))
		{
			number = 0;
			return true;
		}

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

	private static bool TryReadAngle(string text, out double degrees)
	{
		(string Unit, double Scale)[] units = [("deg", 1), ("grad", 0.9), ("rad", 180 / Math.PI), ("turn", 360)];
		foreach ((string unit, double scale) in units)
		{
			if (text.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
			{
				bool read = TryReadNumber(text[..^unit.Length], 1, out degrees);
				degrees *= scale;
				return read;
			}
		}

		return TryReadNumber(text, 1, out degrees) && !text.EndsWith('%');
	}

	private static bool Function(string name, params ReadOnlySpan<string> names)
	{
		foreach (string candidate in names)
		{
			if (name.Equals(candidate, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static int Nibble(char c) => char.IsAsciiDigit(c) ? c - '0' : char.ToLowerInvariant(c) - 'a' + 10;

	private static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

	private static FrozenDictionary<string, int> BuildNamed()
	{
		const string table =
			"aliceblue f0f8ff antiquewhite faebd7 aqua 00ffff aquamarine 7fffd4 azure f0ffff beige f5f5dc bisque ffe4c4 " +
			"black 000000 blanchedalmond ffebcd blue 0000ff blueviolet 8a2be2 brown a52a2a burlywood deb887 cadetblue 5f9ea0 " +
			"chartreuse 7fff00 chocolate d2691e coral ff7f50 cornflowerblue 6495ed cornsilk fff8dc crimson dc143c cyan 00ffff " +
			"darkblue 00008b darkcyan 008b8b darkgoldenrod b8860b darkgray a9a9a9 darkgreen 006400 darkgrey a9a9a9 " +
			"darkkhaki bdb76b darkmagenta 8b008b darkolivegreen 556b2f darkorange ff8c00 darkorchid 9932cc darkred 8b0000 " +
			"darksalmon e9967a darkseagreen 8fbc8f darkslateblue 483d8b darkslategray 2f4f4f darkslategrey 2f4f4f " +
			"darkturquoise 00ced1 darkviolet 9400d3 deeppink ff1493 deepskyblue 00bfff dimgray 696969 dimgrey 696969 " +
			"dodgerblue 1e90ff firebrick b22222 floralwhite fffaf0 forestgreen 228b22 fuchsia ff00ff gainsboro dcdcdc " +
			"ghostwhite f8f8ff gold ffd700 goldenrod daa520 gray 808080 green 008000 greenyellow adff2f grey 808080 " +
			"honeydew f0fff0 hotpink ff69b4 indianred cd5c5c indigo 4b0082 ivory fffff0 khaki f0e68c lavender e6e6fa " +
			"lavenderblush fff0f5 lawngreen 7cfc00 lemonchiffon fffacd lightblue add8e6 lightcoral f08080 lightcyan e0ffff " +
			"lightgoldenrodyellow fafad2 lightgray d3d3d3 lightgreen 90ee90 lightgrey d3d3d3 lightpink ffb6c1 " +
			"lightsalmon ffa07a lightseagreen 20b2aa lightskyblue 87cefa lightslategray 778899 lightslategrey 778899 " +
			"lightsteelblue b0c4de lightyellow ffffe0 lime 00ff00 limegreen 32cd32 linen faf0e6 magenta ff00ff maroon 800000 " +
			"mediumaquamarine 66cdaa mediumblue 0000cd mediumorchid ba55d3 mediumpurple 9370db mediumseagreen 3cb371 " +
			"mediumslateblue 7b68ee mediumspringgreen 00fa9a mediumturquoise 48d1cc mediumvioletred c71585 " +
			"midnightblue 191970 mintcream f5fffa mistyrose ffe4e1 moccasin ffe4b5 navajowhite ffdead navy 000080 " +
			"oldlace fdf5e6 olive 808000 olivedrab 6b8e23 orange ffa500 orangered ff4500 orchid da70d6 " +
			"palegoldenrod eee8aa palegreen 98fb98 paleturquoise afeeee palevioletred db7093 papayawhip ffefd5 " +
			"peachpuff ffdab9 peru cd853f pink ffc0cb plum dda0dd powderblue b0e0e6 purple 800080 rebeccapurple 663399 " +
			"red ff0000 rosybrown bc8f8f royalblue 4169e1 saddlebrown 8b4513 salmon fa8072 sandybrown f4a460 " +
			"seagreen 2e8b57 seashell fff5ee sienna a0522d silver c0c0c0 skyblue 87ceeb slateblue 6a5acd " +
			"slategray 708090 slategrey 708090 snow fffafa springgreen 00ff7f steelblue 4682b4 tan d2b48c teal 008080 " +
			"thistle d8bfd8 tomato ff6347 turquoise 40e0d0 violet ee82ee wheat f5deb3 white ffffff whitesmoke f5f5f5 " +
			"yellow ffff00 yellowgreen 9acd32";

		string[] words = table.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		Dictionary<string, int> named = new(words.Length / 2, StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < words.Length; i += 2)
		{
			named[words[i]] = int.Parse(words[i + 1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		}

		return named.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
	}
}
