using System.Globalization;
using Moka.Red.Navigation.Tabs.Models;

namespace Moka.Red.Navigation.Tabs.Theming;

/// <summary>
///     Utility methods for deterministic color generation, hex parsing, and border position mapping.
/// </summary>
public static class ColorHelper
{
	#region Border Position

	/// <summary>
	///     Converts a <see cref="BorderPosition" /> to the corresponding CSS property name.
	/// </summary>
	public static string ToCssProperty(BorderPosition position)
	{
		return position switch
		{
			BorderPosition.Left => "border-left",
			BorderPosition.Right => "border-right",
			BorderPosition.Top => "border-top",
			BorderPosition.Bottom => "border-bottom",
			_ => "border-left"
		};
	}

	#endregion

	#region Palette

	private static readonly string[] DefaultPalette =
	[
		"#4361ee", "#2ec4b6", "#e63946", "#f4a261",
		"#8338ec", "#3a86ff", "#ff006e", "#2a9d8f",
		"#fb5607", "#e9c46a", "#264653", "#6c757d"
	];

	/// <summary>
	///     Built-in preset colors for the color picker.
	/// </summary>
	public static readonly IReadOnlyList<string> PresetColors =
	[
		"#e63946", "#f4a261", "#e9c46a", "#2a9d8f", "#264653",
		"#4361ee", "#3a86ff", "#8338ec", "#ff006e", "#fb5607",
		"#2ec4b6", "#6c757d", "#1a1a2e", "#ffffff", "#000000"
	];

	#endregion

	#region Deterministic Color

	/// <summary>
	///     Returns a deterministic color from a predefined palette based on a stable hash of the input string.
	///     Same input always yields the same color across processes and platforms.
	/// </summary>
	/// <param name="input">The string to hash (e.g., group name).</param>
	/// <returns>A hex color string (e.g., "#4361ee").</returns>
	public static string GetDeterministicColor(string input)
	{
		ArgumentNullException.ThrowIfNull(input);
		uint hash = StableHash(input);
		int index = (int)(hash % (uint)DefaultPalette.Length);
		return DefaultPalette[index];
	}

	/// <summary>
	///     FNV-1a 32-bit hash — deterministic across processes and platforms.
	/// </summary>
	private static uint StableHash(string input)
	{
		uint hash = 2166136261;
		foreach (char c in input)
		{
			hash ^= c;
			hash *= 16777619;
		}

		return hash;
	}

	#endregion

	#region Hex Parsing

	/// <summary>
	///     Parses a hex color string (#RGB, #RRGGBB, or #RRGGBBAA) into RGBA byte values.
	/// </summary>
	/// <returns><c>true</c> if the hex string was valid and parsed successfully.</returns>
	public static bool TryParseHex(string? hex, out byte r, out byte g, out byte b, out byte a)
	{
		r = g = b = 0;
		a = 255;

		if (string.IsNullOrWhiteSpace(hex))
		{
			return false;
		}

		ReadOnlySpan<char> span = hex.AsSpan().TrimStart('#');

		switch (span.Length)
		{
			case 3: // #RGB
				if (byte.TryParse(new string(span[0], 2), NumberStyles.HexNumber, null, out r) &&
				    byte.TryParse(new string(span[1], 2), NumberStyles.HexNumber, null, out g) &&
				    byte.TryParse(new string(span[2], 2), NumberStyles.HexNumber, null, out b))
				{
					return true;
				}

				r = g = b = 0;
				return false;

			case 6: // #RRGGBB
				if (byte.TryParse(span[..2].ToString(), NumberStyles.HexNumber, null, out r) &&
				    byte.TryParse(span[2..4].ToString(), NumberStyles.HexNumber, null, out g) &&
				    byte.TryParse(span[4..6].ToString(), NumberStyles.HexNumber, null, out b))
				{
					return true;
				}

				r = g = b = 0;
				return false;

			case 8: // #RRGGBBAA
				if (byte.TryParse(span[..2].ToString(), NumberStyles.HexNumber, null, out r) &&
				    byte.TryParse(span[2..4].ToString(), NumberStyles.HexNumber, null, out g) &&
				    byte.TryParse(span[4..6].ToString(), NumberStyles.HexNumber, null, out b) &&
				    byte.TryParse(span[6..8].ToString(), NumberStyles.HexNumber, null, out a))
				{
					return true;
				}

				r = g = b = 0;
				a = 255;
				return false;

			default:
				return false;
		}
	}

	/// <summary>
	///     Formats RGBA byte values as a hex color string.
	/// </summary>
	/// <returns>A hex string in #RRGGBB or #RRGGBBAA format.</returns>
	public static string ToHex(byte r, byte g, byte b, byte a = 255)
	{
		return a == 255
			? $"#{r:x2}{g:x2}{b:x2}"
			: $"#{r:x2}{g:x2}{b:x2}{a:x2}";
	}

	#endregion
}
