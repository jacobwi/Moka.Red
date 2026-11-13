using System.Text.Json;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.ThemeGen.Appearance;

namespace Moka.Red.ThemeGen.Tests.Components;

public class MokaAppearanceTests : BunitContext
{
	private static readonly Dictionary<string, (MokaAppearance Value, Func<MokaTheme, MokaTheme> Expected)> Choices = new()
	{
		["density"] = (new MokaAppearance { Density = MokaDensity.Compact }, t => t.WithDensity(MokaDensity.Compact)),
		["text size"] = (new MokaAppearance { TextSize = MokaTextSize.Small }, t => t.WithTextSize(MokaTextSize.Small)),
		["corners"] = (new MokaAppearance { Corners = MokaCorners.Tight }, t => t.WithCorners(MokaCorners.Tight)),
		["main button"] = (new MokaAppearance { MainButton = MokaMainButton.Outline }, t => t.WithMainButton(MokaMainButton.Outline)),
		["letter case"] = (new MokaAppearance { LetterCase = MokaLetterCase.Uppercase }, t => t.WithLetterCase(MokaLetterCase.Uppercase)),
		["title font"] = (new MokaAppearance { TitleFont = MokaTitleFont.JetBrainsMono }, t => t.WithTitleFont(MokaTitleFont.JetBrainsMono)),
		["selection"] = (new MokaAppearance { Selection = MokaSelection.Glow }, t => t.WithSelection(MokaSelection.Glow)),
		["current place"] = (new MokaAppearance { CurrentPlace = MokaCurrentPlace.Solid }, t => t.WithCurrentPlace(MokaCurrentPlace.Solid)),
		["status bar"] = (new MokaAppearance { StatusBar = MokaStatusBarLook.Terminal }, t => t.WithStatusBar(MokaStatusBarLook.Terminal)),
		["descriptions"] = (new MokaAppearance { Descriptions = false }, t => t.WithDescriptions(false)),
		["label tone"] = (new MokaAppearance { LabelTone = MokaLabelTone.Pastel }, t => t.WithLabelTone(MokaLabelTone.Pastel)),
		["toasts"] = (new MokaAppearance { Toasts = MokaToastLook.Pill }, t => t.WithToasts(MokaToastLook.Pill)),
		["toast stacking"] = (new MokaAppearance { ToastStacking = MokaToastStacking.Pile }, t => t.WithToastStacking(MokaToastStacking.Pile)),
		["notifications"] = (new MokaAppearance { Notifications = MokaNotificationLook.Timeline }, t => t.WithNotifications(MokaNotificationLook.Timeline)),
		["popups"] = (new MokaAppearance { Popups = MokaPopupLook.Contrast }, t => t.WithPopups(MokaPopupLook.Contrast)),
		["lists"] = (new MokaAppearance { Lists = MokaListLook.Solid }, t => t.WithLists(MokaListLook.Solid)),
		["dialogs"] = (new MokaAppearance { Dialogs = MokaDialogLook.Centered }, t => t.WithDialogs(MokaDialogLook.Centered)),
		["buttons"] = (new MokaAppearance { Buttons = MokaButtonLook.Offset }, t => t.WithButtons(MokaButtonLook.Offset)),
		["title size"] = (new MokaAppearance { TitleSize = MokaTitleSize.Compact }, t => t.WithTitleSize(MokaTitleSize.Compact)),
		["number font"] = (new MokaAppearance { NumberFont = MokaNumberFont.Mono }, t => t.WithNumberFont(MokaNumberFont.Mono)),
		["elevation"] = (new MokaAppearance { Elevation = MokaElevation.Glow }, t => t.WithElevation(MokaElevation.Glow)),
		["text buttons"] = (new MokaAppearance { TextButtons = MokaTextButtonColor.Neutral }, t => t.WithTextButtons(MokaTextButtonColor.Neutral)),
		["callouts"] = (new MokaAppearance { Callouts = MokaCalloutLook.Edge }, t => t.WithCallouts(MokaCalloutLook.Edge)),
		["switches"] = (new MokaAppearance { Switches = MokaSwitchLook.Tinted }, t => t.WithSwitches(MokaSwitchLook.Tinted)),
		["settings headers"] = (new MokaAppearance { SettingsHeaders = MokaSettingsHeaderLook.Tinted }, t => t.WithSettingsHeaders(MokaSettingsHeaderLook.Tinted)),
		["solid inset place"] = (new MokaAppearance { CurrentPlace = MokaCurrentPlace.SolidInset }, t => t.WithCurrentPlace(MokaCurrentPlace.SolidInset))
	};

	public static TheoryData<string> ChoiceNames => [.. Choices.Keys];

	[Fact]
	public void ANewValue_IsTheDefaultTheme() =>
		Assert.Same(MokaTheme.Default, new MokaAppearance().ToTheme(isDark: true));

	[Theory]
	[MemberData(nameof(ChoiceNames))]
	public void ToTheme_AppliesEachChoice(string name)
	{
		(MokaAppearance value, Func<MokaTheme, MokaTheme> expected) = Choices[name];

		Assert.Equal(expected(MokaTheme.Red), value.ToTheme(isDark: true));
	}

	[Theory]
	[MemberData(nameof(ChoiceNames))]
	public void ToTheme_DoesNothingWithStyleChoices_OnTheOriginalDesign(string name)
	{
		MokaTheme theme = (Choices[name].Value with { Palette = MokaAppearancePalette.Original.Id }).ToTheme(isDark: true);

		Assert.Equal(MokaTheme.Dark, theme);
	}

	[Fact]
	public void ToTheme_KeepsThePalettesOwnStyle_ForAChoiceLeftUnset()
	{
		MokaAppearancePalette[] palettes = [MokaAppearancePalette.Crisp];

		Assert.Same(MokaTheme.Crisp, new MokaAppearance().ToTheme(isDark: true, palettes));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ToTheme_TakesTheAccentsShadesForTheScheme(bool isDark)
	{
		MokaTheme theme = new MokaAppearance { Accent = MokaAccent.Violet.Id }.ToTheme(isDark);

		Assert.Equal(MokaAccent.Violet.For(isDark).Color, theme.Palette.Primary);
		Assert.Equal(isDark, theme.IsDark);
	}

	[Fact]
	public void ToTheme_AppliesAnAccent_OnTheOriginalDesign()
	{
		MokaTheme theme = new MokaAppearance { Palette = MokaAppearancePalette.Original.Id, Accent = MokaAccent.Cyan.Id }.ToTheme(isDark: true);

		Assert.Equal(MokaAccent.Cyan.Dark.Color, theme.Palette.Primary);
		Assert.Equal(MokaLook.Matrix, theme.Look);
	}

	[Fact]
	public void ToTheme_FallsBackToTheFirstPalette_ForAnIdNoLongerOffered() =>
		Assert.Same(MokaTheme.Red, new MokaAppearance { Palette = "gone" }.ToTheme(isDark: true));

	[Fact]
	public void ToTheme_FallsBackToThePalettesOwnAccent_ForAnIdNoLongerOffered() =>
		Assert.Same(MokaTheme.Gate, new MokaAppearance { Palette = "gate", Accent = "gone" }.ToTheme(isDark: true));

	[Fact]
	public void ToTheme_SurvivesAnEmptyPaletteList() =>
		Assert.Same(MokaTheme.Red, new MokaAppearance().ToTheme(isDark: true, palettes: []));

	[Fact]
	public void ToTheme_ReadsAnAppsOwnPalettesAndAccents()
	{
		MokaTheme brandDark = MokaTheme.Gate.WithSecondary("#112233");
		MokaAppearancePalette[] palettes = [MokaAppearancePalette.Red, new("brand", "Brand", brandDark, MokaTheme.GateLight)];
		MokaAccent accent = MokaAccent.Pink with { Id = "brand-pink" };

		MokaTheme theme = new MokaAppearance { Palette = "brand", Accent = "brand-pink" }.ToTheme(isDark: true, palettes, [accent]);

		Assert.Equal(brandDark.WithAccent(accent), theme);
	}

	[Theory]
	[InlineData(MokaColorMode.Light)]
	[InlineData(MokaColorMode.Dark)]
	public void ToThemes_GivesAFixedModeOneThemeForEveryDevice(MokaColorMode mode)
	{
		MokaAppearanceThemes themes = new MokaAppearance { Mode = mode }.ToThemes();

		Assert.Equal(mode == MokaColorMode.Dark, themes.Theme.IsDark);
		Assert.Same(themes.Theme, themes.DarkTheme);
		Assert.Same(themes.Theme, themes.LightTheme);
	}

	[Fact]
	public void ToThemes_GivesSystemBothForms_StartingDark()
	{
		MokaAppearanceThemes themes = new MokaAppearance { Mode = MokaColorMode.System, Density = MokaDensity.Compact }.ToThemes();

		Assert.True(themes.Theme.IsDark);
		Assert.True(themes.DarkTheme.IsDark);
		Assert.False(themes.LightTheme.IsDark);
		Assert.Equal(MokaTheme.RedLight.WithDensity(MokaDensity.Compact), themes.LightTheme);
	}

	[Fact]
	public void AFixedMode_StaysPut_OnADeviceSetTheOtherWay()
	{
		SetupThemeModule(prefersDark: false);

		IRenderedComponent<MokaThemeProvider> cut = RenderProvider(new MokaAppearance { Mode = MokaColorMode.Dark });

		cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations, i => i.Identifier == "watchColorScheme"));
		Assert.Contains("moka-dark", cut.Find(".moka-root").ClassName, StringComparison.Ordinal);
	}

	// The provider starts watching the device only on its first render, which is why ToThemes never asks to turn it on later.
	[Fact]
	public void PickingSystemAfterAFixedMode_FollowsTheDevice()
	{
		SetupThemeModule(prefersDark: false);
		IRenderedComponent<MokaThemeProvider> cut = RenderProvider(new MokaAppearance { Mode = MokaColorMode.Dark });
		cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations, i => i.Identifier == "watchColorScheme"));

		MokaAppearanceThemes system = new MokaAppearance { Mode = MokaColorMode.System }.ToThemes();
		cut.Render(p => p
			.Add(x => x.Theme, system.Theme)
			.Add(x => x.DarkTheme, system.DarkTheme)
			.Add(x => x.LightTheme, system.LightTheme));

		Assert.DoesNotContain("moka-dark", cut.Find(".moka-root").ClassName ?? "", StringComparison.Ordinal);
	}

	[Fact]
	public void AValue_RoundTripsThroughJson()
	{
		MokaAppearance value = new()
		{
			Mode = MokaColorMode.System,
			Palette = "gate",
			Accent = "cyan",
			Density = MokaDensity.Compact,
			StatusBar = MokaStatusBarLook.Chips,
			Descriptions = false,
			Toasts = MokaToastLook.ToneEdge,
			ToastStacking = MokaToastStacking.Pile,
			Notifications = MokaNotificationLook.Cards,
			Popups = MokaPopupLook.Glass,
			Lists = MokaListLook.Compact,
			Dialogs = MokaDialogLook.Bands,
			Buttons = MokaButtonLook.Raised
		};

		Assert.Equal(value, JsonSerializer.Deserialize<MokaAppearance>(JsonSerializer.Serialize(value)));
	}

	[Theory]
	[InlineData(MokaComponentStyle.Standard)]
	[InlineData(MokaComponentStyle.Soft)]
	[InlineData(MokaComponentStyle.Terminal)]
	public void WithComponentStyle_GivesTheThemeThatStyle(MokaComponentStyle style)
	{
		MokaAppearance value = new MokaAppearance { Toasts = MokaToastLook.Pill, ToastStacking = MokaToastStacking.Pile }.WithComponentStyle(style);

		Assert.Equal(MokaTheme.Red.WithToastStacking(MokaToastStacking.Pile).WithComponentStyle(style), value.ToTheme(isDark: true));
	}

	[Fact]
	public void WithComponentStyle_SetsTheSixLooks_AndLeavesTheRest()
	{
		MokaAppearance value = new MokaAppearance { Density = MokaDensity.Compact }.WithComponentStyle(MokaComponentStyle.Soft);

		Assert.Equal(new MokaAppearance
		{
			Density = MokaDensity.Compact,
			Toasts = MokaToastLook.IconTile,
			Notifications = MokaNotificationLook.Inbox,
			Popups = MokaPopupLook.Elevated,
			Lists = MokaListLook.Check,
			Dialogs = MokaDialogLook.IconLead,
			Buttons = MokaButtonLook.Soft
		}, value);
	}

	[Fact]
	public void Presets_LeaveOutTheOneThatLooksLikeAnother() =>
		Assert.DoesNotContain(MokaAppearancePalette.Crisp, MokaAppearancePalette.Presets);

	[Fact]
	public void Ember_IsOffered_AndKeepsItsOwnPicks_ForAChoiceLeftUnset()
	{
		Assert.Contains(MokaAppearancePalette.Ember, MokaAppearancePalette.Presets);
		Assert.Same(MokaTheme.Ember, new MokaAppearance { Palette = "ember" }.ToTheme(isDark: true));
		Assert.Same(MokaTheme.EmberLight, new MokaAppearance { Palette = "ember" }.ToTheme(isDark: false));
		Assert.True(MokaAppearancePalette.Ember.TakesStyleChoices);
	}

	// A person on Ember can still take any one pick back.
	[Fact]
	public void AChoice_OnEmber_ChangesThatPickAlone()
	{
		MokaTheme theme = new MokaAppearance { Palette = "ember", Elevation = MokaElevation.Shadow, NumberFont = MokaNumberFont.Title }.ToTheme(isDark: true);

		Assert.Equal(MokaTheme.Ember.WithElevation(MokaElevation.Shadow).WithNumberFont(MokaNumberFont.Title), theme);
		Assert.Equal(MokaCalloutLook.Edge, theme.Callouts);
	}

	[Fact]
	public void Presets_HaveDistinctIds() =>
		Assert.Equal(MokaAppearancePalette.Presets.Count, MokaAppearancePalette.Presets.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count());

	private IRenderedComponent<MokaThemeProvider> RenderProvider(MokaAppearance value)
	{
		MokaAppearanceThemes themes = value.ToThemes();
		return Render<MokaThemeProvider>(p => p
			.Add(x => x.AutoDetectColorScheme, true)
			.Add(x => x.Theme, themes.Theme)
			.Add(x => x.DarkTheme, themes.DarkTheme)
			.Add(x => x.LightTheme, themes.LightTheme));
	}

	private void SetupThemeModule(bool prefersDark)
	{
		BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Moka.Red.Core/moka-theme.js");
		module.Setup<bool>("prefersDarkColorScheme", _ => true).SetResult(prefersDark);
		module.Setup<int>("watchColorScheme", _ => true).SetResult(1);
	}
}
