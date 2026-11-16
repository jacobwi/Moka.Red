using System.Text.RegularExpressions;

namespace Moka.Red.Feedback.Tests.Conventions;

/// <summary>
///     Forced colors (Windows High Contrast) drops every box-shadow, so a control whose only focus mark is
///     <c>--moka-focus-ring</c> and whose outline is <c>none</c> shows no focus there at all. A transparent outline draws
///     nothing otherwise and takes the system's color under forced colors, so a focus rule that draws the ring declares an
///     outline of its own that isn't <c>none</c>. Leaving it out left the outline to whatever else matched: the page-wide
///     ring or the browser's, drawn beside the ring, or another rule's <c>none</c>.
/// </summary>
public sealed partial class FocusOutlineTests
{
	[Fact]
	public void AFocusRuleThatDrawsTheRing_KeepsAnOutlineForcedColorsCanShow()
	{
		List<string> found = [];
		string src = FindSrc();
		foreach (string file in Directory.EnumerateFiles(src, "*.razor.css", SearchOption.AllDirectories))
		{
			string relative = Path.GetRelativePath(src, file);
			if (relative.StartsWith("Moka.Red.Diagnostics", StringComparison.Ordinal) || IsBuildOutput(relative))
			{
				continue;
			}

			string css = Comment().Replace(File.ReadAllText(file), " ");
			foreach (Match rule in Rule().Matches(css))
			{
				string head = rule.Groups[1].Value;
				string body = rule.Groups[2].Value;
				if (!head.Contains(":focus", StringComparison.Ordinal) || PseudoElement().IsMatch(head) || !body.Contains("--moka-focus-ring", StringComparison.Ordinal))
				{
					continue;
				}

				if (!Outline().IsMatch(body) || NoOutline().IsMatch(body))
				{
					found.Add($"{relative}: {string.Join(' ', head.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))}");
				}
			}
		}

		Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
	}

	private static bool IsBuildOutput(string relative) =>
		relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
		|| relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

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

	[GeneratedRegex(@"::(?!deep)")]
	private static partial Regex PseudoElement();

	[GeneratedRegex(@"outline\s*:\s*(none|0)\s*(;|$)")]
	private static partial Regex NoOutline();

	[GeneratedRegex(@"(^|[;{\s])outline(-style|-width|-color)?\s*:")]
	private static partial Regex Outline();
}
