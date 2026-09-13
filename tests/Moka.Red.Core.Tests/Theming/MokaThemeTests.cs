using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

public class MokaThemeTests
{
	[Fact]
	public void Light_IsNotDark() => Assert.False(MokaTheme.Light.IsDark);

	[Fact]
	public void Dark_IsDark() => Assert.True(MokaTheme.Dark.IsDark);

	[Fact]
	public void ToCssVariables_ContainsAllColorTokens()
	{
		string css = MokaTheme.Light.ToCssVariables();

		Assert.Contains("--moka-color-primary: #d32f2f", css, StringComparison.Ordinal);
		Assert.Contains("--moka-color-on-primary: #ffffff", css, StringComparison.Ordinal);
		Assert.Contains("--moka-color-secondary:", css, StringComparison.Ordinal);
		Assert.Contains("--moka-color-surface:", css, StringComparison.Ordinal);
		Assert.Contains("--moka-color-error:", css, StringComparison.Ordinal);
		Assert.Contains("--moka-color-warning:", css, StringComparison.Ordinal);
		Assert.Contains("--moka-color-success:", css, StringComparison.Ordinal);
		Assert.Contains("--moka-color-info:", css, StringComparison.Ordinal);
		Assert.Contains("--moka-color-outline:", css, StringComparison.Ordinal);
	}

	[Fact]
	public void ToCssVariables_ContainsTypographyTokens()
	{
		string css = MokaTheme.Light.ToCssVariables();

		Assert.Contains("--moka-font-family:", css, StringComparison.Ordinal);
		Assert.Contains("--moka-font-size-base: 0.8125rem", css, StringComparison.Ordinal);
		Assert.Contains("--moka-line-height-base:", css, StringComparison.Ordinal);
		Assert.Contains("--moka-font-weight-normal:", css, StringComparison.Ordinal);
	}

	[Fact]
	public void ToCssVariables_ContainsSpacingTokens()
	{
		string css = MokaTheme.Light.ToCssVariables();

		Assert.Contains("--moka-spacing-xs:", css, StringComparison.Ordinal);
		Assert.Contains("--moka-spacing-md:", css, StringComparison.Ordinal);
		Assert.Contains("--moka-radius-md:", css, StringComparison.Ordinal);
	}

	[Fact]
	public void ToCssVariables_DarkTheme_HasDifferentColors()
	{
		string lightCss = MokaTheme.Light.ToCssVariables();
		string darkCss = MokaTheme.Dark.ToCssVariables();

		Assert.NotEqual(lightCss, darkCss);
		Assert.Contains("--moka-color-primary: #ef5350", darkCss, StringComparison.Ordinal);
		Assert.Contains("--moka-color-background: #060608", darkCss, StringComparison.Ordinal);
	}

	[Fact]
	public void CustomTheme_OverridesDefaults()
	{
		var custom = new MokaTheme
		{
			Palette = MokaPalette.Light with { Primary = "#00ff00" }
		};

		string css = custom.ToCssVariables();

		Assert.Contains("--moka-color-primary: #00ff00", css, StringComparison.Ordinal);
	}

	[Fact]
	public void ToCssVariables_ContainsSemanticDimTokens()
	{
		string css = MokaTheme.Dark.ToCssVariables();

		Assert.Contains("--moka-color-error-dim: rgba(239, 83, 80, 0.12)", css, StringComparison.Ordinal);
		Assert.Contains("--moka-color-warning-dim: rgba(255, 171, 64, 0.12)", css, StringComparison.Ordinal);
		Assert.Contains("--moka-color-success-dim: rgba(0, 230, 118, 0.15)", css, StringComparison.Ordinal);
		Assert.Contains("--moka-color-info-dim: rgba(66, 165, 245, 0.15)", css, StringComparison.Ordinal);
		Assert.Contains("--moka-color-primary-glow-faint: rgba(239, 83, 80, 0.03)", css, StringComparison.Ordinal);
	}

	[Fact]
	public void WithAccent_RecomputesDerivedTokens()
	{
		MokaTheme themed = MokaTheme.Dark.WithAccent("#42a5f5");

		Assert.Equal("#42a5f5", themed.Palette.Primary);
		Assert.Equal("rgba(66, 165, 245, 0.08)", themed.Palette.PrimaryGlow);
		Assert.Equal("rgba(66, 165, 245, 0.15)", themed.Palette.PrimaryGlowMd);
		Assert.Equal("rgba(66, 165, 245, 0.25)", themed.Palette.PrimaryGlowStrong);
		Assert.Equal("rgba(66, 165, 245, 0.20)", themed.Palette.PrimaryBorder);
		Assert.Equal("rgba(66, 165, 245, 0.03)", themed.Palette.PrimaryGlowFaint);

		// Dark themes get accent-tinted outlines
		Assert.Equal("rgba(66, 165, 245, 0.12)", themed.Palette.Outline);
		Assert.Equal("rgba(66, 165, 245, 0.06)", themed.Palette.OutlineVariant);
	}

	[Fact]
	public void WithAccent_LightTheme_KeepsGrayOutlines()
	{
		MokaTheme themed = MokaTheme.Light.WithAccent("#42a5f5");

		Assert.Equal("#c4c4c4", themed.Palette.Outline);
		Assert.Equal("rgba(66, 165, 245, 0.08)", themed.Palette.PrimaryGlow);
	}

	[Fact]
	public void WithAccent_RetintsDarkShadows()
	{
		string css = MokaTheme.Dark.WithAccent("#42a5f5").ToCssVariables();

		Assert.Contains("--moka-shadow-1: 0 0 0 1px rgba(66, 165, 245, 0.06)", css, StringComparison.Ordinal);
		Assert.DoesNotContain("--moka-shadow-1: 0 0 0 1px rgba(239, 83, 80", css, StringComparison.Ordinal);
	}

	[Fact]
	public void WithAccent_InvalidHex_Throws() =>
		Assert.Throws<ArgumentException>(() => MokaTheme.Dark.WithAccent("tomato"));

	[Fact]
	public void WithAccent_ShortHex_Expands()
	{
		MokaTheme themed = MokaTheme.Dark.WithAccent("#f00");

		Assert.Equal("rgba(255, 0, 0, 0.08)", themed.Palette.PrimaryGlow);
	}

	[Fact]
	public void WithDensity_ScalesSpacingTokensOnly()
	{
		string css = MokaTheme.Dark.WithDensity(0.75).ToCssVariables();

		// 0.5rem * 0.75 = 0.375rem
		Assert.Contains("--moka-spacing-md: 0.375rem", css, StringComparison.Ordinal);
		Assert.Contains("--moka-density: 0.75", css, StringComparison.Ordinal);
		// Radii and font sizes stay untouched
		Assert.Contains("--moka-radius-md: 8px", css, StringComparison.Ordinal);
		Assert.Contains("--moka-font-size-base: 0.8125rem", css, StringComparison.Ordinal);
	}

	[Fact]
	public void WithFontScale_ScalesFontSizeTokensOnly()
	{
		string css = MokaTheme.Dark.WithFontScale(2.0).ToCssVariables();

		// 0.8125rem * 2 = 1.625rem
		Assert.Contains("--moka-font-size-base: 1.625rem", css, StringComparison.Ordinal);
		Assert.Contains("--moka-font-scale: 2", css, StringComparison.Ordinal);
		Assert.Contains("--moka-spacing-md: 0.5rem", css, StringComparison.Ordinal);
	}

	[Fact]
	public void DefaultScales_EmitUnscaledTokens()
	{
		string css = MokaTheme.Dark.ToCssVariables();

		Assert.Contains("--moka-density: 1", css, StringComparison.Ordinal);
		Assert.Contains("--moka-font-scale: 1", css, StringComparison.Ordinal);
		Assert.Contains("--moka-spacing-md: 0.5rem", css, StringComparison.Ordinal);
	}
}
