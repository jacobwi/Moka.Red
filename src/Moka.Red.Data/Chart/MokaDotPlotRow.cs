using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Chart;

/// <summary>One row of a <see cref="MokaDotPlot" />: a name and its value, and for a dumbbell the value before.</summary>
/// <param name="Name">What the row is, such as a service or an endpoint.</param>
/// <param name="Value">Its value, or the value after when <see cref="Before" /> is set.</param>
public sealed record MokaDotPlotRow(string Name, double Value)
{
	/// <summary>The value before, which turns the row into a dumbbell: a pale dot, a dark dot and the line between.</summary>
	public double? Before { get; init; }

	/// <summary>The color of the row's dots. Null takes the chart's first palette color.</summary>
	public MokaColor? Color { get; init; }

	/// <summary>A line about the row for the tooltip, such as "4 min down".</summary>
	public string? Detail { get; init; }

	/// <summary>Its identity for highlighting. Null uses <see cref="Name" />.</summary>
	public string? Key { get; init; }
}
