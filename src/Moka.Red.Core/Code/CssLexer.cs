namespace Moka.Red.Core.Code;

/// <summary>
///     Colors CSS: selectors, at-rules, properties and their values, nesting included. Each statement is read up to the
///     first '{', ';' or '}' to tell a nested rule from a declaration, then colored, so every character is read twice
///     at most.
/// </summary>
internal sealed class CssLexer(CodeScan scan)
{
	private readonly string _text = scan.Text;

	public static List<MokaCodeSpan> Highlight(string text)
	{
		ArgumentNullException.ThrowIfNull(text);
		CodeScan scan = new(text);
		new CssLexer(scan).Scan(0, text.Length);
		return scan.Spans;
	}

	public void Scan(int pos, int end)
	{
		while (pos < end)
		{
			char c = _text[pos];
			if (char.IsWhiteSpace(c))
			{
				pos++;
			}
			else if (c == '/' && CodeText.At(_text, pos + 1, end) == '*')
			{
				pos = Comment(pos, end);
			}
			else if (c is '{' or '}' or ';')
			{
				scan.Mark(pos, pos + 1, MokaCodeTokenKind.Punctuation);
				pos++;
			}
			else if (c == '@' && IsNameStart(CodeText.At(_text, pos + 1, end)))
			{
				int nameEnd = NameEnd(pos + 1, end);
				scan.Mark(pos, nameEnd, MokaCodeTokenKind.Directive);
				pos = Value(nameEnd, StatementEnd(nameEnd, end), end);
			}
			else
			{
				int stop = StatementEnd(pos, end);
				pos = stop < end && _text[stop] == '{' ? Selector(pos, stop, end) : Declaration(pos, stop, end);
			}
		}
	}

	private static bool IsNameStart(char c) => char.IsLetter(c) || c is '_' or '-' or '\\';

	private static bool IsNamePart(char c) => char.IsLetterOrDigit(c) || c is '_' or '-' or '\\';

	private int NameEnd(int pos, int end)
	{
		while (pos < end && IsNamePart(_text[pos]))
		{
			// An escape takes the character after it into the name.
			pos += _text[pos] == '\\' ? 2 : 1;
		}

		return Math.Min(pos, end);
	}

	private int Comment(int pos, int end)
	{
		int close = CodeText.After(_text, "*/", pos + 2, end);
		scan.Mark(pos, close, MokaCodeTokenKind.Comment);
		return close;
	}

	private int QuotedString(int pos, int end)
	{
		char quote = _text[pos];
		int i = pos + 1;
		while (i < end && _text[i] != quote && _text[i] != '\n')
		{
			i += _text[i] == '\\' ? 2 : 1;
		}

		i = Math.Min(end, i < end && _text[i] == quote ? i + 1 : i);
		scan.Mark(pos, i, MokaCodeTokenKind.String);
		return i;
	}

	// The first '{', ';' or '}' outside comments, strings and brackets, or the end.
	private int StatementEnd(int pos, int end)
	{
		int depth = 0;
		int i = pos;
		while (i < end)
		{
			char c = _text[i];
			if (c == '/' && CodeText.At(_text, i + 1, end) == '*')
			{
				i = CodeText.After(_text, "*/", i + 2, end);
				continue;
			}

			if (c is '"' or '\'')
			{
				i++;
				while (i < end && _text[i] != c && _text[i] != '\n')
				{
					i += _text[i] == '\\' ? 2 : 1;
				}

				i++;
				continue;
			}

			if (c is '(' or '[')
			{
				depth++;
			}
			else if (c is ')' or ']')
			{
				depth = Math.Max(0, depth - 1);
			}
			else if (depth == 0 && c is '{' or ';' or '}')
			{
				return i;
			}

			i++;
		}

		return end;
	}

	private int Selector(int pos, int brace, int end)
	{
		bool inBrackets = false;
		while (pos < brace)
		{
			char c = _text[pos];
			char next = CodeText.At(_text, pos + 1, end);
			if (char.IsWhiteSpace(c))
			{
				pos++;
			}
			else if (c == '/' && next == '*')
			{
				pos = Comment(pos, end);
			}
			else if (c is '"' or '\'')
			{
				pos = QuotedString(pos, brace);
			}
			else if (c is '.' or '#' && IsNameStart(next))
			{
				int nameEnd = NameEnd(pos + 1, brace);
				scan.Mark(pos, nameEnd, MokaCodeTokenKind.Type);
				pos = nameEnd;
			}
			else if (c == ':')
			{
				int nameStart = next == ':' ? pos + 2 : pos + 1;
				int nameEnd = NameEnd(nameStart, brace);
				scan.Mark(pos, nameEnd, MokaCodeTokenKind.Keyword);
				pos = Math.Max(nameEnd, pos + 1);
			}
			else if (c is '[' or ']')
			{
				inBrackets = c == '[';
				scan.Mark(pos, pos + 1, MokaCodeTokenKind.Punctuation);
				pos++;
			}
			else if (char.IsAsciiDigit(c))
			{
				pos = Number(pos, brace);
			}
			else if (IsNameStart(c))
			{
				int nameEnd = NameEnd(pos, brace);
				scan.Mark(pos, nameEnd, inBrackets ? MokaCodeTokenKind.Attribute : MokaCodeTokenKind.Tag);
				pos = nameEnd;
			}
			else
			{
				scan.Mark(pos, pos + 1, MokaCodeTokenKind.Punctuation);
				pos++;
			}
		}

		scan.Mark(brace, brace + 1, MokaCodeTokenKind.Punctuation);
		return brace + 1;
	}

	private int Declaration(int pos, int stop, int end)
	{
		if (IsNameStart(_text[pos]))
		{
			int nameEnd = NameEnd(pos, stop);
			int colon = CodeText.SkipWhitespace(_text, nameEnd, stop);
			if (colon < stop && _text[colon] == ':')
			{
				scan.Mark(pos, nameEnd, MokaCodeTokenKind.Key);
				scan.Mark(colon, colon + 1, MokaCodeTokenKind.Punctuation);
				return Value(colon + 1, stop, end);
			}
		}

		return Value(pos, stop, end);
	}

	// A property's value or an at-rule's prelude, up to stop.
	private int Value(int pos, int stop, int end)
	{
		while (pos < stop)
		{
			char c = _text[pos];
			char next = CodeText.At(_text, pos + 1, stop);
			if (char.IsWhiteSpace(c))
			{
				pos++;
			}
			else if (c == '/' && next == '*')
			{
				pos = Comment(pos, end);
			}
			else if (c is '"' or '\'')
			{
				pos = QuotedString(pos, stop);
			}
			else if (c == '#' && char.IsAsciiLetterOrDigit(next))
			{
				int hashEnd = pos + 1;
				while (hashEnd < stop && char.IsAsciiLetterOrDigit(_text[hashEnd]))
				{
					hashEnd++;
				}

				scan.Mark(pos, hashEnd, MokaCodeTokenKind.Constant);
				pos = hashEnd;
			}
			else if (char.IsAsciiDigit(c) || (c is '.' or '-' or '+' && StartsNumber(pos + 1, stop)))
			{
				pos = Number(pos, stop);
			}
			else if (c == '!' && char.IsAsciiLetter(next))
			{
				int wordEnd = NameEnd(pos + 1, stop);
				scan.Mark(pos, wordEnd, MokaCodeTokenKind.Keyword);
				pos = wordEnd;
			}
			else if (IsNameStart(c))
			{
				pos = Word(pos, stop);
			}
			else
			{
				scan.Mark(pos, pos + 1, MokaCodeTokenKind.Punctuation);
				pos++;
			}
		}

		return stop;
	}

	private bool StartsNumber(int pos, int stop)
	{
		char c = CodeText.At(_text, pos, stop);
		return char.IsAsciiDigit(c) || (c == '.' && char.IsAsciiDigit(CodeText.At(_text, pos + 1, stop)));
	}

	// A name in a value: a custom property, a media feature before its colon, a function, or a keyword such as solid.
	private int Word(int pos, int stop)
	{
		int wordEnd = NameEnd(pos, stop);
		ReadOnlySpan<char> word = _text.AsSpan(pos, wordEnd - pos);
		char after = CodeText.At(_text, CodeText.SkipWhitespace(_text, wordEnd, stop), stop);
		if (word.StartsWith("--") || after == ':')
		{
			scan.Mark(pos, wordEnd, MokaCodeTokenKind.Key);
		}
		else if (word is "and" or "not" or "only" or "or")
		{
			scan.Mark(pos, wordEnd, MokaCodeTokenKind.Keyword);
		}
		else if (CodeText.At(_text, wordEnd, stop) == '(' && word.Equals("url", StringComparison.OrdinalIgnoreCase))
		{
			return Url(wordEnd, stop);
		}

		return wordEnd;
	}

	// url( with an unquoted address, which may hold characters that mean something elsewhere.
	private int Url(int paren, int stop)
	{
		scan.Mark(paren, paren + 1, MokaCodeTokenKind.Punctuation);
		int content = CodeText.SkipWhitespace(_text, paren + 1, stop);
		if (CodeText.At(_text, content, stop) is '"' or '\'')
		{
			return content;
		}

		int close = content;
		while (close < stop && _text[close] != ')')
		{
			close++;
		}

		scan.Mark(content, close, MokaCodeTokenKind.String);
		return close;
	}

	private int Number(int pos, int stop)
	{
		int i = pos;
		if (_text[i] is '-' or '+')
		{
			i++;
		}

		while (i < stop && (char.IsAsciiDigit(_text[i]) || _text[i] == '.'))
		{
			i++;
		}

		if (i < stop && (_text[i] | 0x20) == 'e' && char.IsAsciiDigit(CodeText.At(_text, i + 1, stop)))
		{
			i++;
			while (i < stop && char.IsAsciiDigit(_text[i]))
			{
				i++;
			}
		}

		while (i < stop && (char.IsAsciiLetter(_text[i]) || _text[i] == '%'))
		{
			i++;
		}

		scan.Mark(pos, i, MokaCodeTokenKind.Number);
		return i;
	}
}
