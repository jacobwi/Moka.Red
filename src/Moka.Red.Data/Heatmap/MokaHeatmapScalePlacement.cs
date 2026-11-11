namespace Moka.Red.Data.Heatmap;

/// <summary>Where a <see cref="MokaHeatmap" />'s key from little to most sits under the cells.</summary>
public enum MokaHeatmapScalePlacement
{
	/// <summary>At the end of the heatmap's width, however wide that is. Today's key.</summary>
	End,

	/// <summary>At the start of the heatmap's width.</summary>
	Start,

	/// <summary>In the middle of the heatmap's width.</summary>
	Center,

	/// <summary>
	///     Under the grid's cells, ending where they end, so it stays beside them when the heatmap is wider than its grid.
	///     A calendar is as wide as its days, so there it is the same as <see cref="End" />.
	/// </summary>
	GridEnd
}
