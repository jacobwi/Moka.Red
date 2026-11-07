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

	/// <summary>The size of a level 1 heading.</summary>
	public string FontSizeHeading { get; init; } = "var(--moka-font-size-xxl)";

	/// <summary>The size of a card's title.</summary>
	public string FontSizeCardTitle { get; init; } = "var(--moka-font-size-lg)";

	/// <summary>The size of a dialog's title.</summary>
	public string FontSizeDialog { get; init; } = "var(--moka-font-size-base)";

	/// <summary>The size of a row in a side menu.</summary>
	public string FontSizeMenu { get; init; } = "var(--moka-font-size-sm)";

	/// <summary>The size of an address or a key among other text.</summary>
	public string FontSizeMono { get; init; } = "var(--moka-font-size-xs)";

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

	/// <summary>The weight of headings at levels 1 to 3.</summary>
	public string FontWeightHeading { get; init; } = "var(--moka-font-weight-bold)";

	/// <summary>The <c>text-transform</c> of buttons, chips and tags.</summary>
	public string ControlCase { get; init; } = "uppercase";

	/// <summary>The <c>letter-spacing</c> of buttons, chips and tags.</summary>
	public string ControlTracking { get; init; } = "0.08em";

	/// <summary>The <c>text-transform</c> of card and dialog titles.</summary>
	public string TitleCase { get; init; } = "uppercase";

	/// <summary>The <c>letter-spacing</c> of card and dialog titles.</summary>
	public string TitleTracking { get; init; } = "0.08em";

	/// <summary>The <c>letter-spacing</c> of page titles and big numbers.</summary>
	public string HeadingTracking { get; init; } = "var(--moka-title-tracking)";

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
		FontSizeHeading = "var(--moka-font-size-display)",
		FontSizeDialog = "1.0625rem", // 17px
		FontSizeMenu = "0.84375rem", // 13.5px
		FontSizeMono = "0.78125rem", // 12.5px
		FontWeightHeading = "var(--moka-font-weight-semibold)",
		ControlCase = "none",
		ControlTracking = "0",
		TitleCase = "none",
		TitleTracking = "0",
		HeadingTracking = "-0.02em"
	};

	/// <summary>
	///     The type of <see cref="MokaTheme.Crisp" />: <see cref="Gate" /> at <see cref="MokaTextSize.Small" />,
	///     with Inter for titles.
	/// </summary>
	public static MokaTypography Crisp { get; } = Gate.WithGateTextSize(MokaTextSize.Small).WithTitleFont(MokaTitleFont.Inter);

	// Components read the same scale steps at either size, so swapping the steps is all it takes.
	internal MokaTypography WithGateTextSize(MokaTextSize size) => size == MokaTextSize.Small
		? this with
		{
			FontSizeXxs = "0.625rem", // 10px
			FontSizeXs = "0.625rem", // 10px
			FontSizeSm = "0.6875rem", // 11px
			FontSizeBase = "0.75rem", // 12px
			FontSizeMd = "0.75rem", // 12px
			FontSizeLg = "0.875rem", // 14px
			FontSizeXl = "1rem", // 16px
			FontSizeXxl = "1.125rem", // 18px
			FontSizeDisplay = "1.25rem", // 20px
			FontSizeRoot = "0.8125rem", // 13px
			FontSizeDialog = "0.875rem", // 14px
			FontSizeMenu = "0.78125rem", // 12.5px
			FontSizeMono = "0.6875rem" // 11px
		}
		: this with
		{
			FontSizeXxs = Default.FontSizeXxs,
			FontSizeXs = Default.FontSizeXs,
			FontSizeSm = Default.FontSizeSm,
			FontSizeBase = Default.FontSizeBase,
			FontSizeMd = Default.FontSizeMd,
			FontSizeLg = Default.FontSizeLg,
			FontSizeXl = Default.FontSizeXl,
			FontSizeXxl = Default.FontSizeXxl,
			FontSizeDisplay = Default.FontSizeDisplay,
			FontSizeRoot = "var(--moka-font-size-md)",
			FontSizeDialog = "1.0625rem", // 17px
			FontSizeMenu = "0.84375rem", // 13.5px
			FontSizeMono = "0.78125rem" // 12.5px
		};

	internal MokaTypography WithTitleFont(MokaTitleFont font) => this with
	{
		FontFamilyDisplay = font switch
		{
			MokaTitleFont.Inter => "var(--moka-font-family)",
			MokaTitleFont.JetBrainsMono => "var(--moka-font-family-mono)",
			_ => "'Space Grotesk', 'Inter', system-ui, sans-serif"
		}
	};

	internal MokaTypography WithLetterCase(MokaLetterCase letterCase) => letterCase == MokaLetterCase.Uppercase
		? this with
		{
			ControlCase = "uppercase",
			ControlTracking = "0.08em",
			TitleCase = "uppercase",
			TitleTracking = "0.08em",
			HeadingTracking = "var(--moka-title-tracking)",
			FontSizeCardTitle = "var(--moka-font-size-xs)"
		}
		: this with
		{
			ControlCase = "none",
			ControlTracking = "0",
			TitleCase = "none",
			TitleTracking = "0",
			HeadingTracking = "-0.02em",
			FontSizeCardTitle = "var(--moka-font-size-lg)"
		};
}
