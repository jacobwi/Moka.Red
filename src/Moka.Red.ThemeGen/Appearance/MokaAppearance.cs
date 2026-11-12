using Moka.Red.Core.Theming;

namespace Moka.Red.ThemeGen.Appearance;

/// <summary>
///     The themes a <see cref="MokaAppearance" /> needs, in the shape <see cref="MokaThemeProvider" /> takes them. Keep
///     <c>AutoDetectColorScheme</c> on whatever the mode is: the provider starts watching the device only on its first
///     render, so turning it on later would do nothing. For a fixed mode <see cref="DarkTheme" /> and
///     <see cref="LightTheme" /> are the same theme, so the device has no say.
/// </summary>
/// <param name="Theme">What shows before the device's setting is known, such as during a prerender.</param>
/// <param name="DarkTheme">What shows on a device set to dark.</param>
/// <param name="LightTheme">What shows on a device set to light.</param>
public sealed record MokaAppearanceThemes(MokaTheme Theme, MokaTheme DarkTheme, MokaTheme LightTheme);

/// <summary>
///     How someone wants an app to look: the value of <see cref="MokaAppearancePanel" />. It holds ids and plain choices,
///     so the app can store it however it likes. A choice left null keeps what the palette has, so a stored value says
///     only what the person changed.
/// </summary>
public sealed record MokaAppearance
{
	/// <summary>Light, dark, or following the device. Dark unless set.</summary>
	public MokaColorMode Mode { get; init; } = MokaColorMode.Dark;

	/// <summary>The <see cref="MokaAppearancePalette.Id" /> of the palette. Null, or an id no longer offered, is the first palette.</summary>
	public string? Palette { get; init; }

	/// <summary>The <see cref="MokaAccent.Id" /> of the accent. Null, or an id no longer offered, is the palette's own.</summary>
	public string? Accent { get; init; }

	/// <summary>How much room things take. Null keeps the palette's.</summary>
	public MokaDensity? Density { get; init; }

	/// <summary>How large text is. Null keeps the palette's.</summary>
	public MokaTextSize? TextSize { get; init; }

	/// <summary>How round things are. Null keeps the palette's.</summary>
	public MokaCorners? Corners { get; init; }

	/// <summary>How a primary button is drawn. Null keeps the palette's.</summary>
	public MokaMainButton? MainButton { get; init; }

	/// <summary>The letter case of buttons, chips, tabs and titles. Null keeps the palette's.</summary>
	public MokaLetterCase? LetterCase { get; init; }

	/// <summary>The font of page titles and big numbers. Null keeps the palette's.</summary>
	public MokaTitleFont? TitleFont { get; init; }

	/// <summary>How selected and focused things are marked. Null keeps the palette's.</summary>
	public MokaSelection? Selection { get; init; }

	/// <summary>How the place you are in the app is marked. Null keeps the palette's.</summary>
	public MokaCurrentPlace? CurrentPlace { get; init; }

	/// <summary>How status bars are drawn. Null keeps the palette's.</summary>
	public MokaStatusBarLook? StatusBar { get; init; }

	/// <summary>Whether text saying what a thing is for shows. Null keeps the palette's.</summary>
	public bool? Descriptions { get; init; }

	/// <summary>How strong the label colors on tags, chips, colored folders and swatches are. Null keeps the palette's.</summary>
	public MokaLabelTone? LabelTone { get; init; }

	/// <summary>How toasts are drawn. Null keeps the palette's.</summary>
	public MokaToastLook? Toasts { get; init; }

	/// <summary>How several toasts sit together. Null keeps the palette's.</summary>
	public MokaToastStacking? ToastStacking { get; init; }

	/// <summary>How notification lists are drawn. Null keeps the palette's.</summary>
	public MokaNotificationLook? Notifications { get; init; }

	/// <summary>How menus, popovers, hover cards and other floating panels are drawn. Null keeps the palette's.</summary>
	public MokaPopupLook? Popups { get; init; }

	/// <summary>How the rows of lists that open from a field are drawn. Null keeps the palette's.</summary>
	public MokaListLook? Lists { get; init; }

	/// <summary>How dialogs are drawn. Null keeps the palette's.</summary>
	public MokaDialogLook? Dialogs { get; init; }

	/// <summary>How buttons are drawn. Null keeps the palette's.</summary>
	public MokaButtonLook? Buttons { get; init; }

	/// <summary>How tooltips are drawn. Null keeps the palette's.</summary>
	public MokaTooltipLook? Tooltips { get; init; }

	/// <summary>Whether tooltips draw an arrow pointing at their trigger. Null keeps the palette's.</summary>
	public bool? TooltipArrows { get; init; }

	/// <summary>How tooltips come into view. Null keeps the palette's.</summary>
	public MokaTooltipMotion? TooltipMotion { get; init; }

	/// <summary>How much room a tooltip's text takes. Null keeps the palette's.</summary>
	public MokaTooltipSize? TooltipSize { get; init; }

	/// <summary>How charts are drawn. Null keeps the palette's.</summary>
	public MokaChartLook? Charts { get; init; }

	/// <summary>The colors chart series take in turn. Null keeps the palette's.</summary>
	public MokaChartPalette? ChartPalette { get; init; }

	/// <summary>Whether chart marks are hatched on screen. Null keeps the palette's.</summary>
	public MokaChartTexture? ChartTexture { get; init; }

	/// <summary>How charts print and show under forced colors. Null keeps the palette's.</summary>
	public MokaChartPrint? ChartPrint { get; init; }

	/// <summary>
	///     The theme for a known color scheme. For a value that may be <see cref="MokaColorMode.System" />, use
	///     <see cref="ToThemes" />. The result is a new theme each time, so call it when the value changes rather than
	///     while rendering: a new theme redraws everything under its provider.
	/// </summary>
	/// <param name="isDark">Whether to use the palette's dark form.</param>
	/// <param name="palettes">The palettes the panel was given. Null is <see cref="MokaAppearancePalette.Presets" />.</param>
	/// <param name="accents">The accents the panel was given. Null is <see cref="MokaAccent.Presets" />.</param>
	/// <returns>The palette's theme with this value's choices applied.</returns>
	public MokaTheme ToTheme(bool isDark, IReadOnlyList<MokaAppearancePalette>? palettes = null, IReadOnlyList<MokaAccent>? accents = null)
	{
		MokaAppearancePalette palette = PaletteIn(palettes ?? MokaAppearancePalette.Presets);
		MokaTheme theme = isDark ? palette.Dark : palette.Light;

		if (AccentIn(accents ?? MokaAccent.Presets) is { } accent)
		{
			theme = theme.WithAccent(accent);
		}

		if (Density is { } density)
		{
			theme = theme.WithDensity(density);
		}

		if (TextSize is { } textSize)
		{
			theme = theme.WithTextSize(textSize);
		}

		if (Corners is { } corners)
		{
			theme = theme.WithCorners(corners);
		}

		if (MainButton is { } mainButton)
		{
			theme = theme.WithMainButton(mainButton);
		}

		if (LetterCase is { } letterCase)
		{
			theme = theme.WithLetterCase(letterCase);
		}

		if (TitleFont is { } titleFont)
		{
			theme = theme.WithTitleFont(titleFont);
		}

		if (Selection is { } selection)
		{
			theme = theme.WithSelection(selection);
		}

		if (CurrentPlace is { } currentPlace)
		{
			theme = theme.WithCurrentPlace(currentPlace);
		}

		if (StatusBar is { } statusBar)
		{
			theme = theme.WithStatusBar(statusBar);
		}

		if (Descriptions is { } descriptions)
		{
			theme = theme.WithDescriptions(descriptions);
		}

		if (LabelTone is { } labelTone)
		{
			theme = theme.WithLabelTone(labelTone);
		}

		return WithChartChoices(WithComponentLooks(theme));
	}

	/// <summary>
	///     This value with toasts, notifications, popups, lists, dialogs and buttons set the way
	///     <see cref="MokaTheme.WithComponentStyle" /> sets them. <see cref="ToastStacking" /> stays as it is.
	/// </summary>
	/// <param name="style">The set of looks.</param>
	/// <returns>The new value.</returns>
	public MokaAppearance WithComponentStyle(MokaComponentStyle style)
	{
		// Read from a theme on the new design, which is the only one that takes the looks, so the sets live in one place.
		MokaTheme looks = MokaTheme.Red.WithComponentStyle(style);
		return this with
		{
			Toasts = looks.Toasts,
			Notifications = looks.Notifications,
			Popups = looks.Popups,
			Lists = looks.Lists,
			Dialogs = looks.Dialogs,
			Buttons = looks.Buttons
		};
	}

	private MokaTheme WithComponentLooks(MokaTheme theme)
	{
		if (Toasts is { } toasts)
		{
			theme = theme.WithToasts(toasts);
		}

		if (ToastStacking is { } toastStacking)
		{
			theme = theme.WithToastStacking(toastStacking);
		}

		if (Notifications is { } notifications)
		{
			theme = theme.WithNotifications(notifications);
		}

		if (Popups is { } popups)
		{
			theme = theme.WithPopups(popups);
		}

		if (Lists is { } lists)
		{
			theme = theme.WithLists(lists);
		}

		if (Dialogs is { } dialogs)
		{
			theme = theme.WithDialogs(dialogs);
		}

		if (Buttons is { } buttons)
		{
			theme = theme.WithButtons(buttons);
		}

		return WithTooltipChoices(theme);
	}

	private MokaTheme WithTooltipChoices(MokaTheme theme)
	{
		if (Tooltips is { } tooltips)
		{
			theme = theme.WithTooltips(tooltips);
		}

		if (TooltipArrows is { } tooltipArrows)
		{
			theme = theme.WithTooltipArrows(tooltipArrows);
		}

		if (TooltipMotion is { } tooltipMotion)
		{
			theme = theme.WithTooltipMotion(tooltipMotion);
		}

		return TooltipSize is { } tooltipSize ? theme.WithTooltipSize(tooltipSize) : theme;
	}

	private MokaTheme WithChartChoices(MokaTheme theme)
	{
		if (Charts is { } charts)
		{
			theme = theme.WithCharts(charts);
		}

		if (ChartPalette is { } chartPalette)
		{
			theme = theme.WithChartPalette(chartPalette);
		}

		if (ChartTexture is { } chartTexture)
		{
			theme = theme.WithChartTexture(chartTexture);
		}

		return ChartPrint is { } chartPrint ? theme.WithChartPrint(chartPrint) : theme;
	}

	/// <summary>
	///     The themes to hand to <see cref="MokaThemeProvider" />, for every <see cref="Mode" />. Call it when the value
	///     changes and keep the result, as with <see cref="ToTheme" />.
	/// </summary>
	/// <param name="palettes">The palettes the panel was given. Null is <see cref="MokaAppearancePalette.Presets" />.</param>
	/// <param name="accents">The accents the panel was given. Null is <see cref="MokaAccent.Presets" />.</param>
	/// <returns>The provider's <c>Theme</c>, <c>DarkTheme</c> and <c>LightTheme</c>.</returns>
	public MokaAppearanceThemes ToThemes(IReadOnlyList<MokaAppearancePalette>? palettes = null, IReadOnlyList<MokaAccent>? accents = null)
	{
		if (Mode != MokaColorMode.System)
		{
			MokaTheme fixedTheme = ToTheme(Mode == MokaColorMode.Dark, palettes, accents);
			return new MokaAppearanceThemes(fixedTheme, fixedTheme, fixedTheme);
		}

		MokaTheme dark = ToTheme(true, palettes, accents);
		return new MokaAppearanceThemes(dark, dark, ToTheme(false, palettes, accents));
	}

	internal MokaAppearancePalette PaletteIn(IReadOnlyList<MokaAppearancePalette> palettes) =>
		palettes.FirstOrDefault(palette => string.Equals(palette.Id, Palette, StringComparison.Ordinal))
		?? (palettes.Count > 0 ? palettes[0] : MokaAppearancePalette.Red);

	internal MokaAccent? AccentIn(IReadOnlyList<MokaAccent> accents) =>
		accents.FirstOrDefault(accent => string.Equals(accent.Id, Accent, StringComparison.Ordinal));
}
