using System.Text.RegularExpressions;
using Moka.Red.Core.Theming;

namespace Moka.Red.Feedback.Tests.Conventions;

/// <summary>
///     Some padding tokens hold two values ("4px 10px"). Used inside a longer shorthand they shift every value after
///     them, and in a longhand such as <c>padding-left</c> they make the declaration invalid, so the browser drops it.
///     Both happened three times on 2026-11-07 (slash menu, dropdown item, panel header, form builder) without a test
///     noticing. Such a token can only stand alone in <c>padding</c>.
/// </summary>
public sealed partial class PaddingTokenTests
{
	private static readonly string[] Skipped = ["Moka.Red.Diagnostics", "obj", "bin"];

	private const string GateMarker = "/* Gate look.";

	[Fact]
	public void SeveralValuedPaddingTokens_AreKnown()
	{
		Assert.Contains("--moka-item-padding", SeveralValuedTokens(gateOnly: true));
		Assert.Contains("--moka-panel-padding", SeveralValuedTokens(gateOnly: true));
		Assert.Contains("--moka-card-padding", SeveralValuedTokens(gateOnly: false));
	}

	// A Gate block only ever runs on the new design's presets, where some tokens hold one value that holds two on
	// Dark and Light. A rule above the block runs on every preset.
	[Fact]
	public void ASeveralValuedPaddingToken_OnlyEverStandsAloneInPadding()
	{
		List<string> found = [];
		foreach (string file in Stylesheets())
		{
			string text = File.ReadAllText(file);
			int marker = text.IndexOf(GateMarker, StringComparison.Ordinal);
			if (marker < 0)
			{
				Check(file, text, SeveralValuedTokens(gateOnly: false), found);
			}
			else
			{
				Check(file, text[..marker], SeveralValuedTokens(gateOnly: false), found);
				Check(file, text[marker..], SeveralValuedTokens(gateOnly: true), found);
			}
		}

		Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
	}

	private static void Check(string file, string text, HashSet<string> tokens, List<string> found)
	{
		foreach (Match declaration in Declaration().Matches(Comment().Replace(text, " ")))
		{
			string property = declaration.Groups["property"].Value;
			string value = declaration.Groups["value"].Value.Trim();
			foreach (Match use in TokenUse().Matches(value))
			{
				if (!tokens.Contains(use.Groups["token"].Value))
				{
					continue;
				}

				bool alone = string.Equals(value.Replace("!important", "", StringComparison.Ordinal).Trim(), use.Value,
					StringComparison.Ordinal);
				bool shorthandOrToken = property == "padding" || property.StartsWith("--", StringComparison.Ordinal);
				if (!alone || !shorthandOrToken)
				{
					found.Add($"{Path.GetFileName(file)}: {property}: {value}");
				}
			}
		}
	}

	// Every preset at every density, so a token that holds two values on only one of them still counts.
	private static HashSet<string> SeveralValuedTokens(bool gateOnly)
	{
		MokaTheme[] presets = gateOnly
			? [MokaTheme.Red, MokaTheme.Gate, MokaTheme.Crisp, MokaTheme.Matrix]
			: [MokaTheme.Red, MokaTheme.Gate, MokaTheme.Crisp, MokaTheme.Matrix, MokaTheme.Dark, MokaTheme.Light];
		HashSet<string> tokens = new(StringComparer.Ordinal);
		foreach (MokaTheme preset in presets)
		{
			foreach (MokaTheme theme in Enum.GetValues<MokaDensity>().Select(preset.WithDensity).Append(preset))
			{
				foreach (Match variable in PaddingVariable().Matches(theme.ToCssVariables()))
				{
					if (TopLevelValues(variable.Groups["value"].Value) > 1)
					{
						tokens.Add(variable.Groups["token"].Value);
					}
				}
			}
		}

		return tokens;
	}

	// Spaces inside calc() or var() don't make a second value.
	private static int TopLevelValues(string value)
	{
		int depth = 0, count = 0;
		bool inValue = false;
		foreach (char c in value.Trim())
		{
			depth += c == '(' ? 1 : c == ')' ? -1 : 0;
			bool gap = depth == 0 && char.IsWhiteSpace(c);
			if (!gap && !inValue)
			{
				count++;
			}

			inValue = !gap;
		}

		return count;
	}

	private static IEnumerable<string> Stylesheets()
	{
		string src = FindSrc();
		IEnumerable<string> files = Directory.EnumerateFiles(src, "*.razor.css", SearchOption.AllDirectories)
			.Append(Path.Combine(src, "Moka.Red.Core", "wwwroot", "moka.css"));
		return files.Where(file => !Path.GetRelativePath(src, file)
			.Split(Path.DirectorySeparatorChar)
			.Any(part => Skipped.Contains(part, StringComparer.Ordinal)));
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

	[GeneratedRegex(@"(?<![\w-])(?<property>[a-z-]+)\s*:\s*(?<value>[^;{}]*var\(--moka-[\w-]+-padding[^;{}]*)(?=[;}])")]
	private static partial Regex Declaration();

	[GeneratedRegex(@"var\((?<token>--moka-[\w-]+-padding)(?:,[^)]*)?\)")]
	private static partial Regex TokenUse();

	[GeneratedRegex(@"(?<token>--moka-[\w-]+-padding)\s*:\s*(?<value>[^;]+);")]
	private static partial Regex PaddingVariable();
}
