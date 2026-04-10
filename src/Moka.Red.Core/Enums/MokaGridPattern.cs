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
	Honeycomb
}
