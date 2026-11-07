namespace Moka.Red.Core.Theming;

/// <summary>
///     Typography scale definition. Defaults are dense/compact - smaller than typical UI libraries.
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

	/// <summary>Font family for page titles and big numbers. The body font unless a theme gives it its own.</summary>
	public string FontFamilyDisplay { get; init; } = "var(--moka-font-family)";

	// Font sizes - dense defaults (13px base)
	public string FontSizeXxs { get; init; } = "0.65625rem"; // 10.5px
	public string FontSizeXs { get; init; } = "0.6875rem"; // 11px
	public string FontSizeSm { get; init; } = "0.75rem"; // 12px
	public string FontSizeBase { get; init; } = "0.8125rem"; // 13px
	public string FontSizeMd { get; init; } = "0.875rem"; // 14px
	public string FontSizeLg { get; init; } = "1rem"; // 16px
	public string FontSizeXl { get; init; } = "1.25rem"; // 20px
	public string FontSizeXxl { get; init; } = "1.5rem"; // 24px
	public string FontSizeDisplay { get; init; } = "1.875rem"; // 30px

	/// <summary>The size text gets when nothing else sets one. Usually reads another size token.</summary>
	public string FontSizeRoot { get; init; } = "var(--moka-font-size-base)";

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

	/// <summary>The <c>text-transform</c> of buttons, chips and tags.</summary>
	public string ControlCase { get; init; } = "uppercase";

	/// <summary>The <c>letter-spacing</c> of buttons, chips and tags.</summary>
	public string ControlTracking { get; init; } = "0.08em";

	/// <summary>The <c>text-transform</c> of card and dialog titles.</summary>
	public string TitleCase { get; init; } = "uppercase";

	/// <summary>The <c>letter-spacing</c> of card and dialog titles.</summary>
	public string TitleTracking { get; init; } = "0.08em";

	/// <summary>Default dense typography scale.</summary>
	public static MokaTypography Default { get; } = new();

	/// <summary>
	///     The <see cref="MokaLook.Gate" /> look's type: Inter, Space Grotesk for page titles and big numbers,
	///     JetBrains Mono for data, and sentence case everywhere but micro labels.
	/// </summary>
	public static MokaTypography Gate { get; } = new()
	{
		FontFamily = "'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif",
		FontFamilyMono = "'JetBrains Mono', ui-monospace, Menlo, monospace",
		FontFamilyDisplay = "'Space Grotesk', 'Inter', system-ui, sans-serif",
		FontSizeRoot = "var(--moka-font-size-md)",
		ControlCase = "none",
		ControlTracking = "0",
		TitleCase = "none",
		TitleTracking = "0"
	};
}
