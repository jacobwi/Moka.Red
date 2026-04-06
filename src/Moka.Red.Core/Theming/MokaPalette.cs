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
		OutlineVariant = "#e0e0e0"
	};

	/// <summary>Default dark palette.</summary>
	public static MokaPalette Dark => new()
	{
		Primary = "#ef5350",
		PrimaryLight = "#ff867c",
		PrimaryDark = "#b61827",
		OnPrimary = "#ffffff",

		Secondary = "#78909c",
		SecondaryLight = "#a7c0cd",
		SecondaryDark = "#4b636e",
		OnSecondary = "#000000",

		Surface = "#1e1e1e",
		SurfaceVariant = "#2d2d2d",
		OnSurface = "#e0e0e0",
		OnSurfaceVariant = "#999999",
		Background = "#121212",
		OnBackground = "#e0e0e0",

		Error = "#cf6679",
		OnError = "#000000",
		Warning = "#ffb74d",
		OnWarning = "#000000",
		Success = "#388e3c",
		OnSuccess = "#ffffff",
		Info = "#0288d1",
		OnInfo = "#ffffff",

		Outline = "#444444",
		OutlineVariant = "#333333"
	};
}
