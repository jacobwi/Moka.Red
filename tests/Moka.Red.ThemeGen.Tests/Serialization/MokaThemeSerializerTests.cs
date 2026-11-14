using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Table;
using Moka.Red.ThemeGen.Serialization;

namespace Moka.Red.ThemeGen.Tests.Serialization;

// JSON and C# export used to write only the 23 core palette colors plus typography and spacing. Reading
// back lost Density, FontScale and the other palette colors (they came back as the dark MokaPalette
// initializer values, so a light theme got SurfaceHover #2a2a2a), {"palette": null} threw a
// NullReferenceException, and the C# export did not escape quotes.
public class MokaThemeSerializerTests
{
	[Fact]
	public void Json_RoundTripsEveryProperty()
	{
		MokaTheme theme = ThemeReflection.EveryPropertySetImportable();

		MokaTheme? restored = MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(theme));

		Assert.NotNull(restored);
		ThemeReflection.AssertSameProperties(theme, restored);
	}

	[Fact]
	public async Task CSharp_CompilesBackToEveryProperty()
	{
		MokaTheme theme = ThemeReflection.EveryPropertySet();
		string code = MokaThemeSerializer.ToCSharp(theme);

		ScriptOptions options = ScriptOptions.Default
			.AddReferences(typeof(MokaTheme).Assembly)
			.AddImports("Moka.Red.Core.Theming");
		MokaTheme restored = await CSharpScript.EvaluateAsync<MokaTheme>(code, options,
			cancellationToken: TestContext.Current.CancellationToken);

		ThemeReflection.AssertSameProperties(theme, restored);
	}

	[Fact]
	public void Json_RoundTripsPinsAndASeed()
	{
		MokaTheme theme = MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#7C3AED", Neutral = "#1C1917", Contrast = MokaSeedContrast.More }, true)
			.WithToken("--moka-color-primary", "#00B894")
			.WithToken("--moka-card-padding", "12px 16px");

		MokaTheme? restored = MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(theme));

		Assert.NotNull(restored);
		Assert.Equal(theme.Seed, restored.Seed);
		Assert.Equal(theme.Pins.OrderBy(pin => pin.Key, StringComparer.Ordinal), restored.Pins.OrderBy(pin => pin.Key, StringComparer.Ordinal));
		Assert.Equal(theme, restored);
		Assert.Equal(theme.ToCssVariables(), restored.ToCssVariables());
	}

	[Fact]
	public async Task CSharp_CompilesBackPins()
	{
		MokaTheme theme = MokaTheme.Gate.WithToken("--moka-color-primary", "#00B894").WithToken("--moka-radius-card", "6px");

		ScriptOptions options = ScriptOptions.Default
			.AddReferences(typeof(MokaTheme).Assembly)
			.AddImports("Moka.Red.Core.Theming");
		MokaTheme restored = await CSharpScript.EvaluateAsync<MokaTheme>(MokaThemeSerializer.ToCSharp(theme), options,
			cancellationToken: TestContext.Current.CancellationToken);

		Assert.Equal(theme, restored);
	}

	[Theory]
	[InlineData("{ \"pins\": { \"--moka-nothing\": \"#fff\" } }")]
	[InlineData("{ \"pins\": { \"--moka-color-primary\": \"12px\" } }")]
	[InlineData("{ \"seed\": { \"neutral\": \"#111723\" } }")]
	[InlineData("{ \"seed\": { \"accent\": \"not a color\" } }")]
	public void Json_RefusesABadPinOrSeed(string json)
	{
		Assert.False(MokaThemeSerializer.TryFromJson(json, out _, out string? error));
		Assert.False(string.IsNullOrWhiteSpace(error));
	}

	[Fact]
	public void Json_RoundTripsTheLightTheme() =>
		Assert.Equal(MokaTheme.Light, MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(MokaTheme.Light)));

	[Fact]
	public void Json_RoundTripsTheDarkTheme() =>
		Assert.Equal(MokaTheme.Dark, MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(MokaTheme.Dark)));

	[Fact]
	public void Json_RoundTripsDensityAndFontScale()
	{
		MokaTheme theme = MokaTheme.Light.WithDensity(0.75).WithFontScale(1.15);

		MokaTheme? restored = MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(theme));

		Assert.NotNull(restored);
		Assert.Equal(0.75, restored.Density);
		Assert.Equal(1.15, restored.FontScale);
	}

	[Fact]
	public void Json_RoundTripsANonFiniteDensityInsteadOfThrowing()
	{
		MokaTheme theme = MokaTheme.Light.WithDensity(double.NaN).WithFontScale(double.PositiveInfinity);

		MokaTheme? restored = MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(theme));

		Assert.NotNull(restored);
		Assert.True(double.IsNaN(restored.Density));
		Assert.Equal(double.PositiveInfinity, restored.FontScale);
	}

	[Fact]
	public void Json_FillsMissingPaletteColorsFromTheMatchingBuiltInPalette()
	{
		// The shape the old export wrote: core colors only, no density or font scale.
		const string json = """
			{
			  "isDark": false,
			  "palette": { "primary": "#123456", "surface": "#ffffff" }
			}
			""";

		MokaTheme? theme = MokaThemeSerializer.FromJson(json);

		Assert.NotNull(theme);
		Assert.Equal("#123456", theme.Palette.Primary);
		Assert.Equal(MokaPalette.Light.SurfaceHover, theme.Palette.SurfaceHover);
		Assert.Equal(MokaPalette.Light.OnSurfaceQuaternary, theme.Palette.OnSurfaceQuaternary);
		Assert.Equal(1.0, theme.Density);
	}

	[Fact]
	public void Json_FillsMissingDarkPaletteColorsFromTheDarkPalette()
	{
		MokaTheme? theme = MokaThemeSerializer.FromJson("""{ "isDark": true, "palette": { "primary": "#123456" } }""");

		Assert.NotNull(theme);
		Assert.True(theme.IsDark);
		Assert.Equal("#123456", theme.Palette.Primary);
		Assert.Equal(MokaPalette.Dark.Surface, theme.Palette.Surface);
		Assert.Equal(MokaPalette.Dark.SurfaceHover, theme.Palette.SurfaceHover);
	}

	[Fact]
	public void Json_MatchesNamesWhateverTheirCase()
	{
		MokaTheme? theme = MokaThemeSerializer.FromJson("""{ "IsDark": true, "Palette": { "Primary": "#123456" } }""");

		Assert.NotNull(theme);
		Assert.True(theme.IsDark);
		Assert.Equal("#123456", theme.Palette.Primary);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("null")]
	[InlineData("[]")]
	[InlineData("42")]
	[InlineData("\"theme\"")]
	[InlineData("{")]
	[InlineData("{\"palette\": null}")]
	[InlineData("{\"palette\": 5}")]
	[InlineData("{\"palette\": {\"primary\": null}}")]
	[InlineData("{\"palette\": {\"primary\": 5}}")]
	[InlineData("{\"palette\": {\"primary\": \"  \"}}")]
	[InlineData("{\"typography\": null}")]
	[InlineData("{\"typography\": {\"fontFamily\": null}}")]
	[InlineData("{\"spacing\": []}")]
	[InlineData("{\"spacing\": {\"md\": false}}")]
	[InlineData("{\"isDark\": \"yes\"}")]
	[InlineData("{\"isDark\": null}")]
	[InlineData("{\"density\": \"wide\"}")]
	[InlineData("{\"density\": null}")]
	[InlineData("{\"fontScale\": 1e400}")]
	public void FromJson_ReturnsNullForBadInputWithoutThrowing(string json) =>
		Assert.Null(MokaThemeSerializer.FromJson(json));

	public static TheoryData<string, MokaTheme> ComponentLooks => new()
	{
		{ "toasts", MokaTheme.Red.WithToasts(MokaToastLook.Pill) },
		{ "toastStacking", MokaTheme.Red.WithToastStacking(MokaToastStacking.Pile) },
		{ "notifications", MokaTheme.Red.WithNotifications(MokaNotificationLook.Grouped) },
		{ "popups", MokaTheme.Red.WithPopups(MokaPopupLook.Glass) },
		{ "lists", MokaTheme.Red.WithLists(MokaListLook.Radio) },
		{ "dialogs", MokaTheme.Red.WithDialogs(MokaDialogLook.Bands) },
		{ "buttons", MokaTheme.Red.WithButtons(MokaButtonLook.Ghost) },
		{ "tooltips", MokaTheme.Red.WithTooltips(MokaTooltipLook.Glass) },
		{ "tooltipArrows", MokaTheme.Red.WithTooltipArrows(true) },
		{ "tooltipMotion", MokaTheme.Red.WithTooltipMotion(MokaTooltipMotion.Grow) },
		{ "tooltipSize", MokaTheme.Red.WithTooltipSize(MokaTooltipSize.Compact) },
		{ "elevation", MokaTheme.Red.WithElevation(MokaElevation.Glow) },
		{ "textButtons", MokaTheme.Red.WithTextButtons(MokaTextButtonColor.Neutral) },
		{ "callouts", MokaTheme.Red.WithCallouts(MokaCalloutLook.Edge) },
		{ "switches", MokaTheme.Red.WithSwitches(MokaSwitchLook.Tinted) },
		{ "settingsHeaders", MokaTheme.Red.WithSettingsHeaders(MokaSettingsHeaderLook.Tinted) },
		{ "docks", MokaTheme.Red.WithDocks(MokaDockLook.Ide) },
		{ "currentPlace", MokaTheme.Red.WithCurrentPlace(MokaCurrentPlace.SolidInset) },
		{ "fontFamilyNumber", MokaTheme.Red.WithNumberFont(MokaNumberFont.Mono) },
		{ "cardTitleCase", MokaTheme.Red.WithLetterCase(MokaLetterCase.Controls) },
		{ "fontSizeHeading", MokaTheme.Red.WithTitleSize(MokaTitleSize.Compact) }
	};

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Json_RoundTripsEmber(bool dark)
	{
		MokaTheme theme = dark ? MokaTheme.Ember : MokaTheme.EmberLight;

		Assert.Equal(theme, MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(theme)));
	}

	[Fact]
	public void Json_WithoutThePrincipleChoices_ReadsThemAsToday()
	{
		MokaTheme? theme = MokaThemeSerializer.FromJson("""{ "isDark": true, "look": "Gate", "surfaces": "NearBlack" }""");

		Assert.NotNull(theme);
		Assert.Equal(MokaElevation.Shadow, theme.Elevation);
		Assert.Equal(MokaTextButtonColor.Accent, theme.TextButtons);
		Assert.Equal(MokaCalloutLook.Standard, theme.Callouts);
		Assert.Equal(MokaSwitchLook.Standard, theme.Switches);
		Assert.Equal(MokaSettingsHeaderLook.Standard, theme.SettingsHeaders);
		Assert.Equal("var(--moka-font-family-display)", theme.Typography.FontFamilyNumber);
		Assert.Equal("var(--moka-heading-tracking)", theme.Typography.NumberTracking);
	}

	[Theory]
	[InlineData("""{ "look": "Gate", "elevation": "Halo" }""")]
	[InlineData("""{ "look": "Gate", "textButtons": 1 }""")]
	[InlineData("""{ "look": "Gate", "callouts": "Stripe" }""")]
	[InlineData("""{ "look": "Gate", "switches": true }""")]
	[InlineData("""{ "look": "Gate", "settingsHeaders": "Glass" }""")]
	public void Json_RefusesAPrincipleChoiceItCantRead(string json) =>
		Assert.Null(MokaThemeSerializer.FromJson(json));

	[Fact]
	public void CSharp_WritesThePrincipleChoices()
	{
		Assert.Equal(Calls(".WithElevation(MokaElevation.Glow)"), MokaThemeSerializer.ToCSharp(MokaTheme.Gate.WithElevation(MokaElevation.Glow)));
		Assert.Equal(Calls(".WithTextButtons(MokaTextButtonColor.Neutral)"), MokaThemeSerializer.ToCSharp(MokaTheme.Gate.WithTextButtons(MokaTextButtonColor.Neutral)));
		Assert.Equal(Calls(".WithCallouts(MokaCalloutLook.Edge)"), MokaThemeSerializer.ToCSharp(MokaTheme.Gate.WithCallouts(MokaCalloutLook.Edge)));
		Assert.Equal(Calls(".WithSwitches(MokaSwitchLook.Tinted)"), MokaThemeSerializer.ToCSharp(MokaTheme.Gate.WithSwitches(MokaSwitchLook.Tinted)));
		Assert.Equal(Calls(".WithCurrentPlace(MokaCurrentPlace.SolidInset)"), MokaThemeSerializer.ToCSharp(MokaTheme.Gate.WithCurrentPlace(MokaCurrentPlace.SolidInset)));
		Assert.Equal(Calls(".WithTitleSize(MokaTitleSize.Compact)", ".WithNumberFont(MokaNumberFont.Mono)"),
			MokaThemeSerializer.ToCSharp(MokaTheme.Gate.WithNumberFont(MokaNumberFont.Mono).WithTitleSize(MokaTitleSize.Compact)));

		static string Calls(params string[] calls) => string.Join(Environment.NewLine + "\t", ["MokaTheme.Gate", .. calls]);
	}

	// The shortest chain wins: Ember with Red's surfaces is closer to it than Red with Ember's choices.
	[Fact]
	public void CSharp_StartsFromThePresetClosestToTheTheme() =>
		Assert.StartsWith("MokaTheme.Ember" + Environment.NewLine, MokaThemeSerializer.ToCSharp(MokaTheme.Ember.WithSurfaces(MokaSurfaces.Graphite)), StringComparison.Ordinal);

	[Theory]
	[MemberData(nameof(Presets))]
	public void CSharp_WritesAPresetByItsName(string name, MokaTheme preset) =>
		Assert.Equal($"MokaTheme.{name}", MokaThemeSerializer.ToCSharp(preset));

	public static TheoryData<string, MokaTheme> Presets => new()
	{
		{ nameof(MokaTheme.Red), MokaTheme.Red }, { nameof(MokaTheme.RedLight), MokaTheme.RedLight }, { nameof(MokaTheme.Gate), MokaTheme.Gate },
		{ nameof(MokaTheme.CrispLight), MokaTheme.CrispLight }, { nameof(MokaTheme.Ember), MokaTheme.Ember }, { nameof(MokaTheme.MatrixLight), MokaTheme.MatrixLight },
		{ nameof(MokaTheme.Dark), MokaTheme.Dark }, { nameof(MokaTheme.Light), MokaTheme.Light }
	};

	[Fact]
	public void CSharp_WritesASeedThroughFromSeed()
	{
		MokaTheme theme = MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#7C3AED", Neutral = "#1C1917", Contrast = MokaSeedContrast.More }, true)
			.WithDensity(MokaDensity.Compact)
			.WithToken("--moka-radius-card", "6px");

		string code = MokaThemeSerializer.ToCSharp(theme);

		Assert.StartsWith("MokaTheme.FromSeed(new MokaThemeSeed", code, StringComparison.Ordinal);
		Assert.Contains("Accent = \"#7C3AED\"", code, StringComparison.Ordinal);
		Assert.Contains("Contrast = MokaSeedContrast.More", code, StringComparison.Ordinal);
		Assert.Contains("}, dark: true)", code, StringComparison.Ordinal);
		Assert.Contains(".WithDensity(MokaDensity.Compact)", code, StringComparison.Ordinal);
		Assert.Contains(".WithToken(\"--moka-radius-card\", \"6px\")", code, StringComparison.Ordinal);
		Assert.DoesNotContain("new MokaPalette", code, StringComparison.Ordinal);
	}

	public static TheoryData<string, MokaTheme> RecipeThemes => new()
	{
		{ "seed", MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#0EA5E9", Neutral = "#64748B", Success = "#16A34A", NeutralChroma = 0.02 }, false) },
		{ "seeded crisp", MokaTheme.Crisp.WithSeed(new MokaThemeSeed { Accent = "#E11D48" }).WithHighContrast() },
		{ "seed with style and pins", MokaTheme.FromSeed(new MokaThemeSeed { Accent = "#F59E0B" }, true).WithCorners(MokaCorners.Tight)
			.WithLetterCase(MokaLetterCase.Controls).WithComponentStyle(MokaComponentStyle.Terminal).WithCharts(MokaChartLook.Quiet)
			.WithToken("--moka-color-surface", "#101418") },
		{ "accent preset", MokaTheme.GateLight.WithAccent(MokaAccent.Orchid).WithTextSize(MokaTextSize.Small) },
		{ "custom accent", MokaTheme.Red.WithAccent("#12B886").WithSurfaces(MokaSurfaces.BlueGrey) },
		{ "original accent", MokaTheme.Light.WithAccent("#0277bd").WithDensity(0.75) },
		{ "high contrast", MokaTheme.Matrix.WithHighContrast().WithTooltipArrows().WithDescriptions(false) }
	};

	[Theory]
	[MemberData(nameof(RecipeThemes))]
	public async Task CSharp_TheWithChain_CompilesBackToAnEqualTheme(string name, MokaTheme theme)
	{
		string code = MokaThemeSerializer.ToCSharp(theme);

		Assert.DoesNotContain("Palette = new", code, StringComparison.Ordinal);
		MokaTheme restored = await CSharpScript.EvaluateAsync<MokaTheme>(code, ScriptOptions.Default
			.AddReferences(typeof(MokaTheme).Assembly)
			.AddImports("Moka.Red.Core.Theming"), cancellationToken: TestContext.Current.CancellationToken);
		Assert.True(Equals(theme, restored), $"{name} came back different:{Environment.NewLine}{code}");
	}

	[Fact]
	public void CSharp_WritesAThemeNoChainReaches_AsAnInitializer()
	{
		MokaTheme theme = MokaTheme.Gate with { Palette = MokaTheme.Gate.Palette with { Surface = "#123456" } };

		Assert.StartsWith("new MokaTheme", MokaThemeSerializer.ToCSharp(theme), StringComparison.Ordinal);
	}

	[Fact]
	public void Css_WritesTheRootClassesAndTheColorScheme()
	{
		string dark = MokaThemeSerializer.ToCss(MokaTheme.Red.WithButtons(MokaButtonLook.Soft).WithTooltips(MokaTooltipLook.Glass));
		string light = MokaThemeSerializer.ToCss(MokaTheme.Light);

		Assert.StartsWith("/* Root classes: moka-root moka-dark moka-gate moka-popups-standard moka-buttons-soft moka-tooltips-glass */", dark, StringComparison.Ordinal);
		Assert.Contains("\tcolor-scheme: dark;", dark, StringComparison.Ordinal);
		Assert.Contains("\tbackground: var(--moka-color-background);", dark, StringComparison.Ordinal);
		Assert.StartsWith("/* Root classes: moka-root */", light, StringComparison.Ordinal);
		Assert.Contains("\tcolor-scheme: light;", light, StringComparison.Ordinal);
		Assert.DoesNotContain("\tbackground: var(", light, StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(ComponentLooks))]
	public void Json_RoundTripsEachComponentLook(string key, MokaTheme theme)
	{
		string json = MokaThemeSerializer.ToJson(theme);

		Assert.Contains($"\"{key}\":", json, StringComparison.Ordinal);
		Assert.Equal(theme, MokaThemeSerializer.FromJson(json));
	}

	[Fact]
	public void Json_RoundTripsAComponentStyle()
	{
		MokaTheme theme = MokaTheme.GateLight.WithComponentStyle(MokaComponentStyle.Terminal).WithToastStacking(MokaToastStacking.Pile);

		Assert.Equal(theme, MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(theme)));
	}

	// A theme exported before the component looks existed reads back with every one of them standard.
	[Fact]
	public void Json_WithoutComponentLooks_ReadsThemAsStandard()
	{
		MokaTheme? theme = MokaThemeSerializer.FromJson("""{ "isDark": true, "look": "Gate", "surfaces": "Graphite" }""");

		Assert.NotNull(theme);
		Assert.Equal(MokaToastLook.Standard, theme.Toasts);
		Assert.Equal(MokaToastStacking.List, theme.ToastStacking);
		Assert.Equal(MokaNotificationLook.Standard, theme.Notifications);
		Assert.Equal(MokaPopupLook.Standard, theme.Popups);
		Assert.Equal(MokaListLook.Standard, theme.Lists);
		Assert.Equal(MokaDialogLook.Standard, theme.Dialogs);
		Assert.Equal(MokaButtonLook.Standard, theme.Buttons);
		Assert.Equal(MokaTooltipLook.Standard, theme.Tooltips);
		Assert.False(theme.TooltipArrows);
		Assert.Equal(MokaTooltipMotion.Fade, theme.TooltipMotion);
		Assert.Equal(MokaTooltipSize.Standard, theme.TooltipSize);
	}

	[Fact]
	public void Json_RoundTripsEveryTooltipChoiceAtOnce()
	{
		MokaTheme theme = MokaTheme.GateLight
			.WithTooltips(MokaTooltipLook.Contrast)
			.WithTooltipArrows(true)
			.WithTooltipMotion(MokaTooltipMotion.Slide)
			.WithTooltipSize(MokaTooltipSize.Roomy);

		Assert.Equal(theme, MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(theme)));
	}

	[Theory]
	[InlineData("""{ "look": "Gate", "tooltipArrows": "yes" }""")]
	[InlineData("""{ "look": "Gate", "tooltips": "Shiny" }""")]
	[InlineData("""{ "look": "Gate", "tooltipMotion": 3 }""")]
	public void Json_RefusesATooltipChoiceItCantRead(string json) =>
		Assert.Null(MokaThemeSerializer.FromJson(json));

	[Fact]
	public void CSharp_WritesTheTooltipChoices()
	{
		string code = MokaThemeSerializer.ToCSharp(MokaTheme.Red.WithTooltips(MokaTooltipLook.Terminal).WithTooltipArrows(true)
			.WithTooltipMotion(MokaTooltipMotion.Instant).WithTooltipSize(MokaTooltipSize.Compact));

		Assert.Contains(".WithTooltips(MokaTooltipLook.Terminal)", code, StringComparison.Ordinal);
		Assert.Contains(".WithTooltipArrows(true)", code, StringComparison.Ordinal);
		Assert.Contains(".WithTooltipMotion(MokaTooltipMotion.Instant)", code, StringComparison.Ordinal);
		Assert.Contains(".WithTooltipSize(MokaTooltipSize.Compact)", code, StringComparison.Ordinal);
	}

	[Fact]
	public void Json_ReadsComponentLooksWhateverTheirCase()
	{
		MokaTheme? theme = MokaThemeSerializer.FromJson("""{ "look": "Gate", "Toasts": "toneedge", "POPUPS": "accentTint" }""");

		Assert.NotNull(theme);
		Assert.Equal(MokaToastLook.ToneEdge, theme.Toasts);
		Assert.Equal(MokaPopupLook.AccentTint, theme.Popups);
	}

	[Theory]
	[InlineData(MokaLabelTone.Muted)]
	[InlineData(MokaLabelTone.Pastel)]
	public void Json_RoundTripsTheLabelTone(MokaLabelTone tone)
	{
		MokaTheme theme = MokaTheme.RedLight.WithLabelTone(tone);
		string json = MokaThemeSerializer.ToJson(theme);

		Assert.Contains($"\"labelTone\": \"{tone}\"", json, StringComparison.Ordinal);
		Assert.Equal(theme, MokaThemeSerializer.FromJson(json));
	}

	// A theme exported before label colors existed reads back vivid, and the name matches whatever its case.
	[Fact]
	public void Json_ReadsTheLabelTone_WhateverItsCase_AndVividWhenMissing()
	{
		Assert.Equal(MokaLabelTone.Vivid, MokaThemeSerializer.FromJson("""{ "look": "Gate" }""")!.LabelTone);
		Assert.Equal(MokaLabelTone.Pastel, MokaThemeSerializer.FromJson("""{ "look": "Gate", "LABELTONE": "pastel" }""")!.LabelTone);
	}

	[Fact]
	public void Css_WritesTheLabelTokensOfTheTone()
	{
		string vivid = MokaThemeSerializer.ToCss(MokaTheme.Red);
		string muted = MokaThemeSerializer.ToCss(MokaTheme.Red.WithLabelTone(MokaLabelTone.Muted));

		Assert.Contains("\t--moka-label-teal: #14B8A6;", vivid, StringComparison.Ordinal);
		Assert.Contains("\t--moka-label-teal: #46B5A8;", muted, StringComparison.Ordinal);
		Assert.Contains("\t--moka-label-teal-bg: color-mix(in srgb, #46B5A8 20%, var(--moka-color-surface));", muted, StringComparison.Ordinal);
	}

	[Fact]
	public void CSharp_WritesTheLabelTone() =>
		Assert.Equal("MokaTheme.Gate" + Environment.NewLine + "\t.WithLabelTone(MokaLabelTone.Muted)",
			MokaThemeSerializer.ToCSharp(MokaTheme.Gate.WithLabelTone(MokaLabelTone.Muted)));

	[Fact]
	public void CSharp_WritesEachComponentLook()
	{
		string code = MokaThemeSerializer.ToCSharp(MokaTheme.Red.WithComponentStyle(MokaComponentStyle.Soft).WithToastStacking(MokaToastStacking.Pile));

		Assert.Contains(".WithToasts(MokaToastLook.IconTile)", code, StringComparison.Ordinal);
		Assert.Contains(".WithToastStacking(MokaToastStacking.Pile)", code, StringComparison.Ordinal);
		Assert.Contains(".WithNotifications(MokaNotificationLook.Inbox)", code, StringComparison.Ordinal);
		Assert.Contains(".WithPopups(MokaPopupLook.Elevated)", code, StringComparison.Ordinal);
		Assert.Contains(".WithLists(MokaListLook.Check)", code, StringComparison.Ordinal);
		Assert.Contains(".WithDialogs(MokaDialogLook.IconLead)", code, StringComparison.Ordinal);
		Assert.Contains(".WithButtons(MokaButtonLook.Soft)", code, StringComparison.Ordinal);
	}

	[Fact]
	public void CSharp_WritesTheDockLook() =>
		Assert.Equal("MokaTheme.Red" + Environment.NewLine + "\t.WithDocks(MokaDockLook.Cards)",
			MokaThemeSerializer.ToCSharp(MokaTheme.Red.WithDocks(MokaDockLook.Cards)));

	// A theme exported before dock looks existed reads back classic, and one the reader doesn't know is refused.
	[Fact]
	public void Json_ReadsTheDockLook_ClassicWhenMissing_AndRefusesAnUnknownOne()
	{
		Assert.Equal(MokaDockLook.Classic, MokaThemeSerializer.FromJson("""{ "look": "Gate" }""")!.Docks);
		Assert.Equal(MokaDockLook.Minimal, MokaThemeSerializer.FromJson("""{ "look": "Gate", "DOCKS": "minimal" }""")!.Docks);
		Assert.False(MokaThemeSerializer.TryFromJson("""{ "look": "Gate", "docks": "Floating" }""", out _, out string? error));
		Assert.Equal("docks must be one of Classic, Ide, Cards, Minimal, Terminal.", error);
	}

	// The table choices are null until a theme sets them, so the JSON carries them only then, and the C# names their
	// enums in full: they keep the table's namespace, which a file importing Moka.Red.Core.Theming alone doesn't have.
	[Fact]
	public async Task TheTableChoices_AreWrittenOnlyWhenSet_AndCompileBack()
	{
		MokaTheme theme = MokaTheme.Red.WithTables(MokaTableLook.Sheet).WithTableDensity(MokaTableDensity.Standard);

		Assert.DoesNotContain("\"tables\"", MokaThemeSerializer.ToJson(MokaTheme.Red), StringComparison.Ordinal);
		Assert.DoesNotContain("\"tableDensity\"", MokaThemeSerializer.ToJson(MokaTheme.Red), StringComparison.Ordinal);
		Assert.Null(MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(MokaTheme.Red))!.Tables);
		Assert.Equal(theme, MokaThemeSerializer.FromJson(MokaThemeSerializer.ToJson(theme)));

		string code = MokaThemeSerializer.ToCSharp(theme);
		Assert.Contains(".WithTables(Moka.Red.Data.Table.MokaTableLook.Sheet)", code, StringComparison.Ordinal);
		MokaTheme restored = await CSharpScript.EvaluateAsync<MokaTheme>(code, ScriptOptions.Default
			.AddReferences(typeof(MokaTheme).Assembly)
			.AddImports("Moka.Red.Core.Theming"), cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(theme, restored);
	}

	// The CSS export carries what the outermost provider writes, and a dock look isn't on its root: each layout reads it
	// from the cascaded theme.
	[Fact]
	public void Css_LeavesTheDockLookOut() =>
		Assert.Equal(MokaThemeSerializer.ToCss(MokaTheme.Red), MokaThemeSerializer.ToCss(MokaTheme.Red.WithDocks(MokaDockLook.Terminal)));

	[Fact]
	public void CSharp_EscapesQuotesAndBackslashes()
	{
		MokaTheme theme = MokaTheme.Light with
		{
			Typography = MokaTypography.Default with { FontFamily = "\"Fira Sans\", C:\\fonts" }
		};

		string code = MokaThemeSerializer.ToCSharp(theme);

		Assert.Contains("FontFamily = \"\\\"Fira Sans\\\", C:\\\\fonts\",", code, StringComparison.Ordinal);
	}
}
