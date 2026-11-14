namespace Moka.Red.Core.Colors;

/// <summary>
///     A color in OKLab, Björn Ottosson's perceptual space: equal steps look like equal changes, so it suits mixing and
///     measuring how far apart two colors look.
/// </summary>
/// <param name="L">Perceived lightness, 0 (black) to 1 (white).</param>
/// <param name="A">Green (negative) to red (positive).</param>
/// <param name="B">Blue (negative) to yellow (positive).</param>
/// <param name="Alpha">Alpha, 0 to 1.</param>
public readonly record struct MokaOklab(double L, double A, double B, double Alpha = 1)
{
	/// <summary>The color in linear-light sRGB, not mapped into the gamut.</summary>
	/// <returns>The linear color.</returns>
	public MokaLinearRgb ToLinear()
	{
		double l = L + (0.3963377774 * A) + (0.2158037573 * B);
		double m = L - (0.1055613458 * A) - (0.0638541728 * B);
		double s = L - (0.0894841775 * A) - (1.2914855480 * B);
		l = l * l * l;
		m = m * m * m;
		s = s * s * s;
		return new MokaLinearRgb(
			(4.0767416621 * l) - (3.3077115913 * m) + (0.2309699292 * s),
			(-1.2684380046 * l) + (2.6097574011 * m) - (0.3413193965 * s),
			(-0.0041960863 * l) - (0.7034186147 * m) + (1.7076147010 * s),
			Alpha);
	}

	/// <summary>The color in sRGB, not mapped into the gamut: a channel can fall outside 0 to 1.</summary>
	/// <returns>The sRGB color.</returns>
	public MokaSrgb ToSrgb() => ToLinear().ToSrgb();

	/// <summary>The color in OKLCH. The hue of a grey is whatever the arithmetic gives; check the chroma before trusting it.</summary>
	/// <returns>The OKLCH color.</returns>
	public MokaOklch ToOklch()
	{
		double hue = Math.Atan2(B, A) * 180 / Math.PI;
		return new MokaOklch(L, Math.Sqrt((A * A) + (B * B)), MokaHsl.WrapHue(hue), Alpha);
	}

	/// <summary>
	///     How far apart two colors look: the straight distance in OKLab, which CSS Color 4 calls deltaEOK. About 0.02 is
	///     the smallest difference most people notice.
	/// </summary>
	/// <param name="other">The other color.</param>
	/// <returns>The distance.</returns>
	public double DistanceTo(MokaOklab other)
	{
		double dl = L - other.L, da = A - other.A, db = B - other.B;
		return Math.Sqrt((dl * dl) + (da * da) + (db * db));
	}
}
