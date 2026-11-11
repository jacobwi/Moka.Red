using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Chart;

/// <summary>One named set of bars of a <see cref="MokaBarChart" />.</summary>
public sealed record MokaBarSeries
{
	/// <summary>The series name, shown in the legend and the bar tooltips.</summary>
	public required string Name { get; init; }

	/// <summary>The values to draw, one per category.</summary>
	public required IReadOnlyList<double> Values { get; init; }

	/// <summary>The color to draw the bars in. Null = the palette, in rotation.</summary>
	public MokaColor? Color { get; init; }

	/// <summary>The series' identity for hiding, highlighting and colors. Null uses <see cref="Name" />.</summary>
	public string? Key { get; init; }

	/// <summary>The palette slot, counting from 1, pinned so the series keeps its color across charts. Null takes its place in the list.</summary>
	public int? Slot { get; init; }

	/// <summary>Any CSS color or token to draw the bars in, such as <c>var(--moka-color-info)</c>. Wins over <see cref="Color" />.</summary>
	public string? ColorValue { get; init; }

	/// <summary>
	///     A color for each value's bar, by position, such as the three busiest hours in <c>var(--moka-color-warning)</c>.
	///     Null, a missing entry or anything that isn't a CSS color keeps the series' color. The legend, the series'
	///     hatching under a texture, hiding and highlighting stay the series'; a highlight elsewhere still mutes these
	///     bars, and merged columns and <c>Diverging</c> bars draw in their usual colors.
	/// </summary>
	public IReadOnlyList<string?>? Colors { get; init; }
}
