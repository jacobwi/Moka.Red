namespace Moka.Red.Core.Theming;

/// <summary>
///     Color palette definition for a Moka.Red theme.
///     All values are CSS color strings (hex, rgb, hsl, etc.).
/// </summary>
public sealed partial record MokaPalette
{
	// Primary
	public required string Primary { get; init; }
	public required string PrimaryLight { get; init; }
	public required string PrimaryDark { get; init; }
	public required string OnPrimary { get; init; }

	// Secondary
	public required string Secondary { get; init; }
	public required string SecondaryLight { get; init; }
	public required string SecondaryDark { get; init; }
	public required string OnSecondary { get; init; }

	// Surface & background
	public required string Surface { get; init; }
	public required string SurfaceVariant { get; init; }
	public required string OnSurface { get; init; }
	public string OnSurfaceVariant { get; init; } = "#666666";
	public required string Background { get; init; }
	public required string OnBackground { get; init; }

	// Semantic
	public required string Error { get; init; }
	public required string OnError { get; init; }
	public required string Warning { get; init; }
	public required string OnWarning { get; init; }
	public required string Success { get; init; }
	public required string OnSuccess { get; init; }
	public required string Info { get; init; }
	public required string OnInfo { get; init; }

	// Semantic dim fills - low-alpha backgrounds for chips, pills, soft badges
	public string ErrorDim { get; init; } = "rgba(239, 83, 80, 0.12)";
	public string WarningDim { get; init; } = "rgba(255, 171, 64, 0.12)";
	public string SuccessDim { get; init; } = "rgba(0, 230, 118, 0.15)";
	public string InfoDim { get; init; } = "rgba(66, 165, 245, 0.15)";

	// Borders & outlines
	public required string Outline { get; init; }
	public required string OutlineVariant { get; init; }

	// Extended surface scale (matrix dark theme)
	public string SurfaceHover { get; init; } = "#2a2a2a";
	public string Surface2 { get; init; } = "#1a1a1a";
	public string Surface3 { get; init; } = "#242424";

	// Accent glow tokens (used for focus rings, selected states)
	public string PrimaryGlow { get; init; } = "rgba(239, 83, 80, 0.08)";
	public string PrimaryGlowMd { get; init; } = "rgba(239, 83, 80, 0.15)";
	public string PrimaryGlowStrong { get; init; } = "rgba(239, 83, 80, 0.25)";
	public string PrimaryBorder { get; init; } = "rgba(239, 83, 80, 0.20)";
	public string PrimaryBorderDim { get; init; } = "rgba(239, 83, 80, 0.08)";
	public string PrimaryGlowFaint { get; init; } = "rgba(239, 83, 80, 0.03)";

	// Text scale (tertiary/quaternary)
	public string OnSurfaceTertiary { get; init; } = "#888888";
	public string OnSurfaceQuaternary { get; init; } = "#555555";

	// The colors below start as references to other tokens, so a palette that does not set them follows its
	// own colors whichever scheme it is. They carry the Gate look's extra shades, and a Matrix theme that
	// keeps these values renders as it did before they existed.

	/// <summary>Row hover and floating bars.</summary>
	public string SurfaceRaised { get; init; } = "var(--moka-color-surface-hover)";

	/// <summary>Neutral pills and the selected segment.</summary>
	public string NeutralBg { get; init; } = "var(--moka-color-surface-3)";

	/// <summary>Borders that must show, such as buttons and key caps.</summary>
	public string OutlineStrong { get; init; } = "var(--moka-color-outline)";

	/// <summary>A border under the pointer.</summary>
	public string OutlineHover { get; init; } = "var(--moka-color-outline)";

	/// <summary>Behind modal overlays.</summary>
	public string Backdrop { get; init; } = "color-mix(in srgb, var(--moka-color-background) 85%, transparent)";

	/// <summary>Text a step quieter than <see cref="OnSurface" />, such as field labels.</summary>
	public string OnSurface2 { get; init; } = "var(--moka-color-on-surface)";

	/// <summary>Icons that are not the point of what they sit in.</summary>
	public string Icon { get; init; } = "var(--moka-color-on-surface-variant)";

	/// <summary>What hover states mix toward for more contrast.</summary>
	public string Lift { get; init; } = "var(--moka-color-on-surface)";

	/// <summary>The thumb of a switch.</summary>
	public string Knob { get; init; } = "#ffffff";

	/// <summary>The shadow under the thumb of a switch.</summary>
	public string KnobShadow { get; init; } = "0 1px 2px rgba(0, 0, 0, 0.2)";

	/// <summary>What must stay light in every theme, such as a QR code's background.</summary>
	public string Paper { get; init; } = "#ffffff";

	/// <summary>The edge of <see cref="Paper" />, where it could not otherwise be told from its surface.</summary>
	public string PaperEdge { get; init; } = "transparent";

	/// <summary>The tint behind something selected.</summary>
	public string PrimaryBg { get; init; } = "var(--moka-color-primary-glow-md)";

	/// <summary>The active row.</summary>
	public string PrimaryRow { get; init; } = "var(--moka-color-primary-glow)";

	/// <summary>A ring in the accent, around the active row.</summary>
	public string PrimaryRing { get; init; } = "var(--moka-color-primary-border)";

	/// <summary>A pill in the accent.</summary>
	public string PrimaryPill { get; init; } = "var(--moka-color-primary-glow-md)";

	/// <summary>The focus ring's color.</summary>
	public string PrimaryFocus { get; init; } = "var(--moka-color-primary-glow)";

	/// <summary>Text on <see cref="PrimaryBg" />.</summary>
	public string PrimaryText { get; init; } = "var(--moka-color-primary-light)";

	/// <summary>The tint behind something that succeeded.</summary>
	public string SuccessBg { get; init; } = "var(--moka-color-success-dim)";

	/// <summary>Short text on <see cref="SuccessBg" />.</summary>
	public string SuccessInk { get; init; } = "var(--moka-color-success)";

	/// <summary>Longer text on <see cref="SuccessBg" />.</summary>
	public string SuccessText { get; init; } = "var(--moka-color-on-surface)";

	/// <summary>A border on <see cref="SuccessBg" />.</summary>
	public string SuccessLine { get; init; } = "color-mix(in srgb, var(--moka-color-success) 30%, transparent)";

	/// <summary>The tint behind a warning.</summary>
	public string WarningBg { get; init; } = "var(--moka-color-warning-dim)";

	/// <summary>Short text on <see cref="WarningBg" />.</summary>
	public string WarningInk { get; init; } = "var(--moka-color-warning)";

	/// <summary>Longer text on <see cref="WarningBg" />.</summary>
	public string WarningText { get; init; } = "var(--moka-color-on-surface)";

	/// <summary>A border on <see cref="WarningBg" />.</summary>
	public string WarningLine { get; init; } = "color-mix(in srgb, var(--moka-color-warning) 30%, transparent)";

	/// <summary>The tint behind an error.</summary>
	public string ErrorBg { get; init; } = "var(--moka-color-error-dim)";

	/// <summary>Short text on <see cref="ErrorBg" />.</summary>
	public string ErrorInk { get; init; } = "var(--moka-color-error)";

	/// <summary>Longer text on <see cref="ErrorBg" />.</summary>
	public string ErrorText { get; init; } = "var(--moka-color-on-surface)";

	/// <summary>A border on <see cref="ErrorBg" />.</summary>
	public string ErrorLine { get; init; } = "color-mix(in srgb, var(--moka-color-error) 30%, transparent)";

	/// <summary>Counts, danger links and field errors, which read as text on a surface.</summary>
	public string ErrorSoft { get; init; } = "var(--moka-color-error)";

	/// <summary>The tint behind information.</summary>
	public string InfoBg { get; init; } = "var(--moka-color-info-dim)";

	/// <summary>Short text on <see cref="InfoBg" />.</summary>
	public string InfoInk { get; init; } = "var(--moka-color-info)";

	/// <summary>Longer text on <see cref="InfoBg" />.</summary>
	public string InfoText { get; init; } = "var(--moka-color-on-surface)";

	/// <summary>A border on <see cref="InfoBg" />.</summary>
	public string InfoLine { get; init; } = "color-mix(in srgb, var(--moka-color-info) 30%, transparent)";

	/// <summary>Default light palette with Moka Red (#d32f2f) as primary.</summary>
	public static MokaPalette Light { get; } = new()
	{
		Primary = "#d32f2f",
		PrimaryLight = "#ff6659",
		PrimaryDark = "#9a0007",
		OnPrimary = "#ffffff",

		Secondary = "#455a64",
		SecondaryLight = "#718792",
		SecondaryDark = "#1c313a",
		OnSecondary = "#ffffff",

		Surface = "#ffffff",
		SurfaceVariant = "#f5f5f5",
		OnSurface = "#1c1b1f",
		OnSurfaceVariant = "#666666",
		Background = "#fafafa",
		OnBackground = "#1c1b1f",

		Error = "#b00020",
		OnError = "#ffffff",
		Warning = "#f57c00",
		OnWarning = "#ffffff",
		Success = "#2e7d32",
		OnSuccess = "#ffffff",
		Info = "#0288d1",
		OnInfo = "#ffffff",

		ErrorDim = "rgba(176, 0, 32, 0.10)",
		WarningDim = "rgba(245, 124, 0, 0.12)",
		SuccessDim = "rgba(46, 125, 50, 0.12)",
		InfoDim = "rgba(2, 136, 209, 0.12)",

		Outline = "#c4c4c4",
		OutlineVariant = "#e0e0e0",

		SurfaceHover = "#f0f0f0",
		Surface2 = "#f8f8f8",
		Surface3 = "#eeeeee",
		PrimaryGlow = "rgba(211, 47, 47, 0.08)",
		PrimaryGlowMd = "rgba(211, 47, 47, 0.15)",
		PrimaryGlowStrong = "rgba(211, 47, 47, 0.25)",
		PrimaryBorder = "rgba(211, 47, 47, 0.20)",
		PrimaryBorderDim = "rgba(211, 47, 47, 0.08)",
		PrimaryGlowFaint = "rgba(211, 47, 47, 0.03)",
		OnSurfaceTertiary = "#888888",
		OnSurfaceQuaternary = "#bbbbbb"
	};

	/// <summary>Default dark palette - Moka matrix/dark aesthetic. Near-black surfaces, red accent, red-tinted borders.</summary>
	public static MokaPalette Dark { get; } = new()
	{
		Primary = "#ef5350",
		PrimaryLight = "#ff6b68",
		PrimaryDark = "#c62828",
		OnPrimary = "#ffffff",

		Secondary = "#a0a0aa",
		SecondaryLight = "#e8e8ec",
		SecondaryDark = "#6a6a74",
		OnSecondary = "#060608",

		Surface = "#0c0c10",
		SurfaceVariant = "#14141a",
		OnSurface = "#e8e8ec",
		OnSurfaceVariant = "#a0a0aa",
		Background = "#060608",
		OnBackground = "#e8e8ec",

		Error = "#ef5350",
		OnError = "#ffffff",
		Warning = "#ffab40",
		OnWarning = "#060608",
		Success = "#00e676",
		OnSuccess = "#060608",
		Info = "#42a5f5",
		OnInfo = "#060608",

		ErrorDim = "rgba(239, 83, 80, 0.12)",
		WarningDim = "rgba(255, 171, 64, 0.12)",
		SuccessDim = "rgba(0, 230, 118, 0.15)",
		InfoDim = "rgba(66, 165, 245, 0.15)",

		Outline = "rgba(239, 83, 80, 0.12)",
		OutlineVariant = "rgba(239, 83, 80, 0.06)",

		SurfaceHover = "#14141a",
		Surface2 = "#101015",
		Surface3 = "#1a1a22",
		PrimaryGlow = "rgba(239, 83, 80, 0.08)",
		PrimaryGlowMd = "rgba(239, 83, 80, 0.15)",
		PrimaryGlowStrong = "rgba(239, 83, 80, 0.25)",
		PrimaryBorder = "rgba(239, 83, 80, 0.20)",
		PrimaryBorderDim = "rgba(239, 83, 80, 0.08)",
		PrimaryGlowFaint = "rgba(239, 83, 80, 0.03)",
		OnSurfaceTertiary = "#6a6a74",
		OnSurfaceQuaternary = "#40404a"
	};
}
