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
		if (breakpoints.Count == 0)
		{
			return null;
		}

		var sb = new StringBuilder();
		foreach (MokaBreakpoint bp in breakpoints.OrderBy(b => b.MinWidth))
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

			if (declarations.Count > 0)
			{
				sb.Append("@media(min-width:");
				sb.Append(bp.MinWidth);
				sb.Append("){.");
				sb.Append(uniqueSelector);
				sb.Append('{');
				sb.Append(string.Join(';', declarations));
				sb.Append("}}");
			}
		}

		return sb.Length > 0 ? sb.ToString() : null;
	}

	/// <summary>Builds media queries for flexbox breakpoints.</summary>
	public static string? BuildFlexboxStyles(string uniqueSelector, IReadOnlyList<MokaBreakpoint> breakpoints)
	{
		ArgumentNullException.ThrowIfNull(breakpoints);
		if (breakpoints.Count == 0)
		{
			return null;
		}

		var sb = new StringBuilder();
		foreach (MokaBreakpoint bp in breakpoints.OrderBy(b => b.MinWidth))
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

			if (bp.Hidden == true)
			{
				declarations.Add("display: none");
			}

			if (declarations.Count > 0)
			{
				sb.Append("@media(min-width:");
				sb.Append(bp.MinWidth);
				sb.Append("){.");
				sb.Append(uniqueSelector);
				sb.Append('{');
				sb.Append(string.Join(';', declarations));
				sb.Append("}}");
			}
		}

		return sb.Length > 0 ? sb.ToString() : null;
	}
}
