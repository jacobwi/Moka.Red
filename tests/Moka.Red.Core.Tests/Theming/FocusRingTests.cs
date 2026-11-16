using Moka.Red.Core.Colors;
using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

// The ring a focused control draws was a 3 px glow of the accent, 22% on the new design and 8% on the original, which
// measured 1.1 to 1.5:1 against the surface. A focus indicator needs 3:1 against what is next to it (WCAG 1.4.11), so on
// every design it is solid accent past a hairline of surface, and a field in error rings in the error color.
public sealed class FocusRingTests
{
	private const double Indicator = 3;
	private const string Ring = "0 0 0 1px var(--moka-color-surface), 0 0 0 3px var(--moka-color-primary)";
	private const string ErrorRing = "0 0 0 1px var(--moka-color-surface), 0 0 0 3px var(--moka-color-error)";
	private const string InnerGlow = "inset 0 0 12px var(--moka-color-primary-glow)";

	public static TheoryData<string, bool> Presets()
	{
		TheoryData<string, bool> data = [];
		foreach (string preset in new[] { "Red", "RedLight", "Gate", "GateLight", "Crisp", "CrispLight", "Matrix", "MatrixLight", "Ember", "EmberLight", "Dark", "Light" })
		{
			data.Add(preset, false);
			data.Add(preset, true);
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(Presets))]
	public void TheRing_IsSolidAccent(string name, bool highContrast)
	{
		string ring = Theme(name, highContrast).ToTokenMap()["--moka-focus-ring"];

		Assert.Equal(name == "Dark" ? $"{Ring}, {InnerGlow}" : Ring, ring);
	}

	[Theory]
	[MemberData(nameof(Presets))]
	public void TheErrorRing_IsSolidError(string name, bool highContrast) =>
		Assert.Equal(ErrorRing, Theme(name, highContrast).ToTokenMap()["--moka-focus-ring-error"]);

	[Theory]
	[MemberData(nameof(Presets))]
	public void TheAccentAndError_Show3To1_OnCardsPanelsAndThePage(string name, bool highContrast)
	{
		MokaPalette palette = Theme(name, highContrast).Palette;

		foreach (string ring in new[] { palette.Primary, palette.Error })
		{
			foreach (string ground in new[] { palette.Surface, palette.SurfaceVariant, palette.Background, palette.Surface2 })
			{
				double contrast = MokaContrast.Ratio(MokaCssColor.Parse(ring), MokaCssColor.Parse(ground));
				Assert.True(contrast >= Indicator, $"{name}: the ring {ring} on {ground} is {contrast:0.00}:1, needs {Indicator}:1.");
			}
		}
	}

	[Fact]
	public void TheGlowSelection_KeepsItsInnerGlow_OnTopOfTheRing() =>
		Assert.Equal($"{Ring}, {InnerGlow}", MokaTheme.Red.WithSelection(MokaSelection.Glow).ToTokenMap()["--moka-focus-ring"]);

	// The attached list look joins a focused field to its open list with the old soft glow, which is decoration there.
	[Fact]
	public void TheSoftGlow_KeepsTheOldRing() =>
		Assert.Equal("0 0 0 3px var(--moka-color-primary-focus)", MokaTheme.Red.ToTokenMap()["--moka-focus-glow"]);

	private static MokaTheme Theme(string name, bool highContrast)
	{
		MokaTheme theme = name switch
		{
			"Ember" => MokaTheme.Ember,
			"EmberLight" => MokaTheme.EmberLight,
			_ => LabelColorTests.Preset(name)
		};
		return highContrast ? theme.WithHighContrast() : theme;
	}
}
