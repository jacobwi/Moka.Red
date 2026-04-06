using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Moka.Red.Core.Enums;

namespace Moka.Red.Core.Utilities;

/// <summary>
///     Shared helpers for converting Moka enums to CSS class name segments and size values.
/// </summary>
public static class MokaEnumHelpers
{
	private static readonly ConcurrentDictionary<(Type, int), string> s_cssClassCache = new();

	/// <summary>Converts <see cref="MokaSize" /> to a kebab-case CSS class segment.</summary>
	public static string ToCssClass(MokaSize size) => size switch
	{
		MokaSize.Xs => "xs",
		MokaSize.Sm => "sm",
		MokaSize.Md => "md",
		MokaSize.Lg => "lg",
		_ => "md"
	};

	/// <summary>Converts <see cref="MokaColor" /> to a kebab-case CSS class segment.</summary>
	public static string ToCssClass(MokaColor color) => color switch
	{
		MokaColor.Primary => "primary",
		MokaColor.Secondary => "secondary",
		MokaColor.Error => "error",
		MokaColor.Warning => "warning",
		MokaColor.Success => "success",
		MokaColor.Info => "info",
		MokaColor.Surface => "surface",
		_ => "primary"
	};

	/// <summary>Converts <see cref="MokaVariant" /> to a kebab-case CSS class segment.</summary>
	public static string ToCssClass(MokaVariant variant) => variant switch
	{
		MokaVariant.Filled => "filled",
		MokaVariant.Outlined => "outlined",
		MokaVariant.Text => "text",
		MokaVariant.Soft => "soft",
		_ => "filled"
	};

	/// <summary>Maps a <see cref="MokaSize" /> enum value to a pixel string.</summary>
	public static string ToPixels(MokaSize size) => size switch
	{
		MokaSize.Xs => "14px",
		MokaSize.Sm => "16px",
		MokaSize.Md => "20px",
		MokaSize.Lg => "24px",
		_ => "20px"
	};

	/// <summary>Converts <see cref="MokaSpacingScale" /> to a CSS custom property reference.</summary>
	public static string ToCssValue(MokaSpacingScale spacing) => spacing switch
	{
		MokaSpacingScale.None => "0",
		MokaSpacingScale.Xxs => "var(--moka-spacing-xxs)",
		MokaSpacingScale.Xs => "var(--moka-spacing-xs)",
		MokaSpacingScale.Sm => "var(--moka-spacing-sm)",
		MokaSpacingScale.Md => "var(--moka-spacing-md)",
		MokaSpacingScale.Lg => "var(--moka-spacing-lg)",
		MokaSpacingScale.Xl => "var(--moka-spacing-xl)",
		MokaSpacingScale.Xxl => "var(--moka-spacing-xxl)",
		_ => "0"
	};

	/// <summary>Converts <see cref="MokaDirection" /> to a CSS flex-direction value.</summary>
	public static string ToCssValue(MokaDirection dir) => dir switch
	{
		MokaDirection.Column => "column",
		MokaDirection.Row => "row",
		MokaDirection.ColumnReverse => "column-reverse",
		MokaDirection.RowReverse => "row-reverse",
		_ => "column"
	};

	/// <summary>Converts <see cref="MokaJustify" /> to a CSS justify-content value.</summary>
	public static string ToCssValue(MokaJustify justify) => justify switch
	{
		MokaJustify.Start => "flex-start",
		MokaJustify.Center => "center",
		MokaJustify.End => "flex-end",
		MokaJustify.SpaceBetween => "space-between",
		MokaJustify.SpaceAround => "space-around",
		MokaJustify.SpaceEvenly => "space-evenly",
		_ => "flex-start"
	};

	/// <summary>Converts <see cref="MokaAlign" /> to a CSS align-items value.</summary>
	public static string ToCssValue(MokaAlign align) => align switch
	{
		MokaAlign.Start => "flex-start",
		MokaAlign.Center => "center",
		MokaAlign.End => "flex-end",
		MokaAlign.Stretch => "stretch",
		MokaAlign.Baseline => "baseline",
		_ => "stretch"
	};

	/// <summary>Converts <see cref="MokaFontWeight" /> to a CSS custom property reference.</summary>
	public static string ToCssValue(MokaFontWeight weight) => weight switch
	{
		MokaFontWeight.Light => "var(--moka-font-weight-light)",
		MokaFontWeight.Normal => "var(--moka-font-weight-normal)",
		MokaFontWeight.Medium => "var(--moka-font-weight-medium)",
		MokaFontWeight.Semibold => "var(--moka-font-weight-semibold)",
		MokaFontWeight.Bold => "var(--moka-font-weight-bold)",
		_ => "var(--moka-font-weight-normal)"
	};

	/// <summary>Converts <see cref="MokaTextAlign" /> to a CSS text-align value.</summary>
	public static string ToCssValue(MokaTextAlign align) => align switch
	{
		MokaTextAlign.Left => "left",
		MokaTextAlign.Center => "center",
		MokaTextAlign.Right => "right",
		MokaTextAlign.Justify => "justify",
		_ => "left"
	};

	/// <summary>Converts <see cref="MokaTextTransform" /> to a CSS text-transform value.</summary>
	public static string ToCssValue(MokaTextTransform transform) => transform switch
	{
		MokaTextTransform.None => "none",
		MokaTextTransform.Uppercase => "uppercase",
		MokaTextTransform.Lowercase => "lowercase",
		MokaTextTransform.Capitalize => "capitalize",
		_ => "none"
	};

	/// <summary>Converts <see cref="MokaDockPosition" /> to a CSS class segment.</summary>
	public static string ToCssClass(MokaDockPosition position) => position switch
	{
		MokaDockPosition.Left => "left",
		MokaDockPosition.Right => "right",
		MokaDockPosition.Top => "top",
		MokaDockPosition.Bottom => "bottom",
		_ => "left"
	};

	/// <summary>Converts <see cref="MokaRounding" /> to a CSS border-radius value.</summary>
	public static string ToCssValue(MokaRounding rounding) => rounding switch
	{
		MokaRounding.None => "0",
		MokaRounding.Sm => "var(--moka-radius-sm)",
		MokaRounding.Md => "var(--moka-radius-md)",
		MokaRounding.Lg => "var(--moka-radius-lg)",
		MokaRounding.Xl => "var(--moka-radius-xl)",
		MokaRounding.Full => "var(--moka-radius-full)",
		_ => "var(--moka-radius-md)"
	};

	/// <summary>Maps <see cref="MokaSize" /> to a --moka-font-size-* CSS custom property.</summary>
	public static string ToFontSize(MokaSize size) => size switch
	{
		MokaSize.Xs => "var(--moka-font-size-xs)",
		MokaSize.Sm => "var(--moka-font-size-sm)",
		MokaSize.Md => "var(--moka-font-size-base)",
		MokaSize.Lg => "var(--moka-font-size-lg)",
		_ => "var(--moka-font-size-base)"
	};

	/// <summary>
	///     Generic fallback: converts any enum value to lowercase kebab-case.
	///     Results are cached per (enum type, value) pair — zero allocation after warmup.
	///     "TopRight" → "top-right", "Error" → "error", "SpaceBetween" → "space-between"
	/// </summary>
	public static string ToCssClass<TEnum>(TEnum value) where TEnum : struct, Enum
	{
		return s_cssClassCache.GetOrAdd(
			(typeof(TEnum), value.GetHashCode()),
			static key =>
			{
				string name = Enum.GetName(key.Item1, key.Item2) ?? key.Item2.ToString(CultureInfo.InvariantCulture);
				return ConvertToKebabCase(name);
			});
	}

	private static string ConvertToKebabCase(string name)
	{
		var sb = new StringBuilder(name.Length + 4);
		for (int i = 0; i < name.Length; i++)
		{
			if (i > 0 && char.IsUpper(name[i]))
			{
				sb.Append('-');
			}

			sb.Append(char.ToLowerInvariant(name[i]));
		}

		return sb.ToString();
	}
}
