using System.Globalization;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     What the charts drawn in rows share (dot plot, box plot, gantt, funnel): places on the track as CSS, values written
///     as the value axis writes them, row order, and the tooltip and keyboard reading of a row.
/// </summary>
internal static class ChartRows
{
	/// <summary>A share of the track as a CSS percentage, clamped to the track.</summary>
	/// <param name="share">The share, 0 to 1.</param>
	public static string Css(double share) => ChartWholes.Css(double.IsFinite(share) ? Math.Clamp(share, 0, 1) : 0);

	/// <summary>
	///     A value written as the value axis writes it, at its own precision, without asking the axis's scale: rows are
	///     sized from their text while the axis is still being worked out.
	/// </summary>
	/// <param name="chart">The chart.</param>
	/// <param name="value">The value.</param>
	public static string Write(MokaCartesianChartBase chart, double value) => chart.WriteValue(value, chart.XAxis);

	/// <summary>A change written with its sign, such as "+12 ms" or "-3.5%".</summary>
	/// <param name="written">The change's size, written without a sign.</param>
	/// <param name="change">The change.</param>
	public static string Signed(string written, double change) =>
		change > 0 ? "+" + written : change < 0 ? "-" + written : written;

	/// <summary>The rows in the order they are drawn: as given, or by value, missing values last.</summary>
	/// <param name="count">How many rows.</param>
	/// <param name="value">A row's value.</param>
	/// <param name="sort">How to order them.</param>
	public static int[] Order(int count, Func<int, double> value, MokaChartSort sort)
	{
		int[] order = [.. Enumerable.Range(0, Math.Max(0, count))];
		if (sort == MokaChartSort.None)
		{
			return order;
		}

		double sign = sort == MokaChartSort.Descending ? -1 : 1;
		return
		[
			.. order.OrderBy(i => double.IsFinite(value(i)) ? 0 : 1)
				.ThenBy(i => double.IsFinite(value(i)) ? sign * value(i) : 0)
		];
	}

	/// <summary>The color behind a row's class, for a paler copy of it: muted, a tone, the chart's own color, or a slot.</summary>
	/// <param name="colorClass">The row's color class.</param>
	/// <param name="colorStyle">The row's inline color, if any.</param>
	public static string BaseColor(string colorClass, string? colorStyle)
	{
		const string inline = "--moka-chart-color:";
		const string tone = "moka-chart-tone-";
		const string slot = "moka-chart-c";
		if (colorStyle is { } style && style.StartsWith(inline, StringComparison.Ordinal))
		{
			return style[inline.Length..].Trim().TrimEnd(';');
		}

		if (colorClass == "moka-chart-muted")
		{
			return "var(--moka-chart-muted)";
		}

		if (colorClass.StartsWith(tone, StringComparison.Ordinal))
		{
			string name = colorClass[tone.Length..];
			return name == "surface" ? "var(--moka-color-on-surface)" : $"var(--moka-color-{name})";
		}

		return colorClass.StartsWith(slot, StringComparison.Ordinal)
			? $"var(--moka-chart-{colorClass[slot.Length..]})"
			: "var(--moka-chart-1)";
	}

	/// <summary>An inline color that is a paler mix of another with the surface, for a "before" mark.</summary>
	/// <param name="color">The color to pale.</param>
	public static string PaleStyle(string color) => new StyleBuilder()
		.AddStyle("--moka-chart-color", $"color-mix(in srgb, {color} 42%, var(--moka-color-surface))")
		.Build()!;

	/// <summary>An inline color that hides a tooltip row's key, for a row that adds a measure to the one above it.</summary>
	public static string NoKey { get; } = new StyleBuilder().AddStyle("--moka-chart-color", "transparent").Build()!;

	/// <summary>What the tooltip and the keyboard cell say for a row.</summary>
	/// <param name="drawn">The row's drawn position.</param>
	/// <param name="heading">Its name.</param>
	/// <param name="rows">One tooltip row per measure.</param>
	/// <param name="notes">Lines under them.</param>
	/// <param name="points">What a template and the callbacks get.</param>
	public static ChartActiveInfo Describe(int drawn, string heading, IReadOnlyList<ChartActiveRow> rows,
		IReadOnlyList<string> notes, IReadOnlyList<MokaChartPoint> points)
	{
		List<string> parts = [heading];
		parts.AddRange(rows.Select(row => string.IsNullOrEmpty(row.Name) ? row.ValueText : $"{row.Name} {row.ValueText}"));
		parts.AddRange(notes);
		MokaChartPoint? focus = points.Count > 0 ? points[0] : null;
		return new ChartActiveInfo(heading, rows, notes, string.Join(", ", parts),
			new MokaChartTooltipContext(drawn, heading, points, focus, null, notes));
	}

	/// <summary>A length of time as people read it: "41 min", "2 h 5 min", "45 s", "3 d 4 h".</summary>
	/// <param name="span">The length.</param>
	public static string Duration(TimeSpan span)
	{
		if (span < TimeSpan.Zero)
		{
			span = span.Negate();
		}

		CultureInfo culture = CultureInfo.CurrentCulture;
		if (span.TotalSeconds < 60)
		{
			return $"{Math.Round(span.TotalSeconds).ToString("0", culture)} s";
		}

		if (span.TotalMinutes < 60)
		{
			return $"{Math.Round(span.TotalMinutes).ToString("0", culture)} min";
		}

		if (span.TotalHours < 24)
		{
			int minutes = (int)Math.Round(span.TotalMinutes);
			int hours = minutes / 60;
			int rest = minutes % 60;
			return rest == 0
				? $"{hours.ToString(culture)} h"
				: $"{hours.ToString(culture)} h {rest.ToString(culture)} min";
		}

		int totalHours = (int)Math.Round(span.TotalHours);
		int days = totalHours / 24;
		int leftHours = totalHours % 24;
		return leftHours == 0 ? $"{days.ToString(culture)} d" : $"{days.ToString(culture)} d {leftHours.ToString(culture)} h";
	}

	/// <summary>The tone class of an explicit color, or null.</summary>
	/// <param name="color">The color.</param>
	public static string ToneClass(MokaColor color) => $"moka-chart-tone-{MokaEnumHelpers.ToCssClass(color)}";
}
