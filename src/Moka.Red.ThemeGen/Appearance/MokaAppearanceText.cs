namespace Moka.Red.ThemeGen.Appearance;

/// <summary>
///     The words <see cref="MokaAppearancePanel" /> writes. Set the ones to change, or all of them to translate it. The
///     names of palettes and accents are on <see cref="MokaAppearancePalette" /> and <c>MokaAccent</c>, which an app
///     passes with <c>with { Name = ... }</c>.
/// </summary>
public sealed record MokaAppearanceText
{
	/// <summary>The heading over the color mode, palette and accent rows.</summary>
	public string ColorsHeading { get; init; } = "Colors";

	/// <summary>The heading over the style rows.</summary>
	public string StyleHeading { get; init; } = "Style";

	/// <summary>The button that puts every choice back.</summary>
	public string Reset { get; init; } = "Back to defaults";

	/// <summary>Label of the color mode row.</summary>
	public string ColorMode { get; init; } = "Color mode";

	/// <summary>The light mode's card.</summary>
	public string ColorModeLight { get; init; } = "Light";

	/// <summary>The dark mode's card.</summary>
	public string ColorModeDark { get; init; } = "Dark";

	/// <summary>The card for following the device.</summary>
	public string ColorModeSystem { get; init; } = "System";

	/// <summary>The line under the card for following the device.</summary>
	public string ColorModeSystemHint { get; init; } = "Follows this device";

	/// <summary>Says what a click does when the color mode row is a toggle and would turn light mode on.</summary>
	public string ColorModeToLight { get; init; } = "Switch to light mode";

	/// <summary>Says what a click does when the color mode row is a toggle and would turn dark mode on.</summary>
	public string ColorModeToDark { get; init; } = "Switch to dark mode";

	/// <summary>Says what a click does when the color mode row is a toggle and would start following the device.</summary>
	public string ColorModeToSystem { get; init; } = "Follow this device";

	/// <summary>Label of the palette row.</summary>
	public string Palette { get; init; } = "Palette";

	/// <summary>Label of the accent row.</summary>
	public string Accent { get; init; } = "Accent";

	/// <summary>Said beside the accents while none is picked.</summary>
	public string AccentFromPalette { get; init; } = "The palette's own accent";

	/// <summary>The button that goes back to the palette's own accent.</summary>
	public string UsePaletteAccent { get; init; } = "Use the palette's own";

	/// <summary>Said in place of the style rows while the palette picked ignores them.</summary>
	public string StyleFixed { get; init; } = "This palette keeps its own style, so density and the other style choices don't apply to it. They come back with another palette.";

	/// <summary>Label of the density row.</summary>
	public string Density { get; init; } = "Density";

	/// <summary>The comfortable density.</summary>
	public string DensityComfortable { get; init; } = "Comfortable";

	/// <summary>The compact density.</summary>
	public string DensityCompact { get; init; } = "Compact";

	/// <summary>Label of the text size row.</summary>
	public string TextSize { get; init; } = "Text size";

	/// <summary>The normal text size.</summary>
	public string TextSizeNormal { get; init; } = "Normal";

	/// <summary>The small text size.</summary>
	public string TextSizeSmall { get; init; } = "Small";

	/// <summary>Label of the corners row.</summary>
	public string Corners { get; init; } = "Corners";

	/// <summary>The soft corners.</summary>
	public string CornersSoft { get; init; } = "Soft";

	/// <summary>The medium corners.</summary>
	public string CornersMedium { get; init; } = "Medium";

	/// <summary>The tight corners.</summary>
	public string CornersTight { get; init; } = "Tight";

	/// <summary>Label of the main button row.</summary>
	public string MainButton { get; init; } = "Main button";

	/// <summary>The solid main button.</summary>
	public string MainButtonSolid { get; init; } = "Solid";

	/// <summary>The tinted main button.</summary>
	public string MainButtonTinted { get; init; } = "Tinted";

	/// <summary>The outlined main button.</summary>
	public string MainButtonOutline { get; init; } = "Outline";

	/// <summary>Label of the letter case row.</summary>
	public string LetterCase { get; init; } = "Letter case";

	/// <summary>Sentence case.</summary>
	public string LetterCaseSentence { get; init; } = "Sentence";

	/// <summary>Uppercase.</summary>
	public string LetterCaseUppercase { get; init; } = "Uppercase";

	/// <summary>Label of the title font row.</summary>
	public string TitleFont { get; init; } = "Title font";

	/// <summary>The Space Grotesk title font.</summary>
	public string TitleFontSpaceGrotesk { get; init; } = "Space Grotesk";

	/// <summary>The Inter title font.</summary>
	public string TitleFontInter { get; init; } = "Inter";

	/// <summary>The JetBrains Mono title font.</summary>
	public string TitleFontJetBrainsMono { get; init; } = "JetBrains Mono";

	/// <summary>Label of the selection row.</summary>
	public string Selection { get; init; } = "Selected items";

	/// <summary>Selected things marked with a ring.</summary>
	public string SelectionRing { get; init; } = "Ring";

	/// <summary>Selected things marked with a flat tint.</summary>
	public string SelectionFlat { get; init; } = "Flat";

	/// <summary>Selected things marked with a glow.</summary>
	public string SelectionGlow { get; init; } = "Glow";

	/// <summary>Label of the current place row.</summary>
	public string CurrentPlace { get; init; } = "Current page";

	/// <summary>The current place marked with a ring.</summary>
	public string CurrentPlaceRing { get; init; } = "Ring";

	/// <summary>The current place filled with the accent.</summary>
	public string CurrentPlaceSolid { get; init; } = "Solid";

	/// <summary>Label of the status bar row.</summary>
	public string StatusBar { get; init; } = "Status bar";

	/// <summary>The standard status bar.</summary>
	public string StatusBarStandard { get; init; } = "Standard";

	/// <summary>The status bar in the accent.</summary>
	public string StatusBarAccent { get; init; } = "Accent";

	/// <summary>The hairline status bar.</summary>
	public string StatusBarHairline { get; init; } = "Hairline";

	/// <summary>The floating status bar.</summary>
	public string StatusBarFloating { get; init; } = "Floating";

	/// <summary>The status bar drawn as chips.</summary>
	public string StatusBarChips { get; init; } = "Chips";

	/// <summary>The status bar drawn as segments.</summary>
	public string StatusBarSegments { get; init; } = "Segments";

	/// <summary>The terminal status bar.</summary>
	public string StatusBarTerminal { get; init; } = "Terminal";

	/// <summary>The left end of the sample status bar on each card.</summary>
	public string StatusBarSampleStart { get; init; } = "Ready";

	/// <summary>The right end of the sample status bar on each card.</summary>
	public string StatusBarSampleEnd { get; init; } = "12";

	/// <summary>Label of the descriptions row.</summary>
	public string Descriptions { get; init; } = "Descriptions";

	/// <summary>Descriptions showing.</summary>
	public string DescriptionsShown { get; init; } = "Shown";

	/// <summary>Descriptions hidden.</summary>
	public string DescriptionsHidden { get; init; } = "Hidden";
}
