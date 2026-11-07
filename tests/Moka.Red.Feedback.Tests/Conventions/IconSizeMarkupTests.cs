using System.Text.RegularExpressions;

namespace Moka.Red.Feedback.Tests.Conventions;

/// <summary>
///     A <c>SizeValue</c> in the library's own markup carries its unit. The component adds a missing one, so this
///     is about the markup saying what it means.
/// </summary>
public sealed partial class IconSizeMarkupTests
{
	[Fact]
	public void NoSizeValue_IsABareNumber()
	{
		string src = FindSrc();
		List<string> found = [];
		foreach (string file in Directory.EnumerateFiles(src, "*.razor", SearchOption.AllDirectories))
		{
			foreach (Match match in BareSize().Matches(File.ReadAllText(file)))
			{
				found.Add($"{Path.GetRelativePath(src, file)}: {match.Value}");
			}
		}

		Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
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

	[GeneratedRegex("""SizeValue="[0-9.]+" """, RegexOptions.IgnorePatternWhitespace)]
	private static partial Regex BareSize();
}
