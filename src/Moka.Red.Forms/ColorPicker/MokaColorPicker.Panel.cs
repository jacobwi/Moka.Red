using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.ColorPicker;

// The panel layouts other than Standard, and the extras every panel can show. Standard keeps its own state and
// its round trip per pointer move (MokaColorPicker.razor.cs); these layouts drag in the browser and report on
// release (blazor/performance/rendering.md, "Don't trigger events too rapidly").
public partial class MokaColorPicker
{
	private const string PanelModule = "./_content/Moka.Red.Forms/ColorPicker/MokaColorPicker.razor.js";
	private const string DragModule = "./_content/Moka.Red.Core/moka-drag.js";
	private const string EyeHint = "Press it, then click anywhere on your screen. Esc cancels.";

	private static readonly TimeSpan CopiedFor = TimeSpan.FromSeconds(1.5);

	// Two rows of the palette plus a row of lighter tints, eight across.
	private static readonly string[] LayoutPresets =
	[
		"#ef5350", "#ff7043", "#ffa726", "#ffca28", "#66bb6a", "#26a69a", "#42a5f5", "#7e57c2",
		"#c62828", "#d84315", "#ef6c00", "#f9a825", "#2e7d32", "#00695c", "#1565c0", "#4527a0",
		"#ef9a9a", "#ffab91", "#ffcc80", "#ffe082", "#a5d6a7", "#80cbc4", "#90caf9", "#b39ddb"
	];

	// What the Swatches tab offers from the theme when ShowThemeColors is off: the color as it is now, as a hex.
	private static readonly MokaThemeColorChoice[] ThemeHexChoices =
	[
		new("Primary", "primary"),
		new("Success", "success"),
		new("Warning", "warning"),
		new("Info", "info"),
		new("Muted", "on-surface-variant"),
		new("Text", "on-surface")
	];

	private static readonly (string Label, double Needs)[] ContrastNeeds =
	[
		("Body text, AA", 4.5),
		("Large text and icons", 3),
		("Body text, AAA", 7)
	];

	private readonly EventCallback<string?> _choosePanelColor;
	private readonly EventCallback<string?> _chooseRecentColor;
	private string? _boundPanelId;
	private List<string>? _clearedRecent;
	private CancellationTokenSource? _copiedReset;
	private string? _eyeStatus = EyeHint;
	private PanelFocus _focusNext;
	private MokaColorFormat _format;
	private string? _formatStatus;
	private bool _hexCopied;
	private bool _hexInvalid;
	private MokaColorFormat? _lastFormatParameter;
	private string? _lastPicked;
	private IReadOnlyList<string>? _lastRecentParameter;
	private ElementReference _layoutRef;
	private bool _moreOpen = true;
	private double _pickAlpha = 1;
	private double _pickHue;
	private double _pickSat;
	private double _pickVal;
	private bool _recentTaken;
	private ElementReference _recentRow;
	private int _tab;
	private ElementReference _undoButton;

	/// <summary>Creates the picker.</summary>
	public MokaColorPicker()
	{
		_choosePanelColor = EventCallback.Factory.Create<string?>(this, ChoosePanelColor);
		_chooseRecentColor = EventCallback.Factory.Create<string?>(this, ChooseRecentColorAsync);
	}

	private enum PanelFocus
	{
		None,
		Undo,
		FirstRecent
	}

	[CascadingParameter]
	private MokaTheme? CascadedTheme { get; set; }

	/// <summary>
	///     How the panel is laid out. <see cref="MokaColorPickerLayout.Standard" /> (the default) is the original panel.
	///     The others drag in the browser and report the color when the pointer is let go, and every slider in them
	///     takes the arrow keys, with Shift for bigger steps, and Home and End.
	/// </summary>
	[Parameter]
	public MokaColorPickerLayout Layout { get; set; } = MokaColorPickerLayout.Standard;

	/// <summary>
	///     Draws the panel in place, always open, with no field, swatch button or popup. For a panel in a card or
	///     another component's popup. Default false.
	/// </summary>
	[Parameter]
	public bool Inline { get; set; }

	/// <summary>
	///     Shows the chosen color as text on a background, with the contrast ratio and the WCAG level it meets. The
	///     background is <see cref="ContrastBackground" />, else the theme's page background, so the numbers follow a
	///     switch between light and dark. Default false.
	/// </summary>
	[Parameter]
	public bool ShowContrast { get; set; }

	/// <summary>
	///     The background <see cref="ShowContrast" /> measures against: a hex or <c>rgb()</c> color, or a theme token
	///     such as <c>var(--moka-color-surface)</c>. Null (the default) is the theme's page background.
	/// </summary>
	[Parameter]
	public string? ContrastBackground { get; set; }

	/// <summary>
	///     Shows a HEX, RGB, HSL and OKLCH switch with the value in the chosen format, and a copy button that copies
	///     it that way. Default false.
	/// </summary>
	[Parameter]
	public bool ShowFormats { get; set; }

	/// <summary>The format <see cref="ShowFormats" /> starts on. Default <see cref="MokaColorFormat.Hex" />.</summary>
	[Parameter]
	public MokaColorFormat Format { get; set; } = MokaColorFormat.Hex;

	/// <summary>Raised when the person picks another format, so the app can keep it.</summary>
	[Parameter]
	public EventCallback<MokaColorFormat> FormatChanged { get; set; }

	/// <summary>
	///     With <see cref="MokaColorPicker.ShowAlpha" />: draws the alpha bar over a checkerboard made from the theme's
	///     colors on the new design, and adds a preview that is half solid and half see-through with the 8 digit hex
	///     and the opacity as a number. Default false.
	/// </summary>
	[Parameter]
	public bool ShowAlphaPreview { get; set; }

	/// <summary>
	///     Draws the eyedropper as a labelled "Pick from screen" button, with I as its key while focus is in the
	///     panel. The button hides where the browser has no EyeDropper. Default false.
	/// </summary>
	[Parameter]
	public bool LabelledEyeDropper { get; set; }

	/// <summary>
	///     Draws the recent colors with a Clear button (and Undo after it), and moves a recent color to the front as
	///     soon as it is chosen. Shows in every layout. Default false.
	/// </summary>
	[Parameter]
	public bool ClearableRecentColors { get; set; }

	/// <summary>
	///     The recent colors, newest first, at most eight. Null (the default) keeps them inside the picker for as long
	///     as it lives. Bind it with <see cref="RecentColorsChanged" /> to keep them across pages and visits.
	/// </summary>
	[Parameter]
	public IReadOnlyList<string>? RecentColors { get; set; }

	/// <summary>Raised with the new list whenever a color is added, moved to the front or cleared.</summary>
	[Parameter]
	public EventCallback<IReadOnlyList<string>> RecentColorsChanged { get; set; }

	/// <summary>
	///     Shows named swatches for the theme's colors that set the value to the token, such as
	///     <c>var(--moka-color-primary)</c>, in place of a fixed hex, so the color follows the theme. Default false.
	/// </summary>
	[Parameter]
	public bool ShowThemeColors { get; set; }

	private bool IsNewLayout => Layout != MokaColorPickerLayout.Standard;

	private string PanelKey => $"{_generatedId}-panel";

	private MokaTheme? ResolvingTheme => CascadedTheme ?? Theme;

	private IReadOnlyList<string> PanelPresets => Presets ?? LayoutPresets;

	private bool ValueIsToken => MokaColorValues.IsThemeToken(CurrentValue);

	private string DropdownCssClass => new CssBuilder("moka-colorpicker-dropdown")
		.AddClass("moka-colorpicker-dropdown--panel", IsNewLayout)
		.Build();

	private string PanelCssClass => new CssBuilder("moka-colorpicker-panel")
		.AddClass($"moka-colorpicker-panel--{MokaEnumHelpers.ToCssClass(Layout)}")
		.AddClass("moka-colorpicker-panel--wide", ShowContrast || ShowFormats || ShowThemeColors)
		.AddClass("moka-colorpicker-panel--inline", Inline)
		.AddClass("moka-colorpicker-panel--disabled", Disabled)
		.Build();

	private string AlphaSliderCssClass => new CssBuilder("moka-colorpicker-alpha-slider")
		.AddClass("moka-colorpicker-alpha-slider--checker", ShowAlphaPreview)
		.Build();

	private string AlphaTrackCssClass => new CssBuilder("moka-colorpicker-track moka-colorpicker-track--alpha")
		.AddClass("moka-colorpicker-track--checker", ShowAlphaPreview)
		.Build();

	private MokaRgba PickedColor => MokaColorSpace.FromHsv(_pickHue, _pickSat, _pickVal, ShowAlpha ? _pickAlpha : 1);

	// A token stays a token in the previews, so they follow the theme; anything else is the panel's own color.
	private string ShownColor => ValueIsToken ? CurrentValue! : MokaColorSpace.ToHex(PickedColor);

	private string ShownColorWithAlpha => ValueIsToken ? CurrentValue! : MokaColorSpace.ToHex(PickedColor, withAlpha: true);

	private string? PanelStyle
	{
		get
		{
			(_, double saturation, double lightness) = MokaColorSpace.HsvToHsl(_pickHue, _pickSat, _pickVal);
			return new StyleBuilder()
				.AddStyle("--_cp-hue", Number(_pickHue))
				.AddStyle("--_cp-sat", Number(_pickSat))
				.AddStyle("--_cp-val", Number(_pickVal))
				.AddStyle("--_cp-alpha", Number(ShowAlpha ? _pickAlpha : 1))
				.AddStyle("--_cp-color", ShownColor)
				.AddStyle("--_cp-color-a", ShownColorWithAlpha)
				.AddStyle("--_cp-sl", Number(saturation * 100) + "%")
				.AddStyle("--_cp-ll", Number(lightness * 100) + "%")
				.Build();
		}
	}

	private (int Hue, int Saturation, int Lightness) Hsl
	{
		get
		{
			(double h, double s, double l) = MokaColorSpace.HsvToHsl(_pickHue, _pickSat, _pickVal);
			return (Whole(h), Whole(s * 100), Whole(l * 100));
		}
	}

	private string HsvReadout => string.Create(CultureInfo.InvariantCulture,
		$"H {Whole(_pickHue)}°  S {Whole(_pickSat * 100)}%  B {Whole(_pickVal * 100)}%");

	private string HslText
	{
		get
		{
			(int h, int s, int l) = Hsl;
			return string.Create(CultureInfo.InvariantCulture, $"hsl({h % 360} {s}% {l}%)");
		}
	}

	private string ValueReadout => ValueIsToken
		? MokaColorValues.ThemeLabel(CurrentValue) ?? CurrentValue!
		: MokaColorSpace.Format(PickedColor.Opaque, MokaColorFormat.Hex);

	private string AlphaHexText => MokaColorSpace.Format(ResolvedValue() ?? PickedColor, MokaColorFormat.Hex);

	private string AlphaRgbText => MokaColorSpace.Format(ResolvedValue() ?? PickedColor, MokaColorFormat.Rgb);

	private string FormatText => ResolvedValue() is { } color ? MokaColorSpace.Format(color, _format) : "";

	// The color the original panel's extras preview, from the value, since its drags don't move the HSV state.
	private string? ValueColorStyle
	{
		get
		{
			string? shown = HexOrNull(CurrentValue);
			string? solid = ValueIsToken || ResolvedValue() is not { } color ? shown : MokaColorSpace.ToHex(color.Opaque);
			return new StyleBuilder().AddStyle("--_cp-color", solid).AddStyle("--_cp-color-a", shown).Build();
		}
	}

	// Read from the value, since the original panel's own drags don't move the HSV state the new layouts keep.
	private string? AlphaPreviewStyle
	{
		get
		{
			if (ValueIsToken)
			{
				return new StyleBuilder().AddStyle("--_cp-solid", CurrentValue).AddStyle("--_cp-see", CurrentValue).Build();
			}

			MokaRgba color = ResolvedValue() ?? PickedColor;
			return new StyleBuilder()
				.AddStyle("--_cp-solid", MokaColorSpace.ToHex(color.Opaque))
				.AddStyle("--_cp-see", MokaColorSpace.ToHex(color, withAlpha: true))
				.Build();
		}
	}

	private ContrastCheck? Contrast
	{
		get
		{
			MokaTheme? theme = ResolvingTheme;
			string background = ContrastBackground ?? "var(--moka-color-background)";
			if (!MokaColorValues.TryResolve(CurrentValue, theme, out MokaRgba text) ||
			    !MokaColorValues.TryResolve(background, theme, out MokaRgba under))
			{
				return null;
			}

			MokaRgba solid = under.Opaque;
			double ratio = MokaColorSpace.ContrastRatio(MokaColorSpace.Over(text, solid), solid);
			string sampleBackground = MokaColorValues.IsShown(background) ? background : MokaColorSpace.ToHex(solid);
			return new ContrastCheck(ratio, MokaColorSpace.LevelOf(ratio), MokaColorSpace.Format(solid, MokaColorFormat.Hex),
				sampleBackground);
		}
	}

	private string? ContrastSampleStyle(ContrastCheck check) => new StyleBuilder()
		.AddStyle("background", check.Background)
		.AddStyle("color", MokaColorValues.ShownOrNull(CurrentValue))
		.Build();

	private static string LevelText(MokaContrastLevel level) => level switch
	{
		MokaContrastLevel.Aaa => "AAA",
		MokaContrastLevel.Aa => "AA",
		MokaContrastLevel.AaLarge => "Large text only",
		_ => "Fails"
	};

	private static string LevelCssClass(MokaContrastLevel level) => new CssBuilder("moka-colorpicker-grade")
		.AddClass("moka-colorpicker-grade--ok", level >= MokaContrastLevel.Aa)
		.AddClass("moka-colorpicker-grade--warn", level == MokaContrastLevel.AaLarge)
		.AddClass("moka-colorpicker-grade--fail", level == MokaContrastLevel.Fails)
		.Build();

	private static string NeedCssClass(bool passes) => new CssBuilder("moka-colorpicker-need")
		.AddClass("moka-colorpicker-need--pass", passes)
		.AddClass("moka-colorpicker-need--fail", !passes)
		.Build();

	private string TabId(int tab) => $"{_generatedId}-tab-{tab}";

	private string TabPanelId(int tab) => $"{_generatedId}-tabpanel-{tab}";

	private string RangeId(char channel) => $"{_generatedId}-range-{channel}";

	private string MoreId => $"{_generatedId}-more";

	private string RecentLabelId => $"{_generatedId}-recent";

	private string ThemeLabelId => $"{_generatedId}-theme";

	private string ThemeHexLabelId => $"{_generatedId}-theme-hex";

	private string PaletteLabelId => $"{_generatedId}-palette";

	private bool Pressed(string color) => string.Equals(CurrentValue, color, StringComparison.OrdinalIgnoreCase);

	private string PressedText(string color) => Pressed(color) ? "true" : "false";

	private static string SwatchesCssClass(bool grid) => new CssBuilder("moka-colorpicker-swatches")
		.AddClass("moka-colorpicker-swatches--grid", grid)
		.Build();

	// The theme's colors as they are now, as hexes named "Primary #6E92FF".
	private List<(string Hex, string Name)> ThemeHexSwatches()
	{
		List<(string Hex, string Name)> swatches = [];
		foreach (MokaThemeColorChoice choice in ThemeHexChoices)
		{
			if (MokaColorValues.TryResolve(choice.Token, ResolvingTheme, out MokaRgba resolved))
			{
				string hex = MokaColorSpace.ToHex(resolved.Opaque);
				swatches.Add((hex, $"{choice.Label} {hex.ToUpperInvariant()}"));
			}
		}

		return swatches;
	}

	private void ChoosePanelColor(string? color)
	{
		if (color is not null)
		{
			SelectPreset(color);
		}
	}

	private Task ChooseRecentColorAsync(string? color) => color is null ? Task.CompletedTask : ChooseRecentAsync(color);

	private string TokenCssClass(MokaThemeColorChoice choice) => new CssBuilder("moka-colorpicker-token")
		.AddClass("moka-colorpicker-token--selected", Pressed(choice.Token))
		.Build();

	private static string? TokenSwatchStyle(MokaThemeColorChoice choice) => new StyleBuilder()
		.AddStyle("background", choice.Token)
		.Build();

	private string TabCssClass(int tab) => new CssBuilder("moka-colorpicker-tab")
		.AddClass("moka-colorpicker-tab--selected", _tab == tab)
		.Build();

	private string HexFieldCssClass => new CssBuilder("moka-colorpicker-field")
		.AddClass("moka-colorpicker-field--invalid", _hexInvalid)
		.Build();

	private string CopyIconCssClass => new CssBuilder("moka-colorpicker-icon-btn")
		.AddClass("moka-colorpicker-icon-btn--done", _hexCopied)
		.Build();

	private static string SquareCssClass(bool inWheel) => new CssBuilder("moka-colorpicker-square")
		.AddClass("moka-colorpicker-square--wheel", inWheel)
		.Build();

	private static string RangeCssClass(char channel) => new CssBuilder("moka-colorpicker-range")
		.AddClass($"moka-colorpicker-range--{channel}")
		.Build();

	private static string NeedText(double needs) => string.Create(CultureInfo.InvariantCulture, $"{needs:0.##}:1");

	private static string Number(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

	private static int Whole(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

	// The value as a color C# can measure: a hex as written, a token as the theme defines it.
	private MokaRgba? ResolvedValue() =>
		MokaColorValues.TryResolve(CurrentValue, ResolvingTheme, out MokaRgba color) ? color : null;

	// What the original panel's own math reads for a value: a token as the hex the theme gives it.
	private string StateSource(string value) =>
		MokaColorValues.IsThemeToken(value) && MokaColorValues.TryResolve(value, ResolvingTheme, out MokaRgba color)
			? MokaColorSpace.ToHex(color, withAlpha: true)
			: value;

	private void TakeNewParameters()
	{
		if (_lastFormatParameter != Format)
		{
			_lastFormatParameter = Format;
			_format = Format;
		}

		if (!_recentTaken || !ReferenceEquals(RecentColors, _lastRecentParameter))
		{
			_recentTaken = true;
			_lastRecentParameter = RecentColors;
			if (RecentColors is not null)
			{
				_recentColors = [.. RecentColors.Where(color => !string.IsNullOrWhiteSpace(color)).Take(MaxRecentColors)];
			}
		}

		SyncPanelFromValue(CurrentValue, force: false);
	}

	// A value the panel itself just produced keeps the panel's exact position, so a grey keeps the hue it was
	// dragged from instead of jumping to red when the parent passes it back.
	private void SyncPanelFromValue(string? value, bool force)
	{
		if (!force && _lastPicked is not null && string.Equals(value, _lastPicked, StringComparison.Ordinal))
		{
			return;
		}

		_lastPicked = null;
		if (!MokaColorValues.TryResolve(value, ResolvingTheme, out MokaRgba color))
		{
			return;
		}

		(double hue, double saturation, double brightness) = MokaColorSpace.ToHsv(color);
		if (saturation > 0 && brightness > 0)
		{
			_pickHue = hue;
		}

		_pickSat = saturation;
		_pickVal = brightness;
		_pickAlpha = color.A;
	}

	private void TakePanelColor()
	{
		string value = MokaColorSpace.ToHex(PickedColor, withAlpha: ShowAlpha);
		_lastPicked = value;
		_hexInvalid = false;
		CurrentValue = value;
		HexToHsl(value, out _hue, out _saturation, out _lightness, out _alpha);
		UpdateRgbFromHsl();
	}

	/// <summary>Called by the browser when a drag lets go, or a key moves a slider, in a layout other than Standard.</summary>
	/// <param name="hue">Hue, 0 to 360.</param>
	/// <param name="saturation">Saturation, 0 to 1.</param>
	/// <param name="value">Brightness, 0 to 1.</param>
	/// <param name="alpha">Alpha, 0 to 1.</param>
	[JSInvokable]
	public void OnPanelPicked(double hue, double saturation, double value, double alpha)
	{
		if (Disabled || ReadOnly)
		{
			return;
		}

		_pickHue = Math.Clamp(Finite(hue), 0, 360);
		_pickSat = Math.Clamp(Finite(saturation), 0, 1);
		_pickVal = Math.Clamp(Finite(value), 0, 1);
		_pickAlpha = ShowAlpha ? Math.Clamp(Finite(alpha, 1), 0, 1) : 1;
		TakePanelColor();
		StateHasChanged();
	}

	/// <summary>Called by the browser with the color the eyedropper picked, or null when it was cancelled.</summary>
	/// <param name="color">The picked color, as the browser writes it.</param>
	[JSInvokable]
	public void OnEyeDropped(string? color)
	{
		if (MokaColorSpace.TryParse(color, out MokaRgba picked) && !Disabled && !ReadOnly)
		{
			string hex = MokaColorSpace.ToHex(picked.Opaque);
			SelectPreset(hex);
			_eyeStatus = $"Picked {hex.ToUpperInvariant()} from the screen.";
		}
		else
		{
			_eyeStatus = "Cancelled, nothing changed.";
		}

		StateHasChanged();
	}

	/// <summary>Called by the browser when focus leaves an inline panel, which counts as the color being chosen.</summary>
	[JSInvokable]
	public void OnPanelLeft() => RunDetached(() => CommitRecentAsync(CurrentValue));

	private static double Finite(double value, double fallback = 0) => double.IsFinite(value) ? value : fallback;

	private void HandlePanelHexInput(ChangeEventArgs e)
	{
		string text = e.Value?.ToString()?.Trim() ?? "";
		if (MokaColorValues.IsThemeToken(text))
		{
			_hexInvalid = false;
			SelectPreset(text);
			return;
		}

		string hex = text.StartsWith('#') ? text : "#" + text;
		if (!MokaHexColor.IsValid(hex) || !MokaColorSpace.TryParse(hex, out MokaRgba color))
		{
			_hexInvalid = text.Length > 0;
			return;
		}

		_hexInvalid = false;
		SelectPreset(MokaColorSpace.ToHex(color, withAlpha: true));
	}

	private void HandleHslInput(char channel, ChangeEventArgs e)
	{
		if (Disabled || ReadOnly ||
		    !double.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double number) ||
		    !double.IsFinite(number))
		{
			return;
		}

		(double h, double s, double l) = MokaColorSpace.HsvToHsl(_pickHue, _pickSat, _pickVal);
		switch (channel)
		{
			case 'h':
				h = Math.Clamp(number, 0, 360);
				break;
			case 's':
				s = Math.Clamp(number, 0, 100) / 100;
				break;
			default:
				l = Math.Clamp(number, 0, 100) / 100;
				break;
		}

		(double hue, double saturation, double brightness) = MokaColorSpace.HslToHsv(h, s, l);
		_pickHue = hue;
		_pickSat = saturation;
		_pickVal = brightness;
		TakePanelColor();
	}

	private void HandleAlphaPercent(ChangeEventArgs e)
	{
		if (Disabled || ReadOnly ||
		    !double.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double percent) ||
		    !double.IsFinite(percent))
		{
			return;
		}

		_pickAlpha = Math.Clamp(percent, 0, 100) / 100;
		TakePanelColor();
	}

	private void SelectTab(int tab) => _tab = tab;

	private void ToggleMore() => _moreOpen = !_moreOpen;

	private async Task ChooseRecentAsync(string color)
	{
		SelectPreset(color);
		if (ClearableRecentColors)
		{
			await CommitRecentAsync(color);
		}
	}

	private async Task ClearRecentAsync()
	{
		_clearedRecent = [.. _recentColors];
		_recentColors = [];
		_focusNext = PanelFocus.Undo;
		await RaiseRecentAsync();
	}

	private async Task UndoClearAsync()
	{
		_recentColors = _clearedRecent ?? [];
		_clearedRecent = null;
		_focusNext = PanelFocus.FirstRecent;
		await RaiseRecentAsync();
	}

	private async Task CommitRecentAsync(string? color)
	{
		if (AddToRecent(color))
		{
			_clearedRecent = null;
			await RaiseRecentAsync();
		}
	}

	private async Task RaiseRecentAsync()
	{
		IReadOnlyList<string> copy = [.. _recentColors];
		_lastRecentParameter = copy;
		if (RecentColorsChanged.HasDelegate)
		{
			await RecentColorsChanged.InvokeAsync(copy);
		}
	}

	private async Task SelectFormatAsync(string? name)
	{
		if (!Enum.TryParse(name, out MokaColorFormat format) || format == _format)
		{
			return;
		}

		_format = format;
		_formatStatus = null;
		if (FormatChanged.HasDelegate)
		{
			await FormatChanged.InvokeAsync(format);
		}
	}

	private async Task CopyPanelHexAsync()
	{
		if (string.IsNullOrEmpty(CurrentValue) || !await CopyTextAsync(CurrentValue))
		{
			return;
		}

		_hexCopied = true;
		ScheduleCopiedReset();
	}

	private async Task CopyFormatAsync()
	{
		string text = FormatText;
		if (text.Length == 0)
		{
			return;
		}

		_formatStatus = await CopyTextAsync(text) ? $"Copied {text}" : "Press Ctrl+C to copy it.";
		ScheduleCopiedReset();
	}

	private async Task<bool> CopyTextAsync(string text)
	{
		try
		{
			_jsModule ??= await GetJsModuleAsync(DragModule);
			return await _jsModule.InvokeAsync<bool>("copyToClipboard", text);
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
			return false;
		}
		catch (JSException)
		{
			return false;
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
			return false;
		}
	}

	// Only the latest copy's timer puts the copy icon back.
	private void ScheduleCopiedReset()
	{
		_copiedReset?.Cancel();
		_copiedReset?.Dispose();
		_copiedReset = new CancellationTokenSource();
		CancellationToken token = _copiedReset.Token;
		RunDetached(async () =>
		{
			try
			{
				await Task.Delay(CopiedFor, token);
			}
			catch (TaskCanceledException)
			{
				return;
			}

			_hexCopied = false;
			StateHasChanged();
		});
	}

	// The element the panel script listens on: the new layouts' panel, or the original panel when it has a
	// labelled eyedropper or sits inline. Nothing while closed, disabled or read-only.
	private ElementReference? PanelScriptTarget
	{
		get
		{
			if (Disabled || ReadOnly || !(Inline || _isOpen))
			{
				return null;
			}

			if (IsNewLayout)
			{
				return _layoutRef;
			}

			return LabelledEyeDropper || Inline ? _panel : null;
		}
	}

	private async Task SyncPanelScriptAsync()
	{
		ElementReference? target = PanelScriptTarget;
		string? id = target?.Id;
		if (string.Equals(id, _boundPanelId, StringComparison.Ordinal))
		{
			return;
		}

		await UnbindPanelScriptAsync();
		if (target is not { } element || string.IsNullOrEmpty(id))
		{
			return;
		}

		_dotNetRef ??= DotNetObjectReference.Create(this);
		await SafeModuleInvokeVoidAsync(PanelModule, "bind", PanelKey, element, _dotNetRef,
			new Dictionary<string, object?> { ["reportLeave"] = Inline });
		_boundPanelId = id;
	}

	private async Task UnbindPanelScriptAsync()
	{
		if (_boundPanelId is null)
		{
			return;
		}

		_boundPanelId = null;
		await SafeModuleInvokeVoidAsync(PanelModule, "unbind", PanelKey);
	}

	private async Task FocusAfterRenderAsync()
	{
		PanelFocus next = _focusNext;
		_focusNext = PanelFocus.None;
		switch (next)
		{
			case PanelFocus.Undo:
				await FocusElementAsync(_undoButton);
				break;
			case PanelFocus.FirstRecent:
				await SafeModuleInvokeVoidAsync(PanelModule, "focusFirst", _recentRow);
				break;
		}
	}

	private static async Task FocusElementAsync(ElementReference element)
	{
		try
		{
			await element.FocusAsync();
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
		catch (JSException)
		{
		}
		catch (InvalidOperationException)
		{
			// Not interactive, or the element is gone.
		}
	}

	private sealed record ContrastCheck(double Ratio, MokaContrastLevel Level, string BackgroundHex, string Background)
	{
		public string RatioText => MokaColorSpace.RatioText(Ratio);
	}
}
