namespace Moka.Red.Primitives.SelectionBar;

/// <summary>What <see cref="MokaSelectionBar" /> does with actions that don't fit on one row.</summary>
public enum MokaSelectionBarOverflow
{
	/// <summary>The bar wraps onto more rows, as it always has.</summary>
	Wrap,

	/// <summary>
	///     The bar keeps one row. <see cref="MokaSelectionAction" />s that don't fit go into a More menu at the end of the
	///     row, the ones with the lowest <see cref="MokaSelectionAction.Priority" /> first, then from the end. Anything else
	///     among the actions keeps its place. Needs an interactive render; without one the bar wraps.
	/// </summary>
	Menu
}
