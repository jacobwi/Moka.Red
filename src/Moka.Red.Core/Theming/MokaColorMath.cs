using System.Globalization;

namespace Moka.Red.Core.Theming;

internal static class MokaColorMath
{
	internal static bool TryParseHex(string color, out int r, out int g, out int b)
	{
		r = g = b = 0;

		if (string.IsNullOrEmpty(color) || color[0] != '#')
		{
			return false;
		}

		string hex = color[1..];
		if (hex.Length == 3)
		{
			hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
		}

		if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
		{
			return false;
		}

		r = (rgb >> 16) & 0xff;
		g = (rgb >> 8) & 0xff;
		b = rgb & 0xff;
		return true;
	}

	// A MokaAccent is a public record, so its shades reach here unchecked. Without this a shade like "red"
	// would turn every glow token black without a word.
	private static (int R, int G, int B) ParseHex(string color) =>
		TryParseHex(color, out int r, out int g, out int b)
			? (r, g, b)
			: throw new ArgumentException($"An accent's shades must be hex colors like #6e92ff, got '{color}'.", nameof(color));

	internal static string Rgba(int r, int g, int b, double alpha) =>
		string.Create(CultureInfo.InvariantCulture, $"rgba({r}, {g}, {b}, {alpha:0.00})");

	internal static string Rgba(string hex, double alpha)
	{
		(int r, int g, int b) = ParseHex(hex);
		return Rgba(r, g, b, alpha);
	}

	internal static string Mix(int r, int g, int b, int target, double amount)
	{
		int MixChannel(int channel) => (int)Math.Round(channel + ((target - channel) * amount));
		return string.Create(CultureInfo.InvariantCulture, $"#{MixChannel(r):x2}{MixChannel(g):x2}{MixChannel(b):x2}");
	}

	/// <summary>A weighted average of two hex colors, <paramref name="weight" /> of it from <paramref name="hex" />.</summary>
	internal static string MixHex(string hex, string other, double weight)
	{
		(int r, int g, int b) = ParseHex(hex);
		(int otherR, int otherG, int otherB) = ParseHex(other);

		int MixChannel(int a, int z) => (int)Math.Round((a * weight) + (z * (1 - weight)));
		return ToHex(MixChannel(r, otherR), MixChannel(g, otherG), MixChannel(b, otherB));
	}

	internal static (double Hue, double Saturation) ToHueSaturation(int r, int g, int b)
	{
		double red = r / 255.0, green = g / 255.0, blue = b / 255.0;
		double max = Math.Max(red, Math.Max(green, blue));
		double min = Math.Min(red, Math.Min(green, blue));
		double delta = max - min;

		if (delta == 0)
		{
			return (0, 0);
		}

		double lightness = (max + min) / 2;
		double saturation = delta / (1 - Math.Abs((2 * lightness) - 1));
		double hue = max == red ? ((60 * ((green - blue) / delta)) + 360) % 360
			: max == green ? 60 * (((blue - red) / delta) + 2)
			: 60 * (((red - green) / delta) + 4);

		return (hue, Math.Clamp(saturation, 0, 1));
	}

	/// <summary>
	///     The color at this hue and saturation whose WCAG luminance is closest to <paramref name="luminance" />.
	///     At one HSL lightness a blue is far darker than a cyan, so a fixed lightness cannot hold a contrast
	///     target across hues. Luminance only grows with lightness at a fixed hue and saturation, which is what
	///     lets a binary search find it.
	/// </summary>
	internal static string Shade(double hue, double saturation, double luminance)
	{
		double low = 0, high = 1;
		for (int i = 0; i < 24; i++)
		{
			double middle = (low + high) / 2;
			(int r, int g, int b) = Hsl(hue, saturation, middle);
			if (Luminance(r, g, b) < luminance)
			{
				low = middle;
			}
			else
			{
				high = middle;
			}
		}

		(int red, int green, int blue) = Hsl(hue, saturation, (low + high) / 2);
		return ToHex(red, green, blue);
	}

	private static double Luminance(int r, int g, int b)
	{
		static double Channel(int value)
		{
			double c = value / 255.0;
			return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
		}

		return (0.2126 * Channel(r)) + (0.7152 * Channel(g)) + (0.0722 * Channel(b));
	}

	private static (int R, int G, int B) Hsl(double hue, double saturation, double lightness)
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

		int Byte(double value) => (int)Math.Round(Math.Clamp(value + m, 0, 1) * 255);
		return (Byte(r), Byte(g), Byte(b));
	}

	private static string ToHex(int r, int g, int b) =>
		string.Create(CultureInfo.InvariantCulture, $"#{r:X2}{g:X2}{b:X2}");
}
