using Moka.Red.Core.Colors;
using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

// The ring a focused control draws was a 3 px glow of the accent at 22%, which measured 1.3 to 1.5:1 against the surface on
// every new-design preset. A focus indicator needs 3:1 against what is next to it (WCAG 1.4.11), so on the new design it is
// solid accent past a hairline of surface. The original design keeps its look.
public sealed class FocusRingTests
{
	private const double Indicator = 3;
	private const string Ring = "0 0 0 1px var(--moka-color-surface), 0 0 0 3px var(--moka-color-primary)";

	public static TheoryData<string, bool> NewDesign()
	{
		TheoryData<string, bool> data = [];
		foreach (string preset in new[] { "Red", "RedLight", "Gate", "GateLight", "Crisp", "CrispLight", "Matrix", "MatrixLight", "Ember", "EmberLight" })
		{
			data.Add(preset, false);
			data.Add(preset, true);
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(NewDesign))]
	public void TheRing_IsSolidAccent(string name, bool highContrast) =>
		Assert.Equal(Ring, Theme(name, highContrast).ToTokenMap()["--moka-focus-ring"]);

	[Theory]
	[MemberData(nameof(NewDesign))]
	public void TheAccent_Shows3To1_OnCardsPanelsAndThePage(string name, bool highContrast)
	{
		MokaPalette palette = Theme(name, highContrast).Palette;

		foreach (string ground in new[] { palette.Surface, palette.SurfaceVariant, palette.Background, palette.Surface2 })
		{
			double contrast = MokaContrast.Ratio(MokaCssColor.Parse(palette.Primary), MokaCssColor.Parse(ground));
			Assert.True(contrast >= Indicator, $"{name}: the ring {palette.Primary} on {ground} is {contrast:0.00}:1, needs {Indicator}:1.");
		}
	}

	[Fact]
	public void TheGlowSelection_KeepsItsInnerGlow_OnTopOfTheRing() =>
		Assert.Equal($"{Ring}, inset 0 0 12px var(--moka-color-primary-glow)",
			MokaTheme.Red.WithSelection(MokaSelection.Glow).ToTokenMap()["--moka-focus-ring"]);

	// The attached list look joins a focused field to its open list with the old soft glow, which is decoration there.
	[Fact]
	public void TheSoftGlow_KeepsTheOldRing() =>
		Assert.Equal("0 0 0 3px var(--moka-color-primary-focus)", MokaTheme.Red.ToTokenMap()["--moka-focus-glow"]);

	[Theory]
	[InlineData("Dark", "0 0 0 3px var(--moka-color-primary-glow), inset 0 0 12px var(--moka-color-primary-glow)")]
	[InlineData("Light", "0 0 0 3px var(--moka-color-primary-glow)")]
	public void TheOriginalDesign_KeepsItsRing(string name, string ring) =>
		Assert.Equal(ring, LabelColorTests.Preset(name).ToTokenMap()["--moka-focus-ring"]);

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
