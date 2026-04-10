namespace Moka.Red.Core.Theming;

/// <summary>
///     Color palette definition for a Moka.Red theme.
///     All values are CSS color strings (hex, rgb, hsl, etc.).
/// </summary>
public sealed record MokaPalette
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

	// Text scale (tertiary/quaternary)
	public string OnSurfaceTertiary { get; init; } = "#888888";
	public string OnSurfaceQuaternary { get; init; } = "#555555";

	/// <summary>Default light palette with Moka Red (#d32f2f) as primary.</summary>
	public static MokaPalette Light => new()
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
		OnSurfaceTertiary = "#888888",
		OnSurfaceQuaternary = "#bbbbbb"
	};

	/// <summary>Default dark palette — Moka matrix/dark aesthetic. Near-black surfaces, red accent, red-tinted borders.</summary>
	public static MokaPalette Dark => new()
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
		OnSurfaceTertiary = "#6a6a74",
		OnSurfaceQuaternary = "#40404a"
	};
}
