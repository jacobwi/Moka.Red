using Moka.Red.Core.Theming;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Theming;

public class GateThemeTests
{
	[Fact]
	public void Gate_IsTheDarkScheme()
	{
		Assert.True(MokaTheme.Gate.IsDark);
		Assert.Equal(MokaLook.Gate, MokaTheme.Gate.Look);
		Assert.Equal("#0A0E15", MokaTheme.Gate.Palette.Background);
		Assert.Equal("#6E92FF", MokaTheme.Gate.Palette.Primary);
	}

	[Fact]
	public void GateLight_IsTheLightScheme()
	{
		Assert.False(MokaTheme.GateLight.IsDark);
		Assert.Equal(MokaLook.Gate, MokaTheme.GateLight.Look);
		Assert.Equal("#F4F6FA", MokaTheme.GateLight.Palette.Background);
		Assert.Equal("#2F58D6", MokaTheme.GateLight.Palette.Primary);
	}

	[Fact]
	public void EveryToken_SurvivesItsOwnSafetyCheck()
	{
		List<string> expected = CssDeclarations.PropertyNames(MokaTheme.Dark.ToCssVariables());

		Assert.Equal(expected, CssDeclarations.PropertyNames(MokaTheme.Gate.ToCssVariables()));
		Assert.Equal(expected, CssDeclarations.PropertyNames(MokaTheme.GateLight.ToCssVariables()));
	}

	[Theory]
	[InlineData("--moka-color-surface-raised: #141B29")]
	[InlineData("--moka-color-outline-strong: #2C3850")]
	[InlineData("--moka-color-primary-bg: #172243")]
	[InlineData("--moka-color-primary-row: color-mix(in srgb, var(--moka-color-primary) 12%, var(--moka-color-background))")]
	[InlineData("--moka-color-success-bg: #13281F")]
	[InlineData("--moka-color-error-soft: #FF8A8F")]
	[InlineData("--moka-color-backdrop: rgba(6, 9, 14, 0.78)")]
	[InlineData("--moka-color-knob: #E7EBF3")]
	[InlineData("--moka-shadow-knob: none")]
	[InlineData("--moka-font-family-display: 'Space Grotesk', 'Inter', system-ui, sans-serif")]
	[InlineData("--moka-font-size-display: 1.875rem")]
	[InlineData("--moka-control-case: none")]
	[InlineData("--moka-control-tracking: 0")]
	[InlineData("--moka-title-case: none")]
	[InlineData("--moka-radius-md: 10px")]
	[InlineData("--moka-radius-xl: 20px")]
	[InlineData("--moka-radius-card: var(--moka-radius-xl)")]
	[InlineData("--moka-radius-popup: var(--moka-radius-lg)")]
	[InlineData("--moka-card-padding: 22px")]
	[InlineData("--moka-control-height: 40px")]
	[InlineData("--moka-main-padding: 28px 32px 120px")]
	[InlineData("--moka-shadow-1: none")]
	[InlineData("--moka-shadow-popup: 0 18px 40px rgba(0, 0, 0, 0.45)")]
	[InlineData("--moka-focus-ring: 0 0 0 3px var(--moka-color-primary-focus)")]
	[InlineData("--moka-transition-normal: 150ms ease-out")]
	[InlineData("--moka-opacity-disabled: 0.55")]
	public void Gate_WritesTheDesignsValues(string declaration) =>
		Assert.Contains(declaration + ";", MokaTheme.Gate.ToCssVariables(), StringComparison.Ordinal);

	[Theory]
	[InlineData("--moka-color-surface-raised: #FAFBFD")]
	[InlineData("--moka-color-primary-bg: #E6ECFE")]
	[InlineData("--moka-color-primary-row: color-mix(in srgb, var(--moka-color-primary) 9%, var(--moka-color-surface))")]
	[InlineData("--moka-color-success-bg: #E3F4EA")]
	[InlineData("--moka-color-paper-edge: var(--moka-color-outline-strong)")]
	[InlineData("--moka-shadow-1: 0 1px 2px rgba(22, 30, 48, 0.05)")]
	[InlineData("--moka-shadow-modal: 0 8px 28px rgba(22, 30, 48, 0.18)")]
	public void GateLight_WritesTheDesignsValues(string declaration) =>
		Assert.Contains(declaration + ";", MokaTheme.GateLight.ToCssVariables(), StringComparison.Ordinal);

	[Fact]
	public void DarkShadows_AreNotAccentRings()
	{
		string css = MokaTheme.Gate.WithAccent(MokaAccent.Cyan).ToCssVariables();

		Assert.DoesNotContain("0 0 0 1px rgba", css, StringComparison.Ordinal);
	}

	[Fact]
	public void WithDark_StaysInTheLook()
	{
		Assert.Equal(MokaPalette.GateLight, MokaTheme.Gate.WithDark(false).Palette);
		Assert.Equal(MokaPalette.Gate, MokaTheme.GateLight.WithDark(true).Palette);
		Assert.Equal(MokaLook.Gate, MokaTheme.Gate.WithDark(false).Look);
		Assert.Equal(MokaTypography.Gate, MokaTheme.Gate.WithDark(false).Typography);
	}

	[Fact]
	public void WithDark_KeepsHighContrast()
	{
		MokaTheme theme = MokaTheme.Gate.WithHighContrast().WithDark(false);

		Assert.True(theme.HighContrast);
		Assert.Equal("#0B0F18", theme.Palette.OnSurface);
	}

	[Fact]
	public void WithAccent_OfAPreset_TakesItsExactShades()
	{
		MokaPalette dark = MokaTheme.Gate.WithAccent(MokaAccent.Cyan).Palette;
		MokaPalette light = MokaTheme.GateLight.WithAccent(MokaAccent.Cyan).Palette;

		Assert.Equal(("#3CC8E6", "#9EE2F2", "#10303A"), (dark.Primary, dark.PrimaryLight, dark.PrimaryBg));
		Assert.Equal(("#00748C", "#006980", "#DDF3F8"), (light.Primary, light.PrimaryLight, light.PrimaryBg));
	}

	[Theory]
	[InlineData("#6E92FF")]
	[InlineData("#6e92ff")]
	[InlineData("#2F58D6")]
	public void WithAccent_OfAPresetsOwnColor_IsThatPreset(string color) =>
		Assert.Same(MokaAccent.Blue, MokaAccent.FromColor(color));

	[Fact]
	public void WithAccent_FollowsTheSchemesAlphaTiers()
	{
		MokaPalette dark = MokaTheme.Gate.WithAccent(MokaAccent.Cyan).Palette;
		MokaPalette light = MokaTheme.GateLight.WithAccent(MokaAccent.Cyan).Palette;

		Assert.Equal("rgba(60, 200, 230, 0.08)", dark.PrimaryGlow);
		Assert.Equal("rgba(60, 200, 230, 0.30)", dark.PrimaryBorder);
		Assert.Equal("rgba(0, 116, 140, 0.10)", light.PrimaryGlow);
		Assert.Equal("rgba(0, 116, 140, 0.36)", light.PrimaryBorder);
	}

	// A filled button draws OnPrimary on Primary, and on this look the shade math guarantees that pair.
	[Fact]
	public void WithAccent_SetsTheTextOnTheAccent()
	{
		Assert.Equal("#0A0E15", MokaTheme.Gate.WithAccent("#123456").Palette.OnPrimary);
		Assert.Equal("#FFFFFF", MokaTheme.GateLight.WithAccent("#FEDCBA").Palette.OnPrimary);
	}

	[Fact]
	public void WithAccent_RejectsWhatIsNotHex() =>
		Assert.Throws<ArgumentException>(() => MokaTheme.Gate.WithAccent("tomato"));

	[Fact]
	public void WithAccent_RejectsAnAccentWhoseShadesAreNotHex()
	{
		MokaAccentShades named = new("tomato", "#ffffff", "#000000");

		Assert.Throws<ArgumentException>(() => MokaTheme.Gate.WithAccent(new MokaAccent("mine", "Mine", named, named)));
	}

	// The same numbers MokaGate's own Appearance.Custom gives, so a person's accent moves across unchanged.
	[Fact]
	public void FromHue_MatchesMokaGate()
	{
		MokaAccent violet = MokaAccent.FromHue(250);

		Assert.Equal(new MokaAccentShades("#9885F3", "#C8BDFC", "#231C44"), violet.Dark);
		Assert.Equal(new MokaAccentShades("#5F44E1", "#5032E2", "#EFECFE"), violet.Light);
		Assert.Equal(new MokaAccentShades("#EE4FEE", "#FAA9FA", "#351635"), MokaAccent.FromHue(300).Dark);
		Assert.Equal(new MokaAccentShades("#1E5FB9", "#1855A9", "#E5EFFE"), MokaAccent.FromHue(215).Light);
	}

	[Fact]
	public void FromHue_WrapsAnyHue() =>
		Assert.Equal(MokaAccent.FromHue(350), MokaAccent.FromHue(-10));

	[Fact]
	public void WithDensity_BelowOne_SwapsTheLayoutTokens()
	{
		string css = MokaTheme.Gate.WithDensity(0.75).ToCssVariables();

		Assert.Contains("--moka-card-padding: 14px;", css, StringComparison.Ordinal);
		Assert.Contains("--moka-control-height: 34px;", css, StringComparison.Ordinal);
		Assert.Contains("--moka-main-padding: 18px 22px 110px;", css, StringComparison.Ordinal);
		Assert.Contains("--moka-spacing-md: 0.375rem;", css, StringComparison.Ordinal);
	}

	[Fact]
	public void WithDensity_BackToOne_RestoresThemAndKeepsCustomCorners()
	{
		MokaTheme theme = (MokaTheme.Gate with { Spacing = MokaSpacing.Gate with { RadiusMd = "3px" } })
			.WithDensity(0.75)
			.WithDensity(1);

		Assert.Equal("22px", theme.Spacing.CardPadding);
		Assert.Equal("3px", theme.Spacing.RadiusMd);
	}

	[Fact]
	public void WithHighContrast_SwapsOnlyTextAndBorders()
	{
		MokaPalette normal = MokaPalette.Gate;
		MokaPalette high = MokaTheme.Gate.WithHighContrast().Palette;

		Assert.Equal("#F4F6FA", high.OnSurface);
		Assert.Equal("#BAC3D4", high.OnSurfaceVariant);
		Assert.Equal("#3A4762", high.Outline);
		Assert.Equal(normal.Surface, high.Surface);
		Assert.Equal(normal.Primary, high.Primary);
		Assert.Equal(normal.Success, high.Success);
	}

	[Fact]
	public void WithHighContrast_Off_RestoresTheNormalColorsAndKeepsTheAccent()
	{
		MokaTheme theme = MokaTheme.GateLight.WithAccent(MokaAccent.Pink).WithHighContrast().WithHighContrast(false);

		Assert.False(theme.HighContrast);
		Assert.Equal(MokaPalette.GateLight.OnSurface, theme.Palette.OnSurface);
		Assert.Equal(MokaPalette.GateLight.Outline, theme.Palette.Outline);
		Assert.Equal(MokaAccent.Pink.Light.Color, theme.Palette.Primary);
	}

	[Fact]
	public void BuiltInThemes_AreSharedInstances()
	{
		Assert.Same(MokaTheme.Gate, MokaTheme.Gate);
		Assert.Same(MokaTheme.GateLight, MokaTheme.GateLight);
	}
}
