using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.ThemeGen.Appearance;

/// <summary>
///     A ready-made panel where the person using an app picks how it looks. It behaves like an input: <see cref="Value" />
///     and <see cref="ValueChanged" /> carry a <see cref="MokaAppearance" />, every pick applies at once, and storing it is
///     the app's job. <see cref="MokaAppearance.ToThemes" /> turns the value into a theme.
/// </summary>
public partial class MokaAppearancePanel
{
	private readonly Dictionary<(string Palette, bool Dark, string Accent), MokaTheme> _previews = [];
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

	private bool ShowsPalettes => Shows(MokaAppearanceRows.Palette) && Palettes.Count > 1;

	private bool ShowsAccents => Shows(MokaAppearanceRows.Accent) && Accents.Count > 0;

	private bool StyleIsFixed => (Rows & MokaAppearanceRows.Style) != 0 && !CurrentPalette.TakesStyleChoices;

	private List<string> AccentColors => [.. Accents.Select(accent => accent.For(IsDarkNow).Color)];

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
		}
	}

	private bool Shows(MokaAppearanceRows row) =>
		(Rows & row) != 0 && ((row & MokaAppearanceRows.Style) == 0 || CurrentPalette.TakesStyleChoices);

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

	private List<ChoiceRow> ChoiceRows()
	{
		MokaTheme theme = _value.ToTheme(IsDarkNow, Palettes, Accents);
		List<ChoiceRow> rows = [];

		AddRow(rows, MokaAppearanceRows.Density, Text.Density, CurrentChoice<MokaDensity>(theme, (t, v) => t.WithDensity(v)),
			[(MokaDensity.Comfortable, Text.DensityComfortable), (MokaDensity.Compact, Text.DensityCompact)],
			choice => _value with { Density = choice });

		AddRow(rows, MokaAppearanceRows.TextSize, Text.TextSize, CurrentChoice<MokaTextSize>(theme, (t, v) => t.WithTextSize(v)),
			[(MokaTextSize.Normal, Text.TextSizeNormal), (MokaTextSize.Small, Text.TextSizeSmall)],
			choice => _value with { TextSize = choice });

		AddRow(rows, MokaAppearanceRows.Corners, Text.Corners, CurrentChoice<MokaCorners>(theme, (t, v) => t.WithCorners(v)),
			[(MokaCorners.Soft, Text.CornersSoft), (MokaCorners.Medium, Text.CornersMedium), (MokaCorners.Tight, Text.CornersTight)],
			choice => _value with { Corners = choice });

		AddRow(rows, MokaAppearanceRows.MainButton, Text.MainButton, CurrentChoice<MokaMainButton>(theme, (t, v) => t.WithMainButton(v)),
			[(MokaMainButton.Solid, Text.MainButtonSolid), (MokaMainButton.Tinted, Text.MainButtonTinted), (MokaMainButton.Outline, Text.MainButtonOutline)],
			choice => _value with { MainButton = choice });

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
			choice => _value with { StatusBar = choice });

		if (Shows(MokaAppearanceRows.Descriptions))
		{
			rows.Add(new ChoiceRow(MokaAppearanceRows.Descriptions, Text.Descriptions, theme.Descriptions ? bool.TrueString : bool.FalseString,
				[new ChoiceOption(bool.TrueString, Text.DescriptionsShown), new ChoiceOption(bool.FalseString, Text.DescriptionsHidden)],
				choice => ApplyAsync(_value with { Descriptions = bool.Parse(choice) })));
		}

		return rows;
	}

	private void AddRow<TChoice>(List<ChoiceRow> rows, MokaAppearanceRows row, string label, TChoice? current, (TChoice Choice, string Text)[] options,
		Func<TChoice, MokaAppearance> withChoice)
		where TChoice : struct, Enum
	{
		if (Shows(row))
		{
			rows.Add(new ChoiceRow(row, label, current?.ToString(),
				[.. options.Select(option => new ChoiceOption(option.Choice.ToString(), option.Text))],
				choice => ApplyAsync(withChoice(Enum.Parse<TChoice>(choice)))));
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

	private sealed record ChoiceOption(string Value, string Text);

	private sealed record ChoiceRow(MokaAppearanceRows Flag, string Label, string? Current, IReadOnlyList<ChoiceOption> Options, Func<string, Task> Pick);
}
