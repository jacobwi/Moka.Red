using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Moka.Red.Core.Theming;
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
		string code = MokaThemeSerializer.ToCSharp(MokaTheme.Ember);

		Assert.Contains("Elevation = MokaElevation.Glow,", code, StringComparison.Ordinal);
		Assert.Contains("TextButtons = MokaTextButtonColor.Neutral,", code, StringComparison.Ordinal);
		Assert.Contains("Callouts = MokaCalloutLook.Edge,", code, StringComparison.Ordinal);
		Assert.Contains("Switches = MokaSwitchLook.Tinted,", code, StringComparison.Ordinal);
		Assert.Contains("SettingsHeaders = MokaSettingsHeaderLook.Tinted,", code, StringComparison.Ordinal);
		Assert.Contains("CurrentPlace = MokaCurrentPlace.SolidInset,", code, StringComparison.Ordinal);
		Assert.Contains("FontFamilyNumber = \"var(--moka-font-family-mono)\",", code, StringComparison.Ordinal);
		Assert.Contains("NumberTracking = \"0\"", code, StringComparison.Ordinal);
		Assert.Contains("FontSizeHeading = \"1.0625rem\",", code, StringComparison.Ordinal);
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

		Assert.Contains("Tooltips = MokaTooltipLook.Terminal,", code, StringComparison.Ordinal);
		Assert.Contains("TooltipArrows = true,", code, StringComparison.Ordinal);
		Assert.Contains("TooltipMotion = MokaTooltipMotion.Instant,", code, StringComparison.Ordinal);
		Assert.Contains("TooltipSize = MokaTooltipSize.Compact,", code, StringComparison.Ordinal);
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
		Assert.Contains("LabelTone = MokaLabelTone.Muted,",
			MokaThemeSerializer.ToCSharp(MokaTheme.Gate.WithLabelTone(MokaLabelTone.Muted)), StringComparison.Ordinal);

	[Fact]
	public void CSharp_WritesEachComponentLook()
	{
		string code = MokaThemeSerializer.ToCSharp(MokaTheme.Red.WithComponentStyle(MokaComponentStyle.Soft).WithToastStacking(MokaToastStacking.Pile));

		Assert.Contains("Toasts = MokaToastLook.IconTile,", code, StringComparison.Ordinal);
		Assert.Contains("ToastStacking = MokaToastStacking.Pile,", code, StringComparison.Ordinal);
		Assert.Contains("Notifications = MokaNotificationLook.Inbox,", code, StringComparison.Ordinal);
		Assert.Contains("Popups = MokaPopupLook.Elevated,", code, StringComparison.Ordinal);
		Assert.Contains("Lists = MokaListLook.Check,", code, StringComparison.Ordinal);
		Assert.Contains("Dialogs = MokaDialogLook.IconLead,", code, StringComparison.Ordinal);
		Assert.Contains("Buttons = MokaButtonLook.Soft,", code, StringComparison.Ordinal);
	}

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
