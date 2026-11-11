using Moka.Red.Core.Charts;
using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Chart;

/// <summary>One row of a <see cref="MokaBoxPlot" />: a group's name and its values.</summary>
/// <param name="Name">What the group is, such as an endpoint.</param>
/// <param name="Values">The values themselves, in any order. A value that isn't a finite number doesn't count.</param>
public sealed record MokaBoxPlotGroup(string Name, IReadOnlyList<double> Values)
{
	/// <summary>
	///     The box worked out already, such as by the server for a million values. Wins over <see cref="Values" />, which
	///     then only feed the points.
	/// </summary>
	public MokaBoxSummary? Summary { get; init; }

	/// <summary>The color of the group's box. Null takes the chart's first palette color.</summary>
	public MokaColor? Color { get; init; }

	/// <summary>Its identity for highlighting. Null uses <see cref="Name" />.</summary>
	public string? Key { get; init; }
}
