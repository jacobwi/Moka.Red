using System.Text.RegularExpressions;

namespace Moka.Red.Feedback.Tests.Conventions;

/// <summary>
///     A class handed to a child component through <c>Class="..."</c> lands on the child's element, which carries the
///     child's scope, not this component's. Blazor adds this component's scope attribute to a rule's subject, so a rule
///     that names such a class as its subject never matches unless <c>::deep</c> comes before it. Nine rules sat in the
///     stylesheets that way, doing nothing.
/// </summary>
public sealed partial class ChildComponentClassRulesTests
{
	[Fact]
	public void ARuleOnAClassHandedToAChildComponent_ReachesItThroughDeep()
	{
		List<string> found = [];
		string src = FindSrc();
		foreach (string razor in Directory.EnumerateFiles(src, "*.razor", SearchOption.AllDirectories))
		{
			string css = razor + ".css";
			if (!File.Exists(css) || IsBuildOutput(razor))
			{
				continue;
			}

			string markup = RazorComment().Replace(File.ReadAllText(razor), " ");
			HashSet<string> own = [.. ClassTokens(OwnElementClass().Matches(markup))];
			HashSet<string> handed = [.. ClassTokens(ChildComponentClass().Matches(markup)).Where(name => !own.Contains(name))];
			if (handed.Count == 0)
			{
				continue;
			}

			string stylesheet = CssComment().Replace(File.ReadAllText(css), " ");
			foreach (Match rule in RuleHead().Matches(stylesheet))
			{
				string head = rule.Groups[1].Value.Trim();
				if (head.StartsWith('@'))
				{
					continue;
				}

				foreach (string selector in TopLevel(head, ','))
				{
					if (selector.Contains("::deep", StringComparison.Ordinal))
					{
						continue;
					}

					string subject = WithoutArguments(LastCompound(selector));
					foreach (string name in handed)
					{
						if (Regex.IsMatch(subject, $@"\.{Regex.Escape(name)}(?![\w-])"))
						{
							found.Add($"{Path.GetRelativePath(src, css)}: {selector}");
						}
					}
				}
			}
		}

		Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
	}

	private static IEnumerable<string> ClassTokens(MatchCollection attributes) =>
		attributes
			.SelectMany(match => match.Groups["value"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
			.Where(token => ClassName().IsMatch(token));

	private static string LastCompound(string selector)
	{
		int depth = 0;
		int start = 0;
		for (int i = 0; i < selector.Length; i++)
		{
			char c = selector[i];
			if (c == '(' || c == '[')
			{
				depth++;
			}
			else if (c == ')' || c == ']')
			{
				depth--;
			}
			else if (depth == 0 && (char.IsWhiteSpace(c) || c is '>' or '+' or '~'))
			{
				start = i + 1;
			}
		}

		return selector[start..].Trim();
	}

	private static string WithoutArguments(string compound)
	{
		int depth = 0;
		System.Text.StringBuilder kept = new();
		foreach (char c in compound)
		{
			if (c == '(')
			{
				depth++;
			}
			else if (c == ')')
			{
				depth--;
			}
			else if (depth == 0)
			{
				kept.Append(c);
			}
		}

		return kept.ToString();
	}

	private static IEnumerable<string> TopLevel(string list, char separator)
	{
		int depth = 0;
		int start = 0;
		for (int i = 0; i < list.Length; i++)
		{
			switch (list[i])
			{
				case '(':
					depth++;
					break;
				case ')':
					depth--;
					break;
				default:
					if (list[i] == separator && depth == 0)
					{
						yield return list[start..i].Trim();
						start = i + 1;
					}

					break;
			}
		}

		yield return list[start..].Trim();
	}

	private static bool IsBuildOutput(string path) =>
		path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
		|| path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

	private static string FindSrc()
	{
		for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
		{
			string candidate = Path.Combine(dir.FullName, "src");
			if (Directory.Exists(Path.Combine(candidate, "Moka.Red.Core")))
			{
				return candidate;
			}
		}

		throw new DirectoryNotFoundException("Could not find the src folder above the test output.");
	}

	[GeneratedRegex(@"@\*.*?\*@", RegexOptions.Singleline)]
	private static partial Regex RazorComment();

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex CssComment();

	// A component tag starts with a capital letter; its Class attribute holds the classes it hands on.
	[GeneratedRegex(@"<[A-Z][\w.]*(?:<[^>]*>)?\b[^>]*?\sClass=""(?<value>[^""]*)""", RegexOptions.Singleline)]
	private static partial Regex ChildComponentClass();

	[GeneratedRegex(@"<[a-z][\w-]*\b[^>]*?\sclass=""(?<value>[^""]*)""", RegexOptions.Singleline)]
	private static partial Regex OwnElementClass();

	[GeneratedRegex(@"^[a-z][\w-]*$")]
	private static partial Regex ClassName();

	[GeneratedRegex(@"([^{}]+)\{")]
	private static partial Regex RuleHead();
}
