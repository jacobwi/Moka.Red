using Moka.Red.Core.Theming;

namespace Moka.Red.ThemeGen.Appearance;

/// <summary>
///     One option in the chart texture and print row of <see cref="MokaAppearancePanel" />, which sets
///     <see cref="MokaAppearance.ChartTexture" /> and <see cref="MokaAppearance.ChartPrint" /> together. It is the
///     <see cref="MokaAppearanceOption.Value" /> that row asks <see cref="MokaAppearancePanel.OptionState" /> about, so
///     <c>{ Value: MokaChartTextureChoice { Print: MokaChartPrint.Ink } }</c> matches the ink option.
/// </summary>
/// <param name="Texture">Whether marks are hatched on screen.</param>
/// <param name="Print">How charts print and show under forced colors.</param>
public readonly record struct MokaChartTextureChoice(MokaChartTexture Texture, MokaChartPrint Print)
{
	/// <summary>Colors alone, on screen and in print, as charts have always been.</summary>
	public static MokaChartTextureChoice ColorOnly { get; } = new(MokaChartTexture.None, MokaChartPrint.AsShown);

	/// <summary>Colors on screen; in print and forced colors each series is also hatched.</summary>
	public static MokaChartTextureChoice HatchedInPrint { get; } = new(MokaChartTexture.None, MokaChartPrint.Hatched);

	/// <summary>Colors on screen; in print and forced colors no color at all, ink hatching on the page.</summary>
	public static MokaChartTextureChoice InkInPrint { get; } = new(MokaChartTexture.None, MokaChartPrint.Ink);

	/// <summary>Every series hatched, on screen and in print, for readers who can't tell the colors apart.</summary>
	public static MokaChartTextureChoice AlwaysHatched { get; } = new(MokaChartTexture.Hatched, MokaChartPrint.Hatched);

	/// <summary>The options the row offers, in its order.</summary>
	public static IReadOnlyList<MokaChartTextureChoice> All { get; } = [ColorOnly, HatchedInPrint, InkInPrint, AlwaysHatched];
}
