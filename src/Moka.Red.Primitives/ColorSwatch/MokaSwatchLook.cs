namespace Moka.Red.Primitives.ColorSwatch;

/// <summary>How a <see cref="MokaColorSwatch" /> draws its swatches.</summary>
public enum MokaSwatchLook
{
	/// <summary>
	///     24 px circles 4 px apart. The chosen one grows a little and gets a border in the text color and a check.
	///     The default.
	/// </summary>
	Standard,

	/// <summary>
	///     26 px circles 8 px apart. The chosen one gets a ring in the text color with a gap of page between, and keeps
	///     its check. Nothing grows, so the row never shifts.
	/// </summary>
	Ring,

	/// <summary>
	///     30 px tiles with rounded corners, 6 px apart. A hover raises a thin outline, and the chosen tile gets an inner
	///     gap and an outline. Bigger targets, for a settings page or a touch screen.
	/// </summary>
	Tiles,

	/// <summary>
	///     Each color is a pill with its dot and its name, and the chosen pill gets a border in the text color. Wider,
	///     but nobody has to guess between two blues. It ignores <see cref="MokaColorSwatch.Mark" /> and
	///     <see cref="MokaColorSwatch.Columns" />: the chips wrap where the container ends.
	/// </summary>
	Chips,

	/// <summary>
	///     The colors joined into one bar. The chosen one lifts out of the bar with a short mark under it. The smallest
	///     footprint, for a toolbar or a table cell. It ignores <see cref="MokaColorSwatch.Columns" />, so the bar only
	///     wraps where the container ends.
	/// </summary>
	Strip,

	/// <summary>
	///     One color per row with a small dot, its name and a check at the end, inside a menu panel. For a "Color" item
	///     in a context menu. It ignores <see cref="MokaColorSwatch.Mark" />.
	/// </summary>
	Menu,

	/// <summary>
	///     22 px squares with small corners. The chosen one gets an accent outline with a glow, and the line under the
	///     swatches reads like a prompt in mono capitals. For the Matrix colors.
	/// </summary>
	Terminal
}
