using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.ColorPicker;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.ColorInput;

/// <summary>
///     A simple color input field with a color swatch preview and optional native color picker.
///     Lighter than <c>MokaColorPicker</c> - just a text input for hex values with a preview swatch.
///     <see cref="Look" /> draws it another way, most of them opening a <c>MokaColorPicker</c> panel in a popup.
/// </summary>
public partial class MokaColorInput
{
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";
	private const string PickerModule = "./_content/Moka.Red.Forms/ColorPicker/MokaColorPicker.razor.js";
	private const string DragModule = "./_content/Moka.Red.Core/moka-drag.js";
	private const int MaxRecentColors = 8;

	private static readonly string[] DefaultPresets =
		["#ef5350", "#ff7043", "#ffb547", "#66bb6a", "#26a69a", "#42a5f5", "#7e57c2", "#8d8d96"];

	private static readonly Dictionary<string, string> DefaultPresetNames = new(StringComparer.OrdinalIgnoreCase)
	{
		["#ef5350"] = "Red",
		["#ff7043"] = "Orange",
		["#ffb547"] = "Amber",
		["#66bb6a"] = "Green",
		["#26a69a"] = "Teal",
		["#42a5f5"] = "Blue",
		["#7e57c2"] = "Violet",
		["#8d8d96"] = "Gray"
	};

	private readonly string _generatedId = $"moka-colorinput-{Guid.NewGuid():N}";

	private ElementReference _anchor;
	private ElementReference _backdrop;
	private bool _eyeDropperChecked;
	private bool _eyeDropperSupported;
	private MokaColorFormat _format;
	private MokaColorFormat? _lastFormatParameter;
	private IReadOnlyList<string>? _lastRecentParameter;
	private bool _open;
	private ElementReference _popup;
	private bool _popupShown;
	private IReadOnlyList<string> _recents = [];
	private bool _recentTaken;
	private bool _returnFocus;
	private ElementReference _trigger;
	private bool _typedInvalid;
	private string? _valueAtOpen;

	// Id goes on the input, not a wrapper, so a label's for and getElementById reach the control.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	/// <summary>Label text displayed above the input.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the input.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the input when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Whether to show a color swatch preview next to the input. Defaults to true.</summary>
	[Parameter]
	public bool ShowPreview { get; set; } = true;

	/// <summary>Whether to show the browser's native color picker button. Defaults to true.</summary>
	[Parameter]
	public bool ShowNativeInput { get; set; } = true;

	/// <summary>
	///     How the field is drawn. <see cref="MokaColorInputLook.Standard" /> (the default) is the original text field.
	///     The others take only a hex color or a theme token such as <c>var(--moka-color-primary)</c>, and all but
	///     <see cref="MokaColorInputLook.Terminal" /> open a <c>MokaColorPicker</c> panel in a popup.
	/// </summary>
	[Parameter]
	public MokaColorInputLook Look { get; set; } = MokaColorInputLook.Standard;

	/// <summary>The layout of the panel the popup opens. Default <see cref="MokaColorPickerLayout.Compact" />.</summary>
	[Parameter]
	public MokaColorPickerLayout PickerLayout { get; set; } = MokaColorPickerLayout.Compact;

	/// <summary>
	///     The swatches <see cref="MokaColorInputLook.Palette" /> shows, as hex colors, and the palette of a panel that
	///     has one. Null (the default) is eight: red, orange, amber, green, teal, blue, violet and gray.
	/// </summary>
	[Parameter]
	public IReadOnlyList<string>? Presets { get; set; }

	/// <summary>Names a preset swatch for screen readers and its tooltip. Null names it by its value.</summary>
	[Parameter]
	public Func<string, string?>? ColorLabel { get; set; }

	/// <summary>The name of the popup. Default "Choose a color".</summary>
	[Parameter]
	public string PickerLabel { get; set; } = "Choose a color";

	/// <summary>What the Custom button of <see cref="MokaColorInputLook.Palette" /> says. Default "Custom".</summary>
	[Parameter]
	public string CustomText { get; set; } = "Custom";

	/// <summary>
	///     The error when typed text is no color, in every look but Standard, which takes any text. Default
	///     "Six hex digits".
	/// </summary>
	[Parameter]
	public string InvalidText { get; set; } = "Six hex digits";

	/// <summary>Passed to the popup's panel. See <see cref="MokaColorPicker.ShowContrast" />.</summary>
	[Parameter]
	public bool ShowContrast { get; set; }

	/// <summary>Passed to the popup's panel. See <see cref="MokaColorPicker.ContrastBackground" />.</summary>
	[Parameter]
	public string? ContrastBackground { get; set; }

	/// <summary>Passed to the popup's panel. See <see cref="MokaColorPicker.ShowFormats" />.</summary>
	[Parameter]
	public bool ShowFormats { get; set; }

	/// <summary>The format the popup's panel starts on. See <see cref="MokaColorPicker.Format" />.</summary>
	[Parameter]
	public MokaColorFormat Format { get; set; } = MokaColorFormat.Hex;

	/// <summary>Raised when the person picks another format in the popup.</summary>
	[Parameter]
	public EventCallback<MokaColorFormat> FormatChanged { get; set; }

	/// <summary>Passed to the popup's panel. See <see cref="MokaColorPicker.ShowThemeColors" />.</summary>
	[Parameter]
	public bool ShowThemeColors { get; set; }

	/// <summary>Passed to the popup's panel, which then writes 8 digit hex below full opacity.</summary>
	[Parameter]
	public bool ShowAlpha { get; set; }

	/// <summary>Passed to the popup's panel. See <see cref="MokaColorPicker.ShowAlphaPreview" />.</summary>
	[Parameter]
	public bool ShowAlphaPreview { get; set; }

	/// <summary>Passed to the popup's panel. See <see cref="MokaColorPicker.LabelledEyeDropper" />.</summary>
	[Parameter]
	public bool LabelledEyeDropper { get; set; }

	/// <summary>Passed to the popup's panel. See <see cref="MokaColorPicker.ClearableRecentColors" />.</summary>
	[Parameter]
	public bool ClearableRecentColors { get; set; }

	/// <summary>
	///     The recent colors, newest first. The field keeps them across openings of the popup; bind this with
	///     <see cref="RecentColorsChanged" /> to keep them longer.
	/// </summary>
	[Parameter]
	public IReadOnlyList<string>? RecentColors { get; set; }

	/// <summary>Raised with the new list when a color is added to the recent colors, moved or cleared.</summary>
	[Parameter]
	public EventCallback<IReadOnlyList<string>> RecentColorsChanged { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-color-input";

	// ErrorText is the explicit override; without one, fall back to whatever the cascaded
	// EditContext reports, so DataAnnotations messages are actually visible.
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError || _typedInvalid;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText
		: ValidationErrorText ?? (_typedInvalid ? InvalidText : null);

	private string ComputedCssClass { get; set; } = "";

	// The field wrapper is the outermost element, so the margin goes there. This component's own
	// element draws the field's border, so it keeps the padding and the radius.

	/// <inheritdoc />
	protected override string? ComponentStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle(Style)
		.Build();

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string InputCssClass { get; set; } = "";

	// The swatch shows the value only while it is a hex colour or a theme token: typed text such as
	// "red; background-image: url(...)" must not add declarations of its own.
	private string? SwatchStyle => new StyleBuilder()
		.AddStyle("background-color", MokaColorValues.IsShown(CurrentValueAsString) ? CurrentValueAsString : "transparent")
		.Build();

	private bool IsStandard => Look == MokaColorInputLook.Standard;

	private bool HasTextBox => Look is MokaColorInputLook.Split or MokaColorInputLook.Terminal;

	private bool OpensPicker => Look is not (MokaColorInputLook.Standard or MokaColorInputLook.Terminal);

	private string PopupKey => $"{_generatedId}-popup";

	private string PopupId => $"{InputId}-picker";

	private string TextId => $"{InputId}-text";

	private string TagId => $"{InputId}-tag";

	private string LabelId => MokaFieldWrapper.LabelIdFor(InputId);

	private string ExpandedText => _open ? "true" : "false";

	private IReadOnlyList<string> PalettePresets => Presets ?? DefaultPresets;

	private bool ValueIsPreset => PalettePresets.Any(color => string.Equals(color, CurrentValue, StringComparison.OrdinalIgnoreCase));

	private bool HasValue => MokaColorValues.IsShown(CurrentValue);

	// A token reads as its theme color's name; a hex in capitals, as mono hex reads best.
	private string DisplayText => MokaColorValues.ThemeLabel(CurrentValue)
	                              ?? (HasValue ? CurrentValue!.ToUpperInvariant() : Placeholder ?? "");

	private string LookCssClass => new CssBuilder(RootClass)
		.AddClass("moka-color-input--look")
		.AddClass($"moka-color-input--{MokaEnumHelpers.ToCssClass(Look)}")
		.AddClass("moka-color-input--error", HasError)
		.AddClass("moka-color-input--disabled", Disabled)
		.AddClass("moka-color-input--open", _open)
		.AddClass(CssClass)
		.AddClass(Class)
		.Build();

	private string LookTextCssClass => new CssBuilder("moka-color-input__text")
		.AddClass("moka-color-input__text--look")
		.AddClass("moka-color-input__text--terminal", Look == MokaColorInputLook.Terminal)
		.Build();

	// A chip has no field around it to carry the margin, so its own element takes all three.
	private string? ChipStyle => SpacingStyle().AddStyle(Style).Build();

	private string? TerminalSquareStyle => new StyleBuilder()
		.AddStyle("--_ci-color", MokaColorValues.ShownOrNull(CurrentValueAsString))
		.Build();

	private string HexCssClass => new CssBuilder("moka-color-input__hex")
		.AddClass("moka-color-input__hex--empty", !HasValue)
		.Build();

	private string CustomCssClass => new CssBuilder("moka-color-input__custom")
		.AddClass("moka-color-input__custom--on", HasValue && !ValueIsPreset)
		.Build();

	private string? CustomDotStyle => HasValue && !ValueIsPreset ? SwatchStyle : null;

	private string? LabelledBy => string.IsNullOrEmpty(Label) ? TextId : $"{LabelId} {TextId}";

	private string? PresetName(string color) =>
		ColorLabel?.Invoke(color) ?? (Presets is null && DefaultPresetNames.TryGetValue(color, out string? name) ? name : color);

	private string TerminalTag => _typedInvalid ? "INVALID" : MokaColorValues.IsThemeToken(CurrentValue) ? "VAR" : "HEX";

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (string.IsNullOrEmpty(Placeholder))
		{
			Placeholder = "#000000";
		}

		ComputedCssClass = new CssBuilder(RootClass)
			.AddClass("moka-color-input--error", HasError)
			.AddClass("moka-color-input--disabled", Disabled)
			.AddClass($"moka-color-input--{SizeToKebab(Size)}")
			.AddClass(CssClass)
			.AddClass(Class)
			.Build();
		InputCssClass = new CssBuilder("moka-color-input__text")
			.AddClass($"moka-color-input__text--{SizeToKebab(Size)}")
			.Build();

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
				_recents = [.. RecentColors.Take(MaxRecentColors)];
			}
		}
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
	{
		validationErrorMessage = string.Empty;
		if (IsStandard || string.IsNullOrWhiteSpace(value))
		{
			result = value ?? string.Empty;
			return true;
		}

		if (TryReadColor(value, out string color))
		{
			result = color;
			return true;
		}

		result = string.Empty;
		validationErrorMessage = InvalidText;
		return false;
	}

	/// <inheritdoc />
	protected override void OnDebouncedInput(string? value)
	{
		if (IsStandard)
		{
			base.OnDebouncedInput(value);
			return;
		}

		// A color takes effect once it's whole. Until then only text that can't become one shows as wrong.
		string text = value?.Trim() ?? string.Empty;
		if (IsWholeColor(text))
		{
			_typedInvalid = false;
			CurrentValueAsString = text;
			return;
		}

		_typedInvalid = text.Length > 0 && !CouldBecomeColor().IsMatch(text);
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (_popupShown != _open)
		{
			_popupShown = _open;
			if (_open)
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "open", PopupKey, _popup, _anchor, _backdrop,
					new Dictionary<string, object?> { ["placement"] = "bottom-start" });
				await SafeModuleInvokeVoidAsync(PickerModule, "focusFirst", _popup);
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", PopupKey);
				if (_returnFocus)
				{
					_returnFocus = false;
					await FocusAsync(_trigger);
				}
			}
		}

		if (Look == MokaColorInputLook.Split && !_eyeDropperChecked && !Disabled)
		{
			_eyeDropperChecked = true;
			_eyeDropperSupported = await EyeDropperSupportedAsync();
			if (_eyeDropperSupported)
			{
				StateHasChanged();
			}
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_popupShown)
		{
			_popupShown = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", PopupKey);
		}

		await base.DisposeAsyncCore();
	}

	private void HandleNativeInput(ChangeEventArgs e) => CurrentValueAsString = e.Value?.ToString();

	// Leaving the field, or Enter, takes the text as it is: a color, or the error.
	private void HandleLookChange(ChangeEventArgs e)
	{
		string text = e.Value?.ToString()?.Trim() ?? string.Empty;
		_typedInvalid = text.Length > 0 && !TryReadColor(text, out _);
		CurrentValueAsString = text;
	}

	private void TogglePopup()
	{
		if (Disabled || ReadOnly)
		{
			return;
		}

		if (_open)
		{
			_open = false;
			return;
		}

		_open = true;
		_valueAtOpen = CurrentValue;
	}

	private Task CloseFromBackdropAsync() => ClosePopupAsync(returnFocus: false);

	private async Task HandlePopupKeyAsync(KeyboardEventArgs e)
	{
		if (e.Key == "Escape")
		{
			await ClosePopupAsync(returnFocus: true);
		}
	}

	private async Task ClosePopupAsync(bool returnFocus)
	{
		if (!_open)
		{
			return;
		}

		_open = false;
		_returnFocus = returnFocus;
		if (!string.Equals(_valueAtOpen, CurrentValue, StringComparison.Ordinal))
		{
			await AddRecentAsync(CurrentValue);
		}
	}

	private void ChoosePreset(string? color)
	{
		_typedInvalid = false;
		CurrentValueAsString = color;
	}

	private void TakePicked(string? value)
	{
		_typedInvalid = false;
		CurrentValueAsString = value;
	}

	private async Task TakePickerRecentsAsync(IReadOnlyList<string> colors)
	{
		_recents = [.. colors];
		await RaiseRecentAsync();
	}

	private async Task TakePickerFormatAsync(MokaColorFormat format)
	{
		_format = format;
		if (FormatChanged.HasDelegate)
		{
			await FormatChanged.InvokeAsync(format);
		}
	}

	private async Task AddRecentAsync(string? color)
	{
		if (!MokaColorValues.IsShown(color) || (_recents.Count > 0 && _recents[0] == color))
		{
			return;
		}

		_recents = [color, .. _recents.Where(recent => recent != color).Take(MaxRecentColors - 1)];
		await RaiseRecentAsync();
	}

	private async Task RaiseRecentAsync()
	{
		_lastRecentParameter = _recents;
		if (RecentColorsChanged.HasDelegate)
		{
			await RecentColorsChanged.InvokeAsync(_recents);
		}
	}

	private async Task PickFromScreenAsync()
	{
		if (Disabled || ReadOnly)
		{
			return;
		}

		try
		{
			IJSObjectReference module = await GetJsModuleAsync(DragModule);
			string? picked = await module.InvokeAsync<string?>("pickColor");
			if (MokaColorSpace.TryParse(picked, out MokaRgba color))
			{
				TakePicked(MokaColorSpace.ToHex(color.Opaque));
			}
		}
		catch (JSException)
		{
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
	}

	private async Task<bool> EyeDropperSupportedAsync()
	{
		try
		{
			IJSObjectReference module = await GetJsModuleAsync(DragModule);
			return await module.InvokeAsync<bool>("isEyeDropperSupported");
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

	private static async Task FocusAsync(ElementReference element)
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

	// A theme token, or a hex color with or without its #.
	private static bool TryReadColor(string? text, out string color)
	{
		color = string.Empty;
		string value = text?.Trim() ?? string.Empty;
		if (MokaColorValues.IsThemeToken(value))
		{
			color = value;
			return true;
		}

		string hex = value.StartsWith('#') ? value : "#" + value;
		if (!MokaHexColor.IsValid(hex))
		{
			return false;
		}

		color = hex;
		return true;
	}

	private bool IsWholeColor(string text)
	{
		if (MokaColorValues.IsThemeToken(text))
		{
			return true;
		}

		int digits = text.StartsWith('#') ? text.Length - 1 : text.Length;
		return (digits == 6 || (ShowAlpha && digits == 8)) && TryReadColor(text, out _);
	}

	[GeneratedRegex(@"^(#?[0-9a-fA-F]{0,8}|v(a(r(\(.*)?)?)?)$", RegexOptions.CultureInvariant)]
	private static partial Regex CouldBecomeColor();
}
