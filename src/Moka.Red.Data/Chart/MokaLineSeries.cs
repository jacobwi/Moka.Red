using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Chart;

/// <summary>One named line of a <see cref="MokaLineChart" />.</summary>
public sealed record MokaLineSeries
{
	/// <summary>The series name, shown in the legend and the point tooltips.</summary>
	public required string Name { get; init; }

	/// <summary>The values to draw, one per category. A <c>double.NaN</c> is a gap.</summary>
	public required IReadOnlyList<double> Values { get; init; }

	/// <summary>The color to draw the line in. Null = the palette, in rotation.</summary>
	public MokaColor? Color { get; init; }

	/// <summary>
	///     What identifies the series when the legend hides it, a highlight names it, or another chart pins its color.
	///     Null uses <see cref="Name" />.
	/// </summary>
	public string? Key { get; init; }

	/// <summary>
	///     The palette slot the series takes, counting from 1, so it keeps its color across charts that list it in a
	///     different place. Null takes its position in the list.
	/// </summary>
	public int? Slot { get; init; }

	/// <summary>A color of its own, any CSS color or token such as <c>var(--moka-chart-3)</c>. Anything else is ignored.</summary>
	public string? ColorValue { get; init; }

	/// <summary>
	///     The bottom of a band drawn around the line, one per value, such as the 5th percentile around a median. Needs
	///     <see cref="High" /> too. A <c>double.NaN</c> leaves the band out at that value.
	/// </summary>
	public IReadOnlyList<double>? Low { get; init; }

	/// <summary>The top of the band around the line, one per value, such as the 95th percentile.</summary>
	public IReadOnlyList<double>? High { get; init; }

	/// <summary>How this line is drawn between its values. Null takes the chart's <see cref="MokaLineChart.Curve" />.</summary>
	public MokaLineCurve? Curve { get; init; }

	/// <summary>
	///     Makes this series a comparison for the series with this key (its <see cref="Key" />, else its
	///     <see cref="Name" />), such as last week's values beside this week's. It is drawn faint and dashed in that
	///     series' color unless it sets its own, without dots, area or a name at its end; the tooltip says how far the
	///     other series is from it ("+12% vs Last week"), and the legend lists it with a dashed key. It counts in the
	///     value axis's range like any series, is never stacked, and hides with the series it belongs to. A key that names
	///     no series, or one that is itself a comparison, leaves it an ordinary series.
	/// </summary>
	public string? CompareTo { get; init; }
}
