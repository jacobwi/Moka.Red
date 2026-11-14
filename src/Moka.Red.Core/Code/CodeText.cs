namespace Moka.Red.Core.Code;

/// <summary>Character tests and searches the highlighters share. Every search runs forward from a position and stops at an end.</summary>
internal static class CodeText
{
	public static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

	public static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';

	public static char At(string text, int index, int end) => index < end ? text[index] : '\0';

	public static int IdentifierEnd(string text, int pos, int end)
	{
		while (pos < end && IsIdentifierPart(text[pos]))
		{
			pos++;
		}

		return pos;
	}

	public static int SkipWhitespace(string text, int pos, int end)
	{
		while (pos < end && char.IsWhiteSpace(text[pos]))
		{
			pos++;
		}

		return pos;
	}

	public static int SkipSpaces(string text, int pos, int end)
	{
		while (pos < end && text[pos] is ' ' or '\t')
		{
			pos++;
		}

		return pos;
	}

	/// <summary>Where the line holding <paramref name="pos" /> ends: its line break, or the end.</summary>
	public static int LineEnd(string text, int pos, int end)
	{
		int found = text.IndexOf('\n', pos, end - pos);
		return found < 0 ? end : found;
	}

	/// <summary>The position just past the first <paramref name="close" /> at or after <paramref name="pos" />, or the end.</summary>
	public static int After(string text, string close, int pos, int end)
	{
		if (pos >= end)
		{
			return end;
		}

		int found = text.IndexOf(close, pos, end - pos, StringComparison.Ordinal);
		return found < 0 ? end : found + close.Length;
	}

	public static bool StartsWith(string text, int pos, int end, string value) =>
		end - pos >= value.Length && string.CompareOrdinal(text, pos, value, 0, value.Length) == 0;

	public static int RunLength(string text, int pos, int end, char c)
	{
		int i = pos;
		while (i < end && text[i] == c)
		{
			i++;
		}

		return i - pos;
	}

	/// <summary>Whether only spaces and tabs stand between the start of its line and <paramref name="pos" />.</summary>
	public static bool StartsLine(string text, int pos)
	{
		int i = pos - 1;
		while (i >= 0 && text[i] is ' ' or '\t')
		{
			i--;
		}

		return i < 0 || text[i] == '\n';
	}
}
