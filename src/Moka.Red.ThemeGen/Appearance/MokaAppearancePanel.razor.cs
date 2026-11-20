using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Primitives.Utility;

namespace Moka.Red.ThemeGen.Appearance;

/// <summary>
///     A ready-made panel where the person using an app picks how it looks. It behaves like an input: <see cref="Value" />
///     and <see cref="ValueChanged" /> carry a <see cref="MokaAppearance" />, every pick applies at once, and storing it is
///     the app's job. <see cref="MokaAppearance.ToThemes" /> turns the value into a theme.
/// </summary>
public partial class MokaAppearancePanel
{
	// The rows a palette on the original design ignores: it hides them and says why.
	private const MokaAppearanceRows StyleChoiceRows =
		MokaAppearanceRows.Style | MokaAppearanceRows.ComponentStyle | MokaAppearanceRows.Components | MokaAppearanceRows.ChartsGroup;

	private const MokaAppearanceRows ComponentRows = MokaAppearanceRows.ComponentStyle | MokaAppearanceRows.Components;

	private const int ChartSlots = 8;

	// Rows with too many options to sit beside their label, so their cards take the panel's width under it.
	private const MokaAppearanceRows SectionRows =
		MokaAppearanceRows.StatusBar | MokaAppearanceRows.Docks | MokaAppearanceRows.Tables | ComponentRows | MokaAppearanceRows.ChartsGroup;

	private readonly Dictionary<(MokaAppearanceRows Row, string Option), MokaTheme> _choicePreviews = [];
	private readonly Dictionary<PreviewKey, MokaTheme> _previews = [];
	private (MokaAppearance Value, bool Dark)? _choicePreviewsFor;
	private IReadOnlyList<MokaAccent>? _previewAccents;
	private IReadOnlyList<MokaAppearancePalette>? _previewPalettes;
	private MokaAppearance? _lastValue;
	private MokaAppearance _value = new();

	/// <summary>The current choices. Two-way bindable.</summary>
	[Parameter]
	public MokaAppearance Value { get; set; } = new();

	/// <summary>Raised with the new value on every pick.</summary>
	[Parameter]
	public EventCallback<MokaAppearance> ValueChanged { get; set; }

	/// <summary>Which rows show. Default is <see cref="MokaAppearanceRows.Default" />.</summary>
	[Parameter]
	public MokaAppearanceRows Rows { get; set; } = MokaAppearanceRows.Default;

	/// <summary>
	///     The palettes to pick from. Default is <see cref="MokaAppearancePalette.Presets" />. Pass the same list to
	///     <see cref="MokaAppearance.ToThemes" />.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaAppearancePalette> Palettes { get; set; } = MokaAppearancePalette.Presets;

	/// <summary>
	///     The accents to pick from. Default is <see cref="MokaAccent.Presets" />. Pass the same list to
	///     <see cref="MokaAppearance.ToThemes" />.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaAccent> Accents { get; set; } = MokaAccent.Presets;

	/// <summary>
	///     The swatches of the neutral row (<see cref="MokaAppearanceRows.Neutral" />). Default is
	///     <see cref="MokaAppearanceNeutral.Presets" />.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaAppearanceNeutral> Neutrals { get; set; } = MokaAppearanceNeutral.Presets;

	/// <summary>
	///     Whether the accent and neutral rows offer a color picker after their swatches, for a color of the person's own
	///     (<see cref="MokaAppearance.CustomAccent" /> and <see cref="MokaAppearance.CustomNeutral" />). The color picked gets
	///     a swatch of its own. Default false.
	/// </summary>
	[Parameter]
	public bool CustomColors { get; set; }

	/// <summary>
	///     Draws the color mode row as a <see cref="MokaThemeToggle" /> instead of cards. Null is cards, or
	///     <see cref="MokaThemeToggleLook.IconsAndWords" /> when <see cref="Compact" />. The row stays cards while
	///     <see cref="OptionState" /> hides light or dark, or disables a mode, which a toggle has no way to show.
	/// </summary>
	[Parameter]
	public MokaThemeToggleLook? ModeLook { get; set; }

	/// <summary>
	///     The form for a popover or a side sheet: no group headings, smaller cards, the color mode as a toggle, and plain
	///     segments on the style rows in place of their pictures. Default false.
	/// </summary>
	[Parameter]
	public bool Compact { get; set; }

	/// <summary>Whether a button puts every choice back while any differs from <see cref="Defaults" />. Default true.</summary>
	[Parameter]
	public bool ShowReset { get; set; } = true;

	/// <summary>What the reset button goes back to. Null is a new <see cref="MokaAppearance" />.</summary>
	[Parameter]
	public MokaAppearance? Defaults { get; set; }

	/// <summary>
	///     Says how each option is offered, for an app where some can't apply: one with no way to follow the device can hide
	///     <see cref="MokaColorMode.System" />. Null offers every option. It only changes what can be picked, so a value
	///     holding a hidden option keeps it until the person picks another.
	/// </summary>
	[Parameter]
	public Func<MokaAppearanceOption, MokaAppearanceOptionState>? OptionState { get; set; }

	/// <summary>Every word the panel writes.</summary>
	[Parameter]
	public MokaAppearanceText Text { get; set; } = new();

	/// <summary>
	///     The app's own rows, after the built-in ones, in the last group shown. <see cref="MokaAppearanceRow" /> matches
	///     them.
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     A content security policy nonce for the small previews, each of which is a <c>MokaThemeProvider</c>. Pass the one
	///     the app gives its own provider.
	/// </summary>
	[Parameter]
	public string? Nonce { get; set; }

	/// <inheritdoc />
	[Parameter]
	public override MokaSize Size { get; set; } = MokaSize.Sm;

	[CascadingParameter]
	private MokaTheme? AmbientTheme { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-appearance-panel";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-fill-width")
		.AddClass("moka-appearance-panel--disabled", Disabled)
		.AddClass("moka-appearance-panel--compact", Compact)
		.AddClass(Class)
		.Build();

	private MokaAppearancePalette CurrentPalette => _value.PaletteIn(Palettes);

	private MokaAccent? SelectedAccent => _value.AccentIn(Accents);

	// In System mode the picture follows what the page is showing right now, which is what the provider above already
	// worked out. Nothing here can ask the device.
	private bool IsDarkNow => _value.Mode switch
	{
		MokaColorMode.Light => false,
		MokaColorMode.Dark => true,
		_ => AmbientTheme?.IsDark ?? true
	};

	private bool ShowsModes => Shows(MokaAppearanceRows.ColorMode)
		&& OffersChoice(Enum.GetValues<MokaColorMode>().Where(ModeShows).Select(mode => mode.ToString()), _value.Mode.ToString());

	private IEnumerable<MokaAppearancePalette> ShownPalettes => Palettes.Where(palette => OptionShows(MokaAppearanceRows.Palette, palette.Id));

	private IEnumerable<MokaAccent> ShownAccents => Accents.Where(accent => OptionShows(MokaAppearanceRows.Accent, accent.Id));

	private bool ShowsPalettes => Shows(MokaAppearanceRows.Palette) && OffersChoice(ShownPalettes.Select(palette => palette.Id), CurrentPalette.Id);

	private bool ShowsAccents => Shows(MokaAppearanceRows.Accent) && ShownAccents.Any();

	private MokaThemeToggleLook? ModeToggleLook =>
		ModeShows(MokaColorMode.Light) && ModeShows(MokaColorMode.Dark) && !Enum.GetValues<MokaColorMode>().Any(ModeDisabled)
			? ModeLook ?? (Compact ? MokaThemeToggleLook.IconsAndWords : null)
			: null;

	private bool ShowsResetButton => ShowReset && !_value.Equals(ResetValue);

	private MokaAppearance ResetValue => Defaults ?? new MokaAppearance();

	private bool StyleIsFixed => (Rows & StyleChoiceRows) != 0 && !CurrentPalette.TakesStyleChoices;

	private List<string> AccentColors => [.. ShownAccents.Select(accent => accent.For(IsDarkNow).Color)];

	private string? SelectedAccentColor => SelectedAccent?.For(IsDarkNow).Color ?? _value.CustomAccent;

	private bool AccentIsOwn => SelectedAccent is null && _value.CustomAccent is null;

	private bool ShowsNeutral => Shows(MokaAppearanceRows.Neutral) && (CustomColors || ShownNeutrals.Any());

	private IEnumerable<MokaAppearanceNeutral> ShownNeutrals => Neutrals.Where(neutral => OptionShows(MokaAppearanceRows.Neutral, neutral.Color));

	private List<string> NeutralColors => [.. ShownNeutrals.Select(neutral => neutral.Color)];

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (!Equals(_lastValue, Value))
		{
			_lastValue = Value;
			_value = Value;
		}

		if (!ReferenceEquals(_previewPalettes, Palettes) || !ReferenceEquals(_previewAccents, Accents))
		{
			_previewPalettes = Palettes;
			_previewAccents = Accents;
			_previews.Clear();
			_choicePreviews.Clear();
		}
	}

	private bool Shows(MokaAppearanceRows row) =>
		(Rows & row) != 0 && ((row & (StyleChoiceRows | MokaAppearanceRows.Neutral)) == 0 || CurrentPalette.TakesStyleChoices);

	private MokaAppearanceOptionState StateOf(MokaAppearanceRows row, object value) =>
		OptionState?.Invoke(new MokaAppearanceOption(row, value)) ?? MokaAppearanceOptionState.Available;

	private bool OptionShows(MokaAppearanceRows row, object value) => StateOf(row, value) != MokaAppearanceOptionState.Hidden;

	private bool OptionDisabled(MokaAppearanceRows row, object value) => StateOf(row, value) == MokaAppearanceOptionState.Disabled;

	private bool ModeShows(MokaColorMode mode) => OptionShows(MokaAppearanceRows.ColorMode, mode);

	private bool ModeDisabled(MokaColorMode mode) => OptionDisabled(MokaAppearanceRows.ColorMode, mode);

	private bool AccentDisabled(string color) => AccentOf(color) is { } accent && OptionDisabled(MokaAppearanceRows.Accent, accent.Id);

	// The lone option left in a row is still a choice when it isn't the one already picked.
	private static bool OffersChoice(IEnumerable<string> shown, string? current)
	{
		List<string> options = [.. shown];
		return options.Count > 1 || (options.Count == 1 && options[0] != current);
	}

	// A provider compares its theme by reference and writes out about 190 tokens when it changes, so a preview keeps
	// the same instance from one render to the next.
	private MokaTheme PreviewTheme(MokaAppearancePalette palette, bool dark)
	{
		if (SelectedAccent is null && _value.CustomAccent is null && _value.CustomNeutral is null)
		{
			return dark ? palette.Dark : palette.Light;
		}

		PreviewKey key = new(palette.Id, dark, SelectedAccent?.Id, _value.CustomAccent, _value.CustomNeutral);
		if (!_previews.TryGetValue(key, out MokaTheme? tinted))
		{
			tinted = _value.WithColors(palette, dark, Accents);
			_previews[key] = tinted;
		}

		return tinted;
	}

	// The same reason as PreviewTheme, for a style row's option drawn on what's picked now.
	private MokaTheme ChoicePreview(MokaTheme theme, MokaAppearanceRows row, string option, Func<MokaTheme, MokaTheme> apply)
	{
		if (!_choicePreviews.TryGetValue((row, option), out MokaTheme? preview))
		{
			preview = apply(theme);
			_choicePreviews[(row, option)] = preview;
		}

		return preview;
	}

	private MokaAccent? AccentOf(string? color) =>
		Accents.FirstOrDefault(accent => string.Equals(accent.For(IsDarkNow).Color, color, StringComparison.OrdinalIgnoreCase));

	private string? AccentName(string color) => AccentOf(color)?.Name ?? Text.CustomAccent;

	private MokaAppearanceNeutral? NeutralOf(string? color) =>
		Neutrals.FirstOrDefault(neutral => string.Equals(neutral.Color, color, StringComparison.OrdinalIgnoreCase));

	private string? NeutralName(string color) => NeutralOf(color)?.Name ?? Text.CustomNeutral;

	private bool NeutralDisabled(string color) => NeutralOf(color) is { } neutral && OptionDisabled(MokaAppearanceRows.Neutral, neutral.Color);

	private Task ApplyAsync(MokaAppearance next)
	{
		_value = next;
		return ValueChanged.InvokeAsync(next);
	}

	private Task SetModeAsync(MokaColorMode mode) => ApplyAsync(_value with { Mode = mode });

	private Task SetPaletteAsync(string palette) => ApplyAsync(_value with { Palette = palette });

	private Task SetAccentColorAsync(string? color)
	{
		MokaAccent? preset = AccentOf(color);
		return ApplyAsync(_value with { Accent = preset?.Id, CustomAccent = preset is null && CustomColors ? color : null });
	}

	private Task UsePaletteAccentAsync() => ApplyAsync(_value with { Accent = null, CustomAccent = null });

	private Task SetNeutralAsync(string? color) => ApplyAsync(_value with { CustomNeutral = color });

	private Task UsePaletteNeutralAsync() => ApplyAsync(_value with { CustomNeutral = null });

	private Task ResetAsync() => ApplyAsync(ResetValue);

	private List<ChoiceRow> ChoiceRows()
	{
		MokaTheme theme = _value.ToTheme(IsDarkNow, Palettes, Accents);
		List<ChoiceRow> rows = [];

		if (_choicePreviewsFor != (_value, IsDarkNow))
		{
			_choicePreviewsFor = (_value, IsDarkNow);
			_choicePreviews.Clear();
		}

		AddRow(rows, MokaAppearanceRows.Density, CurrentChoice<MokaDensity>(theme, (t, v) => t.WithDensity(v)),
			[(MokaDensity.Comfortable, Text.DensityComfortable), (MokaDensity.Compact, Text.DensityCompact)],
			choice => _value with { Density = choice },
			choice => DensityMock(ChoicePreview(theme, MokaAppearanceRows.Density, choice.ToString(), t => t.WithDensity(choice))));

		AddRow(rows, MokaAppearanceRows.TextSize, CurrentChoice<MokaTextSize>(theme, (t, v) => t.WithTextSize(v)),
			[(MokaTextSize.Normal, Text.TextSizeNormal), (MokaTextSize.Small, Text.TextSizeSmall)],
			choice => _value with { TextSize = choice });

		AddRow(rows, MokaAppearanceRows.Corners, CurrentChoice<MokaCorners>(theme, (t, v) => t.WithCorners(v)),
			[(MokaCorners.Soft, Text.CornersSoft), (MokaCorners.Medium, Text.CornersMedium), (MokaCorners.Tight, Text.CornersTight)],
			choice => _value with { Corners = choice },
			choice => CornersMock(ChoicePreview(theme, MokaAppearanceRows.Corners, choice.ToString(), t => t.WithCorners(choice))));

		AddRow(rows, MokaAppearanceRows.Elevation, theme.Elevation,
			[(MokaElevation.Shadow, Text.ElevationShadow), (MokaElevation.Glow, Text.ElevationGlow), (MokaElevation.Line, Text.ElevationLine)],
			choice => _value with { Elevation = choice });

		AddRow(rows, MokaAppearanceRows.MainButton, CurrentChoice<MokaMainButton>(theme, (t, v) => t.WithMainButton(v)),
			[(MokaMainButton.Solid, Text.MainButtonSolid), (MokaMainButton.Tinted, Text.MainButtonTinted), (MokaMainButton.Outline, Text.MainButtonOutline)],
			choice => _value with { MainButton = choice },
			choice => MainButtonMock(ChoicePreview(theme, MokaAppearanceRows.MainButton, choice.ToString(), t => t.WithMainButton(choice))));

		AddRow(rows, MokaAppearanceRows.TextButtons, theme.TextButtons,
			[(MokaTextButtonColor.Accent, Text.TextButtonsAccent), (MokaTextButtonColor.Neutral, Text.TextButtonsNeutral)],
			choice => _value with { TextButtons = choice });

		AddRow(rows, MokaAppearanceRows.LetterCase, CurrentChoice<MokaLetterCase>(theme, (t, v) => t.WithLetterCase(v)),
			[(MokaLetterCase.Sentence, Text.LetterCaseSentence), (MokaLetterCase.Uppercase, Text.LetterCaseUppercase), (MokaLetterCase.Controls, Text.LetterCaseControls)],
			choice => _value with { LetterCase = choice });

		AddRow(rows, MokaAppearanceRows.TitleFont, CurrentChoice<MokaTitleFont>(theme, (t, v) => t.WithTitleFont(v)),
			[(MokaTitleFont.SpaceGrotesk, Text.TitleFontSpaceGrotesk), (MokaTitleFont.Inter, Text.TitleFontInter), (MokaTitleFont.JetBrainsMono, Text.TitleFontJetBrainsMono)],
			choice => _value with { TitleFont = choice });

		AddRow(rows, MokaAppearanceRows.TitleSize, CurrentChoice<MokaTitleSize>(theme, (t, v) => t.WithTitleSize(v)),
			[(MokaTitleSize.Display, Text.TitleSizeDisplay), (MokaTitleSize.Compact, Text.TitleSizeCompact)],
			choice => _value with { TitleSize = choice });

		AddRow(rows, MokaAppearanceRows.NumberFont, CurrentChoice<MokaNumberFont>(theme, (t, v) => t.WithNumberFont(v)),
			[(MokaNumberFont.Title, Text.NumberFontTitle), (MokaNumberFont.Mono, Text.NumberFontMono)],
			choice => _value with { NumberFont = choice });

		AddRow(rows, MokaAppearanceRows.Selection, CurrentChoice<MokaSelection>(theme, (t, v) => t.WithSelection(v)),
			[(MokaSelection.Ring, Text.SelectionRing), (MokaSelection.Flat, Text.SelectionFlat), (MokaSelection.Glow, Text.SelectionGlow)],
			choice => _value with { Selection = choice });

		AddRow(rows, MokaAppearanceRows.CurrentPlace, CurrentChoice<MokaCurrentPlace>(theme, (t, v) => t.WithCurrentPlace(v)),
			[(MokaCurrentPlace.Ring, Text.CurrentPlaceRing), (MokaCurrentPlace.Solid, Text.CurrentPlaceSolid), (MokaCurrentPlace.SolidInset, Text.CurrentPlaceSolidInset)],
			choice => _value with { CurrentPlace = choice });

		AddRow(rows, MokaAppearanceRows.StatusBar, CurrentChoice<MokaStatusBarLook>(theme, (t, v) => t.WithStatusBar(v)),
			[
				(MokaStatusBarLook.Standard, Text.StatusBarStandard), (MokaStatusBarLook.Accent, Text.StatusBarAccent),
				(MokaStatusBarLook.Hairline, Text.StatusBarHairline), (MokaStatusBarLook.Floating, Text.StatusBarFloating),
				(MokaStatusBarLook.Chips, Text.StatusBarChips), (MokaStatusBarLook.Segments, Text.StatusBarSegments),
				(MokaStatusBarLook.Terminal, Text.StatusBarTerminal)
			],
			choice => _value with { StatusBar = choice },
			StatusBarMock);

		AddRow(rows, MokaAppearanceRows.Docks, theme.Docks,
			[
				(MokaDockLook.Classic, Text.DocksClassic), (MokaDockLook.Ide, Text.DocksIde), (MokaDockLook.Cards, Text.DocksCards),
				(MokaDockLook.Minimal, Text.DocksMinimal), (MokaDockLook.Terminal, Text.DocksTerminal)
			],
			choice => _value with { Docks = choice },
			DocksArt);

		AddRow(rows, MokaAppearanceRows.Tables, ShownTableLook(theme),
			[
				(MokaTableLook.Lined, Text.TablesLined), (MokaTableLook.SoftStripes, Text.TablesSoftStripes), (MokaTableLook.Cards, Text.TablesCards),
				(MokaTableLook.Sheet, Text.TablesSheet), (MokaTableLook.Airy, Text.TablesAiry), (MokaTableLook.Framed, Text.TablesFramed),
				(MokaTableLook.List, Text.TablesList)
			],
			choice => _value with { Tables = choice },
			TablesArt);

		AddRow(rows, MokaAppearanceRows.TableDensity, ShownTableDensity(theme),
			[
				(MokaTableDensity.Compact, Text.TableDensityCompact), (MokaTableDensity.Standard, Text.TableDensityStandard),
				(MokaTableDensity.Comfortable, Text.TableDensityComfortable)
			],
			choice => _value with { TableDensity = choice });

		AddRow(rows, MokaAppearanceRows.Descriptions, theme.Descriptions.ToString(),
			[(true, Text.DescriptionsShown, null), (false, Text.DescriptionsHidden, null)],
			choice => ApplyAsync(_value with { Descriptions = bool.Parse(choice) }));

		AddRow(rows, MokaAppearanceRows.LabelTone, theme.LabelTone,
			[(MokaLabelTone.Vivid, Text.LabelToneVivid), (MokaLabelTone.Muted, Text.LabelToneMuted), (MokaLabelTone.Pastel, Text.LabelTonePastel)],
			choice => _value with { LabelTone = choice },
			choice => LabelToneMock(ChoicePreview(theme, MokaAppearanceRows.LabelTone, choice.ToString(), t => t.WithLabelTone(choice))));

		AddComponentRows(rows, theme);
		AddChartRows(rows, theme);

		return rows;
	}

	private void AddChartRows(List<ChoiceRow> rows, MokaTheme theme)
	{
		AddRow(rows, MokaAppearanceRows.Charts, theme.Charts,
			[
				(MokaChartLook.Standard, Text.ChartsStandard), (MokaChartLook.Quiet, Text.ChartsQuiet), (MokaChartLook.Dense, Text.ChartsDense),
				(MokaChartLook.Soft, Text.ChartsSoft), (MokaChartLook.Bold, Text.ChartsBold)
			],
			choice => _value with { Charts = choice },
			ChartLookArt);

		AddRow(rows, MokaAppearanceRows.ChartPalette, theme.ChartPalette,
			[
				(MokaChartPalette.Standard, Text.ChartPaletteStandard), (MokaChartPalette.Reference, Text.ChartPaletteReference),
				(MokaChartPalette.AccentFirst, Text.ChartPaletteAccentFirst), (MokaChartPalette.Muted, Text.ChartPaletteMuted)
			],
			choice => _value with { ChartPalette = choice },
			choice => ChartPaletteArt(ChoicePreview(theme, MokaAppearanceRows.ChartPalette, choice.ToString(), t => t.WithChartPalette(choice))));

		(MokaChartTextureChoice Choice, string Text)[] textures =
		[
			(MokaChartTextureChoice.ColorOnly, Text.ChartTextureColorOnly), (MokaChartTextureChoice.HatchedInPrint, Text.ChartTextureHatchedInPrint),
			(MokaChartTextureChoice.InkInPrint, Text.ChartTextureInkInPrint), (MokaChartTextureChoice.AlwaysHatched, Text.ChartTextureAlwaysHatched)
		];
		AddRow(rows, MokaAppearanceRows.ChartTexture, new MokaChartTextureChoice(theme.ChartTexture, theme.ChartPrint).ToString(),
			[.. textures.Select(texture => ((object)texture.Choice, texture.Text,
				Compact ? null : ChartTextureArt(texture.Choice, ChoicePreview(theme, MokaAppearanceRows.ChartTexture, texture.Choice.ToString(), t => t))))],
			choice =>
			{
				MokaChartTextureChoice picked = textures.First(texture => texture.Choice.ToString() == choice).Choice;
				return ApplyAsync(_value with { ChartTexture = picked.Texture, ChartPrint = picked.Print });
			});
	}

	private void AddComponentRows(List<ChoiceRow> rows, MokaTheme theme)
	{
		AddRow(rows, MokaAppearanceRows.ComponentStyle, CurrentChoice<MokaComponentStyle>(theme, (t, v) => t.WithComponentStyle(v)),
			[
				(MokaComponentStyle.Standard, Text.ComponentStyleStandard), (MokaComponentStyle.Soft, Text.ComponentStyleSoft),
				(MokaComponentStyle.Terminal, Text.ComponentStyleTerminal)
			],
			choice => _value.WithComponentStyle(choice),
			ComponentStyleArt);

		AddRow(rows, MokaAppearanceRows.Toasts, theme.Toasts,
			[
				(MokaToastLook.Standard, Text.ToastsStandard), (MokaToastLook.ToneEdge, Text.ToastsToneEdge), (MokaToastLook.Tinted, Text.ToastsTinted),
				(MokaToastLook.IconTile, Text.ToastsIconTile), (MokaToastLook.Snackbar, Text.ToastsSnackbar), (MokaToastLook.Pill, Text.ToastsPill),
				(MokaToastLook.Terminal, Text.ToastsTerminal)
			],
			choice => _value with { Toasts = choice },
			ToastArt);

		AddRow(rows, MokaAppearanceRows.ToastStacking, theme.ToastStacking,
			[(MokaToastStacking.List, Text.ToastStackingList), (MokaToastStacking.Pile, Text.ToastStackingPile)],
			choice => _value with { ToastStacking = choice },
			ToastStackingArt);

		AddRow(rows, MokaAppearanceRows.Notifications, theme.Notifications,
			[
				(MokaNotificationLook.Standard, Text.NotificationsStandard), (MokaNotificationLook.Inbox, Text.NotificationsInbox),
				(MokaNotificationLook.Cards, Text.NotificationsCards), (MokaNotificationLook.Compact, Text.NotificationsCompact),
				(MokaNotificationLook.Grouped, Text.NotificationsGrouped), (MokaNotificationLook.Timeline, Text.NotificationsTimeline),
				(MokaNotificationLook.Terminal, Text.NotificationsTerminal)
			],
			choice => _value with { Notifications = choice },
			NotificationsArt);

		AddRow(rows, MokaAppearanceRows.Popups, theme.Popups,
			[
				(MokaPopupLook.Standard, Text.PopupsStandard), (MokaPopupLook.Flat, Text.PopupsFlat), (MokaPopupLook.Elevated, Text.PopupsElevated),
				(MokaPopupLook.Glass, Text.PopupsGlass), (MokaPopupLook.AccentTint, Text.PopupsAccentTint), (MokaPopupLook.Contrast, Text.PopupsContrast),
				(MokaPopupLook.Terminal, Text.PopupsTerminal)
			],
			choice => _value with { Popups = choice },
			PopupsArt);

		AddRow(rows, MokaAppearanceRows.Lists, theme.Lists,
			[
				(MokaListLook.Standard, Text.ListsStandard), (MokaListLook.Check, Text.ListsCheck), (MokaListLook.Solid, Text.ListsSolid),
				(MokaListLook.Attached, Text.ListsAttached), (MokaListLook.Radio, Text.ListsRadio), (MokaListLook.Compact, Text.ListsCompact),
				(MokaListLook.Terminal, Text.ListsTerminal)
			],
			choice => _value with { Lists = choice },
			ListsArt);

		AddRow(rows, MokaAppearanceRows.Dialogs, theme.Dialogs,
			[
				(MokaDialogLook.Standard, Text.DialogsStandard), (MokaDialogLook.Bands, Text.DialogsBands), (MokaDialogLook.IconLead, Text.DialogsIconLead),
				(MokaDialogLook.Centered, Text.DialogsCentered), (MokaDialogLook.Terminal, Text.DialogsTerminal)
			],
			choice => _value with { Dialogs = choice },
			DialogsArt);

		AddRow(rows, MokaAppearanceRows.Buttons, theme.Buttons,
			[
				(MokaButtonLook.Standard, Text.ButtonsStandard), (MokaButtonLook.Raised, Text.ButtonsRaised), (MokaButtonLook.Soft, Text.ButtonsSoft),
				(MokaButtonLook.Pill, Text.ButtonsPill), (MokaButtonLook.Glow, Text.ButtonsGlow), (MokaButtonLook.Offset, Text.ButtonsOffset),
				(MokaButtonLook.Gradient, Text.ButtonsGradient), (MokaButtonLook.Ghost, Text.ButtonsGhost)
			],
			choice => _value with { Buttons = choice },
			ButtonsArt);

		AddRow(rows, MokaAppearanceRows.Tooltips, theme.Tooltips,
			[
				(MokaTooltipLook.Standard, Text.TooltipsStandard), (MokaTooltipLook.Flat, Text.TooltipsFlat), (MokaTooltipLook.Elevated, Text.TooltipsElevated),
				(MokaTooltipLook.Glass, Text.TooltipsGlass), (MokaTooltipLook.AccentTint, Text.TooltipsAccentTint), (MokaTooltipLook.Contrast, Text.TooltipsContrast),
				(MokaTooltipLook.Terminal, Text.TooltipsTerminal)
			],
			choice => _value with { Tooltips = choice },
			TooltipsArt);

		AddRow(rows, MokaAppearanceRows.TooltipArrows, theme.TooltipArrows.ToString(),
			[(false, Text.TooltipArrowsHidden, null), (true, Text.TooltipArrowsShown, null)],
			choice => ApplyAsync(_value with { TooltipArrows = bool.Parse(choice) }));

		AddRow(rows, MokaAppearanceRows.TooltipMotion, theme.TooltipMotion,
			[
				(MokaTooltipMotion.Fade, Text.TooltipMotionFade), (MokaTooltipMotion.Slide, Text.TooltipMotionSlide),
				(MokaTooltipMotion.Grow, Text.TooltipMotionGrow), (MokaTooltipMotion.Instant, Text.TooltipMotionInstant)
			],
			choice => _value with { TooltipMotion = choice });

		AddRow(rows, MokaAppearanceRows.TooltipSize, theme.TooltipSize,
			[(MokaTooltipSize.Compact, Text.TooltipSizeCompact), (MokaTooltipSize.Standard, Text.TooltipSizeStandard), (MokaTooltipSize.Roomy, Text.TooltipSizeRoomy)],
			choice => _value with { TooltipSize = choice });

		AddRow(rows, MokaAppearanceRows.DisabledButtons, theme.DisabledButtons,
			[
				(MokaDisabledButtonLook.Faded, Text.DisabledButtonsFaded), (MokaDisabledButtonLook.Muted, Text.DisabledButtonsMuted),
				(MokaDisabledButtonLook.Outline, Text.DisabledButtonsOutline)
			],
			choice => _value with { DisabledButtons = choice });

		AddRow(rows, MokaAppearanceRows.ControlHeights, theme.ControlHeights,
			[(MokaControlHeights.Standard, Text.ControlHeightsStandard), (MokaControlHeights.Matched, Text.ControlHeightsMatched)],
			choice => _value with { ControlHeights = choice });

		AddRow(rows, MokaAppearanceRows.Callouts, theme.Callouts,
			[(MokaCalloutLook.Standard, Text.CalloutsStandard), (MokaCalloutLook.Edge, Text.CalloutsEdge)],
			choice => _value with { Callouts = choice });

		AddRow(rows, MokaAppearanceRows.Switches, theme.Switches,
			[(MokaSwitchLook.Standard, Text.SwitchesStandard), (MokaSwitchLook.Tinted, Text.SwitchesTinted)],
			choice => _value with { Switches = choice });

		AddRow(rows, MokaAppearanceRows.SettingsHeaders, theme.SettingsHeaders,
			[(MokaSettingsHeaderLook.Standard, Text.SettingsHeadersStandard), (MokaSettingsHeaderLook.Tinted, Text.SettingsHeadersTinted)],
			choice => _value with { SettingsHeaders = choice });
	}

	private void AddRow<TChoice>(List<ChoiceRow> rows, MokaAppearanceRows row, TChoice? current, (TChoice Choice, string Text)[] options,
		Func<TChoice, MokaAppearance> withChoice, Func<TChoice, RenderFragment>? preview = null)
		where TChoice : struct, Enum =>
		AddRow(rows, row, current?.ToString(),
			[.. options.Select(option => ((object)option.Choice, option.Text, Compact || preview is null ? null : preview(option.Choice)))],
			choice => ApplyAsync(withChoice(Enum.Parse<TChoice>(choice))));

	private void AddRow(List<ChoiceRow> rows, MokaAppearanceRows row, string? current, (object Choice, string Text, RenderFragment? Preview)[] options, Func<string, Task> pick)
	{
		if (!Shows(row))
		{
			return;
		}

		List<ChoiceOption> shown = [.. options
			.Where(option => OptionShows(row, option.Choice))
			.Select(option => new ChoiceOption(option.Choice.ToString()!, option.Text, OptionDisabled(row, option.Choice), option.Preview))];

		if (OffersChoice(shown.Select(option => option.Value), current))
		{
			rows.Add(new ChoiceRow(row, Text.LabelOf(row), current, shown, pick));
		}
	}

	// Corners, density and the like live in the theme's spacing and type tokens, with nothing saying which option made
	// them, so an option is current when applying it changes nothing. A theme tuned by hand may match none.
	private static TChoice? CurrentChoice<TChoice>(MokaTheme theme, Func<MokaTheme, TChoice, MokaTheme> apply)
		where TChoice : struct, Enum
	{
		foreach (TChoice choice in Enum.GetValues<TChoice>())
		{
			if (apply(theme, choice).Equals(theme))
			{
				return choice;
			}
		}

		return null;
	}

	// A theme without a table look draws Lined, and without a density each table's look picks its own: Comfortable for
	// Airy, Compact for the rest. The rows show what tables draw, as the other rows do.
	private static MokaTableLook ShownTableLook(MokaTheme theme) => theme.Tables ?? MokaTableLook.Lined;

	private static MokaTableDensity ShownTableDensity(MokaTheme theme) =>
		theme.TableDensity ?? (ShownTableLook(theme) == MokaTableLook.Airy ? MokaTableDensity.Comfortable : MokaTableDensity.Compact);

	private static readonly MokaLabelColor[] ToneSamples = [MokaLabelColor.Teal, MokaLabelColor.Amber, MokaLabelColor.Purple];

	private static string? LabelSampleStyle(MokaLabelColor label) => new StyleBuilder()
		.AddStyle("--_label", MokaLabelColors.Solid(label))
		.AddStyle("--_label-bg", MokaLabelColors.Background(label))
		.AddStyle("--_label-ink", MokaLabelColors.Ink(label))
		.Build();

	private static string? MiniStyle(ChoiceRow row) => new StyleBuilder()
		.AddStyle("--_appearance-options", row.Options.Count.ToString(CultureInfo.InvariantCulture))
		.Build();

	private static string SectionClass(ChoiceRow row) => new CssBuilder("moka-appearance-panel__section")
		.AddClass("moka-appearance-panel__section--looks", row.Group != RowGroup.Style)
		.Build();

	// The new looks share most of their picture, so it hangs on one class and each look adds what sets it apart.
	private static string ChartLookClass(MokaChartLook look) => new CssBuilder(LookClass("charts", look))
		.AddClass("moka-appearance-panel__charts--modern", look != MokaChartLook.Standard)
		.AddClass($"moka-chart-look-{MokaEnumHelpers.ToCssClass(look)}")
		.Build();

	private static string ChartSeriesClass(int slot, bool hatched) => new CssBuilder($"moka-chart-c{slot.ToString(CultureInfo.InvariantCulture)}")
		.AddClass("moka-chart-hatch", hatched)
		.Build();

	// Screen and page each carry the real texture class when they're hatched, so moka.css draws the hatching; ink is
	// print-only in moka.css, so the panel draws the page's ink itself.
	private static string ChartScreenClass(MokaChartTextureChoice texture) => new CssBuilder("moka-appearance-panel__screen")
		.AddClass("moka-chart-texture", texture.Texture == MokaChartTexture.Hatched)
		.Build();

	private static string ChartPageClass(MokaChartTextureChoice texture) => new CssBuilder("moka-appearance-panel__page")
		.AddClass("moka-chart-texture", texture.Texture == MokaChartTexture.Hatched || texture.Print == MokaChartPrint.Hatched)
		.AddClass("moka-appearance-panel__page--ink", texture.Print == MokaChartPrint.Ink)
		.Build();

	// A picture's classes name its row and its look, so the stylesheet draws each look from those two alone.
	private static string LookClass<TLook>(string row, TLook look) where TLook : struct, Enum => new CssBuilder("moka-appearance-panel__look")
		.AddClass(RowClass(row, look))
		.Build();

	private static string RowClass<TLook>(string row, TLook look) where TLook : struct, Enum => new CssBuilder($"moka-appearance-panel__{row}")
		.AddClass($"moka-appearance-panel__{row}--{MokaEnumHelpers.ToCssClass(look)}")
		.Build();

	// The tiny menu carries the real popup class from moka.css, so its picture reads the variables the real panels do.
	private static string PopupMenuClass(MokaPopupLook look) => new CssBuilder("moka-appearance-panel__menu")
		.AddClass($"moka-popups-{MokaEnumHelpers.ToCssClass(look)}")
		.Build();

	// The tiny tooltip carries the real tooltip class from moka.css, as the popup picture does.
	private static string TooltipBoxClass(MokaTooltipLook look) => new CssBuilder("moka-appearance-panel__tip")
		.AddClass($"moka-tooltips-{MokaEnumHelpers.ToCssClass(look)}")
		.Build();

	private static string TableRowClass(int row) => new CssBuilder("moka-appearance-panel__table-row")
		.AddClass("moka-appearance-panel__table-row--head", row == 0)
		.Build();

	private static string ToneClass(string block, MokaColor tone) => new CssBuilder(block)
		.AddClass($"moka-appearance-panel__tone--{MokaEnumHelpers.ToCssClass(tone)}")
		.Build();

	private static string NoteClass(MokaColor tone, bool unread, bool extra) => new CssBuilder(ToneClass("moka-appearance-panel__note", tone))
		.AddClass("moka-appearance-panel__note--unread", unread)
		.AddClass("moka-appearance-panel__note--extra", extra)
		.Build();

	private static string OptionClass(bool chosen, bool lit) => new CssBuilder("moka-appearance-panel__option")
		.AddClass("moka-appearance-panel__option--chosen", chosen)
		.AddClass("moka-appearance-panel__option--lit", lit)
		.Build();

	// A component style is drawn as the toast and the buttons it picks, read from the theme so the picture can't drift
	// from what the style sets.
	private static MokaTheme PackOf(MokaComponentStyle style) => MokaTheme.Red.WithComponentStyle(style);

	private sealed record ChoiceOption(string Value, string Text, bool Disabled, RenderFragment? Preview);

	private readonly record struct PreviewKey(string Palette, bool Dark, string? Accent, string? CustomAccent, string? CustomNeutral);

	private enum RowGroup
	{
		Style,
		Components,
		Charts
	}

	private sealed record ChoiceRow(MokaAppearanceRows Flag, string Label, string? Current, IReadOnlyList<ChoiceOption> Options, Func<string, Task> Pick)
	{
		public bool HasPictures => Options.Any(option => option.Preview is not null);

		public RowGroup Group => (Flag & ComponentRows) != 0 ? RowGroup.Components
			: (Flag & MokaAppearanceRows.ChartsGroup) != 0 ? RowGroup.Charts
			: RowGroup.Style;

		public bool IsSection => HasPictures && (Flag & SectionRows) != 0;
	}
}
