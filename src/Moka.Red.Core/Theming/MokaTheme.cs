using Moka.Red.Core.Colors;
using Moka.Red.Core.Utilities;
using Moka.Red.Data.Table;

namespace Moka.Red.Core.Theming;

/// <summary>
///     Complete theme definition composing palette, typography, and spacing.
///     Use <see cref="ToCssVariables" /> to generate CSS custom property declarations.
/// </summary>
public sealed record MokaTheme
{
	// The accent's alpha in the original dark design's borders, so WithAccent re-tints them as MokaPalette.Dark has them.
	private const double MatrixDarkOutline = 0.12;
	private const double MatrixDarkOutlineVariant = 0.06;

	/// <summary>The theme's colors, written as the <c>--moka-color-*</c> tokens.</summary>
	public required MokaPalette Palette { get; init; }

	/// <summary>The theme's fonts, text sizes, weights and line heights, written as the <c>--moka-font-*</c> and <c>--moka-line-height-*</c> tokens.</summary>
	public MokaTypography Typography { get; init; } = MokaTypography.Default;

	/// <summary>The theme's spacing scale, corners and layout gaps, written as the <c>--moka-spacing-*</c>, <c>--moka-radius-*</c> and layout tokens.</summary>
	public MokaSpacing Spacing { get; init; } = MokaSpacing.Default;

	/// <summary>Whether the theme is dark. <see cref="MokaThemeProvider" /> adds the <c>moka-dark</c> class to its root when it is.</summary>
	public bool IsDark { get; init; }

	/// <summary>The design this theme belongs to. Decides shadows, the focus ring, motion and accent math.</summary>
	public MokaLook Look { get; init; } = MokaLook.Matrix;

	/// <summary>
	///     Stronger secondary text and borders. Set it through <see cref="WithHighContrast" />, which also swaps
	///     the colors. Only the <see cref="MokaLook.Gate" /> look has a high contrast palette.
	/// </summary>
	public bool HighContrast { get; init; }

	/// <summary>
	///     Which surface colors <see cref="WithDark" /> and <see cref="WithHighContrast" /> stay on. Set it
	///     through <see cref="WithSurfaces" />, which also swaps the colors. Only the
	///     <see cref="MokaLook.Gate" /> look reads it.
	/// </summary>
	public MokaSurfaces Surfaces { get; init; } = MokaSurfaces.BlueGrey;

	/// <summary>How selected and focused things are marked. Only the <see cref="MokaLook.Gate" /> look reads it.</summary>
	public MokaSelection Selection { get; init; } = MokaSelection.Ring;

	/// <summary>
	///     How the place you are in the app is marked, such as the current page in the rail. Only the
	///     <see cref="MokaLook.Gate" /> look reads it.
	/// </summary>
	public MokaCurrentPlace CurrentPlace { get; init; } = MokaCurrentPlace.Ring;

	/// <summary>
	///     How popups, menus, dialogs and other floating panels stand off the page, through the shadow tokens. Only the
	///     <see cref="MokaLook.Gate" /> look reads it.
	/// </summary>
	public MokaElevation Elevation { get; init; } = MokaElevation.Shadow;

	/// <summary>
	///     The color of text buttons left at the primary color. Only the <see cref="MokaLook.Gate" /> look reads it.
	/// </summary>
	public MokaTextButtonColor TextButtons { get; init; } = MokaTextButtonColor.Accent;

	/// <summary>How callouts and alerts are drawn. Only the <see cref="MokaLook.Gate" /> look reads it.</summary>
	public MokaCalloutLook Callouts { get; init; } = MokaCalloutLook.Standard;

	/// <summary>How a switch that is on is drawn. Only the <see cref="MokaLook.Gate" /> look reads it.</summary>
	public MokaSwitchLook Switches { get; init; } = MokaSwitchLook.Standard;

	/// <summary>
	///     How the header of a <c>MokaSettingsForm</c> is drawn. Only the <see cref="MokaLook.Gate" /> look reads it.
	/// </summary>
	public MokaSettingsHeaderLook SettingsHeaders { get; init; } = MokaSettingsHeaderLook.Standard;

	/// <summary>
	///     How status bars are drawn, for bars that don't set a look of their own. Only the <see cref="MokaLook.Gate" />
	///     look reads it.
	/// </summary>
	public MokaStatusBarLook StatusBar { get; init; } = MokaStatusBarLook.Standard;

	/// <summary>
	///     How dock layouts are drawn, for layouts that don't set a look of their own. Only the <see cref="MokaLook.Gate" />
	///     look reads it.
	/// </summary>
	public MokaDockLook Docks { get; init; } = MokaDockLook.Classic;

	/// <summary>
	///     How toasts are drawn, unless a toast host sets a look of its own. Only the <see cref="MokaLook.Gate" /> look
	///     reads it.
	/// </summary>
	public MokaToastLook Toasts { get; init; } = MokaToastLook.Standard;

	/// <summary>
	///     How several toasts sit together, unless a toast host sets its own. Only the <see cref="MokaLook.Gate" /> look
	///     reads it.
	/// </summary>
	public MokaToastStacking ToastStacking { get; init; } = MokaToastStacking.List;

	/// <summary>
	///     How notification lists are drawn, unless one sets a look of its own. Only the <see cref="MokaLook.Gate" /> look
	///     reads it.
	/// </summary>
	public MokaNotificationLook Notifications { get; init; } = MokaNotificationLook.Standard;

	/// <summary>
	///     How menus, popovers, hover cards and other floating panels are drawn. Only the <see cref="MokaLook.Gate" />
	///     look reads it.
	/// </summary>
	public MokaPopupLook Popups { get; init; } = MokaPopupLook.Standard;

	/// <summary>
	///     How the rows of lists that open from a field are drawn, unless a field sets a look of its own. Only the
	///     <see cref="MokaLook.Gate" /> look reads it.
	/// </summary>
	public MokaListLook Lists { get; init; } = MokaListLook.Standard;

	/// <summary>
	///     How dialogs are drawn, unless a dialog sets a look of its own. Only the <see cref="MokaLook.Gate" /> look
	///     reads it.
	/// </summary>
	public MokaDialogLook Dialogs { get; init; } = MokaDialogLook.Standard;

	/// <summary>
	///     How buttons are drawn, unless a button sets a look of its own. Only the <see cref="MokaLook.Gate" /> look
	///     reads it.
	/// </summary>
	public MokaButtonLook Buttons { get; init; } = MokaButtonLook.Standard;

	/// <summary>
	///     How disabled buttons are drawn, unless a button sets a disabled look of its own. Only the
	///     <see cref="MokaLook.Gate" /> look reads it.
	/// </summary>
	public MokaDisabledButtonLook DisabledButtons { get; init; } = MokaDisabledButtonLook.Faded;

	/// <summary>
	///     How tooltips are drawn, unless a tooltip sets a look of its own. Only the <see cref="MokaLook.Gate" /> look
	///     reads it.
	/// </summary>
	public MokaTooltipLook Tooltips { get; init; } = MokaTooltipLook.Standard;

	/// <summary>
	///     Whether tooltips draw an arrow pointing at their trigger, unless a tooltip says otherwise. Only the
	///     <see cref="MokaLook.Gate" /> look reads it.
	/// </summary>
	public bool TooltipArrows { get; init; }

	/// <summary>
	///     How tooltips come into view, unless a tooltip sets its own. Only the <see cref="MokaLook.Gate" /> look reads it.
	/// </summary>
	public MokaTooltipMotion TooltipMotion { get; init; } = MokaTooltipMotion.Fade;

	/// <summary>
	///     How much room a tooltip's text takes, unless a tooltip sets its own. Only the <see cref="MokaLook.Gate" /> look
	///     reads it.
	/// </summary>
	public MokaTooltipSize TooltipSize { get; init; } = MokaTooltipSize.Standard;

	/// <summary>
	///     How strong the label colors are (<c>--moka-label-*</c>, see <see cref="MokaLabelColors" />). Only the
	///     <see cref="MokaLook.Gate" /> look reads it: the original design draws labels <see cref="MokaLabelTone.Vivid" />.
	/// </summary>
	public MokaLabelTone LabelTone { get; init; } = MokaLabelTone.Vivid;

	/// <summary>
	///     How charts are drawn, for charts that don't set a look of their own. Only the <see cref="MokaLook.Gate" /> look
	///     reads it.
	/// </summary>
	public MokaChartLook Charts { get; init; } = MokaChartLook.Standard;

	/// <summary>The colors chart series take in turn. Only the <see cref="MokaLook.Gate" /> look reads it.</summary>
	public MokaChartPalette ChartPalette { get; init; } = MokaChartPalette.Standard;

	/// <summary>
	///     Whether chart marks are hatched on screen, for charts that don't say. Only the <see cref="MokaLook.Gate" /> look
	///     reads it.
	/// </summary>
	public MokaChartTexture ChartTexture { get; init; } = MokaChartTexture.None;

	/// <summary>
	///     How charts print and show under forced colors. Only the <see cref="MokaLook.Gate" /> look reads it.
	/// </summary>
	public MokaChartPrint ChartPrint { get; init; } = MokaChartPrint.AsShown;

	/// <summary>
	///     How tables draw their rows, for tables that don't set a look of their own. Null, the default, keeps the
	///     table's own, <see cref="MokaTableLook.Lined" />. Only the <see cref="MokaLook.Gate" /> look reads it.
	/// </summary>
	public MokaTableLook? Tables { get; init; }

	/// <summary>
	///     How tightly tables space their rows, for tables whose state and parameters don't say. Null, the default, keeps
	///     each look's own spacing. Only the <see cref="MokaLook.Gate" /> look reads it.
	/// </summary>
	public MokaTableDensity? TableDensity { get; init; }

	/// <summary>
	///     Whether text saying what a thing is for shows (<c>class="moka-about"</c>). Text saying what is so right
	///     now always shows. Only the <see cref="MokaLook.Gate" /> look reads it.
	/// </summary>
	public bool Descriptions { get; init; } = true;

	/// <summary>How a primary button is drawn. Only the <see cref="MokaLook.Gate" /> look reads it.</summary>
	public MokaMainButton MainButton { get; init; } = MokaMainButton.Solid;

	/// <summary>
	///     Density multiplier applied to the spacing scale. 1.0 is the default (cozy).
	///     0.75 gives compact layouts, 1.15 comfy. Radii and font sizes are not affected.
	/// </summary>
	public double Density { get; init; } = 1.0;

	/// <summary>
	///     Multiplier applied to every font-size token. 1.0 keeps the dense 13px base scale.
	/// </summary>
	public double FontScale { get; init; } = 1.0;

	/// <summary>
	///     Tokens held at values of their own (see <see cref="WithToken" />), by name, whatever the palette and style
	///     choices give them. They stay through every other <c>With</c> call, including a change of scheme or accent.
	/// </summary>
	public IReadOnlyDictionary<string, string> Pins => PinnedTokens;

	private MokaPinnedTokens PinnedTokens { get; init; } = MokaPinnedTokens.Empty;

	/// <summary>
	///     The colors this theme was derived from by <see cref="FromSeed" />, <see cref="WithSeed" /> or
	///     <see cref="WithNeutral" />, which <see cref="WithDark" />, <see cref="WithHighContrast" /> and
	///     <see cref="WithAccent(string)" /> derive it again from. Null for a theme with a palette of its own.
	/// </summary>
	public MokaThemeSeed? Seed { get; init; }

	/// <summary>
	///     The accent <see cref="WithAccent(string)" /> tinted this theme with, which <see cref="WithDark" /> puts back in the
	///     other scheme. Null for a theme that keeps its palette's own accent. On the <see cref="MokaLook.Matrix" /> look a
	///     color given as a string is kept as it is, so both of its schemes' shades are that color.
	/// </summary>
	public MokaAccent? Accent { get; init; }

	/// <summary>
	///     The palette as it was before <see cref="WithHighContrast" /> turned high contrast on, which turning it off gives
	///     back exactly. Null while high contrast is off, and on a theme from a seed, which is derived again instead.
	/// </summary>
	public MokaPalette? PaletteBeforeHighContrast { get; init; }

	/// <summary>
	///     The original design on light surfaces, exactly as it has always rendered. Uppercase, small and outlined,
	///     and the style choices such as <see cref="WithCorners" /> do nothing on it. For Matrix colors with the
	///     style choices, use <see cref="MatrixLight" />.
	/// </summary>
	public static MokaTheme Light { get; } = new()
	{
		Palette = MokaPalette.Light,
		IsDark = false
	};

	/// <summary>
	///     The original design on near-black surfaces, exactly as it has always rendered. Uppercase, small and
	///     outlined, and the style choices such as <see cref="WithCorners" /> do nothing on it. For Matrix colors
	///     with the style choices, use <see cref="Matrix" />.
	/// </summary>
	public static MokaTheme Dark { get; } = new()
	{
		Palette = MokaPalette.Dark,
		IsDark = true
	};

	/// <summary>
	///     Near-black surfaces, red-tinted borders and a red accent, on the design that takes every style choice:
	///     <see cref="WithDensity(MokaDensity)" />, <see cref="WithCorners" /> and the rest. <see cref="Dark" /> is
	///     the original design in these colors.
	/// </summary>
	public static MokaTheme Matrix { get; } = new()
	{
		Look = MokaLook.Gate,
		Surfaces = MokaSurfaces.NearBlack,
		Palette = MokaPalette.NearBlack,
		Typography = MokaTypography.Gate,
		Spacing = MokaSpacing.Gate,
		IsDark = true
	};

	/// <summary>The light version of <see cref="Matrix" />.</summary>
	public static MokaTheme MatrixLight { get; } = new()
	{
		Look = MokaLook.Gate,
		Surfaces = MokaSurfaces.NearBlack,
		Palette = MokaPalette.NearBlackLight,
		Typography = MokaTypography.Gate,
		Spacing = MokaSpacing.Gate,
		IsDark = false
	};

	/// <summary>
	///     Dark graphite with a red accent, the colors of <see cref="Default" />. Every style choice can be changed
	///     on it: <see cref="WithDensity(MokaDensity)" />, <see cref="WithTextSize" />, <see cref="WithCorners" />,
	///     <see cref="WithMainButton" />, <see cref="WithLetterCase" />, <see cref="WithTitleFont" />,
	///     <see cref="WithSelection" /> and <see cref="WithCurrentPlace" />.
	/// </summary>
	public static MokaTheme Red { get; } = new()
	{
		Look = MokaLook.Gate,
		Surfaces = MokaSurfaces.Graphite,
		Palette = MokaPalette.Graphite,
		Typography = MokaTypography.Gate,
		Spacing = MokaSpacing.Gate,
		IsDark = true
	};

	/// <summary>Light graphite with a red accent, the colors of <see cref="DefaultLight" />.</summary>
	public static MokaTheme RedLight { get; } = new()
	{
		Look = MokaLook.Gate,
		Surfaces = MokaSurfaces.Graphite,
		Palette = MokaPalette.GraphiteLight,
		Typography = MokaTypography.Gate,
		Spacing = MokaSpacing.Gate,
		IsDark = false
	};

	/// <summary>
	///     The colors of <see cref="Gate" /> with the style choices at their tightest: tight corners, compact
	///     density, small text, Inter titles and flat selection, for tools that need to show a lot at once. Each of
	///     those can still be changed.
	/// </summary>
	public static MokaTheme Crisp { get; } = new()
	{
		Look = MokaLook.Gate,
		Palette = MokaPalette.Gate,
		Typography = MokaTypography.Crisp,
		Spacing = MokaSpacing.Crisp,
		Selection = MokaSelection.Flat,
		IsDark = true
	};

	/// <summary>The light version of <see cref="Crisp" />.</summary>
	public static MokaTheme CrispLight { get; } = new()
	{
		Look = MokaLook.Gate,
		Palette = MokaPalette.GateLight,
		Typography = MokaTypography.Crisp,
		Spacing = MokaSpacing.Crisp,
		Selection = MokaSelection.Flat,
		IsDark = false
	};

	/// <summary>
	///     The near-black colors of <see cref="Matrix" /> with the Moka principles already chosen: glow rings for depth
	///     (<see cref="MokaElevation.Glow" />), quiet text buttons, monospace numbers, callouts with an edge, tight
	///     corners, compact Inter page titles, the current place filled with its glow inside, tinted switches and tinted
	///     settings headers. Each of those can still be changed.
	/// </summary>
	public static MokaTheme Ember { get; } = new()
	{
		Look = MokaLook.Gate,
		Surfaces = MokaSurfaces.NearBlack,
		Palette = MokaPalette.NearBlack,
		Typography = MokaTypography.Ember,
		Spacing = MokaSpacing.Ember,
		Elevation = MokaElevation.Glow,
		TextButtons = MokaTextButtonColor.Neutral,
		Callouts = MokaCalloutLook.Edge,
		CurrentPlace = MokaCurrentPlace.SolidInset,
		Switches = MokaSwitchLook.Tinted,
		SettingsHeaders = MokaSettingsHeaderLook.Tinted,
		IsDark = true
	};

	/// <summary>The light version of <see cref="Ember" />.</summary>
	public static MokaTheme EmberLight { get; } = Ember with
	{
		Palette = MokaPalette.NearBlackLight,
		IsDark = false
	};

	/// <summary>Dark blue-grey with a blue accent. Every style choice can be changed on it.</summary>
	public static MokaTheme Gate { get; } = new()
	{
		Look = MokaLook.Gate,
		Palette = MokaPalette.Gate,
		Typography = MokaTypography.Gate,
		Spacing = MokaSpacing.Gate,
		IsDark = true
	};

	/// <summary>Light blue-grey with a blue accent, the light version of <see cref="Gate" />.</summary>
	public static MokaTheme GateLight { get; } = new()
	{
		Look = MokaLook.Gate,
		Palette = MokaPalette.GateLight,
		Typography = MokaTypography.Gate,
		Spacing = MokaSpacing.Gate,
		IsDark = false
	};

	/// <summary>
	///     The dark theme used when nothing names one: <see cref="MokaThemeProvider" /> without a
	///     <c>Theme</c>, and <c>AddMokaRed()</c>. It is <see cref="Red" />.
	/// </summary>
	public static MokaTheme Default => Red;

	/// <summary>The light partner of <see cref="Default" />. It is <see cref="RedLight" />.</summary>
	public static MokaTheme DefaultLight => RedLight;

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

	/// <summary>
	///     Creates a new theme with other corners. It has no effect on the <see cref="MokaLook.Matrix" /> look, like
	///     the other style choices.
	/// </summary>
	public MokaTheme WithCorners(MokaCorners corners) =>
		Look == MokaLook.Gate ? this with { Spacing = Spacing.WithCorners(corners) } : this;

	/// <summary>
	///     Creates a new theme at another density, swapping the sizes of controls, cards and rows. It has no effect
	///     on the <see cref="MokaLook.Matrix" /> look, where <see cref="WithDensity(double)" /> scales the spacing.
	/// </summary>
	public MokaTheme WithDensity(MokaDensity density) => Look == MokaLook.Gate
		? this with { Density = 1.0, Spacing = Spacing.WithGateLayout(density == MokaDensity.Compact) }
		: this;

	/// <summary>Creates a new theme with larger or smaller text. No effect on the <see cref="MokaLook.Matrix" /> look.</summary>
	public MokaTheme WithTextSize(MokaTextSize size) =>
		Look == MokaLook.Gate ? this with { Typography = Typography.WithGateTextSize(size) } : this;

	/// <summary>Creates a new theme with another primary button. No effect on the <see cref="MokaLook.Matrix" /> look.</summary>
	public MokaTheme WithMainButton(MokaMainButton button) =>
		Look == MokaLook.Gate ? this with { MainButton = button } : this;

	/// <summary>
	///     Creates a new theme with another letter case: <see cref="MokaLetterCase.Controls" /> uppercases buttons and card
	///     titles while headings stay as written. No effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	public MokaTheme WithLetterCase(MokaLetterCase letterCase) =>
		Look == MokaLook.Gate ? this with { Typography = Typography.WithLetterCase(letterCase) } : this;

	/// <summary>Creates a new theme with another title font. No effect on the <see cref="MokaLook.Matrix" /> look.</summary>
	public MokaTheme WithTitleFont(MokaTitleFont font) =>
		Look == MokaLook.Gate ? this with { Typography = Typography.WithTitleFont(font) } : this;

	/// <summary>
	///     Creates a new theme that marks selected and focused things another way. No effect on the
	///     <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	public MokaTheme WithSelection(MokaSelection selection) =>
		Look == MokaLook.Gate ? this with { Selection = selection } : this;

	/// <summary>
	///     Creates a new theme that marks the place you are in the app another way: the current page in the rail and the
	///     side menu, and the current section of a settings list. Selected rows and picks keep
	///     <see cref="WithSelection" />. No effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	public MokaTheme WithCurrentPlace(MokaCurrentPlace place) =>
		Look == MokaLook.Gate ? this with { CurrentPlace = place } : this;

	/// <summary>
	///     Creates a new theme whose popups, menus, dialogs, floating bars and other floating panels stand off the page
	///     this way. It sets the floating shadow tokens (<c>--moka-shadow-3</c>, <c>-4</c>, <c>-popup</c>,
	///     <c>-popup-lg</c> and <c>-modal</c>), so every component that reads them follows; cards keep theirs. No
	///     effect on the <see cref="MokaLook.Matrix" /> look, which already draws glow rings on dark.
	/// </summary>
	/// <param name="elevation">How floating panels stand off the page.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithElevation(MokaElevation elevation) =>
		Look == MokaLook.Gate ? this with { Elevation = elevation } : this;

	/// <summary>
	///     Creates a new theme whose text buttons at the primary color (<c>MokaButton</c> and <c>MokaSplitButton</c> with
	///     the text variant) are drawn in this color. A text button given another <c>Color</c> keeps it. No effect on the
	///     <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="color">The color of primary text buttons.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithTextButtons(MokaTextButtonColor color) =>
		Look == MokaLook.Gate ? this with { TextButtons = color } : this;

	/// <summary>
	///     Creates a new theme whose big numbers, such as a stat's value and a gauge's reading, are drawn in this font. Page
	///     titles keep <see cref="WithTitleFont" />. No effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="font">The font of big numbers.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithNumberFont(MokaNumberFont font) =>
		Look == MokaLook.Gate ? this with { Typography = Typography.WithNumberFont(font) } : this;

	/// <summary>
	///     Creates a new theme whose page titles are this size: <c>MokaPageHeader</c>'s title, a level 1
	///     <c>MokaHeading</c> and a chart's large title. No effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="size">The size of page titles.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithTitleSize(MokaTitleSize size) =>
		Look == MokaLook.Gate ? this with { Typography = Typography.WithTitleSize(size) } : this;

	/// <summary>
	///     Creates a new theme whose callouts and alerts are drawn this way. An outlined alert stays outlined. No effect on
	///     the <see cref="MokaLook.Matrix" /> look, whose callouts already have an edge.
	/// </summary>
	/// <param name="look">How callouts and alerts are drawn.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithCallouts(MokaCalloutLook look) =>
		Look == MokaLook.Gate ? this with { Callouts = look } : this;

	/// <summary>
	///     Creates a new theme whose switches are drawn this way while on. A switch's <c>Color</c> still sets its tone. No
	///     effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="look">How a switch that is on is drawn.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithSwitches(MokaSwitchLook look) =>
		Look == MokaLook.Gate ? this with { Switches = look } : this;

	/// <summary>
	///     Creates a new theme whose <c>MokaSettingsForm</c> headers are drawn this way: the icon's tile, and where the
	///     line saying what's set now goes. No effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="look">How settings headers are drawn.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithSettingsHeaders(MokaSettingsHeaderLook look) =>
		Look == MokaLook.Gate ? this with { SettingsHeaders = look } : this;

	/// <summary>
	///     Creates a new theme whose status bars are drawn this way, unless a bar sets its own look. No effect on the
	///     <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="look">How status bars are drawn.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithStatusBar(MokaStatusBarLook look) =>
		Look == MokaLook.Gate ? this with { StatusBar = look } : this;

	/// <summary>
	///     Creates a new theme whose dock layouts are drawn this way, unless a layout sets its own look. No effect on the
	///     <see cref="MokaLook.Matrix" /> look, which draws <see cref="MokaDockLook.Classic" />.
	/// </summary>
	/// <param name="look">How dock layouts are drawn.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithDocks(MokaDockLook look) =>
		Look == MokaLook.Gate ? this with { Docks = look } : this;

	/// <summary>
	///     Creates a new theme whose charts are drawn this way, unless a chart sets its own look. No effect on the
	///     <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="look">How charts are drawn.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithCharts(MokaChartLook look) =>
		Look == MokaLook.Gate ? this with { Charts = look } : this;

	/// <summary>
	///     Creates a new theme whose chart series take these colors in turn, written as <c>--moka-chart-1</c> to
	///     <c>--moka-chart-8</c>. <see cref="MokaChartPalette.AccentFirst" /> leads with the accent: each accent preset has
	///     an order checked for color-blind readers, and any other accent leads the reference hues without the one nearest it,
	///     unchecked. No effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="palette">The colors.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithChartPalette(MokaChartPalette palette) =>
		Look == MokaLook.Gate ? this with { ChartPalette = palette } : this;

	/// <summary>
	///     Creates a new theme whose charts hatch their marks on screen, unless a chart says otherwise. No effect on the
	///     <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="texture">Whether marks are hatched.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithChartTexture(MokaChartTexture texture) =>
		Look == MokaLook.Gate ? this with { ChartTexture = texture } : this;

	/// <summary>
	///     Creates a new theme whose charts print, and show under forced colors, this way. No effect on the
	///     <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="print">How charts print.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithChartPrint(MokaChartPrint print) =>
		Look == MokaLook.Gate ? this with { ChartPrint = print } : this;

	/// <summary>
	///     Creates a new theme whose tables draw their rows this way, unless a table sets its own look. No effect on the
	///     <see cref="MokaLook.Matrix" /> look, where tables draw <see cref="MokaTableLook.Lined" />.
	/// </summary>
	/// <param name="look">How tables draw their rows.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithTables(MokaTableLook look) =>
		Look == MokaLook.Gate ? this with { Tables = look } : this;

	/// <summary>
	///     Creates a new theme whose tables space their rows this tightly, unless a table's state or parameters say
	///     otherwise. No effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="density">How tightly tables space their rows.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithTableDensity(MokaTableDensity density) =>
		Look == MokaLook.Gate ? this with { TableDensity = density } : this;

	/// <summary>
	///     Creates a new theme whose toasts are drawn this way, unless a toast host sets its own look. No effect on the
	///     <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="look">How toasts are drawn.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithToasts(MokaToastLook look) =>
		Look == MokaLook.Gate ? this with { Toasts = look } : this;

	/// <summary>
	///     Creates a new theme whose toasts sit together this way, unless a toast host sets its own. No effect on the
	///     <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="stacking">How several toasts sit together.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithToastStacking(MokaToastStacking stacking) =>
		Look == MokaLook.Gate ? this with { ToastStacking = stacking } : this;

	/// <summary>
	///     Creates a new theme whose notification lists are drawn this way, unless one sets its own look. No effect on the
	///     <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="look">How notification lists are drawn.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithNotifications(MokaNotificationLook look) =>
		Look == MokaLook.Gate ? this with { Notifications = look } : this;

	/// <summary>
	///     Creates a new theme whose menus, popovers, hover cards and other floating panels are drawn this way, unless a
	///     panel sets its own look. No effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="look">How floating panels are drawn.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithPopups(MokaPopupLook look) =>
		Look == MokaLook.Gate ? this with { Popups = look } : this;

	/// <summary>
	///     Creates a new theme whose lists that open from a field draw their rows this way, unless a field sets its own
	///     look. No effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="look">How the rows are drawn.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithLists(MokaListLook look) =>
		Look == MokaLook.Gate ? this with { Lists = look } : this;

	/// <summary>
	///     Creates a new theme whose dialogs are drawn this way, unless a dialog sets its own look. No effect on the
	///     <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="look">How dialogs are drawn.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithDialogs(MokaDialogLook look) =>
		Look == MokaLook.Gate ? this with { Dialogs = look } : this;

	/// <summary>
	///     Creates a new theme whose buttons are drawn this way, unless a button sets its own look. Only the outermost
	///     <see cref="MokaThemeProvider" /> sets it for the page. <see cref="WithLetterCase" /> still applies, and so do
	///     <see cref="WithCorners" /> and <see cref="WithMainButton" /> except where the look says otherwise. No effect on
	///     the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="look">How buttons are drawn.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithButtons(MokaButtonLook look) =>
		Look == MokaLook.Gate ? this with { Buttons = look } : this;

	/// <summary>
	///     Creates a new theme whose disabled buttons are drawn this way, whatever their <see cref="Buttons" /> look, unless
	///     a button sets its own. <see cref="MokaDisabledButtonLook.Muted" /> and <see cref="MokaDisabledButtonLook.Outline" />
	///     draw every disabled button in neutral colors, so a solid one doesn't read as a dim, broken version of itself. A
	///     busy button keeps its colors. Only the outermost <see cref="MokaThemeProvider" /> sets it for the page. No effect
	///     on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="look">How disabled buttons are drawn.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithDisabledButtons(MokaDisabledButtonLook look) =>
		Look == MokaLook.Gate ? this with { DisabledButtons = look } : this;

	/// <summary>
	///     Creates a new theme whose tooltips are drawn this way, unless a tooltip sets its own look. Chart tooltips take
	///     the look's colors, border, corners, shadow, blur and font too. No effect on the <see cref="MokaLook.Matrix" />
	///     look.
	/// </summary>
	/// <param name="look">How tooltips are drawn.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithTooltips(MokaTooltipLook look) =>
		Look == MokaLook.Gate ? this with { Tooltips = look } : this;

	/// <summary>
	///     Creates a new theme whose tooltips draw an arrow pointing at their trigger, or none, unless a tooltip says
	///     otherwise. The arrow follows the tooltip when the browser moves it to another side for room, and where the
	///     browser can't say which side that is, it isn't drawn. No effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="arrows">Whether tooltips draw an arrow.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithTooltipArrows(bool arrows = true) =>
		Look == MokaLook.Gate ? this with { TooltipArrows = arrows } : this;

	/// <summary>
	///     Creates a new theme whose tooltips come into view this way, unless a tooltip sets its own. With reduced motion
	///     asked for, <see cref="MokaTooltipMotion.Slide" /> and <see cref="MokaTooltipMotion.Grow" /> fade instead. No
	///     effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="motion">How tooltips come into view.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithTooltipMotion(MokaTooltipMotion motion) =>
		Look == MokaLook.Gate ? this with { TooltipMotion = motion } : this;

	/// <summary>
	///     Creates a new theme whose tooltips take this much room, unless a tooltip sets its own size. No effect on the
	///     <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="size">How much room a tooltip's text takes.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithTooltipSize(MokaTooltipSize size) =>
		Look == MokaLook.Gate ? this with { TooltipSize = size } : this;

	/// <summary>
	///     Creates a new theme whose label colors are this strong: the tints, inks and dots of tags, chips, colored folders
	///     and swatches given a <see cref="Moka.Red.Core.Enums.MokaLabelColor" />. Every tone keeps ink readable on its tint
	///     and dots visible on the page. No effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="tone">How strong the label colors are.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithLabelTone(MokaLabelTone tone) =>
		Look == MokaLook.Gate ? this with { LabelTone = tone } : this;

	/// <summary>
	///     Creates a new theme with toasts, notifications, popups, lists, dialogs and buttons set in one step. The toast
	///     stack is left as it is. No effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	/// <param name="style">The set of looks.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithComponentStyle(MokaComponentStyle style)
	{
		if (Look != MokaLook.Gate)
		{
			return this;
		}

		return style switch
		{
			MokaComponentStyle.Soft => this with
			{
				Toasts = MokaToastLook.IconTile,
				Notifications = MokaNotificationLook.Inbox,
				Popups = MokaPopupLook.Elevated,
				Lists = MokaListLook.Check,
				Dialogs = MokaDialogLook.IconLead,
				Buttons = MokaButtonLook.Soft
			},
			MokaComponentStyle.Terminal => this with
			{
				Toasts = MokaToastLook.Terminal,
				Notifications = MokaNotificationLook.Terminal,
				Popups = MokaPopupLook.Terminal,
				Lists = MokaListLook.Terminal,
				Dialogs = MokaDialogLook.Terminal,
				Buttons = MokaButtonLook.Glow
			},
			_ => this with
			{
				Toasts = MokaToastLook.Standard,
				Notifications = MokaNotificationLook.Standard,
				Popups = MokaPopupLook.Standard,
				Lists = MokaListLook.Standard,
				Dialogs = MokaDialogLook.Standard,
				Buttons = MokaButtonLook.Standard
			}
		};
	}

	/// <summary>
	///     Creates a new theme that shows or hides text saying what a thing is for, such as
	///     <c>MokaPageHeader.Lead</c>. Warnings, empty states and text saying what is so right now never hide. No
	///     effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	public MokaTheme WithDescriptions(bool show = true) =>
		Look == MokaLook.Gate ? this with { Descriptions = show } : this;

	/// <summary>
	///     Creates a new theme in the other color scheme, staying in the same <see cref="Look" /> and keeping its style
	///     choices, accent, high contrast and pinned tokens. A theme from a seed is derived again from its seed; any other
	///     takes its preset palette for that scheme with the accent <see cref="WithAccent(string)" /> gave it put back.
	/// </summary>
	/// <param name="isDark">Whether the new theme is dark.</param>
	/// <returns>The new theme.</returns>
	public MokaTheme WithDark(bool isDark)
	{
		if (Seed is not null)
		{
			return this with { IsDark = isDark, Palette = MokaSeedPalette.Derive(Seed, isDark, HighContrast) };
		}

		MokaPalette palette = Look == MokaLook.Gate
			? MokaPalette.ForSurfaces(Surfaces, isDark)
			: isDark ? MokaPalette.Dark : MokaPalette.Light;
		MokaTheme rebuilt = this with { IsDark = isDark, Palette = palette, PaletteBeforeHighContrast = null };
		if (Accent is not null)
		{
			rebuilt = rebuilt.WithAccent(Accent);
		}

		return HighContrast && Look == MokaLook.Gate ? rebuilt.TurnOnHighContrast() : rebuilt;
	}

	/// <summary>
	///     Creates a new theme on other surface colors, keeping its accent. On a theme from a seed the seed's neutral
	///     becomes that family's surface color. It has no effect on the <see cref="MokaLook.Matrix" /> look, which has one
	///     set of surfaces.
	/// </summary>
	/// <param name="surfaces">The surface colors.</param>
	/// <returns>The new theme, or this one on the <see cref="MokaLook.Matrix" /> look.</returns>
	public MokaTheme WithSurfaces(MokaSurfaces surfaces)
	{
		if (Look != MokaLook.Gate)
		{
			return this;
		}

		if (Seed is not null)
		{
			MokaThemeSeed seed = Seed with { Neutral = MokaPalette.ForSurfaces(surfaces, true).Surface, NeutralChroma = null };
			return this with { Surfaces = surfaces, Seed = seed, Palette = MokaSeedPalette.Derive(seed, IsDark, HighContrast) };
		}

		var accent = new MokaAccentShades(Palette.Primary, Palette.PrimaryLight, Palette.PrimaryBg);
		MokaPalette normal = MokaPalette.ForSurfaces(surfaces, IsDark).WithGateAccent(accent, IsDark);
		MokaTheme rebuilt = this with { Surfaces = surfaces, Palette = normal, PaletteBeforeHighContrast = null };
		return HighContrast ? rebuilt.TurnOnHighContrast() : rebuilt;
	}

	/// <summary>
	///     Creates a new theme re-tinted around the given accent color. On a theme from a seed it is any CSS color, and the
	///     theme is derived again with it as the seed's accent. Otherwise it is a hex color (#rrggbb or #rgb): on the
	///     <see cref="MokaLook.Matrix" /> look the color becomes the primary as given, its light and dark variants mix
	///     toward white and black, every glow and border token takes the standard alpha tiers, and dark themes also get
	///     accent-tinted outlines and shadow rings. On the <see cref="MokaLook.Gate" /> look it is read as a hue and a
	///     saturation (see <see cref="MokaAccent.FromColor" />), so every shade keeps its contrast and the primary may
	///     differ from the color you passed. The theme remembers it, so <see cref="WithDark" /> keeps it.
	/// </summary>
	/// <param name="color">The accent color.</param>
	/// <returns>The new theme.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="color" /> is not a color this theme can take.</exception>
	public MokaTheme WithAccent(string color)
	{
		if (Seed is not null)
		{
			return WithSeed(Seed with { Accent = color });
		}

		if (!MokaColorMath.TryParseHex(color, out int r, out int g, out int b))
		{
			throw new ArgumentException($"Accent must be a hex color like #ef5350, got '{color}'.", nameof(color));
		}

		if (Look == MokaLook.Gate)
		{
			return WithAccent(MokaAccent.FromColor(color));
		}

		var exact = new MokaAccentShades(color, color, color);
		return this with { Palette = MatrixAccentPalette(color, r, g, b), Accent = new MokaAccent("custom", "Custom", exact, exact) };
	}

	// The original design's accent: the color as given, its light and dark shades mixed toward white and black, every
	// glow and border at the standard tiers, and on dark the outlines tinted too.
	private MokaPalette MatrixAccentPalette(string color, int r, int g, int b)
	{
		MokaPalette palette = MokaAccentTiers.Matrix.ApplyTo(Palette, color) with
		{
			Primary = color,
			PrimaryLight = MokaColorMath.Mix(r, g, b, MokaSrgb.White, MokaColorMath.MatrixShadeMix),
			PrimaryDark = MokaColorMath.Mix(r, g, b, MokaSrgb.Black, MokaColorMath.MatrixShadeMix)
		};

		return IsDark
			? palette with
			{
				Outline = MokaColorMath.Rgba(r, g, b, MatrixDarkOutline),
				OutlineVariant = MokaColorMath.Rgba(r, g, b, MatrixDarkOutlineVariant)
			}
			: palette;
	}

	/// <summary>
	///     Creates a new theme with this accent's shades for the theme's color scheme. On the
	///     <see cref="MokaLook.Matrix" /> look only the accent's main color is used, as
	///     <see cref="WithAccent(string)" /> would. On a theme from a seed the shade for the theme's scheme becomes the
	///     seed's accent. The theme remembers it, so <see cref="WithDark" /> takes the accent's other shades.
	/// </summary>
	/// <param name="accent">The accent.</param>
	/// <returns>The new theme.</returns>
	public MokaTheme WithAccent(MokaAccent accent)
	{
		ArgumentNullException.ThrowIfNull(accent);

		MokaAccentShades shades = accent.For(IsDark);
		if (Seed is not null)
		{
			return WithSeed(Seed with { Accent = shades.Color });
		}

		if (Look == MokaLook.Gate)
		{
			return this with { Palette = Palette.WithGateAccent(shades, IsDark), Accent = accent };
		}

		if (!MokaColorMath.TryParseHex(shades.Color, out int r, out int g, out int b))
		{
			throw new ArgumentException($"An accent's shades must be hex colors like #ef5350, got '{shades.Color}'.", nameof(accent));
		}

		return this with { Palette = MatrixAccentPalette(shades.Color, r, g, b), Accent = accent };
	}

	/// <summary>
	///     Creates a new theme with stronger secondary text and borders, or back to exactly what it had before. A theme
	///     from a seed is derived again with high contrast. It has no effect on the <see cref="MokaLook.Matrix" /> look,
	///     which has no high contrast palette.
	/// </summary>
	/// <param name="highContrast">Whether the theme has high contrast.</param>
	/// <returns>The new theme, or this one when nothing changes.</returns>
	public MokaTheme WithHighContrast(bool highContrast = true)
	{
		if (Look != MokaLook.Gate || highContrast == HighContrast)
		{
			return this;
		}

		if (Seed is not null)
		{
			return this with { HighContrast = highContrast, Palette = MokaSeedPalette.Derive(Seed, IsDark, highContrast) };
		}

		if (highContrast)
		{
			return TurnOnHighContrast();
		}

		MokaPalette normal = PaletteBeforeHighContrast ?? MokaPalette.ForSurfaces(Surfaces, IsDark);
		return this with { HighContrast = false, PaletteBeforeHighContrast = null, Palette = Palette.WithContrastOf(normal) };
	}

	/// <summary>
	///     A theme on the new design (<see cref="MokaLook.Gate" />) derived from a few colors: the accent, a neutral for the
	///     surfaces, and optionally the meanings (see <see cref="MokaThemeSeed" />). Surfaces, borders and text are worked
	///     out in OKLCH, and every text color meets its contrast target on what it sits on. It takes every style choice like
	///     a preset, and <see cref="WithDark" />, <see cref="WithHighContrast" /> and <see cref="WithAccent(string)" />
	///     derive it again from its seed.
	/// </summary>
	/// <param name="seed">The colors to start from.</param>
	/// <param name="dark">Whether the theme is dark.</param>
	/// <returns>The theme.</returns>
	/// <exception cref="ArgumentException">Thrown when a seed color isn't a CSS color, or the accent isn't solid.</exception>
	public static MokaTheme FromSeed(MokaThemeSeed seed, bool dark)
	{
		ArgumentNullException.ThrowIfNull(seed);
		return new MokaTheme
		{
			Look = MokaLook.Gate,
			Typography = MokaTypography.Gate,
			Spacing = MokaSpacing.Gate,
			IsDark = dark,
			Seed = seed,
			Palette = MokaSeedPalette.Derive(seed, dark, highContrast: false)
		};
	}

	/// <summary>
	///     Creates a new theme with its colors derived from <paramref name="seed" />, keeping its scheme, high contrast,
	///     style choices and pinned tokens. A theme on the original design moves to the new one, which seeds belong to,
	///     with the new design's type and sizes.
	/// </summary>
	/// <param name="seed">The colors to start from.</param>
	/// <returns>The new theme.</returns>
	/// <exception cref="ArgumentException">Thrown when a seed color isn't a CSS color, or the accent isn't solid.</exception>
	public MokaTheme WithSeed(MokaThemeSeed seed)
	{
		ArgumentNullException.ThrowIfNull(seed);
		MokaTheme gate = Look == MokaLook.Gate
			? this
			: this with { Look = MokaLook.Gate, Typography = MokaTypography.Gate, Spacing = MokaSpacing.Gate, Density = 1.0 };
		return gate with
		{
			Seed = seed,
			Accent = null,
			PaletteBeforeHighContrast = null,
			Palette = MokaSeedPalette.Derive(seed, IsDark, gate.HighContrast)
		};
	}

	/// <summary>
	///     Creates a new theme whose surfaces, borders and greys take the hue and chroma of <paramref name="color" />,
	///     keeping its accent and style choices. A theme without a seed gets one, with its current primary color as the
	///     accent.
	/// </summary>
	/// <param name="color">Any CSS color; its lightness doesn't matter.</param>
	/// <returns>The new theme.</returns>
	/// <exception cref="ArgumentException">Thrown when the color, or the theme's primary color, isn't a CSS color.</exception>
	public MokaTheme WithNeutral(string color)
	{
		MokaThemeSeed seed = Seed ?? new MokaThemeSeed { Accent = MokaSeedPalette.Read(Palette.Primary, "accent").ToHex(uppercase: true) };
		return WithSeed(seed with { Neutral = color });
	}

	private MokaTheme TurnOnHighContrast() =>
		this with { HighContrast = true, PaletteBeforeHighContrast = Palette, Palette = Palette.WithHighContrast(IsDark, Surfaces) };

	/// <summary>
	///     Creates a new theme with the spacing density changed (0.75 compact, 1.0 cozy, 1.15 comfy). On the
	///     <see cref="MokaLook.Gate" /> look a density below 1 also swaps the layout tokens for their compact
	///     values, and any other density for their comfortable ones.
	/// </summary>
	public MokaTheme WithDensity(double density) => Look == MokaLook.Gate
		? this with { Density = density, Spacing = Spacing.WithGateLayout(density < 1) }
		: this with { Density = density };

	/// <summary>Creates a new theme with all font-size tokens scaled by the given factor.</summary>
	public MokaTheme WithFontScale(double fontScale) => this with { FontScale = fontScale };

	/// <summary>
	///     Creates a new theme that holds one token at <paramref name="value" />, whatever its palette and style choices
	///     give it. The pin stays through every other <c>With</c> call, including <see cref="WithDark" />,
	///     <see cref="WithHighContrast" /> and <see cref="WithAccent(string)" />, until <see cref="WithoutToken" /> lets it go.
	///     A pinned value is written as it is: it doesn't move with the scheme, so pin a color that reads on both or pin it
	///     again after a change of scheme.
	/// </summary>
	/// <param name="name">The token, such as <c>--moka-color-primary</c>; see <see cref="MokaTokens.All" />.</param>
	/// <param name="value">The CSS value, checked against the token's kind (see <see cref="MokaToken.Accepts" />).</param>
	/// <returns>The new theme.</returns>
	/// <exception cref="ArgumentException">
	///     Thrown when no token has that name, or the value isn't one the token takes or could end its declaration.
	/// </exception>
	public MokaTheme WithToken(string name, string value)
	{
		if (!MokaTokens.TryFind(name, out MokaToken? token))
		{
			throw new ArgumentException($"No theme token is called '{name}'.", nameof(name));
		}

		string trimmed = value?.Trim() ?? "";
		if (!token.Accepts(trimmed))
		{
			string kind = token.Kind switch
			{
				MokaTokenKind.Color => "a color",
				MokaTokenKind.Length => "a length",
				_ => "a CSS value"
			};
			throw new ArgumentException($"'{value}' is not a value {name} can take: it needs {kind} that can't end its declaration.", nameof(value));
		}

		return this with { PinnedTokens = PinnedTokens.With(name, trimmed) };
	}

	/// <summary>Creates a new theme that lets a token pinned by <see cref="WithToken" /> go back to what the theme gives it.</summary>
	/// <param name="name">The token.</param>
	/// <returns>The new theme, or this one when the token isn't pinned.</returns>
	public MokaTheme WithoutToken(string name) =>
		PinnedTokens.ContainsKey(name) ? this with { PinnedTokens = PinnedTokens.Without(name) } : this;

	/// <summary>
	///     The CSS custom property declarations of every token, as <c>name: value; </c> in the order of
	///     <see cref="MokaTokens.All" />, for an inline style attribute or a CSS rule. A token whose value holds any of
	///     <c>; { } &lt; &gt; \</c> (see <see cref="CssValues.IsSafe" />), or leaves a string, bracket or comment open (see
	///     <see cref="CssValues.IsSelfContained" />), is left out, so the <c>moka.css</c> default for it applies instead.
	///     Worked out once per theme, and shared by equal themes.
	/// </summary>
	/// <returns>The declarations.</returns>
	public string ToCssVariables() => MokaThemeTokens.For(this).Css;

	/// <summary>
	///     Every token's value, by name: what <see cref="ToCssVariables" /> writes, with <see cref="Pins" /> in place. A value
	///     <see cref="ToCssVariables" /> leaves out is missing here too. Worked out once per theme, and shared by equal themes.
	/// </summary>
	/// <returns>The values by token name.</returns>
	public IReadOnlyDictionary<string, string> ToTokenMap() => MokaThemeTokens.For(this).Map;

	/// <summary>
	///     Works a color out over this theme's tokens the way a browser would: a token name (<c>--moka-color-surface</c>), a
	///     <c>var()</c>, a <c>color-mix()</c> in srgb or oklab, or any color <see cref="MokaCssColor" /> reads. A value that
	///     depends on the element it is drawn on, such as <c>currentColor</c>, doesn't resolve.
	/// </summary>
	/// <param name="value">A token name or a CSS color value.</param>
	/// <param name="color">The color, when it resolved.</param>
	/// <returns><c>true</c> when the value resolved to one color.</returns>
	public bool TryResolveColor(string value, out MokaSrgb color)
	{
		ArgumentNullException.ThrowIfNull(value);
		return new MokaTokenResolver(ToTokenMap()).TryResolve(value, out color);
	}

	/// <summary>
	///     Measures every text-on-background and line-on-background pair the library draws from this theme's tokens: body
	///     and secondary text on each surface, links, the accent's and meanings' text, code colors, label inks and the
	///     lines of controls, with each pair's WCAG ratio, APCA Lc and what it has to meet. Pins count, so the report says
	///     what a page would show.
	/// </summary>
	/// <returns>The report.</returns>
	public MokaContrastReport CheckContrast() => MokaContrastReport.For(this);
}
