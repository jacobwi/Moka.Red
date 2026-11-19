using System.Text.RegularExpressions;

namespace Moka.Red.Feedback.Tests.Conventions;

/// <summary>
///     A theme provider that follows the OS writes <c>moka-scheme-auto</c> on its root and adds <c>moka-dark</c> only once
///     the browser has answered, which a static page never does. So a rule on <c>.moka-dark</c> applies from the first
///     paint only through its twin: the same declarations on <c>.moka-scheme-auto</c>, in the same stylesheet, under
///     <c>@media (prefers-color-scheme: dark)</c>.
/// </summary>
public sealed partial class DarkSchemeTwinTests
{
	private const string DarkQuery = "@media (prefers-color-scheme: dark)";

	// Tokens for dark markup without a provider. A provider's root never matches them; it writes its own tokens.
	private static readonly string[] WithoutAProvider = [":root", ".moka-dark:not(.moka-root)"];

	[Fact]
	public void EveryRuleOnTheDarkClass_HasATwinUnderTheDarkMediaQuery()
	{
		int darkRules = 0;
		List<string> missing = [];
		foreach ((string file, List<CssRule> rules) in Stylesheets())
		{
			foreach (CssRule rule in rules.Where(r => r.Media.Count == 0 && DarkClass().IsMatch(r.Selectors)))
			{
				if (SelectorList(rule.Selectors).All(selector => WithoutAProvider.Contains(selector, StringComparer.Ordinal)))
				{
					continue;
				}

				darkRules++;
				string twinSelectors = DarkClass().Replace(rule.Selectors, ".moka-scheme-auto");
				bool found = rules.Any(r => r.Media.Count == 1 && r.Media[0] == DarkQuery
				                            && SelectorList(r.Selectors).SequenceEqual(SelectorList(twinSelectors))
				                            && r.Declarations.SequenceEqual(rule.Declarations));
				if (!found)
				{
					missing.Add($"{file}: {Squash(rule.Selectors)}");
				}
			}
		}

		Assert.True(darkRules >= 6, $"Only {darkRules} rules on .moka-dark were found, so the stylesheets weren't read.");
		Assert.True(missing.Count == 0, "No twin on .moka-scheme-auto under the dark media query:\n" + string.Join("\n", missing));
	}

	// The other way round: a twin that no longer matches a rule on .moka-dark would keep a look the class lost.
	[Fact]
	public void EveryTwin_MatchesARuleOnTheDarkClass()
	{
		List<string> orphans = [];
		foreach ((string file, List<CssRule> rules) in Stylesheets())
		{
			foreach (CssRule twin in rules.Where(r => AutoClass().IsMatch(r.Selectors)))
			{
				string original = AutoClass().Replace(twin.Selectors, ".moka-dark");
				bool found = twin.Media.Count == 1 && twin.Media[0] == DarkQuery
				             && rules.Any(r => r.Media.Count == 0
				                               && SelectorList(r.Selectors).SequenceEqual(SelectorList(original))
				                               && r.Declarations.SequenceEqual(twin.Declarations));
				if (!found)
				{
					orphans.Add($"{file}: {Squash(twin.Selectors)}");
				}
			}
		}

		Assert.True(orphans.Count == 0, "A rule on .moka-scheme-auto with no matching rule on .moka-dark:\n" + string.Join("\n", orphans));
	}

	private static IEnumerable<(string File, List<CssRule> Rules)> Stylesheets()
	{
		string src = FindSrc();
		foreach (string file in Directory.EnumerateFiles(src, "*.css", SearchOption.AllDirectories))
		{
			string relative = Path.GetRelativePath(src, file);
			if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
			    relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
			{
				continue;
			}

			yield return (relative, Parse(Comment().Replace(File.ReadAllText(file), " ")));
		}
	}

	// Style rules with the at-rules around them, outermost first. Enough of CSS for stylesheets that hold no braces in
	// strings or comments.
	private static List<CssRule> Parse(string css)
	{
		List<CssRule> rules = [];
		Stack<(string Prelude, int BodyStart)> open = new();
		int preludeStart = 0;
		for (int i = 0; i < css.Length; i++)
		{
			switch (css[i])
			{
				case '{':
					open.Push((Squash(css[preludeStart..i]), i + 1));
					preludeStart = i + 1;
					break;
				case '}' when open.Count > 0:
					(string prelude, int bodyStart) = open.Pop();
					if (!prelude.StartsWith('@'))
					{
						List<string> media = [.. open.Reverse().Select(o => o.Prelude)];
						rules.Add(new CssRule(prelude, media, Declarations(css[bodyStart..i])));
					}

					preludeStart = i + 1;
					break;
				case ';':
					preludeStart = i + 1;
					break;
			}
		}

		return rules;
	}

	private static List<string> Declarations(string body) =>
	[
		.. body.Split(';').Select(Squash).Where(declaration => declaration.Length > 0 && !declaration.Contains('{', StringComparison.Ordinal))
	];

	private static List<string> SelectorList(string list)
	{
		List<string> selectors = [];
		int depth = 0, start = 0;
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
				case ',' when depth == 0:
					selectors.Add(Squash(list[start..i]));
					start = i + 1;
					break;
			}
		}

		selectors.Add(Squash(list[start..]));
		return selectors;
	}

	private static string Squash(string text) => Space().Replace(text, " ").Trim();

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

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();

	[GeneratedRegex(@"\.moka-dark(?![\w-])")]
	private static partial Regex DarkClass();

	[GeneratedRegex(@"\.moka-scheme-auto(?![\w-])")]
	private static partial Regex AutoClass();

	[GeneratedRegex(@"\s+")]
	private static partial Regex Space();

	private sealed record CssRule(string Selectors, List<string> Media, List<string> Declarations);
}
