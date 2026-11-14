using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.Primitives.ColorSwatch;
using Moka.Red.ThemeGen.Appearance;
using Moka.Red.ThemeGen.Serialization;

namespace Moka.Red.ThemeGen.Tests.Components;

public sealed class MokaAppearanceCustomColorsTests : BunitContext
{
	public MokaAppearanceCustomColorsTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ACustomAccent_TintsThePalette_AsWithAccentDoes()
	{
		MokaAppearance value = new() { Palette = "red", CustomAccent = "#12b886" };

		Assert.Equal(MokaTheme.Red.WithAccent("#12B886"), value.ToTheme(true));
		Assert.Equal(MokaTheme.RedLight.WithAccent("#12B886"), value.ToTheme(false));
	}

	[Fact]
	public void ACustomAccent_WinsOverAPresetAccent()
	{
		MokaAppearance value = new() { Palette = "gate", Accent = MokaAccent.Pink.Id, CustomAccent = "#F59E0B" };

		Assert.Equal(MokaTheme.Gate.WithAccent("#F59E0B"), value.ToTheme(true));
	}

	[Fact]
	public void ACustomNeutral_DerivesTheThemeFromASeed_WithTheAccentInUse()
	{
		MokaAppearance value = new() { Palette = "gate", Accent = MokaAccent.Cyan.Id, CustomNeutral = "#8E857A" };

		MokaTheme dark = value.ToTheme(true);
		MokaTheme light = value.ToTheme(false);

		Assert.Equal(new MokaThemeSeed { Accent = MokaAccent.Cyan.Dark.Color.ToUpperInvariant(), Neutral = "#8E857A" }, dark.Seed);
		Assert.Equal(MokaAccent.Cyan.Light.Color.ToUpperInvariant(), light.Seed?.Accent);
		Assert.True(dark.IsDark);
		Assert.False(light.IsDark);
		Assert.Equal(MokaTheme.Gate.Typography, dark.Typography);
	}

	[Fact]
	public void ACustomNeutral_AndACustomAccent_SeedTogether()
	{
		MokaAppearance value = new() { Palette = "crisp", CustomAccent = "#E11D48", CustomNeutral = "#7D8A7F", TextSize = MokaTextSize.Normal };

		MokaTheme theme = value.ToTheme(true);

		Assert.Equal("#E11D48", theme.Seed?.Accent);
		Assert.Equal("#7D8A7F", theme.Seed?.Neutral);
		Assert.Equal(MokaTheme.Crisp.WithSeed(theme.Seed!).WithTextSize(MokaTextSize.Normal), theme);
	}

	[Fact]
	public void TheOriginalDesign_KeepsItsSurfaces_ButTakesACustomAccent()
	{
		MokaAppearance value = new() { Palette = "original", CustomAccent = "#0277BD", CustomNeutral = "#887D8E" };

		Assert.Equal(MokaTheme.Dark.WithAccent("#0277BD"), value.ToTheme(true));
	}

	[Theory]
	[InlineData("not a color", null)]
	[InlineData("rgb(0 0 0 / 50%)", null)]
	public void ACustomColorThatIsntOne_IsIgnored(string accent, string? neutral)
	{
		MokaAppearance value = new() { Palette = "red", CustomAccent = accent, CustomNeutral = neutral };

		Assert.Equal(MokaTheme.Red, value.ToTheme(true));
	}

	[Fact]
	public void ToThemes_CarriesTheCustomColors()
	{
		MokaAppearance value = new() { Mode = MokaColorMode.System, Palette = "red", CustomNeutral = "#7A8494" };

		MokaAppearanceThemes themes = value.ToThemes();

		Assert.Equal("#7A8494", themes.DarkTheme.Seed?.Neutral);
		Assert.Equal("#7A8494", themes.LightTheme.Seed?.Neutral);
		Assert.False(themes.LightTheme.IsDark);
	}

	[Fact]
	public void TheValue_RoundTripsThroughJson_AndItsThemeThroughTheSerializer()
	{
		MokaAppearance value = new() { Palette = "gate", CustomAccent = "#12B886", CustomNeutral = "#7A8494", Density = MokaDensity.Compact };

		MokaAppearance? restored = JsonSerializer.Deserialize<MokaAppearance>(JsonSerializer.Serialize(value));
		MokaTheme theme = value.ToTheme(true);

		Assert.Equal(value, restored);
		Assert.Equal(theme, MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(theme)));
	}

	[Fact]
	public void Crisp_IsAPreset()
	{
		Assert.Contains(MokaAppearancePalette.Crisp, MokaAppearancePalette.Presets);

		IRenderedComponent<MokaAppearancePanel> cut = Render<MokaAppearancePanel>(p => p.Add(x => x.Rows, MokaAppearanceRows.Palette));

		Assert.Single(cut.FindAll("input[type='radio'][value='crisp']"));
	}

	[Fact]
	public void ThePanel_OffersNoPicker_UnlessAsked()
	{
		IRenderedComponent<MokaAppearancePanel> cut = Render<MokaAppearancePanel>(p => p.Add(x => x.Rows, MokaAppearanceRows.Accent | MokaAppearanceRows.Neutral));

		Assert.All(cut.FindComponents<MokaColorSwatch>(), swatch => Assert.False(swatch.Instance.AllowCustom));
	}

	[Fact]
	public async Task ThePanel_TakesACustomAccent_FromItsPicker()
	{
		MokaAppearance? changed = null;
		IRenderedComponent<MokaAppearancePanel> cut = Render<MokaAppearancePanel>(p => p
			.Add(x => x.Rows, MokaAppearanceRows.Accent)
			.Add(x => x.CustomColors, true)
			.Add(x => x.Value, new MokaAppearance { Accent = MokaAccent.Blue.Id })
			.Add(x => x.ValueChanged, EventCallback.Factory.Create<MokaAppearance>(this, value => changed = value)));

		IRenderedComponent<MokaColorSwatch> swatch = cut.FindComponent<MokaColorSwatch>();
		Assert.True(swatch.Instance.AllowCustom);
		await cut.InvokeAsync(() => swatch.Instance.SelectedColorChanged.InvokeAsync("#123456"));

		Assert.Equal("#123456", changed?.CustomAccent);
		Assert.Null(changed?.Accent);
		Assert.Contains(new MokaAppearanceText().UsePaletteAccent, cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void ThePanel_PicksANeutral_AndPutsThePalettesBack()
	{
		MokaAppearance value = new();
		IRenderedComponent<MokaAppearancePanel> cut = Render<MokaAppearancePanel>(p => p
			.Add(x => x.Rows, MokaAppearanceRows.Neutral)
			.Add(x => x.CustomColors, true)
			.Add(x => x.ValueChanged, EventCallback.Factory.Create<MokaAppearance>(this, next => value = next)));

		cut.Find($"input[type='radio'][value='{MokaAppearanceNeutral.Slate.Color}']").Change(new ChangeEventArgs { Value = MokaAppearanceNeutral.Slate.Color });
		Assert.Equal(MokaAppearanceNeutral.Slate.Color, value.CustomNeutral);

		cut.FindAll("button").First(button => button.TextContent.Trim() == new MokaAppearanceText().UsePaletteNeutral).Click();
		Assert.Null(value.CustomNeutral);
	}

	[Fact]
	public void TheNeutralRow_Hides_OnTheOriginalDesign()
	{
		IRenderedComponent<MokaAppearancePanel> cut = Render<MokaAppearancePanel>(p => p
			.Add(x => x.Rows, MokaAppearanceRows.Neutral | MokaAppearanceRows.Accent)
			.Add(x => x.Value, new MokaAppearance { Palette = "original" }));

		Assert.Single(cut.FindComponents<MokaColorSwatch>());
		Assert.DoesNotContain(new MokaAppearanceText().Neutral, cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void APaletteCard_PreviewsTheCustomColors()
	{
		MokaAppearance value = new() { Palette = "red", CustomAccent = "#12B886" };
		IRenderedComponent<MokaAppearancePanel> cut = Render<MokaAppearancePanel>(p => p
			.Add(x => x.Rows, MokaAppearanceRows.Palette)
			.Add(x => x.Value, value));

		Assert.Contains(cut.FindComponents<MokaThemeProvider>(), provider => provider.Instance.Theme.Equals(MokaTheme.Gate.WithAccent("#12B886")));
	}
}
