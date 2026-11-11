namespace Moka.Red.Data.Heatmap;

/// <summary>How a <see cref="MokaHeatmap" /> arranges its cells.</summary>
public enum MokaHeatmapLayout
{
	/// <summary>One square per day, weeks across and weekdays down, from <c>Data</c>. Today's heatmap.</summary>
	Calendar,

	/// <summary>
	///     Rows by columns from <c>GridValues</c>, with labels on both, such as hour by weekday to show when things happen.
	///     On a narrow screen it turns on its side, so the longer run goes down.
	/// </summary>
	Grid
}
