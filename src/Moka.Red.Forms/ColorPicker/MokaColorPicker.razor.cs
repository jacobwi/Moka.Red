using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.ColorPicker;

/// <summary>
///     A color input component with a visual picker, hue slider, preset swatches,
///     RGB inputs, eyedropper support, copy-to-clipboard, and recent color history.
///     Uses pointer-based drag interaction via JS interop for smooth gradient/slider updates.
/// </summary>
public partial class MokaColorPicker
{
	private const int MaxRecentColors = 8;

	private static readonly string[] DefaultPresets =
	[
		"#d32f2f", "#c2185b", "#7b1fa2", "#512da8", "#303f9f",
		"#1976d2", "#0288d1", "#00796b", "#388e3c", "#689f38",
		"#fbc02d", "#f57c00", "#e64a19", "#5d4037", "#455a64",
		"#000000", "#424242", "#757575", "#bdbdbd", "#ffffff"
	];

	private readonly string _inputId = $"moka-colorpicker-{Guid.NewGuid():N}";
	private readonly List<string> _recentColors = [];
	private double _alpha = 1;
	private bool _alphaAttached;
	private ElementReference _alphaSliderRef;
	private int _blue;
	private DotNetObjectReference<MokaColorPicker>? _dotNetRef;
	private bool _eyeDropperChecked;

	private bool _eyeDropperSupported;
	private ElementReference _gradientAreaRef;
	private bool _gradientAttached;
	private int _green;
	private double _hue;
	private bool _hueAttached;
	private ElementReference _hueSliderRef;
	private bool _isOpen;

	private IJSObjectReference? _jsModule;
	private double _lightness = 50;
	private int _red;
	private double _saturation = 100;

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

	/// <summary>Placeholder text displayed when no color is selected.</summary>
	[Parameter]
	public string Placeholder { get; set; } = "#000000";

	/// <summary>Preset color swatches. Default includes common material colors.</summary>
	[Parameter]
	public IReadOnlyList<string>? Presets { get; set; }

	/// <summary>Whether to show the hex input field. Default is true.</summary>
	[Parameter]
	public bool ShowHexInput { get; set; } = true;

	/// <summary>Whether to show preset color swatches. Default is true.</summary>
	[Parameter]
	public bool ShowPresets { get; set; } = true;

	/// <summary>Whether to show the alpha channel slider. Default is false.</summary>
	[Parameter]
	public bool ShowAlpha { get; set; }

	/// <summary>Whether the input is read-only.</summary>
	[Parameter]
	public bool ReadOnly { get; set; }

	/// <summary>Whether to show individual R/G/B number inputs. Default true.</summary>
	[Parameter]
	public bool ShowRgbInputs { get; set; } = true;

	/// <summary>Whether to show a copy-to-clipboard button for the hex value. Default true.</summary>
	[Parameter]
	public bool ShowCopyButton { get; set; } = true;

	/// <summary>Whether to show an eyedropper button (browser EyeDropper API). Default false.</summary>
	[Parameter]
	public bool ShowEyeDropper { get; set; }

	/// <summary>
	///     When true, automatically detects if the browser supports the EyeDropper API
	///     and shows/hides the eyedropper button accordingly. Overrides <see cref="ShowEyeDropper" />.
	///     Default false.
	/// </summary>
	[Parameter]
	public bool AutoDetectEyeDropper { get; set; }

	/// <summary>Whether to show recently selected colors. Default true.</summary>
	[Parameter]
	public bool ShowRecentColors { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-colorpicker";

	private bool HasError => !string.IsNullOrEmpty(ErrorText);

	private string InputCssClass => new CssBuilder("moka-colorpicker-input")
		.AddClass($"moka-colorpicker-input--{SizeToKebab(Size)}")
		.Build();

	private IReadOnlyList<string> EffectivePresets => Presets ?? DefaultPresets;

	private string HslColor => $"hsl({_hue:F0}, {_saturation:F0}%, {_lightness:F0}%)";

	private bool ShouldShowEyeDropper => AutoDetectEyeDropper ? _eyeDropperSupported : ShowEyeDropper;

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		if (!string.IsNullOrEmpty(CurrentValue))
		{
			HexToHsl(CurrentValue, out _hue, out _saturation, out _lightness, out _alpha);
			UpdateRgbFromHsl();
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (AutoDetectEyeDropper && !_eyeDropperChecked)
		{
			_eyeDropperChecked = true;
			try
			{
				_jsModule ??= await GetJsModuleAsync("./_content/Moka.Red.Core/moka-drag.js");
				_eyeDropperSupported = await _jsModule.InvokeAsync<bool>("isEyeDropperSupported");
				StateHasChanged();
			}
			catch (JSDisconnectedException)
			{
			}
		}

		if (_isOpen)
		{
			await AttachJsInteractionsAsync();
		}
	}

	private async Task AttachJsInteractionsAsync()
	{
		try
		{
			_jsModule ??= await GetJsModuleAsync("./_content/Moka.Red.Core/moka-drag.js");
			_dotNetRef ??= DotNetObjectReference.Create(this);

			if (!_gradientAttached)
			{
				await _jsModule.InvokeVoidAsync("initColorArea", _dotNetRef, _gradientAreaRef, "OnColorAreaChanged");
				_gradientAttached = true;
			}

			if (!_hueAttached)
			{
				await _jsModule.InvokeVoidAsync("initColorSlider", _dotNetRef, _hueSliderRef, "OnHueSliderChanged");
				_hueAttached = true;
			}

			if (ShowAlpha && !_alphaAttached)
			{
				await _jsModule.InvokeVoidAsync("initColorSlider", _dotNetRef, _alphaSliderRef, "OnAlphaSliderChanged");
				_alphaAttached = true;
			}
		}
		catch (JSDisconnectedException)
		{
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
		}
	}

	private async Task DetachJsInteractionsAsync()
	{
		if (_jsModule is null)
		{
			return;
		}

		try
		{
			if (_gradientAttached)
			{
				await _jsModule.InvokeVoidAsync("removeColorArea", _gradientAreaRef);
				_gradientAttached = false;
			}

			if (_hueAttached)
			{
				await _jsModule.InvokeVoidAsync("removeColorSlider", _hueSliderRef);
				_hueAttached = false;
			}

			if (_alphaAttached)
			{
				await _jsModule.InvokeVoidAsync("removeColorSlider", _alphaSliderRef);
				_alphaAttached = false;
			}
		}
		catch (JSDisconnectedException)
		{
		}
	}

	/// <summary>Called from JS when the gradient area is dragged.</summary>
	[JSInvokable]
	public void OnColorAreaChanged(double x, double y)
	{
		_saturation = x * 100;
		_lightness = (1 - y) * 100;
		UpdateRgbFromHsl();
		UpdateColorFromHslSync();
		StateHasChanged();
	}

	/// <summary>Called from JS when the hue slider is dragged.</summary>
	[JSInvokable]
	public void OnHueSliderChanged(double x)
	{
		_hue = x * 360;
		UpdateRgbFromHsl();
		UpdateColorFromHslSync();
		StateHasChanged();
	}

	/// <summary>Called from JS when the alpha slider is dragged.</summary>
	[JSInvokable]
	public void OnAlphaSliderChanged(double x)
	{
		_alpha = Math.Clamp(x, 0, 1);
		UpdateColorFromHslSync();
		StateHasChanged();
	}

	private void ToggleDropdown()
	{
		if (ReadOnly || Disabled)
		{
			return;
		}

		_isOpen = !_isOpen;
		if (_isOpen && !string.IsNullOrEmpty(CurrentValue))
		{
			HexToHsl(CurrentValue, out _hue, out _saturation, out _lightness, out _alpha);
			UpdateRgbFromHsl();
		}

		if (!_isOpen)
		{
			// Fire-and-forget detach since we can't await in sync toggle
			_ = DetachJsInteractionsAsync();
		}
	}

	private async Task CloseDropdown()
	{
		_isOpen = false;
		await DetachJsInteractionsAsync();
		AddToRecent(CurrentValue);
	}

	private void UpdateColorFromHslSync()
	{
		CurrentValue = ShowAlpha
			? HslToHex(_hue, _saturation, _lightness, _alpha)
			: HslToHex(_hue, _saturation, _lightness);
		_ = ValueChanged.InvokeAsync(CurrentValue);
	}

	private async Task UpdateColorFromHsl()
	{
		CurrentValue = ShowAlpha
			? HslToHex(_hue, _saturation, _lightness, _alpha)
			: HslToHex(_hue, _saturation, _lightness);
		UpdateRgbFromHsl();
		await ValueChanged.InvokeAsync(CurrentValue);
	}

	private async Task HandleHexInput(ChangeEventArgs e)
	{
		string hex = e.Value?.ToString()?.Trim() ?? "";
		if (!hex.StartsWith('#'))
		{
			hex = "#" + hex;
		}

		if (IsValidHex(hex))
		{
			CurrentValue = hex;
			HexToHsl(hex, out _hue, out _saturation, out _lightness, out _alpha);
			UpdateRgbFromHsl();
			await ValueChanged.InvokeAsync(CurrentValue);
		}
	}

	private async Task HandleRgbInput(ChangeEventArgs e, char channel)
	{
		if (!int.TryParse(e.Value?.ToString(), out int val))
		{
			return;
		}

		val = Math.Clamp(val, 0, 255);

		switch (channel)
		{
			case 'r': _red = val; break;
			case 'g': _green = val; break;
			case 'b': _blue = val; break;
		}

		CurrentValue = $"#{_red:x2}{_green:x2}{_blue:x2}";
		HexToHsl(CurrentValue, out _hue, out _saturation, out _lightness, out _alpha);
		await ValueChanged.InvokeAsync(CurrentValue);
	}

	private async Task SelectPreset(string color)
	{
		CurrentValue = color;
		HexToHsl(color, out _hue, out _saturation, out _lightness, out _alpha);
		UpdateRgbFromHsl();
		await ValueChanged.InvokeAsync(CurrentValue);
	}

	private async Task PickColorFromScreen()
	{
		try
		{
			_jsModule ??= await GetJsModuleAsync("./_content/Moka.Red.Core/moka-drag.js");
			string? hex = await _jsModule.InvokeAsync<string?>("pickColor");
			if (!string.IsNullOrEmpty(hex))
			{
				CurrentValue = hex;
				HexToHsl(hex, out _hue, out _saturation, out _lightness, out _alpha);
				UpdateRgbFromHsl();
				await ValueChanged.InvokeAsync(CurrentValue);
			}
		}
		catch (JSException)
		{
		}
		catch (JSDisconnectedException)
		{
		}
	}

	private async Task CopyHexToClipboard()
	{
		if (string.IsNullOrEmpty(CurrentValue))
		{
			return;
		}

		try
		{
			_jsModule ??= await GetJsModuleAsync("./_content/Moka.Red.Core/moka-drag.js");
			await _jsModule.InvokeVoidAsync("copyToClipboard", CurrentValue);
		}
		catch (JSDisconnectedException)
		{
		}
	}

	private void AddToRecent(string? color)
	{
		if (string.IsNullOrEmpty(color))
		{
			return;
		}

		_recentColors.Remove(color);
		_recentColors.Insert(0, color);
		if (_recentColors.Count > MaxRecentColors)
		{
			_recentColors.RemoveAt(_recentColors.Count - 1);
		}
	}

	private void UpdateRgbFromHsl()
	{
		string hex = HslToHex(_hue, _saturation, _lightness);
		string body = hex.TrimStart('#');
		if (body.Length >= 6)
		{
			int.TryParse(body[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _red);
			int.TryParse(body[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _green);
			int.TryParse(body[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _blue);
		}
	}

	// --- Color conversion helpers ---

	/// <summary>Converts HSL values to a hex color string.</summary>
	private static string HslToHex(double h, double s, double l, double a = 1)
	{
		s /= 100;
		l /= 100;

		double c = (1 - Math.Abs(2 * l - 1)) * s;
		double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
		double m = l - c / 2;

		double r, g, b;
		if (h < 60)
		{
			r = c;
			g = x;
			b = 0;
		}
		else if (h < 120)
		{
			r = x;
			g = c;
			b = 0;
		}
		else if (h < 180)
		{
			r = 0;
			g = c;
			b = x;
		}
		else if (h < 240)
		{
			r = 0;
			g = x;
			b = c;
		}
		else if (h < 300)
		{
			r = x;
			g = 0;
			b = c;
		}
		else
		{
			r = c;
			g = 0;
			b = x;
		}

		int ri = (int)Math.Round((r + m) * 255);
		int gi = (int)Math.Round((g + m) * 255);
		int bi = (int)Math.Round((b + m) * 255);

		if (a < 1)
		{
			int ai = (int)Math.Round(a * 255);
			return $"#{ri:x2}{gi:x2}{bi:x2}{ai:x2}";
		}

		return $"#{ri:x2}{gi:x2}{bi:x2}";
	}

	/// <summary>Converts a hex color string to HSL values.</summary>
	private static void HexToHsl(string hex, out double h, out double s, out double l, out double a)
	{
		h = 0;
		s = 0;
		l = 0;
		a = 1;

		hex = hex.TrimStart('#');
		if (hex.Length == 3)
		{
			hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
		}
		else if (hex.Length == 4)
		{
			hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}{hex[3]}{hex[3]}";
		}

		if (hex.Length < 6)
		{
			return;
		}

		if (!int.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r))
		{
			return;
		}

		if (!int.TryParse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g))
		{
			return;
		}

		if (!int.TryParse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
		{
			return;
		}

		if (hex.Length >= 8 &&
		    int.TryParse(hex[6..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int ai))
		{
			a = ai / 255.0;
		}

		double rf = r / 255.0;
		double gf = g / 255.0;
		double bf = b / 255.0;

		double max = Math.Max(rf, Math.Max(gf, bf));
		double min = Math.Min(rf, Math.Min(gf, bf));
		double delta = max - min;

		l = (max + min) / 2;

		if (delta == 0)
		{
			h = 0;
			s = 0;
		}
		else
		{
			s = l > 0.5 ? delta / (2 - max - min) : delta / (max + min);

			if (max == rf)
			{
				h = ((gf - bf) / delta + (gf < bf ? 6 : 0)) * 60;
			}
			else if (max == gf)
			{
				h = ((bf - rf) / delta + 2) * 60;
			}
			else
			{
				h = ((rf - gf) / delta + 4) * 60;
			}
		}

		s *= 100;
		l *= 100;
	}

	private static bool IsValidHex(string hex)
	{
		if (string.IsNullOrEmpty(hex) || hex[0] != '#')
		{
			return false;
		}

		string body = hex[1..];
		return body.Length is 3 or 4 or 6 or 8 &&
		       body.All(c => char.IsAsciiHexDigit(c));
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			result = "";
			validationErrorMessage = "";
			return true;
		}

		if (IsValidHex(value))
		{
			result = value;
			validationErrorMessage = "";
			return true;
		}

		result = "";
		validationErrorMessage = $"'{value}' is not a valid hex color.";
		return false;
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		await DetachJsInteractionsAsync();
		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
