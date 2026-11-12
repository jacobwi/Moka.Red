namespace Moka.Red.Data.Chart;

/// <summary>The trend a line chart draws over each series, as a thin dashed line in the series' color.</summary>
public enum MokaChartTrend
{
	/// <summary>No trend.</summary>
	None,

	/// <summary>The mean of each value and the ones before it, over a window of values.</summary>
	MovingAverage,

	/// <summary>The straight line nearest to the values, by least squares.</summary>
	Linear
}
