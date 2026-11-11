namespace Moka.Red.Primitives.ColorSwatch;

/// <summary>
///     A set of colors under a heading in a <see cref="MokaColorSwatch" />, such as "Theme colors", "Standard" or
///     "Recent". Every group's swatches are one radio group, so Tab still reaches one swatch and the arrow keys run on
///     from one group into the next.
/// </summary>
/// <param name="Title">The heading over the colors. An empty title draws the colors with no heading.</param>
/// <param name="Colors">The colors, in the forms <see cref="MokaColorSwatch.Colors" /> takes.</param>
public sealed record MokaSwatchGroup(string Title, IReadOnlyList<string> Colors);
