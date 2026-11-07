namespace Moka.Red.Forms.RangeBar;

/// <summary>A range on a <see cref="MokaRangeBar" />'s scale: the value of the start handle and of the end handle.</summary>
/// <param name="Start">Where the range starts. Never above <paramref name="End" /> when the bar reports it.</param>
/// <param name="End">Where the range ends.</param>
public readonly record struct MokaRange(double Start, double End);
