using System.Text;
using System.Text.RegularExpressions;

namespace Moka.Red.DevApp.Components.Shared;

internal static partial class RazorSourceSlicer
{
	private const string SectionTag = "DemoSection";
	private const string CloseTag = "</" + SectionTag + ">";
	private static readonly string[] Slots = ["ChildContent", "Notes", "Playground"];

	public static IReadOnlyDictionary<string, string> Slice(string source)
	{
		string markup = MarkupOf(source.Replace("\r\n", "\n", StringComparison.Ordinal));
		List<Block> blocks = FindBlocks(markup);
		Dictionary<string, string> sections = new(StringComparer.Ordinal);
		foreach (Block block in blocks)
		{
			if (block.Id is { Length: > 0 } id)
			{
				sections.TryAdd(id, Outdent(ExampleOf(markup, block)));
			}
		}

		return sections;
	}

	// The @code block holds C#, where a string could spell the tag without being one.
	private static string MarkupOf(string source)
	{
		Match code = CodeBlock().Match(source);
		return code.Success ? source[..code.Index] : source;
	}

	private static List<Block> FindBlocks(string markup)
	{
		List<Block> blocks = [];
		Stack<Block> open = new();
		int position = 0;
		while (true)
		{
			int start = NextStartTag(markup, SectionTag, position);
			int close = markup.IndexOf(CloseTag, position, StringComparison.Ordinal);
			if (start < 0 && close < 0)
			{
				break;
			}

			if (start >= 0 && (close < 0 || start < close))
			{
				StartTag tag = ReadStartTag(markup, start);
				Block block = new(start, tag.End, IndentAt(markup, start), tag.Attribute("Id"), open.Count > 0 ? open.Peek() : null);
				if (tag.SelfClosing)
				{
					block.Close(tag.End, tag.End);
					blocks.Add(block);
				}
				else
				{
					open.Push(block);
				}

				position = tag.End;
				continue;
			}

			if (open.Count > 0)
			{
				Block block = open.Pop();
				block.Close(close, close + CloseTag.Length);
				blocks.Add(block);
			}

			position = close + CloseTag.Length;
		}

		foreach (Block block in blocks)
		{
			block.Parent?.Children.Add(block);
		}

		foreach (Block block in blocks)
		{
			block.Children.Sort((a, b) => a.Start.CompareTo(b.Start));
		}

		return blocks;
	}

	private static string ExampleOf(string markup, Block block)
	{
		(int from, int to) = ChildContentRange(markup, block.BodyStart, block.BodyEnd);
		StringBuilder example = new();
		int position = from;
		foreach (Block child in block.Children)
		{
			if (child.Start < from || child.End > to)
			{
				continue;
			}

			example.Append(markup, position, child.Start - position);
			string inner = Outdent(ExampleOf(markup, child));
			example.Append(inner.Replace("\n", "\n" + child.Indent, StringComparison.Ordinal));
			position = child.End;
		}

		example.Append(markup, position, to - position);
		return example.ToString();
	}

	// With Notes or a Playground beside it, Razor wants the example in an explicit ChildContent element, and only
	// that element is the example. Without them the whole body is.
	private static (int From, int To) ChildContentRange(string markup, int bodyStart, int bodyEnd)
	{
		int position = SkipWhitespace(markup, bodyStart, bodyEnd);
		if (SlotAt(markup, position) is null)
		{
			return (bodyStart, bodyEnd);
		}

		(int From, int To) found = (bodyStart, bodyStart);
		while (position < bodyEnd && SlotAt(markup, position) is { } slot)
		{
			StartTag tag = ReadStartTag(markup, position);
			if (tag.SelfClosing)
			{
				position = SkipWhitespace(markup, tag.End, bodyEnd);
				continue;
			}

			int close = MatchingClose(markup, slot, tag.End, bodyEnd);
			if (close < 0)
			{
				break;
			}

			if (slot == "ChildContent")
			{
				found = (tag.End, close);
			}

			position = SkipWhitespace(markup, close + slot.Length + 3, bodyEnd);
		}

		return found;
	}

	private static int MatchingClose(string markup, string name, int from, int limit)
	{
		string close = "</" + name + ">";
		int depth = 1;
		int position = from;
		while (position < limit)
		{
			int nextOpen = NextStartTag(markup, name, position);
			int nextClose = markup.IndexOf(close, position, StringComparison.Ordinal);
			if (nextClose < 0 || nextClose >= limit)
			{
				return -1;
			}

			if (nextOpen >= 0 && nextOpen < nextClose)
			{
				StartTag tag = ReadStartTag(markup, nextOpen);
				if (!tag.SelfClosing)
				{
					depth++;
				}

				position = tag.End;
				continue;
			}

			depth--;
			if (depth == 0)
			{
				return nextClose;
			}

			position = nextClose + close.Length;
		}

		return -1;
	}

	private static string? SlotAt(string markup, int position)
	{
		foreach (string slot in Slots)
		{
			if (IsStartTag(markup, slot, position))
			{
				return slot;
			}
		}

		return null;
	}

	private static int NextStartTag(string markup, string name, int from)
	{
		int position = from;
		while (true)
		{
			int found = markup.IndexOf("<" + name, position, StringComparison.Ordinal);
			if (found < 0 || IsStartTag(markup, name, found))
			{
				return found;
			}

			position = found + name.Length + 1;
		}
	}

	private static bool IsStartTag(string markup, string name, int position)
	{
		int after = position + name.Length + 1;
		return after < markup.Length
			&& markup[position] == '<'
			&& string.CompareOrdinal(markup, position + 1, name, 0, name.Length) == 0
			&& (char.IsWhiteSpace(markup[after]) || markup[after] is '>' or '/');
	}

	// Reads up to the tag's closing '>', skipping quoted values. A value can hold Razor such as
	// @(a > b ? "x" : "y"), whose quotes and '>' belong to C#.
	private static StartTag ReadStartTag(string markup, int start)
	{
		Dictionary<string, string> attributes = new(StringComparer.Ordinal);
		int position = start + 1;
		while (position < markup.Length && !char.IsWhiteSpace(markup[position]) && markup[position] is not ('>' or '/'))
		{
			position++;
		}

		while (position < markup.Length)
		{
			char c = markup[position];
			if (c == '>')
			{
				return new StartTag(position + 1, false, attributes);
			}

			if (c == '/' && position + 1 < markup.Length && markup[position + 1] == '>')
			{
				return new StartTag(position + 2, true, attributes);
			}

			if (char.IsWhiteSpace(c))
			{
				position++;
				continue;
			}

			int nameStart = position;
			while (position < markup.Length && !char.IsWhiteSpace(markup[position]) && markup[position] is not ('=' or '>' or '/'))
			{
				position++;
			}

			string name = markup[nameStart..position];
			position = SkipWhitespace(markup, position, markup.Length);
			if (position >= markup.Length || markup[position] != '=')
			{
				attributes.TryAdd(name, string.Empty);
				continue;
			}

			position = SkipWhitespace(markup, position + 1, markup.Length);
			(string value, int end) = ReadValue(markup, position);
			attributes.TryAdd(name, value);
			position = end;
		}

		return new StartTag(markup.Length, false, attributes);
	}

	private static (string Value, int End) ReadValue(string markup, int position)
	{
		if (position >= markup.Length)
		{
			return (string.Empty, position);
		}

		char quote = markup[position];
		if (quote is not ('"' or '\''))
		{
			int start = position;
			while (position < markup.Length && !char.IsWhiteSpace(markup[position]) && markup[position] is not ('>' or '/'))
			{
				position++;
			}

			return (markup[start..position], position);
		}

		int valueStart = position + 1;
		int cursor = valueStart;
		int depth = 0;
		while (cursor < markup.Length)
		{
			char c = markup[cursor];
			if (depth == 0 && c == quote)
			{
				return (markup[valueStart..cursor], cursor + 1);
			}

			if (c == '@' && cursor + 1 < markup.Length && markup[cursor + 1] == '(')
			{
				depth++;
				cursor += 2;
				continue;
			}

			if (depth > 0)
			{
				if (c == '(')
				{
					depth++;
				}
				else if (c == ')')
				{
					depth--;
				}
				else if (c == '"')
				{
					cursor = SkipCSharpString(markup, cursor);
					continue;
				}
			}

			cursor++;
		}

		return (markup[valueStart..], markup.Length);
	}

	private static int SkipCSharpString(string markup, int quote)
	{
		int cursor = quote + 1;
		while (cursor < markup.Length && markup[cursor] != '"')
		{
			cursor += markup[cursor] == '\\' ? 2 : 1;
		}

		return cursor + 1;
	}

	private static int SkipWhitespace(string markup, int position, int limit)
	{
		while (position < limit && char.IsWhiteSpace(markup[position]))
		{
			position++;
		}

		return position;
	}

	private static string IndentAt(string markup, int position)
	{
		int lineStart = markup.LastIndexOf('\n', Math.Max(0, position - 1)) + 1;
		int end = lineStart;
		while (end < position && markup[end] is ' ' or '\t')
		{
			end++;
		}

		return markup[lineStart..end];
	}

	public static string Outdent(string text)
	{
		List<string> lines = [.. text.Split('\n')];
		while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
		{
			lines.RemoveAt(0);
		}

		while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
		{
			lines.RemoveAt(lines.Count - 1);
		}

		int common = int.MaxValue;
		foreach (string line in lines)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			int indent = 0;
			while (indent < line.Length && line[indent] is ' ' or '\t')
			{
				indent++;
			}

			common = Math.Min(common, indent);
		}

		if (common == int.MaxValue)
		{
			return string.Empty;
		}

		return string.Join('\n', lines.Select(line => string.IsNullOrWhiteSpace(line) ? string.Empty : line[common..].TrimEnd()));
	}

	[GeneratedRegex(@"^@code\b", RegexOptions.Multiline)]
	private static partial Regex CodeBlock();

	private sealed record StartTag(int End, bool SelfClosing, Dictionary<string, string> Attributes)
	{
		public string? Attribute(string name) => Attributes.GetValueOrDefault(name);
	}

	private sealed class Block(int start, int bodyStart, string indent, string? id, Block? parent)
	{
		public int Start { get; } = start;
		public int BodyStart { get; } = bodyStart;
		public string Indent { get; } = indent;
		public string? Id { get; } = id;
		public Block? Parent { get; } = parent;
		public List<Block> Children { get; } = [];
		public int BodyEnd { get; private set; }
		public int End { get; private set; }

		public void Close(int bodyEnd, int end)
		{
			BodyEnd = bodyEnd;
			End = end;
		}
	}
}
