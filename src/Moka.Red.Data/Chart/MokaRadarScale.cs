namespace Moka.Red.Data.Chart;

/// <summary>How a <see cref="MokaRadarChart" /> maps values onto its axes.</summary>
public enum MokaRadarScale
{
	/// <summary>One scale for every axis, written along the top axis: for measures in the same unit, such as scores out of 10.</summary>
	Shared,

	/// <summary>
	///     A scale of its own on each axis, from its <see cref="MokaRadarAxis.Min" /> to its <see cref="MokaRadarAxis.Max" />,
	///     for measures in different units. The rings then mark quarters of each axis, and the tooltip and table give the
	///     values.
	/// </summary>
	PerAxis
}
