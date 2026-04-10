using System.Text;

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
	///     Generates CSS custom property declarations for all theme tokens.
	///     Output is suitable for use as an inline style attribute value.
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

		// Text scale (tertiary/quaternary)
		AppendVar(sb, "--moka-color-on-surface-tertiary", Palette.OnSurfaceTertiary);
		AppendVar(sb, "--moka-color-on-surface-quaternary", Palette.OnSurfaceQuaternary);

		// Typography
		AppendVar(sb, "--moka-font-family", Typography.FontFamily);
		AppendVar(sb, "--moka-font-family-mono", Typography.FontFamilyMono);
		AppendVar(sb, "--moka-font-size-xs", Typography.FontSizeXs);
		AppendVar(sb, "--moka-font-size-sm", Typography.FontSizeSm);
		AppendVar(sb, "--moka-font-size-base", Typography.FontSizeBase);
		AppendVar(sb, "--moka-font-size-md", Typography.FontSizeMd);
		AppendVar(sb, "--moka-font-size-lg", Typography.FontSizeLg);
		AppendVar(sb, "--moka-font-size-xl", Typography.FontSizeXl);
		AppendVar(sb, "--moka-font-size-xxl", Typography.FontSizeXxl);
		AppendVar(sb, "--moka-line-height-tight", Typography.LineHeightTight);
		AppendVar(sb, "--moka-line-height-base", Typography.LineHeightBase);
		AppendVar(sb, "--moka-line-height-relaxed", Typography.LineHeightRelaxed);
		AppendVar(sb, "--moka-font-weight-light", Typography.FontWeightLight);
		AppendVar(sb, "--moka-font-weight-normal", Typography.FontWeightNormal);
		AppendVar(sb, "--moka-font-weight-medium", Typography.FontWeightMedium);
		AppendVar(sb, "--moka-font-weight-semibold", Typography.FontWeightSemibold);
		AppendVar(sb, "--moka-font-weight-bold", Typography.FontWeightBold);

		// Spacing
		AppendVar(sb, "--moka-spacing-xxs", Spacing.Xxs);
		AppendVar(sb, "--moka-spacing-xs", Spacing.Xs);
		AppendVar(sb, "--moka-spacing-sm", Spacing.Sm);
		AppendVar(sb, "--moka-spacing-md", Spacing.Md);
		AppendVar(sb, "--moka-spacing-lg", Spacing.Lg);
		AppendVar(sb, "--moka-spacing-xl", Spacing.Xl);
		AppendVar(sb, "--moka-spacing-xxl", Spacing.Xxl);

		// Border radius
		AppendVar(sb, "--moka-radius-none", Spacing.RadiusNone);
		AppendVar(sb, "--moka-radius-sm", Spacing.RadiusSm);
		AppendVar(sb, "--moka-radius-md", Spacing.RadiusMd);
		AppendVar(sb, "--moka-radius-lg", Spacing.RadiusLg);
		AppendVar(sb, "--moka-radius-xl", Spacing.RadiusXl);
		AppendVar(sb, "--moka-radius-full", Spacing.RadiusFull);

		// Elevation / box-shadow (compile-time interned constants)
		AppendVar(sb, "--moka-shadow-0", "none");
		AppendVar(sb, "--moka-shadow-1", IsDark ? Shadows.Dark1 : Shadows.Light1);
		AppendVar(sb, "--moka-shadow-2", IsDark ? Shadows.Dark2 : Shadows.Light2);
		AppendVar(sb, "--moka-shadow-3", IsDark ? Shadows.Dark3 : Shadows.Light3);
		AppendVar(sb, "--moka-shadow-4", IsDark ? Shadows.Dark4 : Shadows.Light4);
		AppendVar(sb, "--moka-shadow-popup", IsDark ? Shadows.DarkPopup : Shadows.LightPopup);
		AppendVar(sb, "--moka-shadow-popup-lg", IsDark ? Shadows.DarkPopupLg : Shadows.LightPopupLg);
		AppendVar(sb, "--moka-shadow-modal", IsDark ? Shadows.DarkModal : Shadows.LightModal);
		AppendVar(sb, "--moka-shadow-subtle", IsDark ? Shadows.DarkSubtle : Shadows.LightSubtle);

		// Transitions — fast and subtle, 120–200ms
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
		AppendVar(sb, "--moka-z-appbar", "1100");

		// Semantic state tokens
		AppendVar(sb, "--moka-opacity-disabled", "0.4");
		AppendVar(sb, "--moka-opacity-hover", "0.08");
		AppendVar(sb, "--moka-focus-color", "var(--moka-color-primary)");
		AppendVar(sb, "--moka-focus-width", "2px");
		AppendVar(sb, "--moka-border-width", "1px");

		// Focus ring — the signature red-glow interaction
		AppendVar(sb, "--moka-focus-ring",
			IsDark
				? "0 0 0 3px var(--moka-color-primary-glow), inset 0 0 12px var(--moka-color-primary-glow)"
				: "0 0 0 3px var(--moka-color-primary-glow)");

		// Selected state — inset glow (for list rows, tabs, active items)
		AppendVar(sb, "--moka-selected-glow",
			IsDark
				? "inset 0 0 20px var(--moka-color-primary-glow), 0 0 12px var(--moka-color-primary-glow)"
				: "0 0 0 2px var(--moka-color-primary-glow-md)");

		return sb.ToString();
	}

	private static void AppendVar(StringBuilder sb, string name, string value)
	{
		sb.Append(name);
		sb.Append(": ");
		sb.Append(value);
		sb.Append("; ");
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
