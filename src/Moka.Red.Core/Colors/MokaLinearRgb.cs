namespace Moka.Red.Core.Colors;

/// <summary>
///     A color in linear-light sRGB: the sRGB primaries with the gamma curve taken off, where light adds up. Luminance and
///     OKLab are computed from here.
/// </summary>
/// <param name="R">Red light, 0 to 1 inside the gamut.</param>
/// <param name="G">Green light, 0 to 1 inside the gamut.</param>
/// <param name="B">Blue light, 0 to 1 inside the gamut.</param>
/// <param name="Alpha">Alpha, 0 to 1.</param>
public readonly record struct MokaLinearRgb(double R, double G, double B, double Alpha = 1)
{
	/// <summary>The WCAG 2 relative luminance of these channels as they are, without clipping.</summary>
	public double Luminance => (0.2126 * R) + (0.7152 * G) + (0.0722 * B);

	/// <summary>Whether every channel is within sRGB, give or take <see cref="MokaSrgb.GamutTolerance" />.</summary>
	public bool IsInGamut => InUnit(R) && InUnit(G) && InUnit(B);

	/// <summary>The color gamma-encoded as sRGB.</summary>
	/// <returns>The sRGB color.</returns>
	public MokaSrgb ToSrgb() => new(MokaSrgb.Encode(R), MokaSrgb.Encode(G), MokaSrgb.Encode(B), Alpha);

	/// <summary>The color in OKLab, through Björn Ottosson's matrices.</summary>
	/// <returns>The OKLab color.</returns>
	public MokaOklab ToOklab()
	{
		double l = Math.Cbrt((0.4122214708 * R) + (0.5363325363 * G) + (0.0514459929 * B));
		double m = Math.Cbrt((0.2119034982 * R) + (0.6806995451 * G) + (0.1073969566 * B));
		double s = Math.Cbrt((0.0883024619 * R) + (0.2817188376 * G) + (0.6299787005 * B));
		return new MokaOklab(
			(0.2104542553 * l) + (0.7936177850 * m) - (0.0040720468 * s),
			(1.9779984951 * l) - (2.4285922050 * m) + (0.4505937099 * s),
			(0.0259040371 * l) + (0.7827717662 * m) - (0.8086757660 * s),
			Alpha);
	}

	private static bool InUnit(double channel) =>
		channel is >= -MokaSrgb.GamutTolerance and <= 1 + MokaSrgb.GamutTolerance;
}
