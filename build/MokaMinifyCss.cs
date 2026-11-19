using System;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;

namespace Moka.Red.Build;

public sealed class MokaMinifyCss : Microsoft.Build.Utilities.Task
{
	private static readonly UTF8Encoding Utf8 = new(false, true);

	private static readonly UTF8Encoding Utf8WithBom = new(true, true);

#pragma warning disable CA1819 // MSBuild binds an item list only to an array property.
	[Required]
	public ITaskItem[] Files { get; set; } = Array.Empty<ITaskItem>();
#pragma warning restore CA1819

	public override bool Execute()
	{
		foreach (ITaskItem file in Files)
		{
			string source = file.GetMetadata("FullPath");
			string destination = file.GetMetadata("MinifiedFile");
			if (destination.Length == 0)
			{
				destination = source;
			}

			try
			{
				byte[] bytes = File.ReadAllBytes(source);
				bool bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
				int start = bom ? 3 : 0;
				string css = Utf8.GetString(bytes, start, bytes.Length - start);
				var folder = Path.GetDirectoryName(destination);
				if (!string.IsNullOrEmpty(folder))
				{
					Directory.CreateDirectory(folder);
				}

				File.WriteAllText(destination, MokaCssMinifier.Minify(css), bom ? Utf8WithBom : Utf8);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DecoderFallbackException)
			{
				Log.LogError("Could not minify {0}: {1}", source, ex.Message);
			}
		}

		return !Log.HasLoggedErrors;
	}
}

// Drops comments and whitespace only, so every other token stays exactly as written and a browser builds the same rules
// from the result. Tokens follow CSS Syntax Module Level 3: strings, escapes and unquoted url() are copied as they are,
// and a whitespace run becomes one space unless the grammar ignores it at that place.
internal static class MokaCssMinifier
{
	public static string Minify(string css) => new Run(css).Minify();

	private enum StatementKind
	{
		Prelude,
		Declaration,
		CustomProperty,
	}

	private sealed class Run
	{
		private readonly string _css;
		private readonly StringBuilder _output;
		private int _position;
		private bool _pendingSpace;
		private bool _pendingComment;
		private bool _statementStart = true;
		private StatementKind _kind;
		private bool _inValue;
		private bool _valueStart;
		private int _nesting;

		public Run(string css)
		{
			_css = css;
			_output = new StringBuilder(css.Length);
		}

		public string Minify()
		{
			while (_position < _css.Length)
			{
				char current = _css[_position];
				if (IsWhitespace(current))
				{
					_pendingSpace = true;
					_position++;
				}
				else if (current == '/' && CharAt(_position + 1) == '*')
				{
					Comment();
				}
				else
				{
					if (_statementStart)
					{
						_kind = Classify(_position);
						_statementStart = false;
						_inValue = false;
						_valueStart = false;
						_nesting = 0;
					}

					WriteSeparator(current);
					Token();
				}
			}

			return _output.ToString();
		}

		private void Comment()
		{
			int stop = CommentEnd(_position);
			if (CharAt(_position + 2) == '!')
			{
				WriteSeparator('/');
				_output.Append(_css, _position, stop - _position);
			}
			else if (!_pendingSpace)
			{
				_pendingComment = true;
			}

			_position = stop;
		}

		private void WriteSeparator(char next)
		{
			bool space = _pendingSpace;
			bool comment = _pendingComment;
			_pendingSpace = false;
			_pendingComment = false;
			if (_output.Length == 0 || (!space && !comment))
			{
				return;
			}

			char previous = _output[_output.Length - 1];
			if (space)
			{
				if (KeepsSpace(previous, next))
				{
					_output.Append(' ');
				}
			}
			else if (!IsTightAfter(previous) && !IsTightBefore(next))
			{
				// A comment with no whitespace beside it still parts two tokens, so it stays where they could join.
				_output.Append("/**/");
			}
		}

		private bool KeepsSpace(char previous, char next)
		{
			if (_kind != StatementKind.Prelude)
			{
				if ((!_inValue && next == ':') || _valueStart)
				{
					return false;
				}

				// A custom property keeps its value as written, so only the space at its ends goes.
				if (_kind == StatementKind.CustomProperty && _inValue)
				{
					return next is not (';' or '}' or '!');
				}
			}

			if (IsTightAfter(previous))
			{
				// "> =" is not the ">=" of a range query, nor "~ =" the "~=" of an attribute selector.
				return previous is '>' or '~' && next == '=';
			}

			return !IsTightBefore(next);
		}

		private void Token()
		{
			int start = _position;
			bool startsValue = false;
			switch (_css[_position])
			{
				case '"':
				case '\'':
					_position = StringEnd(_position);
					break;
				case '\\':
					_position = EscapeEnd(_position);
					break;
				case '{':
				case '}':
					_position++;
					_statementStart = true;
					break;
				case ';':
					_position++;
					_statementStart = _nesting == 0;
					break;
				case '(':
					if (IsUrlToken(_position))
					{
						_position = UrlEnd(_position);
					}
					else
					{
						_nesting++;
						_position++;
					}

					break;
				case '[':
					_nesting++;
					_position++;
					break;
				case ')':
				case ']':
					_nesting = Math.Max(0, _nesting - 1);
					_position++;
					break;
				case ':':
					startsValue = _kind != StatementKind.Prelude && !_inValue && _nesting == 0;
					_position++;
					break;
				default:
					_position++;
					break;
			}

			_output.Append(_css, start, _position - start);
			_valueStart = startsValue;
			_inValue |= startsValue;
		}

		// A statement whose first "{" comes before any ";" or "}" is a rule; anything else in a block is a declaration.
		private StatementKind Classify(int start)
		{
			if (_css[start] == '@')
			{
				return StatementKind.Prelude;
			}

			int nesting = 0;
			int index = start;
			while (index < _css.Length)
			{
				char current = _css[index];
				if (current is '"' or '\'')
				{
					index = StringEnd(index);
					continue;
				}

				if (current == '\\')
				{
					index = EscapeEnd(index);
					continue;
				}

				if (current == '/' && CharAt(index + 1) == '*')
				{
					index = CommentEnd(index);
					continue;
				}

				if (current == '(' && IsUrlToken(index))
				{
					index = UrlEnd(index);
					continue;
				}

				if (current is '(' or '[')
				{
					nesting++;
				}
				else if (current is ')' or ']')
				{
					nesting = Math.Max(0, nesting - 1);
				}
				else if (nesting == 0 && current == '{')
				{
					return StatementKind.Prelude;
				}
				else if (nesting == 0 && current is ';' or '}')
				{
					break;
				}

				index++;
			}

			return CharAt(start) == '-' && CharAt(start + 1) == '-' ? StatementKind.CustomProperty : StatementKind.Declaration;
		}

		private int StringEnd(int start)
		{
			char quote = _css[start];
			int index = start + 1;
			while (index < _css.Length)
			{
				char current = _css[index];
				if (current == quote)
				{
					return index + 1;
				}

				if (current is '\n' or '\r' or '\f')
				{
					return index;
				}

				index += current == '\\' ? EscapedLength(index) : 1;
			}

			return index;
		}

		private int EscapeEnd(int start)
		{
			int index = start + 1;
			if (index >= _css.Length || _css[index] is '\n' or '\r' or '\f')
			{
				return index;
			}

			if (!IsHexDigit(_css[index]))
			{
				return index + 1;
			}

			int digits = 0;
			while (index < _css.Length && digits < 6 && IsHexDigit(_css[index]))
			{
				index++;
				digits++;
			}

			// One whitespace after a hex escape ends the escape and belongs to it.
			if (CharAt(index) == '\r' && CharAt(index + 1) == '\n')
			{
				return index + 2;
			}

			return index < _css.Length && IsWhitespace(_css[index]) ? index + 1 : index;
		}

		private int EscapedLength(int backslash)
		{
			if (backslash + 1 >= _css.Length)
			{
				return 1;
			}

			return _css[backslash + 1] == '\r' && CharAt(backslash + 2) == '\n' ? 3 : 2;
		}

		private bool IsUrlToken(int parenthesis)
		{
			if (parenthesis < 3
				|| string.Compare(_css, parenthesis - 3, "url", 0, 3, StringComparison.OrdinalIgnoreCase) != 0
				|| (parenthesis > 3 && IsNameCharacter(_css[parenthesis - 4])))
			{
				return false;
			}

			int index = parenthesis + 1;
			while (index < _css.Length && IsWhitespace(_css[index]))
			{
				index++;
			}

			return CharAt(index) is not ('"' or '\'');
		}

		private int UrlEnd(int parenthesis)
		{
			int index = parenthesis + 1;
			while (index < _css.Length)
			{
				char current = _css[index];
				if (current == ')')
				{
					return index + 1;
				}

				index = current == '\\' ? EscapeEnd(index) : index + 1;
			}

			return index;
		}

		private int CommentEnd(int start)
		{
			int end = _css.IndexOf("*/", start + 2, StringComparison.Ordinal);
			return end < 0 ? _css.Length : end + 2;
		}

		private char CharAt(int index) => index < _css.Length ? _css[index] : '\0';

		private static bool IsWhitespace(char c) => c is ' ' or '\t' or '\n' or '\r' or '\f';

		private static bool IsHexDigit(char c) => c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');

		private static bool IsNameCharacter(char c) =>
			c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-' or '_' or '\\' || c >= '\u0080';

		private static bool IsTightAfter(char c) => c is '{' or '}' or ';' or ',' or '(' or '>' or '~';

		private static bool IsTightBefore(char c) => c is '{' or '}' or ';' or ',' or ')' or '>' or '~' or '!';
	}
}
