using System.Text.RegularExpressions;

namespace Moka.Red.Feedback.Tests.Conventions;

/// <summary>
///     A title's case and its letter spacing come from the same family of tokens, so a letter case that moves one
///     family moves both: uppercase never runs tight, and text as written never runs loose. Card titles read the card
///     tokens and headings the shared ones, which is what lets <c>MokaLetterCase.Controls</c> tell them apart.
/// </summary>
public sealed partial class TitleCaseTokenTests
{
	private static readonly Dictionary<string, string[]> TrackingFor = new(StringComparer.Ordinal)
	{
		["var(--moka-card-title-case)"] = ["var(--moka-card-title-tracking)", "var(--moka-chart-title-tracking)"],
		["var(--moka-title-case)"] = ["var(--moka-title-tracking)", "var(--moka-heading-tracking)"]
	};

	private static readonly string[] CardTitles =
	[
		"Moka.Red.Layout/Card/MokaCard.razor.css",
		"Moka.Red.Layout/Panel/MokaPanel.razor.css",
		"Moka.Red.Layout/DockLayout/MokaDockPanel.razor.css",
		"Moka.Red.Layout/GlassCard/MokaGlassCard.razor.css",
		"Moka.Red.Forms/SettingsForm/MokaSettingsForm.razor.css",
		"Moka.Red.Feedback/ErrorBoundary/MokaErrorBoundary.razor.css",
		"Moka.Red.Data/Chart/Parts/MokaChartFrame.razor.css"
	];

	private static readonly string[] Headings =
	[
		"Moka.Red.Layout/PageHeader/MokaPageHeader.razor.css",
		"Moka.Red.Feedback/Dialog/MokaDialog.razor.css",
		"Moka.Red.Feedback/Drawer/MokaDrawer.razor.css",
		"Moka.Red.Feedback/BottomSheet/MokaBottomSheet.razor.css",
		"Moka.Red.Feedback/Cheatsheet/MokaCheatsheet.razor.css",
		"Moka.Red.Feedback/Onboarding/MokaOnboarding.razor.css",
		"Moka.Red.Feedback/Reconnect/MokaReconnectModal.razor.css",
		"Moka.Red.Feedback/Alert/MokaAlert.razor.css"
	];

	[Fact]
	public void EveryTitleCase_ComesWithTrackingFromItsFamily()
	{
		List<string> wrong = [];
		foreach ((string file, string selector, Dictionary<string, string> declarations) in Rules())
		{
			if (!declarations.TryGetValue("text-transform", out string? textCase) || !TrackingFor.TryGetValue(textCase, out string[]? allowed))
			{
				continue;
			}

			string tracking = declarations.GetValueOrDefault("letter-spacing", "(none)");
			if (!allowed.Contains(tracking, StringComparer.Ordinal))
			{
				wrong.Add($"{file}: {selector} pairs {textCase} with {tracking}");
			}
		}

		Assert.Empty(wrong);
	}

	[Fact]
	public void CardTitles_ReadTheCardTokens()
	{
		foreach (string file in CardTitles)
		{
			string text = Read(file);
			Assert.Contains("var(--moka-card-title-case)", text, StringComparison.Ordinal);
			Assert.DoesNotContain("var(--moka-title-case)", text, StringComparison.Ordinal);
		}
	}

	[Fact]
	public void Headings_ReadTheSharedTokens()
	{
		foreach (string file in Headings)
		{
			string text = Read(file);
			Assert.Contains("var(--moka-title-case)", text, StringComparison.Ordinal);
			Assert.DoesNotContain("var(--moka-card-title-case)", text, StringComparison.Ordinal);
		}
	}

	private static string Read(string file) => File.ReadAllText(Path.Combine(FindSrc(), file));

	private static IEnumerable<(string File, string Selector, Dictionary<string, string> Declarations)> Rules()
	{
		string src = FindSrc();
		foreach (string file in Directory.EnumerateFiles(src, "*.razor.css", SearchOption.AllDirectories))
		{
			string relative = Path.GetRelativePath(src, file);
			if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
			    relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
			{
				continue;
			}

			string text = Comment().Replace(File.ReadAllText(file), "");
			foreach (Match rule in Rule().Matches(text))
			{
				Dictionary<string, string> declarations = new(StringComparer.Ordinal);
				foreach (string declaration in rule.Groups[2].Value.Split(';'))
				{
					int colon = declaration.IndexOf(':', StringComparison.Ordinal);
					if (colon > 0)
					{
						declarations[declaration[..colon].Trim()] = declaration[(colon + 1)..].Trim();
					}
				}

				yield return (relative, rule.Groups[1].Value.Trim(), declarations);
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

	[GeneratedRegex(@"([^{}]+)\{([^{}]*)\}")]
	private static partial Regex Rule();
}
