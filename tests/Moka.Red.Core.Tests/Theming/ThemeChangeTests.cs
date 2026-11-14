using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

// A change to a theme doesn't undo an earlier one: pins, the accent and what was there before high contrast all stay.
public class ThemeChangeTests
{
	[Fact]
	public void WithToken_PinsATokenThroughEveryChange()
	{
		MokaTheme theme = MokaTheme.Red.WithToken("--moka-color-primary", "#00B894");

		foreach (MokaTheme changed in new[]
		         {
			         theme, theme.WithDark(false), theme.WithHighContrast(), theme.WithAccent("#3B82F6"), theme.WithCorners(MokaCorners.Tight),
			         theme.WithSurfaces(MokaSurfaces.BlueGrey), theme.WithNeutral("#1C1917")
		         })
		{
			Assert.Equal("#00B894", changed.ToTokenMap()["--moka-color-primary"]);
			Assert.Contains("--moka-color-primary: #00B894; ", changed.ToCssVariables(), StringComparison.Ordinal);
		}
	}

	[Fact]
	public void WithoutToken_LetsThePinGo()
	{
		MokaTheme theme = MokaTheme.Red.WithToken("--moka-radius-card", "4px").WithoutToken("--moka-radius-card");

		Assert.Empty(theme.Pins);
		Assert.Equal(MokaTheme.Red.ToCssVariables(), theme.ToCssVariables());
		Assert.Same(MokaTheme.Red, MokaTheme.Red.WithoutToken("--moka-radius-card"));
	}

	[Theory]
	[InlineData("--moka-nothing", "#fff")]
	[InlineData("--moka-color-primary", "12px")]
	[InlineData("--moka-color-primary", "red; color: blue")]
	[InlineData("--moka-radius-card", "red")]
	[InlineData("--moka-font-family", "'Inter} body { color: red")]
	[InlineData("--moka-color-surface", "")]
	public void WithToken_RefusesWhatTheTokenCantTake(string name, string value) =>
		Assert.Throws<ArgumentException>(() => MokaTheme.Red.WithToken(name, value));

	[Theory]
	[InlineData("--moka-color-surface", "color-mix(in srgb, var(--moka-color-background) 50%, white)")]
	[InlineData("--moka-card-padding", "12px calc(1rem + 2px)")]
	[InlineData("--moka-shadow-modal", "0 4px 8px rgba(0, 0, 0, 0.3)")]
	public void WithToken_TakesValuesOfTheTokensKind(string name, string value) =>
		Assert.Equal(value, MokaTheme.Red.WithToken(name, value).Pins[name]);

	[Fact]
	public void ThemesPinnedAlike_AreEqual() =>
		Assert.Equal(
			MokaTheme.Gate.WithToken("--moka-color-primary", "#123456").WithToken("--moka-radius-card", "6px"),
			MokaTheme.Gate.WithToken("--moka-radius-card", "6px").WithToken("--moka-color-primary", "#123456"));

	// The accent used to be lost: WithDark took the scheme's preset palette whole.
	[Fact]
	public void WithDark_AfterWithAccent_KeepsTheAccent()
	{
		Assert.Equal("#42a5f5", MokaTheme.Dark.WithAccent("#42a5f5").WithDark(false).Palette.Primary);
		Assert.Equal(MokaAccent.Violet.Light.Color, MokaTheme.Gate.WithAccent(MokaAccent.Violet).WithDark(false).Palette.Primary);
		Assert.Equal(MokaAccent.Pink.Light.Color, MokaTheme.Light.WithAccent(MokaAccent.Pink).WithDark(true).WithDark(false).Palette.Primary);
		Assert.Equal(MokaAccent.Cyan.Light.Color, MokaTheme.Gate.WithAccent("#3CC8E6").WithDark(false).Palette.Primary);
	}

	[Fact]
	public void WithDark_WithoutAnAccent_TakesThePresetPalette()
	{
		Assert.Equal(MokaPalette.Dark, MokaTheme.Light.WithDark(true).Palette);
		Assert.Equal(MokaPalette.GraphiteLight, MokaTheme.Red.WithDark(false).Palette);
	}

	// Turning high contrast off used to put back the preset's colors, so a theme's own text colors were lost.
	[Fact]
	public void HighContrastOff_GivesBackWhatWasThere()
	{
		MokaTheme custom = MokaTheme.Red.WithSurface("#101820", "#D0E0F0");

		MokaTheme round = custom.WithHighContrast().WithHighContrast(false);

		Assert.Equal(custom.Palette, round.Palette);
		Assert.Equal(custom, round);
	}

	[Fact]
	public void HighContrastOff_OnAThemeWithout_ChangesNothing()
	{
		MokaTheme custom = MokaTheme.Red.WithSurface("#101820", "#D0E0F0");

		Assert.Same(custom, custom.WithHighContrast(false));
	}

	[Fact]
	public void HighContrast_StaysThroughAChangeOfScheme_AndComesOffCleanly()
	{
		MokaTheme theme = MokaTheme.Gate.WithHighContrast().WithDark(false);

		Assert.True(theme.HighContrast);
		Assert.Equal(MokaTheme.GateLight.WithHighContrast().Palette, theme.Palette);
		Assert.Equal(MokaPalette.GateLight, theme.WithHighContrast(false).Palette);
	}
}
