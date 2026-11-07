using Moka.Red.Core.Enums;

namespace Moka.Red.Feedback.Progress;

/// <summary>One colored part of a linear <see cref="MokaProgress" /> bar.</summary>
/// <param name="Value">How much of the bar it fills, from 0 to 100. Anything outside that is brought back into it.</param>
/// <param name="Color">The color it's drawn in.</param>
public sealed record MokaProgressSegment(double Value, MokaColor Color);
