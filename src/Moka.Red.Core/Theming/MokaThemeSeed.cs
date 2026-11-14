namespace Moka.Red.Core.Theming;

/// <summary>How much contrast a theme made from a <see cref="MokaThemeSeed" /> holds its text and lines to.</summary>
public enum MokaSeedContrast
{
	/// <summary>WCAG AA: text 4.5:1 on what it sits on, icons and the lines of controls 3:1.</summary>
	Standard,

	/// <summary>WCAG AAA text: 7:1 for text, 4.5:1 for icons, and 3:1 for every control line, not only the strong ones.</summary>
	More
}

/// <summary>
///     The few colors a whole theme is derived from by <see cref="MokaTheme.FromSeed" />: an accent, a neutral that tints
///     the surfaces, and optionally the four meaning colors. Every other color is worked out in OKLCH against the derived
///     surfaces, so text meets its contrast target by construction. Colors are any CSS color
///     <see cref="Moka.Red.Core.Colors.MokaCssColor" /> reads.
/// </summary>
public sealed record MokaThemeSeed
{
	/// <summary>
	///     The accent: main buttons, focus and selection. The theme's <c>--moka-color-primary</c> is this color, as given in
	///     both schemes; the accent's text and line shades are derived from it.
	/// </summary>
	public required string Accent { get; init; }

	/// <summary>
	///     The color whose hue and chroma tint the surfaces, borders and greys, such as a blue-grey or a warm stone. Its
	///     lightness doesn't matter. Null gives pure greys.
	/// </summary>
	public string? Neutral { get; init; }

	/// <summary>
	///     How strongly the surfaces take the neutral's hue, as an OKLCH chroma from 0 (grey) to 0.1. Null keeps the
	///     neutral's own chroma, up to 0.1. Borders and secondary text take a little more of it than surfaces do.
	/// </summary>
	public double? NeutralChroma { get; init; }

	/// <summary>
	///     The success color's hue and strength. Null takes the new design's green. It is kept as given in a scheme where it
	///     reads, and moved in lightness where it doesn't.
	/// </summary>
	public string? Success { get; init; }

	/// <summary>The warning color's hue and strength. Null takes the new design's amber. Kept or moved as <see cref="Success" /> is.</summary>
	public string? Warning { get; init; }

	/// <summary>The error color's hue and strength. Null takes the new design's red. Kept or moved as <see cref="Success" /> is.</summary>
	public string? Error { get; init; }

	/// <summary>The info color's hue and strength. Null takes the new design's blue. Kept or moved as <see cref="Success" /> is.</summary>
	public string? Info { get; init; }

	/// <summary>How much contrast text and lines are held to.</summary>
	public MokaSeedContrast Contrast { get; init; } = MokaSeedContrast.Standard;
}
