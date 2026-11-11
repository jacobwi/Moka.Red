namespace Moka.Red.Core.Enums;

/// <summary>
///     Decorative grid background pattern styles for <c>MokaGridBackground</c>.
/// </summary>
public enum MokaGridPattern
{
	/// <summary>Horizontal + vertical thin lines forming squares.</summary>
	Lines,

	/// <summary>Evenly spaced small dots at grid intersections.</summary>
	Dots,

	/// <summary>Dashed grid lines (SVG-based).</summary>
	Dashed,

	/// <summary>Small + signs at grid intersections (SVG-based).</summary>
	Cross,

	/// <summary>45-degree parallel diagonal lines.</summary>
	DiagonalLines,

	/// <summary>Hexagonal honeycomb pattern (SVG-based).</summary>
	Honeycomb,

	/// <summary>
	///     A fine grid with a stronger line every few cells, like engineering paper. The cell is 16 px unless
	///     <c>CellSize</c> says otherwise, and <c>MajorEvery</c> sets how many cells sit between the stronger lines.
	/// </summary>
	GraphPaper,

	/// <summary>Vertical lines crossed by two 30 degree families, the paper for drawing cubes (SVG-based).</summary>
	Isometric,

	/// <summary>Rows of equal triangles, every other one lightly filled (SVG-based).</summary>
	Triangles,

	/// <summary>A lattice of wide diamonds with a dot where they meet (SVG-based).</summary>
	Diamonds,

	/// <summary>A checkerboard of faint squares.</summary>
	Checker,

	/// <summary>Horizontal lines only, like lined paper.</summary>
	Rows,

	/// <summary>Vertical lines only, like a layout grid laid over the page.</summary>
	Columns,

	/// <summary>Circuit board traces with 45 degree bends, ending in round pads (SVG-based).</summary>
	Circuit,

	/// <summary>Contour lines around two hills, scaled to cover the box rather than repeated (SVG-based).</summary>
	Topographic,

	/// <summary>Rings from the centre with a crosshair through the middle.</summary>
	Radar,

	/// <summary>Small L marks at each cell's corners, like crop marks on a print sheet (SVG-based).</summary>
	CornerMarks,

	/// <summary>Faint noise over the whole box, no lines at all (SVG-based).</summary>
	Grain
}
