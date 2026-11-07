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
