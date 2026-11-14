namespace Moka.Red.Core.Colors;

/// <summary>
///     Mixes two colors the way CSS <c>color-mix()</c> does: each color's channels are weighted by its alpha before they
///     are mixed, so mixing toward <c>transparent</c> fades a color instead of darkening it.
/// </summary>
public static class MokaColorMix
{
	/// <summary>
	///     <c>color-mix(in srgb, first {share}, second)</c>: the gamma-encoded channels mixed, as a browser mixes in sRGB.
	/// </summary>
	/// <param name="first">The first color.</param>
	/// <param name="second">The second color.</param>
	/// <param name="firstShare">How much of the first color, 0 to 1. The second gets the rest.</param>
	/// <returns>The mix.</returns>
	public static MokaSrgb InSrgb(MokaSrgb first, MokaSrgb second, double firstShare)
	{
		(double r, double g, double b, double alpha) = Mix(
			(first.R, first.G, first.B, first.Alpha),
			(second.R, second.G, second.B, second.Alpha),
			firstShare);
		return new MokaSrgb(r, g, b, alpha);
	}

	/// <summary>
	///     <c>color-mix(in oklab, first {share}, second)</c>: mixed in OKLab, so a step reads as the same change in lightness
	///     whatever the hue.
	/// </summary>
	/// <param name="first">The first color.</param>
	/// <param name="second">The second color.</param>
	/// <param name="firstShare">How much of the first color, 0 to 1. The second gets the rest.</param>
	/// <returns>
	///     The mix in sRGB. Two colors inside sRGB can mix to one a hair outside it; <see cref="MokaSrgb.ToHex" /> and
	///     <see cref="MokaSrgb.ToBytes()" /> clip, as a browser does when it paints.
	/// </returns>
	public static MokaSrgb InOklab(MokaSrgb first, MokaSrgb second, double firstShare)
	{
		MokaOklab a = first.ToOklab();
		MokaOklab b = second.ToOklab();
		(double l, double aa, double bb, double alpha) = Mix((a.L, a.A, a.B, a.Alpha), (b.L, b.A, b.B, b.Alpha), firstShare);
		return new MokaOklab(l, aa, bb, alpha).ToSrgb();
	}

	private static (double X, double Y, double Z, double Alpha) Mix(
		(double X, double Y, double Z, double Alpha) first,
		(double X, double Y, double Z, double Alpha) second,
		double firstShare)
	{
		double p = Math.Clamp(firstShare, 0, 1);
		double firstAlpha = Math.Clamp(first.Alpha, 0, 1);
		double secondAlpha = Math.Clamp(second.Alpha, 0, 1);
		double alpha = (firstAlpha * p) + (secondAlpha * (1 - p));
		if (alpha <= 0)
		{
			return (0, 0, 0, 0);
		}

		double Channel(double a, double b) => ((a * firstAlpha * p) + (b * secondAlpha * (1 - p))) / alpha;
		return (Channel(first.X, second.X), Channel(first.Y, second.Y), Channel(first.Z, second.Z), alpha);
	}
}
