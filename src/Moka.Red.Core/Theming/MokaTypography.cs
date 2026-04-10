namespace Moka.Red.Core.Theming;

/// <summary>
///     Typography scale definition. Defaults are dense/compact — smaller than typical UI libraries.
///     All values are CSS size strings.
/// </summary>
public sealed record MokaTypography
{
	/// <summary>Font family stack. Inter preferred, falls back to system fonts.</summary>
	public string FontFamily { get; init; } =
		"'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif";

	/// <summary>Monospace font family for code, data, hashes, IDs.</summary>
	public string FontFamilyMono { get; init; } =
		"'JetBrains Mono', 'Cascadia Code', 'SF Mono', Menlo, monospace";

	// Font sizes — dense defaults (13px base)
	public string FontSizeXs { get; init; } = "0.6875rem"; // 11px
	public string FontSizeSm { get; init; } = "0.75rem"; // 12px
	public string FontSizeBase { get; init; } = "0.8125rem"; // 13px
	public string FontSizeMd { get; init; } = "0.875rem"; // 14px
	public string FontSizeLg { get; init; } = "1rem"; // 16px
	public string FontSizeXl { get; init; } = "1.25rem"; // 20px
	public string FontSizeXxl { get; init; } = "1.5rem"; // 24px

	// Line heights
	public string LineHeightTight { get; init; } = "1.2";
	public string LineHeightBase { get; init; } = "1.4";
	public string LineHeightRelaxed { get; init; } = "1.6";

	// Font weights
	public string FontWeightLight { get; init; } = "300";
	public string FontWeightNormal { get; init; } = "400";
	public string FontWeightMedium { get; init; } = "500";
	public string FontWeightSemibold { get; init; } = "600";
	public string FontWeightBold { get; init; } = "700";

	/// <summary>Default dense typography scale.</summary>
	public static MokaTypography Default => new();
}
