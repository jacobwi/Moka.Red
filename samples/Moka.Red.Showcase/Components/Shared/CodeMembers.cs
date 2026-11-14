using System.Text;
using System.Text.RegularExpressions;

namespace Moka.Red.Showcase.Components.Shared;

internal sealed record CodeMember(string Name, string Text, IReadOnlySet<string> Uses);

// Reads a demo file's @code block as its members, so a section's Code tab can show the ones its markup names. It knows
// enough C# to find where each member ends (strings, comments, Razor templates and brackets), not to compile it.
internal static partial class CodeMembers
{
	private static readonly HashSet<string> Modifiers = new(StringComparer.Ordinal)
	{
		"public", "private", "protected", "internal", "static", "readonly", "sealed", "abstract", "partial", "new",
		"file", "unsafe", "required", "override", "virtual", "async", "const", "volatile", "extern", "ref", "event"
	};

	public static IReadOnlyList<CodeMember> Parse(string body)
	{
		List<CodeMember> members = [];
		foreach ((int start, int end) in Spans(body))
		{
			string text = body[start..end].TrimEnd();
			string head = StripLeading(text);
			members.Add(new CodeMember(DeclaredName(head) ?? string.Empty, text, Identifiers(head)));
		}

		return members;
	}

	// The members a section's markup names, then the members those name, in the order the file has them.
	public static IReadOnlyList<CodeMember> UsedBy(IReadOnlyList<CodeMember> members, string markup)
	{
		HashSet<string> named = Identifiers(markup, codeOnly: true);
		HashSet<CodeMember> direct = [.. members.Where(member => member.Name.Length > 0 && named.Contains(member.Name))];
		HashSet<string> second = [.. direct.SelectMany(member => member.Uses)];
		return [.. members.Where(member => direct.Contains(member) || (member.Name.Length > 0 && second.Contains(member.Name)))];
	}

	public static string Render(IReadOnlyList<CodeMember> members)
	{
		if (members.Count == 0)
		{
			return string.Empty;
		}

		StringBuilder code = new("@code {\n");
		for (int i = 0; i < members.Count; i++)
		{
			if (i > 0)
			{
				code.Append('\n');
			}

			code.Append(members[i].Text).Append('\n');
		}

		return code.Append('}').ToString();
	}

	public static (int BodyStart, int BodyEnd)? CodeBlock(string source)
	{
		Match code = CodeDirective().Match(source);
		if (!code.Success)
		{
			return null;
		}

		int open = source.IndexOf('{', code.Index);
		int end = SkipBalanced(source, open, '{', '}');
		return (open + 1, end - 1);
	}

	private static List<(int Start, int End)> Spans(string body)
	{
		List<(int, int)> spans = [];
		int i = 0;
		int depth = 0;
		int start = -1;
		bool expression = false;
		while (i < body.Length)
		{
			char c = body[i];
			if (char.IsWhiteSpace(c))
			{
				i++;
				continue;
			}

			if (start < 0)
			{
				start = body.LastIndexOf('\n', Math.Max(0, i - 1)) + 1;
				expression = false;
			}

			// After an initializer or an expression body, only a semicolon ends the member: a brace there closes a
			// pattern or an object initializer, as in "=> x is { Count: > 0 } list ? ...".
			if (c == '=' && depth == 0 && i > 0 && body[i - 1] is not ('=' or '!' or '<' or '>')
				&& !(i + 1 < body.Length && body[i + 1] == '='))
			{
				expression = true;
			}

			int skipped = Skip(body, i);
			if (skipped > i)
			{
				i = skipped;
				continue;
			}

			if (c is '{' or '(' or '[')
			{
				depth++;
			}
			else if (c is '}' or ')' or ']')
			{
				depth--;
				if (depth == 0 && c == '}' && !expression)
				{
					int next = i + 1;
					while (next < body.Length && char.IsWhiteSpace(body[next]))
					{
						next++;
					}

					bool continues = next < body.Length
						&& (body[next] is ',' or ')' || (body[next] == '=' && !(next + 1 < body.Length && body[next + 1] == '>')));
					if (continues)
					{
						i++;
						continue;
					}

					int end = next < body.Length && body[next] == ';' ? next + 1 : i + 1;
					spans.Add((start, end));
					start = -1;
					i = end;
					continue;
				}
			}
			else if (c == ';' && depth == 0)
			{
				spans.Add((start, i + 1));
				start = -1;
			}

			i++;
		}

		return spans;
	}

	// Moves past a string, a character literal, a comment, a directive line or a Razor template starting at i.
	private static int Skip(string text, int i)
	{
		char c = text[i];
		if (c == '/' && i + 1 < text.Length && text[i + 1] is '/' or '*')
		{
			return SkipComment(text, i);
		}

		if (c == '#' && (i == 0 || text[i - 1] == '\n' || string.IsNullOrWhiteSpace(text[(text.LastIndexOf('\n', i - 1) + 1)..i])))
		{
			int end = text.IndexOf('\n', i);
			return end < 0 ? text.Length : end;
		}

		if (StartsString(text, i))
		{
			return SkipString(text, i);
		}

		if (c == '\'')
		{
			return SkipChar(text, i);
		}

		if (c == '@' && i + 1 < text.Length && text[i + 1] == '<')
		{
			return SkipMarkup(text, i + 1);
		}

		return i;
	}

	private static bool StartsString(string text, int i)
	{
		int k = i;
		while (k < text.Length && k - i < 3 && text[k] is '$' or '@')
		{
			k++;
		}

		return k < text.Length && text[k] == '"';
	}

	private static int SkipString(string text, int i)
	{
		bool interpolated = false;
		bool verbatim = false;
		int j = i;
		while (text[j] is '$' or '@')
		{
			interpolated |= text[j] == '$';
			verbatim |= text[j] == '@';
			j++;
		}

		int quotes = 0;
		while (j + quotes < text.Length && text[j + quotes] == '"')
		{
			quotes++;
		}

		if (quotes >= 3)
		{
			int close = text.IndexOf(new string('"', quotes), j + quotes, StringComparison.Ordinal);
			return close < 0 ? text.Length : close + quotes;
		}

		j++;
		while (j < text.Length)
		{
			char c = text[j];
			if (c == '"')
			{
				if (verbatim && j + 1 < text.Length && text[j + 1] == '"')
				{
					j += 2;
					continue;
				}

				return j + 1;
			}

			if (c == '\\' && !verbatim)
			{
				j += 2;
				continue;
			}

			if (interpolated && c == '{')
			{
				if (j + 1 < text.Length && text[j + 1] == '{')
				{
					j += 2;
					continue;
				}

				j = SkipBalanced(text, j, '{', '}');
				continue;
			}

			j++;
		}

		return text.Length;
	}

	private static int SkipChar(string text, int i)
	{
		int j = i + 1;
		while (j < text.Length)
		{
			if (text[j] == '\\')
			{
				j += 2;
				continue;
			}

			if (text[j] is '\'' or '\n')
			{
				return text[j] == '\'' ? j + 1 : j;
			}

			j++;
		}

		return text.Length;
	}

	private static int SkipComment(string text, int i)
	{
		if (text[i + 1] == '/')
		{
			int end = text.IndexOf('\n', i);
			return end < 0 ? text.Length : end;
		}

		int close = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
		return close < 0 ? text.Length : close + 2;
	}

	// A Razor template (@<div>...</div>) inside C#: its text is markup, where an apostrophe is not a character literal.
	private static int SkipMarkup(string text, int i)
	{
		int depth = 0;
		int j = i;
		while (j < text.Length)
		{
			if (text[j] != '<')
			{
				j++;
				continue;
			}

			if (j + 1 < text.Length && text[j + 1] == '/')
			{
				int end = text.IndexOf('>', j);
				j = end < 0 ? text.Length : end + 1;
				if (--depth <= 0)
				{
					return j;
				}

				continue;
			}

			if (j + 1 < text.Length && char.IsLetter(text[j + 1]))
			{
				int k = j + 1;
				char quote = '\0';
				while (k < text.Length)
				{
					char c = text[k];
					if (quote != '\0')
					{
						if (c == quote)
						{
							quote = '\0';
						}
					}
					else if (c is '"' or '\'')
					{
						quote = c;
					}
					else if (c == '>')
					{
						break;
					}

					k++;
				}

				bool selfClosing = k > 0 && k < text.Length && text[k - 1] == '/';
				j = k + 1;
				if (!selfClosing)
				{
					depth++;
				}
				else if (depth == 0)
				{
					return j;
				}

				continue;
			}

			j++;
		}

		return text.Length;
	}

	private static int SkipBalanced(string text, int i, char open, char close)
	{
		int depth = 0;
		int j = i;
		while (j < text.Length)
		{
			int skipped = Skip(text, j);
			if (skipped > j)
			{
				j = skipped;
				continue;
			}

			if (text[j] == open)
			{
				depth++;
			}
			else if (text[j] == close && --depth == 0)
			{
				return j + 1;
			}

			j++;
		}

		return text.Length;
	}

	private static string StripLeading(string member)
	{
		string text = member;
		while (true)
		{
			string trimmed = text.TrimStart();
			if (trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith("/*", StringComparison.Ordinal))
			{
				text = trimmed[SkipComment(trimmed, 0)..];
				continue;
			}

			if (trimmed.StartsWith('['))
			{
				text = trimmed[SkipBalanced(trimmed, 0, '[', ']')..];
				continue;
			}

			return trimmed;
		}
	}

	private static string? DeclaredName(string head)
	{
		Match type = TypeDeclaration().Match(head);
		if (type.Success)
		{
			return type.Groups["name"].Value;
		}

		int angle = 0;
		string? last = null;
		string previous = string.Empty;
		bool typeSeen = false;
		MatchCollection tokens = Token().Matches(head);
		for (int i = 0; i < tokens.Count; i++)
		{
			string token = tokens[i].Value;
			switch (token)
			{
				case "<":
					angle++;
					continue;
				case ">":
					angle--;
					previous = ">";
					typeSeen = true;
					continue;
			}

			if (angle > 0)
			{
				continue;
			}

			switch (token)
			{
				case "=" or "=>" or ";" or "{":
					return last;
				case "(" when previous == "ident" && last is not null && !Modifiers.Contains(last) && typeSeen:
					return last;
				case "(" or "[":
					i = Matching(tokens, i);
					previous = ")";
					typeSeen = true;
					continue;
			}

			if (char.IsLetter(token[0]) || token[0] == '_')
			{
				typeSeen |= (previous == "ident" && last is not null && !Modifiers.Contains(last)) || previous is ">" or ")" or "?";
				last = token;
				previous = "ident";
			}
			else
			{
				previous = token;
			}
		}

		return last;
	}

	private static int Matching(MatchCollection tokens, int open)
	{
		string opening = tokens[open].Value;
		string closing = opening == "(" ? ")" : "]";
		int depth = 0;
		for (int i = open; i < tokens.Count; i++)
		{
			if (tokens[i].Value == opening)
			{
				depth++;
			}
			else if (tokens[i].Value == closing && --depth == 0)
			{
				return i;
			}
		}

		return tokens.Count;
	}

	// With codeOnly, only the parts of markup that are C# count: attribute values and @ expressions. Plain text such
	// as a button's label "Save" must not pull in a method called Save.
	private static HashSet<string> Identifiers(string text, bool codeOnly = false)
	{
		if (!codeOnly)
		{
			return [.. Identifier().Matches(text).Select(match => match.Value)];
		}

		HashSet<string> found = [];
		int i = 0;
		while (i < text.Length)
		{
			char c = text[i];
			if (c == '@' && i + 1 < text.Length)
			{
				int end = text[i + 1] switch
				{
					'(' => SkipBalanced(text, i + 1, '(', ')'),
					'{' => SkipBalanced(text, i + 1, '{', '}'),
					_ => ExpressionEnd(text, i + 1)
				};
				found.UnionWith(Identifiers(text[i..end]));
				i = Math.Max(end, i + 1);
				continue;
			}

			if (c == '=' && i + 1 < text.Length && text[i + 1] == '"')
			{
				int j = i + 2;
				while (j < text.Length && text[j] != '"')
				{
					j = text[j] == '@' && j + 1 < text.Length && text[j + 1] == '(' ? SkipBalanced(text, j + 1, '(', ')') : j + 1;
				}

				found.UnionWith(Identifiers(text[(i + 2)..Math.Min(j, text.Length)]));
				i = j + 1;
				continue;
			}

			i++;
		}

		return found;
	}

	// An implicit expression (@foo.Bar(x)[0]) or a code statement (@if (x) / @foreach (...)): to the end of its
	// brackets, not into the markup after it.
	private static int ExpressionEnd(string text, int i)
	{
		Match statement = Statement().Match(text, i);
		if (statement.Success && statement.Index == i)
		{
			return SkipBalanced(text, statement.Index + statement.Length - 1, '(', ')');
		}

		int j = i;
		while (j < text.Length)
		{
			if (char.IsLetterOrDigit(text[j]) || text[j] is '_' or '.')
			{
				j++;
			}
			else if (text[j] is '(' or '[')
			{
				j = SkipBalanced(text, j, text[j], text[j] == '(' ? ')' : ']');
			}
			else
			{
				break;
			}
		}

		return j;
	}

	[GeneratedRegex(@"^@code\b", RegexOptions.Multiline)]
	private static partial Regex CodeDirective();

	[GeneratedRegex(@"^(?:(?:public|private|protected|internal|static|readonly|sealed|abstract|partial|new|file)\s+)*(?:record\s+struct|record\s+class|record|class|struct|enum|interface)\s+(?<name>[A-Za-z_]\w*)")]
	private static partial Regex TypeDeclaration();

	[GeneratedRegex(@"[A-Za-z_]\w*|=>|==|!=|<=|>=|[=;{}()<>\[\],?.:]|\S")]
	private static partial Regex Token();

	[GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_]*")]
	private static partial Regex Identifier();

	[GeneratedRegex(@"(?:if|foreach|for|while|switch|using|else if)\s*\(")]
	private static partial Regex Statement();
}
