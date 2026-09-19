using System.Globalization;
using System.Text;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Watermark;

/// <summary>
///     Builds the SVG tile a text watermark repeats. The tile is an image, so it cannot read the page's
///     CSS: sizes are worked out here and the text's color comes from the overlay the tile masks.
/// </summary>
internal static class WatermarkTile
{
	/// <summary>Font size when <c>FontSize</c> is missing or not a length.</summary>
	internal const double DefaultFontSizePx = 48;

	/// <summary>Gap when <c>Gap</c> is missing or not a length.</summary>
	internal const double DefaultGapPx = 100;

	// Height of one line of text as a share of the font size, room for ascenders and descenders.
	private const double LineHeight = 1.2;

	// Beyond this a tile is not a watermark any more, and browsers cap image sizes around here.
	private const int MaxTileSize = 16384;

	/// <summary>
	///     The CSS <c>url()</c> of an SVG tile that holds <paramref name="text" /> once, rotated about its center,
	///     with <paramref name="gapPx" /> of space to spare on each axis. Repeated, neighbouring marks sit one
	///     gap apart.
	/// </summary>
	internal static string Build(string text, double fontSizePx, double gapPx, int rotation, string fontFamily)
	{
		double textWidth = EstimateWidthEm(text) * fontSizePx;
		double textHeight = fontSizePx * LineHeight;
		double radians = rotation * Math.PI / 180;
		double cos = Math.Abs(Math.Cos(radians));
		double sin = Math.Abs(Math.Sin(radians));

		int width = TileSide(textWidth * cos + textHeight * sin + gapPx);
		int height = TileSide(textWidth * sin + textHeight * cos + gapPx);
		double cx = width / 2.0;
		double cy = height / 2.0;

		string svg = string.Create(CultureInfo.InvariantCulture,
			$"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'>"
			+ $"<text x='{cx:0.##}' y='{cy:0.##}' text-anchor='middle' dominant-baseline='central'"
			+ $" font-family='{CssValues.EscapeXml(fontFamily)}' font-size='{fontSizePx:0.##}'"
			+ $" transform='rotate({rotation} {cx:0.##} {cy:0.##})'>{CssValues.EscapeXml(text)}</text></svg>");

		return CssValues.SvgDataUrl(svg);
	}

	/// <summary>
	///     Guesses the width of <paramref name="text" /> in em. Nothing can measure text inside an image, so
	///     this counts per character and errs wide: a wrong guess should cost a little extra gap, not a
	///     clipped word.
	/// </summary>
	internal static double EstimateWidthEm(string text)
	{
		double em = 0;
		foreach (Rune rune in text.EnumerateRunes())
		{
			em += WidthEm(rune);
		}

		return em;
	}

	private static int TileSide(double px) => (int)Math.Clamp(Math.Ceiling(px), 1, MaxTileSize);

	private static double WidthEm(Rune rune)
	{
		if (Rune.IsWhiteSpace(rune))
		{
			return 0.3;
		}

		if (Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark
		    or UnicodeCategory.Format)
		{
			return 0;
		}

		if (IsWide(rune.Value))
		{
			return 1.05;
		}

		if (!rune.IsAscii)
		{
			return 0.8;
		}

		char c = (char)rune.Value;
		if (c is 'm' or 'w' or 'M' or 'W' or '@' or '%')
		{
			return 1;
		}

		if (c is 'i' or 'j' or 'l' or 'I' or '.' or ',' or ':' or ';' or '\'' or '!' or '|' or '`')
		{
			return 0.35;
		}

		return char.IsAsciiLetterUpper(c) || char.IsAsciiDigit(c) ? 0.8 : 0.62;
	}

	// East Asian wide and fullwidth characters and emoji take a full em or more.
	private static bool IsWide(int codePoint) => codePoint is >= 0x1100 and <= 0x115F
		or >= 0x2E80 and <= 0xA4CF
		or >= 0xAC00 and <= 0xD7A3
		or >= 0xF900 and <= 0xFAFF
		or >= 0xFE30 and <= 0xFE4F
		or >= 0xFF00 and <= 0xFF60
		or >= 0xFFE0 and <= 0xFFE6
		or >= 0x1F300 and <= 0x1FAFF
		or >= 0x20000 and <= 0x3FFFD;
}
