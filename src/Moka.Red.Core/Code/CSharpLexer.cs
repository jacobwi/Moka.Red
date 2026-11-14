using System.Collections.Frozen;

namespace Moka.Red.Core.Code;

/// <summary>
///     Colors C#, alone or inside Razor. One forward pass: comments, strings of every form (holes colored as C#),
///     numbers, keywords, preprocessor lines and attributes are certain; types are a guess from where a name stands.
/// </summary>
internal sealed class CSharpLexer
{
	// Far enough for a type argument list or a declaration, short enough that a text of '<' stays linear.
	private const int LookaheadLimit = 128;

	private static readonly FrozenSet<string> Keywords = FrozenSet.Create(StringComparer.Ordinal,
		"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
		"decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
		"fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
		"long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
		"public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
		"struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
		"using", "virtual", "void", "volatile", "while",
		// Contextual keywords that are rarely a name. Query words, value, args, field and file are left out, because a
		// variable called group or file is common and coloring it would mislead.
		"add", "and", "async", "await", "dynamic", "get", "global", "init", "nameof", "nint", "not", "notnull", "nuint", "or",
		"partial", "record", "remove", "required", "scoped", "set", "unmanaged", "var", "when", "where", "with", "yield");

	// A name followed by one of these is used, not declared: "x is Foo", "item in Items".
	private static readonly FrozenSet<string> NotDeclaredBy = FrozenSet.Create(StringComparer.Ordinal,
		"is", "as", "in", "and", "or", "not", "when", "with", "where", "switch", "select", "from", "into", "on", "equals",
		"by", "orderby", "ascending", "descending", "group", "let", "join");

	private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> KeywordLookup =
		Keywords.GetAlternateLookup<ReadOnlySpan<char>>();

	private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> NotDeclaredByLookup =
		NotDeclaredBy.GetAlternateLookup<ReadOnlySpan<char>>();

	private readonly CodeScan _scan;
	private readonly string _text;
	private readonly bool _razor;

	public CSharpLexer(CodeScan scan, bool razor)
	{
		_scan = scan;
		_text = scan.Text;
		_razor = razor;
	}

	/// <summary>The markup scanner Razor code blocks hand their markup to. Null outside Razor.</summary>
	public MarkupLexer? Markup { get; set; }

	public static List<MokaCodeSpan> Highlight(string text)
	{
		ArgumentNullException.ThrowIfNull(text);
		CodeScan scan = new(text);
		new CSharpLexer(scan, razor: false).Scan(0, text.Length, default, State.Start);
		return scan.Spans;
	}

	/// <summary>A Razor block from its opening brace to the matching closing one, markup inside it included.</summary>
	public int Block(int brace, int end)
	{
		_scan.Mark(brace, brace + 1, MokaCodeTokenKind.Punctuation);
		return Scan(brace + 1, end, new Stop(Close: '}', Islands: true), State.After('{'));
	}

	/// <summary>A bracketed expression from <paramref name="open" /> to the matching <paramref name="close" />.</summary>
	public int Brackets(int open, int end, char close)
	{
		_scan.Mark(open, open + 1, MokaCodeTokenKind.Punctuation);
		return Scan(open + 1, end, new Stop(Close: close), State.After(_text[open]));
	}

	/// <summary>The rest of a directive's line. With <paramref name="typeFirst" /> the first name on it is a type.</summary>
	public int Line(int pos, int end, bool typeFirst)
	{
		State state = State.Start;
		state.ExpectType = typeFirst;
		return Scan(pos, end, new Stop(LineEnd: true), state);
	}

	/// <summary>A directive attribute's value, up to its closing quote, or to a space or the tag's end when unquoted.</summary>
	public int AttributeValue(int pos, int end, char quote) =>
		Scan(pos, end, quote == '\0' ? new Stop(ValueEnd: true) : new Stop(Quote: quote), State.Start);

	/// <summary>
	///     A Razor control statement from its keyword: the header, the block and what may follow it (else, catch,
	///     finally, the while of a do).
	/// </summary>
	public int Statement(int keyword, int keywordEnd, int end)
	{
		_scan.Mark(keyword, keywordEnd, MokaCodeTokenKind.Keyword);
		ReadOnlySpan<char> statement = _text.AsSpan(keyword, keywordEnd - keyword);
		int pos = keywordEnd;
		bool afterHeader = false;
		while (pos < end)
		{
			int start = CodeText.SkipWhitespace(_text, pos, end);
			if (start >= end)
			{
				return start;
			}

			char c = _text[start];
			if (c == '(')
			{
				pos = Brackets(start, end, ')');
				afterHeader = true;
				continue;
			}

			if (c == '{')
			{
				pos = Block(start, end);
				int next = CodeText.SkipWhitespace(_text, pos, end);
				int wordEnd = CodeText.IdentifierEnd(_text, next, end);
				if (!Continues(statement, _text.AsSpan(next, wordEnd - next)))
				{
					return pos;
				}

				_scan.Mark(next, wordEnd, MokaCodeTokenKind.Keyword);
				pos = wordEnd;
				afterHeader = false;
				continue;
			}

			if (c == ';')
			{
				_scan.Mark(start, start + 1, MokaCodeTokenKind.Punctuation);
				return start + 1;
			}

			if (c == '<' && afterHeader && CanStartIsland(start, end))
			{
				return Markup!.Island(start, end);
			}

			int scanned = Scan(start, end, new Stop(BlockStart: true), State.Start);
			if (scanned == start)
			{
				return start;
			}

			pos = scanned;
		}

		return pos;
	}

	/// <summary>A Razor implicit expression from its first name: member access, calls and indexers, as Razor reads them.</summary>
	public int Implicit(int pos, int end)
	{
		while (true)
		{
			int wordEnd = CodeText.IdentifierEnd(_text, pos, end);
			ReadOnlySpan<char> word = _text.AsSpan(pos, wordEnd - pos);
			if (KeywordLookup.Contains(word))
			{
				_scan.Mark(pos, wordEnd, MokaCodeTokenKind.Keyword);
			}

			pos = wordEnd;
			int next = CodeText.SkipSpaces(_text, pos, end);
			if (!word.SequenceEqual("await") || next >= end || !CodeText.IsIdentifierStart(_text[next]))
			{
				break;
			}

			pos = next;
		}

		while (pos < end)
		{
			char c = _text[pos];
			char next = CodeText.At(_text, pos + 1, end);
			if (c is '(' or '[' && _scan.CanNest)
			{
				pos = Brackets(pos, end, c == '(' ? ')' : ']');
			}
			else if (c == '.' && CodeText.IsIdentifierStart(next))
			{
				_scan.Mark(pos, pos + 1, MokaCodeTokenKind.Punctuation);
				pos = MemberName(pos + 1, end);
			}
			else if (c == '?' && next == '.' && CodeText.IsIdentifierStart(CodeText.At(_text, pos + 2, end)))
			{
				_scan.Mark(pos, pos + 2, MokaCodeTokenKind.Punctuation);
				pos = MemberName(pos + 2, end);
			}
			else if ((c == '?' && next == '[') || (c == '!' && next is '.' or '['))
			{
				_scan.Mark(pos, pos + 1, MokaCodeTokenKind.Punctuation);
				pos++;
			}
			else
			{
				break;
			}
		}

		return pos;
	}

	private static bool Continues(ReadOnlySpan<char> statement, ReadOnlySpan<char> word) =>
		(statement.SequenceEqual("if") && word.SequenceEqual("else"))
		|| (statement.SequenceEqual("try") && (word.SequenceEqual("catch") || word.SequenceEqual("finally")))
		|| (statement.SequenceEqual("do") && word.SequenceEqual("while"));

	private int MemberName(int pos, int end)
	{
		int wordEnd = CodeText.IdentifierEnd(_text, pos, end);
		if (KeywordLookup.Contains(_text.AsSpan(pos, wordEnd - pos)))
		{
			_scan.Mark(pos, wordEnd, MokaCodeTokenKind.Keyword);
		}

		return wordEnd;
	}

	private int Scan(int pos, int end, Stop stop, State state)
	{
		_scan.Enter();
		int stopped = ScanCore(pos, end, stop, ref state);
		_scan.Leave();
		return stopped;
	}

	private int ScanCore(int pos, int end, Stop stop, ref State st)
	{
		while (pos < end)
		{
			char c = _text[pos];
			if (c == '\n' && stop.LineEnd)
			{
				return pos;
			}

			if (char.IsWhiteSpace(c))
			{
				if (stop.ValueEnd && st.Depth == 0)
				{
					return pos;
				}

				pos++;
				continue;
			}

			char next = CodeText.At(_text, pos + 1, end);
			if (c == '/' && next == '/')
			{
				int lineEnd = CodeText.LineEnd(_text, pos, end);
				_scan.Mark(pos, lineEnd, MokaCodeTokenKind.Comment);
				pos = lineEnd;
				continue;
			}

			if (c == '/' && next == '*')
			{
				int close = CodeText.After(_text, "*/", pos + 2, end);
				_scan.Mark(pos, close, MokaCodeTokenKind.Comment);
				pos = close;
				continue;
			}

			if (st.Depth == 0 && StopsAt(pos, end, c, next, stop))
			{
				return pos;
			}

			if (_razor && c == '@' && TryRazor(ref pos, end, next, stop, ref st))
			{
				continue;
			}

			if (c == '#' && CodeText.StartsLine(_text, pos))
			{
				int lineEnd = CodeText.LineEnd(_text, pos, end);
				_scan.Mark(pos, lineEnd, MokaCodeTokenKind.Directive);
				pos = lineEnd;
				continue;
			}

			if (c is '"' or '\'' or '$' or '@' && TryString(pos, end, out int afterString))
			{
				st.Token('"');
				pos = afterString;
				continue;
			}

			if (char.IsAsciiDigit(c) || (c == '.' && char.IsAsciiDigit(next)))
			{
				pos = Number(pos, end);
				st.Token('0');
				continue;
			}

			if (CodeText.IsIdentifierStart(c) || (c == '@' && CodeText.IsIdentifierStart(next)))
			{
				pos = Identifier(pos, end, ref st);
				continue;
			}

			if (c is '(' or '[' or '{')
			{
				Open(pos, c, ref st);
				pos++;
				continue;
			}

			if (c is ')' or ']' or '}')
			{
				if (st.Depth == 0)
				{
					if (c == stop.Close)
					{
						_scan.Mark(pos, pos + 1, MokaCodeTokenKind.Punctuation);
						return pos + 1;
					}

					// Someone else's closer: hand it back to whoever opened it.
					if (stop.Nested)
					{
						return pos;
					}
				}

				Close(pos, c, ref st);
				pos++;
				continue;
			}

			if (c == '<' && stop.Islands && IsStatementPosition(st.Last) && CanStartIsland(pos, end))
			{
				pos = Markup!.Island(pos, end);
				st.Token(';');
				continue;
			}

			if (IsOperator(c))
			{
				Operator(pos, c, next, ref st);
			}

			pos++;
		}

		return pos;
	}

	private bool StopsAt(int pos, int end, char c, char next, Stop stop)
	{
		if (stop.Quote != '\0' && c == stop.Quote)
		{
			return true;
		}

		if (stop.HoleBraces > 0)
		{
			if (c == '}' && CodeText.RunLength(_text, pos, end, '}') >= stop.HoleBraces)
			{
				return true;
			}

			// The format after a hole's value, as in {price:N2}. A double colon is global:: instead.
			if (c == ':' && next != ':' && (pos == 0 || _text[pos - 1] != ':'))
			{
				return true;
			}
		}

		return (stop.BlockStart && c is '{' or ';') || (stop.ValueEnd && (c == '>' || (c == '/' && next == '>')));
	}

	private bool TryRazor(ref int pos, int end, char next, Stop stop, ref State st)
	{
		if (next == '*')
		{
			int close = CodeText.After(_text, "*@", pos + 2, end);
			_scan.Mark(pos, close, MokaCodeTokenKind.Comment);
			pos = close;
			return true;
		}

		if (next == ':' && stop.Islands && Markup is not null && _scan.CanNest)
		{
			_scan.Mark(pos, pos + 2, MokaCodeTokenKind.Directive);
			pos = Markup.Line(pos + 2, end);
			st.Token(';');
			return true;
		}

		if (next == '<' && CanStartIsland(pos + 1, end))
		{
			_scan.Mark(pos, pos + 1, MokaCodeTokenKind.Directive);
			pos = Markup!.Island(pos + 1, end);
			st.Token('>');
			return true;
		}

		// In a Razor block, @if and @name are transitions, not verbatim identifiers.
		if (next == '(' || (stop.Islands && CodeText.IsIdentifierStart(next)))
		{
			_scan.Mark(pos, pos + 1, MokaCodeTokenKind.Directive);
			pos++;
			return true;
		}

		return false;
	}

	private bool CanStartIsland(int lessThan, int end) =>
		Markup is not null && _scan.CanNest && char.IsAsciiLetter(CodeText.At(_text, lessThan + 1, end));

	// Markup starts a statement in a Razor block; anywhere else a '<' compares.
	private static bool IsStatementPosition(char last) => last is '{' or '}' or ';' or ':';

	private static bool IsOperator(char c) => c is '+' or '-' or '*' or '/' or '%' or '=' or '<' or '>' or '!' or '&'
		or '|' or '^' or '~' or '?' or ':' or ';' or ',' or '.';

	private void Open(int pos, char c, ref State st)
	{
		bool attribute = c == '[' && (st.Last is '\0' or ';' or '{' or '}' || (st.Last == ']' && st.AfterAttribute));
		bool typeOf = c == '(' && st.TypeOfPending;
		if (c == '{')
		{
			st.Declaring = false;
			st.BaseList = false;
		}

		_scan.Mark(pos, pos + 1, MokaCodeTokenKind.Punctuation);
		st.Token(c);
		st.ExpectType = typeOf;
		if (attribute)
		{
			st.AttributeDepth = st.Depth;
			st.AttributeName = true;
		}

		st.Depth++;
	}

	private void Close(int pos, char c, ref State st)
	{
		_scan.Mark(pos, pos + 1, MokaCodeTokenKind.Punctuation);
		bool closesAttribute = false;
		if (st.Depth > 0)
		{
			st.Depth--;
			closesAttribute = c == ']' && st.AttributeDepth == st.Depth;
			if (closesAttribute)
			{
				st.AttributeDepth = -1;
				st.AttributeName = false;
			}
		}

		st.Token(c);
		st.AfterAttribute = closesAttribute;
	}

	private void Operator(int pos, char c, char next, ref State st)
	{
		_scan.Mark(pos, pos + 1, MokaCodeTokenKind.Punctuation);
		bool keepType = st.ExpectType && c == '.';
		switch (c)
		{
			case ';':
				st.Declaring = false;
				st.BaseList = false;
				break;
			case ',' when st.AttributeDepth >= 0 && st.Depth == st.AttributeDepth + 1:
				st.AttributeName = true;
				break;
			case ':' when st.Declaring && !st.BaseList:
				st.BaseList = true;
				st.BaseListDepth = st.Depth;
				break;
			case '=' when next == '>':
				st.Declaring = false;
				st.BaseList = false;
				break;
		}

		st.Token(c);
		st.ExpectType = keepType;
	}

	private int Identifier(int pos, int end, ref State st)
	{
		bool verbatim = _text[pos] == '@';
		int start = verbatim ? pos + 1 : pos;
		int wordEnd = CodeText.IdentifierEnd(_text, start, end);
		ReadOnlySpan<char> word = _text.AsSpan(start, wordEnd - start);
		char after = CodeText.At(_text, wordEnd, end);

		// An attribute's target, as in [assembly: X]. A double colon is an alias instead.
		if (st.AttributeName && after == ':' && CodeText.At(_text, wordEnd + 1, end) != ':')
		{
			_scan.Mark(start, wordEnd, MokaCodeTokenKind.Keyword);
			st.Token('a');
			st.AttributeName = true;
			return wordEnd;
		}

		if (!verbatim && KeywordLookup.Contains(word))
		{
			_scan.Mark(start, wordEnd, MokaCodeTokenKind.Keyword);
			Keyword(word, ref st);
			return wordEnd;
		}

		if (IsType(start, wordEnd, word, after, end, ref st))
		{
			_scan.Mark(start, wordEnd, MokaCodeTokenKind.Type);
		}

		st.Token('a');
		return wordEnd;
	}

	private static void Keyword(ReadOnlySpan<char> word, ref State st)
	{
		bool expectType = st.ExpectType && word.SequenceEqual("not");
		bool typeOf = word.SequenceEqual("typeof") || word.SequenceEqual("sizeof");
		if (word.SequenceEqual("new") || word.SequenceEqual("is") || word.SequenceEqual("as"))
		{
			expectType = true;
		}
		else if (word.SequenceEqual("where"))
		{
			st.Declaring = true;
			st.BaseList = false;
		}
		else if (!st.BaseList && (word.SequenceEqual("class") || word.SequenceEqual("struct") || word.SequenceEqual("interface")
			         || word.SequenceEqual("enum") || word.SequenceEqual("record")))
		{
			st.Declaring = true;
			expectType = true;
		}

		st.Token('a');
		st.ExpectType = expectType;
		st.TypeOfPending = typeOf;
	}

	private bool IsType(int start, int wordEnd, ReadOnlySpan<char> word, char after, int end, ref State st)
	{
		if (start < st.TypeArgumentsEnd)
		{
			return true;
		}

		bool type = false;
		if (st.AttributeName && after != '.')
		{
			st.AttributeName = false;
			type = true;
		}
		else if (st.ExpectType && after != '.')
		{
			st.ExpectType = false;
			type = true;
		}
		else if (st.BaseList && st.Depth == st.BaseListDepth && after != '.')
		{
			type = true;
		}

		if (after == '<')
		{
			int close = TypeArgumentsClose(wordEnd, end);
			if (close >= 0)
			{
				st.TypeArgumentsEnd = close;
				// A generic method call, Foo<T>(), names a method.
				return type || (char.IsUpper(word[0]) && NextSignificant(close + 1, end) != '(');
			}
		}

		return type || (char.IsUpper(word[0]) && DeclarationFollows(wordEnd, end));
	}

	// The '>' closing the type argument list that starts at lessThan, or -1 when it is a comparison.
	private int TypeArgumentsClose(int lessThan, int end)
	{
		int depth = 0;
		int limit = Math.Min(end, lessThan + LookaheadLimit);
		for (int i = lessThan; i < limit; i++)
		{
			char c = _text[i];
			if (c == '<')
			{
				depth++;
			}
			else if (c == '>')
			{
				if (--depth == 0)
				{
					return FollowsTypeArguments(i + 1, end) ? i : -1;
				}
			}
			else if (!CodeText.IsIdentifierPart(c) && c is not ('.' or ',' or '?' or '[' or ']' or '(' or ')' or '*' or ':'
				         or '@' or ' ' or '\t' or '\r' or '\n'))
			{
				return -1;
			}
		}

		return -1;
	}

	// What C# itself looks at after a '>' to read a list as type arguments, plus a name for "List<int> items".
	private bool FollowsTypeArguments(int pos, int end)
	{
		char c = NextSignificant(pos, end);
		return c == '\0' || CodeText.IsIdentifierStart(c) || c is '(' or ')' or ']' or '}' or ':' or ';' or ',' or '.'
			or '?' or '=' or '|' or '^' or '&' or '[' or '{' or '>';
	}

	private char NextSignificant(int pos, int end)
	{
		int limit = Math.Min(end, pos + LookaheadLimit);
		int i = CodeText.SkipWhitespace(_text, pos, limit);
		return i < limit ? _text[i] : '\0';
	}

	// "Foo bar", "Foo? bar", "Foo[] bar": a name after the identifier declares something of its type.
	private bool DeclarationFollows(int pos, int end)
	{
		int limit = Math.Min(end, pos + LookaheadLimit);
		int i = pos;
		while (i < limit)
		{
			char c = _text[i];
			if (c is '?' or '*')
			{
				i++;
			}
			else if (c == '[')
			{
				int j = i + 1;
				while (j < limit && _text[j] is ',' or ' ')
				{
					j++;
				}

				if (j >= limit || _text[j] != ']')
				{
					return false;
				}

				i = j + 1;
			}
			else
			{
				break;
			}
		}

		int name = CodeText.SkipSpaces(_text, i, limit);
		if (name == i || name >= limit)
		{
			return false;
		}

		if (_text[name] == '@')
		{
			name++;
		}

		if (name >= limit || !CodeText.IsIdentifierStart(_text[name]))
		{
			return false;
		}

		int nameEnd = CodeText.IdentifierEnd(_text, name, end);
		return !NotDeclaredByLookup.Contains(_text.AsSpan(name, nameEnd - name));
	}

	private int Number(int pos, int end)
	{
		int i = pos;
		char marker = (char)(CodeText.At(_text, i + 1, end) | 0x20);
		if (_text[i] == '0' && marker == 'x')
		{
			i += 2;
			while (i < end && (char.IsAsciiHexDigit(_text[i]) || _text[i] == '_'))
			{
				i++;
			}
		}
		else if (_text[i] == '0' && marker == 'b')
		{
			i += 2;
			while (i < end && _text[i] is '0' or '1' or '_')
			{
				i++;
			}
		}
		else
		{
			i = Digits(i, end);
			if (CodeText.At(_text, i, end) == '.' && char.IsAsciiDigit(CodeText.At(_text, i + 1, end)))
			{
				i = Digits(i + 1, end);
			}

			if ((CodeText.At(_text, i, end) | 0x20) == 'e')
			{
				int exponent = i + 1;
				if (CodeText.At(_text, exponent, end) is '+' or '-')
				{
					exponent++;
				}

				if (char.IsAsciiDigit(CodeText.At(_text, exponent, end)))
				{
					i = Digits(exponent, end);
				}
			}
		}

		while (i < end && _text[i] is 'u' or 'U' or 'l' or 'L' or 'f' or 'F' or 'd' or 'D' or 'm' or 'M')
		{
			i++;
		}

		_scan.Mark(pos, i, MokaCodeTokenKind.Number);
		return i;
	}

	private int Digits(int pos, int end)
	{
		while (pos < end && (char.IsAsciiDigit(_text[pos]) || _text[pos] == '_'))
		{
			pos++;
		}

		return pos;
	}

	private bool TryString(int pos, int end, out int after)
	{
		after = pos;
		char c = _text[pos];
		if (c == '\'')
		{
			after = CharLiteral(pos, end);
			return after > pos;
		}

		int i = pos;
		bool verbatim = false;
		if (c == '@')
		{
			verbatim = true;
			i++;
		}

		int dollars = CodeText.RunLength(_text, i, end, '$');
		i += dollars;
		if (!verbatim && dollars > 0 && CodeText.At(_text, i, end) == '@')
		{
			verbatim = true;
			i++;
		}

		if (CodeText.At(_text, i, end) != '"')
		{
			return false;
		}

		int quotes = CodeText.RunLength(_text, i, end, '"');
		after = !verbatim && quotes >= 3
			? RawString(pos, i, quotes, dollars, end)
			: QuotedString(pos, i + 1, verbatim, dollars, end);
		return true;
	}

	private int CharLiteral(int pos, int end)
	{
		int i = pos + 1 + (CodeText.At(_text, pos + 1, end) == '\\' ? 2 : 1);
		int limit = Math.Min(end, pos + 12);
		while (i < limit && _text[i] is not ('\'' or '\n'))
		{
			i++;
		}

		if (i >= limit || _text[i] != '\'')
		{
			return pos;
		}

		_scan.Mark(pos, i + 1, MokaCodeTokenKind.String);
		return i + 1;
	}

	// A regular or verbatim string, interpolated when it has dollars. A regular one that isn't closed ends with its line.
	private int QuotedString(int pos, int content, bool verbatim, int dollars, int end)
	{
		int segment = pos;
		int i = content;
		while (i < end)
		{
			char c = _text[i];
			if (c == '\n' && !verbatim)
			{
				break;
			}

			if (c == '\\' && !verbatim)
			{
				i += 2;
				continue;
			}

			if (c == '"')
			{
				if (verbatim && CodeText.At(_text, i + 1, end) == '"')
				{
					i += 2;
					continue;
				}

				_scan.Mark(segment, i + 1, MokaCodeTokenKind.String);
				return i + 1;
			}

			if (dollars > 0 && c is '{' or '}' && CodeText.At(_text, i + 1, end) == c)
			{
				i += 2;
				continue;
			}

			if (dollars > 0 && c == '{' && _scan.CanNest)
			{
				_scan.Mark(segment, i, MokaCodeTokenKind.String);
				i = Hole(i, 1, end);
				segment = i;
				continue;
			}

			i++;
		}

		i = Math.Min(i, end);
		_scan.Mark(segment, i, MokaCodeTokenKind.String);
		return i;
	}

	// """raw""": closed by as many quotes as opened it, and with dollars, holes opened by as many braces as dollars.
	private int RawString(int pos, int open, int quotes, int dollars, int end)
	{
		int segment = pos;
		int i = open + quotes;
		while (i < end)
		{
			char c = _text[i];
			if (c == '"')
			{
				int run = CodeText.RunLength(_text, i, end, '"');
				if (run >= quotes)
				{
					_scan.Mark(segment, i + run, MokaCodeTokenKind.String);
					return i + run;
				}

				i += run;
				continue;
			}

			if (c == '{' && dollars > 0)
			{
				int run = CodeText.RunLength(_text, i, end, '{');
				if (run >= dollars && _scan.CanNest)
				{
					int hole = i + run - dollars;
					_scan.Mark(segment, hole, MokaCodeTokenKind.String);
					i = Hole(hole, dollars, end);
					segment = i;
					continue;
				}

				i += run;
				continue;
			}

			i++;
		}

		_scan.Mark(segment, end, MokaCodeTokenKind.String);
		return end;
	}

	// An interpolation hole from its opening braces: the value as C#, an optional format, the closing braces. Returns
	// where the string goes on.
	private int Hole(int brace, int braces, int end)
	{
		_scan.Mark(brace, brace + braces, MokaCodeTokenKind.Punctuation);
		int pos = Scan(brace + braces, end, new Stop(HoleBraces: braces), State.Start);
		if (pos < end && _text[pos] == ':')
		{
			int format = pos;
			while (pos < end && _text[pos] is not ('}' or '"' or '\n'))
			{
				pos++;
			}

			_scan.Mark(format, pos, MokaCodeTokenKind.String);
		}

		if (pos < end && CodeText.RunLength(_text, pos, end, '}') >= braces)
		{
			_scan.Mark(pos, pos + braces, MokaCodeTokenKind.Punctuation);
			return pos + braces;
		}

		return pos;
	}

	/// <summary>Where a scan stops, besides the end of the text. Nothing set scans to the end.</summary>
	private readonly record struct Stop(
		char Close = '\0',
		char Quote = '\0',
		int HoleBraces = 0,
		bool LineEnd = false,
		bool BlockStart = false,
		bool ValueEnd = false,
		bool Islands = false)
	{
		/// <summary>Whether this scan belongs to something that opened a bracket, so a closer it didn't open goes back.</summary>
		public bool Nested => Close != '\0' || Quote != '\0' || HoleBraces > 0 || BlockStart || ValueEnd;
	}

	/// <summary>What one scan remembers about the tokens before, for the guesses at types and attributes.</summary>
	private struct State
	{
		public char Last;
		public int Depth;
		public bool ExpectType;
		public bool TypeOfPending;
		public bool Declaring;
		public bool BaseList;
		public int BaseListDepth;
		public int AttributeDepth;
		public bool AttributeName;
		public bool AfterAttribute;
		public int TypeArgumentsEnd;

		public static State Start => new() { AttributeDepth = -1, TypeArgumentsEnd = -1 };

		public static State After(char opener)
		{
			State state = Start;
			state.Last = opener;
			return state;
		}

		/// <summary>Records a token that isn't a name, a keyword or a bracket, which ends every pending guess but a type.</summary>
		public void Token(char last)
		{
			Last = last;
			AfterAttribute = false;
			TypeOfPending = false;
		}
	}
}
