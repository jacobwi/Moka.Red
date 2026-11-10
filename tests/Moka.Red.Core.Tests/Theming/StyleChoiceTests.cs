using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Theming;

// Colors and style are separate choices: picking one leaves the other as it was.
public class StyleChoiceTests
{
	public static TheoryData<string> Presets => ["Red", "Gate", "Crisp", "Matrix"];

	[Theory]
	[MemberData(nameof(Presets))]
	public void EveryChoice_LeavesTheColorsAlone(string preset)
	{
		MokaTheme theme = Preset(preset);

		MokaTheme changed = theme
			.WithCorners(MokaCorners.Medium)
			.WithDensity(MokaDensity.Compact)
			.WithTextSize(MokaTextSize.Small)
			.WithMainButton(MokaMainButton.Tinted)
			.WithLetterCase(MokaLetterCase.Uppercase)
			.WithTitleFont(MokaTitleFont.JetBrainsMono)
			.WithSelection(MokaSelection.Glow)
			.WithCurrentPlace(MokaCurrentPlace.Solid)
			.WithStatusBar(MokaStatusBarLook.Accent)
			.WithToasts(MokaToastLook.Pill)
			.WithToastStacking(MokaToastStacking.Pile)
			.WithNotifications(MokaNotificationLook.Inbox)
			.WithPopups(MokaPopupLook.Glass)
			.WithLists(MokaListLook.Check)
			.WithDialogs(MokaDialogLook.Centered)
			.WithButtons(MokaButtonLook.Raised);

		Assert.Equal(theme.Palette, changed.Palette);
		Assert.Equal(theme.Surfaces, changed.Surfaces);
		Assert.Equal(theme.IsDark, changed.IsDark);
	}

	[Theory]
	[InlineData(true, "block")]
	[InlineData(false, "none")]
	public void Descriptions_ShowOrHideThroughAToken(bool show, string display)
	{
		Assert.Contains($"--moka-about-display: {display}", MokaTheme.Red.WithDescriptions(show).ToCssVariables(), StringComparison.Ordinal);
	}

	[Fact]
	public void Descriptions_DoNothingOnTheOriginalDesign() =>
		Assert.Same(MokaTheme.Dark, MokaTheme.Dark.WithDescriptions(false));

	[Fact]
	public void PageTitles_AreTightInSentenceCase_AndFollowUppercase()
	{
		Assert.Contains("--moka-heading-tracking: -0.02em", MokaTheme.Red.ToCssVariables(), StringComparison.Ordinal);
		Assert.Contains("--moka-heading-tracking: var(--moka-title-tracking)",
			MokaTheme.Red.WithLetterCase(MokaLetterCase.Uppercase).ToCssVariables(), StringComparison.Ordinal);
		Assert.Contains("--moka-heading-tracking: var(--moka-title-tracking)", MokaTheme.Dark.ToCssVariables(), StringComparison.Ordinal);
	}

	[Fact]
	public void TheStyleOfOnePreset_TravelsToTheColorsOfAnother()
	{
		MokaTheme styled = MokaTheme.Red.WithDensity(MokaDensity.Compact).WithCorners(MokaCorners.Tight);

		MokaTheme matrixColors = styled.WithSurfaces(MokaSurfaces.NearBlack);

		Assert.Equal(MokaPalette.NearBlack.Surface, matrixColors.Palette.Surface);
		Assert.Equal("34px", matrixColors.Spacing.ControlHeight);
		Assert.Equal("12px", matrixColors.Spacing.RadiusXl);
	}

	[Fact]
	public void Corners_SetEveryRadius()
	{
		MokaSpacing soft = MokaTheme.Crisp.WithCorners(MokaCorners.Soft).Spacing;
		MokaSpacing tight = MokaTheme.Red.WithCorners(MokaCorners.Tight).Spacing;

		Assert.Equal(MokaSpacing.Gate.RadiusXl, soft.RadiusXl);
		Assert.Equal(MokaSpacing.Gate.RadiusCard, soft.RadiusCard);
		Assert.Equal(MokaSpacing.Crisp.RadiusLg, tight.RadiusLg);
		Assert.Equal(MokaSpacing.Crisp.RadiusCard, tight.RadiusCard);
	}

	[Fact]
	public void Density_SwapsTheLayoutAndLeavesTheCorners()
	{
		MokaSpacing compact = MokaTheme.Red.WithDensity(MokaDensity.Compact).Spacing;
		MokaSpacing back = MokaTheme.Red.WithDensity(MokaDensity.Compact).WithDensity(MokaDensity.Comfortable).Spacing;

		Assert.Equal("34px", compact.ControlHeight);
		Assert.Equal(MokaSpacing.Gate.RadiusXl, compact.RadiusXl);
		Assert.Equal(MokaSpacing.Gate, back);
	}

	[Theory]
	[InlineData(MokaDensity.Comfortable, "--moka-height-toolbar: 52px;", "--moka-height-toolbar-dense: 44px;", "--moka-height-statusbar: 28px;")]
	[InlineData(MokaDensity.Compact, "--moka-height-toolbar: 44px;", "--moka-height-toolbar-dense: 36px;", "--moka-height-statusbar: 24px;")]
	public void Density_SizesTheBars(MokaDensity density, string toolbar, string dense, string statusBar)
	{
		string css = MokaTheme.Red.WithDensity(density).ToCssVariables();

		Assert.Contains(toolbar, css, StringComparison.Ordinal);
		Assert.Contains(dense, css, StringComparison.Ordinal);
		Assert.Contains(statusBar, css, StringComparison.Ordinal);
	}

	[Fact]
	public void TheBars_KeepTheirSizeOnTheOriginalDesign()
	{
		string css = MokaTheme.Dark.ToCssVariables();

		Assert.Contains("--moka-height-toolbar: 40px;", css, StringComparison.Ordinal);
		Assert.Contains("--moka-height-toolbar-dense: 32px;", css, StringComparison.Ordinal);
		Assert.Contains("--moka-height-statusbar: 22px;", css, StringComparison.Ordinal);
	}

	[Fact]
	public void TextSize_RoundTrips()
	{
		MokaTypography small = MokaTheme.Red.WithTextSize(MokaTextSize.Small).Typography;
		MokaTypography back = MokaTheme.Red.WithTextSize(MokaTextSize.Small).WithTextSize(MokaTextSize.Normal).Typography;

		Assert.Equal(MokaTypography.Crisp.FontSizeMd, small.FontSizeMd);
		Assert.Equal(MokaTypography.Gate.FontFamilyDisplay, small.FontFamilyDisplay);
		Assert.Equal(MokaTypography.Gate, back);
	}

	[Theory]
	[InlineData(MokaTitleFont.SpaceGrotesk, "Space Grotesk")]
	[InlineData(MokaTitleFont.Inter, "--moka-font-family)")]
	[InlineData(MokaTitleFont.JetBrainsMono, "--moka-font-family-mono")]
	public void TitleFont_SetsTheDisplayFont(MokaTitleFont font, string expected) =>
		Assert.Contains(expected, MokaTheme.Crisp.WithTitleFont(font).Typography.FontFamilyDisplay, StringComparison.Ordinal);

	[Fact]
	public void LetterCase_SetsTheCaseTokens()
	{
		string upper = MokaTheme.Red.WithLetterCase(MokaLetterCase.Uppercase).ToCssVariables();
		string sentence = MokaTheme.Red.WithLetterCase(MokaLetterCase.Uppercase).WithLetterCase(MokaLetterCase.Sentence).ToCssVariables();

		Assert.Contains("--moka-control-case: uppercase;", upper, StringComparison.Ordinal);
		Assert.Contains("--moka-title-case: uppercase;", upper, StringComparison.Ordinal);
		Assert.Equal(MokaTheme.Red.ToCssVariables(), sentence);
	}

	[Theory]
	[InlineData(MokaMainButton.Solid, "--moka-button-primary-bg: var(--moka-color-primary);")]
	[InlineData(MokaMainButton.Tinted, "--moka-button-primary-bg: var(--moka-color-primary-bg);")]
	[InlineData(MokaMainButton.Outline, "--moka-button-primary-bg: transparent;")]
	public void MainButton_SetsItsTokens(MokaMainButton button, string expected) =>
		Assert.Contains(expected, MokaTheme.Red.WithMainButton(button).ToCssVariables(), StringComparison.Ordinal);

	[Theory]
	[InlineData(MokaSelection.Ring, "--moka-selected-glow: inset 0 0 0 1px")]
	[InlineData(MokaSelection.Flat, "--moka-selected-glow: none;")]
	[InlineData(MokaSelection.Glow, "--moka-selected-glow: inset 0 0 20px")]
	public void Selection_SetsItsTokens(MokaSelection selection, string expected) =>
		Assert.Contains(expected, MokaTheme.Red.WithSelection(selection).ToCssVariables(), StringComparison.Ordinal);

	[Fact]
	public void StrongGlow_AlsoReachesTheFocusRing() =>
		Assert.Contains("inset 0 0 12px", MokaTheme.Red.WithSelection(MokaSelection.Glow).ToCssVariables(), StringComparison.Ordinal);

	[Fact]
	public void NoChoice_MovesTheOriginalDesign()
	{
		foreach (MokaTheme original in new[] { MokaTheme.Dark, MokaTheme.Light })
		{
			Assert.Same(original, original.WithCorners(MokaCorners.Medium));
			Assert.Same(original, original.WithDensity(MokaDensity.Compact));
			Assert.Same(original, original.WithTextSize(MokaTextSize.Small));
			Assert.Same(original, original.WithMainButton(MokaMainButton.Tinted));
			Assert.Same(original, original.WithLetterCase(MokaLetterCase.Sentence));
			Assert.Same(original, original.WithTitleFont(MokaTitleFont.Inter));
			Assert.Same(original, original.WithSelection(MokaSelection.Flat));
			Assert.Same(original, original.WithCurrentPlace(MokaCurrentPlace.Solid));
			Assert.Same(original, original.WithStatusBar(MokaStatusBarLook.Accent));
			Assert.Same(original, original.WithToasts(MokaToastLook.Tinted));
			Assert.Same(original, original.WithToastStacking(MokaToastStacking.Pile));
			Assert.Same(original, original.WithNotifications(MokaNotificationLook.Cards));
			Assert.Same(original, original.WithPopups(MokaPopupLook.Flat));
			Assert.Same(original, original.WithLists(MokaListLook.Solid));
			Assert.Same(original, original.WithDialogs(MokaDialogLook.Bands));
			Assert.Same(original, original.WithButtons(MokaButtonLook.Pill));
			Assert.Same(original, original.WithComponentStyle(MokaComponentStyle.Terminal));
			Assert.Same(original, original.WithSurfaces(MokaSurfaces.Graphite));
		}
	}

	[Fact]
	public void Matrix_HasTheOriginalColorsOnTheDesignThatTakesChoices()
	{
		Assert.Equal(MokaLook.Gate, MokaTheme.Matrix.Look);
		Assert.Equal(MokaPalette.Dark.Background, MokaTheme.Matrix.Palette.Background, ignoreCase: true);
		Assert.Equal(MokaPalette.Dark.Surface, MokaTheme.Matrix.Palette.Surface, ignoreCase: true);
		Assert.Equal(MokaPalette.Dark.Primary, MokaTheme.Matrix.Palette.Primary, ignoreCase: true);
		Assert.Equal(MokaLook.Matrix, MokaTheme.Dark.Look);
	}

	[Fact]
	public void Crisp_HasTheColorsOfGate() => Assert.Equal(MokaTheme.Gate.Palette, MokaTheme.Crisp.Palette);

	// Where you are is marked like a selected row unless the theme asks for a solid tile, so nothing changes by default.
	[Fact]
	public void StatusBar_IsStandard_UntilAnAppSaysOtherwise()
	{
		Assert.Equal(MokaStatusBarLook.Standard, MokaTheme.Red.StatusBar);
		Assert.Equal(MokaStatusBarLook.Terminal, MokaTheme.Red.WithStatusBar(MokaStatusBarLook.Terminal).StatusBar);
	}

	[Fact]
	public void TheComponentLooks_AreStandard_UntilAnAppSaysOtherwise()
	{
		MokaTheme theme = MokaTheme.Red;

		Assert.Equal(MokaToastLook.Standard, theme.Toasts);
		Assert.Equal(MokaToastStacking.List, theme.ToastStacking);
		Assert.Equal(MokaNotificationLook.Standard, theme.Notifications);
		Assert.Equal(MokaPopupLook.Standard, theme.Popups);
		Assert.Equal(MokaListLook.Standard, theme.Lists);
		Assert.Equal(MokaDialogLook.Standard, theme.Dialogs);
		Assert.Equal(MokaButtonLook.Standard, theme.Buttons);
	}

	[Fact]
	public void AComponentStyle_SetsEveryLookButTheStacking()
	{
		MokaTheme piled = MokaTheme.Red.WithToastStacking(MokaToastStacking.Pile);

		MokaTheme soft = piled.WithComponentStyle(MokaComponentStyle.Soft);
		Assert.Equal(MokaToastLook.IconTile, soft.Toasts);
		Assert.Equal(MokaNotificationLook.Inbox, soft.Notifications);
		Assert.Equal(MokaPopupLook.Elevated, soft.Popups);
		Assert.Equal(MokaListLook.Check, soft.Lists);
		Assert.Equal(MokaDialogLook.IconLead, soft.Dialogs);
		Assert.Equal(MokaButtonLook.Soft, soft.Buttons);
		Assert.Equal(MokaToastStacking.Pile, soft.ToastStacking);

		MokaTheme terminal = soft.WithComponentStyle(MokaComponentStyle.Terminal);
		Assert.Equal(MokaToastLook.Terminal, terminal.Toasts);
		Assert.Equal(MokaNotificationLook.Terminal, terminal.Notifications);
		Assert.Equal(MokaPopupLook.Terminal, terminal.Popups);
		Assert.Equal(MokaListLook.Terminal, terminal.Lists);
		Assert.Equal(MokaDialogLook.Terminal, terminal.Dialogs);
		Assert.Equal(MokaButtonLook.Glow, terminal.Buttons);

		Assert.Equal(piled, terminal.WithComponentStyle(MokaComponentStyle.Standard));
	}

	// Each popup look sets only variables, and starts from all of them cleared, so a Standard panel inside another look
	// takes its own values again.
	[Fact]
	public void EveryPopupLook_StartsFromClearedVariables()
	{
		IReadOnlyDictionary<string, string> cleared = ScopedCss.Declarations("moka.css", ".moka-popups-standard");
		Assert.Equal(15, cleared.Count);
		Assert.All(cleared.Values, value => Assert.Equal("initial", value));

		foreach (MokaPopupLook look in Enum.GetValues<MokaPopupLook>())
		{
			string selector = $".moka-popups-{MokaEnumHelpers.ToCssClass(look)}";
			IReadOnlyDictionary<string, string> declared = ScopedCss.Declarations("moka.css", selector);
			Assert.All(declared.Keys, name => Assert.StartsWith("--moka-popup-", name, StringComparison.Ordinal));
			Assert.Equal(cleared.Keys.Order(), declared.Keys.Order());
		}
	}

	[Fact]
	public void CurrentPlace_IsTheSelectedLook_ByDefault()
	{
		string tokens = MokaTheme.Red.ToCssVariables();

		Assert.Equal(MokaCurrentPlace.Ring, MokaTheme.Red.CurrentPlace);
		Assert.Contains("--moka-current-bg: var(--moka-color-primary-row);", tokens, StringComparison.Ordinal);
		Assert.Contains("--moka-current-color: var(--moka-color-on-surface);", tokens, StringComparison.Ordinal);
		Assert.Contains("--moka-current-shadow: var(--moka-selected-glow);", tokens, StringComparison.Ordinal);
		Assert.Contains("--moka-current-pill-ring: none;", tokens, StringComparison.Ordinal);
	}

	[Fact]
	public void CurrentPlaceSolid_FillsWithTheAccent_InTheAccentsInk()
	{
		string tokens = MokaTheme.Red.WithCurrentPlace(MokaCurrentPlace.Solid).ToCssVariables();

		Assert.Contains("--moka-current-bg: var(--moka-color-primary);", tokens, StringComparison.Ordinal);
		Assert.Contains("--moka-current-color: var(--moka-color-on-primary);", tokens, StringComparison.Ordinal);
		Assert.Contains("--moka-current-icon: var(--moka-color-on-primary);", tokens, StringComparison.Ordinal);
		Assert.Contains("--moka-current-edge: var(--moka-color-on-primary);", tokens, StringComparison.Ordinal);
		Assert.Contains("--moka-current-shadow: 0 0 24px color-mix(in srgb, var(--moka-color-primary) 25%, transparent);", tokens,
			StringComparison.Ordinal);
	}

	// Rows picked in a table or a list keep WithSelection: a solid fill there would shout.
	[Fact]
	public void CurrentPlace_LeavesSelectionAlone()
	{
		MokaTheme solid = MokaTheme.Red.WithCurrentPlace(MokaCurrentPlace.Solid);

		Assert.Equal(MokaSelection.Ring, solid.Selection);
		Assert.Contains("--moka-selected-glow: inset 0 0 0 1px", solid.ToCssVariables(), StringComparison.Ordinal);
	}

	private static MokaTheme Preset(string name) => name switch
	{
		"Red" => MokaTheme.Red,
		"Gate" => MokaTheme.Gate,
		"Crisp" => MokaTheme.Crisp,
		_ => MokaTheme.Matrix
	};
}
