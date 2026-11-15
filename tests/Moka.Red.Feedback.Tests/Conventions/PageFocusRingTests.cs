using System.Text.RegularExpressions;

namespace Moka.Red.Feedback.Tests.Conventions;

/// <summary>
///     On the new design moka.css draws a keyboard ring on every link, button and element with a tabindex. It weighed two
///     classes and came after the scoped styles, so it beat any component that turned its outline off at that weight or
///     less: a menu, list or grid whose lit item shows the keyboard drew a ring round itself (the menu that held focus had
///     one and its sub-menu didn't), a dialog focused as a whole drew one, and a select drew one round a box already lit.
///     The whole selector sits in <c>:where</c> now, so it weighs nothing and only reaches what sets no outline itself.
/// </summary>
public sealed partial class PageFocusRingTests
{
	private const string MokaCss = "Moka.Red.Core/wwwroot/moka.css";

	[Fact]
	public void TheRing_SitsWhollyInWhere_SoAnyRuleOfAComponentWins()
	{
		Dictionary<string, string> ring = Declarations(MokaCss, ":where(.moka-gate :is(a, button, summary, [tabindex]):focus-visible)");

		Assert.Equal("var(--moka-focus-width) solid var(--moka-color-primary)", ring["outline"]);
		Assert.Equal("2px", ring["outline-offset"]);
	}

	[Fact]
	public void NoFocusVisibleRule_InMokaCss_WeighsAClassOutsideWhere()
	{
		string css = Comment().Replace(File.ReadAllText(Path.Combine(FindSrc(), MokaCss)), " ");
		List<string> weighted = [];
		foreach (Match rule in Rule().Matches(css))
		{
			foreach (string selector in TopLevelSelectors(rule.Groups[1].Value))
			{
				if (selector.Contains(":focus-visible", StringComparison.Ordinal) && !selector.StartsWith(":where(", StringComparison.Ordinal)
					&& Declarations(MokaCss, selector).ContainsKey("outline"))
				{
					weighted.Add(selector);
				}
			}
		}

		Assert.Empty(weighted);
	}

	private static Dictionary<string, string> Declarations(string file, string selector)
	{
		string css = Comment().Replace(File.ReadAllText(Path.Combine(FindSrc(), file)), " ");
		Dictionary<string, string> found = new(StringComparer.Ordinal);
		foreach (Match rule in Rule().Matches(css))
		{
			if (!TopLevelSelectors(rule.Groups[1].Value).Contains(selector, StringComparer.Ordinal))
			{
				continue;
			}

			foreach (string declaration in rule.Groups[2].Value.Split(';'))
			{
				int colon = declaration.IndexOf(':', StringComparison.Ordinal);
				if (colon > 0)
				{
					found[declaration[..colon].Trim()] = declaration[(colon + 1)..].Trim();
				}
			}
		}

		return found;
	}

	private static IEnumerable<string> TopLevelSelectors(string list)
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
				case ',' when depth == 0:
					yield return list[start..i].Trim();
					start = i + 1;
					break;
			}
		}

		yield return list[start..].Trim();
	}

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

	[GeneratedRegex(@"([^{}]+)\{([^{}]*)\}")]
	private static partial Regex Rule();
}
