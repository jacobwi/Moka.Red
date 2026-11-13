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

	/// <summary>
	///     Font family for big numbers, such as a stat's value or a gauge's reading. The title font unless
	///     <see cref="MokaTheme.WithNumberFont" /> gives them their own.
	/// </summary>
	public string FontFamilyNumber { get; init; } = "var(--moka-font-family-display)";

	/// <summary>
	///     Font family for numbers in small places, such as the value in a stat's fact tile. The text font unless
	///     <see cref="MokaTheme.WithNumberFont" /> gives numbers their own.
	/// </summary>
	public string FontFamilyFigure { get; init; } = "var(--moka-font-family)";

	/// <summary>The smallest text size (<c>--moka-font-size-xxs</c>), 10.5 px by default.</summary>
	public string FontSizeXxs { get; init; } = "0.65625rem";

	/// <summary>Text size <c>--moka-font-size-xs</c>, 11 px by default, for captions and micro labels.</summary>
	public string FontSizeXs { get; init; } = "0.6875rem";

	/// <summary>Text size <c>--moka-font-size-sm</c>, 12 px by default, for helper text and small controls.</summary>
	public string FontSizeSm { get; init; } = "0.75rem";

	/// <summary>The body text size (<c>--moka-font-size-base</c>), 13 px by default.</summary>
	public string FontSizeBase { get; init; } = "0.8125rem";

	/// <summary>Text size <c>--moka-font-size-md</c>, 14 px by default.</summary>
	public string FontSizeMd { get; init; } = "0.875rem";

	/// <summary>Text size <c>--moka-font-size-lg</c>, 16 px by default.</summary>
	public string FontSizeLg { get; init; } = "1rem";

	/// <summary>Text size <c>--moka-font-size-xl</c>, 20 px by default.</summary>
	public string FontSizeXl { get; init; } = "1.25rem";

	/// <summary>Text size <c>--moka-font-size-xxl</c>, 24 px by default.</summary>
	public string FontSizeXxl { get; init; } = "1.5rem";

	/// <summary>The largest text size (<c>--moka-font-size-display</c>), 30 px by default, for big numbers and hero titles.</summary>
	public string FontSizeDisplay { get; init; } = "1.875rem";

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

	/// <summary>The line height of headings and single-line controls (<c>--moka-line-height-tight</c>).</summary>
	public string LineHeightTight { get; init; } = "1.2";

	/// <summary>The line height of body text (<c>--moka-line-height-base</c>).</summary>
	public string LineHeightBase { get; init; } = "1.4";

	/// <summary>The line height of long passages (<c>--moka-line-height-relaxed</c>).</summary>
	public string LineHeightRelaxed { get; init; } = "1.6";

	/// <summary>The light weight (<c>--moka-font-weight-light</c>), <see cref="Enums.MokaFontWeight.Light" />.</summary>
	public string FontWeightLight { get; init; } = "300";

	/// <summary>The regular weight (<c>--moka-font-weight-normal</c>), <see cref="Enums.MokaFontWeight.Normal" />.</summary>
	public string FontWeightNormal { get; init; } = "400";

	/// <summary>The medium weight (<c>--moka-font-weight-medium</c>), <see cref="Enums.MokaFontWeight.Medium" />.</summary>
	public string FontWeightMedium { get; init; } = "500";

	/// <summary>The semibold weight (<c>--moka-font-weight-semibold</c>), <see cref="Enums.MokaFontWeight.Semibold" />.</summary>
	public string FontWeightSemibold { get; init; } = "600";

	/// <summary>The bold weight (<c>--moka-font-weight-bold</c>), <see cref="Enums.MokaFontWeight.Bold" />.</summary>
	public string FontWeightBold { get; init; } = "700";

	/// <summary>The weight of headings at levels 1 to 3.</summary>
	public string FontWeightHeading { get; init; } = "var(--moka-font-weight-bold)";

	/// <summary>The <c>text-transform</c> of buttons, chips and tags.</summary>
	public string ControlCase { get; init; } = "uppercase";

	/// <summary>The <c>letter-spacing</c> of buttons, chips and tags.</summary>
	public string ControlTracking { get; init; } = "0.08em";

	/// <summary>
	///     The <c>text-transform</c> of page, dialog, drawer and sheet titles, and of card titles unless
	///     <see cref="CardTitleCase" /> says otherwise.
	/// </summary>
	public string TitleCase { get; init; } = "uppercase";

	/// <summary>
	///     The <c>letter-spacing</c> of dialog, drawer and sheet titles, and of card titles unless
	///     <see cref="CardTitleTracking" /> says otherwise.
	/// </summary>
	public string TitleTracking { get; init; } = "0.08em";

	/// <summary>The <c>text-transform</c> of card, panel and chart titles. The other titles' unless set.</summary>
	public string CardTitleCase { get; init; } = "var(--moka-title-case)";

	/// <summary>The <c>letter-spacing</c> of card and panel titles. The other titles' unless set.</summary>
	public string CardTitleTracking { get; init; } = "var(--moka-title-tracking)";

	/// <summary>The <c>letter-spacing</c> of a chart's title. The page titles' unless set.</summary>
	public string ChartTitleTracking { get; init; } = "var(--moka-heading-tracking)";

	/// <summary>The <c>letter-spacing</c> of page titles and big numbers.</summary>
	public string HeadingTracking { get; init; } = "var(--moka-title-tracking)";

	/// <summary>The <c>letter-spacing</c> of big numbers. The titles' unless <see cref="MokaTheme.WithNumberFont" /> sets it.</summary>
	public string NumberTracking { get; init; } = "var(--moka-heading-tracking)";

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

	/// <summary>
	///     The type of <see cref="MokaTheme.Ember" />: <see cref="Gate" /> with Inter titles at
	///     <see cref="MokaTitleSize.Compact" /> and big numbers in JetBrains Mono.
	/// </summary>
	public static MokaTypography Ember { get; } = Gate
		.WithTitleFont(MokaTitleFont.Inter)
		.WithTitleSize(MokaTitleSize.Compact)
		.WithNumberFont(MokaNumberFont.Mono);

	// 17 px is a dialog title's size at normal text. Fixed rather than read from the dialog's token, so a theme that
	// changes dialog titles leaves page titles alone.
	internal MokaTypography WithTitleSize(MokaTitleSize size) => this with
	{
		FontSizeHeading = size == MokaTitleSize.Compact ? "1.0625rem" : "var(--moka-font-size-display)"
	};

	// Monospace digits are wide already, so they lose the titles' tight letter-spacing.
	internal MokaTypography WithNumberFont(MokaNumberFont font) => font == MokaNumberFont.Mono
		? this with
		{
			FontFamilyNumber = "var(--moka-font-family-mono)",
			FontFamilyFigure = "var(--moka-font-family-mono)",
			NumberTracking = "0"
		}
		: this with
		{
			FontFamilyNumber = "var(--moka-font-family-display)",
			FontFamilyFigure = "var(--moka-font-family)",
			NumberTracking = "var(--moka-heading-tracking)"
		};

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

	// Controls is Sentence for every title but the card ones, which take Uppercase's case, spacing and size. A chart's
	// title follows the page titles' spacing in the other two, so it needs its own token to follow the cards here.
	internal MokaTypography WithLetterCase(MokaLetterCase letterCase) => letterCase switch
	{
		MokaLetterCase.Uppercase => this with
		{
			ControlCase = "uppercase",
			ControlTracking = "0.08em",
			TitleCase = "uppercase",
			TitleTracking = "0.08em",
			HeadingTracking = "var(--moka-title-tracking)",
			CardTitleCase = "var(--moka-title-case)",
			CardTitleTracking = "var(--moka-title-tracking)",
			ChartTitleTracking = "var(--moka-heading-tracking)",
			FontSizeCardTitle = "var(--moka-font-size-xs)"
		},
		MokaLetterCase.Controls => this with
		{
			ControlCase = "uppercase",
			ControlTracking = "0.08em",
			TitleCase = "none",
			TitleTracking = "0",
			HeadingTracking = "-0.02em",
			CardTitleCase = "uppercase",
			CardTitleTracking = "0.08em",
			ChartTitleTracking = "var(--moka-card-title-tracking)",
			FontSizeCardTitle = "var(--moka-font-size-xs)"
		},
		_ => this with
		{
			ControlCase = "none",
			ControlTracking = "0",
			TitleCase = "none",
			TitleTracking = "0",
			HeadingTracking = "-0.02em",
			CardTitleCase = "var(--moka-title-case)",
			CardTitleTracking = "var(--moka-title-tracking)",
			ChartTitleTracking = "var(--moka-heading-tracking)",
			FontSizeCardTitle = "var(--moka-font-size-lg)"
		}
	};
}
