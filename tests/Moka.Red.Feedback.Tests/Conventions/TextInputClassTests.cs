using System.Text.RegularExpressions;

namespace Moka.Red.Feedback.Tests.Conventions;

/// <summary>
///     moka.css ends a placeholder too long for its field in an ellipsis, on every input whose class starts with
///     <c>moka-</c>, so an app's own inputs are left alone. A library text input without such a class would be cut
///     mid-word again.
/// </summary>
public sealed partial class TextInputClassTests
{
	private static readonly string[] NotText = ["checkbox", "radio", "range", "color", "file", "hidden"];

	[Fact]
	public void EveryTextInput_HasAMokaClass()
	{
		List<string> missing = [];
		string src = FindSrc();
		foreach (string file in Directory.EnumerateFiles(src, "*.razor", SearchOption.AllDirectories))
		{
			string relative = Path.GetRelativePath(src, file);
			if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
			{
				continue;
			}

			string text = File.ReadAllText(file);
			foreach (Match input in Input().Matches(text))
			{
				string tag = input.Groups[1].Value;
				Match type = Type().Match(tag);
				if (type.Success && NotText.Contains(type.Groups[1].Value, StringComparer.Ordinal))
				{
					continue;
				}

				Match cls = Class().Match(tag);
				bool built = cls.Success && cls.Groups[1].Value.StartsWith('@');
				if (!built && !(cls.Success && cls.Groups[1].Value.Contains("moka-", StringComparison.Ordinal)))
				{
					int line = text[..input.Index].Count(c => c == '\n') + 1;
					missing.Add($"{relative}:{line}");
				}
			}
		}

		Assert.Empty(missing);
	}

	[Fact]
	public void MokaCss_EndsALongPlaceholderInAnEllipsis()
	{
		string css = File.ReadAllText(Path.Combine(FindSrc(), "Moka.Red.Core", "wwwroot", "moka.css"));
		const string selector =
			":where(input[class*=\"moka-\"]:not([type=\"checkbox\"], [type=\"radio\"], [type=\"range\"], [type=\"color\"], [type=\"file\"], [type=\"hidden\"])) {";

		int rule = css.IndexOf(selector, StringComparison.Ordinal);
		Assert.True(rule >= 0);
		Assert.Contains("text-overflow: ellipsis;", css[rule..css.IndexOf('}', rule)], StringComparison.Ordinal);
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

	[GeneratedRegex(@"<input\b(.*?)/?>", RegexOptions.Singleline)]
	private static partial Regex Input();

	[GeneratedRegex(@"type=""([^""]+)""")]
	private static partial Regex Type();

	[GeneratedRegex(@"class=""([^""]*)""")]
	private static partial Regex Class();
}
