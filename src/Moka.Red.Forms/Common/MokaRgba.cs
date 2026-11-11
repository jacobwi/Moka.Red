namespace Moka.Red.Forms.Common;

/// <summary>An sRGB color with 8-bit channels and an alpha from 0 to 1.</summary>
/// <param name="R">Red, 0 to 255.</param>
/// <param name="G">Green, 0 to 255.</param>
/// <param name="B">Blue, 0 to 255.</param>
/// <param name="A">Alpha, 0 (see-through) to 1 (solid).</param>
internal readonly record struct MokaRgba(int R, int G, int B, double A = 1)
{
	/// <summary>The same color, solid.</summary>
	internal MokaRgba Opaque => this with { A = 1 };
}
