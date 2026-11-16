using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Moka.Red.Core.Theming;

/// <summary>
///     One string property of <see cref="MokaPalette" />, <see cref="MokaTypography" /> or <see cref="MokaSpacing" />,
///     with the token it feeds. What an editor or a serializer walks to read and set a theme's values by name.
/// </summary>
/// <typeparam name="TRecord">The record the property is on.</typeparam>
public sealed class MokaThemeProperty<TRecord>
{
	internal MokaThemeProperty(string name, string token, Func<TRecord, string> get, Func<TRecord, string, TRecord> set)
	{
		Name = name;
		Token = token;
		Get = get;
		Set = set;
	}

	/// <summary>The property's name, such as <c>Primary</c>.</summary>
	public string Name { get; }

	/// <summary>The token the property is written as, such as <c>--moka-color-primary</c>.</summary>
	public string Token { get; }

	/// <summary>Reads the property.</summary>
	public Func<TRecord, string> Get { get; }

	/// <summary>Returns a copy of the record with the property set.</summary>
	public Func<TRecord, string, TRecord> Set { get; }
}

/// <summary>
///     Every CSS custom property a theme writes, in the order <see cref="MokaTheme.ToCssVariables" /> writes them, with
///     what each is for and its <c>moka.css</c> fallback. <see cref="MokaTheme.ToTokenMap" /> gives one theme's values.
/// </summary>
public static partial class MokaTokens
{
	private static readonly string[] LabelDescriptions =
	[
		"The {0} label's solid color, for dots, icons and swatches.",
		"The {0} label's tint behind a tag, mixed into the surface.",
		"Text on the {0} label's tint."
	];

	private static readonly Lazy<FrozenDictionary<string, (string Light, string? Dark)>> FallbackTable = new(BuildFallbacks);

	/// <summary>The palette's properties, in declaration order.</summary>
	public static IReadOnlyList<MokaThemeProperty<MokaPalette>> PaletteProperties { get; } =
	[
		Palette(nameof(MokaPalette.Primary), "--moka-color-primary", p => p.Primary, (p, v) => p with { Primary = v }),
		Palette(nameof(MokaPalette.PrimaryLight), "--moka-color-primary-light", p => p.PrimaryLight, (p, v) => p with { PrimaryLight = v }),
		Palette(nameof(MokaPalette.PrimaryDark), "--moka-color-primary-dark", p => p.PrimaryDark, (p, v) => p with { PrimaryDark = v }),
		Palette(nameof(MokaPalette.OnPrimary), "--moka-color-on-primary", p => p.OnPrimary, (p, v) => p with { OnPrimary = v }),
		Palette(nameof(MokaPalette.Secondary), "--moka-color-secondary", p => p.Secondary, (p, v) => p with { Secondary = v }),
		Palette(nameof(MokaPalette.SecondaryLight), "--moka-color-secondary-light", p => p.SecondaryLight, (p, v) => p with { SecondaryLight = v }),
		Palette(nameof(MokaPalette.SecondaryDark), "--moka-color-secondary-dark", p => p.SecondaryDark, (p, v) => p with { SecondaryDark = v }),
		Palette(nameof(MokaPalette.OnSecondary), "--moka-color-on-secondary", p => p.OnSecondary, (p, v) => p with { OnSecondary = v }),
		Palette(nameof(MokaPalette.Surface), "--moka-color-surface", p => p.Surface, (p, v) => p with { Surface = v }),
		Palette(nameof(MokaPalette.SurfaceVariant), "--moka-color-surface-variant", p => p.SurfaceVariant, (p, v) => p with { SurfaceVariant = v }),
		Palette(nameof(MokaPalette.OnSurface), "--moka-color-on-surface", p => p.OnSurface, (p, v) => p with { OnSurface = v }),
		Palette(nameof(MokaPalette.OnSurfaceVariant), "--moka-color-on-surface-variant", p => p.OnSurfaceVariant, (p, v) => p with { OnSurfaceVariant = v }),
		Palette(nameof(MokaPalette.Background), "--moka-color-background", p => p.Background, (p, v) => p with { Background = v }),
		Palette(nameof(MokaPalette.OnBackground), "--moka-color-on-background", p => p.OnBackground, (p, v) => p with { OnBackground = v }),
		Palette(nameof(MokaPalette.Error), "--moka-color-error", p => p.Error, (p, v) => p with { Error = v }),
		Palette(nameof(MokaPalette.OnError), "--moka-color-on-error", p => p.OnError, (p, v) => p with { OnError = v }),
		Palette(nameof(MokaPalette.Warning), "--moka-color-warning", p => p.Warning, (p, v) => p with { Warning = v }),
		Palette(nameof(MokaPalette.OnWarning), "--moka-color-on-warning", p => p.OnWarning, (p, v) => p with { OnWarning = v }),
		Palette(nameof(MokaPalette.Success), "--moka-color-success", p => p.Success, (p, v) => p with { Success = v }),
		Palette(nameof(MokaPalette.OnSuccess), "--moka-color-on-success", p => p.OnSuccess, (p, v) => p with { OnSuccess = v }),
		Palette(nameof(MokaPalette.Info), "--moka-color-info", p => p.Info, (p, v) => p with { Info = v }),
		Palette(nameof(MokaPalette.OnInfo), "--moka-color-on-info", p => p.OnInfo, (p, v) => p with { OnInfo = v }),
		Palette(nameof(MokaPalette.ErrorDim), "--moka-color-error-dim", p => p.ErrorDim, (p, v) => p with { ErrorDim = v }),
		Palette(nameof(MokaPalette.WarningDim), "--moka-color-warning-dim", p => p.WarningDim, (p, v) => p with { WarningDim = v }),
		Palette(nameof(MokaPalette.SuccessDim), "--moka-color-success-dim", p => p.SuccessDim, (p, v) => p with { SuccessDim = v }),
		Palette(nameof(MokaPalette.InfoDim), "--moka-color-info-dim", p => p.InfoDim, (p, v) => p with { InfoDim = v }),
		Palette(nameof(MokaPalette.Outline), "--moka-color-outline", p => p.Outline, (p, v) => p with { Outline = v }),
		Palette(nameof(MokaPalette.OutlineVariant), "--moka-color-outline-variant", p => p.OutlineVariant, (p, v) => p with { OutlineVariant = v }),
		Palette(nameof(MokaPalette.SurfaceHover), "--moka-color-surface-hover", p => p.SurfaceHover, (p, v) => p with { SurfaceHover = v }),
		Palette(nameof(MokaPalette.Surface2), "--moka-color-surface-2", p => p.Surface2, (p, v) => p with { Surface2 = v }),
		Palette(nameof(MokaPalette.Surface3), "--moka-color-surface-3", p => p.Surface3, (p, v) => p with { Surface3 = v }),
		Palette(nameof(MokaPalette.PrimaryGlow), "--moka-color-primary-glow", p => p.PrimaryGlow, (p, v) => p with { PrimaryGlow = v }),
		Palette(nameof(MokaPalette.PrimaryGlowMd), "--moka-color-primary-glow-md", p => p.PrimaryGlowMd, (p, v) => p with { PrimaryGlowMd = v }),
		Palette(nameof(MokaPalette.PrimaryGlowStrong), "--moka-color-primary-glow-strong", p => p.PrimaryGlowStrong, (p, v) => p with { PrimaryGlowStrong = v }),
		Palette(nameof(MokaPalette.PrimaryBorder), "--moka-color-primary-border", p => p.PrimaryBorder, (p, v) => p with { PrimaryBorder = v }),
		Palette(nameof(MokaPalette.PrimaryBorderDim), "--moka-color-primary-border-dim", p => p.PrimaryBorderDim, (p, v) => p with { PrimaryBorderDim = v }),
		Palette(nameof(MokaPalette.PrimaryGlowFaint), "--moka-color-primary-glow-faint", p => p.PrimaryGlowFaint, (p, v) => p with { PrimaryGlowFaint = v }),
		Palette(nameof(MokaPalette.OnSurfaceTertiary), "--moka-color-on-surface-tertiary", p => p.OnSurfaceTertiary, (p, v) => p with { OnSurfaceTertiary = v }),
		Palette(nameof(MokaPalette.OnSurfaceQuaternary), "--moka-color-on-surface-quaternary", p => p.OnSurfaceQuaternary, (p, v) => p with { OnSurfaceQuaternary = v }),
		Palette(nameof(MokaPalette.SurfaceRaised), "--moka-color-surface-raised", p => p.SurfaceRaised, (p, v) => p with { SurfaceRaised = v }),
		Palette(nameof(MokaPalette.NeutralBg), "--moka-color-neutral-bg", p => p.NeutralBg, (p, v) => p with { NeutralBg = v }),
		Palette(nameof(MokaPalette.OutlineStrong), "--moka-color-outline-strong", p => p.OutlineStrong, (p, v) => p with { OutlineStrong = v }),
		Palette(nameof(MokaPalette.OutlineHover), "--moka-color-outline-hover", p => p.OutlineHover, (p, v) => p with { OutlineHover = v }),
		Palette(nameof(MokaPalette.Backdrop), "--moka-color-backdrop", p => p.Backdrop, (p, v) => p with { Backdrop = v }),
		Palette(nameof(MokaPalette.OnSurface2), "--moka-color-on-surface-2", p => p.OnSurface2, (p, v) => p with { OnSurface2 = v }),
		Palette(nameof(MokaPalette.Icon), "--moka-color-icon", p => p.Icon, (p, v) => p with { Icon = v }),
		Palette(nameof(MokaPalette.Lift), "--moka-color-lift", p => p.Lift, (p, v) => p with { Lift = v }),
		Palette(nameof(MokaPalette.Knob), "--moka-color-knob", p => p.Knob, (p, v) => p with { Knob = v }),
		Palette(nameof(MokaPalette.KnobShadow), "--moka-shadow-knob", p => p.KnobShadow, (p, v) => p with { KnobShadow = v }),
		Palette(nameof(MokaPalette.Paper), "--moka-color-paper", p => p.Paper, (p, v) => p with { Paper = v }),
		Palette(nameof(MokaPalette.PaperEdge), "--moka-color-paper-edge", p => p.PaperEdge, (p, v) => p with { PaperEdge = v }),
		Palette(nameof(MokaPalette.PrimaryBg), "--moka-color-primary-bg", p => p.PrimaryBg, (p, v) => p with { PrimaryBg = v }),
		Palette(nameof(MokaPalette.PrimaryRow), "--moka-color-primary-row", p => p.PrimaryRow, (p, v) => p with { PrimaryRow = v }),
		Palette(nameof(MokaPalette.PrimaryRing), "--moka-color-primary-ring", p => p.PrimaryRing, (p, v) => p with { PrimaryRing = v }),
		Palette(nameof(MokaPalette.PrimaryPill), "--moka-color-primary-pill", p => p.PrimaryPill, (p, v) => p with { PrimaryPill = v }),
		Palette(nameof(MokaPalette.PrimaryFocus), "--moka-color-primary-focus", p => p.PrimaryFocus, (p, v) => p with { PrimaryFocus = v }),
		Palette(nameof(MokaPalette.PrimaryText), "--moka-color-primary-text", p => p.PrimaryText, (p, v) => p with { PrimaryText = v }),
		Palette(nameof(MokaPalette.SuccessBg), "--moka-color-success-bg", p => p.SuccessBg, (p, v) => p with { SuccessBg = v }),
		Palette(nameof(MokaPalette.SuccessInk), "--moka-color-success-ink", p => p.SuccessInk, (p, v) => p with { SuccessInk = v }),
		Palette(nameof(MokaPalette.SuccessText), "--moka-color-success-text", p => p.SuccessText, (p, v) => p with { SuccessText = v }),
		Palette(nameof(MokaPalette.SuccessLine), "--moka-color-success-line", p => p.SuccessLine, (p, v) => p with { SuccessLine = v }),
		Palette(nameof(MokaPalette.WarningBg), "--moka-color-warning-bg", p => p.WarningBg, (p, v) => p with { WarningBg = v }),
		Palette(nameof(MokaPalette.WarningInk), "--moka-color-warning-ink", p => p.WarningInk, (p, v) => p with { WarningInk = v }),
		Palette(nameof(MokaPalette.WarningText), "--moka-color-warning-text", p => p.WarningText, (p, v) => p with { WarningText = v }),
		Palette(nameof(MokaPalette.WarningLine), "--moka-color-warning-line", p => p.WarningLine, (p, v) => p with { WarningLine = v }),
		Palette(nameof(MokaPalette.ErrorBg), "--moka-color-error-bg", p => p.ErrorBg, (p, v) => p with { ErrorBg = v }),
		Palette(nameof(MokaPalette.ErrorInk), "--moka-color-error-ink", p => p.ErrorInk, (p, v) => p with { ErrorInk = v }),
		Palette(nameof(MokaPalette.ErrorText), "--moka-color-error-text", p => p.ErrorText, (p, v) => p with { ErrorText = v }),
		Palette(nameof(MokaPalette.ErrorLine), "--moka-color-error-line", p => p.ErrorLine, (p, v) => p with { ErrorLine = v }),
		Palette(nameof(MokaPalette.ErrorSoft), "--moka-color-error-soft", p => p.ErrorSoft, (p, v) => p with { ErrorSoft = v }),
		Palette(nameof(MokaPalette.InfoBg), "--moka-color-info-bg", p => p.InfoBg, (p, v) => p with { InfoBg = v }),
		Palette(nameof(MokaPalette.InfoInk), "--moka-color-info-ink", p => p.InfoInk, (p, v) => p with { InfoInk = v }),
		Palette(nameof(MokaPalette.InfoText), "--moka-color-info-text", p => p.InfoText, (p, v) => p with { InfoText = v }),
		Palette(nameof(MokaPalette.InfoLine), "--moka-color-info-line", p => p.InfoLine, (p, v) => p with { InfoLine = v })
	];

	/// <summary>The typography's properties, in declaration order.</summary>
	public static IReadOnlyList<MokaThemeProperty<MokaTypography>> TypographyProperties { get; } =
	[
		Type(nameof(MokaTypography.FontFamily), "--moka-font-family", t => t.FontFamily, (t, v) => t with { FontFamily = v }),
		Type(nameof(MokaTypography.FontFamilyMono), "--moka-font-family-mono", t => t.FontFamilyMono, (t, v) => t with { FontFamilyMono = v }),
		Type(nameof(MokaTypography.FontFamilyDisplay), "--moka-font-family-display", t => t.FontFamilyDisplay, (t, v) => t with { FontFamilyDisplay = v }),
		Type(nameof(MokaTypography.FontFamilyNumber), "--moka-font-family-number", t => t.FontFamilyNumber, (t, v) => t with { FontFamilyNumber = v }),
		Type(nameof(MokaTypography.FontFamilyFigure), "--moka-font-family-figure", t => t.FontFamilyFigure, (t, v) => t with { FontFamilyFigure = v }),
		Type(nameof(MokaTypography.FontSizeXxs), "--moka-font-size-xxs", t => t.FontSizeXxs, (t, v) => t with { FontSizeXxs = v }),
		Type(nameof(MokaTypography.FontSizeXs), "--moka-font-size-xs", t => t.FontSizeXs, (t, v) => t with { FontSizeXs = v }),
		Type(nameof(MokaTypography.FontSizeSm), "--moka-font-size-sm", t => t.FontSizeSm, (t, v) => t with { FontSizeSm = v }),
		Type(nameof(MokaTypography.FontSizeBase), "--moka-font-size-base", t => t.FontSizeBase, (t, v) => t with { FontSizeBase = v }),
		Type(nameof(MokaTypography.FontSizeMd), "--moka-font-size-md", t => t.FontSizeMd, (t, v) => t with { FontSizeMd = v }),
		Type(nameof(MokaTypography.FontSizeLg), "--moka-font-size-lg", t => t.FontSizeLg, (t, v) => t with { FontSizeLg = v }),
		Type(nameof(MokaTypography.FontSizeXl), "--moka-font-size-xl", t => t.FontSizeXl, (t, v) => t with { FontSizeXl = v }),
		Type(nameof(MokaTypography.FontSizeXxl), "--moka-font-size-xxl", t => t.FontSizeXxl, (t, v) => t with { FontSizeXxl = v }),
		Type(nameof(MokaTypography.FontSizeDisplay), "--moka-font-size-display", t => t.FontSizeDisplay, (t, v) => t with { FontSizeDisplay = v }),
		Type(nameof(MokaTypography.FontSizeRoot), "--moka-font-size-root", t => t.FontSizeRoot, (t, v) => t with { FontSizeRoot = v }),
		Type(nameof(MokaTypography.FontSizeHeading), "--moka-font-size-heading", t => t.FontSizeHeading, (t, v) => t with { FontSizeHeading = v }),
		Type(nameof(MokaTypography.FontSizeCardTitle), "--moka-font-size-card-title", t => t.FontSizeCardTitle, (t, v) => t with { FontSizeCardTitle = v }),
		Type(nameof(MokaTypography.FontSizeDialog), "--moka-font-size-dialog", t => t.FontSizeDialog, (t, v) => t with { FontSizeDialog = v }),
		Type(nameof(MokaTypography.FontSizeMenu), "--moka-font-size-menu", t => t.FontSizeMenu, (t, v) => t with { FontSizeMenu = v }),
		Type(nameof(MokaTypography.FontSizeMono), "--moka-font-size-mono", t => t.FontSizeMono, (t, v) => t with { FontSizeMono = v }),
		Type(nameof(MokaTypography.LineHeightTight), "--moka-line-height-tight", t => t.LineHeightTight, (t, v) => t with { LineHeightTight = v }),
		Type(nameof(MokaTypography.LineHeightBase), "--moka-line-height-base", t => t.LineHeightBase, (t, v) => t with { LineHeightBase = v }),
		Type(nameof(MokaTypography.LineHeightRelaxed), "--moka-line-height-relaxed", t => t.LineHeightRelaxed, (t, v) => t with { LineHeightRelaxed = v }),
		Type(nameof(MokaTypography.FontWeightLight), "--moka-font-weight-light", t => t.FontWeightLight, (t, v) => t with { FontWeightLight = v }),
		Type(nameof(MokaTypography.FontWeightNormal), "--moka-font-weight-normal", t => t.FontWeightNormal, (t, v) => t with { FontWeightNormal = v }),
		Type(nameof(MokaTypography.FontWeightMedium), "--moka-font-weight-medium", t => t.FontWeightMedium, (t, v) => t with { FontWeightMedium = v }),
		Type(nameof(MokaTypography.FontWeightSemibold), "--moka-font-weight-semibold", t => t.FontWeightSemibold, (t, v) => t with { FontWeightSemibold = v }),
		Type(nameof(MokaTypography.FontWeightBold), "--moka-font-weight-bold", t => t.FontWeightBold, (t, v) => t with { FontWeightBold = v }),
		Type(nameof(MokaTypography.FontWeightHeading), "--moka-font-weight-heading", t => t.FontWeightHeading, (t, v) => t with { FontWeightHeading = v }),
		Type(nameof(MokaTypography.ControlCase), "--moka-control-case", t => t.ControlCase, (t, v) => t with { ControlCase = v }),
		Type(nameof(MokaTypography.ControlTracking), "--moka-control-tracking", t => t.ControlTracking, (t, v) => t with { ControlTracking = v }),
		Type(nameof(MokaTypography.TitleCase), "--moka-title-case", t => t.TitleCase, (t, v) => t with { TitleCase = v }),
		Type(nameof(MokaTypography.TitleTracking), "--moka-title-tracking", t => t.TitleTracking, (t, v) => t with { TitleTracking = v }),
		Type(nameof(MokaTypography.HeadingTracking), "--moka-heading-tracking", t => t.HeadingTracking, (t, v) => t with { HeadingTracking = v }),
		Type(nameof(MokaTypography.CardTitleCase), "--moka-card-title-case", t => t.CardTitleCase, (t, v) => t with { CardTitleCase = v }),
		Type(nameof(MokaTypography.CardTitleTracking), "--moka-card-title-tracking", t => t.CardTitleTracking, (t, v) => t with { CardTitleTracking = v }),
		Type(nameof(MokaTypography.ChartTitleTracking), "--moka-chart-title-tracking", t => t.ChartTitleTracking, (t, v) => t with { ChartTitleTracking = v }),
		Type(nameof(MokaTypography.NumberTracking), "--moka-number-tracking", t => t.NumberTracking, (t, v) => t with { NumberTracking = v })
	];

	/// <summary>The spacing's properties, in declaration order.</summary>
	public static IReadOnlyList<MokaThemeProperty<MokaSpacing>> SpacingProperties { get; } =
	[
		Space(nameof(MokaSpacing.Xxs), "--moka-spacing-xxs", s => s.Xxs, (s, v) => s with { Xxs = v }),
		Space(nameof(MokaSpacing.Xs), "--moka-spacing-xs", s => s.Xs, (s, v) => s with { Xs = v }),
		Space(nameof(MokaSpacing.Sm), "--moka-spacing-sm", s => s.Sm, (s, v) => s with { Sm = v }),
		Space(nameof(MokaSpacing.Md), "--moka-spacing-md", s => s.Md, (s, v) => s with { Md = v }),
		Space(nameof(MokaSpacing.Lg), "--moka-spacing-lg", s => s.Lg, (s, v) => s with { Lg = v }),
		Space(nameof(MokaSpacing.Xl), "--moka-spacing-xl", s => s.Xl, (s, v) => s with { Xl = v }),
		Space(nameof(MokaSpacing.Xxl), "--moka-spacing-xxl", s => s.Xxl, (s, v) => s with { Xxl = v }),
		Space(nameof(MokaSpacing.RadiusNone), "--moka-radius-none", s => s.RadiusNone, (s, v) => s with { RadiusNone = v }),
		Space(nameof(MokaSpacing.RadiusSm), "--moka-radius-sm", s => s.RadiusSm, (s, v) => s with { RadiusSm = v }),
		Space(nameof(MokaSpacing.RadiusMd), "--moka-radius-md", s => s.RadiusMd, (s, v) => s with { RadiusMd = v }),
		Space(nameof(MokaSpacing.RadiusLg), "--moka-radius-lg", s => s.RadiusLg, (s, v) => s with { RadiusLg = v }),
		Space(nameof(MokaSpacing.RadiusXl), "--moka-radius-xl", s => s.RadiusXl, (s, v) => s with { RadiusXl = v }),
		Space(nameof(MokaSpacing.RadiusFull), "--moka-radius-full", s => s.RadiusFull, (s, v) => s with { RadiusFull = v }),
		Space(nameof(MokaSpacing.RadiusCard), "--moka-radius-card", s => s.RadiusCard, (s, v) => s with { RadiusCard = v }),
		Space(nameof(MokaSpacing.RadiusPopup), "--moka-radius-popup", s => s.RadiusPopup, (s, v) => s with { RadiusPopup = v }),
		Space(nameof(MokaSpacing.PageGap), "--moka-page-gap", s => s.PageGap, (s, v) => s with { PageGap = v }),
		Space(nameof(MokaSpacing.CardPadding), "--moka-card-padding", s => s.CardPadding, (s, v) => s with { CardPadding = v }),
		Space(nameof(MokaSpacing.CardGap), "--moka-card-gap", s => s.CardGap, (s, v) => s with { CardGap = v }),
		Space(nameof(MokaSpacing.GridGap), "--moka-grid-gap", s => s.GridGap, (s, v) => s with { GridGap = v }),
		Space(nameof(MokaSpacing.PanelPadding), "--moka-panel-padding", s => s.PanelPadding, (s, v) => s with { PanelPadding = v }),
		Space(nameof(MokaSpacing.InnerPadding), "--moka-inner-padding", s => s.InnerPadding, (s, v) => s with { InnerPadding = v }),
		Space(nameof(MokaSpacing.RowPadding), "--moka-row-padding", s => s.RowPadding, (s, v) => s with { RowPadding = v }),
		Space(nameof(MokaSpacing.ItemPadding), "--moka-item-padding", s => s.ItemPadding, (s, v) => s with { ItemPadding = v }),
		Space(nameof(MokaSpacing.ControlHeight), "--moka-control-height", s => s.ControlHeight, (s, v) => s with { ControlHeight = v }),
		Space(nameof(MokaSpacing.MainPadding), "--moka-main-padding", s => s.MainPadding, (s, v) => s with { MainPadding = v }),
		Space(nameof(MokaSpacing.MenuRow), "--moka-menu-row", s => s.MenuRow, (s, v) => s with { MenuRow = v }),
		Space(nameof(MokaSpacing.ButtonHeight), "--moka-button-height", s => s.ButtonHeight, (s, v) => s with { ButtonHeight = v }),
		Space(nameof(MokaSpacing.ButtonHeightSm), "--moka-button-height-sm", s => s.ButtonHeightSm, (s, v) => s with { ButtonHeightSm = v }),
		Space(nameof(MokaSpacing.ButtonHeightLg), "--moka-button-height-lg", s => s.ButtonHeightLg, (s, v) => s with { ButtonHeightLg = v }),
		Space(nameof(MokaSpacing.ButtonPadding), "--moka-button-padding", s => s.ButtonPadding, (s, v) => s with { ButtonPadding = v }),
		Space(nameof(MokaSpacing.ButtonPaddingSm), "--moka-button-padding-sm", s => s.ButtonPaddingSm, (s, v) => s with { ButtonPaddingSm = v }),
		Space(nameof(MokaSpacing.InputPadding), "--moka-input-padding", s => s.InputPadding, (s, v) => s with { InputPadding = v }),
		Space(nameof(MokaSpacing.SegmentHeight), "--moka-segment-height", s => s.SegmentHeight, (s, v) => s with { SegmentHeight = v }),
		Space(nameof(MokaSpacing.CellPadding), "--moka-cell-padding", s => s.CellPadding, (s, v) => s with { CellPadding = v }),
		Space(nameof(MokaSpacing.PillPadding), "--moka-pill-padding", s => s.PillPadding, (s, v) => s with { PillPadding = v }),
		Space(nameof(MokaSpacing.ToolbarHeight), "--moka-height-toolbar", s => s.ToolbarHeight, (s, v) => s with { ToolbarHeight = v }),
		Space(nameof(MokaSpacing.ToolbarHeightDense), "--moka-height-toolbar-dense", s => s.ToolbarHeightDense, (s, v) => s with { ToolbarHeightDense = v }),
		Space(nameof(MokaSpacing.StatusBarHeight), "--moka-height-statusbar", s => s.StatusBarHeight, (s, v) => s with { StatusBarHeight = v })
	];

	/// <summary>Every token, in the order a theme writes them.</summary>
	public static IReadOnlyList<MokaToken> All { get; } = BuildAll();

	private static readonly FrozenDictionary<string, MokaToken> ByName = All.ToFrozenDictionary(token => token.Name, StringComparer.Ordinal);

	private static readonly FrozenDictionary<string, int> Positions =
		All.Select((token, index) => (token.Name, index)).ToFrozenDictionary(entry => entry.Name, entry => entry.index, StringComparer.Ordinal);

	internal static FrozenDictionary<string, (string Light, string? Dark)> Fallbacks => FallbackTable.Value;

	/// <summary>Finds a token by its name, such as <c>--moka-color-primary</c>.</summary>
	/// <param name="name">The custom property's name.</param>
	/// <param name="token">The token, when there is one by that name.</param>
	/// <returns><c>true</c> when a theme writes a token by that name.</returns>
	public static bool TryFind([NotNullWhen(true)] string? name, [NotNullWhen(true)] out MokaToken? token)
	{
		token = null;
		return name is not null && ByName.TryGetValue(name, out token);
	}

	// A token's position in All, or -1 for a custom property the table doesn't hold.
	internal static int IndexOf(string name) => Positions.TryGetValue(name, out int index) ? index : -1;

	/// <summary>The custom properties a value reads through <c>var()</c>, such as <c>--moka-color-primary</c>.</summary>
	/// <param name="value">A CSS value.</param>
	/// <returns>The names, in the order they appear.</returns>
	public static IEnumerable<string> References(string value) =>
		VariableReference().Matches(value).Select(match => match.Groups["name"].Value);

	private static MokaThemeProperty<MokaPalette> Palette(string name, string token, Func<MokaPalette, string> get, Func<MokaPalette, string, MokaPalette> set) =>
		new(name, token, get, set);

	private static MokaThemeProperty<MokaTypography> Type(string name, string token, Func<MokaTypography, string> get, Func<MokaTypography, string, MokaTypography> set) =>
		new(name, token, get, set);

	private static MokaThemeProperty<MokaSpacing> Space(string name, string token, Func<MokaSpacing, string> get, Func<MokaSpacing, string, MokaSpacing> set) =>
		new(name, token, get, set);

	private static FrozenDictionary<string, (string Light, string? Dark)> BuildFallbacks()
	{
		IReadOnlyDictionary<string, string> light = MokaTheme.Light.ToTokenMap();
		IReadOnlyDictionary<string, string> dark = MokaTheme.Dark.ToTokenMap();

		HashSet<string> differ = [.. All.Where(token => light[token.Name] != dark[token.Name]).Select(token => token.Name)];
		bool grew = true;
		while (grew)
		{
			grew = false;
			foreach (MokaToken token in All)
			{
				if (!differ.Contains(token.Name) && References(dark[token.Name]).Any(differ.Contains))
				{
					differ.Add(token.Name);
					grew = true;
				}
			}
		}

		return All.ToFrozenDictionary(
			token => token.Name,
			token => (light[token.Name], differ.Contains(token.Name) ? dark[token.Name] : null),
			StringComparer.Ordinal);
	}

	[GeneratedRegex(@"var\(\s*(?<name>--[A-Za-z0-9_-]+)")]
	private static partial Regex VariableReference();

	private static MokaToken Token(string name, MokaTokenGroup group, MokaTokenKind kind, string description, Func<MokaTokenContext, string> produce) =>
		new(name, group, kind, description, produce);

	private static MokaToken FromPalette(string name, MokaTokenGroup group, MokaTokenKind kind, string description)
	{
		MokaThemeProperty<MokaPalette> property = PaletteProperties.Single(p => p.Token == name);
		return new MokaToken(name, group, kind, description, context => property.Get(context.Palette));
	}

	private static MokaToken Color(string name, string description) => FromPalette(name, MokaTokenGroup.Color, MokaTokenKind.Color, description);

	private static MokaToken FromTypography(string name, MokaTokenKind kind, string description, bool scaled = false)
	{
		MokaThemeProperty<MokaTypography> property = TypographyProperties.Single(p => p.Token == name);
		return new MokaToken(name, MokaTokenGroup.Typography, kind, description, scaled
			? context => MokaTokenContext.Scale(property.Get(context.Typography), context.Theme.FontScale)
			: context => property.Get(context.Typography));
	}

	private static MokaToken FromSpacing(string name, MokaTokenGroup group, MokaTokenKind kind, string description, bool scaled = false)
	{
		MokaThemeProperty<MokaSpacing> property = SpacingProperties.Single(p => p.Token == name);
		return new MokaToken(name, group, kind, description, scaled
			? context => MokaTokenContext.Scale(property.Get(context.Spacing), context.Theme.Density)
			: context => property.Get(context.Spacing));
	}

	private static List<MokaToken> BuildAll()
	{
		List<MokaToken> all =
		[
			Color("--moka-color-primary", "The accent: main buttons, links, focus and selection."),
			Color("--moka-color-primary-light", "A lighter accent shade, for hover states and accent text."),
			Color("--moka-color-primary-dark", "A darker accent shade, for pressed states."),
			Color("--moka-color-on-primary", "Text and icons on the accent."),
			Color("--moka-color-secondary", "The second accent, where a component takes the secondary color."),
			Color("--moka-color-secondary-light", "A lighter shade of the second accent."),
			Color("--moka-color-secondary-dark", "A darker shade of the second accent, for hover and pressed states."),
			Color("--moka-color-on-secondary", "Text and icons on the second accent."),
			Color("--moka-color-surface", "Cards, panels and fields."),
			Color("--moka-color-surface-variant", "A surface set apart, such as a header row or a well."),
			Color("--moka-color-on-surface", "Text and icons on a surface."),
			Color("--moka-color-on-surface-variant", "Secondary text and icons, such as helper text."),
			Color("--moka-color-background", "The page behind every surface."),
			Color("--moka-color-on-background", "Text on the page."),
			Color("--moka-color-error", "Errors and destructive actions."),
			Color("--moka-color-on-error", "Text and icons on the error color."),
			Color("--moka-color-warning", "Warnings."),
			Color("--moka-color-on-warning", "Text and icons on the warning color."),
			Color("--moka-color-success", "Success and healthy states."),
			Color("--moka-color-on-success", "Text and icons on the success color."),
			Color("--moka-color-info", "Information and neutral notices."),
			Color("--moka-color-on-info", "Text and icons on the info color."),
			Color("--moka-color-error-dim", "A faint error fill behind chips, pills and soft badges."),
			Color("--moka-color-warning-dim", "A faint warning fill behind chips, pills and soft badges."),
			Color("--moka-color-success-dim", "A faint success fill behind chips, pills and soft badges."),
			Color("--moka-color-info-dim", "A faint info fill behind chips, pills and soft badges."),
			Color("--moka-color-outline", "Borders of fields and controls."),
			Color("--moka-color-outline-variant", "Quieter borders and dividers, such as card edges and table rules."),
			Color("--moka-color-surface-hover", "A surface under the pointer."),
			Color("--moka-color-surface-2", "A surface a step from the card, such as a suggestion list or a code block."),
			Color("--moka-color-surface-3", "A surface two steps from the card, such as a tooltip."),
			Color("--moka-color-primary-glow", "A faint accent tint for focus rings and selected rows."),
			Color("--moka-color-primary-glow-md", "A medium accent tint for selected items and accent pills."),
			Color("--moka-color-primary-glow-strong", "A strong accent tint for glows around what is active."),
			Color("--moka-color-primary-border", "A border in the accent around what is selected or focused."),
			Color("--moka-color-primary-border-dim", "A faint border in the accent."),
			Color("--moka-color-primary-glow-faint", "The faintest accent tint, for large areas such as a background grid."),
			Color("--moka-color-on-surface-tertiary", "Text a step quieter than secondary, such as captions and timestamps."),
			Color("--moka-color-on-surface-quaternary", "The quietest text and marks, and the color of things that are off."),
			Color("--moka-color-surface-raised", "Row hover and floating bars."),
			Color("--moka-color-neutral-bg", "Neutral pills and the selected segment."),
			Color("--moka-color-outline-strong", "Borders that must show, such as buttons and key caps."),
			Color("--moka-color-outline-hover", "A border under the pointer."),
			Color("--moka-color-on-surface-2", "Text a step quieter than body text, such as field labels."),
			Color("--moka-color-icon", "Icons that are not the point of what they sit in."),
			Color("--moka-color-lift", "What hover states mix toward for more contrast."),
			Color("--moka-color-knob", "The thumb of a switch."),
			FromPalette("--moka-shadow-knob", MokaTokenGroup.Elevation, MokaTokenKind.Other, "The shadow under the thumb of a switch."),
			Color("--moka-color-paper", "What must stay light in every theme, such as a QR code's background."),
			Color("--moka-color-paper-edge", "The edge of paper, where it could not otherwise be told from its surface."),
			Color("--moka-color-primary-bg", "The tint behind something selected."),
			Color("--moka-color-primary-row", "The active row."),
			Color("--moka-color-primary-ring", "A ring in the accent around the active row."),
			Color("--moka-color-primary-pill", "A pill in the accent."),
			Color("--moka-color-primary-focus", "The focus ring's color."),
			Color("--moka-color-primary-text", "Accent text on the selected tint."),
			Color("--moka-color-success-bg", "The tint behind something that succeeded."),
			Color("--moka-color-success-ink", "Short text on the success tint."),
			Color("--moka-color-success-text", "Longer text on the success tint."),
			Color("--moka-color-success-line", "A border on the success tint."),
			Color("--moka-color-warning-bg", "The tint behind a warning."),
			Color("--moka-color-warning-ink", "Short text on the warning tint."),
			Color("--moka-color-warning-text", "Longer text on the warning tint."),
			Color("--moka-color-warning-line", "A border on the warning tint."),
			Color("--moka-color-error-bg", "The tint behind an error."),
			Color("--moka-color-error-ink", "Short text on the error tint."),
			Color("--moka-color-error-text", "Longer text on the error tint."),
			Color("--moka-color-error-line", "A border on the error tint."),
			Color("--moka-color-error-soft", "Counts, danger links and field errors, read as text on a surface."),
			Color("--moka-color-info-bg", "The tint behind information."),
			Color("--moka-color-info-ink", "Short text on the info tint."),
			Color("--moka-color-info-text", "Longer text on the info tint."),
			Color("--moka-color-info-line", "A border on the info tint.")
		];

		for (int i = 0; i < MokaLabelPalette.Names.Count; i++)
		{
			int index = i;
			string hue = MokaLabelColors.Key(MokaLabelColors.All[i / 3]);
			all.Add(Token(MokaLabelPalette.Names[i], MokaTokenGroup.Label, MokaTokenKind.Color,
				string.Format(CultureInfo.InvariantCulture, LabelDescriptions[i % 3], hue), context => context.Labels[index]));
		}

		for (int i = 0; i < MokaChartPaletteValues.TokenNames.Count; i++)
		{
			int index = i;
			string name = MokaChartPaletteValues.TokenNames[i];
			all.Add(Token(name, MokaTokenGroup.Chart, MokaTokenKind.Color, ChartDescription(name), context => context.Chart[index]));
		}

		all.AddRange(CodeTokens());

		all.AddRange(
		[
			FromTypography("--moka-font-family", MokaTokenKind.Other, "The text font."),
			FromTypography("--moka-font-family-mono", MokaTokenKind.Other, "The monospace font for code, addresses and ids."),
			FromTypography("--moka-font-family-display", MokaTokenKind.Other, "The font of page titles."),
			FromTypography("--moka-font-family-number", MokaTokenKind.Other, "The font of big numbers, such as a stat's value."),
			FromTypography("--moka-font-family-figure", MokaTokenKind.Other, "The font of numbers in small places, such as a fact tile."),
			FromTypography("--moka-font-size-xxs", MokaTokenKind.Length, "The smallest text size.", scaled: true),
			FromTypography("--moka-font-size-xs", MokaTokenKind.Length, "Captions and micro labels.", scaled: true),
			FromTypography("--moka-font-size-sm", MokaTokenKind.Length, "Helper text and small controls.", scaled: true),
			FromTypography("--moka-font-size-base", MokaTokenKind.Length, "Body text.", scaled: true),
			FromTypography("--moka-font-size-md", MokaTokenKind.Length, "A step above body text.", scaled: true),
			FromTypography("--moka-font-size-lg", MokaTokenKind.Length, "Card titles and lead text.", scaled: true),
			FromTypography("--moka-font-size-xl", MokaTokenKind.Length, "Section titles.", scaled: true),
			FromTypography("--moka-font-size-xxl", MokaTokenKind.Length, "Large titles.", scaled: true),
			FromTypography("--moka-font-size-display", MokaTokenKind.Length, "The largest text, for big numbers and hero titles.", scaled: true),
			FromTypography("--moka-font-size-root", MokaTokenKind.Length, "The size text gets when nothing else sets one.", scaled: true),
			FromTypography("--moka-font-size-heading", MokaTokenKind.Length, "Page titles and level 1 headings.", scaled: true),
			FromTypography("--moka-font-size-card-title", MokaTokenKind.Length, "A card's title.", scaled: true),
			FromTypography("--moka-font-size-dialog", MokaTokenKind.Length, "A dialog's title.", scaled: true),
			FromTypography("--moka-font-size-menu", MokaTokenKind.Length, "A row in a side menu.", scaled: true),
			FromTypography("--moka-font-size-mono", MokaTokenKind.Length, "An address or a key among other text.", scaled: true),
			FromTypography("--moka-line-height-tight", MokaTokenKind.Other, "The line height of headings and single-line controls."),
			FromTypography("--moka-line-height-base", MokaTokenKind.Other, "The line height of body text."),
			FromTypography("--moka-line-height-relaxed", MokaTokenKind.Other, "The line height of long passages."),
			FromTypography("--moka-font-weight-light", MokaTokenKind.Other, "The light weight."),
			FromTypography("--moka-font-weight-normal", MokaTokenKind.Other, "The regular weight."),
			FromTypography("--moka-font-weight-medium", MokaTokenKind.Other, "The medium weight."),
			FromTypography("--moka-font-weight-semibold", MokaTokenKind.Other, "The semibold weight."),
			FromTypography("--moka-font-weight-bold", MokaTokenKind.Other, "The bold weight."),
			FromTypography("--moka-font-weight-heading", MokaTokenKind.Other, "The weight of headings at levels 1 to 3."),
			FromTypography("--moka-control-case", MokaTokenKind.Other, "The letter case of buttons, chips and tags."),
			FromTypography("--moka-control-tracking", MokaTokenKind.Length, "The letter spacing of buttons, chips and tags."),
			FromTypography("--moka-title-case", MokaTokenKind.Other, "The letter case of page, dialog, drawer and sheet titles."),
			FromTypography("--moka-title-tracking", MokaTokenKind.Length, "The letter spacing of dialog, drawer and sheet titles."),
			FromTypography("--moka-heading-tracking", MokaTokenKind.Length, "The letter spacing of page titles and big numbers."),
			FromTypography("--moka-card-title-case", MokaTokenKind.Other, "The letter case of card, panel and chart titles."),
			FromTypography("--moka-card-title-tracking", MokaTokenKind.Length, "The letter spacing of card and panel titles."),
			FromTypography("--moka-chart-title-tracking", MokaTokenKind.Length, "The letter spacing of a chart's title."),
			FromTypography("--moka-number-tracking", MokaTokenKind.Length, "The letter spacing of big numbers."),

			FromSpacing("--moka-spacing-xxs", MokaTokenGroup.Spacing, MokaTokenKind.Length, "The smallest spacing step.", scaled: true),
			FromSpacing("--moka-spacing-xs", MokaTokenGroup.Spacing, MokaTokenKind.Length, "Spacing step xs.", scaled: true),
			FromSpacing("--moka-spacing-sm", MokaTokenGroup.Spacing, MokaTokenKind.Length, "Spacing step sm.", scaled: true),
			FromSpacing("--moka-spacing-md", MokaTokenGroup.Spacing, MokaTokenKind.Length, "Spacing step md.", scaled: true),
			FromSpacing("--moka-spacing-lg", MokaTokenGroup.Spacing, MokaTokenKind.Length, "Spacing step lg.", scaled: true),
			FromSpacing("--moka-spacing-xl", MokaTokenGroup.Spacing, MokaTokenKind.Length, "Spacing step xl.", scaled: true),
			FromSpacing("--moka-spacing-xxl", MokaTokenGroup.Spacing, MokaTokenKind.Length, "The largest spacing step.", scaled: true),
			Token("--moka-density", MokaTokenGroup.Spacing, MokaTokenKind.Other, "The density factor the spacing scale is multiplied by, for an app's own calc().",
				context => context.Theme.Density.ToString("0.###", CultureInfo.InvariantCulture)),
			Token("--moka-font-scale", MokaTokenGroup.Spacing, MokaTokenKind.Other, "The factor every text size is multiplied by, for an app's own calc().",
				context => context.Theme.FontScale.ToString("0.###", CultureInfo.InvariantCulture)),

			FromSpacing("--moka-radius-none", MokaTokenGroup.Radius, MokaTokenKind.Length, "Square corners."),
			FromSpacing("--moka-radius-sm", MokaTokenGroup.Radius, MokaTokenKind.Length, "Small corners for chips, badges and small buttons."),
			FromSpacing("--moka-radius-md", MokaTokenGroup.Radius, MokaTokenKind.Length, "The corners of buttons and fields."),
			FromSpacing("--moka-radius-lg", MokaTokenGroup.Radius, MokaTokenKind.Length, "Large corners for list rows and panels."),
			FromSpacing("--moka-radius-xl", MokaTokenGroup.Radius, MokaTokenKind.Length, "The largest corners."),
			FromSpacing("--moka-radius-full", MokaTokenGroup.Radius, MokaTokenKind.Length, "Fully rounded ends for pills and circles."),
			FromSpacing("--moka-radius-card", MokaTokenGroup.Radius, MokaTokenKind.Length, "The corners of a card."),
			FromSpacing("--moka-radius-popup", MokaTokenGroup.Radius, MokaTokenKind.Length, "The corners of dialogs, popovers and menus."),

			FromSpacing("--moka-page-gap", MokaTokenGroup.Layout, MokaTokenKind.Length, "The gap between the blocks of a page."),
			FromSpacing("--moka-card-padding", MokaTokenGroup.Layout, MokaTokenKind.Length, "The padding of a card."),
			FromSpacing("--moka-card-gap", MokaTokenGroup.Layout, MokaTokenKind.Length, "The gap between the parts of a card."),
			FromSpacing("--moka-grid-gap", MokaTokenGroup.Layout, MokaTokenKind.Length, "The gap between the cells of a grid of cards."),
			FromSpacing("--moka-panel-padding", MokaTokenGroup.Layout, MokaTokenKind.Length, "The padding of a panel inside a card."),
			FromSpacing("--moka-inner-padding", MokaTokenGroup.Layout, MokaTokenKind.Length, "The padding of an inner card."),
			FromSpacing("--moka-row-padding", MokaTokenGroup.Layout, MokaTokenKind.Length, "The padding of a list row."),
			FromSpacing("--moka-item-padding", MokaTokenGroup.Layout, MokaTokenKind.Length, "The padding of a small item in a list."),
			FromSpacing("--moka-control-height", MokaTokenGroup.Layout, MokaTokenKind.Length, "The height of a medium control."),
			Token("--moka-control-height-xs", MokaTokenGroup.Layout, MokaTokenKind.Length, "The height of an extra small control.",
				_ => "calc(var(--moka-control-height) - var(--moka-spacing-xl))"),
			Token("--moka-control-height-sm", MokaTokenGroup.Layout, MokaTokenKind.Length, "The height of a small control.",
				_ => "calc(var(--moka-control-height) - var(--moka-spacing-md))"),
			Token("--moka-control-height-lg", MokaTokenGroup.Layout, MokaTokenKind.Length, "The height of a large control.",
				_ => "calc(var(--moka-control-height) + var(--moka-spacing-md))"),
			FromSpacing("--moka-main-padding", MokaTokenGroup.Layout, MokaTokenKind.Length, "The padding of a page's main area."),
			Token("--moka-main-padding-top", MokaTokenGroup.Layout, MokaTokenKind.Length, "The top of the main area's padding, for a page's own calc().",
				context => context.MainPaddingTop),
			Token("--moka-main-padding-bottom", MokaTokenGroup.Layout, MokaTokenKind.Length, "The bottom of the main area's padding, for a page's own calc().",
				context => context.MainPaddingBottom),
			FromSpacing("--moka-menu-row", MokaTokenGroup.Layout, MokaTokenKind.Length, "The height of a row in a side menu."),
			FromSpacing("--moka-button-height", MokaTokenGroup.Layout, MokaTokenKind.Length, "The height of a medium button on the new design."),
			FromSpacing("--moka-button-height-sm", MokaTokenGroup.Layout, MokaTokenKind.Length, "The height of a small button on the new design."),
			FromSpacing("--moka-button-height-lg", MokaTokenGroup.Layout, MokaTokenKind.Length, "The height of a large button on the new design."),
			FromSpacing("--moka-button-padding", MokaTokenGroup.Layout, MokaTokenKind.Length, "The side padding of a medium button."),
			FromSpacing("--moka-button-padding-sm", MokaTokenGroup.Layout, MokaTokenKind.Length, "The side padding of a small button."),
			FromSpacing("--moka-input-padding", MokaTokenGroup.Layout, MokaTokenKind.Length, "The side padding of a text input."),
			FromSpacing("--moka-segment-height", MokaTokenGroup.Layout, MokaTokenKind.Length, "The height of a segment in a segmented control."),
			FromSpacing("--moka-cell-padding", MokaTokenGroup.Layout, MokaTokenKind.Length, "The padding of a table cell."),
			FromSpacing("--moka-pill-padding", MokaTokenGroup.Layout, MokaTokenKind.Length, "The padding of a tag, a chip or a pill."),

			Token("--moka-shadow-0", MokaTokenGroup.Elevation, MokaTokenKind.Other, "No shadow.", _ => "none"),
			Token("--moka-shadow-1", MokaTokenGroup.Elevation, MokaTokenKind.Other, "Cards resting on the page.", context => context.Shadows.Card),
			Token("--moka-shadow-2", MokaTokenGroup.Elevation, MokaTokenKind.Other, "Raised cards and pressed-out controls.", context => context.Shadows.Raised),
			Token("--moka-shadow-3", MokaTokenGroup.Elevation, MokaTokenKind.Other, "Floating panels.", context => context.Shadows.Floating),
			Token("--moka-shadow-4", MokaTokenGroup.Elevation, MokaTokenKind.Other, "Floating bars.", context => context.Shadows.Bar),
			Token("--moka-shadow-popup", MokaTokenGroup.Elevation, MokaTokenKind.Other, "Popovers, menus and tooltips.", context => context.Shadows.Popup),
			Token("--moka-shadow-popup-lg", MokaTokenGroup.Elevation, MokaTokenKind.Other, "Large popups such as the command palette.", context => context.Shadows.PopupLarge),
			Token("--moka-shadow-modal", MokaTokenGroup.Elevation, MokaTokenKind.Other, "Dialogs and sheets.", context => context.Shadows.Modal),
			Token("--moka-shadow-subtle", MokaTokenGroup.Elevation, MokaTokenKind.Other, "A hint of depth under small things.", context => context.Shadows.Subtle),

			Token("--moka-transition-fast", MokaTokenGroup.Motion, MokaTokenKind.Other, "Hover and focus changes.", context => context.Gate ? "120ms ease-out" : "120ms ease"),
			Token("--moka-transition-normal", MokaTokenGroup.Motion, MokaTokenKind.Other, "Most state changes.", context => context.Gate ? "150ms ease-out" : "150ms ease"),
			Token("--moka-transition-slow", MokaTokenGroup.Motion, MokaTokenKind.Other, "Panels opening and sliding.", context => context.Gate ? "200ms ease-out" : "200ms ease"),

			FromSpacing("--moka-height-statusbar", MokaTokenGroup.Layout, MokaTokenKind.Length, "The height of a status bar."),
			FromSpacing("--moka-height-toolbar", MokaTokenGroup.Layout, MokaTokenKind.Length, "The height of a toolbar, a command bar or an app bar."),
			FromSpacing("--moka-height-toolbar-dense", MokaTokenGroup.Layout, MokaTokenKind.Length, "The height of a dense toolbar and of a tab strip."),

			Token("--moka-z-dropdown", MokaTokenGroup.Layer, MokaTokenKind.Other, "Dropdowns and suggestion lists.", _ => "1000"),
			Token("--moka-z-sticky", MokaTokenGroup.Layer, MokaTokenKind.Other, "Sticky headers.", _ => "1020"),
			Token("--moka-z-fixed", MokaTokenGroup.Layer, MokaTokenKind.Other, "Fixed bars and buttons.", _ => "1030"),
			Token("--moka-z-modal-backdrop", MokaTokenGroup.Layer, MokaTokenKind.Other, "The backdrop behind a modal.", _ => "1040"),
			Token("--moka-z-modal", MokaTokenGroup.Layer, MokaTokenKind.Other, "Dialogs, drawers and sheets.", _ => "1050"),
			Token("--moka-z-popover", MokaTokenGroup.Layer, MokaTokenKind.Other, "Popovers and context menus.", _ => "1060"),
			Token("--moka-z-tooltip", MokaTokenGroup.Layer, MokaTokenKind.Other, "Tooltips.", _ => "1070"),
			Token("--moka-z-appbar", MokaTokenGroup.Layer, MokaTokenKind.Other, "A fixed app bar, above other fixed bars and below every overlay.", _ => "1035"),

			Token("--moka-opacity-disabled", MokaTokenGroup.Interaction, MokaTokenKind.Other, "How faint a disabled control is.", context => context.Gate ? "0.55" : "0.4"),
			Token("--moka-about-display", MokaTokenGroup.Interaction, MokaTokenKind.Other, "Whether text saying what a thing is for shows (block or none).",
				context => context.Theme.Descriptions ? "block" : "none"),
			Token("--moka-opacity-hover", MokaTokenGroup.Interaction, MokaTokenKind.Other, "How strong a hover tint is.", _ => "0.08"),
			Token("--moka-focus-color", MokaTokenGroup.Interaction, MokaTokenKind.Color, "The color of focus outlines.", _ => "var(--moka-color-primary)"),
			Token("--moka-focus-width", MokaTokenGroup.Interaction, MokaTokenKind.Length, "The width of focus outlines.", _ => "2px"),
			Token("--moka-border-width", MokaTokenGroup.Interaction, MokaTokenKind.Length, "The width of borders.", _ => "1px"),
			Token("--moka-focus-ring", MokaTokenGroup.Interaction, MokaTokenKind.Other, "The ring around a focused control.", FocusRing),
			Token("--moka-focus-glow", MokaTokenGroup.Interaction, MokaTokenKind.Other,
				"A soft glow that joins a focused field to the list it has open.", FocusGlow),
			Token("--moka-focus-ring-error", MokaTokenGroup.Interaction, MokaTokenKind.Other, "The ring around a focused control in error.",
				_ => "0 0 0 3px color-mix(in srgb, var(--moka-color-error) 22%, transparent)"),
			Token("--moka-selected-glow", MokaTokenGroup.Interaction, MokaTokenKind.Other, "How a selected row, tab or item is marked.", SelectedGlow),
			Token("--moka-selected-bar", MokaTokenGroup.Interaction, MokaTokenKind.Other, "The bar along a selected table row.",
				context => context.Gate && context.Theme.Selection != MokaSelection.Flat ? "inset 2px 0 0 var(--moka-color-primary)" : "none"),

			Token("--moka-current-bg", MokaTokenGroup.Component, MokaTokenKind.Color, "The fill of where you are in the app, such as the current page in the rail.",
				context => SolidPlace(context) ? "var(--moka-color-primary)" : "var(--moka-color-primary-row)"),
			Token("--moka-current-color", MokaTokenGroup.Component, MokaTokenKind.Color, "Text on the current place.",
				context => SolidPlace(context) ? "var(--moka-color-on-primary)" : "var(--moka-color-on-surface)"),
			Token("--moka-current-icon", MokaTokenGroup.Component, MokaTokenKind.Color, "The icon of the current place.",
				context => SolidPlace(context) ? "var(--moka-color-on-primary)" : "var(--moka-color-primary-light)"),
			Token("--moka-current-shadow", MokaTokenGroup.Component, MokaTokenKind.Other, "The glow around the current place.", CurrentShadow),
			Token("--moka-current-edge", MokaTokenGroup.Component, MokaTokenKind.Color, "The edge along the current place.",
				context => SolidPlace(context) ? "var(--moka-color-on-primary)" : "var(--moka-color-primary)"),
			Token("--moka-current-pill-ring", MokaTokenGroup.Component, MokaTokenKind.Other, "The ring around a count on the current place.",
				context => SolidPlace(context) ? "0 0 0 2px var(--moka-color-on-primary)" : "none"),

			Token("--moka-button-primary-bg", MokaTokenGroup.Component, MokaTokenKind.Color, "The fill of a primary button.", context => MainButton(context).Background),
			Token("--moka-button-primary-color", MokaTokenGroup.Component, MokaTokenKind.Color, "Text on a primary button.", context => MainButton(context).Text),
			Token("--moka-button-primary-border", MokaTokenGroup.Component, MokaTokenKind.Color, "The border of a primary button.", context => MainButton(context).Border),
			Token("--moka-button-primary-hover-bg", MokaTokenGroup.Component, MokaTokenKind.Color, "The fill of a primary button under the pointer.",
				context => MainButton(context).HoverBackground),
			Token("--moka-button-primary-hover-border", MokaTokenGroup.Component, MokaTokenKind.Color, "The border of a primary button under the pointer.",
				context => MainButton(context).HoverBorder),
			Token("--moka-button-text-color", MokaTokenGroup.Component, MokaTokenKind.Color, "A text button left at the primary color.",
				context => NeutralText(context) ? "var(--moka-color-on-surface-2)" : "var(--moka-color-primary-light)"),
			Token("--moka-button-text-hover-color", MokaTokenGroup.Component, MokaTokenKind.Color, "That text button under the pointer.",
				context => NeutralText(context) ? "var(--moka-color-primary-light)" : "var(--moka-color-on-surface)"),

			Token("--moka-callout-fill", MokaTokenGroup.Component, MokaTokenKind.Other, "How much of the filled look callouts and alerts mix in.",
				context => EdgeCallouts(context) ? "0%" : "100%"),
			Token("--moka-callout-edge", MokaTokenGroup.Component, MokaTokenKind.Length, "The width of a callout's start edge.",
				context => EdgeCallouts(context) ? "3px" : "0px"),
			Token("--moka-callout-edge-radius", MokaTokenGroup.Component, MokaTokenKind.Length, "The corners beside a callout's edge.",
				context => EdgeCallouts(context) ? "var(--moka-radius-sm)" : "var(--moka-radius-lg)"),

			Token("--moka-switch-on-track", MokaTokenGroup.Component, MokaTokenKind.Color, "The track of a switch that is on, in the switch's color as currentColor.",
				context => TintedSwitch(context) ? "color-mix(in srgb, currentColor 18%, transparent)" : "currentColor"),
			Token("--moka-switch-on-ring", MokaTokenGroup.Component, MokaTokenKind.Other, "The ring inside a switch's track while on.",
				context => TintedSwitch(context) ? "inset 0 0 0 1px color-mix(in srgb, currentColor 45%, transparent)" : "none"),
			Token("--moka-switch-on-knob", MokaTokenGroup.Component, MokaTokenKind.Color, "The thumb of a switch that is on.",
				context => TintedSwitch(context) ? "currentColor" : "var(--moka-color-knob)"),
			Token("--moka-switch-on-knob-shadow", MokaTokenGroup.Component, MokaTokenKind.Other, "The shadow under that thumb.",
				context => TintedSwitch(context) ? "0 0 8px color-mix(in srgb, currentColor 45%, transparent)" : "var(--moka-shadow-knob)"),

			FromPalette("--moka-color-backdrop", MokaTokenGroup.Overlay, MokaTokenKind.Color, "Behind modal overlays."),
			Token("--moka-backdrop-blur", MokaTokenGroup.Overlay, MokaTokenKind.Length, "How much a modal's backdrop blurs the page.", _ => "8px"),
			Token("--moka-color-backdrop-strong", MokaTokenGroup.Overlay, MokaTokenKind.Color, "A backdrop that hides more of the page.",
				_ => "color-mix(in srgb, var(--moka-color-background) 92%, transparent)"),
			Token("--moka-color-backdrop-frosted", MokaTokenGroup.Overlay, MokaTokenKind.Color, "A backdrop that frosts the page.",
				_ => "color-mix(in srgb, var(--moka-color-background) 55%, transparent)"),
			Token("--moka-backdrop-blur-strong", MokaTokenGroup.Overlay, MokaTokenKind.Length, "The stronger backdrop blur.", _ => "16px")
		]);

		return all;
	}

	private static string ChartDescription(string name)
	{
		string part = name["--moka-chart-".Length..];
		return part switch
		{
			"up" => "A rise in a chart.",
			"down" => "A fall in a chart.",
			"muted" => "Chart marks that step back, such as the older stretch of a line.",
			"grid" => "Chart grid lines.",
			"axis" => "Chart axis lines.",
			"zero" => "The zero line of a chart.",
			_ when part.StartsWith("band-", StringComparison.Ordinal) => $"Chart band {part[5..]}, behind the marks.",
			_ when part.StartsWith("seq-", StringComparison.Ordinal) => $"Step {part[4..]} of the sequential ramp, low to high.",
			_ when part.StartsWith("ord-", StringComparison.Ordinal) => $"Step {part[4..]} of the ordinal ramp, first stage first.",
			_ => $"Chart series color {part}."
		};
	}

	// Code colors read other tokens, so they follow the accent, the scheme and the label tone. Every one reads 4.5:1 on a
	// code block's background and comments and punctuation 3:1 on every preset (CodeTokenTests). A seed's accent is kept
	// as given and may not read as text, so on a theme from a seed directives take the accent's text shade.
	private static IEnumerable<MokaToken> CodeTokens()
	{
		MokaToken Code(string part, string description, Func<MokaTokenContext, string> produce) =>
			Token($"--moka-code-{part}", MokaTokenGroup.Code, MokaTokenKind.Color, description, produce);

		return
		[
			Code("text", "Code that has no other color.", _ => "var(--moka-color-on-surface)"),
			Code("tag", "Markup tags.", context => context.Gate
				? "var(--moka-color-primary-text)"
				: context.Dark ? "var(--moka-color-primary-light)" : "var(--moka-color-primary-dark)"),
			Code("attribute", "Markup attribute names.", context => context.Gate ? "var(--moka-color-on-surface-2)" : "var(--moka-color-on-surface-variant)"),
			Code("string", "Strings.", _ => "var(--moka-label-green-ink)"),
			Code("keyword", "Keywords.", _ => "var(--moka-label-violet-ink)"),
			Code("type", "Type names.", _ => "var(--moka-label-cyan-ink)"),
			Code("number", "Numbers and literals.", _ => "var(--moka-label-amber-ink)"),
			Code("comment", "Comments.", _ => "var(--moka-color-on-surface-tertiary)"),
			Code("punctuation", "Brackets, separators and operators.", _ => "var(--moka-color-on-surface-tertiary)"),
			Code("directive", "Directives, such as Razor's @ lines.", context => context.Theme.Seed is null
				? "var(--moka-color-primary)"
				: "var(--moka-color-primary-light)")
		];
	}

	// On the new design the ring is solid primary past a hairline of surface, so it stands out against the surface and against
	// a filled control: a 22% glow measured 1.3 to 1.5:1 on every preset, short of the 3:1 a focus indicator needs.
	private static string FocusRing(MokaTokenContext context) => context.Gate
		? context.Theme.Selection == MokaSelection.Glow
			? "0 0 0 1px var(--moka-color-surface), 0 0 0 3px var(--moka-color-primary), inset 0 0 12px var(--moka-color-primary-glow)"
			: "0 0 0 1px var(--moka-color-surface), 0 0 0 3px var(--moka-color-primary)"
		: context.Dark
			? "0 0 0 3px var(--moka-color-primary-glow), inset 0 0 12px var(--moka-color-primary-glow)"
			: "0 0 0 3px var(--moka-color-primary-glow)";

	private static string FocusGlow(MokaTokenContext context) => context.Gate
		? "0 0 0 3px var(--moka-color-primary-focus)"
		: "0 0 0 3px var(--moka-color-primary-glow)";

	private static string SelectedGlow(MokaTokenContext context) => context.Gate
		? context.Theme.Selection switch
		{
			MokaSelection.Flat => "none",
			MokaSelection.Glow => "inset 0 0 20px var(--moka-color-primary-glow-md), 0 0 12px var(--moka-color-primary-glow-md)",
			_ => "inset 0 0 0 1px var(--moka-color-primary-ring), inset 0 0 18px var(--moka-color-primary-glow)"
		}
		: context.Dark
			? "inset 0 0 20px var(--moka-color-primary-glow), 0 0 12px var(--moka-color-primary-glow)"
			: "0 0 0 2px var(--moka-color-primary-glow-md)";

	// Ring is the look of a selected row, so a theme that never asks keeps it. On the dark scheme the solid fill's text is
	// the accent's dark ink: white on the dark accent is about 3:1.
	private static bool SolidPlace(MokaTokenContext context) =>
		context.Gate && context.Theme.CurrentPlace is MokaCurrentPlace.Solid or MokaCurrentPlace.SolidInset;

	private static string CurrentShadow(MokaTokenContext context) => !SolidPlace(context)
		? "var(--moka-selected-glow)"
		: context.Theme.CurrentPlace == MokaCurrentPlace.SolidInset
			? MokaShadowSet.InsetPlace
			: "0 0 24px color-mix(in srgb, var(--moka-color-primary) 25%, transparent)";

	private static (string Background, string Text, string Border, string HoverBackground, string HoverBorder) MainButton(MokaTokenContext context) =>
		context.Theme.MainButton switch
		{
			MokaMainButton.Tinted => ("var(--moka-color-primary-bg)", "var(--moka-color-primary-light)", "var(--moka-color-primary-ring)",
				"var(--moka-color-primary-pill)", "var(--moka-color-primary)"),
			MokaMainButton.Outline => ("transparent", "var(--moka-color-primary)", "var(--moka-color-primary-ring)",
				"var(--moka-color-primary-row)", "var(--moka-color-primary)"),
			_ => ("var(--moka-color-primary)", "var(--moka-color-on-primary)", "var(--moka-color-primary)",
				"color-mix(in srgb, var(--moka-color-primary) 85%, var(--moka-color-lift))",
				"color-mix(in srgb, var(--moka-color-primary) 85%, var(--moka-color-lift))")
		};

	private static bool NeutralText(MokaTokenContext context) => context.Gate && context.Theme.TextButtons == MokaTextButtonColor.Neutral;

	// Callouts and alerts mix their own severity colors by these amounts, since a token can't name a color only the
	// component knows.
	private static bool EdgeCallouts(MokaTokenContext context) => context.Gate && context.Theme.Callouts == MokaCalloutLook.Edge;

	// A switch's track carries its color as currentColor, which a token keeps as a keyword for the element to resolve,
	// where a var() would resolve on the root.
	private static bool TintedSwitch(MokaTokenContext context) => context.Gate && context.Theme.Switches == MokaSwitchLook.Tinted;
}
