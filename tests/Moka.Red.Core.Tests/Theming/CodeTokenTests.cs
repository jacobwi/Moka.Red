using Moka.Red.Core.Colors;
using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

// Highlighted code is read as text, so every code color has to read on the code block's background in every preset,
// scheme and label tone: 4.5:1, and 3:1 for comments and punctuation, which step back on purpose.
public class CodeTokenTests
{
	private static readonly string[] Parts = ["text", "tag", "attribute", "string", "keyword", "type", "number", "directive"];

	private static readonly string[] Quiet = ["comment", "punctuation"];

	public static TheoryData<string, MokaLabelTone, bool> Themes()
	{
		TheoryData<string, MokaLabelTone, bool> data = [];
		foreach (string name in Presets.Keys)
		{
			foreach (MokaLabelTone tone in Enum.GetValues<MokaLabelTone>())
			{
				data.Add(name, tone, false);
				data.Add(name, tone, true);
			}
		}

		return data;
	}

	private static Dictionary<string, MokaTheme> Presets { get; } = new(StringComparer.Ordinal)
	{
		["Red"] = MokaTheme.Red, ["RedLight"] = MokaTheme.RedLight, ["Gate"] = MokaTheme.Gate, ["GateLight"] = MokaTheme.GateLight,
		["Crisp"] = MokaTheme.Crisp, ["CrispLight"] = MokaTheme.CrispLight, ["Matrix"] = MokaTheme.Matrix,
		["MatrixLight"] = MokaTheme.MatrixLight, ["Ember"] = MokaTheme.Ember, ["EmberLight"] = MokaTheme.EmberLight,
		["Dark"] = MokaTheme.Dark, ["Light"] = MokaTheme.Light
	};

	[Theory]
	[MemberData(nameof(Themes))]
	public void EveryCodeColor_ReadsOnTheCodeBlock(string name, MokaLabelTone tone, bool highContrast)
	{
		MokaTheme theme = Presets[name].WithLabelTone(tone);
		theme = highContrast ? theme.WithHighContrast() : theme;
		string ground = theme.Look == MokaLook.Gate ? "--moka-color-surface-2" : "--moka-color-surface-variant";
		Assert.True(theme.TryResolveColor(ground, out MokaSrgb background));

		foreach (string part in Parts)
		{
			AtLeast(theme, $"--moka-code-{part}", background, MokaContrast.TextRatio, name);
		}

		foreach (string part in Quiet)
		{
			AtLeast(theme, $"--moka-code-{part}", background, MokaContrast.LargeTextRatio, name);
		}
	}

	[Fact]
	public void TheNewDesign_DrawsCodeInItsOwnRoles()
	{
		IReadOnlyDictionary<string, string> tokens = MokaTheme.Red.ToTokenMap();

		Assert.Equal("var(--moka-color-on-surface)", tokens["--moka-code-text"]);
		Assert.Equal("var(--moka-color-primary-text)", tokens["--moka-code-tag"]);
		Assert.Equal("var(--moka-color-on-surface-2)", tokens["--moka-code-attribute"]);
		Assert.Equal("var(--moka-label-green-ink)", tokens["--moka-code-string"]);
		Assert.Equal("var(--moka-label-violet-ink)", tokens["--moka-code-keyword"]);
		Assert.Equal("var(--moka-label-cyan-ink)", tokens["--moka-code-type"]);
		Assert.Equal("var(--moka-label-amber-ink)", tokens["--moka-code-number"]);
		Assert.Equal("var(--moka-color-on-surface-tertiary)", tokens["--moka-code-comment"]);
		Assert.Equal("var(--moka-color-on-surface-tertiary)", tokens["--moka-code-punctuation"]);
		Assert.Equal("var(--moka-color-primary)", tokens["--moka-code-directive"]);
	}

	[Fact]
	public void TheOriginalDesign_DrawsTagsInTheAccentShadeThatReads()
	{
		Assert.Equal("var(--moka-color-primary-light)", MokaTheme.Dark.ToTokenMap()["--moka-code-tag"]);
		Assert.Equal("var(--moka-color-primary-dark)", MokaTheme.Light.ToTokenMap()["--moka-code-tag"]);
	}

	private static void AtLeast(MokaTheme theme, string token, MokaSrgb background, double target, string name)
	{
		Assert.True(theme.TryResolveColor(token, out MokaSrgb color), $"{name}: {token} doesn't resolve.");
		double ratio = MokaContrast.Ratio(color, background);
		Assert.True(ratio >= target, $"{name} ({theme.LabelTone}, high contrast {theme.HighContrast}): {token} {color.ToHex()} on {background.ToHex()} is {ratio:0.00}, needs {target}.");
	}
}
