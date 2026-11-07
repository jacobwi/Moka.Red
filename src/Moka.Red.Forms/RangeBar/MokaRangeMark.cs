using Moka.Red.Core.Enums;

namespace Moka.Red.Forms.RangeBar;

/// <summary>One point under a <see cref="MokaRangeBar" />'s track: a tick or a pin.</summary>
/// <param name="Value">Where the mark sits on the bar's scale.</param>
/// <param name="Label">
///     What the mark is, such as the device holding an address. Shown in a tooltip on hover, with the value, and read
///     to screen readers.
/// </param>
/// <param name="Color">
///     The mark's tone. Null draws a tick in the muted text color and a pin in the accent.
/// </param>
public sealed record MokaRangeMark(double Value, string? Label = null, MokaColor? Color = null);
