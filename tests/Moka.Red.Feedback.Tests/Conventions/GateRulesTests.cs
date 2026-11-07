using System.Text.RegularExpressions;

namespace Moka.Red.Feedback.Tests.Conventions;

/// <summary>
///     Each component's Gate look is a block at the end of its stylesheet, with every selector under
///     <c>.moka-gate</c>, so the rules above it, which are the Matrix look, never move. Every value in the block
///     has to be a token, because three presets read those tokens at different sizes and colors.
/// </summary>
public sealed partial class GateRulesTests
{
	private const string Marker = "/* Gate look.";

	private static readonly string[] DevTools = ["Moka.Red.Diagnostics", "Moka.Red.ThemeGen"];

	[Fact]
	public void EverySelectorInAGateBlock_IsUnderMokaGate()
	{
		List<string> found = [];
		foreach ((string file, string block) in GateBlocks())
		{
			string withoutComments = Comment().Replace(block, " ");
			foreach (Match rule in RuleHead().Matches(withoutComments))
			{
				string head = rule.Groups[1].Value;
				if (head.TrimStart().StartsWith('@'))
				{
					continue;
				}

				foreach (string selector in TopLevelSelectors(head))
				{
					if (!selector.StartsWith(".moka-gate", StringComparison.Ordinal) && !IsKeyframeStep(selector))
					{
						found.Add($"{file}: {selector}");
					}
				}
			}
		}

		Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
	}

	// Blazor puts the scope attribute on what sits before ::deep. On .moka-gate it can never match.
	[Fact]
	public void NoGateRule_PutsDeepRightAfterMokaGate()
	{
		List<string> found = [.. GateBlocks().Where(b => DeepOnGate().IsMatch(b.Block)).Select(b => b.File)];

		Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
	}

	[Fact]
	public void NoGateRule_HardCodesAColor()
	{
		List<string> found = [];
		foreach ((string file, string block) in GateBlocks())
		{
			foreach (Match match in HardCodedColor().Matches(Comment().Replace(block, " ")))
			{
				found.Add($"{file}: {match.Value}");
			}
		}

		Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
	}

	// 1px borders and 2px offsets are fine. Anything bigger has to follow the theme, so it reads a token.
	[Fact]
	public void NoGateRule_HardCodesAPixelSize()
	{
		List<string> found = [];
		foreach ((string file, string block) in GateBlocks())
		{
			foreach (Match match in LargePixelSize().Matches(Comment().Replace(block, " ")))
			{
				found.Add($"{file}: {match.Value}");
			}
		}

		Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
	}

	[Fact]
	public void TheCoreComponents_HaveAGateBlock()
	{
		string[] names = [.. GateBlocks().Select(b => Path.GetFileName(b.File))];

		foreach (string expected in new[]
		         {
			         "MokaButton.razor.css", "MokaTextField.razor.css", "MokaCard.razor.css", "MokaTable.razor.css",
			         "MokaDialog.razor.css", "MokaMenu.razor.css", "MokaTag.razor.css", "MokaAlert.razor.css"
		         })
		{
			Assert.Contains(expected, names);
		}
	}

	// moka-hover.js keeps one highlight: the row under the mouse or the keyboard's, whichever moved last. A rule
	// that lights either kind of row without reading data-moka-pointer draws two.
	[Fact]
	public void AListWithOneHighlight_KeepsItInTheGateBlock()
	{
		string src = FindSrc();
		int lists = 0;
		List<string> found = [];
		foreach ((string file, string block) in GateBlocks())
		{
			string text = File.ReadAllText(Path.Combine(src, file));
			string matrix = text[..text.IndexOf(Marker, StringComparison.Ordinal)];
			Match mouseRow = MouseRow().Match(matrix);
			Match keyboardRow = KeyboardRow().Match(matrix);
			if (!mouseRow.Success || !keyboardRow.Success)
			{
				continue;
			}

			lists++;
			string hovered = $"{mouseRow.Groups[1].Value}:hover";
			string focused = keyboardRow.Groups[1].Value;
			foreach (Match rule in RuleHead().Matches(Comment().Replace(block, " ")))
			{
				found.AddRange(TopLevelSelectors(rule.Groups[1].Value)
					.Where(s => s.Contains(hovered, StringComparison.Ordinal) || s.Contains(focused, StringComparison.Ordinal))
					.Where(s => !s.Contains("data-moka-pointer", StringComparison.Ordinal))
					.Select(s => $"{file}: {s}"));
			}
		}

		Assert.True(lists > 0, "No stylesheet reads data-moka-pointer any more, so this test checks nothing.");
		Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
	}

	// A comma inside :not() or :is() does not separate selectors.
	private static IEnumerable<string> TopLevelSelectors(string head)
	{
		int depth = 0, start = 0;
		for (int i = 0; i < head.Length; i++)
		{
			switch (head[i])
			{
				case '(':
					depth++;
					break;
				case ')':
					depth--;
					break;
				case ',' when depth == 0:
					yield return head[start..i].Trim();
					start = i + 1;
					break;
			}
		}

		yield return head[start..].Trim();
	}

	private static bool IsKeyframeStep(string selector) =>
		selector is "from" or "to" || selector.EndsWith('%');

	private static IEnumerable<(string File, string Block)> GateBlocks()
	{
		string src = FindSrc();
		foreach (string file in Directory.EnumerateFiles(src, "*.razor.css", SearchOption.AllDirectories))
		{
			string relative = Path.GetRelativePath(src, file);
			if (DevTools.Any(tool => relative.StartsWith(tool, StringComparison.Ordinal)) ||
			    relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
			    relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
			{
				continue;
			}

			string text = File.ReadAllText(file);
			int start = text.IndexOf(Marker, StringComparison.Ordinal);
			if (start >= 0)
			{
				yield return (relative, text[start..]);
			}
		}
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

	[GeneratedRegex(@"([^{}]+)\{")]
	private static partial Regex RuleHead();

	[GeneratedRegex(@"\[data-moka-pointer\]\s+(\.[\w-]+):hover")]
	private static partial Regex MouseRow();

	[GeneratedRegex(@":not\(\[data-moka-pointer\]\)\s+(\.[\w-]+)")]
	private static partial Regex KeyboardRow();

	[GeneratedRegex(@"\.moka-gate\s+::deep")]
	private static partial Regex DeepOnGate();

	[GeneratedRegex(@":[^;{}]*?(#[0-9a-fA-F]{3,8}\b|rgba?\()")]
	private static partial Regex HardCodedColor();

	[GeneratedRegex(@"(?<![\d.])([3-9]|\d{2,})px\b")]
	private static partial Regex LargePixelSize();
}
