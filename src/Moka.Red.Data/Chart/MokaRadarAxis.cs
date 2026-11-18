namespace Moka.Red.Data.Chart;

/// <summary>One spoke of a <see cref="MokaRadarChart" />: a measure every series has a value on.</summary>
/// <param name="Name">What the axis measures, written at its end.</param>
public sealed record MokaRadarAxis(string Name)
{
	/// <summary>
	///     The value at the center under <see cref="MokaRadarScale.PerAxis" />. Null starts at zero, or lower when a value
	///     is below zero. Ignored on a shared scale, which takes the chart's <c>Min</c>.
	/// </summary>
	public double? Min { get; init; }

	/// <summary>
	///     The value at the edge under <see cref="MokaRadarScale.PerAxis" />, such as 100 for a score out of 100. Null takes
	///     a round number at or above the axis's largest value. Ignored on a shared scale, which takes the chart's <c>Max</c>.
	/// </summary>
	public double? Max { get; init; }

	/// <summary>
	///     Whether less is better on this axis, so the axis runs from its highest value at the center to its lowest at the
	///     edge and further out always reads as better, such as a price or a response time beside scores.
	/// </summary>
	public bool Reversed { get; init; }

	/// <summary>
	///     Writes this axis's values in the tooltip and the table, such as <c>v =&gt; $"{v:0} ms"</c>, when its unit differs
	///     from the others'. Null takes the chart's <c>Formatter</c>. Keep it a field or a method, as with the chart's.
	/// </summary>
	public Func<double, string>? Formatter { get; init; }
}
