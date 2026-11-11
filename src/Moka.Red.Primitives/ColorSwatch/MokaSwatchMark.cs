namespace Moka.Red.Primitives.ColorSwatch;

/// <summary>
///     The mark on the chosen color of a <see cref="MokaColorSwatch" />. <see cref="MokaSwatchLook.Chips" /> and
///     <see cref="MokaSwatchLook.Menu" /> draw their own and ignore it.
/// </summary>
public enum MokaSwatchMark
{
	/// <summary>
	///     The look's own mark. On <see cref="MokaSwatchLook.Standard" /> a border in the text color, a little growth and
	///     a check. The default.
	/// </summary>
	Standard,

	/// <summary>Just the check, black or white by the color, and a slight grow. The quietest mark.</summary>
	Check,

	/// <summary>A ring in the text color with a gap, and the color left whole.</summary>
	Ring,

	/// <summary>
	///     The ring with a gap and the check together. Two signals, so it holds for people who can't tell the colors
	///     apart and on swatches near the page's own color.
	/// </summary>
	RingAndCheck,

	/// <summary>The ring takes the chosen color. Lively, but a pale ring is faint on some pages.</summary>
	ColorRing,

	/// <summary>A dot in the middle, black or white by the color, like a radio button's own dot.</summary>
	Dot,

	/// <summary>A small accent badge with a check at the bottom right, outside the color.</summary>
	Badge
}
