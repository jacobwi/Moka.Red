using System.Collections.Frozen;

namespace Moka.Red.Core.Code;

/// <summary>Which markup a <see cref="MarkupLexer" /> reads.</summary>
internal enum MarkupFlavor
{
	Html,
	Xml,
	Razor
}

/// <summary>
///     Colors HTML, XML and Razor: tags, attributes, comments and entities, plus in Razor the transitions to C# (@code,
///     @if and the other blocks, @expressions, directives, comments) and the markup inside C# blocks, which goes from a
///     tag at the start of a statement to the end tag that closes it.
/// </summary>
internal sealed class MarkupLexer
{
	private static readonly FrozenSet<string> VoidElements = FrozenSet.Create(StringComparer.OrdinalIgnoreCase,
		"area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr");

	private static readonly FrozenSet<string> Directives = FrozenSet.Create(StringComparer.Ordinal,
		"page", "using", "inject", "inherits", "namespace", "implements", "attribute", "layout", "typeparam", "model",
		"rendermode", "preservewhitespace", "section", "addTagHelper", "removeTagHelper", "tagHelperPrefix");

	// Directives whose first name is a type: @inherits LayoutComponentBase, @inject IFoo Foo.
	private static readonly FrozenSet<string> TypeDirectives = FrozenSet.Create(StringComparer.Ordinal,
		"inherits", "implements", "layout", "inject", "model", "typeparam");

	private static readonly FrozenSet<string> Statements = FrozenSet.Create(StringComparer.Ordinal,
		"if", "for", "foreach", "while", "switch", "lock", "try", "do", "using");

	private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> VoidLookup =
		VoidElements.GetAlternateLookup<ReadOnlySpan<char>>();

	private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> DirectiveLookup =
		Directives.GetAlternateLookup<ReadOnlySpan<char>>();

	private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> TypeDirectiveLookup =
		TypeDirectives.GetAlternateLookup<ReadOnlySpan<char>>();

	private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> StatementLookup =
		Statements.GetAlternateLookup<ReadOnlySpan<char>>();

	private readonly CodeScan _scan;
	private readonly string _text;
	private readonly MarkupFlavor _flavor;
	private readonly CSharpLexer? _csharp;
	private readonly CssLexer _css;

	private MarkupLexer(CodeScan scan, MarkupFlavor flavor)
	{
		_scan = scan;
		_text = scan.Text;
		_flavor = flavor;
		_css = new CssLexer(scan);
		if (flavor == MarkupFlavor.Razor)
		{
			_csharp = new CSharpLexer(scan, razor: true) { Markup = this };
		}
	}

	private enum Mode
	{
		Document,
		Island,
		Line
	}

	public static List<MokaCodeSpan> Html(string text) => Highlight(text, MarkupFlavor.Html);

	public static List<MokaCodeSpan> Xml(string text) => Highlight(text, MarkupFlavor.Xml);

	public static List<MokaCodeSpan> Razor(string text) => Highlight(text, MarkupFlavor.Razor);

	/// <summary>One element inside a Razor code block, from its start tag to the end tag that closes it.</summary>
	public int Island(int pos, int end) => Scan(pos, end, Mode.Island);

	/// <summary>The markup after @: in a Razor code block, up to the end of its line.</summary>
	public int Line(int pos, int end) => Scan(pos, end, Mode.Line);

	private static List<MokaCodeSpan> Highlight(string text, MarkupFlavor flavor)
	{
		ArgumentNullException.ThrowIfNull(text);
		CodeScan scan = new(text);
		new MarkupLexer(scan, flavor).Scan(0, text.Length, Mode.Document);
		return scan.Spans;
	}

	private static bool IsNameStart(char c) => char.IsAsciiLetter(c) || c == '_';

	private static bool IsNamePart(char c) => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or ':' or '.';

	private int Scan(int pos, int end, Mode mode)
	{
		_scan.Enter();
		int stopped = ScanCore(pos, end, mode);
		_scan.Leave();
		return stopped;
	}

	private int ScanCore(int pos, int end, Mode mode)
	{
		int depth = 0;
		while (pos < end)
		{
			char c = _text[pos];
			if (c == '\n' && mode == Mode.Line)
			{
				return pos;
			}

			if (c == '<')
			{
				char next = CodeText.At(_text, pos + 1, end);
				if (TryConstruct(pos, end, next, out int afterConstruct))
				{
					pos = afterConstruct;
				}
				else if (next == '/' && IsNameStart(CodeText.At(_text, pos + 2, end)))
				{
					pos = EndTag(pos, end, out bool closesVoid);
					if (mode == Mode.Island && !closesVoid && --depth <= 0)
					{
						return pos;
					}
				}
				else if (IsNameStart(next))
				{
					pos = StartTag(pos, end, out int nameStart, out int nameEnd, out bool opens);
					if (opens && _flavor != MarkupFlavor.Xml)
					{
						pos = RawText(pos, end, nameStart, nameEnd);
					}

					if (mode == Mode.Island && (opens ? ++depth : depth) == 0)
					{
						return pos;
					}
				}
				else
				{
					pos++;
				}
			}
			else if (c == '@' && _flavor == MarkupFlavor.Razor)
			{
				pos = Transition(pos, end, inValue: false);
			}
			else if (c == '&')
			{
				pos = Entity(pos, end);
			}
			else
			{
				pos++;
			}
		}

		return pos;
	}

	// Comments, CDATA, a doctype and processing instructions.
	private bool TryConstruct(int pos, int end, char next, out int after)
	{
		after = pos;
		if (next != '!' && !(next == '?' && _flavor == MarkupFlavor.Xml))
		{
			return false;
		}

		MokaCodeTokenKind kind;
		if (CodeText.StartsWith(_text, pos, end, "<!--"))
		{
			after = CodeText.After(_text, "-->", pos + 4, end);
			kind = MokaCodeTokenKind.Comment;
		}
		else if (_flavor == MarkupFlavor.Xml && CodeText.StartsWith(_text, pos, end, "<![CDATA["))
		{
			after = CodeText.After(_text, "]]>", pos + 9, end);
			kind = MokaCodeTokenKind.String;
		}
		else if (next == '?')
		{
			after = CodeText.After(_text, "?>", pos + 2, end);
			kind = MokaCodeTokenKind.Directive;
		}
		else if (char.IsAsciiLetter(CodeText.At(_text, pos + 2, end)))
		{
			after = CodeText.After(_text, ">", pos + 2, end);
			kind = MokaCodeTokenKind.Directive;
		}
		else
		{
			return false;
		}

		_scan.Mark(pos, after, kind);
		return true;
	}

	private int StartTag(int pos, int end, out int nameStart, out int nameEnd, out bool opens)
	{
		_scan.Mark(pos, pos + 1, MokaCodeTokenKind.Punctuation);
		nameStart = pos + 1;
		nameEnd = NameEnd(nameStart, end);
		_scan.Mark(nameStart, nameEnd, NameKind(nameStart, nameEnd));
		opens = !IsVoid(nameStart, nameEnd);
		int i = nameEnd;
		while (i < end)
		{
			char c = _text[i];
			char next = CodeText.At(_text, i + 1, end);
			if (char.IsWhiteSpace(c))
			{
				i++;
			}
			else if (c == '>')
			{
				_scan.Mark(i, i + 1, MokaCodeTokenKind.Punctuation);
				return i + 1;
			}
			else if (c == '/' && next == '>')
			{
				_scan.Mark(i, i + 2, MokaCodeTokenKind.Punctuation);
				opens = false;
				return i + 2;
			}
			else if (c == '<')
			{
				// A tag left open: the next one starts here.
				return i;
			}
			else if (c == '@' && next == '*' && _flavor == MarkupFlavor.Razor)
			{
				int close = CodeText.After(_text, "*@", i + 2, end);
				_scan.Mark(i, close, MokaCodeTokenKind.Comment);
				i = close;
			}
			else
			{
				i = Attribute(i, end);
			}
		}

		return i;
	}

	private int Attribute(int pos, int end)
	{
		int i = pos;
		while (i < end && !char.IsWhiteSpace(_text[i]) && _text[i] is not ('=' or '>' or '<' or '"' or '\'')
		       && !(_text[i] == '/' && CodeText.At(_text, i + 1, end) == '>'))
		{
			i++;
		}

		if (i == pos)
		{
			return pos + 1;
		}

		bool directive = _flavor == MarkupFlavor.Razor && _text[pos] == '@';
		_scan.Mark(pos, i, directive ? MokaCodeTokenKind.Directive : MokaCodeTokenKind.Attribute);
		int equals = CodeText.SkipWhitespace(_text, i, end);
		if (CodeText.At(_text, equals, end) != '=')
		{
			return i;
		}

		_scan.Mark(equals, equals + 1, MokaCodeTokenKind.Punctuation);
		int value = CodeText.SkipWhitespace(_text, equals + 1, end);
		if (value >= end || _text[value] == '>')
		{
			return value;
		}

		return directive && _csharp is not null && _scan.CanNest ? CSharpValue(value, end) : MarkupValue(value, end);
	}

	// A directive attribute's value is C#, as in @onclick="Save", with an optional @ in front.
	private int CSharpValue(int pos, int end)
	{
		char quote = _text[pos] is '"' or '\'' ? _text[pos] : '\0';
		int i = pos;
		if (quote != '\0')
		{
			_scan.Mark(i, i + 1, MokaCodeTokenKind.String);
			i++;
		}

		if (CodeText.At(_text, i, end) == '@')
		{
			_scan.Mark(i, i + 1, MokaCodeTokenKind.Directive);
			i++;
		}

		i = _csharp!.AttributeValue(i, end, quote);
		if (quote != '\0' && CodeText.At(_text, i, end) == quote)
		{
			_scan.Mark(i, i + 1, MokaCodeTokenKind.String);
			i++;
		}

		return i;
	}

	// A plain value is a string, and in Razor it may hold @expressions.
	private int MarkupValue(int pos, int end)
	{
		char quote = _text[pos] is '"' or '\'' ? _text[pos] : '\0';
		int segment = pos;
		int i = quote == '\0' ? pos : pos + 1;
		while (i < end)
		{
			char c = _text[i];
			if (quote == '\0' ? char.IsWhiteSpace(c) || c == '>' || (c == '/' && CodeText.At(_text, i + 1, end) == '>') : c == quote)
			{
				break;
			}

			if (c == '@' && _flavor == MarkupFlavor.Razor && StartsTransition(i, end))
			{
				_scan.Mark(segment, i, MokaCodeTokenKind.String);
				i = Transition(i, end, inValue: true);
				segment = i;
				continue;
			}

			i++;
		}

		if (quote != '\0' && i < end)
		{
			i++;
		}

		_scan.Mark(segment, i, MokaCodeTokenKind.String);
		return i;
	}

	private int EndTag(int pos, int end, out bool closesVoid)
	{
		_scan.Mark(pos, pos + 2, MokaCodeTokenKind.Punctuation);
		int nameEnd = NameEnd(pos + 2, end);
		_scan.Mark(pos + 2, nameEnd, NameKind(pos + 2, nameEnd));
		closesVoid = IsVoid(pos + 2, nameEnd);
		int i = nameEnd;
		while (i < end && _text[i] is not ('>' or '<'))
		{
			i++;
		}

		if (i < end && _text[i] == '>')
		{
			_scan.Mark(i, i + 1, MokaCodeTokenKind.Punctuation);
			i++;
		}

		return i;
	}

	private int NameEnd(int pos, int end)
	{
		while (pos < end && IsNamePart(_text[pos]))
		{
			pos++;
		}

		return pos;
	}

	// Razor's <text> wrapper renders no element, so it reads as Razor rather than as a tag.
	private MokaCodeTokenKind NameKind(int start, int end) =>
		_flavor == MarkupFlavor.Razor && _text.AsSpan(start, end - start).SequenceEqual("text")
			? MokaCodeTokenKind.Directive
			: MokaCodeTokenKind.Tag;

	// A Razor component is named in PascalCase, so only a lowercase name is a void HTML element there.
	private bool IsVoid(int start, int end)
	{
		ReadOnlySpan<char> name = _text.AsSpan(start, end - start);
		return _flavor switch
		{
			MarkupFlavor.Html => VoidLookup.Contains(name),
			MarkupFlavor.Razor => VoidLookup.Contains(name) && name.Length > 0 && char.IsAsciiLetterLower(name[0]),
			_ => false
		};
	}

	// A script's or a style's content isn't markup. A style's is CSS.
	private int RawText(int pos, int end, int nameStart, int nameEnd)
	{
		ReadOnlySpan<char> name = _text.AsSpan(nameStart, nameEnd - nameStart);
		bool style = name.Equals("style", StringComparison.OrdinalIgnoreCase);
		if (!style && !name.Equals("script", StringComparison.OrdinalIgnoreCase))
		{
			return pos;
		}

		int close = _text.IndexOf(style ? "</style" : "</script", pos, end - pos, StringComparison.OrdinalIgnoreCase);
		if (close < 0)
		{
			close = end;
		}

		if (style)
		{
			_css.Scan(pos, close);
		}

		return close;
	}

	private int Entity(int pos, int end)
	{
		int limit = Math.Min(end, pos + 32);
		int i = pos + 1;
		if (CodeText.At(_text, i, limit) == '#')
		{
			i++;
		}

		int name = i;
		while (i < limit && char.IsAsciiLetterOrDigit(_text[i]))
		{
			i++;
		}

		if (i == name || CodeText.At(_text, i, limit) != ';')
		{
			return pos + 1;
		}

		_scan.Mark(pos, i + 1, MokaCodeTokenKind.Constant);
		return i + 1;
	}

	// Whether an @ in a value moves to C#. An address, an escaped @@ or an @ before anything else stays in the string.
	private bool StartsTransition(int pos, int end)
	{
		char next = CodeText.At(_text, pos + 1, end);
		return (pos == 0 || !char.IsLetterOrDigit(_text[pos - 1])) && (next is '(' or '*' || CodeText.IsIdentifierStart(next));
	}

	// A Razor @ in text or in an attribute value. Always moves past the @.
	private int Transition(int pos, int end, bool inValue)
	{
		char next = CodeText.At(_text, pos + 1, end);
		if (next == '@')
		{
			return pos + 2;
		}

		// An address such as me@example.com stays text, as in Razor.
		if (pos > 0 && char.IsLetterOrDigit(_text[pos - 1]))
		{
			return pos + 1;
		}

		if (next == '*')
		{
			int close = CodeText.After(_text, "*@", pos + 2, end);
			_scan.Mark(pos, close, MokaCodeTokenKind.Comment);
			return close;
		}

		if (_csharp is null || !_scan.CanNest)
		{
			return pos + 1;
		}

		if (next == '(')
		{
			_scan.Mark(pos, pos + 1, MokaCodeTokenKind.Directive);
			return _csharp.Brackets(pos + 1, end, ')');
		}

		if (next == '{' && !inValue)
		{
			_scan.Mark(pos, pos + 1, MokaCodeTokenKind.Directive);
			return _csharp.Block(pos + 1, end);
		}

		if (!CodeText.IsIdentifierStart(next))
		{
			return pos + 1;
		}

		int wordEnd = CodeText.IdentifierEnd(_text, pos + 1, end);
		ReadOnlySpan<char> word = _text.AsSpan(pos + 1, wordEnd - pos - 1);
		if (!inValue)
		{
			if (word.SequenceEqual("code") || word.SequenceEqual("functions"))
			{
				_scan.Mark(pos, wordEnd, MokaCodeTokenKind.Directive);
				int brace = CodeText.SkipWhitespace(_text, wordEnd, end);
				return CodeText.At(_text, brace, end) == '{' ? _csharp.Block(brace, end) : wordEnd;
			}

			// @using (...) is a statement; @using Some.Namespace is a directive.
			bool usingStatement = word.SequenceEqual("using") && CodeText.At(_text, CodeText.SkipSpaces(_text, wordEnd, end), end) == '(';
			if (StatementLookup.Contains(word) && (usingStatement || !word.SequenceEqual("using")))
			{
				_scan.Mark(pos, pos + 1, MokaCodeTokenKind.Directive);
				return _csharp.Statement(pos + 1, wordEnd, end);
			}

			if (DirectiveLookup.Contains(word))
			{
				_scan.Mark(pos, wordEnd, MokaCodeTokenKind.Directive);
				return _csharp.Line(wordEnd, end, TypeDirectiveLookup.Contains(word));
			}
		}

		_scan.Mark(pos, pos + 1, MokaCodeTokenKind.Directive);
		return _csharp.Implicit(pos + 1, end);
	}
}
