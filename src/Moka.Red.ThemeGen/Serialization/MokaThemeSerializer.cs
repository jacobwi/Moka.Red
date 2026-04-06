using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Moka.Red.Core.Theming;

namespace Moka.Red.ThemeGen.Serialization;

/// <summary>
///     Serializes and deserializes <see cref="MokaTheme" /> to/from JSON, CSS, and C# code.
/// </summary>
public static class MokaThemeSerializer
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	/// <summary>Serialize a <see cref="MokaTheme" /> to JSON.</summary>
	public static string ToJson(MokaTheme theme)
	{
		ArgumentNullException.ThrowIfNull(theme);

		var dto = new ThemeDto
		{
			IsDark = theme.IsDark,
			Palette = PaletteDto.FromPalette(theme.Palette),
			Typography = TypographyDto.FromTypography(theme.Typography),
			Spacing = SpacingDto.FromSpacing(theme.Spacing)
		};

		return JsonSerializer.Serialize(dto, JsonOptions);
	}

	/// <summary>Deserialize a <see cref="MokaTheme" /> from JSON.</summary>
	public static MokaTheme? FromJson(string json)
	{
		try
		{
			ThemeDto? dto = JsonSerializer.Deserialize<ThemeDto>(json, JsonOptions);
			return dto?.ToTheme();
		}
		catch (JsonException)
		{
			return null;
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

	/// <summary>Export theme as C# code that can be pasted into source.</summary>
	public static string ToCSharp(MokaTheme theme)
	{
		ArgumentNullException.ThrowIfNull(theme);

		var sb = new StringBuilder(2048);
		sb.AppendLine("new MokaTheme");
		sb.AppendLine("{");
		AppendProp(sb, "\t", "IsDark", theme.IsDark ? "true" : "false", false);
		sb.AppendLine("\tPalette = new MokaPalette");
		sb.AppendLine("\t{");
		AppendProp(sb, "\t\t", "Primary", theme.Palette.Primary);
		AppendProp(sb, "\t\t", "PrimaryLight", theme.Palette.PrimaryLight);
		AppendProp(sb, "\t\t", "PrimaryDark", theme.Palette.PrimaryDark);
		AppendProp(sb, "\t\t", "OnPrimary", theme.Palette.OnPrimary);
		AppendProp(sb, "\t\t", "Secondary", theme.Palette.Secondary);
		AppendProp(sb, "\t\t", "SecondaryLight", theme.Palette.SecondaryLight);
		AppendProp(sb, "\t\t", "SecondaryDark", theme.Palette.SecondaryDark);
		AppendProp(sb, "\t\t", "OnSecondary", theme.Palette.OnSecondary);
		AppendProp(sb, "\t\t", "Surface", theme.Palette.Surface);
		AppendProp(sb, "\t\t", "SurfaceVariant", theme.Palette.SurfaceVariant);
		AppendProp(sb, "\t\t", "OnSurface", theme.Palette.OnSurface);
		AppendProp(sb, "\t\t", "Background", theme.Palette.Background);
		AppendProp(sb, "\t\t", "OnBackground", theme.Palette.OnBackground);
		AppendProp(sb, "\t\t", "Error", theme.Palette.Error);
		AppendProp(sb, "\t\t", "OnError", theme.Palette.OnError);
		AppendProp(sb, "\t\t", "Warning", theme.Palette.Warning);
		AppendProp(sb, "\t\t", "OnWarning", theme.Palette.OnWarning);
		AppendProp(sb, "\t\t", "Success", theme.Palette.Success);
		AppendProp(sb, "\t\t", "OnSuccess", theme.Palette.OnSuccess);
		AppendProp(sb, "\t\t", "Info", theme.Palette.Info);
		AppendProp(sb, "\t\t", "OnInfo", theme.Palette.OnInfo);
		AppendProp(sb, "\t\t", "Outline", theme.Palette.Outline);
		AppendProp(sb, "\t\t", "OutlineVariant", theme.Palette.OutlineVariant);
		sb.AppendLine("\t},");
		sb.AppendLine("\tTypography = new MokaTypography");
		sb.AppendLine("\t{");
		AppendProp(sb, "\t\t", "FontFamily", theme.Typography.FontFamily);
		AppendProp(sb, "\t\t", "FontFamilyMono", theme.Typography.FontFamilyMono);
		AppendProp(sb, "\t\t", "FontSizeXs", theme.Typography.FontSizeXs);
		AppendProp(sb, "\t\t", "FontSizeSm", theme.Typography.FontSizeSm);
		AppendProp(sb, "\t\t", "FontSizeBase", theme.Typography.FontSizeBase);
		AppendProp(sb, "\t\t", "FontSizeMd", theme.Typography.FontSizeMd);
		AppendProp(sb, "\t\t", "FontSizeLg", theme.Typography.FontSizeLg);
		AppendProp(sb, "\t\t", "FontSizeXl", theme.Typography.FontSizeXl);
		AppendProp(sb, "\t\t", "FontSizeXxl", theme.Typography.FontSizeXxl);
		AppendProp(sb, "\t\t", "LineHeightTight", theme.Typography.LineHeightTight);
		AppendProp(sb, "\t\t", "LineHeightBase", theme.Typography.LineHeightBase);
		AppendProp(sb, "\t\t", "LineHeightRelaxed", theme.Typography.LineHeightRelaxed);
		AppendProp(sb, "\t\t", "FontWeightLight", theme.Typography.FontWeightLight);
		AppendProp(sb, "\t\t", "FontWeightNormal", theme.Typography.FontWeightNormal);
		AppendProp(sb, "\t\t", "FontWeightMedium", theme.Typography.FontWeightMedium);
		AppendProp(sb, "\t\t", "FontWeightSemibold", theme.Typography.FontWeightSemibold);
		AppendProp(sb, "\t\t", "FontWeightBold", theme.Typography.FontWeightBold);
		sb.AppendLine("\t},");
		sb.AppendLine("\tSpacing = new MokaSpacing");
		sb.AppendLine("\t{");
		AppendProp(sb, "\t\t", "Xxs", theme.Spacing.Xxs);
		AppendProp(sb, "\t\t", "Xs", theme.Spacing.Xs);
		AppendProp(sb, "\t\t", "Sm", theme.Spacing.Sm);
		AppendProp(sb, "\t\t", "Md", theme.Spacing.Md);
		AppendProp(sb, "\t\t", "Lg", theme.Spacing.Lg);
		AppendProp(sb, "\t\t", "Xl", theme.Spacing.Xl);
		AppendProp(sb, "\t\t", "Xxl", theme.Spacing.Xxl);
		AppendProp(sb, "\t\t", "RadiusNone", theme.Spacing.RadiusNone);
		AppendProp(sb, "\t\t", "RadiusSm", theme.Spacing.RadiusSm);
		AppendProp(sb, "\t\t", "RadiusMd", theme.Spacing.RadiusMd);
		AppendProp(sb, "\t\t", "RadiusLg", theme.Spacing.RadiusLg);
		AppendProp(sb, "\t\t", "RadiusXl", theme.Spacing.RadiusXl);
		AppendProp(sb, "\t\t", "RadiusFull", theme.Spacing.RadiusFull);
		sb.AppendLine("\t},");
		sb.Append('}');

		return sb.ToString();
	}

	private static void AppendProp(StringBuilder sb, string indent, string name, string value, bool quote = true)
	{
		sb.Append(indent);
		sb.Append(name);
		sb.Append(" = ");
		if (quote)
		{
			sb.Append('"');
			sb.Append(value);
			sb.Append('"');
		}
		else
		{
			sb.Append(value);
		}

		sb.Append(',');
		sb.AppendLine();
	}

	/// <summary>DTO for JSON serialization of <see cref="MokaTheme" />.</summary>
	private sealed class ThemeDto
	{
		public bool IsDark { get; set; }
		public PaletteDto Palette { get; set; } = new();
		public TypographyDto Typography { get; set; } = new();
		public SpacingDto Spacing { get; set; } = new();

		public MokaTheme ToTheme() => new()
		{
			IsDark = IsDark,
			Palette = Palette.ToPalette(),
			Typography = Typography.ToTypography(),
			Spacing = Spacing.ToSpacing()
		};
	}

	/// <summary>DTO for JSON serialization of <see cref="MokaPalette" />.</summary>
	private sealed class PaletteDto
	{
		public string Primary { get; set; } = "#d32f2f";
		public string PrimaryLight { get; set; } = "#ff6659";
		public string PrimaryDark { get; set; } = "#9a0007";
		public string OnPrimary { get; set; } = "#ffffff";
		public string Secondary { get; set; } = "#455a64";
		public string SecondaryLight { get; set; } = "#718792";
		public string SecondaryDark { get; set; } = "#1c313a";
		public string OnSecondary { get; set; } = "#ffffff";
		public string Surface { get; set; } = "#ffffff";
		public string SurfaceVariant { get; set; } = "#f5f5f5";
		public string OnSurface { get; set; } = "#1c1b1f";
		public string Background { get; set; } = "#fafafa";
		public string OnBackground { get; set; } = "#1c1b1f";
		public string Error { get; set; } = "#b00020";
		public string OnError { get; set; } = "#ffffff";
		public string Warning { get; set; } = "#f57c00";
		public string OnWarning { get; set; } = "#ffffff";
		public string Success { get; set; } = "#2e7d32";
		public string OnSuccess { get; set; } = "#ffffff";
		public string Info { get; set; } = "#0288d1";
		public string OnInfo { get; set; } = "#ffffff";
		public string Outline { get; set; } = "#c4c4c4";
		public string OutlineVariant { get; set; } = "#e0e0e0";

		public static PaletteDto FromPalette(MokaPalette p) => new()
		{
			Primary = p.Primary,
			PrimaryLight = p.PrimaryLight,
			PrimaryDark = p.PrimaryDark,
			OnPrimary = p.OnPrimary,
			Secondary = p.Secondary,
			SecondaryLight = p.SecondaryLight,
			SecondaryDark = p.SecondaryDark,
			OnSecondary = p.OnSecondary,
			Surface = p.Surface,
			SurfaceVariant = p.SurfaceVariant,
			OnSurface = p.OnSurface,
			Background = p.Background,
			OnBackground = p.OnBackground,
			Error = p.Error,
			OnError = p.OnError,
			Warning = p.Warning,
			OnWarning = p.OnWarning,
			Success = p.Success,
			OnSuccess = p.OnSuccess,
			Info = p.Info,
			OnInfo = p.OnInfo,
			Outline = p.Outline,
			OutlineVariant = p.OutlineVariant
		};

		public MokaPalette ToPalette() => new()
		{
			Primary = Primary,
			PrimaryLight = PrimaryLight,
			PrimaryDark = PrimaryDark,
			OnPrimary = OnPrimary,
			Secondary = Secondary,
			SecondaryLight = SecondaryLight,
			SecondaryDark = SecondaryDark,
			OnSecondary = OnSecondary,
			Surface = Surface,
			SurfaceVariant = SurfaceVariant,
			OnSurface = OnSurface,
			Background = Background,
			OnBackground = OnBackground,
			Error = Error,
			OnError = OnError,
			Warning = Warning,
			OnWarning = OnWarning,
			Success = Success,
			OnSuccess = OnSuccess,
			Info = Info,
			OnInfo = OnInfo,
			Outline = Outline,
			OutlineVariant = OutlineVariant
		};
	}

	/// <summary>DTO for JSON serialization of <see cref="MokaTypography" />.</summary>
	private sealed class TypographyDto
	{
		public string FontFamily { get; set; } = MokaTypography.Default.FontFamily;
		public string FontFamilyMono { get; set; } = MokaTypography.Default.FontFamilyMono;
		public string FontSizeXs { get; set; } = "0.6875rem";
		public string FontSizeSm { get; set; } = "0.75rem";
		public string FontSizeBase { get; set; } = "0.8125rem";
		public string FontSizeMd { get; set; } = "0.875rem";
		public string FontSizeLg { get; set; } = "1rem";
		public string FontSizeXl { get; set; } = "1.25rem";
		public string FontSizeXxl { get; set; } = "1.5rem";
		public string LineHeightTight { get; set; } = "1.2";
		public string LineHeightBase { get; set; } = "1.4";
		public string LineHeightRelaxed { get; set; } = "1.6";
		public string FontWeightLight { get; set; } = "300";
		public string FontWeightNormal { get; set; } = "400";
		public string FontWeightMedium { get; set; } = "500";
		public string FontWeightSemibold { get; set; } = "600";
		public string FontWeightBold { get; set; } = "700";

		public static TypographyDto FromTypography(MokaTypography t) => new()
		{
			FontFamily = t.FontFamily,
			FontFamilyMono = t.FontFamilyMono,
			FontSizeXs = t.FontSizeXs,
			FontSizeSm = t.FontSizeSm,
			FontSizeBase = t.FontSizeBase,
			FontSizeMd = t.FontSizeMd,
			FontSizeLg = t.FontSizeLg,
			FontSizeXl = t.FontSizeXl,
			FontSizeXxl = t.FontSizeXxl,
			LineHeightTight = t.LineHeightTight,
			LineHeightBase = t.LineHeightBase,
			LineHeightRelaxed = t.LineHeightRelaxed,
			FontWeightLight = t.FontWeightLight,
			FontWeightNormal = t.FontWeightNormal,
			FontWeightMedium = t.FontWeightMedium,
			FontWeightSemibold = t.FontWeightSemibold,
			FontWeightBold = t.FontWeightBold
		};

		public MokaTypography ToTypography() => new()
		{
			FontFamily = FontFamily,
			FontFamilyMono = FontFamilyMono,
			FontSizeXs = FontSizeXs,
			FontSizeSm = FontSizeSm,
			FontSizeBase = FontSizeBase,
			FontSizeMd = FontSizeMd,
			FontSizeLg = FontSizeLg,
			FontSizeXl = FontSizeXl,
			FontSizeXxl = FontSizeXxl,
			LineHeightTight = LineHeightTight,
			LineHeightBase = LineHeightBase,
			LineHeightRelaxed = LineHeightRelaxed,
			FontWeightLight = FontWeightLight,
			FontWeightNormal = FontWeightNormal,
			FontWeightMedium = FontWeightMedium,
			FontWeightSemibold = FontWeightSemibold,
			FontWeightBold = FontWeightBold
		};
	}

	/// <summary>DTO for JSON serialization of <see cref="MokaSpacing" />.</summary>
	private sealed class SpacingDto
	{
		public string Xxs { get; set; } = "0.125rem";
		public string Xs { get; set; } = "0.25rem";
		public string Sm { get; set; } = "0.375rem";
		public string Md { get; set; } = "0.5rem";
		public string Lg { get; set; } = "0.75rem";
		public string Xl { get; set; } = "1rem";
		public string Xxl { get; set; } = "1.5rem";
		public string RadiusNone { get; set; } = "0";
		public string RadiusSm { get; set; } = "0.125rem";
		public string RadiusMd { get; set; } = "0.25rem";
		public string RadiusLg { get; set; } = "0.375rem";
		public string RadiusXl { get; set; } = "0.5rem";
		public string RadiusFull { get; set; } = "9999px";

		public static SpacingDto FromSpacing(MokaSpacing s) => new()
		{
			Xxs = s.Xxs,
			Xs = s.Xs,
			Sm = s.Sm,
			Md = s.Md,
			Lg = s.Lg,
			Xl = s.Xl,
			Xxl = s.Xxl,
			RadiusNone = s.RadiusNone,
			RadiusSm = s.RadiusSm,
			RadiusMd = s.RadiusMd,
			RadiusLg = s.RadiusLg,
			RadiusXl = s.RadiusXl,
			RadiusFull = s.RadiusFull
		};

		public MokaSpacing ToSpacing() => new()
		{
			Xxs = Xxs,
			Xs = Xs,
			Sm = Sm,
			Md = Md,
			Lg = Lg,
			Xl = Xl,
			Xxl = Xxl,
			RadiusNone = RadiusNone,
			RadiusSm = RadiusSm,
			RadiusMd = RadiusMd,
			RadiusLg = RadiusLg,
			RadiusXl = RadiusXl,
			RadiusFull = RadiusFull
		};
	}
}
