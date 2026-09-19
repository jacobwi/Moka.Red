using System.Globalization;
using System.Text;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Theming;

/// <summary>
///     Complete theme definition composing palette, typography, and spacing.
///     Use <see cref="ToCssVariables" /> to generate CSS custom property declarations.
/// </summary>
public sealed record MokaTheme
{
	public required MokaPalette Palette { get; init; }
	public MokaTypography Typography { get; init; } = MokaTypography.Default;
	public MokaSpacing Spacing { get; init; } = MokaSpacing.Default;
	public bool IsDark { get; init; }

	/// <summary>
	///     Density multiplier applied to the spacing scale. 1.0 is the default (cozy).
	///     0.75 gives compact layouts, 1.15 comfy. Radii and font sizes are not affected.
	/// </summary>
	public double Density { get; init; } = 1.0;

	/// <summary>
	///     Multiplier applied to every font-size token. 1.0 keeps the dense 13px base scale.
	/// </summary>
	public double FontScale { get; init; } = 1.0;

	/// <summary>Built-in light theme with Moka Red primary color.</summary>
	public static MokaTheme Light => new()
	{
		Palette = MokaPalette.Light,
		IsDark = false
	};

	/// <summary>Built-in dark theme.</summary>
	public static MokaTheme Dark => new()
	{
		Palette = MokaPalette.Dark,
		IsDark = true
	};

	// ── Fluent customization API ──────────────────────────────────

	/// <summary>Creates a new theme with only the primary color changed.</summary>
	public MokaTheme WithPrimary(string color) => this with { Palette = Palette with { Primary = color } };

	/// <summary>Creates a new theme with only the secondary color changed.</summary>
	public MokaTheme WithSecondary(string color) => this with { Palette = Palette with { Secondary = color } };

	/// <summary>Creates a new theme with only the surface color changed.</summary>
	public MokaTheme WithSurface(string surface, string onSurface) =>
		this with { Palette = Palette with { Surface = surface, OnSurface = onSurface } };

	/// <summary>Creates a new theme with the base font size changed.</summary>
	public MokaTheme WithFontSize(string baseSize) =>
		this with { Typography = Typography with { FontSizeBase = baseSize } };

	/// <summary>Creates a new theme with the font family changed.</summary>
	public MokaTheme WithFontFamily(string fontFamily) =>
		this with { Typography = Typography with { FontFamily = fontFamily } };

	/// <summary>Creates a new theme with the spacing scale replaced.</summary>
	public MokaTheme WithSpacing(MokaSpacing spacing) => this with { Spacing = spacing };

	/// <summary>Creates a new theme with dark mode toggled.</summary>
	public MokaTheme WithDark(bool isDark) => isDark
		? this with { Palette = MokaPalette.Dark, IsDark = true }
		: this with { Palette = MokaPalette.Light, IsDark = false };

	/// <summary>
	///     Creates a new theme re-tinted around the given accent color (#rrggbb or #rgb).
	///     Recomputes the primary color, its light/dark variants, and every derived glow/border
	///     token using the standard alpha tiers. Dark themes also get accent-tinted outlines,
	///     and the dark shadow rings follow the accent automatically in <see cref="ToCssVariables" />.
	/// </summary>
	/// <exception cref="ArgumentException">Thrown when <paramref name="color" /> is not a hex color.</exception>
	public MokaTheme WithAccent(string color)
	{
		if (!TryParseHex(color, out int r, out int g, out int b))
		{
			throw new ArgumentException($"Accent must be a hex color like #ef5350, got '{color}'.", nameof(color));
		}

		MokaPalette palette = Palette with
		{
			Primary = color,
			PrimaryLight = Mix(r, g, b, 255, 0.30),
			PrimaryDark = Mix(r, g, b, 0, 0.30),
			PrimaryGlow = Rgba(r, g, b, 0.08),
			PrimaryGlowMd = Rgba(r, g, b, 0.15),
			PrimaryGlowStrong = Rgba(r, g, b, 0.25),
			PrimaryGlowFaint = Rgba(r, g, b, 0.03),
			PrimaryBorder = Rgba(r, g, b, 0.20),
			PrimaryBorderDim = Rgba(r, g, b, 0.08)
		};

		if (IsDark)
		{
			palette = palette with
			{
				Outline = Rgba(r, g, b, 0.12),
				OutlineVariant = Rgba(r, g, b, 0.06)
			};
		}

		return this with { Palette = palette };
	}

	/// <summary>Creates a new theme with the spacing density changed (0.75 compact, 1.0 cozy, 1.15 comfy).</summary>
	public MokaTheme WithDensity(double density) => this with { Density = density };

	/// <summary>Creates a new theme with all font-size tokens scaled by the given factor.</summary>
	public MokaTheme WithFontScale(double fontScale) => this with { FontScale = fontScale };

	/// <summary>
	///     Generates CSS custom property declarations for all theme tokens.
	///     Output is suitable for use as an inline style attribute value or inside a CSS rule.
	///     A token whose value holds any of <c>; { } &lt; &gt; \</c> (see <see cref="CssValues.IsSafe" />), or
	///     leaves a string, bracket or comment open (see <see cref="CssValues.IsSelfContained" />), is left
	///     out, so the <c>moka.css</c> default for it applies instead.
	/// </summary>
	public string ToCssVariables()
	{
		var sb = new StringBuilder(1024);

		// Palette
		AppendVar(sb, "--moka-color-primary", Palette.Primary);
		AppendVar(sb, "--moka-color-primary-light", Palette.PrimaryLight);
		AppendVar(sb, "--moka-color-primary-dark", Palette.PrimaryDark);
		AppendVar(sb, "--moka-color-on-primary", Palette.OnPrimary);

		AppendVar(sb, "--moka-color-secondary", Palette.Secondary);
		AppendVar(sb, "--moka-color-secondary-light", Palette.SecondaryLight);
		AppendVar(sb, "--moka-color-secondary-dark", Palette.SecondaryDark);
		AppendVar(sb, "--moka-color-on-secondary", Palette.OnSecondary);

		AppendVar(sb, "--moka-color-surface", Palette.Surface);
		AppendVar(sb, "--moka-color-surface-variant", Palette.SurfaceVariant);
		AppendVar(sb, "--moka-color-on-surface", Palette.OnSurface);
		AppendVar(sb, "--moka-color-on-surface-variant", Palette.OnSurfaceVariant);
		AppendVar(sb, "--moka-color-background", Palette.Background);
		AppendVar(sb, "--moka-color-on-background", Palette.OnBackground);

		AppendVar(sb, "--moka-color-error", Palette.Error);
		AppendVar(sb, "--moka-color-on-error", Palette.OnError);
		AppendVar(sb, "--moka-color-warning", Palette.Warning);
		AppendVar(sb, "--moka-color-on-warning", Palette.OnWarning);
		AppendVar(sb, "--moka-color-success", Palette.Success);
		AppendVar(sb, "--moka-color-on-success", Palette.OnSuccess);
		AppendVar(sb, "--moka-color-info", Palette.Info);
		AppendVar(sb, "--moka-color-on-info", Palette.OnInfo);

		// Semantic dim fills (chips, pills, soft badges)
		AppendVar(sb, "--moka-color-error-dim", Palette.ErrorDim);
		AppendVar(sb, "--moka-color-warning-dim", Palette.WarningDim);
		AppendVar(sb, "--moka-color-success-dim", Palette.SuccessDim);
		AppendVar(sb, "--moka-color-info-dim", Palette.InfoDim);

		AppendVar(sb, "--moka-color-outline", Palette.Outline);
		AppendVar(sb, "--moka-color-outline-variant", Palette.OutlineVariant);

		// Extended surface scale
		AppendVar(sb, "--moka-color-surface-hover", Palette.SurfaceHover);
		AppendVar(sb, "--moka-color-surface-2", Palette.Surface2);
		AppendVar(sb, "--moka-color-surface-3", Palette.Surface3);

		// Accent glow tokens (focus rings, selected states, hover glows)
		AppendVar(sb, "--moka-color-primary-glow", Palette.PrimaryGlow);
		AppendVar(sb, "--moka-color-primary-glow-md", Palette.PrimaryGlowMd);
		AppendVar(sb, "--moka-color-primary-glow-strong", Palette.PrimaryGlowStrong);
		AppendVar(sb, "--moka-color-primary-border", Palette.PrimaryBorder);
		AppendVar(sb, "--moka-color-primary-border-dim", Palette.PrimaryBorderDim);
		AppendVar(sb, "--moka-color-primary-glow-faint", Palette.PrimaryGlowFaint);

		// Text scale (tertiary/quaternary)
		AppendVar(sb, "--moka-color-on-surface-tertiary", Palette.OnSurfaceTertiary);
		AppendVar(sb, "--moka-color-on-surface-quaternary", Palette.OnSurfaceQuaternary);

		// Typography
		AppendVar(sb, "--moka-font-family", Typography.FontFamily);
		AppendVar(sb, "--moka-font-family-mono", Typography.FontFamilyMono);
		AppendVar(sb, "--moka-font-size-xs", ScaleSize(Typography.FontSizeXs, FontScale));
		AppendVar(sb, "--moka-font-size-sm", ScaleSize(Typography.FontSizeSm, FontScale));
		AppendVar(sb, "--moka-font-size-base", ScaleSize(Typography.FontSizeBase, FontScale));
		AppendVar(sb, "--moka-font-size-md", ScaleSize(Typography.FontSizeMd, FontScale));
		AppendVar(sb, "--moka-font-size-lg", ScaleSize(Typography.FontSizeLg, FontScale));
		AppendVar(sb, "--moka-font-size-xl", ScaleSize(Typography.FontSizeXl, FontScale));
		AppendVar(sb, "--moka-font-size-xxl", ScaleSize(Typography.FontSizeXxl, FontScale));
		AppendVar(sb, "--moka-line-height-tight", Typography.LineHeightTight);
		AppendVar(sb, "--moka-line-height-base", Typography.LineHeightBase);
		AppendVar(sb, "--moka-line-height-relaxed", Typography.LineHeightRelaxed);
		AppendVar(sb, "--moka-font-weight-light", Typography.FontWeightLight);
		AppendVar(sb, "--moka-font-weight-normal", Typography.FontWeightNormal);
		AppendVar(sb, "--moka-font-weight-medium", Typography.FontWeightMedium);
		AppendVar(sb, "--moka-font-weight-semibold", Typography.FontWeightSemibold);
		AppendVar(sb, "--moka-font-weight-bold", Typography.FontWeightBold);

		// Spacing (scaled by density)
		AppendVar(sb, "--moka-spacing-xxs", ScaleSize(Spacing.Xxs, Density));
		AppendVar(sb, "--moka-spacing-xs", ScaleSize(Spacing.Xs, Density));
		AppendVar(sb, "--moka-spacing-sm", ScaleSize(Spacing.Sm, Density));
		AppendVar(sb, "--moka-spacing-md", ScaleSize(Spacing.Md, Density));
		AppendVar(sb, "--moka-spacing-lg", ScaleSize(Spacing.Lg, Density));
		AppendVar(sb, "--moka-spacing-xl", ScaleSize(Spacing.Xl, Density));
		AppendVar(sb, "--moka-spacing-xxl", ScaleSize(Spacing.Xxl, Density));

		// Scale factors, exposed for consumer CSS (calc(Npx * var(--moka-density)))
		AppendVar(sb, "--moka-density", Density.ToString("0.###", CultureInfo.InvariantCulture));
		AppendVar(sb, "--moka-font-scale", FontScale.ToString("0.###", CultureInfo.InvariantCulture));

		// Border radius
		AppendVar(sb, "--moka-radius-none", Spacing.RadiusNone);
		AppendVar(sb, "--moka-radius-sm", Spacing.RadiusSm);
		AppendVar(sb, "--moka-radius-md", Spacing.RadiusMd);
		AppendVar(sb, "--moka-radius-lg", Spacing.RadiusLg);
		AppendVar(sb, "--moka-radius-xl", Spacing.RadiusXl);
		AppendVar(sb, "--moka-radius-full", Spacing.RadiusFull);

		// Elevation / box-shadow. Dark shadows are accent-tinted glow rings: recompute them
		// from the primary color when it parses as hex so WithAccent() re-tints them too;
		// otherwise fall back to the interned red constants.
		string shadow1 = Shadows.Light1, shadow2 = Shadows.Light2, shadow3 = Shadows.Light3, shadow4 = Shadows.Light4;
		string shadowPopup = Shadows.LightPopup, shadowPopupLg = Shadows.LightPopupLg;
		string shadowModal = Shadows.LightModal, shadowSubtle = Shadows.LightSubtle;

		if (IsDark)
		{
			if (TryParseHex(Palette.Primary, out int ar, out int ag, out int ab))
			{
				shadow1 = $"0 0 0 1px {Rgba(ar, ag, ab, 0.06)}";
				shadow2 = $"0 0 0 1px {Rgba(ar, ag, ab, 0.12)}";
				shadow3 = $"0 0 0 1px {Rgba(ar, ag, ab, 0.12)}, 0 0 12px {Rgba(ar, ag, ab, 0.08)}";
				shadow4 = $"0 0 0 1px {Rgba(ar, ag, ab, 0.20)}, 0 0 24px {Rgba(ar, ag, ab, 0.12)}";
				shadowPopup = $"0 0 0 1px {Rgba(ar, ag, ab, 0.12)}, 0 0 16px {Rgba(ar, ag, ab, 0.08)}";
				shadowPopupLg = $"0 0 0 1px {Rgba(ar, ag, ab, 0.16)}, 0 0 24px {Rgba(ar, ag, ab, 0.12)}";
				shadowModal = $"0 0 0 1px {Rgba(ar, ag, ab, 0.20)}, 0 0 32px {Rgba(ar, ag, ab, 0.15)}";
				shadowSubtle = $"0 0 0 1px {Rgba(ar, ag, ab, 0.06)}";
			}
			else
			{
				shadow1 = Shadows.Dark1;
				shadow2 = Shadows.Dark2;
				shadow3 = Shadows.Dark3;
				shadow4 = Shadows.Dark4;
				shadowPopup = Shadows.DarkPopup;
				shadowPopupLg = Shadows.DarkPopupLg;
				shadowModal = Shadows.DarkModal;
				shadowSubtle = Shadows.DarkSubtle;
			}
		}

		AppendVar(sb, "--moka-shadow-0", "none");
		AppendVar(sb, "--moka-shadow-1", shadow1);
		AppendVar(sb, "--moka-shadow-2", shadow2);
		AppendVar(sb, "--moka-shadow-3", shadow3);
		AppendVar(sb, "--moka-shadow-4", shadow4);
		AppendVar(sb, "--moka-shadow-popup", shadowPopup);
		AppendVar(sb, "--moka-shadow-popup-lg", shadowPopupLg);
		AppendVar(sb, "--moka-shadow-modal", shadowModal);
		AppendVar(sb, "--moka-shadow-subtle", shadowSubtle);

		// Transitions - fast and subtle, 120-200ms
		AppendVar(sb, "--moka-transition-fast", "120ms ease");
		AppendVar(sb, "--moka-transition-normal", "150ms ease");
		AppendVar(sb, "--moka-transition-slow", "200ms ease");

		// Component heights
		AppendVar(sb, "--moka-height-statusbar", "22px");
		AppendVar(sb, "--moka-height-toolbar", "40px");
		AppendVar(sb, "--moka-height-toolbar-dense", "32px");

		// Z-index scale
		AppendVar(sb, "--moka-z-dropdown", "1000");
		AppendVar(sb, "--moka-z-sticky", "1020");
		AppendVar(sb, "--moka-z-fixed", "1030");
		AppendVar(sb, "--moka-z-modal-backdrop", "1040");
		AppendVar(sb, "--moka-z-modal", "1050");
		AppendVar(sb, "--moka-z-popover", "1060");
		AppendVar(sb, "--moka-z-tooltip", "1070");
		// Above other fixed bars, below every overlay: 1100 drew the app bar over open dialogs.
		AppendVar(sb, "--moka-z-appbar", "1035");

		// Semantic state tokens
		AppendVar(sb, "--moka-opacity-disabled", "0.4");
		AppendVar(sb, "--moka-opacity-hover", "0.08");
		AppendVar(sb, "--moka-focus-color", "var(--moka-color-primary)");
		AppendVar(sb, "--moka-focus-width", "2px");
		AppendVar(sb, "--moka-border-width", "1px");

		// Focus ring - the signature red-glow interaction
		AppendVar(sb, "--moka-focus-ring",
			IsDark
				? "0 0 0 3px var(--moka-color-primary-glow), inset 0 0 12px var(--moka-color-primary-glow)"
				: "0 0 0 3px var(--moka-color-primary-glow)");

		// Selected state - inset glow (for list rows, tabs, active items)
		AppendVar(sb, "--moka-selected-glow",
			IsDark
				? "inset 0 0 20px var(--moka-color-primary-glow), 0 0 12px var(--moka-color-primary-glow)"
				: "0 0 0 2px var(--moka-color-primary-glow-md)");

		// Behind modal overlays: the theme's background at 85%, so a dark theme dims the page and a
		// light one frosts it.
		AppendVar(sb, "--moka-color-backdrop", "color-mix(in srgb, var(--moka-color-background) 85%, transparent)");
		AppendVar(sb, "--moka-backdrop-blur", "8px");

		return sb.ToString();
	}

	// MokaThemeProvider writes this output into a <style> element, where a semicolon or a brace in a
	// value would add declarations or whole rules for the page, and < could end the element. An open
	// string or bracket would swallow every token after it.
	private static void AppendVar(StringBuilder sb, string name, string value)
	{
		if (!CssValues.IsSafe(value) || !CssValues.IsSelfContained(value))
		{
			return;
		}

		sb.Append(name);
		sb.Append(": ");
		sb.Append(value);
		sb.Append("; ");
	}

	private static string Rgba(int r, int g, int b, double alpha) =>
		string.Create(CultureInfo.InvariantCulture, $"rgba({r}, {g}, {b}, {alpha:0.00})");

	private static string Mix(int r, int g, int b, int target, double amount)
	{
		int MixChannel(int channel) => (int)Math.Round(channel + ((target - channel) * amount));
		return string.Create(CultureInfo.InvariantCulture, $"#{MixChannel(r):x2}{MixChannel(g):x2}{MixChannel(b):x2}");
	}

	private static bool TryParseHex(string color, out int r, out int g, out int b)
	{
		r = g = b = 0;

		if (string.IsNullOrEmpty(color) || color[0] != '#')
		{
			return false;
		}

		string hex = color[1..];
		if (hex.Length == 3)
		{
			hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
		}

		if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
		{
			return false;
		}

		r = (rgb >> 16) & 0xff;
		g = (rgb >> 8) & 0xff;
		b = rgb & 0xff;
		return true;
	}

	private static string ScaleSize(string value, double factor)
	{
		if (factor == 1.0 || string.IsNullOrEmpty(value))
		{
			return value;
		}

		int unitStart = 0;
		while (unitStart < value.Length && (char.IsAsciiDigit(value[unitStart]) || value[unitStart] == '.'))
		{
			unitStart++;
		}

		if (unitStart == 0 ||
		    !double.TryParse(value[..unitStart], NumberStyles.Float, CultureInfo.InvariantCulture, out double size))
		{
			return value;
		}

		return string.Create(CultureInfo.InvariantCulture, $"{size * factor:0.####}{value[unitStart..]}");
	}

	/// <summary>Compile-time interned shadow string constants. Zero runtime allocation.</summary>
	private static class Shadows
	{
		public const string Light1 = "0 1px 3px rgba(0, 0, 0, 0.08), 0 1px 2px rgba(0, 0, 0, 0.06)";
		public const string Light2 = "0 4px 6px rgba(0, 0, 0, 0.07), 0 2px 4px rgba(0, 0, 0, 0.06)";
		public const string Light3 = "0 10px 15px rgba(0, 0, 0, 0.07), 0 4px 6px rgba(0, 0, 0, 0.05)";
		public const string Light4 = "0 20px 25px rgba(0, 0, 0, 0.08), 0 8px 10px rgba(0, 0, 0, 0.04)";
		public const string LightPopup = "0 4px 12px rgba(0, 0, 0, 0.15)";
		public const string LightPopupLg = "0 4px 16px rgba(0, 0, 0, 0.12)";
		public const string LightModal = "0 8px 32px rgba(0, 0, 0, 0.2)";
		public const string LightSubtle = "0 1px 4px rgba(0, 0, 0, 0.1)";

		// Dark mode: glow rings instead of drop shadows (matrix aesthetic)
		public const string Dark1 = "0 0 0 1px rgba(239, 83, 80, 0.06)";
		public const string Dark2 = "0 0 0 1px rgba(239, 83, 80, 0.12)";
		public const string Dark3 = "0 0 0 1px rgba(239, 83, 80, 0.12), 0 0 12px rgba(239, 83, 80, 0.08)";
		public const string Dark4 = "0 0 0 1px rgba(239, 83, 80, 0.20), 0 0 24px rgba(239, 83, 80, 0.12)";
		public const string DarkPopup = "0 0 0 1px rgba(239, 83, 80, 0.12), 0 0 16px rgba(239, 83, 80, 0.08)";
		public const string DarkPopupLg = "0 0 0 1px rgba(239, 83, 80, 0.16), 0 0 24px rgba(239, 83, 80, 0.12)";
		public const string DarkModal = "0 0 0 1px rgba(239, 83, 80, 0.20), 0 0 32px rgba(239, 83, 80, 0.15)";
		public const string DarkSubtle = "0 0 0 1px rgba(239, 83, 80, 0.06)";
	}
}
