namespace Moka.Red.Core.Code;

/// <summary>Colors JSON: keys, strings, numbers, true, false and null, and the comments JSON with comments allows.</summary>
internal static class JsonLexer
{
	public static List<MokaCodeSpan> Highlight(string text)
	{
		ArgumentNullException.ThrowIfNull(text);
		CodeScan scan = new(text);
		int end = text.Length;
		int pos = 0;
		while (pos < end)
		{
			char c = text[pos];
			char next = CodeText.At(text, pos + 1, end);
			if (char.IsWhiteSpace(c))
			{
				pos++;
			}
			else if (c == '"')
			{
				int close = StringEnd(text, pos, end);
				int after = CodeText.SkipWhitespace(text, close, end);
				scan.Mark(pos, close, CodeText.At(text, after, end) == ':' ? MokaCodeTokenKind.Key : MokaCodeTokenKind.String);
				pos = close;
			}
			else if (char.IsAsciiDigit(c) || (c == '-' && char.IsAsciiDigit(next)))
			{
				int numberEnd = pos + 1;
				while (numberEnd < end && (char.IsAsciiDigit(text[numberEnd]) || text[numberEnd] is '.' or 'e' or 'E' or '+' or '-'))
				{
					numberEnd++;
				}

				scan.Mark(pos, numberEnd, MokaCodeTokenKind.Number);
				pos = numberEnd;
			}
			else if (char.IsAsciiLetter(c))
			{
				int wordEnd = CodeText.IdentifierEnd(text, pos, end);
				if (text.AsSpan(pos, wordEnd - pos) is "true" or "false" or "null")
				{
					scan.Mark(pos, wordEnd, MokaCodeTokenKind.Keyword);
				}

				pos = wordEnd;
			}
			else if (c == '/' && next == '/')
			{
				int lineEnd = CodeText.LineEnd(text, pos, end);
				scan.Mark(pos, lineEnd, MokaCodeTokenKind.Comment);
				pos = lineEnd;
			}
			else if (c == '/' && next == '*')
			{
				int close = CodeText.After(text, "*/", pos + 2, end);
				scan.Mark(pos, close, MokaCodeTokenKind.Comment);
				pos = close;
			}
			else
			{
				if (c is '{' or '}' or '[' or ']' or ':' or ',')
				{
					scan.Mark(pos, pos + 1, MokaCodeTokenKind.Punctuation);
				}

				pos++;
			}
		}

		return scan.Spans;
	}

	// Past the closing quote, or the end of the line when it isn't closed.
	private static int StringEnd(string text, int pos, int end)
	{
		int i = pos + 1;
		while (i < end && text[i] is not ('"' or '\n'))
		{
			i += text[i] == '\\' ? 2 : 1;
		}

		return i < end && text[i] == '"' ? i + 1 : Math.Min(i, end);
	}
}
