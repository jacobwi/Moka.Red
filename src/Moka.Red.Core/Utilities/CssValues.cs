using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Moka.Red.Core.Utilities;

/// <summary>
///     Checks and escapes strings a component writes where Blazor does not encode them: SVG built as a
///     string (rendered as a <c>MarkupString</c> or put in a data URI), a CSS <c>url()</c>, or a
///     <c>&lt;style&gt;</c> block. In those places a quote, a parenthesis or a semicolon from a parameter
///     ends the value it was meant to stay inside, and whatever follows becomes markup or CSS.
/// </summary>
/// <remarks>
///     A value that passes <see cref="IsColor" />, <see cref="IsLength" /> or <see cref="IsLengthList" />
///     holds no quote, angle bracket, ampersand, semicolon, brace or backslash, so it cannot end the SVG
///     attribute or the CSS declaration it is written into.
/// </remarks>
public static partial class CssValues
{
	// A color longer than this is not a color anyone typed.
	private const int MaxColorLength = 256;

	private const string Number = @"(?:[0-9]+(?:\.[0-9]+)?|\.[0-9]+)";

	private const string Unit = "(?:px|em|rem|%|pt|pc|ex|ch|cm|mm|in|q|vw|vh|vmin|vmax)";

	// Functions a color may call. url(), image() and the like are left out: they fetch a resource.
	private static readonly string[] ColorFunctions =
	[
		"rgb", "rgba", "hsl", "hsla", "hwb", "lab", "lch", "oklab", "oklch", "color", "color-mix", "light-dark",
		"var"
	];

	private static readonly SearchValues<char> HexDigits = SearchValues.Create("0123456789abcdefABCDEF");

	private static readonly SearchValues<char> Letters =
		SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");

	// What ends a declaration (;), a rule block ({ }) or a style element (< >), and the escape character,
	// which at the end of a value would swallow the separator written after it.
	private static readonly SearchValues<char> UnsafeChars = SearchValues.Create(";{}<>\\");

	// Everything IsSelfContained has to read with care. A value with none of these, and with only
	// balanced parentheses, cannot leave its declaration, which covers most values.
	private static readonly SearchValues<char> StructuralChars = SearchValues.Create(";{}[]\"'\\/");

	// The same characters plus the parentheses, as bit masks over ASCII for the short values a style
	// usually holds, where a plain loop beats a vectorized search.
	private const ulong SpecialBelow64 =
		(1UL << '"') | (1UL << '\'') | (1UL << '(') | (1UL << ')') | (1UL << '/') | (1UL << ';');

	private const ulong SpecialFrom64 =
		(1UL << ('[' - 64)) | (1UL << ('\\' - 64)) | (1UL << (']' - 64)) | (1UL << ('{' - 64)) | (1UL << ('}' - 64));

	private const int ShortValue = 64;

	// What can end a string or needs a closer look inside one.
	private static readonly SearchValues<char> DoubleQuotedStops = SearchValues.Create("\"\\\n\r\f");
	private static readonly SearchValues<char> SingleQuotedStops = SearchValues.Create("'\\\n\r\f");

	// Bracket nesting deeper than this is not real CSS, and one bit per level keeps the scan
	// allocation free.
	private const int MaxNesting = 64;

	/// <summary>
	///     True for a CSS color: a hex color (<c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c>, <c>#rrggbbaa</c>), a
	///     keyword such as <c>red</c>, <c>transparent</c> or <c>currentColor</c>, or one color function such as
	///     <c>rgb(0 0 0 / 50%)</c>, <c>oklch(62.8% 0.25 29.2)</c>, <c>color-mix(in srgb, red 40%, white)</c> or
	///     <c>var(--moka-color-primary)</c>. Arguments may nest other color functions. Surrounding whitespace
	///     is ignored.
	/// </summary>
	/// <param name="value">The value to check. <c>null</c> and blank values are not colors.</param>
	/// <returns><c>true</c> when <paramref name="value" /> is a color by these rules.</returns>
	public static bool IsColor([NotNullWhen(true)] string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		ReadOnlySpan<char> color = value.AsSpan().Trim();
		if (color.Length > MaxColorLength)
		{
			return false;
		}

		if (color[0] == '#')
		{
			ReadOnlySpan<char> digits = color[1..];
			return digits.Length is 3 or 4 or 6 or 8 && !digits.ContainsAnyExcept(HexDigits);
		}

		return !color.ContainsAnyExcept(Letters) || IsColorFunction(color);
	}

	/// <summary>
	///     True for a non-negative number with an optional CSS unit, such as <c>12px</c>, <c>0.75rem</c>,
	///     <c>.8em</c>, <c>80%</c> or <c>10</c>. The units are px, em, rem, %, pt, pc, ex, ch, cm, mm, in, q, vw,
	///     vh, vmin and vmax, in any case. Surrounding whitespace is ignored; <c>calc()</c> and other functions
	///     are not lengths here.
	/// </summary>
	/// <param name="value">The value to check.</param>
	/// <returns><c>true</c> when <paramref name="value" /> is a length by these rules.</returns>
	public static bool IsLength([NotNullWhen(true)] string? value) =>
		value is not null && LengthPattern().IsMatch(value.Trim());

	/// <summary>
	///     True for one or more lengths as <see cref="IsLength" /> reads them, separated by commas, spaces or
	///     both, such as <c>4 4</c> or <c>6, 3, 1</c>: the form of SVG's <c>stroke-dasharray</c>.
	/// </summary>
	/// <param name="value">The value to check.</param>
	/// <returns><c>true</c> when <paramref name="value" /> is a list of lengths.</returns>
	public static bool IsLengthList([NotNullWhen(true)] string? value) =>
		value is not null && LengthListPattern().IsMatch(value.Trim());

	/// <summary>
	///     True when <paramref name="value" /> holds none of <c>; { } &lt; &gt; \</c>, so written into a CSS
	///     declaration, a <c>style</c> attribute or a <c>&lt;style&gt;</c> block it cannot end the declaration,
	///     the rule or the element, or escape the separator after it. Use it for values too varied to check
	///     against a grammar. It says nothing about whether the value is valid CSS, and a value that passes
	///     can still hold a <c>url()</c>.
	/// </summary>
	/// <param name="value">The value to check. <c>null</c> is not safe to write.</param>
	/// <returns><c>true</c> when the value can be written as it is.</returns>
	public static bool IsSafe([NotNullWhen(true)] string? value) =>
		value is not null && !value.AsSpan().ContainsAny(UnsafeChars);

	/// <summary>
	///     True when the value, written as one declaration's value in a <c>style</c> attribute, stays inside
	///     that declaration: every string, bracket, <c>url()</c> and comment it opens is closed, it holds no
	///     semicolon outside them and no brace, and it does not end in an escape. A semicolon inside quotes or
	///     brackets is part of the value, as in <c>url("data:image/png;base64,...")</c>. <see cref="StyleBuilder" />
	///     drops a value that fails this check. It says nothing about whether the value is valid CSS, and it
	///     is not enough for a <c>&lt;style&gt;</c> element, where <c>&lt;/style&gt;</c> ends the element even
	///     inside a string: use <see cref="IsSafe" /> there.
	/// </summary>
	/// <param name="value">The value to check. <c>null</c> is not written anywhere, so it is not contained.</param>
	/// <returns><c>true</c> when the value cannot end its declaration or run into the next one.</returns>
	public static bool IsSelfContained([NotNullWhen(true)] string? value)
	{
		if (value is null)
		{
			return false;
		}

		ReadOnlySpan<char> span = value;
		if (span.Length > ShortValue)
		{
			return span.ContainsAny(StructuralChars) ? ScanDeclarationValue(span) : ParenthesesClose(span);
		}

		int depth = 0;
		foreach (char c in span)
		{
			ulong mask = c < 64 ? SpecialBelow64 : SpecialFrom64;
			if (c >= 128 || ((mask >> (c & 63)) & 1) == 0)
			{
				continue;
			}

			if (c == '(')
			{
				depth++;
			}
			else if (c == ')')
			{
				depth = Math.Max(0, depth - 1);
			}
			else
			{
				return ScanDeclarationValue(span);
			}
		}

		return depth == 0;
	}

	/// <summary>
	///     <paramref name="value" /> without surrounding whitespace when <see cref="IsColor" /> accepts it,
	///     otherwise <paramref name="fallback" />, which is returned as given.
	/// </summary>
	/// <param name="value">The color to check.</param>
	/// <param name="fallback">What to use instead, usually the parameter's default.</param>
	/// <returns>The checked color or the fallback.</returns>
	public static string ColorOrDefault(string? value, string fallback)
	{
		ArgumentNullException.ThrowIfNull(fallback);
		return IsColor(value) ? value.Trim() : fallback;
	}

	/// <summary>
	///     <paramref name="value" /> without surrounding whitespace when <see cref="IsLength" /> accepts it,
	///     otherwise <paramref name="fallback" />, which is returned as given.
	/// </summary>
	/// <param name="value">The length to check.</param>
	/// <param name="fallback">What to use instead, usually the parameter's default.</param>
	/// <returns>The checked length or the fallback.</returns>
	public static string LengthOrDefault(string? value, string fallback)
	{
		ArgumentNullException.ThrowIfNull(fallback);
		return value is not null && IsLength(value) ? value.Trim() : fallback;
	}

	/// <summary>
	///     Reads an absolute CSS length as pixels: a non-negative number with px, pt, pc, in, cm, mm, q, em or
	///     rem, or with no unit for px. em and rem count as 16px, the default root font size and the one an SVG
	///     image has. Units that depend on a layout (%, vw, vh, ex, ch and so on) do not convert.
	/// </summary>
	/// <param name="value">The length to read. Surrounding whitespace is ignored.</param>
	/// <param name="pixels">The length in pixels, or 0 when it could not be read.</param>
	/// <returns><c>true</c> when <paramref name="value" /> converted to a finite number of pixels.</returns>
	public static bool TryParsePixels([NotNullWhen(true)] string? value, out double pixels)
	{
		pixels = 0;
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		Match match = LengthPattern().Match(value.Trim());
		if (!match.Success)
		{
			return false;
		}

		double number = double.Parse(match.Groups["number"].ValueSpan, NumberStyles.AllowDecimalPoint,
			CultureInfo.InvariantCulture);
		double px = match.Groups["unit"].Value.ToUpperInvariant() switch
		{
			"" or "PX" => number,
			"PT" => number * 96 / 72,
			"PC" => number * 16,
			"IN" => number * 96,
			"CM" => number * 96 / 2.54,
			"MM" => number * 96 / 25.4,
			"Q" => number * 96 / 101.6,
			"EM" or "REM" => number * 16,
			_ => double.NaN
		};

		if (!double.IsFinite(px))
		{
			return false;
		}

		pixels = px;
		return true;
	}

	/// <summary>
	///     <paramref name="url" /> as a CSS <c>url("...")</c>. Quotes and backslashes are escaped and control
	///     characters become CSS hex escapes, so the value cannot end the string, the function or the
	///     declaration it is written into. The URL itself is not checked.
	/// </summary>
	/// <param name="url">The URL, as the consumer gave it.</param>
	/// <returns>The <c>url()</c> function, ready for a declaration.</returns>
	[SuppressMessage("Design", "CA1054:URI-like parameters should not be strings",
		Justification = "Takes the URL as a component parameter holds it, often relative. Uri would reject or rewrite some.")]
	[SuppressMessage("Design", "CA1055:URI-like return values should not be strings",
		Justification = "Returns CSS text, a url() function, not a URI.")]
	public static string Url(string url)
	{
		ArgumentNullException.ThrowIfNull(url);

		var sb = new StringBuilder(url.Length + 8).Append("url(\"");
		foreach (char c in url)
		{
			if (c is '"' or '\\')
			{
				sb.Append('\\').Append(c);
			}
			else if (char.IsControl(c))
			{
				sb.Append('\\').Append(((int)c).ToString("x", CultureInfo.InvariantCulture)).Append(' ');
			}
			else
			{
				sb.Append(c);
			}
		}

		return sb.Append("\")").ToString();
	}

	/// <summary>
	///     An SVG document as a CSS <c>url("data:image/svg+xml,...")</c>. The markup is percent-encoded, so
	///     nothing in it can end the URI, the string or the declaration. The markup is not checked: escape
	///     every value that goes into it with <see cref="EscapeXml" /> or check it first.
	/// </summary>
	/// <param name="svg">The complete SVG document.</param>
	/// <returns>The <c>url()</c> function, ready for a declaration such as <c>background-image</c>.</returns>
	[SuppressMessage("Design", "CA1055:URI-like return values should not be strings",
		Justification = "Returns CSS text, a url() function, not a URI.")]
	public static string SvgDataUrl(string svg)
	{
		ArgumentNullException.ThrowIfNull(svg);
		return $"url(\"data:image/svg+xml,{Uri.EscapeDataString(svg)}\")";
	}

	/// <summary>
	///     Escapes <paramref name="value" /> for an XML or SVG attribute, quoted either way, or a text node.
	///     Characters XML does not allow at all (most control characters, unpaired surrogates) are dropped,
	///     so the document stays well formed.
	/// </summary>
	/// <param name="value">The text to escape.</param>
	/// <returns>The escaped text.</returns>
	public static string EscapeXml(string value)
	{
		ArgumentNullException.ThrowIfNull(value);

		StringBuilder? kept = null;
		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];
			if (XmlConvert.IsXmlChar(c))
			{
				kept?.Append(c);
			}
			else if (i + 1 < value.Length && XmlConvert.IsXmlSurrogatePair(value[i + 1], c))
			{
				kept?.Append(c).Append(value[i + 1]);
				i++;
			}
			else
			{
				kept ??= new StringBuilder(value.Length).Append(value, 0, i);
			}
		}

		return SecurityElement.Escape(kept?.ToString() ?? value);
	}

	// With no strings, comments, escapes, braces, square brackets or semicolons, only an unclosed
	// parenthesis could run into the next declaration. A url( closes at its first ')', never later than
	// this plain count says, so the count is enough.
	private static bool ParenthesesClose(ReadOnlySpan<char> value)
	{
		int open = value.IndexOf('(');
		if (open < 0)
		{
			return true;
		}

		int depth = 0;
		foreach (char c in value[open..])
		{
			if (c == '(')
			{
				depth++;
			}
			else if (c == ')' && depth > 0)
			{
				depth--;
			}
		}

		return depth == 0;
	}

	// Follows the CSS tokenizer as far as the question needs: strings, comments, escapes, brackets and
	// url( tokens. Anything it cannot place safely makes the value fail.
	private static bool ScanDeclarationValue(ReadOnlySpan<char> value)
	{
		// An odd run of backslashes at the end leaves the last one to escape whatever follows the value.
		int backslashes = value.Length - value.TrimEnd('\\').Length;
		if (backslashes % 2 == 1)
		{
			return false;
		}

		ulong squareBrackets = 0; // one bit per open bracket, set for '[' and clear for '('
		int depth = 0;
		int i = 0;
		while (i < value.Length)
		{
			char c = value[i];
			switch (c)
			{
				case '"' or '\'':
					if (!SkipString(value, ref i))
					{
						return false;
					}

					break;
				case '/' when i + 1 < value.Length && value[i + 1] == '*':
					int end = value[(i + 2)..].IndexOf("*/", StringComparison.Ordinal);
					if (end < 0)
					{
						return false;
					}

					i += end + 4;
					break;
				case '(' or '[':
					if (depth == MaxNesting)
					{
						return false;
					}

					squareBrackets = (squareBrackets << 1) | (c == '[' ? 1UL : 0UL);
					depth++;
					i++;
					break;
				case ')' or ']':
					// A closer that does not match the innermost bracket is an ordinary token.
					if (depth > 0 && (squareBrackets & 1) == (c == ']' ? 1UL : 0UL))
					{
						squareBrackets >>= 1;
						depth--;
					}

					i++;
					break;
				case '{' or '}':
					return false;
				case ';':
					if (depth == 0)
					{
						return false;
					}

					i++;
					break;
				default:
					if (!SkipToken(value, ref i))
					{
						return false;
					}

					break;
			}
		}

		return depth == 0;
	}

	// A string ends at its quote. A raw newline ends it early, and what follows the newline would be
	// read as CSS, so that fails like a string that never closes.
	private static bool SkipString(ReadOnlySpan<char> value, ref int i)
	{
		char quote = value[i];
		SearchValues<char> stops = quote == '"' ? DoubleQuotedStops : SingleQuotedStops;
		i++;
		while (i < value.Length)
		{
			int next = value[i..].IndexOfAny(stops);
			if (next < 0)
			{
				return false;
			}

			i += next;
			char c = value[i];
			if (c == quote)
			{
				i++;
				return true;
			}

			if (IsNewline(c))
			{
				return false;
			}

			if (c == '\\')
			{
				if (i + 1 >= value.Length)
				{
					return false;
				}

				// An escaped newline continues the string, and CR LF is one newline.
				i += value[i + 1] == '\r' && i + 2 < value.Length && value[i + 2] == '\n' ? 3 : 2;
			}
		}

		return false;
	}

	// Numbers, names and everything else the main loop does not handle. Only a name spelling url can
	// start a url( token, and that token runs to its first ')' with no nesting, strings or comments.
	private static bool SkipToken(ReadOnlySpan<char> value, ref int i)
	{
		if (StartsNumber(value, i))
		{
			SkipNumber(value, ref i);

			// A unit is part of the number, so in "1url(" the '(' opens a plain block.
			if (StartsName(value, i))
			{
				SkipName(value, ref i, out _);
			}

			return true;
		}

		char c = value[i];
		if (c is '#' or '@')
		{
			// A hash or at-keyword owns the name after it, so "#url(" is not a url( token either.
			i++;
			if (i < value.Length && (IsIdentChar(value[i]) || IsValidEscape(value, i)))
			{
				SkipName(value, ref i, out _);
			}

			return true;
		}

		if (StartsName(value, i))
		{
			SkipName(value, ref i, out bool isUrl);
			if (isUrl && i < value.Length && value[i] == '(' && !IsQuotedArgument(value, i + 1))
			{
				return SkipUrl(value, ref i);
			}

			return true;
		}

		i++;
		return true;
	}

	private static bool SkipUrl(ReadOnlySpan<char> value, ref int i)
	{
		i++;
		while (i < value.Length)
		{
			int next = value[i..].IndexOfAny(')', '\\');
			if (next < 0)
			{
				return false;
			}

			i += next;
			if (value[i] == ')')
			{
				i++;
				return true;
			}

			i += i + 1 < value.Length && !IsNewline(value[i + 1]) ? 2 : 1;
		}

		return false;
	}

	// url( followed by a quoted argument is an ordinary function whose argument is a string.
	private static bool IsQuotedArgument(ReadOnlySpan<char> value, int i)
	{
		while (i < value.Length && IsWhitespace(value[i]))
		{
			i++;
		}

		return i < value.Length && value[i] is '"' or '\'';
	}

	private static void SkipNumber(ReadOnlySpan<char> value, ref int i)
	{
		if (value[i] is '+' or '-')
		{
			i++;
		}

		SkipDigits(value, ref i);
		if (i + 1 < value.Length && value[i] == '.' && char.IsAsciiDigit(value[i + 1]))
		{
			i++;
			SkipDigits(value, ref i);
		}

		if (i + 1 < value.Length && value[i] is 'e' or 'E')
		{
			int digit = value[i + 1] is '+' or '-' ? i + 2 : i + 1;
			if (digit < value.Length && char.IsAsciiDigit(value[digit]))
			{
				i = digit;
				SkipDigits(value, ref i);
			}
		}
	}

	private static void SkipDigits(ReadOnlySpan<char> value, ref int i)
	{
		while (i < value.Length && char.IsAsciiDigit(value[i]))
		{
			i++;
		}
	}

	// Reads name characters and escapes. isUrl says whether they spell url in any case, escapes
	// included, since CSS compares the name after unescaping it.
	private static void SkipName(ReadOnlySpan<char> value, ref int i, out bool isUrl)
	{
		const string url = "url";
		int length = 0;
		bool matches = true;
		while (i < value.Length)
		{
			int codePoint;
			if (IsIdentChar(value[i]))
			{
				codePoint = value[i];
				i++;
			}
			else if (IsValidEscape(value, i))
			{
				codePoint = SkipEscape(value, ref i);
			}
			else
			{
				break;
			}

			if (length < url.Length)
			{
				matches &= (codePoint | 0x20) == url[length];
			}

			length++;
		}

		isUrl = matches && length == url.Length;
	}

	// A hex escape takes up to six digits and one whitespace after them.
	private static int SkipEscape(ReadOnlySpan<char> value, ref int i)
	{
		i++;
		if (!char.IsAsciiHexDigit(value[i]))
		{
			return value[i++];
		}

		int codePoint = 0;
		int digits = 0;
		while (i < value.Length && digits < 6 && char.IsAsciiHexDigit(value[i]))
		{
			codePoint = (codePoint * 16) + HexValue(value[i]);
			i++;
			digits++;
		}

		if (i < value.Length && IsWhitespace(value[i]))
		{
			i += value[i] == '\r' && i + 1 < value.Length && value[i + 1] == '\n' ? 2 : 1;
		}

		return codePoint;
	}

	private static int HexValue(char c) => char.IsAsciiDigit(c) ? c - '0' : (c | 0x20) - 'a' + 10;

	private static bool StartsNumber(ReadOnlySpan<char> value, int i)
	{
		char c = value[i];
		if (char.IsAsciiDigit(c))
		{
			return true;
		}

		if (c == '.')
		{
			return i + 1 < value.Length && char.IsAsciiDigit(value[i + 1]);
		}

		if (c is not ('+' or '-') || i + 1 >= value.Length)
		{
			return false;
		}

		char next = value[i + 1];
		return char.IsAsciiDigit(next) || (next == '.' && i + 2 < value.Length && char.IsAsciiDigit(value[i + 2]));
	}

	private static bool StartsName(ReadOnlySpan<char> value, int i)
	{
		if (i >= value.Length)
		{
			return false;
		}

		char c = value[i];
		if (c == '-')
		{
			return i + 1 < value.Length &&
			       (IsIdentStartChar(value[i + 1]) || value[i + 1] == '-' || IsValidEscape(value, i + 1));
		}

		return IsIdentStartChar(c) || IsValidEscape(value, i);
	}

	private static bool IsIdentStartChar(char c) => char.IsAsciiLetter(c) || c == '_' || c >= 0x80;

	private static bool IsIdentChar(char c) => IsIdentStartChar(c) || char.IsAsciiDigit(c) || c == '-';

	private static bool IsValidEscape(ReadOnlySpan<char> value, int i) =>
		i + 1 < value.Length && value[i] == '\\' && !IsNewline(value[i + 1]);

	private static bool IsNewline(char c) => c is '\n' or '\r' or '\f';

	private static bool IsWhitespace(char c) => c is ' ' or '\t' || IsNewline(c);

	// One function call covering the whole value. Arguments may nest other color functions, as in
	// var(--x, rgb(0 0 0)), but hold nothing that could end an attribute or a declaration.
	private static bool IsColorFunction(ReadOnlySpan<char> color)
	{
		int depth = 0;
		for (int i = 0; i < color.Length; i++)
		{
			char c = color[i];
			if (depth == 0 && i > 0 && color[i - 1] == ')')
			{
				// Text after the outer call's closing parenthesis.
				return false;
			}

			if (c == '(')
			{
				if (!IsColorFunctionName(color[..i]))
				{
					return false;
				}

				depth++;
			}
			else if (c == ')')
			{
				if (--depth < 0)
				{
					return false;
				}
			}
			else if (depth == 0 ? !IsNameChar(c) : !IsArgumentChar(c))
			{
				return false;
			}
		}

		return depth == 0 && color[^1] == ')';
	}

	// The name is the run of letters and hyphens just before the opening parenthesis.
	private static bool IsColorFunctionName(ReadOnlySpan<char> before)
	{
		int start = before.Length;
		while (start > 0 && IsNameChar(before[start - 1]))
		{
			start--;
		}

		ReadOnlySpan<char> name = before[start..];
		foreach (string function in ColorFunctions)
		{
			if (name.Equals(function, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsNameChar(char c) => char.IsAsciiLetter(c) || c == '-';

	private static bool IsArgumentChar(char c) =>
		char.IsAsciiLetterOrDigit(c) || c is ' ' or '.' or ',' or '%' or '/' or '+' or '-' or '_' or '#';

	[GeneratedRegex("^(?<number>" + Number + ")(?<unit>" + Unit + ")?$",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex LengthPattern();

	[GeneratedRegex("^" + Number + Unit + "?(?:(?:[ \t]*,[ \t]*|[ \t]+)" + Number + Unit + "?)*$",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex LengthListPattern();
}
