using Moka.Red.Core.Colors;

namespace Moka.Red.Core.Theming;

// The theme's accent derivations, written on the shared color library. They keep the hex-only input and the exact
// output formats the built-in palettes were written in.
internal static class MokaColorMath
{
	// What the original design mixes its accent toward white and black for the light and dark shades.
	internal const double MatrixShadeMix = 0.30;

	// How much of the accent the new design keeps in its pressed shade, the rest being the scheme's ink.
	internal const double PressedAccentShare = 0.78;

	internal static bool TryParseHex(string color, out int r, out int g, out int b)
	{
		r = g = b = 0;
		if (string.IsNullOrEmpty(color) || color.Length is not (4 or 7) || !MokaCssColor.TryParseHex(color, out MokaSrgb parsed))
		{
			return false;
		}

		(r, g, b) = parsed.ToBytes();
		return true;
	}

	internal static string Rgba(int r, int g, int b, double alpha) =>
		MokaCssColor.Format(MokaSrgb.FromBytes(r, g, b, alpha), MokaCssFormat.LegacyRgba);

	internal static string Rgba(string hex, double alpha)
	{
		(int r, int g, int b) = ParseHex(hex);
		return Rgba(r, g, b, alpha);
	}

	// The two accent mixes below are worked in 8-bit steps and round half to even, as the palettes were first
	// written. A mix in the library's 0 to 1 channels lands on a different step for some accents (black's light
	// shade is #4c4c4c this way and #4d4d4d that way), so they stay in bytes.

	// The original design's shade of an accent: amount of the way toward white or black.
	internal static string Mix(int r, int g, int b, MokaSrgb target, double amount)
	{
		(int targetR, int targetG, int targetB) = target.ToBytes();
		int Channel(int channel, int toward) => (int)Math.Round(channel + ((toward - channel) * amount), MidpointRounding.ToEven);
		return MokaSrgb.FromBytes(Channel(r, targetR), Channel(g, targetG), Channel(b, targetB)).ToHex();
	}

	// The new design's pressed shade of an accent: mostly the accent, the rest the scheme's ink.
	internal static string PressedShade(string accent, string ink)
	{
		(int r, int g, int b) = ParseHex(accent);
		(int inkR, int inkG, int inkB) = ParseHex(ink);
		int Channel(int a, int z) => (int)Math.Round((a * PressedAccentShare) + (z * (1 - PressedAccentShare)), MidpointRounding.ToEven);
		return MokaSrgb.FromBytes(Channel(r, inkR), Channel(g, inkG), Channel(b, inkB)).ToHex(uppercase: true);
	}

	internal static (double Hue, double Saturation) ToHueSaturation(int r, int g, int b)
	{
		MokaHsl hsl = MokaSrgb.FromBytes(r, g, b).ToHsl();
		return (hsl.Hue, hsl.Saturation);
	}

	// The color at this hue and saturation whose WCAG luminance is closest to luminance.
	// At one HSL lightness a blue is far darker than a cyan, so a fixed lightness cannot hold a contrast
	// target across hues. Luminance only grows with lightness at a fixed hue and saturation, which is what
	// lets a binary search find it.
	internal static string Shade(double hue, double saturation, double luminance)
	{
		double low = 0, high = 1;
		for (int i = 0; i < 24; i++)
		{
			double middle = (low + high) / 2;
			if (EightBit(MokaSrgb.FromHsl(hue, saturation, middle)).RelativeLuminance < luminance)
			{
				low = middle;
			}
			else
			{
				high = middle;
			}
		}

		return EightBit(MokaSrgb.FromHsl(hue, saturation, (low + high) / 2)).ToHex(uppercase: true);
	}

	// A MokaAccent is a public record, so its shades reach here unchecked. Without this a shade like "red"
	// would turn every glow token black without a word.
	private static (int R, int G, int B) ParseHex(string color) =>
		TryParseHex(color, out int r, out int g, out int b)
			? (r, g, b)
			: throw new ArgumentException($"An accent's shades must be hex colors like #6e92ff, got '{color}'.", nameof(color));

	// The accent presets were found with their HSL channels rounded half to even.
	private static MokaSrgb EightBit(MokaSrgb color)
	{
		(int r, int g, int b) = color.ToBytes(MidpointRounding.ToEven);
		return MokaSrgb.FromBytes(r, g, b);
	}
}
