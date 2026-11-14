namespace Moka.Red.Core.Colors;

/// <summary>
///     A color in OKLCH, OKLab as lightness, chroma and hue: the CSS <c>oklch()</c> space. Lightness steps read as equal
///     steps at any hue, which is what theme derivation works in.
/// </summary>
/// <param name="L">Perceived lightness, 0 (black) to 1 (white).</param>
/// <param name="C">Chroma: 0 for a grey, about 0.37 for the most vivid sRGB colors.</param>
/// <param name="H">Hue in degrees, 0 to 360.</param>
/// <param name="Alpha">Alpha, 0 to 1.</param>
public readonly record struct MokaOklch(double L, double C, double H, double Alpha = 1)
{
	/// <summary>
	///     The difference CSS Color 4's gamut mapping accepts between a color and its clipped version, in deltaEOK: about
	///     the smallest difference most people notice.
	/// </summary>
	public const double JustNoticeable = 0.02;

	private const double ChromaPrecision = 0.0001;

	private const int ChromaSteps = 30;

	/// <summary>The color in OKLab.</summary>
	/// <returns>The OKLab color.</returns>
	public MokaOklab ToOklab()
	{
		double radians = H * Math.PI / 180;
		return new MokaOklab(L, C * Math.Cos(radians), C * Math.Sin(radians), Alpha);
	}

	/// <summary>The color in sRGB, not mapped into the gamut: a channel can fall outside 0 to 1.</summary>
	/// <returns>The sRGB color.</returns>
	public MokaSrgb ToSrgb() => ToOklab().ToSrgb();

	/// <summary>
	///     The color in sRGB, mapped into the gamut the way CSS Color 4 does it: lightness and hue stay, and chroma drops
	///     until clipping what is left changes the color by less than <see cref="JustNoticeable" />. What a browser shows
	///     for an <c>oklch()</c> color outside sRGB.
	/// </summary>
	/// <returns>A color inside sRGB.</returns>
	public MokaSrgb ToSrgbInGamut()
	{
		if (L >= 1)
		{
			return MokaSrgb.White with { Alpha = Alpha };
		}

		if (L <= 0)
		{
			return MokaSrgb.Black with { Alpha = Alpha };
		}

		MokaSrgb origin = ToSrgb();
		if (origin.IsInGamut)
		{
			return origin.Clip();
		}

		MokaSrgb clipped = origin.Clip();
		if (Difference(clipped, this) < JustNoticeable)
		{
			return clipped;
		}

		double low = 0, high = C;
		bool lowInGamut = true;
		while (high - low > ChromaPrecision)
		{
			double chroma = (low + high) / 2;
			MokaOklch current = this with { C = chroma };
			MokaSrgb candidate = current.ToSrgb();
			if (lowInGamut && candidate.IsInGamut)
			{
				low = chroma;
				continue;
			}

			clipped = candidate.Clip();
			double difference = Difference(clipped, current);
			if (difference < JustNoticeable)
			{
				if (JustNoticeable - difference < ChromaPrecision)
				{
					return clipped;
				}

				lowInGamut = false;
				low = chroma;
			}
			else
			{
				high = chroma;
			}
		}

		return clipped;
	}

	/// <summary>
	///     The color with as much of its chroma as sRGB can show at its lightness and hue, found by halving the difference
	///     thirty times. Unlike <see cref="ToSrgbInGamut" /> it never clips, so the hue stays exact; theme ramps are made
	///     this way.
	/// </summary>
	/// <returns>This color, or the same lightness and hue with less chroma.</returns>
	public MokaOklch ReduceChromaToFit()
	{
		if (ToOklab().ToLinear().IsInGamut)
		{
			return this;
		}

		double low = 0, high = C;
		for (int i = 0; i < ChromaSteps; i++)
		{
			double middle = (low + high) / 2;
			if ((this with { C = middle }).ToOklab().ToLinear().IsInGamut)
			{
				low = middle;
			}
			else
			{
				high = middle;
			}
		}

		return this with { C = low };
	}

	private static double Difference(MokaSrgb clipped, MokaOklch color) => clipped.ToOklab().DistanceTo(color.ToOklab());
}
