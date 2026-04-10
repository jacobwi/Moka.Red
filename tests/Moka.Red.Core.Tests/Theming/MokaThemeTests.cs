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
}
