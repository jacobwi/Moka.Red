namespace Moka.Red.Core.Theming;

// How strongly each translucent accent token shows, as the accent's alpha: the glow fills behind selected and hovered
// things, the faintest wash, and the borders. One set per design and scheme, so a preset and WithAccent agree.
internal sealed record MokaAccentTiers(double Glow, double GlowMd, double GlowStrong, double GlowFaint, double Border, double BorderDim)
{
	internal static MokaAccentTiers Matrix { get; } = new(0.08, 0.15, 0.25, 0.03, 0.20, 0.08);

	internal static MokaAccentTiers GateDark { get; } = new(0.08, 0.15, 0.25, 0.04, 0.30, 0.12);

	internal static MokaAccentTiers GateLight { get; } = new(0.10, 0.16, 0.26, 0.05, 0.36, 0.16);

	internal MokaPalette ApplyTo(MokaPalette palette, string accent) => palette with
	{
		PrimaryGlow = MokaColorMath.Rgba(accent, Glow),
		PrimaryGlowMd = MokaColorMath.Rgba(accent, GlowMd),
		PrimaryGlowStrong = MokaColorMath.Rgba(accent, GlowStrong),
		PrimaryGlowFaint = MokaColorMath.Rgba(accent, GlowFaint),
		PrimaryBorder = MokaColorMath.Rgba(accent, Border),
		PrimaryBorderDim = MokaColorMath.Rgba(accent, BorderDim)
	};
}
