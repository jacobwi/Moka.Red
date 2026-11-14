using System.Text.RegularExpressions;

namespace Moka.Red.Feedback.Tests.Conventions;

// The transition tokens hold a duration and an easing ("150ms ease"), so a second easing after one makes the whole
// transition or animation declaration invalid, and the browser drops it: the accordion, panel, knob, gauge and
// others never animated.
public sealed partial class TransitionTokenTests
{
	[Fact]
	public void NoTransitionToken_IsFollowedByASecondEasing()
	{
		string src = FindSrc();
		List<string> wrong = [];
		foreach (string file in Directory.EnumerateFiles(src, "*.css", SearchOption.AllDirectories))
		{
			string relative = Path.GetRelativePath(src, file);
			if (IsBuildOutput(relative))
			{
				continue;
			}

			string[] lines = File.ReadAllLines(file);
			for (int i = 0; i < lines.Length; i++)
			{
				if (TokenThenEasing().IsMatch(lines[i]))
				{
					wrong.Add($"{relative}:{i + 1}: {lines[i].Trim()}");
				}
			}
		}

		Assert.Empty(wrong);
	}

	private static bool IsBuildOutput(string relative) =>
		relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
		relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

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

	[GeneratedRegex(@"var\(--moka-transition-(fast|normal|slow)\)\s+(ease|linear|cubic-bezier|steps)")]
	private static partial Regex TokenThenEasing();
}
