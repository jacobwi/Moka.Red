using System.Globalization;
using Moka.Red.Core.Theming;

namespace Moka.Red.ThemeGen;

/// <summary>
///     Color parsing and mixing for the palette editor and the presets. Reads the four hex forms and the
///     comma-separated <c>rgb()</c>/<c>rgba()</c> form that <see cref="MokaTheme.WithAccent" /> writes.
/// </summary>
internal static class CssColor
{
	/// <summary>Whether <paramref name="value" /> is #rgb, #rgba, #rrggbb or #rrggbbaa.</summary>
	internal static bool IsHex(string? value) => TryParseHex(value, out _, out _, out _);

	/// <summary>
	///     The #rrggbb value a native color input can show. Alpha is dropped because the input has none,
	///     and a color it cannot read comes back as #000000, which is what the browser would show anyway.
	/// </summary>
	internal static string ToPickerHex(string? value) =>
		TryParse(value, out byte r, out byte g, out byte b) ? Hex(r, g, b) : "#000000";

	/// <summary>
	///     Moves <paramref name="from" /> toward <paramref name="to" /> by <paramref name="amount" /> (0 to 1)
	///     and returns #rrggbb. Returns <paramref name="from" /> unchanged when either color is unreadable.
	/// </summary>
	internal static string Mix(string from, string to, double amount)
	{
		if (!TryParse(from, out byte r1, out byte g1, out byte b1) ||
		    !TryParse(to, out byte r2, out byte g2, out byte b2))
		{
			return from;
		}

		return Hex(MixChannel(r1, r2, amount), MixChannel(g1, g2, amount), MixChannel(b1, b2, amount));
	}

	/// <summary>
	///     <paramref name="color" /> as <c>rgba(r, g, b, alpha)</c>, the format the built-in palettes use for
	///     their dim fills. Returns <paramref name="color" /> unchanged when it is unreadable.
	/// </summary>
	internal static string WithAlpha(string color, double alpha) =>
		TryParse(color, out byte r, out byte g, out byte b)
			? string.Create(CultureInfo.InvariantCulture, $"rgba({r}, {g}, {b}, {alpha:0.00})")
			: color;

	/// <summary>Reads the red, green and blue channels of a hex or <c>rgb()</c>/<c>rgba()</c> color.</summary>
	internal static bool TryParse(string? value, out byte r, out byte g, out byte b) =>
		TryParseHex(value, out r, out g, out b) || TryParseRgb(value, out r, out g, out b);

	private static bool TryParseHex(string? value, out byte r, out byte g, out byte b)
	{
		r = g = b = 0;
		string text = value?.Trim() ?? "";
		if (text.Length < 2 || text[0] != '#')
		{
			return false;
		}

		ReadOnlySpan<char> hex = text.AsSpan(1);
		foreach (char c in hex)
		{
			if (!char.IsAsciiHexDigit(c))
			{
				return false;
			}
		}

		switch (hex.Length)
		{
			case 3 or 4:
				r = (byte)(HexDigit(hex[0]) * 17);
				g = (byte)(HexDigit(hex[1]) * 17);
				b = (byte)(HexDigit(hex[2]) * 17);
				return true;
			case 6 or 8:
				r = (byte)((HexDigit(hex[0]) * 16) + HexDigit(hex[1]));
				g = (byte)((HexDigit(hex[2]) * 16) + HexDigit(hex[3]));
				b = (byte)((HexDigit(hex[4]) * 16) + HexDigit(hex[5]));
				return true;
			default:
				return false;
		}
	}

	private static bool TryParseRgb(string? value, out byte r, out byte g, out byte b)
	{
		r = g = b = 0;
		string text = value?.Trim() ?? "";
		int open = text.IndexOf('(', StringComparison.Ordinal);
		if (open < 0 || !text.EndsWith(')'))
		{
			return false;
		}

		string function = text[..open].Trim();
		if (!function.Equals("rgb", StringComparison.OrdinalIgnoreCase) &&
		    !function.Equals("rgba", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		string[] parts = text[(open + 1)..^1].Split(',', StringSplitOptions.TrimEntries);
		return parts.Length is 3 or 4 &&
		       byte.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out r) &&
		       byte.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out g) &&
		       byte.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out b);
	}

	private static int HexDigit(char c) => char.IsAsciiDigit(c) ? c - '0' : char.ToLowerInvariant(c) - 'a' + 10;

	private static byte MixChannel(byte from, byte to, double amount) =>
		(byte)Math.Clamp(Math.Round(from + ((to - from) * amount)), 0, 255);

	private static string Hex(byte r, byte g, byte b) =>
		string.Create(CultureInfo.InvariantCulture, $"#{r:x2}{g:x2}{b:x2}");
}
