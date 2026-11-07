using Moka.Red.Core.Enums;

namespace Moka.Red.Forms.RangeBar;

/// <summary>A colored stretch of a <see cref="MokaRangeBar" />'s track, such as the addresses kept for devices.</summary>
/// <param name="Start">Where the part starts on the bar's scale.</param>
/// <param name="End">Where the part ends on the bar's scale. A part with no width still shows as a thin line.</param>
/// <param name="Label">
///     What the part is. Drawn inside it while the bar shows labels, and in the key under the bar when it doesn't.
///     Screen readers hear it with the part's start and end either way.
/// </param>
/// <param name="Color">The part's tone. Defaults to <see cref="MokaColor.Secondary" />, a neutral grey.</param>
public sealed record MokaRangePart(double Start, double End, string? Label = null, MokaColor Color = MokaColor.Secondary);
