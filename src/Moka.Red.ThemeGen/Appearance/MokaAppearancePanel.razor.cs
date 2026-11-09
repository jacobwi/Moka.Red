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
	private readonly Dictionary<(MokaAppearanceRows Row, string Option), MokaTheme> _choicePreviews = [];
	private readonly Dictionary<(string Palette, bool Dark, string Accent), MokaTheme> _previews = [];
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

	/// <summary>The app's own rows, after the built-in ones. <see cref="MokaAppearanceRow" /> matches them.</summary>
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

	private bool StyleIsFixed => (Rows & MokaAppearanceRows.Style) != 0 && !CurrentPalette.TakesStyleChoices;

	private List<string> AccentColors => [.. ShownAccents.Select(accent => accent.For(IsDarkNow).Color)];

	private string? SelectedAccentColor => SelectedAccent?.For(IsDarkNow).Color;

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
		(Rows & row) != 0 && ((row & MokaAppearanceRows.Style) == 0 || CurrentPalette.TakesStyleChoices);

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
		MokaTheme theme = dark ? palette.Dark : palette.Light;
		if (SelectedAccent is not { } accent)
		{
			return theme;
		}

		(string, bool, string) key = (palette.Id, dark, accent.Id);
		if (!_previews.TryGetValue(key, out MokaTheme? tinted))
		{
			tinted = theme.WithAccent(accent);
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

	private string? AccentName(string color) => AccentOf(color)?.Name;

	private Task ApplyAsync(MokaAppearance next)
	{
		_value = next;
		return ValueChanged.InvokeAsync(next);
	}

	private Task SetModeAsync(MokaColorMode mode) => ApplyAsync(_value with { Mode = mode });

	private Task SetPaletteAsync(string palette) => ApplyAsync(_value with { Palette = palette });

	private Task SetAccentColorAsync(string? color) => ApplyAsync(_value with { Accent = AccentOf(color)?.Id });

	private Task UsePaletteAccentAsync() => ApplyAsync(_value with { Accent = null });

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

		AddRow(rows, MokaAppearanceRows.Density, Text.Density, CurrentChoice<MokaDensity>(theme, (t, v) => t.WithDensity(v)),
			[(MokaDensity.Comfortable, Text.DensityComfortable), (MokaDensity.Compact, Text.DensityCompact)],
			choice => _value with { Density = choice },
			choice => DensityMock(ChoicePreview(theme, MokaAppearanceRows.Density, choice.ToString(), t => t.WithDensity(choice))));

		AddRow(rows, MokaAppearanceRows.TextSize, Text.TextSize, CurrentChoice<MokaTextSize>(theme, (t, v) => t.WithTextSize(v)),
			[(MokaTextSize.Normal, Text.TextSizeNormal), (MokaTextSize.Small, Text.TextSizeSmall)],
			choice => _value with { TextSize = choice });

		AddRow(rows, MokaAppearanceRows.Corners, Text.Corners, CurrentChoice<MokaCorners>(theme, (t, v) => t.WithCorners(v)),
			[(MokaCorners.Soft, Text.CornersSoft), (MokaCorners.Medium, Text.CornersMedium), (MokaCorners.Tight, Text.CornersTight)],
			choice => _value with { Corners = choice },
			choice => CornersMock(ChoicePreview(theme, MokaAppearanceRows.Corners, choice.ToString(), t => t.WithCorners(choice))));

		AddRow(rows, MokaAppearanceRows.MainButton, Text.MainButton, CurrentChoice<MokaMainButton>(theme, (t, v) => t.WithMainButton(v)),
			[(MokaMainButton.Solid, Text.MainButtonSolid), (MokaMainButton.Tinted, Text.MainButtonTinted), (MokaMainButton.Outline, Text.MainButtonOutline)],
			choice => _value with { MainButton = choice },
			choice => MainButtonMock(ChoicePreview(theme, MokaAppearanceRows.MainButton, choice.ToString(), t => t.WithMainButton(choice))));

		AddRow(rows, MokaAppearanceRows.LetterCase, Text.LetterCase, CurrentChoice<MokaLetterCase>(theme, (t, v) => t.WithLetterCase(v)),
			[(MokaLetterCase.Sentence, Text.LetterCaseSentence), (MokaLetterCase.Uppercase, Text.LetterCaseUppercase)],
			choice => _value with { LetterCase = choice });

		AddRow(rows, MokaAppearanceRows.TitleFont, Text.TitleFont, CurrentChoice<MokaTitleFont>(theme, (t, v) => t.WithTitleFont(v)),
			[(MokaTitleFont.SpaceGrotesk, Text.TitleFontSpaceGrotesk), (MokaTitleFont.Inter, Text.TitleFontInter), (MokaTitleFont.JetBrainsMono, Text.TitleFontJetBrainsMono)],
			choice => _value with { TitleFont = choice });

		AddRow(rows, MokaAppearanceRows.Selection, Text.Selection, CurrentChoice<MokaSelection>(theme, (t, v) => t.WithSelection(v)),
			[(MokaSelection.Ring, Text.SelectionRing), (MokaSelection.Flat, Text.SelectionFlat), (MokaSelection.Glow, Text.SelectionGlow)],
			choice => _value with { Selection = choice });

		AddRow(rows, MokaAppearanceRows.CurrentPlace, Text.CurrentPlace, CurrentChoice<MokaCurrentPlace>(theme, (t, v) => t.WithCurrentPlace(v)),
			[(MokaCurrentPlace.Ring, Text.CurrentPlaceRing), (MokaCurrentPlace.Solid, Text.CurrentPlaceSolid)],
			choice => _value with { CurrentPlace = choice });

		AddRow(rows, MokaAppearanceRows.StatusBar, Text.StatusBar, CurrentChoice<MokaStatusBarLook>(theme, (t, v) => t.WithStatusBar(v)),
			[
				(MokaStatusBarLook.Standard, Text.StatusBarStandard), (MokaStatusBarLook.Accent, Text.StatusBarAccent),
				(MokaStatusBarLook.Hairline, Text.StatusBarHairline), (MokaStatusBarLook.Floating, Text.StatusBarFloating),
				(MokaStatusBarLook.Chips, Text.StatusBarChips), (MokaStatusBarLook.Segments, Text.StatusBarSegments),
				(MokaStatusBarLook.Terminal, Text.StatusBarTerminal)
			],
			choice => _value with { StatusBar = choice },
			StatusBarMock);

		AddRow(rows, MokaAppearanceRows.Descriptions, Text.Descriptions, theme.Descriptions.ToString(),
			[(true, Text.DescriptionsShown, null), (false, Text.DescriptionsHidden, null)],
			choice => ApplyAsync(_value with { Descriptions = bool.Parse(choice) }));

		return rows;
	}

	private void AddRow<TChoice>(List<ChoiceRow> rows, MokaAppearanceRows row, string label, TChoice? current, (TChoice Choice, string Text)[] options,
		Func<TChoice, MokaAppearance> withChoice, Func<TChoice, RenderFragment>? preview = null)
		where TChoice : struct, Enum =>
		AddRow(rows, row, label, current?.ToString(),
			[.. options.Select(option => ((object)option.Choice, option.Text, Compact || preview is null ? null : preview(option.Choice)))],
			choice => ApplyAsync(withChoice(Enum.Parse<TChoice>(choice))));

	private void AddRow(List<ChoiceRow> rows, MokaAppearanceRows row, string label, string? current, (object Choice, string Text, RenderFragment? Preview)[] options, Func<string, Task> pick)
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
			rows.Add(new ChoiceRow(row, label, current, shown, pick));
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

	private static string? MiniStyle(ChoiceRow row) => new StyleBuilder()
		.AddStyle("--_appearance-options", row.Options.Count.ToString(CultureInfo.InvariantCulture))
		.Build();

	private sealed record ChoiceOption(string Value, string Text, bool Disabled, RenderFragment? Preview);

	private sealed record ChoiceRow(MokaAppearanceRows Flag, string Label, string? Current, IReadOnlyList<ChoiceOption> Options, Func<string, Task> Pick)
	{
		public bool HasPictures => Options.Any(option => option.Preview is not null);
	}
}
