using System.Collections.Frozen;

namespace Moka.Red.Core.Code;

/// <summary>
///     Colors shell commands, bash and PowerShell alike: comments, strings with their variables, here-documents and
///     here-strings, variables, options, the command that starts each pipeline step, and keywords.
/// </summary>
internal sealed class ShellLexer
{
	private static readonly FrozenSet<string> Keywords = FrozenSet.Create(StringComparer.OrdinalIgnoreCase,
		"if", "then", "else", "elif", "fi", "case", "esac", "for", "select", "while", "until", "do", "done", "in", "function",
		"time", "return", "break", "continue", "local", "export", "readonly", "declare", "elseif", "foreach", "switch",
		"filter", "param", "begin", "process", "end", "try", "catch", "finally", "throw", "trap", "exit");

	// After these a new command starts: "then echo", "do make".
	private static readonly FrozenSet<string> CommandFollows = FrozenSet.Create(StringComparer.OrdinalIgnoreCase,
		"if", "then", "else", "elif", "while", "until", "do", "time", "elseif", "try", "finally", "begin", "process", "end");

	private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> KeywordLookup =
		Keywords.GetAlternateLookup<ReadOnlySpan<char>>();

	private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> CommandFollowsLookup =
		CommandFollows.GetAlternateLookup<ReadOnlySpan<char>>();

	private readonly CodeScan _scan;
	private readonly string _text;
	private readonly int _end;

	private ShellLexer(CodeScan scan)
	{
		_scan = scan;
		_text = scan.Text;
		_end = scan.Text.Length;
	}

	public static List<MokaCodeSpan> Highlight(string text)
	{
		ArgumentNullException.ThrowIfNull(text);
		CodeScan scan = new(text);
		new ShellLexer(scan).Scan();
		return scan.Spans;
	}

	private static bool IsWordPart(char c) => !char.IsWhiteSpace(c) && c is not ('|' or '&' or ';' or '<' or '>' or '('
		or ')' or '{' or '}' or '$' or '"' or '\'' or '`' or '=');

	private bool StartsWord(int pos) => pos == 0 || char.IsWhiteSpace(_text[pos - 1]) || _text[pos - 1] is ';' or '|' or '&' or '(' or '{';

	private void Scan()
	{
		int pos = 0;
		bool command = true;
		while (pos < _end)
		{
			char c = _text[pos];
			char next = CodeText.At(_text, pos + 1, _end);
			if (c == '\n')
			{
				command = true;
				pos++;
			}
			else if (char.IsWhiteSpace(c))
			{
				pos++;
			}
			else if (c == '#' && StartsWord(pos))
			{
				int lineEnd = CodeText.LineEnd(_text, pos, _end);
				_scan.Mark(pos, lineEnd, MokaCodeTokenKind.Comment);
				pos = lineEnd;
			}
			else if (c == '<' && next == '#')
			{
				int close = CodeText.After(_text, "#>", pos + 2, _end);
				_scan.Mark(pos, close, MokaCodeTokenKind.Comment);
				pos = close;
			}
			else if (c == '@' && next is '"' or '\'' && CodeText.At(_text, CodeText.SkipSpaces(_text, pos + 2, _end), _end) is '\n' or '\r')
			{
				pos = HereString(pos, next);
			}
			else if (c == '<' && next == '<' && CodeText.At(_text, pos + 2, _end) != '<')
			{
				pos = HereDocument(pos);
			}
			else if (c == '"')
			{
				pos = DoubleQuoted(pos);
				command = false;
			}
			else if (c == '\'')
			{
				int close = _text.IndexOf('\'', pos + 1);
				close = close < 0 ? _end : close + 1;
				_scan.Mark(pos, close, MokaCodeTokenKind.String);
				pos = close;
				command = false;
			}
			else if (c == '$')
			{
				pos = Variable(pos);
				command = next == '(';
			}
			else if (c is '\\' or '`')
			{
				pos = Math.Min(_end, pos + 2);
			}
			else if (c == '-' && StartsWord(pos) && (char.IsAsciiLetter(next) || next == '-'))
			{
				int wordEnd = WordEnd(pos);
				_scan.Mark(pos, wordEnd, MokaCodeTokenKind.Attribute);
				pos = wordEnd;
			}
			else if (c == '[' && StartsWord(pos) && char.IsAsciiLetter(next) && TypeLiteralEnd(pos) is var typeEnd and > 0)
			{
				_scan.Mark(pos, typeEnd, MokaCodeTokenKind.Type);
				pos = typeEnd;
			}
			else if (IsWordPart(c))
			{
				pos = Word(pos, ref command);
			}
			else
			{
				_scan.Mark(pos, pos + 1, MokaCodeTokenKind.Punctuation);
				command = c is '|' or '&' or ';' or '(' or '{' || (command && c == '=');
				pos++;
			}
		}
	}

	private int WordEnd(int pos)
	{
		while (pos < _end && IsWordPart(_text[pos]))
		{
			pos++;
		}

		return pos;
	}

	private int Word(int pos, ref bool command)
	{
		int wordEnd = WordEnd(pos);
		ReadOnlySpan<char> word = _text.AsSpan(pos, wordEnd - pos);
		if (pos > 0 && _text[pos - 1] == '=')
		{
			// The value in NAME=value, which the command still follows.
			return wordEnd;
		}

		if (CodeText.At(_text, wordEnd, _end) == '=')
		{
			_scan.Mark(pos, wordEnd, MokaCodeTokenKind.Key);
		}
		else if (KeywordLookup.Contains(word))
		{
			_scan.Mark(pos, wordEnd, MokaCodeTokenKind.Keyword);
			command = CommandFollowsLookup.Contains(word);
		}
		else if (command)
		{
			_scan.Mark(pos, wordEnd, MokaCodeTokenKind.Type);
			command = false;
		}
		else if (word.Length > 0 && char.IsAsciiDigit(word[0]) && IsNumber(word))
		{
			_scan.Mark(pos, wordEnd, MokaCodeTokenKind.Number);
		}

		return wordEnd;
	}

	private static bool IsNumber(ReadOnlySpan<char> word)
	{
		foreach (char c in word)
		{
			if (!char.IsAsciiDigit(c) && c != '.')
			{
				return false;
			}
		}

		return true;
	}

	// [string], [System.IO.Path]: a PowerShell type.
	private int TypeLiteralEnd(int pos)
	{
		int limit = Math.Min(_end, pos + 128);
		int i = pos + 1;
		while (i < limit && (CodeText.IsIdentifierPart(_text[i]) || _text[i] is '.' or ','))
		{
			i++;
		}

		return i < limit && _text[i] == ']' ? i + 1 : -1;
	}

	private int Variable(int pos)
	{
		char next = CodeText.At(_text, pos + 1, _end);
		int end;
		if (next == '{')
		{
			end = CodeText.After(_text, "}", pos + 2, _end);
		}
		else if (next == '(')
		{
			_scan.Mark(pos, pos + 2, MokaCodeTokenKind.Punctuation);
			return pos + 2;
		}
		else if (CodeText.IsIdentifierStart(next))
		{
			end = pos + 1;
			while (end < _end && (CodeText.IsIdentifierPart(_text[end])
			                      || (_text[end] == ':' && CodeText.IsIdentifierStart(CodeText.At(_text, end + 1, _end)))))
			{
				end++;
			}
		}
		else if (char.IsAsciiDigit(next) || next is '?' or '$' or '#' or '@' or '*' or '!' or '_' or '-')
		{
			end = pos + 2;
		}
		else
		{
			return pos + 1;
		}

		_scan.Mark(pos, end, MokaCodeTokenKind.Key);
		return end;
	}

	// "text $name text": the string with its variables colored as variables.
	private int DoubleQuoted(int pos)
	{
		int segment = pos;
		int i = pos + 1;
		while (i < _end && _text[i] != '"')
		{
			if (_text[i] is '\\' or '`')
			{
				i += 2;
			}
			else if (_text[i] == '$' && CodeText.At(_text, i + 1, _end) is not ('"' or '(' or ' ' or '\0'))
			{
				_scan.Mark(segment, i, MokaCodeTokenKind.String);
				int after = Variable(i);
				segment = after;
				i = Math.Max(after, i + 1);
			}
			else
			{
				i++;
			}
		}

		i = Math.Min(_end, i + 1);
		_scan.Mark(segment, i, MokaCodeTokenKind.String);
		return i;
	}

	// PowerShell's @" ... "@: it ends at a line that starts with the closing pair.
	private int HereString(int pos, char quote)
	{
		string close = quote == '"' ? "\n\"@" : "\n'@";
		int after = CodeText.After(_text, close, pos + 2, _end);
		_scan.Mark(pos, after, MokaCodeTokenKind.String);
		return after;
	}

	// <<EOF ... EOF: the lines up to one that holds only the delimiter, tabs before it allowed with <<-.
	private int HereDocument(int pos)
	{
		int i = pos + 2;
		bool tabs = CodeText.At(_text, i, _end) == '-';
		if (tabs)
		{
			i++;
		}

		i = CodeText.SkipSpaces(_text, i, _end);
		char quote = CodeText.At(_text, i, _end) is '\'' or '"' ? _text[i] : '\0';
		int wordStart = quote == '\0' ? i : i + 1;
		int wordEnd = wordStart;
		while (wordEnd < _end && (CodeText.IsIdentifierPart(_text[wordEnd]) || _text[wordEnd] == '-'))
		{
			wordEnd++;
		}

		if (wordEnd == wordStart)
		{
			_scan.Mark(pos, pos + 2, MokaCodeTokenKind.Punctuation);
			return pos + 2;
		}

		ReadOnlySpan<char> delimiter = _text.AsSpan(wordStart, wordEnd - wordStart);
		int markerEnd = quote != '\0' && CodeText.At(_text, wordEnd, _end) == quote ? wordEnd + 1 : wordEnd;
		_scan.Mark(pos, markerEnd, MokaCodeTokenKind.Punctuation);

		int bodyStart = CodeText.LineEnd(_text, markerEnd, _end);
		int line = Math.Min(_end, bodyStart + 1);
		while (line < _end)
		{
			int lineEnd = CodeText.LineEnd(_text, line, _end);
			ReadOnlySpan<char> content = _text.AsSpan(line, lineEnd - line).TrimEnd('\r');
			if ((tabs ? content.TrimStart('\t') : content).SequenceEqual(delimiter))
			{
				_scan.Mark(bodyStart, line, MokaCodeTokenKind.String);
				_scan.Mark(line, lineEnd, MokaCodeTokenKind.Punctuation);
				return lineEnd;
			}

			line = lineEnd + 1;
		}

		_scan.Mark(bodyStart, _end, MokaCodeTokenKind.String);
		return _end;
	}
}
