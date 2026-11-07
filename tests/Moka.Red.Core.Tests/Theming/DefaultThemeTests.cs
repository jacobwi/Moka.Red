using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Extensions;
using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

public class DefaultThemeTests : BunitContext
{
	public DefaultThemeTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TheDefault_IsRed()
	{
		Assert.Same(MokaTheme.Red, MokaTheme.Default);
		Assert.Same(MokaTheme.RedLight, MokaTheme.DefaultLight);
	}

	[Fact]
	public void AProviderWithoutATheme_DrawsTheDefault()
	{
		IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>();

		string rootClass = cut.Find(".moka-root").ClassName!;
		Assert.Contains("moka-gate", rootClass, StringComparison.Ordinal);
		Assert.Contains("moka-dark", rootClass, StringComparison.Ordinal);
		Assert.Contains(MokaPalette.Graphite.Surface, cut.Find(".moka-root").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void DarkAndLight_StayOnTheMatrixLook()
	{
		Assert.Equal(MokaLook.Matrix, MokaTheme.Dark.Look);
		Assert.Equal(MokaLook.Matrix, MokaTheme.Light.Look);
		Assert.Equal("#060608", MokaTheme.Dark.Palette.Background);
	}

	[Fact]
	public void Red_IsGraphiteWithARedAccent()
	{
		Assert.Equal(MokaLook.Gate, MokaTheme.Red.Look);
		Assert.Equal(MokaSurfaces.Graphite, MokaTheme.Red.Surfaces);
		Assert.Equal(MokaAccent.Red.Dark.Color, MokaTheme.Red.Palette.Primary);
		Assert.Equal(MokaAccent.Red.Light.Color, MokaTheme.RedLight.Palette.Primary);
		Assert.Equal("#0C0C0E", MokaTheme.Red.Palette.Background);
	}

	[Fact]
	public void Crisp_IsBlueGreyAndTight()
	{
		Assert.Equal(MokaSurfaces.BlueGrey, MokaTheme.Crisp.Surfaces);
		Assert.Equal(MokaAccent.Blue.Dark.Color, MokaTheme.Crisp.Palette.Primary);
		Assert.Equal("34px", MokaTheme.Crisp.Spacing.ControlHeight);
		Assert.Equal("10px", MokaTheme.Crisp.Spacing.RadiusLg);
		Assert.Equal(MokaSelection.Flat, MokaTheme.Crisp.Selection);
	}

	[Fact]
	public void WithDark_StaysOnTheThemesSurfaces()
	{
		Assert.Equal(MokaPalette.GraphiteLight.Background, MokaTheme.Red.WithDark(false).Palette.Background);
		Assert.Equal(MokaAccent.Red.Light.Color, MokaTheme.Red.WithDark(false).Palette.Primary);
		Assert.Equal(MokaPalette.Graphite.Background, MokaTheme.RedLight.WithDark(true).Palette.Background);
		Assert.Equal(MokaPalette.GateLight.Background, MokaTheme.Gate.WithDark(false).Palette.Background);
	}

	[Fact]
	public void WithSurfaces_KeepsTheAccent()
	{
		MokaTheme theme = MokaTheme.Gate.WithAccent(MokaAccent.Violet).WithSurfaces(MokaSurfaces.Graphite);

		Assert.Equal(MokaPalette.Graphite.Surface, theme.Palette.Surface);
		Assert.Equal(MokaAccent.Violet.Dark.Color, theme.Palette.Primary);
		Assert.Equal(MokaSurfaces.Graphite, theme.Surfaces);
	}

	[Fact]
	public void WithSurfaces_DoesNothingOnMatrix() =>
		Assert.Same(MokaTheme.Dark, MokaTheme.Dark.WithSurfaces(MokaSurfaces.Graphite));

	[Fact]
	public void TurningHighContrastOff_ReturnsToTheThemesOwnSurfaces()
	{
		MokaTheme round = MokaTheme.Red.WithHighContrast().WithHighContrast(false);

		Assert.Equal(MokaPalette.Graphite.Outline, round.Palette.Outline);
		Assert.Equal(MokaPalette.Graphite.OnSurfaceVariant, round.Palette.OnSurfaceVariant);
	}

	[Fact]
	public void HighContrast_OnGraphite_IsStrongerAndStaysNeutral()
	{
		MokaPalette high = MokaTheme.Red.WithHighContrast().Palette;

		Assert.NotEqual(MokaPalette.Graphite.Outline, high.Outline);
		Assert.NotEqual(MokaTheme.Gate.WithHighContrast().Palette.Outline, high.Outline);
	}

	[Fact]
	public void FlatSelection_OnCrisp_DrawsNoRingAndNoBar()
	{
		string flat = MokaTheme.Crisp.ToCssVariables();
		string ring = MokaTheme.Red.ToCssVariables();

		Assert.Contains("--moka-selected-glow: none;", flat, StringComparison.Ordinal);
		Assert.Contains("--moka-selected-bar: none;", flat, StringComparison.Ordinal);
		Assert.Contains("--moka-selected-glow: inset 0 0 0 1px", ring, StringComparison.Ordinal);
		Assert.Contains("--moka-selected-bar: inset 2px", ring, StringComparison.Ordinal);
	}

	[Fact]
	public void Compact_SwapsTheNewLayoutTokens()
	{
		Assert.Equal("38px", MokaTheme.Red.Spacing.ButtonHeight);
		Assert.Equal("32px", MokaTheme.Red.WithDensity(0.75).Spacing.ButtonHeight);
		Assert.Equal("6px 14px", MokaTheme.Red.WithDensity(0.75).Spacing.CellPadding);
	}
}
