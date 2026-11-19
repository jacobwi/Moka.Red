namespace Moka.Red.Primitives.LogViewer;

/// <summary>Which end of a <see cref="MokaLogViewer" /> holds the newest entry.</summary>
public enum MokaLogOrder
{
	/// <summary>The oldest entry at the top and the newest at the bottom, as a terminal writes them.</summary>
	OldestFirst,

	/// <summary>The newest entry at the top and older ones below it, as a feed shows them.</summary>
	NewestFirst
}
