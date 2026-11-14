using System.Text.RegularExpressions;

namespace Moka.Red.Layout.Tests.Components;

// The looks are the new design's: every rule that names one sits in a stylesheet's Gate block under .moka-gate, so the
// original design draws Classic whatever a layout asks for, and Classic itself is the rules above the blocks.
public sealed partial class MokaDockLookRulesTests
{
	private static readonly string[] Sheets =
	[
		"MokaDockLayout.razor.css",
		"MokaDockFrame.razor.css",
		"MokaDockGroupView.razor.css",
		"MokaDockPanel.razor.css",
		"MokaDockContent.razor.css"
	];

	public static TheoryData<string> Looks => new() { "ide", "cards", "minimal", "terminal" };

	[Theory]
	[MemberData(nameof(Looks))]
	public void EveryLookRule_IsUnderTheGateClass(string look)
	{
		List<string> found = [];
		foreach (string sheet in Sheets)
		{
			foreach (Match rule in Rule().Matches(Read(sheet)))
			{
				foreach (string selector in rule.Groups[1].Value.Split(',').Select(s => s.Trim()))
				{
					if (selector.Contains("moka-dock-layout--" + look, StringComparison.Ordinal)
					    && !selector.StartsWith(".moka-gate ", StringComparison.Ordinal))
					{
						found.Add($"{sheet}: {selector}");
					}
				}
			}
		}

		Assert.Empty(found);
		Assert.Contains(Sheets, sheet => Read(sheet).Contains("moka-dock-layout--" + look, StringComparison.Ordinal));
	}

	// A layout nested in a panel lives in the outer layout's hosts, never in its main area, so a look reaches only its
	// own frame by anchoring on .moka-dock-main.
	[Theory]
	[MemberData(nameof(Looks))]
	public void LookRules_ReachOnlyTheirOwnLayout(string look)
	{
		foreach (string sheet in Sheets)
		{
			foreach (Match rule in Rule().Matches(Read(sheet)))
			{
				foreach (string selector in rule.Groups[1].Value.Split(',').Select(s => s.Trim()))
				{
					int at = selector.IndexOf("moka-dock-layout--" + look, StringComparison.Ordinal);
					if (at < 0)
					{
						continue;
					}

					string rest = selector[(at + ("moka-dock-layout--" + look).Length)..].TrimStart();
					Assert.True(rest.Length == 0 || rest.StartsWith('>'), $"{sheet}: {selector}");
				}
			}
		}
	}

	[Fact]
	public void TheHosts_AreDrawnOverTheirSlots()
	{
		string panel = Read("MokaDockPanel.razor.css");

		Assert.Contains("top: anchor(top);", panel, StringComparison.Ordinal);
		Assert.Contains("left: anchor(left);", panel, StringComparison.Ordinal);
		Assert.Contains("anchor-scope: all;", Read("MokaDockLayout.razor.css"), StringComparison.Ordinal);
	}

	private static string Read(string sheet) =>
		Comment().Replace(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", sheet)).ReplaceLineEndings("\n"), " ");

	[GeneratedRegex(@"([^{}]+)\{[^{}]*\}")]
	private static partial Regex Rule();

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();
}
