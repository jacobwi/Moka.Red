namespace Moka.Red.Primitives.Meter;

/// <summary>
///     Defines a colored segment within a <see cref="MokaMeter" />.
///     Segments divide the meter into visual zones (e.g., green/yellow/red).
/// </summary>
/// <param name="FromPercent">Start position as a percentage (0-100).</param>
/// <param name="ToPercent">End position as a percentage (0-100).</param>
/// <param name="Color">CSS color value for this segment.</param>
public record MokaMeterSegment(double FromPercent, double ToPercent, string Color);
