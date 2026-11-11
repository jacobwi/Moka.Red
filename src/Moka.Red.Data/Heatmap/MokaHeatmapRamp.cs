namespace Moka.Red.Data.Heatmap;

/// <summary>The colors a <see cref="MokaHeatmap" /> steps through from little to most.</summary>
public enum MokaHeatmapRamp
{
	/// <summary>The accent in four steps of transparency. Today's calendar.</summary>
	Accent,

	/// <summary>Five solid steps of one hue, lightest to deepest, from the chart tokens. Reads more evenly on any surface.</summary>
	Steps
}
