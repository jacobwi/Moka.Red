using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     The color a chart draws a series in. An explicit <see cref="MokaColor" /> wins; otherwise the
///     series take the palette in rotation, so side-by-side series are always distinguishable. The
///     class sets the <c>--moka-chart-color</c> custom property each chart's CSS paints from.
/// </summary>
internal static class MokaChartPalette
{
	private static readonly MokaColor[] Rotation =
		[MokaColor.Primary, MokaColor.Secondary, MokaColor.Success, MokaColor.Info, MokaColor.Warning, MokaColor.Error, MokaColor.Surface];

	public static string ClassFor(MokaColor? color, int index) =>
		$"moka-chart-color-{MokaEnumHelpers.ToCssClass(color ?? Rotation[Math.Abs(index) % Rotation.Length])}";
}
