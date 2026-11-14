using Moka.Red.Core.Colors;
using Moka.Red.Core.Theming;

namespace Moka.Red.ThemeGen;

/// <summary>
///     Color mixing for the presets, on Core's color library. Reads the four hex forms and the comma-separated
///     <c>rgb()</c>/<c>rgba()</c> form that <see cref="MokaTheme.WithAccent(string)" /> writes.
/// </summary>
internal static class CssColor
{
	/// <summary>
	///     Moves <paramref name="from" /> toward <paramref name="to" /> by <paramref name="amount" /> (0 to 1)
	///     and returns #rrggbb. Returns <paramref name="from" /> unchanged when either color is unreadable.
	/// </summary>
	internal static string Mix(string from, string to, double amount)
	{
		if (!TryParse(from, out MokaSrgb start) || !TryParse(to, out MokaSrgb end))
		{
			return from;
		}

		// Worked in 8-bit steps with halves rounded to even, as the presets were first worked out. A mix in fractions
		// (MokaColorMix) lands a step away on many exact halves, such as 15 toward 0 by 0.3.
		(int r1, int g1, int b1) = start.ToBytes();
		(int r2, int g2, int b2) = end.ToBytes();
		int Channel(int a, int z) => (int)Math.Clamp(Math.Round(a + ((z - a) * amount), MidpointRounding.ToEven), 0, 255);
		return MokaSrgb.FromBytes(Channel(r1, r2), Channel(g1, g2), Channel(b1, b2)).ToHex();
	}

	/// <summary>
	///     <paramref name="color" /> as <c>rgba(r, g, b, alpha)</c>, the format the built-in palettes use for
	///     their dim fills. Returns <paramref name="color" /> unchanged when it is unreadable.
	/// </summary>
	internal static string WithAlpha(string color, double alpha) =>
		TryParse(color, out MokaSrgb parsed)
			? MokaCssColor.Format(parsed with { Alpha = alpha }, MokaCssFormat.LegacyRgba)
			: color;

	// The hex forms and the comma rgb()/rgba() form with whole channels, which is what a palette holds.
	private static bool TryParse(string? value, out MokaSrgb color)
	{
		color = default;
		string text = value?.Trim() ?? "";
		if (text.StartsWith('#'))
		{
			return MokaCssColor.TryParseHex(text, out color);
		}

		int open = text.IndexOf('(', StringComparison.Ordinal);
		string function = open < 0 ? "" : text[..open].Trim();
		bool rgb = function.Equals("rgb", StringComparison.OrdinalIgnoreCase) || function.Equals("rgba", StringComparison.OrdinalIgnoreCase);
		return rgb && text.Contains(',', StringComparison.Ordinal) && MokaCssColor.TryParse(text, out color);
	}
}
