using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.ColorWheel;

/// <summary>
///     A circular color picker with a hue wheel and a saturation/lightness area.
///     Outputs a hex color string. Supports optional hex input and preview swatch.
/// </summary>
public partial class MokaColorWheel : MokaVisualComponentBase
{
	private double _hue;
	private double _lightness = 50;
	private double _saturation = 100;

	/// <summary>The current color value as a hex string (e.g., "#ff0000").</summary>
	[Parameter]
	public string Value { get; set; } = "#ef5350";

	/// <summary>Callback invoked when the color value changes.</summary>
	[Parameter]
	public EventCallback<string> ValueChanged { get; set; }

	/// <summary>Whether to show a hex input field below the wheel.</summary>
	[Parameter]
	public bool ShowHexInput { get; set; } = true;

	/// <summary>Whether to show a color preview swatch.</summary>
	[Parameter]
	public bool ShowPreview { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-color-wheel";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-color-wheel--{SizeToKebab(Size)}")
		.AddClass("moka-color-wheel--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string WheelSize => Size switch
	{
		MokaSize.Xs => "8rem",
		MokaSize.Sm => "10rem",
		MokaSize.Md => "12rem",
		MokaSize.Lg => "16rem",
		_ => "12rem"
	};

	private static string HueBackground =>
		"conic-gradient(from 0deg, hsl(0,100%,50%), hsl(60,100%,50%), hsl(120,100%,50%), hsl(180,100%,50%), hsl(240,100%,50%), hsl(300,100%,50%), hsl(360,100%,50%))";

	private string SlAreaBackground =>
		$"linear-gradient(to right, hsl({_hue:F0}, 0%, 50%), hsl({_hue:F0}, 100%, 50%))";

	private static string SlAreaOverlay =>
		"linear-gradient(to bottom, hsl(0, 0%, 100%), transparent, hsl(0, 0%, 0%))";

	private string CurrentHexColor => HslToHex(_hue, _saturation, _lightness);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		ParseHexToHsl(Value);
	}

	private void ParseHexToHsl(string hex)
	{
		if (string.IsNullOrWhiteSpace(hex))
		{
			return;
		}

		hex = hex.TrimStart('#');
		if (hex.Length is not (6 or 3))
		{
			return;
		}

		if (hex.Length == 3)
		{
			hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
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

		RgbToHsl(r, g, b, out _hue, out _saturation, out _lightness);
	}

	private static void RgbToHsl(int r, int g, int b, out double h, out double s, out double l)
	{
		double rf = r / 255.0;
		double gf = g / 255.0;
		double bf = b / 255.0;

		double max = Math.Max(rf, Math.Max(gf, bf));
		double min = Math.Min(rf, Math.Min(gf, bf));
		double delta = max - min;

		l = (max + min) / 2.0 * 100.0;

		if (delta < 0.0001)
		{
			h = 0;
			s = 0;
			return;
		}

		s = l > 50 ? delta / (2.0 - max - min) * 100.0 : delta / (max + min) * 100.0;

		if (Math.Abs(max - rf) < 0.0001)
		{
			h = ((gf - bf) / delta + (gf < bf ? 6 : 0)) * 60.0;
		}
		else if (Math.Abs(max - gf) < 0.0001)
		{
			h = ((bf - rf) / delta + 2) * 60.0;
		}
		else
		{
			h = ((rf - gf) / delta + 4) * 60.0;
		}
	}

	private static string HslToHex(double h, double s, double l)
	{
		double sn = s / 100.0;
		double ln = l / 100.0;

		double c = (1 - Math.Abs(2 * ln - 1)) * sn;
		double x = c * (1 - Math.Abs(h / 60.0 % 2 - 1));
		double m = ln - c / 2;

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

		return $"#{ri:x2}{gi:x2}{bi:x2}";
	}

	private async Task OnHueChanged(ChangeEventArgs e)
	{
		if (Disabled)
		{
			return;
		}

		if (double.TryParse(e.Value?.ToString(), CultureInfo.InvariantCulture, out double hue))
		{
			_hue = Math.Clamp(hue, 0, 360);
			await EmitValueAsync();
		}
	}

	private async Task OnSaturationChanged(ChangeEventArgs e)
	{
		if (Disabled)
		{
			return;
		}

		if (double.TryParse(e.Value?.ToString(), CultureInfo.InvariantCulture, out double sat))
		{
			_saturation = Math.Clamp(sat, 0, 100);
			await EmitValueAsync();
		}
	}

	private async Task OnLightnessChanged(ChangeEventArgs e)
	{
		if (Disabled)
		{
			return;
		}

		if (double.TryParse(e.Value?.ToString(), CultureInfo.InvariantCulture, out double lit))
		{
			_lightness = Math.Clamp(lit, 0, 100);
			await EmitValueAsync();
		}
	}

	private async Task OnHexInput(ChangeEventArgs e)
	{
		if (Disabled)
		{
			return;
		}

		string hex = e.Value?.ToString() ?? string.Empty;
		if (hex.StartsWith('#') && hex.Length == 7)
		{
			ParseHexToHsl(hex);
			Value = hex;
			await ValueChanged.InvokeAsync(Value);
		}
	}

	/// <summary>
	///     Handles click on the saturation/lightness area.
	///     OffsetX/Y are relative to the SL area div (children have pointer-events: none).
	///     X axis = saturation (0-100%), Y axis = lightness (100% at top, 0% at bottom).
	///     We approximate the SL area pixel size from the known wheel rem sizes minus padding.
	/// </summary>
	private async Task OnSlAreaClick(MouseEventArgs e)
	{
		if (Disabled)
		{
			return;
		}

		// Wheel rem sizes: Xs=8, Sm=10, Md=12, Lg=16. Assume 16px/rem.
		// Ring padding = ~8px (moka-spacing-md), inner padding = ~6px (moka-spacing-sm), each side.
		// SL area ≈ wheelPx - 2*(ringPad + innerPad) = wheelPx - 28
		double wheelPx = Size switch
		{
			MokaSize.Xs => 128,
			MokaSize.Sm => 160,
			MokaSize.Lg => 256,
			_ => 192
		};
		double slSize = Math.Max(1, wheelPx - 28);

		_saturation = Math.Clamp(e.OffsetX / slSize * 100.0, 0, 100);
		_lightness = Math.Clamp(100.0 - e.OffsetY / slSize * 100.0, 0, 100);
		await EmitValueAsync();
	}

	/// <summary>Has internal color state.</summary>
	protected override bool ShouldRender() => true;

	private async Task EmitValueAsync()
	{
		Value = CurrentHexColor;
		await ValueChanged.InvokeAsync(Value);
	}
}
