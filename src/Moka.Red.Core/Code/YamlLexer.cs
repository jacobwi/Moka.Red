using System.Buffers;

namespace Moka.Red.Core.Code;

/// <summary>
///     Colors YAML: comments, keys, plain scalars with the core schema's null, booleans and numbers, quoted strings,
///     anchors, aliases, tags, directives, document markers, the sequence and mapping indicators, flow collections, and
///     block scalars, whose more-indented lines are one string.
/// </summary>
internal sealed class YamlLexer
{
	private static readonly SearchValues<char> HexDigits = SearchValues.Create("0123456789abcdefABCDEF");

	private readonly CodeScan _scan;
	private readonly string _text;
	private readonly int _end;

	// Where the line being read starts, and how deep in [ ] and { } the reader is, which carries over lines.
	private int _lineStart;
	private int _flowDepth;

	// On the line being read: the column of the key or dash a block scalar belongs to, and whether a ? came before.
	private int _owner = -1;
	private bool _explicitKey;

	private YamlLexer(CodeScan scan)
	{
		_scan = scan;
		_text = scan.Text;
		_end = scan.Text.Length;
	}

	public static List<MokaCodeSpan> Highlight(string text)
	{
		ArgumentNullException.ThrowIfNull(text);
		CodeScan scan = new(text);
		new YamlLexer(scan).Scan();
		return scan.Spans;
	}

	private static bool IsBlank(char c) => c is ' ' or '\t' or '\r' or '\n';

	private static bool IsFlowIndicator(char c) => c is ',' or '[' or ']' or '{' or '}';

	private bool BlankAt(int pos) => pos >= _end || IsBlank(_text[pos]);

	// What may follow a : that ends a key: a blank, and in a flow collection the collection's own punctuation.
	private bool EndsIndicator(int pos) => BlankAt(pos) || (_flowDepth > 0 && IsFlowIndicator(_text[pos]));

	private bool DocumentMarkerAt(int pos) =>
		pos + 3 <= _end && (CodeText.StartsWith(_text, pos, _end, "---") || CodeText.StartsWith(_text, pos, _end, "..."))
		                && BlankAt(pos + 3);

	private void Scan()
	{
		int pos = 0;
		while (pos < _end)
		{
			char c = _text[pos];
			if (c == '\n')
			{
				pos++;
				_lineStart = pos;
				_owner = -1;
				_explicitKey = false;
			}
			else if (pos == _lineStart && DocumentMarkerAt(pos))
			{
				_scan.Mark(pos, pos + 3, MokaCodeTokenKind.Punctuation);
				_flowDepth = 0;
				pos += 3;
			}
			else if (pos == _lineStart && c == '%')
			{
				pos = Directive(pos);
			}
			else if (IsBlank(c))
			{
				pos++;
			}
			else if (c == '#' && (pos == _lineStart || IsBlank(_text[pos - 1])))
			{
				int lineEnd = CodeText.LineEnd(_text, pos, _end);
				_scan.Mark(pos, lineEnd, MokaCodeTokenKind.Comment);
				pos = lineEnd;
			}
			else
			{
				pos = Node(pos, c);
			}
		}
	}

	private int Node(int pos, char c)
	{
		char next = CodeText.At(_text, pos + 1, _end);
		switch (c)
		{
			case '-' when _flowDepth == 0 && BlankAt(pos + 1):
				_owner = pos - _lineStart;
				return Indicator(pos);
			case '?' when BlankAt(pos + 1):
				_owner = pos - _lineStart;
				_explicitKey = true;
				return Indicator(pos);
			case ':' when EndsIndicator(pos + 1):
				return Indicator(pos);
			case '[' or '{':
				_flowDepth++;
				return Indicator(pos);
			case ']' or '}':
				_flowDepth = Math.Max(0, _flowDepth - 1);
				return Indicator(pos);
			case ',' when _flowDepth > 0:
				return Indicator(pos);
			case '&' or '*' when !IsBlank(next) && !IsFlowIndicator(next) && next != '\0':
				return Name(pos);
			case '!':
				return Tag(pos);
			case '|' or '>' when _flowDepth == 0:
				return BlockScalar(pos);
			case '"' or '\'':
				return Quoted(pos, c);
			default:
				return Plain(pos);
		}
	}

	private int Indicator(int pos)
	{
		_scan.Mark(pos, pos + 1, MokaCodeTokenKind.Punctuation);
		return pos + 1;
	}

	// %YAML 1.2 or %TAG ! tag:example.com,2000: up to a comment.
	private int Directive(int pos)
	{
		int lineEnd = CodeText.LineEnd(_text, pos, _end);
		int end = pos + 1;
		int contentEnd = end;
		while (end < lineEnd && !(_text[end] == '#' && IsBlank(_text[end - 1])))
		{
			if (!IsBlank(_text[end]))
			{
				contentEnd = end + 1;
			}

			end++;
		}

		_scan.Mark(pos, contentEnd, MokaCodeTokenKind.Directive);
		return end;
	}

	// &anchor and *alias.
	private int Name(int pos)
	{
		int end = pos + 1;
		while (end < _end && !IsBlank(_text[end]) && !IsFlowIndicator(_text[end]))
		{
			end++;
		}

		_scan.Mark(pos, end, MokaCodeTokenKind.Directive);
		return end;
	}

	// !local, !!str, !e!tag or !<tag:yaml.org,2002:str>.
	private int Tag(int pos)
	{
		int end = pos + 1;
		if (CodeText.At(_text, end, _end) == '<')
		{
			while (end < _end && _text[end] != '>' && _text[end] != '\n')
			{
				end++;
			}

			end = end < _end && _text[end] == '>' ? end + 1 : end;
		}
		else
		{
			while (end < _end && !IsBlank(_text[end]) && !IsFlowIndicator(_text[end]))
			{
				end++;
			}
		}

		_scan.Mark(pos, end, MokaCodeTokenKind.Type);
		return end;
	}

	// A plain scalar runs to the end of its line, a comment, a : that makes it a key, or in a flow collection the
	// collection's punctuation.
	private int Plain(int pos)
	{
		int end = pos;
		int contentEnd = pos + 1;
		bool key = false;
		while (end < _end && _text[end] != '\n')
		{
			char c = _text[end];
			if (c == ':' && end > pos && EndsIndicator(end + 1))
			{
				key = true;
				break;
			}

			if ((c == '#' && end > pos && IsBlank(_text[end - 1])) || (_flowDepth > 0 && IsFlowIndicator(c) && end > pos))
			{
				break;
			}

			if (!IsBlank(c))
			{
				contentEnd = end + 1;
			}

			end++;
		}

		if (key)
		{
			_owner = pos - _lineStart;
			_explicitKey = false;
			_scan.Mark(pos, contentEnd, MokaCodeTokenKind.Key);
			_scan.Mark(end, end + 1, MokaCodeTokenKind.Punctuation);
			return end + 1;
		}

		_scan.Mark(pos, contentEnd, _explicitKey ? MokaCodeTokenKind.Key : ScalarKind(_text.AsSpan(pos, contentEnd - pos)));
		_explicitKey = false;
		return Math.Max(end, pos + 1);
	}

	// A quoted scalar may run over lines. One left open stops before a document marker rather than taking the rest.
	private int Quoted(int pos, char quote)
	{
		int end = pos + 1;
		int lastBreak = -1;
		bool closed = false;
		while (end < _end)
		{
			char c = _text[end];
			if (c == '\n')
			{
				if (DocumentMarkerAt(end + 1))
				{
					break;
				}

				lastBreak = end;
				end++;
			}
			else if (quote == '"' && c == '\\' && end + 1 < _end)
			{
				if (_text[end + 1] == '\n')
				{
					lastBreak = end + 1;
				}

				end += 2;
			}
			else if (c == quote && quote == '\'' && CodeText.At(_text, end + 1, _end) == '\'')
			{
				end += 2;
			}
			else if (c == quote)
			{
				end++;
				closed = true;
				break;
			}
			else
			{
				end++;
			}
		}

		end = Math.Min(end, _end);
		if (lastBreak >= 0)
		{
			_lineStart = lastBreak + 1;
			_owner = -1;
		}

		int colon = CodeText.SkipSpaces(_text, end, _end);
		bool key = closed && lastBreak < 0 && CodeText.At(_text, colon, _end) == ':'
		           && (EndsIndicator(colon + 1) || (_flowDepth > 0 && colon == end));
		if (key)
		{
			_owner = pos - _lineStart;
			_explicitKey = false;
			_scan.Mark(pos, end, MokaCodeTokenKind.Key);
			_scan.Mark(colon, colon + 1, MokaCodeTokenKind.Punctuation);
			return colon + 1;
		}

		_scan.Mark(pos, end, _explicitKey && lastBreak < 0 ? MokaCodeTokenKind.Key : MokaCodeTokenKind.String);
		_explicitKey = false;
		return end;
	}

	// | or >, with an indentation indicator and a chomping one in either order, then nothing but a comment. The lines
	// after it that are indented past the key or dash it belongs to are its content, blank lines among them included.
	private int BlockScalar(int pos)
	{
		int header = pos + 1;
		int indent = 0;
		bool chomping = false;
		for (int i = 0; i < 2 && header < _end; i++)
		{
			char c = _text[header];
			if (indent == 0 && c is >= '1' and <= '9')
			{
				indent = c - '0';
			}
			else if (!chomping && c is '+' or '-')
			{
				chomping = true;
			}
			else
			{
				break;
			}

			header++;
		}

		int after = CodeText.SkipSpaces(_text, header, _end);
		char following = CodeText.At(_text, after, _end);
		if (following is not ('\0' or '\r' or '\n') && !(following == '#' && after > header))
		{
			return Plain(pos);
		}

		_scan.Mark(pos, header, MokaCodeTokenKind.Punctuation);
		int headerEnd = CodeText.LineEnd(_text, header, _end);
		if (following == '#')
		{
			_scan.Mark(after, headerEnd, MokaCodeTokenKind.Comment);
		}

		int content = indent > 0 ? Math.Max(0, _owner + indent) : FirstContentIndent(headerEnd + 1);
		if (content <= _owner)
		{
			return headerEnd;
		}

		int bodyStart = -1;
		int bodyEnd = headerEnd;
		int line = headerEnd + 1;
		while (line < _end)
		{
			int lineEnd = CodeText.LineEnd(_text, line, _end);
			int spaces = CodeText.RunLength(_text, line, lineEnd, ' ');
			bool blank = CodeText.SkipWhitespace(_text, line, lineEnd) == lineEnd;
			if (!blank && (spaces < content || DocumentMarkerAt(line)))
			{
				break;
			}

			if (!blank)
			{
				bodyStart = bodyStart < 0 ? line + content : bodyStart;
				bodyEnd = lineEnd;
			}

			line = lineEnd + 1;
		}

		if (bodyStart >= 0)
		{
			_scan.Mark(bodyStart, bodyEnd, MokaCodeTokenKind.String);
		}

		return bodyEnd;
	}

	// The indentation of the first line after a block scalar's header with anything on it, or -1 when there's none.
	private int FirstContentIndent(int line)
	{
		while (line < _end)
		{
			int lineEnd = CodeText.LineEnd(_text, line, _end);
			if (CodeText.SkipWhitespace(_text, line, lineEnd) < lineEnd)
			{
				return CodeText.RunLength(_text, line, lineEnd, ' ');
			}

			line = lineEnd + 1;
		}

		return -1;
	}

	// The YAML 1.2 core schema: null, booleans, integers in decimal, octal and hex, and floats with infinity and NaN.
	private static MokaCodeTokenKind ScalarKind(ReadOnlySpan<char> scalar)
	{
		if (scalar is "null" or "Null" or "NULL" or "~" or "true" or "True" or "TRUE" or "false" or "False" or "FALSE")
		{
			return MokaCodeTokenKind.Keyword;
		}

		return IsInteger(scalar) || IsFloat(scalar) ? MokaCodeTokenKind.Number : MokaCodeTokenKind.Value;
	}

	private static bool IsInteger(ReadOnlySpan<char> scalar)
	{
		if (scalar.Length > 2 && scalar[0] == '0' && scalar[1] is 'o' or 'x')
		{
			ReadOnlySpan<char> digits = scalar[2..];
			return scalar[1] == 'o'
				? !digits.ContainsAnyExceptInRange('0', '7')
				: !digits.ContainsAnyExcept(HexDigits);
		}

		int start = scalar.Length > 0 && scalar[0] is '+' or '-' ? 1 : 0;
		return scalar.Length > start && !scalar[start..].ContainsAnyExceptInRange('0', '9');
	}

	private static bool IsFloat(ReadOnlySpan<char> scalar)
	{
		if (scalar is ".nan" or ".NaN" or ".NAN")
		{
			return true;
		}

		int i = scalar.Length > 0 && scalar[0] is '+' or '-' ? 1 : 0;
		if (scalar[i..] is ".inf" or ".Inf" or ".INF")
		{
			return true;
		}

		int whole = Digits(scalar, ref i);
		int fraction = 0;
		if (i < scalar.Length && scalar[i] == '.')
		{
			i++;
			fraction = Digits(scalar, ref i);
		}

		if (whole == 0 && fraction == 0)
		{
			return false;
		}

		if (i < scalar.Length && scalar[i] is 'e' or 'E')
		{
			i++;
			if (i < scalar.Length && scalar[i] is '+' or '-')
			{
				i++;
			}

			if (Digits(scalar, ref i) == 0)
			{
				return false;
			}
		}

		return i == scalar.Length;
	}

	private static int Digits(ReadOnlySpan<char> scalar, ref int i)
	{
		int start = i;
		while (i < scalar.Length && char.IsAsciiDigit(scalar[i]))
		{
			i++;
		}

		return i - start;
	}
}
