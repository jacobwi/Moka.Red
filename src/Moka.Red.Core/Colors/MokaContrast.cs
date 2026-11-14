namespace Moka.Red.Core.Colors;

/// <summary>
///     How well text or a line stands out from what is behind it: the WCAG 2 contrast ratio, and APCA's lightness contrast
///     (Lc), the measure proposed for WCAG 3.
/// </summary>
public static class MokaContrast
{
	/// <summary>The WCAG 2 ratio body text needs (level AA).</summary>
	public const double TextRatio = 4.5;

	/// <summary>The WCAG 2 ratio large text, icons and the lines of controls need (level AA).</summary>
	public const double LargeTextRatio = 3;

	/// <summary>The WCAG 2 ratio body text needs at level AAA.</summary>
	public const double EnhancedTextRatio = 7;

	private const double ApcaBlackThreshold = 0.022;
	private const double ApcaBlackClamp = 1.414;
	private const double ApcaMinimumDelta = 0.0005;
	private const double ApcaScale = 1.14;
	private const double ApcaOffset = 0.027;
	private const double ApcaClip = 0.1;

	/// <summary>
	///     The WCAG 2 contrast ratio, from 1 to 21. A translucent <paramref name="foreground" /> is drawn over the
	///     background first; a translucent background is drawn over black, as on an empty page in a dark window.
	/// </summary>
	/// <param name="foreground">The text or line.</param>
	/// <param name="background">What it sits on.</param>
	/// <returns>The ratio.</returns>
	public static double Ratio(MokaSrgb foreground, MokaSrgb background)
	{
		MokaSrgb solidBackground = background.Over(MokaSrgb.Black);
		return Ratio(foreground.Over(solidBackground).RelativeLuminance, solidBackground.RelativeLuminance);
	}

	/// <summary>The WCAG 2 contrast ratio between two relative luminances, from 1 to 21.</summary>
	/// <param name="first">One luminance, 0 to 1.</param>
	/// <param name="second">The other luminance, 0 to 1.</param>
	/// <returns>The ratio.</returns>
	public static double Ratio(double first, double second) =>
		(Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);

	/// <summary>
	///     APCA's lightness contrast (APCA-W3 0.0.98G-4g), about -108 to 106: positive for dark text on a light
	///     background, negative for light text on a dark one. Read its size: 75 suits body text, 60 large or bold text, 45
	///     large headings and 30 lines and icons. Translucent colors are composited as in <see cref="Ratio(MokaSrgb, MokaSrgb)" />.
	/// </summary>
	/// <param name="text">The text.</param>
	/// <param name="background">What it sits on.</param>
	/// <returns>Lc, signed.</returns>
	public static double Apca(MokaSrgb text, MokaSrgb background)
	{
		MokaSrgb solidBackground = background.Over(MokaSrgb.Black);
		double textY = ApcaLuminance(text.Over(solidBackground).Clip());
		double backgroundY = ApcaLuminance(solidBackground.Clip());

		if (Math.Abs(backgroundY - textY) < ApcaMinimumDelta)
		{
			return 0;
		}

		if (backgroundY > textY)
		{
			double darkOnLight = (Math.Pow(backgroundY, 0.56) - Math.Pow(textY, 0.57)) * ApcaScale;
			return darkOnLight < ApcaClip ? 0 : (darkOnLight - ApcaOffset) * 100;
		}

		double lightOnDark = (Math.Pow(backgroundY, 0.65) - Math.Pow(textY, 0.62)) * ApcaScale;
		return lightOnDark > -ApcaClip ? 0 : (lightOnDark + ApcaOffset) * 100;
	}

	// APCA estimates screen luminance with a plain 2.4 power and lifts near-black values, unlike WCAG's piecewise curve.
	private static double ApcaLuminance(MokaSrgb color)
	{
		double y = (0.2126729 * Math.Pow(color.R, 2.4)) + (0.7151522 * Math.Pow(color.G, 2.4)) + (0.0721750 * Math.Pow(color.B, 2.4));
		return y < ApcaBlackThreshold ? y + Math.Pow(ApcaBlackThreshold - y, ApcaBlackClamp) : y;
	}
}
