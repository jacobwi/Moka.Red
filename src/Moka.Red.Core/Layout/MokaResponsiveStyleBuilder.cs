using System.Globalization;
using System.Text;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Layout;

/// <summary>
///     Generates scoped CSS media queries from MokaBreakpoint definitions.
///     Each component instance gets a unique CSS selector based on its ID.
/// </summary>
public static class MokaResponsiveStyleBuilder
{
	/// <summary>
	///     Builds a &lt;style&gt; block string with media queries for the given breakpoints,
	///     scoped to a unique component selector.
	/// </summary>
	public static string? BuildGridStyles(string uniqueSelector, IReadOnlyList<MokaBreakpoint> breakpoints)
	{
		ArgumentNullException.ThrowIfNull(breakpoints);
		ThrowIfNotAClassName(uniqueSelector);
		if (breakpoints.Count == 0)
		{
			return null;
		}

		var sb = new StringBuilder();
		foreach (MokaBreakpoint bp in OrderByWidth(breakpoints))
		{
			var declarations = new List<string>();

			if (bp.Columns.HasValue)
			{
				declarations.Add($"grid-template-columns: repeat({bp.Columns.Value}, 1fr)");
			}

			if (bp.ColumnsValue is not null)
			{
				declarations.Add($"grid-template-columns: {bp.ColumnsValue}");
			}

			if (bp.Rows.HasValue)
			{
				declarations.Add($"grid-template-rows: repeat({bp.Rows.Value}, 1fr)");
			}

			if (bp.RowsValue is not null)
			{
				declarations.Add($"grid-template-rows: {bp.RowsValue}");
			}

			if (bp.Justify.HasValue)
			{
				declarations.Add($"justify-items: {MokaEnumHelpers.ToCssValue(bp.Justify.Value)}");
			}

			if (bp.Align.HasValue)
			{
				declarations.Add($"align-items: {MokaEnumHelpers.ToCssValue(bp.Align.Value)}");
			}

			if (bp.Gap.HasValue)
			{
				declarations.Add($"gap: {MokaEnumHelpers.ToCssValue(bp.Gap.Value)}");
			}

			if (bp.GapValue is not null)
			{
				declarations.Add($"gap: {bp.GapValue}");
			}

			if (bp.RowGap.HasValue)
			{
				declarations.Add($"row-gap: {MokaEnumHelpers.ToCssValue(bp.RowGap.Value)}");
			}

			if (bp.RowGapValue is not null)
			{
				declarations.Add($"row-gap: {bp.RowGapValue}");
			}

			if (bp.Hidden == true)
			{
				declarations.Add("display: none");
			}

			AppendMediaRule(sb, uniqueSelector, bp.MinWidth, declarations);
		}

		return sb.Length > 0 ? sb.ToString() : null;
	}

	/// <summary>Builds media queries for flexbox breakpoints.</summary>
	public static string? BuildFlexboxStyles(string uniqueSelector, IReadOnlyList<MokaBreakpoint> breakpoints)
	{
		ArgumentNullException.ThrowIfNull(breakpoints);
		ThrowIfNotAClassName(uniqueSelector);
		if (breakpoints.Count == 0)
		{
			return null;
		}

		var sb = new StringBuilder();
		foreach (MokaBreakpoint bp in OrderByWidth(breakpoints))
		{
			var declarations = new List<string>();

			if (bp.Direction.HasValue)
			{
				declarations.Add($"flex-direction: {MokaEnumHelpers.ToCssValue(bp.Direction.Value)}");
			}

			if (bp.Wrap.HasValue)
			{
				declarations.Add(bp.Wrap.Value ? "flex-wrap: wrap" : "flex-wrap: nowrap");
			}

			if (bp.Justify.HasValue)
			{
				declarations.Add($"justify-content: {MokaEnumHelpers.ToCssValue(bp.Justify.Value)}");
			}

			if (bp.Align.HasValue)
			{
				declarations.Add($"align-items: {MokaEnumHelpers.ToCssValue(bp.Align.Value)}");
			}

			if (bp.Gap.HasValue)
			{
				declarations.Add($"gap: {MokaEnumHelpers.ToCssValue(bp.Gap.Value)}");
			}

			if (bp.GapValue is not null)
			{
				declarations.Add($"gap: {bp.GapValue}");
			}

			if (bp.RowGap.HasValue)
			{
				declarations.Add($"row-gap: {MokaEnumHelpers.ToCssValue(bp.RowGap.Value)}");
			}

			if (bp.RowGapValue is not null)
			{
				declarations.Add($"row-gap: {bp.RowGapValue}");
			}

			if (bp.Hidden == true)
			{
				declarations.Add("display: none");
			}

			AppendMediaRule(sb, uniqueSelector, bp.MinWidth, declarations);
		}

		return sb.Length > 0 ? sb.ToString() : null;
	}

	// The output goes into a <style> element, so a value that could end a declaration, a block or
	// the element itself would let breakpoint data rewrite the page's CSS or markup. Such a
	// declaration is dropped; so is the whole rule when the width is unsafe.
	private static void AppendMediaRule(StringBuilder sb, string selector, string minWidth, List<string> declarations)
	{
		declarations.RemoveAll(static d => !IsWritable(d));
		if (declarations.Count == 0 || !IsWritable(minWidth))
		{
			return;
		}

		sb.Append("@media(min-width:");
		sb.Append(minWidth);
		sb.Append("){.");
		sb.Append(selector);
		sb.Append('{');
		sb.Append(string.Join(';', declarations));
		sb.Append("}}");
	}

	// An open string or bracket would not escape the rule, but it would swallow every rule after it.
	private static bool IsWritable(string css) => CssValues.IsSafe(css) && CssValues.IsSelfContained(css);

	private static void ThrowIfNotAClassName(string selector)
	{
		ArgumentException.ThrowIfNullOrEmpty(selector);
		foreach (char c in selector)
		{
			if (!char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_')
			{
				throw new ArgumentException($"'{selector}' is not a plain CSS class name.", nameof(selector));
			}
		}
	}

	/// <summary>
	///     Orders breakpoints by ascending viewport width so later rules win the cascade.
	///     Sorting the raw <see cref="MokaBreakpoint.MinWidth" /> strings put "1024px" before "768px";
	///     this parses the numeric part and normalises rem/em to px at the 16px browser default so
	///     mixed units still sort correctly. Unparsable values sort last and keep their relative order.
	/// </summary>
	private static IEnumerable<MokaBreakpoint> OrderByWidth(IReadOnlyList<MokaBreakpoint> breakpoints) =>
		breakpoints.OrderBy(static b => ToPixels(b.MinWidth));

	private static double ToPixels(string minWidth)
	{
		ReadOnlySpan<char> span = minWidth.AsSpan().Trim();

		int digits = 0;
		while (digits < span.Length && (char.IsAsciiDigit(span[digits]) || span[digits] == '.'))
		{
			digits++;
		}

		if (digits == 0 ||
		    !double.TryParse(span[..digits], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
		{
			return double.MaxValue;
		}

		ReadOnlySpan<char> unit = span[digits..].Trim();
		return unit.Equals("rem", StringComparison.OrdinalIgnoreCase) || unit.Equals("em", StringComparison.OrdinalIgnoreCase)
			? value * 16
			: value;
	}
}
