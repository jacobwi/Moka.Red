using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Moka.Red.Feedback.Tests.Conventions;

/// <summary>
///     An icon with no visible label is named by a MokaTooltip and keeps an aria-label with the same name. A native
///     <c>title</c> on one brings the browser's own tooltip back beside Moka's: a pale box that turns up a second
///     late. This reads every <c>.razor</c> file under src the way Razor does (attribute values hold C# with quotes,
///     brackets and lambdas of their own) and checks every <c>button</c> and <c>MokaButton</c> it draws.
/// </summary>
/// <remarks>
///     A button is icon-only when nothing in it reads as a label: no letter or digit in its text (an arrow or a cross
///     drawn as a character is a glyph, not a label) and no Razor expression that could print one. An expression that
///     names an icon (<c>@(IconProvider?.CloseIcon)</c>), a badge's count and visually hidden text are not labels.
///     Content the reader cannot see into (<c>@ChildContent</c>, <c>@{ RenderInner(); }</c>) counts as a label, so
///     the test never fails on a button that might show text; those are covered by the components' own tests.
///     The one case the rule allows is Moka.Red.Diagnostics: a development tool that references only Core, draws in
///     its own fixed colours and keeps its titles.
/// </remarks>
public sealed partial class IconTooltipMarkupTests
{
	private static readonly string[] AllowedFolders = ["Moka.Red.Diagnostics"];

	[Fact]
	public void NoIconOnlyButton_CarriesATitle()
	{
		List<string> found = [];
		foreach ((string file, ButtonFinding button) in LibraryButtons())
		{
			if (button.IconOnly && button.HasTitle)
			{
				found.Add($"{file}:{button.Line}");
			}
		}

		Assert.Empty(found);
	}

	[Fact]
	public void EveryIconOnlyButton_IsNamedAndSitsInATooltip()
	{
		List<string> found = [];
		foreach ((string file, ButtonFinding button) in LibraryButtons())
		{
			if (button.IconOnly && (!button.HasName || !button.InTooltip))
			{
				found.Add($"{file}:{button.Line}{(button.HasName ? "" : " has no aria-label")}{(button.InTooltip ? "" : " is not in a MokaTooltip")}");
			}
		}

		Assert.Empty(found);
	}

	[Theory]
	[InlineData("""<button title="Close"><MokaIcon Icon="X"/></button>""", true, true, false)]
	[InlineData("""<button title="Close details">Close</button>""", false, true, false)]
	[InlineData("""<button @onclick="() => GoToPage(_page - 1)" title="Previous page"><MokaIcon Icon="X"/></button>""", true, true, false)]
	[InlineData("""<button title="@(_copied ? "Copied!" : "Copy")">@(_copied ? "Copied!" : "Copy")</button>""", false, true, false)]
	[InlineData("""<button @onclick="PrevMonth" type="button">&lt;</button>""", true, false, false)]
	[InlineData("""<button aria-label="Close">&#x2715;</button>""", true, false, false)]
	[InlineData("""<button>@if (_dark) { <MokaIcon Icon="A"/> } else { <MokaIcon Icon="B"/> }</button>""", true, false, false)]
	[InlineData("""<button aria-label="Close">@(IconProvider?.CloseIcon)</button>""", true, false, false)]
	[InlineData("""<button aria-label="Close"><MokaIcon Icon="X"/><span class="moka-visually-hidden">Close</span></button>""", true, false, false)]
	[InlineData("""<button aria-label="Inbox"><MokaIcon Icon="X"/>@if (n > 0) { <span class="moka-x-badge">@n</span> }</button>""", true, false, false)]
	[InlineData("""<button>@pageNum</button>""", false, false, false)]
	[InlineData("""<button>@{ RenderInner(); }</button>""", false, false, false)]
	[InlineData("""<button><MokaIcon Icon="X"/> Export</button>""", false, false, false)]
	[InlineData("""<button class="swatch" style="@SwatchStyle(color)"></button>""", true, false, false)]
	[InlineData("""<button aria-label="@($"Remove {tag} (\"{x}\")")" title=")"><MokaIcon Icon="X"/></button>""", true, true, false)]
	[InlineData("""<MokaButton StartIcon="X" title="Refresh" />""", true, true, false)]
	[InlineData("""<MokaButton StartIcon="X">Save</MokaButton>""", false, false, false)]
	[InlineData("""<MokaTooltip Text="Close"><button aria-label="Close"><MokaIcon Icon="X"/></button></MokaTooltip>""", true, false, true)]
	[InlineData("""<MokaTooltip Text="Hi" /><button aria-label="Close"><MokaIcon Icon="X"/></button>""", true, false, false)]
	public void TheReader_TellsIconOnlyButtonsTitlesAndTooltipsApart(string markup, bool iconOnly, bool hasTitle, bool inTooltip)
	{
		ButtonFinding button = Scan(markup)[0];

		Assert.Equal(iconOnly, button.IconOnly);
		Assert.Equal(hasTitle, button.HasTitle);
		Assert.Equal(inTooltip, button.InTooltip);
	}

	// A button inside a chip's own button: the outer one shows the chip's text, the inner one only a cross.
	[Fact]
	public void TheReader_ReadsANestedButtonOnItsOwn()
	{
		List<ButtonFinding> buttons = Scan("""
			<button class="chip">@Text
				<MokaTooltip Text="Remove"><button aria-label="Remove"><MokaIcon Icon="X"/></button></MokaTooltip>
			</button>
			""");

		Assert.Equal(2, buttons.Count);
		Assert.False(buttons[0].IconOnly);
		Assert.True(buttons[1].IconOnly);
		Assert.True(buttons[1].HasName);
		Assert.True(buttons[1].InTooltip);
		Assert.Equal(2, buttons[1].Line);
	}

	// Commented-out markup is not drawn.
	[Fact]
	public void TheReader_SkipsComments()
	{
		Assert.Empty(Scan("""@* <button title="x"><MokaIcon Icon="X"/></button> *@ <!-- <button title="y"></button> -->"""));
	}

	private static List<(string File, ButtonFinding Button)> LibraryButtons()
	{
		string src = Path.Combine(RepositoryRoot(), "src");
		int read = 0;
		List<(string File, ButtonFinding Button)> buttons = [];
		foreach (string file in Directory.EnumerateFiles(src, "*.razor", SearchOption.AllDirectories))
		{
			string relative = Path.GetRelativePath(src, file);
			string[] parts = relative.Split(Path.DirectorySeparatorChar);
			if (parts.Any(part => part is "bin" or "obj"))
			{
				continue;
			}

			List<ButtonFinding> found = Scan(File.ReadAllText(file));
			read += found.Count;
			if (AllowedFolders.Contains(parts[0], StringComparer.Ordinal))
			{
				continue;
			}

			buttons.AddRange(found.Select(button => (relative, button)));
		}

		// Well over a hundred buttons, so an empty folder or a wrong root cannot pass for clean markup.
		Assert.True(read > 100, $"Only {read} buttons were read under {src}.");
		return buttons;
	}

	/// <summary>Every button the markup draws, in order, with what the rule asks about it.</summary>
	private static List<ButtonFinding> Scan(string markup)
	{
		string text = WithoutComments(markup);
		List<(int Start, int End)> tooltips = Elements(text, "MokaTooltip");
		List<ButtonFinding> buttons = [];
		foreach (Match match in ButtonStart().Matches(text))
		{
			Tag tag = ReadTag(text, match.Index);
			string content = "";
			if (!tag.SelfClosing)
			{
				int close = MatchingClose(text, tag.End, tag.Name);
				content = text[tag.End..close];
			}

			bool hasIcon = tag.Attributes.ContainsKey("StartIcon") || tag.Attributes.ContainsKey("EndIcon") ||
			               tag.Attributes.ContainsKey("Icon");
			bool iconOnly = tag.Name == "button" ? !HasVisibleLabel(content) : hasIcon && string.IsNullOrWhiteSpace(content);
			buttons.Add(new ButtonFinding(
				LineOf(text, match.Index),
				iconOnly,
				tag.Attributes.ContainsKey("title"),
				tag.Attributes.ContainsKey("aria-label") || tag.Attributes.ContainsKey("aria-labelledby"),
				tooltips.Any(tooltip => tooltip.Start < match.Index && match.Index < tooltip.End)));
		}

		return buttons;
	}

	// Razor and HTML comments become spaces, so every index and line number stays where it was.
	private static string WithoutComments(string text)
	{
		StringBuilder result = new(text);
		foreach (Match comment in Comment().Matches(text))
		{
			for (int i = comment.Index; i < comment.Index + comment.Length; i++)
			{
				if (result[i] is not ('\n' or '\r'))
				{
					result[i] = ' ';
				}
			}
		}

		return result.ToString();
	}

	// The extent of every element with this name that has content, from its start tag to the end of its close tag.
	private static List<(int Start, int End)> Elements(string text, string name)
	{
		List<(int, int)> elements = [];
		foreach (Match match in Regex.Matches(text, $"<{name}\\b"))
		{
			Tag tag = ReadTag(text, match.Index);
			if (!tag.SelfClosing)
			{
				elements.Add((match.Index, MatchingClose(text, tag.End, name)));
			}
		}

		return elements;
	}

	// Where the element that starts before 'from' ends: the close tag that matches it, counting elements of the same
	// name opened in between. The end of the text when there is none.
	private static int MatchingClose(string text, int from, string name)
	{
		int depth = 0;
		int i = from;
		while (i < text.Length)
		{
			int open = text.IndexOf($"<{name}", i, StringComparison.Ordinal);
			int close = text.IndexOf($"</{name}>", i, StringComparison.Ordinal);
			if (close < 0)
			{
				return text.Length;
			}

			if (open >= 0 && open < close && IsNameEnd(text, open + name.Length + 1))
			{
				Tag inner = ReadTag(text, open);
				depth += inner.SelfClosing ? 0 : 1;
				i = inner.End;
				continue;
			}

			if (depth == 0)
			{
				return close;
			}

			depth--;
			i = close + name.Length + 3;
		}

		return text.Length;
	}

	private static bool IsNameEnd(string text, int i) =>
		i >= text.Length || !(char.IsLetterOrDigit(text[i]) || text[i] is '-' or '_' or '.' or ':');

	private static bool HasVisibleLabel(string content)
	{
		string drawn = RemoveElements(content, static (name, tag) =>
			name == "button" ||
			(tag.Attributes.TryGetValue("class", out string? css) &&
			 (css.Contains("visually-hidden", StringComparison.Ordinal) || css.Contains("badge", StringComparison.Ordinal))));

		StringBuilder rest = new();
		int i = 0;
		while (i < drawn.Length)
		{
			if (drawn[i] == '<' && i + 1 < drawn.Length && (char.IsLetter(drawn[i + 1]) || drawn[i + 1] == '/'))
			{
				i = ReadTag(drawn, i).End;
				rest.Append(' ');
				continue;
			}

			rest.Append(drawn[i]);
			i++;
		}

		return RazorPrintsLabel(rest.ToString());
	}

	// Removes the elements the predicate picks, start tag to close tag, so neither their text nor their
	// expressions count.
	private static string RemoveElements(string content, Func<string, Tag, bool> remove)
	{
		StringBuilder result = new();
		int i = 0;
		while (i < content.Length)
		{
			if (content[i] == '<' && i + 1 < content.Length && char.IsLetter(content[i + 1]))
			{
				Tag tag = ReadTag(content, i);
				if (remove(tag.Name, tag))
				{
					i = tag.SelfClosing
						? tag.End
						: Math.Min(content.Length, MatchingClose(content, tag.End, tag.Name) + tag.Name.Length + 3);
					continue;
				}
			}

			result.Append(content[i]);
			i++;
		}

		return result.ToString();
	}

	// The text left once the tags are gone: Razor control flow prints nothing, an expression prints a label unless it
	// names an icon, a code block could print anything, and plain text is a label when it holds a letter or a digit.
	private static bool RazorPrintsLabel(string text)
	{
		StringBuilder literal = new();
		int i = 0;
		while (i < text.Length)
		{
			char c = text[i];
			if (c == '@' && i + 1 < text.Length && text[i + 1] == '@')
			{
				literal.Append('@');
				i += 2;
				continue;
			}

			if (c == '@' && i + 1 < text.Length && text[i + 1] == '{')
			{
				return true;
			}

			if (c == '@' && i + 1 < text.Length && text[i + 1] == '(')
			{
				int end = SkipCode(text, i + 2, ')');
				if (!NamesAnIcon(text[i..end]))
				{
					return true;
				}

				i = end;
				continue;
			}

			if (c == '@' && i + 1 < text.Length && char.IsLetter(text[i + 1]))
			{
				int start = i + 1;
				int end = start;
				while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_'))
				{
					end++;
				}

				string word = text[start..end];
				if (word is "if" or "foreach" or "for" or "while" or "switch")
				{
					i = SkipCondition(text, end);
					continue;
				}

				end = SkipChain(text, end);
				if (!NamesAnIcon(text[start..end]))
				{
					return true;
				}

				i = end;
				continue;
			}

			if (ElseKeyword().Match(text, i) is { Success: true } elseMatch && elseMatch.Index == i)
			{
				i = elseMatch.Groups["if"].Success ? SkipCondition(text, i + elseMatch.Length) : i + elseMatch.Length;
				continue;
			}

			if (c is not ('{' or '}'))
			{
				literal.Append(c);
			}

			i++;
		}

		return WebUtility.HtmlDecode(literal.ToString()).Any(char.IsLetterOrDigit);
	}

	private static bool NamesAnIcon(string expression) => expression.Contains("Icon", StringComparison.Ordinal);

	// Past the condition in brackets after a control keyword.
	private static int SkipCondition(string text, int i)
	{
		while (i < text.Length && char.IsWhiteSpace(text[i]))
		{
			i++;
		}

		return i < text.Length && text[i] == '(' ? SkipCode(text, i + 1, ')') : i;
	}

	// The rest of an implicit expression: members, calls and indexers, as in @item.Title or @Format(x).
	private static int SkipChain(string text, int i)
	{
		while (i < text.Length)
		{
			if (text[i] is '(' or '[')
			{
				i = SkipCode(text, i + 1, text[i] == '(' ? ')' : ']');
			}
			else if (text[i] == '.' && i + 1 < text.Length && char.IsLetter(text[i + 1]))
			{
				i++;
				while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
				{
					i++;
				}
			}
			else
			{
				return i;
			}
		}

		return i;
	}

	private static Tag ReadTag(string text, int at)
	{
		int i = at + 1;
		int nameStart = i;
		while (i < text.Length && !IsNameEnd(text, i))
		{
			i++;
		}

		string name = text[nameStart..i];
		Dictionary<string, string> attributes = new(StringComparer.Ordinal);
		while (i < text.Length)
		{
			while (i < text.Length && char.IsWhiteSpace(text[i]))
			{
				i++;
			}

			if (i >= text.Length)
			{
				break;
			}

			if (text[i] == '>')
			{
				return new Tag(name, i + 1, false, attributes);
			}

			if (text[i] == '/' && i + 1 < text.Length && text[i + 1] == '>')
			{
				return new Tag(name, i + 2, true, attributes);
			}

			int start = i;
			while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] is not ('=' or '>' or '/'))
			{
				i++;
			}

			string attribute = text[start..i];
			string value = "";
			if (i < text.Length && text[i] == '=')
			{
				i++;
				if (i < text.Length && text[i] is '"' or '\'')
				{
					int valueStart = i + 1;
					i = SkipValue(text, valueStart, text[i]);
					value = text[valueStart..Math.Max(valueStart, i - 1)];
				}
				else
				{
					int valueStart = i;
					while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] != '>')
					{
						i++;
					}

					value = text[valueStart..i];
				}
			}
			else if (i == start)
			{
				// A stray character; step over it rather than loop on it.
				i++;
			}

			if (attribute.Length > 0)
			{
				attributes[attribute] = value;
			}
		}

		return new Tag(name, text.Length, false, attributes);
	}

	// A quoted attribute value: text up to the closing quote, where @(...) and @name(...) hold C# whose own quotes
	// and brackets do not end the value.
	private static int SkipValue(string text, int i, char quote)
	{
		while (i < text.Length)
		{
			char c = text[i];
			if (c == quote)
			{
				return i + 1;
			}

			if (c == '@' && i + 1 < text.Length && text[i + 1] == '(')
			{
				i = SkipCode(text, i + 2, ')');
				continue;
			}

			if (c == '@' && i + 1 < text.Length && char.IsLetter(text[i + 1]))
			{
				i = SkipChain(text, i + 1 + LengthOfName(text, i + 1));
				continue;
			}

			i++;
		}

		return i;
	}

	private static int LengthOfName(string text, int i)
	{
		int start = i;
		while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
		{
			i++;
		}

		return i - start;
	}

	// C# up to the bracket that closes the one before i, stepping over strings, interpolations and characters.
	private static int SkipCode(string text, int i, char close)
	{
		int depth = 0;
		while (i < text.Length)
		{
			char c = text[i];
			if (c == '"')
			{
				i = SkipString(text, i);
				continue;
			}

			if (c == '\'')
			{
				i += i + 1 < text.Length && text[i + 1] == '\\' ? 4 : 3;
				continue;
			}

			if (c is '(' or '[' or '{')
			{
				depth++;
			}
			else if (c is ')' or ']' or '}')
			{
				if (depth == 0 && c == close)
				{
					return i + 1;
				}

				depth--;
			}

			i++;
		}

		return i;
	}

	private static int SkipString(string text, int i)
	{
		bool interpolated = i > 0 && text[i - 1] == '$';
		i++;
		while (i < text.Length)
		{
			char c = text[i];
			if (c == '\\')
			{
				i += 2;
				continue;
			}

			if (c == '"')
			{
				return i + 1;
			}

			if (interpolated && c == '{')
			{
				if (i + 1 < text.Length && text[i + 1] == '{')
				{
					i += 2;
					continue;
				}

				i = SkipCode(text, i + 1, '}');
				continue;
			}

			i++;
		}

		return i;
	}

	private static int LineOf(string text, int index) => text.AsSpan(0, index).Count('\n') + 1;

	private static string RepositoryRoot()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				return folder.FullName;
			}
		}

		Assert.Fail("The tests are not running inside the repository, so there are no .razor files to read.");
		return "";
	}

	[GeneratedRegex("<(button|MokaButton)\\b")]
	private static partial Regex ButtonStart();

	[GeneratedRegex(@"@\*.*?\*@|<!--.*?-->", RegexOptions.Singleline)]
	private static partial Regex Comment();

	[GeneratedRegex(@"\belse\b\s*(?<if>if\b)?")]
	private static partial Regex ElseKeyword();

	private sealed record ButtonFinding(int Line, bool IconOnly, bool HasTitle, bool HasName, bool InTooltip);

	private sealed record Tag(string Name, int End, bool SelfClosing, IReadOnlyDictionary<string, string> Attributes);
}
