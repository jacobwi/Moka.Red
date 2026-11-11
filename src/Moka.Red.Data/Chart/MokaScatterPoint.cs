namespace Moka.Red.Data.Chart;

/// <summary>
///     One point of a <see cref="MokaScatterChart" />: where it sits on the two measures, and for a bubble chart a third
///     measure its area follows. A struct, so a hundred thousand points cost no allocation each.
/// </summary>
/// <param name="X">Its value across.</param>
/// <param name="Y">Its value up.</param>
/// <param name="Size">A third measure; the point's area follows it, which makes the chart a bubble chart. Null draws a dot.</param>
/// <param name="Label">What the point is, for the tooltip and the table, such as "router-2".</param>
public readonly record struct MokaScatterPoint(double X, double Y, double? Size = null, string? Label = null);
