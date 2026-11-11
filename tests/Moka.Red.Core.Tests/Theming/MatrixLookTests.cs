using System.Text.RegularExpressions;
using Moka.Red.Core.Theming;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Theming;

// Adding the Gate look must not move anything an app on MokaTheme.Dark or MokaTheme.Light can see.
public partial class MatrixLookTests
{
	[Theory]
	[InlineData("Dark", MatrixGolden.Dark)]
	[InlineData("Light", MatrixGolden.Light)]
	[InlineData("Dark with accent, density and scale", MatrixGolden.DarkAccentDensityScale)]
	public void EveryExistingToken_KeepsItsValue(string name, string golden)
	{
		MokaTheme theme = name switch
		{
			"Dark" => MokaTheme.Dark,
			"Light" => MokaTheme.Light,
			_ => MokaTheme.Dark.WithAccent("#42a5f5").WithDensity(0.75).WithFontScale(1.25)
		};

		Dictionary<string, string> now = Tokens(theme.ToCssVariables());

		foreach ((string token, string value) in Tokens(golden))
		{
			Assert.True(now.TryGetValue(token, out string? current), $"{name}: {token} is gone.");
			Assert.True(value == current, $"{name}: {token} was '{value}' and is now '{current}'.");
		}
	}

	// A token that did not exist before reads another token or holds what a component hard-coded, so a
	// component that moves onto it looks the same on this look.
	[Theory]
	[InlineData("--moka-color-surface-raised", "var(--moka-color-surface-hover)")]
	[InlineData("--moka-color-outline-strong", "var(--moka-color-outline)")]
	[InlineData("--moka-color-primary-focus", "var(--moka-color-primary-glow)")]
	[InlineData("--moka-color-success-bg", "var(--moka-color-success-dim)")]
	[InlineData("--moka-color-knob", "#ffffff")]
	[InlineData("--moka-shadow-knob", "0 1px 2px rgba(0, 0, 0, 0.2)")]
	[InlineData("--moka-control-case", "uppercase")]
	[InlineData("--moka-control-tracking", "0.08em")]
	[InlineData("--moka-title-case", "uppercase")]
	[InlineData("--moka-radius-card", "var(--moka-radius-lg)")]
	[InlineData("--moka-radius-popup", "var(--moka-radius-xl)")]
	[InlineData("--moka-chart-1", "var(--moka-color-primary)")]
	[InlineData("--moka-chart-2", "var(--moka-color-secondary)")]
	[InlineData("--moka-chart-3", "var(--moka-color-success)")]
	[InlineData("--moka-chart-4", "var(--moka-color-info)")]
	[InlineData("--moka-chart-5", "var(--moka-color-warning)")]
	[InlineData("--moka-chart-6", "var(--moka-color-error)")]
	[InlineData("--moka-chart-7", "var(--moka-color-on-surface)")]
	[InlineData("--moka-chart-8", "var(--moka-color-primary)")]
	[InlineData("--moka-chart-up", "var(--moka-color-info)")]
	[InlineData("--moka-chart-down", "var(--moka-color-error)")]
	[InlineData("--moka-chart-muted", "var(--moka-color-on-surface-tertiary)")]
	[InlineData("--moka-chart-grid", "var(--moka-color-outline-variant)")]
	[InlineData("--moka-chart-axis", "var(--moka-color-outline-variant)")]
	[InlineData("--moka-chart-band-1", "color-mix(in srgb, var(--moka-color-on-surface) 6%, transparent)")]
	[InlineData("--moka-chart-band-2", "color-mix(in srgb, var(--moka-color-on-surface) 10%, transparent)")]
	[InlineData("--moka-chart-band-3", "color-mix(in srgb, var(--moka-color-on-surface) 16%, transparent)")]
	[InlineData("--moka-chart-seq-1", "color-mix(in srgb, var(--moka-color-primary) 20%, transparent)")]
	[InlineData("--moka-chart-seq-2", "color-mix(in srgb, var(--moka-color-primary) 40%, transparent)")]
	[InlineData("--moka-chart-seq-3", "color-mix(in srgb, var(--moka-color-primary) 60%, transparent)")]
	[InlineData("--moka-chart-seq-4", "color-mix(in srgb, var(--moka-color-primary) 80%, transparent)")]
	[InlineData("--moka-chart-seq-5", "var(--moka-color-primary)")]
	[InlineData("--moka-chart-ord-1", "var(--moka-color-primary)")]
	[InlineData("--moka-chart-ord-2", "color-mix(in srgb, var(--moka-color-primary) 80%, transparent)")]
	[InlineData("--moka-chart-ord-3", "color-mix(in srgb, var(--moka-color-primary) 60%, transparent)")]
	[InlineData("--moka-chart-ord-4", "color-mix(in srgb, var(--moka-color-primary) 40%, transparent)")]
	[InlineData("--moka-chart-ord-5", "color-mix(in srgb, var(--moka-color-primary) 20%, transparent)")]
	[InlineData("--moka-chart-zero", "var(--moka-color-outline-variant)")]
	public void NewTokens_StartFromTheLookTheyAreAddedTo(string token, string value)
	{
		Assert.Equal(value, Tokens(MokaTheme.Dark.ToCssVariables())[token]);
		Assert.Equal(value, Tokens(MokaTheme.Light.ToCssVariables())[token]);
	}

	[Fact]
	public void WithDark_StaysInTheLook()
	{
		Assert.Equal(MokaLook.Matrix, MokaTheme.Light.WithDark(true).Look);
		Assert.Equal(MokaPalette.Dark, MokaTheme.Light.WithDark(true).Palette);
		Assert.Equal(MokaPalette.Light, MokaTheme.Dark.WithDark(false).Palette);
	}

	[Fact]
	public void WithHighContrast_DoesNothing() =>
		Assert.Same(MokaTheme.Dark, MokaTheme.Dark.WithHighContrast());

	[Fact]
	public void WithDensity_LeavesTheLayoutTokensToTheSpacingScale()
	{
		string css = MokaTheme.Dark.WithDensity(0.75).ToCssVariables();

		Assert.Contains("--moka-spacing-md: 0.375rem", css, StringComparison.Ordinal);
		Assert.Contains("--moka-card-gap: var(--moka-spacing-md)", css, StringComparison.Ordinal);
	}

	// Parameters such as Theme="MokaTheme.Dark" compare by reference to skip regenerating the tokens.
	[Fact]
	public void BuiltInThemes_AreSharedInstances()
	{
		Assert.Same(MokaTheme.Dark, MokaTheme.Dark);
		Assert.Same(MokaTheme.Light, MokaTheme.Light);
		Assert.Same(MokaPalette.Dark, MokaPalette.Dark);
		Assert.Same(MokaTypography.Default, MokaTypography.Default);
		Assert.Same(MokaSpacing.Default, MokaSpacing.Default);
	}

	private static Dictionary<string, string> Tokens(string css)
	{
		Dictionary<string, string> tokens = new(StringComparer.Ordinal);
		foreach (Match match in Declaration().Matches(css))
		{
			tokens[match.Groups["name"].Value] = match.Groups["value"].Value.Trim();
		}

		return tokens;
	}

	[GeneratedRegex(@"(?<name>--moka-[a-z0-9-]+):\s*(?<value>[^;]+)(;|$)")]
	private static partial Regex Declaration();
}
