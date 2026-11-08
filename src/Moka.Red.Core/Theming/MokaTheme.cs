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

	/// <summary>Dark blue-grey with a blue accent, the colors of MokaGate. Every style choice can be changed on it.</summary>
	public static MokaTheme Gate { get; } = new()
	{
		Look = MokaLook.Gate,
		Palette = MokaPalette.Gate,
		Typography = MokaTypography.Gate,
		Spacing = MokaSpacing.Gate,
		IsDark = true
	};

	/// <summary>Light blue-grey with a blue accent, the light colors of MokaGate.</summary>
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

	/// <summary>Creates a new theme with another letter case. No effect on the <see cref="MokaLook.Matrix" /> look.</summary>
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
	///     Creates a new theme that shows or hides text saying what a thing is for, such as
	///     <c>MokaPageHeader.Lead</c>. Warnings, empty states and text saying what is so right now never hide. No
	///     effect on the <see cref="MokaLook.Matrix" /> look.
	/// </summary>
	public MokaTheme WithDescriptions(bool show = true) =>
		Look == MokaLook.Gate ? this with { Descriptions = show } : this;

	/// <summary>
	///     Creates a new theme with dark mode toggled, staying in the same <see cref="Look" />. This replaces the
	///     whole palette, so call it before <see cref="WithAccent(string)" />.
	/// </summary>
	public MokaTheme WithDark(bool isDark)
	{
		if (Look == MokaLook.Gate)
		{
			MokaPalette palette = MokaPalette.ForSurfaces(Surfaces, isDark);
			return this with { Palette = palette.WithGateContrast(HighContrast, isDark, Surfaces), IsDark = isDark };
		}

		return isDark
			? this with { Palette = MokaPalette.Dark, IsDark = true }
			: this with { Palette = MokaPalette.Light, IsDark = false };
	}

	/// <summary>
	///     Creates a new theme on other surface colors, keeping its accent. It has no effect on the
	///     <see cref="MokaLook.Matrix" /> look, which has one set of surfaces.
	/// </summary>
	public MokaTheme WithSurfaces(MokaSurfaces surfaces)
	{
		if (Look != MokaLook.Gate)
		{
			return this;
		}

		var accent = new MokaAccentShades(Palette.Primary, Palette.PrimaryLight, Palette.PrimaryBg);
		return this with
		{
			Surfaces = surfaces,
			Palette = MokaPalette.ForSurfaces(surfaces, IsDark)
				.WithGateAccent(accent, IsDark)
				.WithGateContrast(HighContrast, IsDark, surfaces)
		};
	}

	/// <summary>
	///     Creates a new theme re-tinted around the given accent color (#rrggbb or #rgb). On the
	///     <see cref="MokaLook.Matrix" /> look the color becomes the primary as given, its light and dark variants
	///     mix toward white and black, every glow and border token takes the standard alpha tiers, and dark
	///     themes also get accent-tinted outlines and shadow rings. On the <see cref="MokaLook.Gate" /> look it is
	///     read as a hue and a saturation (see <see cref="MokaAccent.FromColor" />), so every shade keeps its
	///     contrast and the primary may differ from the color you passed.
	/// </summary>
	/// <exception cref="ArgumentException">Thrown when <paramref name="color" /> is not a hex color.</exception>
	public MokaTheme WithAccent(string color)
	{
		if (!MokaColorMath.TryParseHex(color, out int r, out int g, out int b))
		{
			throw new ArgumentException($"Accent must be a hex color like #ef5350, got '{color}'.", nameof(color));
		}

		if (Look == MokaLook.Gate)
		{
			return WithAccent(MokaAccent.FromColor(color));
		}

		MokaPalette palette = Palette with
		{
			Primary = color,
			PrimaryLight = MokaColorMath.Mix(r, g, b, 255, 0.30),
			PrimaryDark = MokaColorMath.Mix(r, g, b, 0, 0.30),
			PrimaryGlow = MokaColorMath.Rgba(r, g, b, 0.08),
			PrimaryGlowMd = MokaColorMath.Rgba(r, g, b, 0.15),
			PrimaryGlowStrong = MokaColorMath.Rgba(r, g, b, 0.25),
			PrimaryGlowFaint = MokaColorMath.Rgba(r, g, b, 0.03),
			PrimaryBorder = MokaColorMath.Rgba(r, g, b, 0.20),
			PrimaryBorderDim = MokaColorMath.Rgba(r, g, b, 0.08)
		};

		if (IsDark)
		{
			palette = palette with
			{
				Outline = MokaColorMath.Rgba(r, g, b, 0.12),
				OutlineVariant = MokaColorMath.Rgba(r, g, b, 0.06)
			};
		}

		return this with { Palette = palette };
	}

	/// <summary>
	///     Creates a new theme with this accent's shades for the theme's color scheme. On the
	///     <see cref="MokaLook.Matrix" /> look only the accent's main color is used, as
	///     <see cref="WithAccent(string)" /> would.
	/// </summary>
	public MokaTheme WithAccent(MokaAccent accent)
	{
		ArgumentNullException.ThrowIfNull(accent);

		MokaAccentShades shades = accent.For(IsDark);
		return Look == MokaLook.Gate
			? this with { Palette = Palette.WithGateAccent(shades, IsDark) }
			: WithAccent(shades.Color);
	}

	/// <summary>
	///     Creates a new theme with stronger secondary text and borders, or back to the normal ones. It has no
	///     effect on the <see cref="MokaLook.Matrix" /> look, which has no high contrast palette.
	/// </summary>
	public MokaTheme WithHighContrast(bool highContrast = true) => Look == MokaLook.Gate
		? this with { HighContrast = highContrast, Palette = Palette.WithGateContrast(highContrast, IsDark, Surfaces) }
		: this;

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
	///     Generates CSS custom property declarations for all theme tokens.
	///     Output is suitable for use as an inline style attribute value or inside a CSS rule.
	///     A token whose value holds any of <c>; { } &lt; &gt; \</c> (see <see cref="CssValues.IsSafe" />), or
	///     leaves a string, bracket or comment open (see <see cref="CssValues.IsSelfContained" />), is left
	///     out, so the <c>moka.css</c> default for it applies instead.
	/// </summary>
	public string ToCssVariables()
	{
		bool gate = Look == MokaLook.Gate;
		var sb = new StringBuilder(10240);

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

		// Surfaces, borders and text the Gate look tells apart
		AppendVar(sb, "--moka-color-surface-raised", Palette.SurfaceRaised);
		AppendVar(sb, "--moka-color-neutral-bg", Palette.NeutralBg);
		AppendVar(sb, "--moka-color-outline-strong", Palette.OutlineStrong);
		AppendVar(sb, "--moka-color-outline-hover", Palette.OutlineHover);
		AppendVar(sb, "--moka-color-on-surface-2", Palette.OnSurface2);
		AppendVar(sb, "--moka-color-icon", Palette.Icon);
		AppendVar(sb, "--moka-color-lift", Palette.Lift);
		AppendVar(sb, "--moka-color-knob", Palette.Knob);
		AppendVar(sb, "--moka-shadow-knob", Palette.KnobShadow);
		AppendVar(sb, "--moka-color-paper", Palette.Paper);
		AppendVar(sb, "--moka-color-paper-edge", Palette.PaperEdge);

		// The accent's selected shades
		AppendVar(sb, "--moka-color-primary-bg", Palette.PrimaryBg);
		AppendVar(sb, "--moka-color-primary-row", Palette.PrimaryRow);
		AppendVar(sb, "--moka-color-primary-ring", Palette.PrimaryRing);
		AppendVar(sb, "--moka-color-primary-pill", Palette.PrimaryPill);
		AppendVar(sb, "--moka-color-primary-focus", Palette.PrimaryFocus);
		AppendVar(sb, "--moka-color-primary-text", Palette.PrimaryText);

		// Each meaning's tint, ink, text and line
		AppendVar(sb, "--moka-color-success-bg", Palette.SuccessBg);
		AppendVar(sb, "--moka-color-success-ink", Palette.SuccessInk);
		AppendVar(sb, "--moka-color-success-text", Palette.SuccessText);
		AppendVar(sb, "--moka-color-success-line", Palette.SuccessLine);
		AppendVar(sb, "--moka-color-warning-bg", Palette.WarningBg);
		AppendVar(sb, "--moka-color-warning-ink", Palette.WarningInk);
		AppendVar(sb, "--moka-color-warning-text", Palette.WarningText);
		AppendVar(sb, "--moka-color-warning-line", Palette.WarningLine);
		AppendVar(sb, "--moka-color-error-bg", Palette.ErrorBg);
		AppendVar(sb, "--moka-color-error-ink", Palette.ErrorInk);
		AppendVar(sb, "--moka-color-error-text", Palette.ErrorText);
		AppendVar(sb, "--moka-color-error-line", Palette.ErrorLine);
		AppendVar(sb, "--moka-color-error-soft", Palette.ErrorSoft);
		AppendVar(sb, "--moka-color-info-bg", Palette.InfoBg);
		AppendVar(sb, "--moka-color-info-ink", Palette.InfoInk);
		AppendVar(sb, "--moka-color-info-text", Palette.InfoText);
		AppendVar(sb, "--moka-color-info-line", Palette.InfoLine);

		// Typography
		AppendVar(sb, "--moka-font-family", Typography.FontFamily);
		AppendVar(sb, "--moka-font-family-mono", Typography.FontFamilyMono);
		AppendVar(sb, "--moka-font-family-display", Typography.FontFamilyDisplay);
		AppendVar(sb, "--moka-font-size-xxs", ScaleSize(Typography.FontSizeXxs, FontScale));
		AppendVar(sb, "--moka-font-size-xs", ScaleSize(Typography.FontSizeXs, FontScale));
		AppendVar(sb, "--moka-font-size-sm", ScaleSize(Typography.FontSizeSm, FontScale));
		AppendVar(sb, "--moka-font-size-base", ScaleSize(Typography.FontSizeBase, FontScale));
		AppendVar(sb, "--moka-font-size-md", ScaleSize(Typography.FontSizeMd, FontScale));
		AppendVar(sb, "--moka-font-size-lg", ScaleSize(Typography.FontSizeLg, FontScale));
		AppendVar(sb, "--moka-font-size-xl", ScaleSize(Typography.FontSizeXl, FontScale));
		AppendVar(sb, "--moka-font-size-xxl", ScaleSize(Typography.FontSizeXxl, FontScale));
		AppendVar(sb, "--moka-font-size-display", ScaleSize(Typography.FontSizeDisplay, FontScale));
		AppendVar(sb, "--moka-font-size-root", ScaleSize(Typography.FontSizeRoot, FontScale));
		AppendVar(sb, "--moka-font-size-heading", ScaleSize(Typography.FontSizeHeading, FontScale));
		AppendVar(sb, "--moka-font-size-card-title", ScaleSize(Typography.FontSizeCardTitle, FontScale));
		AppendVar(sb, "--moka-font-size-dialog", ScaleSize(Typography.FontSizeDialog, FontScale));
		AppendVar(sb, "--moka-font-size-menu", ScaleSize(Typography.FontSizeMenu, FontScale));
		AppendVar(sb, "--moka-font-size-mono", ScaleSize(Typography.FontSizeMono, FontScale));
		AppendVar(sb, "--moka-line-height-tight", Typography.LineHeightTight);
		AppendVar(sb, "--moka-line-height-base", Typography.LineHeightBase);
		AppendVar(sb, "--moka-line-height-relaxed", Typography.LineHeightRelaxed);
		AppendVar(sb, "--moka-font-weight-light", Typography.FontWeightLight);
		AppendVar(sb, "--moka-font-weight-normal", Typography.FontWeightNormal);
		AppendVar(sb, "--moka-font-weight-medium", Typography.FontWeightMedium);
		AppendVar(sb, "--moka-font-weight-semibold", Typography.FontWeightSemibold);
		AppendVar(sb, "--moka-font-weight-bold", Typography.FontWeightBold);
		AppendVar(sb, "--moka-font-weight-heading", Typography.FontWeightHeading);
		AppendVar(sb, "--moka-control-case", Typography.ControlCase);
		AppendVar(sb, "--moka-control-tracking", Typography.ControlTracking);
		AppendVar(sb, "--moka-title-case", Typography.TitleCase);
		AppendVar(sb, "--moka-title-tracking", Typography.TitleTracking);
		AppendVar(sb, "--moka-heading-tracking", Typography.HeadingTracking);

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
		AppendVar(sb, "--moka-radius-card", Spacing.RadiusCard);
		AppendVar(sb, "--moka-radius-popup", Spacing.RadiusPopup);

		// Layout, which components read to follow density
		AppendVar(sb, "--moka-page-gap", Spacing.PageGap);
		AppendVar(sb, "--moka-card-padding", Spacing.CardPadding);
		AppendVar(sb, "--moka-card-gap", Spacing.CardGap);
		AppendVar(sb, "--moka-grid-gap", Spacing.GridGap);
		AppendVar(sb, "--moka-panel-padding", Spacing.PanelPadding);
		AppendVar(sb, "--moka-inner-padding", Spacing.InnerPadding);
		AppendVar(sb, "--moka-row-padding", Spacing.RowPadding);
		AppendVar(sb, "--moka-item-padding", Spacing.ItemPadding);
		AppendVar(sb, "--moka-control-height", Spacing.ControlHeight);
		AppendVar(sb, "--moka-main-padding", Spacing.MainPadding);
		(string mainTop, string mainBottom) = PaddingEnds(Spacing.MainPadding);
		AppendVar(sb, "--moka-main-padding-top", mainTop);
		AppendVar(sb, "--moka-main-padding-bottom", mainBottom);
		AppendVar(sb, "--moka-menu-row", Spacing.MenuRow);
		AppendVar(sb, "--moka-button-height", Spacing.ButtonHeight);
		AppendVar(sb, "--moka-button-height-sm", Spacing.ButtonHeightSm);
		AppendVar(sb, "--moka-button-height-lg", Spacing.ButtonHeightLg);
		AppendVar(sb, "--moka-button-padding", Spacing.ButtonPadding);
		AppendVar(sb, "--moka-button-padding-sm", Spacing.ButtonPaddingSm);
		AppendVar(sb, "--moka-input-padding", Spacing.InputPadding);
		AppendVar(sb, "--moka-segment-height", Spacing.SegmentHeight);
		AppendVar(sb, "--moka-cell-padding", Spacing.CellPadding);
		AppendVar(sb, "--moka-pill-padding", Spacing.PillPadding);

		// Elevation / box-shadow. Dark Matrix shadows are accent-tinted glow rings: recompute them
		// from the primary color when it parses as hex so WithAccent() re-tints them too;
		// otherwise fall back to the interned red constants. The Gate look has real drop shadows,
		// and on dark only for what floats.
		string shadow1 = Shadows.Light1, shadow2 = Shadows.Light2, shadow3 = Shadows.Light3, shadow4 = Shadows.Light4;
		string shadowPopup = Shadows.LightPopup, shadowPopupLg = Shadows.LightPopupLg;
		string shadowModal = Shadows.LightModal, shadowSubtle = Shadows.LightSubtle;

		if (gate)
		{
			shadow1 = IsDark ? "none" : Shadows.GateLightCard;
			shadow2 = IsDark ? "none" : Shadows.GateLightRaised;
			shadow3 = IsDark ? Shadows.GateDarkPopup : Shadows.GateLightPopup;
			shadow4 = IsDark ? Shadows.GateDarkBar : Shadows.GateLightBar;
			shadowPopup = IsDark ? Shadows.GateDarkPopup : Shadows.GateLightPopup;
			shadowPopupLg = IsDark ? Shadows.GateDarkBar : Shadows.GateLightBar;
			shadowModal = IsDark ? Shadows.GateDarkModal : Shadows.GateLightModal;
			shadowSubtle = IsDark ? "none" : Shadows.GateLightCard;
		}
		else if (IsDark)
		{
			if (MokaColorMath.TryParseHex(Palette.Primary, out int ar, out int ag, out int ab))
			{
				shadow1 = $"0 0 0 1px {MokaColorMath.Rgba(ar, ag, ab, 0.06)}";
				shadow2 = $"0 0 0 1px {MokaColorMath.Rgba(ar, ag, ab, 0.12)}";
				shadow3 = $"0 0 0 1px {MokaColorMath.Rgba(ar, ag, ab, 0.12)}, 0 0 12px {MokaColorMath.Rgba(ar, ag, ab, 0.08)}";
				shadow4 = $"0 0 0 1px {MokaColorMath.Rgba(ar, ag, ab, 0.20)}, 0 0 24px {MokaColorMath.Rgba(ar, ag, ab, 0.12)}";
				shadowPopup = $"0 0 0 1px {MokaColorMath.Rgba(ar, ag, ab, 0.12)}, 0 0 16px {MokaColorMath.Rgba(ar, ag, ab, 0.08)}";
				shadowPopupLg = $"0 0 0 1px {MokaColorMath.Rgba(ar, ag, ab, 0.16)}, 0 0 24px {MokaColorMath.Rgba(ar, ag, ab, 0.12)}";
				shadowModal = $"0 0 0 1px {MokaColorMath.Rgba(ar, ag, ab, 0.20)}, 0 0 32px {MokaColorMath.Rgba(ar, ag, ab, 0.15)}";
				shadowSubtle = $"0 0 0 1px {MokaColorMath.Rgba(ar, ag, ab, 0.06)}";
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
		AppendVar(sb, "--moka-transition-fast", gate ? "120ms ease-out" : "120ms ease");
		AppendVar(sb, "--moka-transition-normal", gate ? "150ms ease-out" : "150ms ease");
		AppendVar(sb, "--moka-transition-slow", gate ? "200ms ease-out" : "200ms ease");

		// Component heights
		AppendVar(sb, "--moka-height-statusbar", Spacing.StatusBarHeight);
		AppendVar(sb, "--moka-height-toolbar", Spacing.ToolbarHeight);
		AppendVar(sb, "--moka-height-toolbar-dense", Spacing.ToolbarHeightDense);

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
		AppendVar(sb, "--moka-opacity-disabled", gate ? "0.55" : "0.4");
		AppendVar(sb, "--moka-about-display", Descriptions ? "block" : "none");
		AppendVar(sb, "--moka-opacity-hover", "0.08");
		AppendVar(sb, "--moka-focus-color", "var(--moka-color-primary)");
		AppendVar(sb, "--moka-focus-width", "2px");
		AppendVar(sb, "--moka-border-width", "1px");

		// Focus ring - the signature red-glow interaction on the Matrix look. The Gate look draws it in its
		// own token, because components use the glow tokens for hover backgrounds and keep those low.
		AppendVar(sb, "--moka-focus-ring",
			gate
				? Selection == MokaSelection.Glow
					? "0 0 0 3px var(--moka-color-primary-focus), inset 0 0 12px var(--moka-color-primary-glow)"
					: "0 0 0 3px var(--moka-color-primary-focus)"
			: IsDark ? "0 0 0 3px var(--moka-color-primary-glow), inset 0 0 12px var(--moka-color-primary-glow)"
			: "0 0 0 3px var(--moka-color-primary-glow)");
		AppendVar(sb, "--moka-focus-ring-error", "0 0 0 3px color-mix(in srgb, var(--moka-color-error) 22%, transparent)");

		// Selected state - inset glow (for list rows, tabs, active items)
		AppendVar(sb, "--moka-selected-glow",
			gate
				? Selection switch
				{
					MokaSelection.Flat => "none",
					MokaSelection.Glow => "inset 0 0 20px var(--moka-color-primary-glow-md), 0 0 12px var(--moka-color-primary-glow-md)",
					_ => "inset 0 0 0 1px var(--moka-color-primary-ring), inset 0 0 18px var(--moka-color-primary-glow)"
				}
			: IsDark ? "inset 0 0 20px var(--moka-color-primary-glow), 0 0 12px var(--moka-color-primary-glow)"
			: "0 0 0 2px var(--moka-color-primary-glow-md)");

		// The bar along a selected table row
		AppendVar(sb, "--moka-selected-bar", gate && Selection != MokaSelection.Flat
			? "inset 2px 0 0 var(--moka-color-primary)"
			: "none");

		// Where you are in the app. Ring is the look of a selected row, so a theme that never asks keeps it. On the
		// dark scheme the solid fill's text is the accent's dark ink: white on the dark accent is about 3:1.
		bool solidPlace = gate && CurrentPlace == MokaCurrentPlace.Solid;
		AppendVar(sb, "--moka-current-bg", solidPlace ? "var(--moka-color-primary)" : "var(--moka-color-primary-row)");
		AppendVar(sb, "--moka-current-color", solidPlace ? "var(--moka-color-on-primary)" : "var(--moka-color-on-surface)");
		AppendVar(sb, "--moka-current-icon", solidPlace ? "var(--moka-color-on-primary)" : "var(--moka-color-primary-light)");
		AppendVar(sb, "--moka-current-shadow", solidPlace
			? "0 0 24px color-mix(in srgb, var(--moka-color-primary) 25%, transparent)"
			: "var(--moka-selected-glow)");
		AppendVar(sb, "--moka-current-edge", solidPlace ? "var(--moka-color-on-primary)" : "var(--moka-color-primary)");
		AppendVar(sb, "--moka-current-pill-ring", solidPlace ? "0 0 0 2px var(--moka-color-on-primary)" : "none");

		// A primary button. Only the Gate look reads these.
		(string buttonBg, string buttonColor, string buttonBorder, string buttonHoverBg, string buttonHoverBorder) = MainButton switch
		{
			MokaMainButton.Tinted => ("var(--moka-color-primary-bg)", "var(--moka-color-primary-light)", "var(--moka-color-primary-ring)",
				"var(--moka-color-primary-pill)", "var(--moka-color-primary)"),
			MokaMainButton.Outline => ("transparent", "var(--moka-color-primary)", "var(--moka-color-primary-ring)",
				"var(--moka-color-primary-row)", "var(--moka-color-primary)"),
			_ => ("var(--moka-color-primary)", "var(--moka-color-on-primary)", "var(--moka-color-primary)",
				"color-mix(in srgb, var(--moka-color-primary) 85%, var(--moka-color-lift))",
				"color-mix(in srgb, var(--moka-color-primary) 85%, var(--moka-color-lift))")
		};
		AppendVar(sb, "--moka-button-primary-bg", buttonBg);
		AppendVar(sb, "--moka-button-primary-color", buttonColor);
		AppendVar(sb, "--moka-button-primary-border", buttonBorder);
		AppendVar(sb, "--moka-button-primary-hover-bg", buttonHoverBg);
		AppendVar(sb, "--moka-button-primary-hover-border", buttonHoverBorder);

		// Behind modal overlays: the theme's background at 85% unless the palette says otherwise, so a
		// dark theme dims the page and a light one frosts it. The strong and frosted mixes back the
		// dialog's selectable backdrop effects; each resolves var(--moka-color-background) where it is
		// declared, so they follow the theme the same way the base backdrop does.
		AppendVar(sb, "--moka-color-backdrop", Palette.Backdrop);
		AppendVar(sb, "--moka-backdrop-blur", "8px");
		AppendVar(sb, "--moka-color-backdrop-strong", "color-mix(in srgb, var(--moka-color-background) 92%, transparent)");
		AppendVar(sb, "--moka-color-backdrop-frosted", "color-mix(in srgb, var(--moka-color-background) 55%, transparent)");
		AppendVar(sb, "--moka-backdrop-blur-strong", "16px");

		return sb.ToString();
	}

	// MokaThemeProvider writes this output into a <style> element, where a semicolon or a brace in a
	// value would add declarations or whole rules for the page, and < could end the element. An open
	// string or bracket would swallow every token after it.
	// The top and bottom of a padding shorthand, by CSS's own rule: one value is every side, and from three values on
	// the third is the bottom. Split at the top level, so a var() or calc() stays one value.
	private static (string Top, string Bottom) PaddingEnds(string shorthand)
	{
		List<string> values = [];
		int depth = 0;
		int start = 0;
		for (int i = 0; i <= shorthand.Length; i++)
		{
			char c = i < shorthand.Length ? shorthand[i] : ' ';
			if (c == '(')
			{
				depth++;
			}
			else if (c == ')')
			{
				depth = Math.Max(0, depth - 1);
			}
			else if (char.IsWhiteSpace(c) && depth == 0)
			{
				if (i > start)
				{
					values.Add(shorthand[start..i]);
				}

				start = i + 1;
			}
		}

		return values.Count switch
		{
			0 => ("0", "0"),
			1 or 2 => (values[0], values[0]),
			_ => (values[0], values[2])
		};
	}

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

		public const string GateDarkPopup = "0 18px 40px rgba(0, 0, 0, 0.45)";
		public const string GateDarkBar = "0 18px 50px rgba(0, 0, 0, 0.55)";
		public const string GateDarkModal = "0 3px 6px 2px rgba(0, 0, 0, 0.3)";

		public const string GateLightCard = "0 1px 2px rgba(22, 30, 48, 0.05)";
		public const string GateLightRaised = "0 2px 6px rgba(22, 30, 48, 0.08)";
		public const string GateLightPopup = "0 16px 36px rgba(22, 30, 48, 0.14), 0 2px 6px rgba(22, 30, 48, 0.06)";
		public const string GateLightBar = "0 14px 40px rgba(22, 30, 48, 0.16), 0 2px 6px rgba(22, 30, 48, 0.06)";
		public const string GateLightModal = "0 8px 28px rgba(22, 30, 48, 0.18)";
	}
}
