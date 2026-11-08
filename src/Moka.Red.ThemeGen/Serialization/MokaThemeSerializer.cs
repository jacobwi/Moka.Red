using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.ThemeGen.Serialization;

/// <summary>
///     Serializes and deserializes <see cref="MokaTheme" /> to/from JSON, CSS, and C# code.
///     JSON and C# carry every theme, palette, typography and spacing property, so both read back
///     to an equal theme.
/// </summary>
public static class MokaThemeSerializer
{
	private const string IsDarkKey = "isDark";
	private const string LookKey = "look";
	private const string HighContrastKey = "highContrast";
	private const string SurfacesKey = "surfaces";
	private const string SelectionKey = "selection";
	private const string CurrentPlaceKey = "currentPlace";
	private const string MainButtonKey = "mainButton";
	private const string DescriptionsKey = "descriptions";
	private const string DensityKey = "density";
	private const string FontScaleKey = "fontScale";
	private const string PaletteKey = "palette";
	private const string TypographyKey = "typography";
	private const string SpacingKey = "spacing";

	// The export box shows this text to people, so the quotes in font names stay readable instead of
	// turning into escape sequences. It is never written into HTML or a script unencoded.
	private static readonly JsonWriterOptions WriterOptions = new()
	{
		Indented = true,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	private static readonly JsonDocumentOptions ReaderOptions = new()
	{
		AllowTrailingCommas = true,
		CommentHandling = JsonCommentHandling.Skip
	};

	// Besides numbers with a unit, a length field takes the CSS functions that compute one, and var().
	private static readonly string[] LengthFunctions = ["calc(", "min(", "max(", "clamp(", "var("];

	// One row per record property. Both formats are written from these tables, and the round-trip
	// tests fail when a record gains a property that is missing here.
	private static readonly Field<MokaPalette>[] PaletteFields =
	[
		new(nameof(MokaPalette.Primary), p => p.Primary, (p, v) => p with { Primary = v }),
		new(nameof(MokaPalette.PrimaryLight), p => p.PrimaryLight, (p, v) => p with { PrimaryLight = v }),
		new(nameof(MokaPalette.PrimaryDark), p => p.PrimaryDark, (p, v) => p with { PrimaryDark = v }),
		new(nameof(MokaPalette.OnPrimary), p => p.OnPrimary, (p, v) => p with { OnPrimary = v }),
		new(nameof(MokaPalette.Secondary), p => p.Secondary, (p, v) => p with { Secondary = v }),
		new(nameof(MokaPalette.SecondaryLight), p => p.SecondaryLight, (p, v) => p with { SecondaryLight = v }),
		new(nameof(MokaPalette.SecondaryDark), p => p.SecondaryDark, (p, v) => p with { SecondaryDark = v }),
		new(nameof(MokaPalette.OnSecondary), p => p.OnSecondary, (p, v) => p with { OnSecondary = v }),
		new(nameof(MokaPalette.Surface), p => p.Surface, (p, v) => p with { Surface = v }),
		new(nameof(MokaPalette.SurfaceVariant), p => p.SurfaceVariant, (p, v) => p with { SurfaceVariant = v }),
		new(nameof(MokaPalette.OnSurface), p => p.OnSurface, (p, v) => p with { OnSurface = v }),
		new(nameof(MokaPalette.OnSurfaceVariant), p => p.OnSurfaceVariant, (p, v) => p with { OnSurfaceVariant = v }),
		new(nameof(MokaPalette.Background), p => p.Background, (p, v) => p with { Background = v }),
		new(nameof(MokaPalette.OnBackground), p => p.OnBackground, (p, v) => p with { OnBackground = v }),
		new(nameof(MokaPalette.Error), p => p.Error, (p, v) => p with { Error = v }),
		new(nameof(MokaPalette.OnError), p => p.OnError, (p, v) => p with { OnError = v }),
		new(nameof(MokaPalette.Warning), p => p.Warning, (p, v) => p with { Warning = v }),
		new(nameof(MokaPalette.OnWarning), p => p.OnWarning, (p, v) => p with { OnWarning = v }),
		new(nameof(MokaPalette.Success), p => p.Success, (p, v) => p with { Success = v }),
		new(nameof(MokaPalette.OnSuccess), p => p.OnSuccess, (p, v) => p with { OnSuccess = v }),
		new(nameof(MokaPalette.Info), p => p.Info, (p, v) => p with { Info = v }),
		new(nameof(MokaPalette.OnInfo), p => p.OnInfo, (p, v) => p with { OnInfo = v }),
		new(nameof(MokaPalette.ErrorDim), p => p.ErrorDim, (p, v) => p with { ErrorDim = v }),
		new(nameof(MokaPalette.WarningDim), p => p.WarningDim, (p, v) => p with { WarningDim = v }),
		new(nameof(MokaPalette.SuccessDim), p => p.SuccessDim, (p, v) => p with { SuccessDim = v }),
		new(nameof(MokaPalette.InfoDim), p => p.InfoDim, (p, v) => p with { InfoDim = v }),
		new(nameof(MokaPalette.Outline), p => p.Outline, (p, v) => p with { Outline = v }),
		new(nameof(MokaPalette.OutlineVariant), p => p.OutlineVariant, (p, v) => p with { OutlineVariant = v }),
		new(nameof(MokaPalette.SurfaceHover), p => p.SurfaceHover, (p, v) => p with { SurfaceHover = v }),
		new(nameof(MokaPalette.Surface2), p => p.Surface2, (p, v) => p with { Surface2 = v }),
		new(nameof(MokaPalette.Surface3), p => p.Surface3, (p, v) => p with { Surface3 = v }),
		new(nameof(MokaPalette.PrimaryGlow), p => p.PrimaryGlow, (p, v) => p with { PrimaryGlow = v }),
		new(nameof(MokaPalette.PrimaryGlowMd), p => p.PrimaryGlowMd, (p, v) => p with { PrimaryGlowMd = v }),
		new(nameof(MokaPalette.PrimaryGlowStrong), p => p.PrimaryGlowStrong, (p, v) => p with { PrimaryGlowStrong = v }),
		new(nameof(MokaPalette.PrimaryBorder), p => p.PrimaryBorder, (p, v) => p with { PrimaryBorder = v }),
		new(nameof(MokaPalette.PrimaryBorderDim), p => p.PrimaryBorderDim, (p, v) => p with { PrimaryBorderDim = v }),
		new(nameof(MokaPalette.PrimaryGlowFaint), p => p.PrimaryGlowFaint, (p, v) => p with { PrimaryGlowFaint = v }),
		new(nameof(MokaPalette.OnSurfaceTertiary), p => p.OnSurfaceTertiary, (p, v) => p with { OnSurfaceTertiary = v }),
		new(nameof(MokaPalette.OnSurfaceQuaternary), p => p.OnSurfaceQuaternary,
			(p, v) => p with { OnSurfaceQuaternary = v }),
		new(nameof(MokaPalette.SurfaceRaised), p => p.SurfaceRaised, (p, v) => p with { SurfaceRaised = v }),
		new(nameof(MokaPalette.NeutralBg), p => p.NeutralBg, (p, v) => p with { NeutralBg = v }),
		new(nameof(MokaPalette.OutlineStrong), p => p.OutlineStrong, (p, v) => p with { OutlineStrong = v }),
		new(nameof(MokaPalette.OutlineHover), p => p.OutlineHover, (p, v) => p with { OutlineHover = v }),
		new(nameof(MokaPalette.Backdrop), p => p.Backdrop, (p, v) => p with { Backdrop = v }),
		new(nameof(MokaPalette.OnSurface2), p => p.OnSurface2, (p, v) => p with { OnSurface2 = v }),
		new(nameof(MokaPalette.Icon), p => p.Icon, (p, v) => p with { Icon = v }),
		new(nameof(MokaPalette.Lift), p => p.Lift, (p, v) => p with { Lift = v }),
		new(nameof(MokaPalette.Knob), p => p.Knob, (p, v) => p with { Knob = v }),
		new(nameof(MokaPalette.KnobShadow), p => p.KnobShadow, (p, v) => p with { KnobShadow = v }),
		new(nameof(MokaPalette.Paper), p => p.Paper, (p, v) => p with { Paper = v }),
		new(nameof(MokaPalette.PaperEdge), p => p.PaperEdge, (p, v) => p with { PaperEdge = v }),
		new(nameof(MokaPalette.PrimaryBg), p => p.PrimaryBg, (p, v) => p with { PrimaryBg = v }),
		new(nameof(MokaPalette.PrimaryRow), p => p.PrimaryRow, (p, v) => p with { PrimaryRow = v }),
		new(nameof(MokaPalette.PrimaryRing), p => p.PrimaryRing, (p, v) => p with { PrimaryRing = v }),
		new(nameof(MokaPalette.PrimaryPill), p => p.PrimaryPill, (p, v) => p with { PrimaryPill = v }),
		new(nameof(MokaPalette.PrimaryFocus), p => p.PrimaryFocus, (p, v) => p with { PrimaryFocus = v }),
		new(nameof(MokaPalette.PrimaryText), p => p.PrimaryText, (p, v) => p with { PrimaryText = v }),
		new(nameof(MokaPalette.SuccessBg), p => p.SuccessBg, (p, v) => p with { SuccessBg = v }),
		new(nameof(MokaPalette.SuccessInk), p => p.SuccessInk, (p, v) => p with { SuccessInk = v }),
		new(nameof(MokaPalette.SuccessText), p => p.SuccessText, (p, v) => p with { SuccessText = v }),
		new(nameof(MokaPalette.SuccessLine), p => p.SuccessLine, (p, v) => p with { SuccessLine = v }),
		new(nameof(MokaPalette.WarningBg), p => p.WarningBg, (p, v) => p with { WarningBg = v }),
		new(nameof(MokaPalette.WarningInk), p => p.WarningInk, (p, v) => p with { WarningInk = v }),
		new(nameof(MokaPalette.WarningText), p => p.WarningText, (p, v) => p with { WarningText = v }),
		new(nameof(MokaPalette.WarningLine), p => p.WarningLine, (p, v) => p with { WarningLine = v }),
		new(nameof(MokaPalette.ErrorBg), p => p.ErrorBg, (p, v) => p with { ErrorBg = v }),
		new(nameof(MokaPalette.ErrorInk), p => p.ErrorInk, (p, v) => p with { ErrorInk = v }),
		new(nameof(MokaPalette.ErrorText), p => p.ErrorText, (p, v) => p with { ErrorText = v }),
		new(nameof(MokaPalette.ErrorLine), p => p.ErrorLine, (p, v) => p with { ErrorLine = v }),
		new(nameof(MokaPalette.ErrorSoft), p => p.ErrorSoft, (p, v) => p with { ErrorSoft = v }),
		new(nameof(MokaPalette.InfoBg), p => p.InfoBg, (p, v) => p with { InfoBg = v }),
		new(nameof(MokaPalette.InfoInk), p => p.InfoInk, (p, v) => p with { InfoInk = v }),
		new(nameof(MokaPalette.InfoText), p => p.InfoText, (p, v) => p with { InfoText = v }),
		new(nameof(MokaPalette.InfoLine), p => p.InfoLine, (p, v) => p with { InfoLine = v })
	];

	private static readonly Field<MokaTypography>[] TypographyFields =
	[
		new(nameof(MokaTypography.FontFamily), t => t.FontFamily, (t, v) => t with { FontFamily = v }),
		new(nameof(MokaTypography.FontFamilyMono), t => t.FontFamilyMono, (t, v) => t with { FontFamilyMono = v }),
		new(nameof(MokaTypography.FontFamilyDisplay), t => t.FontFamilyDisplay,
			(t, v) => t with { FontFamilyDisplay = v }),
		new(nameof(MokaTypography.FontSizeXxs), t => t.FontSizeXxs, (t, v) => t with { FontSizeXxs = v }),
		new(nameof(MokaTypography.FontSizeXs), t => t.FontSizeXs, (t, v) => t with { FontSizeXs = v }),
		new(nameof(MokaTypography.FontSizeSm), t => t.FontSizeSm, (t, v) => t with { FontSizeSm = v }),
		new(nameof(MokaTypography.FontSizeBase), t => t.FontSizeBase, (t, v) => t with { FontSizeBase = v }),
		new(nameof(MokaTypography.FontSizeMd), t => t.FontSizeMd, (t, v) => t with { FontSizeMd = v }),
		new(nameof(MokaTypography.FontSizeLg), t => t.FontSizeLg, (t, v) => t with { FontSizeLg = v }),
		new(nameof(MokaTypography.FontSizeXl), t => t.FontSizeXl, (t, v) => t with { FontSizeXl = v }),
		new(nameof(MokaTypography.FontSizeXxl), t => t.FontSizeXxl, (t, v) => t with { FontSizeXxl = v }),
		new(nameof(MokaTypography.FontSizeDisplay), t => t.FontSizeDisplay, (t, v) => t with { FontSizeDisplay = v }),
		new(nameof(MokaTypography.FontSizeRoot), t => t.FontSizeRoot, (t, v) => t with { FontSizeRoot = v }),
		new(nameof(MokaTypography.FontSizeHeading), t => t.FontSizeHeading, (t, v) => t with { FontSizeHeading = v }),
		new(nameof(MokaTypography.FontSizeCardTitle), t => t.FontSizeCardTitle, (t, v) => t with { FontSizeCardTitle = v }),
		new(nameof(MokaTypography.FontSizeDialog), t => t.FontSizeDialog, (t, v) => t with { FontSizeDialog = v }),
		new(nameof(MokaTypography.FontSizeMenu), t => t.FontSizeMenu, (t, v) => t with { FontSizeMenu = v }),
		new(nameof(MokaTypography.FontSizeMono), t => t.FontSizeMono, (t, v) => t with { FontSizeMono = v }),
		new(nameof(MokaTypography.LineHeightTight), t => t.LineHeightTight, (t, v) => t with { LineHeightTight = v }),
		new(nameof(MokaTypography.LineHeightBase), t => t.LineHeightBase, (t, v) => t with { LineHeightBase = v }),
		new(nameof(MokaTypography.LineHeightRelaxed), t => t.LineHeightRelaxed,
			(t, v) => t with { LineHeightRelaxed = v }),
		new(nameof(MokaTypography.FontWeightLight), t => t.FontWeightLight, (t, v) => t with { FontWeightLight = v }),
		new(nameof(MokaTypography.FontWeightNormal), t => t.FontWeightNormal, (t, v) => t with { FontWeightNormal = v }),
		new(nameof(MokaTypography.FontWeightMedium), t => t.FontWeightMedium, (t, v) => t with { FontWeightMedium = v }),
		new(nameof(MokaTypography.FontWeightSemibold), t => t.FontWeightSemibold,
			(t, v) => t with { FontWeightSemibold = v }),
		new(nameof(MokaTypography.FontWeightBold), t => t.FontWeightBold, (t, v) => t with { FontWeightBold = v }),
		new(nameof(MokaTypography.FontWeightHeading), t => t.FontWeightHeading, (t, v) => t with { FontWeightHeading = v }),
		new(nameof(MokaTypography.ControlCase), t => t.ControlCase, (t, v) => t with { ControlCase = v }),
		new(nameof(MokaTypography.ControlTracking), t => t.ControlTracking, (t, v) => t with { ControlTracking = v }),
		new(nameof(MokaTypography.TitleCase), t => t.TitleCase, (t, v) => t with { TitleCase = v }),
		new(nameof(MokaTypography.TitleTracking), t => t.TitleTracking, (t, v) => t with { TitleTracking = v }),
		new(nameof(MokaTypography.HeadingTracking), t => t.HeadingTracking, (t, v) => t with { HeadingTracking = v })
	];

	private static readonly Field<MokaSpacing>[] SpacingFields =
	[
		new(nameof(MokaSpacing.Xxs), s => s.Xxs, (s, v) => s with { Xxs = v }),
		new(nameof(MokaSpacing.Xs), s => s.Xs, (s, v) => s with { Xs = v }),
		new(nameof(MokaSpacing.Sm), s => s.Sm, (s, v) => s with { Sm = v }),
		new(nameof(MokaSpacing.Md), s => s.Md, (s, v) => s with { Md = v }),
		new(nameof(MokaSpacing.Lg), s => s.Lg, (s, v) => s with { Lg = v }),
		new(nameof(MokaSpacing.Xl), s => s.Xl, (s, v) => s with { Xl = v }),
		new(nameof(MokaSpacing.Xxl), s => s.Xxl, (s, v) => s with { Xxl = v }),
		new(nameof(MokaSpacing.RadiusNone), s => s.RadiusNone, (s, v) => s with { RadiusNone = v }),
		new(nameof(MokaSpacing.RadiusSm), s => s.RadiusSm, (s, v) => s with { RadiusSm = v }),
		new(nameof(MokaSpacing.RadiusMd), s => s.RadiusMd, (s, v) => s with { RadiusMd = v }),
		new(nameof(MokaSpacing.RadiusLg), s => s.RadiusLg, (s, v) => s with { RadiusLg = v }),
		new(nameof(MokaSpacing.RadiusXl), s => s.RadiusXl, (s, v) => s with { RadiusXl = v }),
		new(nameof(MokaSpacing.RadiusFull), s => s.RadiusFull, (s, v) => s with { RadiusFull = v }),
		new(nameof(MokaSpacing.RadiusCard), s => s.RadiusCard, (s, v) => s with { RadiusCard = v }),
		new(nameof(MokaSpacing.RadiusPopup), s => s.RadiusPopup, (s, v) => s with { RadiusPopup = v }),
		new(nameof(MokaSpacing.PageGap), s => s.PageGap, (s, v) => s with { PageGap = v }),
		new(nameof(MokaSpacing.CardPadding), s => s.CardPadding, (s, v) => s with { CardPadding = v }),
		new(nameof(MokaSpacing.CardGap), s => s.CardGap, (s, v) => s with { CardGap = v }),
		new(nameof(MokaSpacing.GridGap), s => s.GridGap, (s, v) => s with { GridGap = v }),
		new(nameof(MokaSpacing.PanelPadding), s => s.PanelPadding, (s, v) => s with { PanelPadding = v }),
		new(nameof(MokaSpacing.InnerPadding), s => s.InnerPadding, (s, v) => s with { InnerPadding = v }),
		new(nameof(MokaSpacing.RowPadding), s => s.RowPadding, (s, v) => s with { RowPadding = v }),
		new(nameof(MokaSpacing.ItemPadding), s => s.ItemPadding, (s, v) => s with { ItemPadding = v }),
		new(nameof(MokaSpacing.ControlHeight), s => s.ControlHeight, (s, v) => s with { ControlHeight = v }),
		new(nameof(MokaSpacing.MainPadding), s => s.MainPadding, (s, v) => s with { MainPadding = v }),
		new(nameof(MokaSpacing.MenuRow), s => s.MenuRow, (s, v) => s with { MenuRow = v }),
		new(nameof(MokaSpacing.ButtonHeight), s => s.ButtonHeight, (s, v) => s with { ButtonHeight = v }),
		new(nameof(MokaSpacing.ButtonHeightSm), s => s.ButtonHeightSm, (s, v) => s with { ButtonHeightSm = v }),
		new(nameof(MokaSpacing.ButtonHeightLg), s => s.ButtonHeightLg, (s, v) => s with { ButtonHeightLg = v }),
		new(nameof(MokaSpacing.ButtonPadding), s => s.ButtonPadding, (s, v) => s with { ButtonPadding = v }),
		new(nameof(MokaSpacing.ButtonPaddingSm), s => s.ButtonPaddingSm, (s, v) => s with { ButtonPaddingSm = v }),
		new(nameof(MokaSpacing.InputPadding), s => s.InputPadding, (s, v) => s with { InputPadding = v }),
		new(nameof(MokaSpacing.SegmentHeight), s => s.SegmentHeight, (s, v) => s with { SegmentHeight = v }),
		new(nameof(MokaSpacing.CellPadding), s => s.CellPadding, (s, v) => s with { CellPadding = v }),
		new(nameof(MokaSpacing.PillPadding), s => s.PillPadding, (s, v) => s with { PillPadding = v }),
		new(nameof(MokaSpacing.ToolbarHeight), s => s.ToolbarHeight, (s, v) => s with { ToolbarHeight = v }),
		new(nameof(MokaSpacing.ToolbarHeightDense), s => s.ToolbarHeightDense, (s, v) => s with { ToolbarHeightDense = v }),
		new(nameof(MokaSpacing.StatusBarHeight), s => s.StatusBarHeight, (s, v) => s with { StatusBarHeight = v })
	];

	/// <summary>Serialize a <see cref="MokaTheme" /> to indented JSON with camelCase names.</summary>
	public static string ToJson(MokaTheme theme)
	{
		ArgumentNullException.ThrowIfNull(theme);

		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream, WriterOptions))
		{
			writer.WriteStartObject();
			writer.WriteBoolean(IsDarkKey, theme.IsDark);
			writer.WriteString(LookKey, theme.Look.ToString());
			writer.WriteBoolean(HighContrastKey, theme.HighContrast);
			writer.WriteString(SurfacesKey, theme.Surfaces.ToString());
			writer.WriteString(SelectionKey, theme.Selection.ToString());
			writer.WriteString(CurrentPlaceKey, theme.CurrentPlace.ToString());
			writer.WriteString(MainButtonKey, theme.MainButton.ToString());
			writer.WriteBoolean(DescriptionsKey, theme.Descriptions);
			WriteNumber(writer, DensityKey, theme.Density);
			WriteNumber(writer, FontScaleKey, theme.FontScale);
			WriteObject(writer, PaletteKey, theme.Palette, PaletteFields);
			WriteObject(writer, TypographyKey, theme.Typography, TypographyFields);
			WriteObject(writer, SpacingKey, theme.Spacing, SpacingFields);
			writer.WriteEndObject();
		}

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	/// <summary>
	///     Deserialize a <see cref="MokaTheme" /> from JSON. Returns <c>null</c> when the text is not a theme;
	///     <see cref="TryFromJson" /> also says why.
	/// </summary>
	public static MokaTheme? FromJson(string? json) => TryFromJson(json, out MokaTheme? theme, out _) ? theme : null;

	/// <summary>
	///     Reads a theme from JSON written by <see cref="ToJson" />. Names match case-insensitively. Missing
	///     values come from the built-in theme of the same <c>look</c> and <c>isDark</c>. Keys it does not know
	///     are skipped. Every value is checked before the theme is built: palette values must be CSS colors
	///     (<see cref="CssValues.IsColor" />), font sizes and spacing CSS lengths (a number with a unit, or a
	///     <c>calc()</c>, <c>min()</c>, <c>max()</c>, <c>clamp()</c> or <c>var()</c> expression; paddings may hold
	///     several), and the rest must pass <see cref="CssValues.IsSafe" /> and
	///     <see cref="CssValues.IsSelfContained" />. Never throws.
	/// </summary>
	/// <param name="json">The JSON text.</param>
	/// <param name="theme">The theme, when the text could be read.</param>
	/// <param name="error">A message for the user, when it could not.</param>
	/// <returns><c>true</c> when <paramref name="theme" /> was read.</returns>
	public static bool TryFromJson(
		string? json,
		[NotNullWhen(true)] out MokaTheme? theme,
		[NotNullWhen(false)] out string? error)
	{
		theme = null;

		if (string.IsNullOrWhiteSpace(json))
		{
			error = "The theme JSON is empty.";
			return false;
		}

		JsonDocument document;
		try
		{
			document = JsonDocument.Parse(json, ReaderOptions);
		}
		catch (JsonException ex)
		{
			error = DescribeSyntaxError(ex);
			return false;
		}
		catch (ArgumentException)
		{
			// Thrown for text that is not valid UTF-16, such as a lone surrogate.
			error = "The theme JSON contains characters that are not valid text.";
			return false;
		}

		using (document)
		{
			error = ReadTheme(document.RootElement, out theme);
			return error is null;
		}
	}

	/// <summary>Export theme as CSS custom properties block.</summary>
	public static string ToCss(MokaTheme theme)
	{
		ArgumentNullException.ThrowIfNull(theme);
		string cssVars = theme.ToCssVariables();
		var sb = new StringBuilder();
		sb.AppendLine(":root {");

		string[] pairs = cssVars.Split("; ", StringSplitOptions.RemoveEmptyEntries);
		foreach (string pair in pairs)
		{
			string trimmed = pair.TrimEnd(';').Trim();
			if (!string.IsNullOrEmpty(trimmed))
			{
				sb.Append('\t');
				sb.Append(trimmed);
				sb.AppendLine(";");
			}
		}

		sb.Append('}');
		return sb.ToString();
	}

	/// <summary>
	///     Export theme as a C# <c>new MokaTheme { ... }</c> expression that can be pasted into source.
	///     Strings are written as escaped C# literals.
	/// </summary>
	public static string ToCSharp(MokaTheme theme)
	{
		ArgumentNullException.ThrowIfNull(theme);

		var sb = new StringBuilder(4096);
		sb.AppendLine("new MokaTheme");
		sb.AppendLine("{");
		AppendAssignment(sb, "\t", nameof(MokaTheme.IsDark), theme.IsDark ? "true" : "false");
		AppendAssignment(sb, "\t", nameof(MokaTheme.Look), $"{nameof(MokaLook)}.{theme.Look}");
		AppendAssignment(sb, "\t", nameof(MokaTheme.HighContrast), theme.HighContrast ? "true" : "false");
		AppendAssignment(sb, "\t", nameof(MokaTheme.Surfaces), $"{nameof(MokaSurfaces)}.{theme.Surfaces}");
		AppendAssignment(sb, "\t", nameof(MokaTheme.Selection), $"{nameof(MokaSelection)}.{theme.Selection}");
		AppendAssignment(sb, "\t", nameof(MokaTheme.CurrentPlace), $"{nameof(MokaCurrentPlace)}.{theme.CurrentPlace}");
		AppendAssignment(sb, "\t", nameof(MokaTheme.MainButton), $"{nameof(MokaMainButton)}.{theme.MainButton}");
		AppendAssignment(sb, "\t", nameof(MokaTheme.Descriptions), theme.Descriptions ? "true" : "false");
		AppendAssignment(sb, "\t", nameof(MokaTheme.Density), CSharpDouble(theme.Density));
		AppendAssignment(sb, "\t", nameof(MokaTheme.FontScale), CSharpDouble(theme.FontScale));
		AppendObject(sb, nameof(MokaTheme.Palette), nameof(MokaPalette), theme.Palette, PaletteFields);
		AppendObject(sb, nameof(MokaTheme.Typography), nameof(MokaTypography), theme.Typography, TypographyFields);
		AppendObject(sb, nameof(MokaTheme.Spacing), nameof(MokaSpacing), theme.Spacing, SpacingFields);
		sb.Append('}');

		return sb.ToString();
	}

	// ── JSON writing ──────────────────────────────────────────────

	private static void WriteNumber(Utf8JsonWriter writer, string name, double value)
	{
		// JSON has no NaN or Infinity. They go out as the string names System.Text.Json uses for
		// them, which ReadNumber accepts, so even a broken density round-trips instead of throwing.
		if (double.IsFinite(value))
		{
			writer.WriteNumber(name, value);
		}
		else
		{
			writer.WriteString(name, value.ToString(CultureInfo.InvariantCulture));
		}
	}

	private static void WriteObject<T>(Utf8JsonWriter writer, string name, T value, Field<T>[] fields)
	{
		writer.WriteStartObject(name);
		foreach (Field<T> field in fields)
		{
			writer.WriteString(field.JsonName, field.Get(value));
		}

		writer.WriteEndObject();
	}

	// ── JSON reading ──────────────────────────────────────────────

	private static string? ReadTheme(JsonElement root, out MokaTheme? theme)
	{
		theme = null;

		if (root.ValueKind != JsonValueKind.Object)
		{
			return "The theme JSON must be an object, like the one Export writes.";
		}

		JsonElement? isDark = null, look = null, highContrast = null, density = null, fontScale = null;
		JsonElement? surfaces = null, selection = null, currentPlace = null, mainButton = null, descriptions = null;
		JsonElement? palette = null, typography = null, spacing = null;
		foreach (JsonProperty property in root.EnumerateObject())
		{
			string name = property.Name;
			if (IsKey(name, IsDarkKey))
			{
				isDark = property.Value;
			}
			else if (IsKey(name, LookKey))
			{
				look = property.Value;
			}
			else if (IsKey(name, HighContrastKey))
			{
				highContrast = property.Value;
			}
			else if (IsKey(name, SurfacesKey))
			{
				surfaces = property.Value;
			}
			else if (IsKey(name, SelectionKey))
			{
				selection = property.Value;
			}
			else if (IsKey(name, CurrentPlaceKey))
			{
				currentPlace = property.Value;
			}
			else if (IsKey(name, MainButtonKey))
			{
				mainButton = property.Value;
			}
			else if (IsKey(name, DescriptionsKey))
			{
				descriptions = property.Value;
			}
			else if (IsKey(name, DensityKey))
			{
				density = property.Value;
			}
			else if (IsKey(name, FontScaleKey))
			{
				fontScale = property.Value;
			}
			else if (IsKey(name, PaletteKey))
			{
				palette = property.Value;
			}
			else if (IsKey(name, TypographyKey))
			{
				typography = property.Value;
			}
			else if (IsKey(name, SpacingKey))
			{
				spacing = property.Value;
			}

			// Any other key is skipped, so a theme exported by a later version, with tokens this one
			// does not have yet, still imports.
		}

		bool dark = false;
		if (isDark is { } isDarkValue)
		{
			if (isDarkValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
			{
				return $"'{IsDarkKey}' must be true or false.";
			}

			dark = isDarkValue.GetBoolean();
		}

		bool high = false;
		if (highContrast is { } highContrastValue)
		{
			if (highContrastValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
			{
				return $"'{HighContrastKey}' must be true or false.";
			}

			high = highContrastValue.GetBoolean();
		}

		bool showDescriptions = true;
		if (descriptions is { } descriptionsValue)
		{
			if (descriptionsValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
			{
				return $"'{DescriptionsKey}' must be true or false.";
			}

			showDescriptions = descriptionsValue.GetBoolean();
		}

		string? choiceError = null;
		MokaSurfaces themeSurfaces = ReadChoice(surfaces, SurfacesKey, MokaSurfaces.BlueGrey, ref choiceError);
		MokaSelection themeSelection = ReadChoice(selection, SelectionKey, MokaSelection.Ring, ref choiceError);
		MokaCurrentPlace themeCurrentPlace = ReadChoice(currentPlace, CurrentPlaceKey, MokaCurrentPlace.Ring, ref choiceError);
		MokaMainButton themeMainButton = ReadChoice(mainButton, MainButtonKey, MokaMainButton.Solid, ref choiceError);
		if (choiceError is not null)
		{
			return choiceError;
		}

		MokaLook themeLook = MokaLook.Matrix;
		if (look is { } lookValue)
		{
			// By name only: Enum.TryParse also takes numbers and comma lists, which no export writes.
			string lookName = lookValue.ValueKind == JsonValueKind.String ? lookValue.GetString() ?? "" : "";
			string? known = Array.Find(Enum.GetNames<MokaLook>(), name => IsKey(name, lookName));
			if (known is null)
			{
				return $"'{LookKey}' must be one of {string.Join(", ", Enum.GetNames<MokaLook>())}.";
			}

			themeLook = Enum.Parse<MokaLook>(known);
		}

		// Missing values come from the built-in theme that matches the look and isDark, so a light theme
		// written before a palette color existed does not pick up the dark MokaPalette initializer defaults.
		MokaTheme baseTheme = themeLook != MokaLook.Gate
			? dark ? MokaTheme.Dark : MokaTheme.Light
			: themeSurfaces switch
			{
				MokaSurfaces.Graphite => dark ? MokaTheme.Red : MokaTheme.RedLight,
				MokaSurfaces.NearBlack => dark ? MokaTheme.Matrix : MokaTheme.MatrixLight,
				_ => dark ? MokaTheme.Gate : MokaTheme.GateLight
			};

		string? error = null;
		theme = new MokaTheme
		{
			IsDark = dark,
			Look = themeLook,
			HighContrast = high,
			Surfaces = themeSurfaces,
			Selection = themeSelection,
			CurrentPlace = themeCurrentPlace,
			MainButton = themeMainButton,
			Descriptions = showDescriptions,
			Density = ReadNumber(density, DensityKey, baseTheme.Density, ref error),
			FontScale = ReadNumber(fontScale, FontScaleKey, baseTheme.FontScale, ref error),
			Palette = ReadObject(palette, PaletteKey, baseTheme.Palette, PaletteFields,
				field => field == nameof(MokaPalette.KnobShadow) ? ValueKind.Css : ValueKind.Color,
				ref error),
			Typography = ReadObject(typography, TypographyKey, baseTheme.Typography, TypographyFields,
				field => field.StartsWith("FontSize", StringComparison.Ordinal) ? ValueKind.Length : ValueKind.Css,
				ref error),
			Spacing = ReadObject(spacing, SpacingKey, baseTheme.Spacing, SpacingFields,
				field => field.EndsWith("Padding", StringComparison.Ordinal) ? ValueKind.Lengths : ValueKind.Length,
				ref error)
		};

		if (error is not null)
		{
			theme = null;
		}

		return error;
	}

	private static bool IsKey(string name, string key) => string.Equals(name, key, StringComparison.OrdinalIgnoreCase);

	// The Read helpers return the fallback and record the first problem in error, so the caller can
	// read every part in one expression and check once.
	private static double ReadNumber(JsonElement? element, string name, double fallback, ref string? error)
	{
		if (error is not null || element is not { } json)
		{
			return fallback;
		}

		if (json.ValueKind == JsonValueKind.Number && json.TryGetDouble(out double number) && double.IsFinite(number))
		{
			return number;
		}

		if (json.ValueKind == JsonValueKind.String && TryReadNamedDouble(json.GetString(), out double named))
		{
			return named;
		}

		error = $"'{name}' must be a number.";
		return fallback;
	}

	private static bool TryReadNamedDouble(string? text, out double value)
	{
		switch (text)
		{
			case "NaN":
				value = double.NaN;
				return true;
			case "Infinity":
				value = double.PositiveInfinity;
				return true;
			case "-Infinity":
				value = double.NegativeInfinity;
				return true;
			default:
				value = 0;
				return false;
		}
	}

	private static T ReadObject<T>(JsonElement? element, string name, T fallback, Field<T>[] fields,
		Func<string, ValueKind> kindOf, ref string? error)
	{
		if (error is not null || element is not { } json)
		{
			return fallback;
		}

		if (json.ValueKind != JsonValueKind.Object)
		{
			error = $"'{name}' must be an object.";
			return fallback;
		}

		T value = fallback;
		foreach (JsonProperty property in json.EnumerateObject())
		{
			Field<T>? field = Array.Find(fields, f => IsKey(property.Name, f.JsonName));
			if (field is null)
			{
				// A token added in a later version: skipped, like an unknown top-level key.
				continue;
			}

			string? text = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
			if (string.IsNullOrWhiteSpace(text))
			{
				error = $"'{name}.{field.JsonName}' must be a non-empty string.";
				return fallback;
			}

			// The theme writes its values into a <style> element, where anything else could end the rule
			// or swallow the tokens after it, and a value of the wrong kind does nothing.
			string? problem = Check(kindOf(field.Name), text);
			if (problem is not null)
			{
				error = $"'{name}.{field.JsonName}' {problem}";
				return fallback;
			}

			value = field.Set(value, text);
		}

		return value;
	}

	// By name only: Enum.TryParse also takes numbers and comma lists, which no export writes.
	private static TChoice ReadChoice<TChoice>(JsonElement? element, string key, TChoice fallback, ref string? error)
		where TChoice : struct, Enum
	{
		if (element is not { } value)
		{
			return fallback;
		}

		string name = value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
		string? known = Array.Find(Enum.GetNames<TChoice>(), candidate => IsKey(candidate, name));
		if (known is null)
		{
			error ??= $"{key} must be one of {string.Join(", ", Enum.GetNames<TChoice>())}.";
			return fallback;
		}

		return Enum.Parse<TChoice>(known);
	}

	private static string? Check(ValueKind kind, string value) => kind switch
	{
		ValueKind.Color when !CssValues.IsColor(value) => "must be a CSS color, like #ef5350 or rgb(239 83 80).",
		ValueKind.Length when !IsLength(value) =>
			"must be a CSS length, like 8px or 0.5rem, or a calc(), min(), max(), clamp() or var() expression.",
		ValueKind.Lengths when !IsLength(value) && !CssValues.IsLengthList(value) =>
			"must be CSS lengths, like 12px 14px, or a calc(), min(), max(), clamp() or var() expression.",
		ValueKind.Css when !IsCssValue(value) =>
			"cannot contain ; { } < > or \\, or leave a quote, bracket or comment open.",
		_ => null
	};

	private static bool IsLength(string value)
	{
		if (CssValues.IsLength(value))
		{
			return true;
		}

		ReadOnlySpan<char> trimmed = value.AsSpan().Trim();
		foreach (string function in LengthFunctions)
		{
			if (trimmed.StartsWith(function, StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(')'))
			{
				return IsCssValue(value);
			}
		}

		return false;
	}

	private static bool IsCssValue(string value) => CssValues.IsSafe(value) && CssValues.IsSelfContained(value);

	private static string DescribeSyntaxError(JsonException ex)
	{
		// The reader's message ends with a 0-based "LineNumber: 0 | BytePositionInLine: 3." part, which
		// reads badly next to editors that count from 1. Keep the reason and give the place 1-based.
		string reason = ex.Message;
		int cut = reason.IndexOf(" LineNumber:", StringComparison.Ordinal);
		if (cut > 0)
		{
			reason = reason[..cut];
		}

		return ex.LineNumber is { } line && ex.BytePositionInLine is { } position
			? string.Create(CultureInfo.InvariantCulture,
				$"Not valid JSON (line {line + 1}, position {position + 1}): {reason}")
			: $"Not valid JSON: {reason}";
	}

	// ── C# writing ────────────────────────────────────────────────

	private static void AppendObject<T>(StringBuilder sb, string propertyName, string typeName, T value, Field<T>[] fields)
	{
		sb.Append('\t').Append(propertyName).Append(" = new ").AppendLine(typeName);
		sb.AppendLine("\t{");
		foreach (Field<T> field in fields)
		{
			AppendAssignment(sb, "\t\t", field.Name, CSharpString(field.Get(value)));
		}

		sb.AppendLine("\t},");
	}

	private static void AppendAssignment(StringBuilder sb, string indent, string name, string literal) =>
		sb.Append(indent).Append(name).Append(" = ").Append(literal).AppendLine(",");

	private static string CSharpDouble(double value) => value switch
	{
		double.PositiveInfinity => "double.PositiveInfinity",
		double.NegativeInfinity => "double.NegativeInfinity",
		_ when double.IsNaN(value) => "double.NaN",
		_ => value.ToString("R", CultureInfo.InvariantCulture)
	};

	private static string CSharpString(string value)
	{
		var sb = new StringBuilder(value.Length + 2);
		sb.Append('"');
		foreach (char c in value)
		{
			switch (c)
			{
				case '"':
					sb.Append("\\\"");
					break;
				case '\\':
					sb.Append("\\\\");
					break;
				case '\n':
					sb.Append("\\n");
					break;
				case '\r':
					sb.Append("\\r");
					break;
				case '\t':
					sb.Append("\\t");
					break;
				case '\0':
					sb.Append("\\0");
					break;
				// Other control characters, and the characters C# treats as line breaks, cannot sit
				// in a regular string literal.
				case < ' ' or (char)0x7F or (char)0x85 or (char)0x2028 or (char)0x2029:
					sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
					break;
				default:
					sb.Append(c);
					break;
			}
		}

		sb.Append('"');
		return sb.ToString();
	}

	private enum ValueKind
	{
		Color,
		Length,
		Lengths,
		Css
	}

	private sealed class Field<T>(string name, Func<T, string> get, Func<T, string, T> set)
	{
		public string Name { get; } = name;

		public string JsonName { get; } = JsonNamingPolicy.CamelCase.ConvertName(name);

		public Func<T, string> Get { get; } = get;

		public Func<T, string, T> Set { get; } = set;
	}
}
