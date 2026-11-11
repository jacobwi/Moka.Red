namespace Moka.Red.ContextMenu;

/// <summary>How the buttons of a <see cref="MokaRadialMenu" /> sit around what it acts on.</summary>
/// <remarks>In every shape the highlight follows the pointer's angle, so a flick works the same way.</remarks>
public enum MokaRadialShape
{
	/// <summary>Round buttons on a band around a hub with the name and the pointed button's word.</summary>
	Ring,

	/// <summary>
	///     Wedges that fill the ring out to its edge, each with its icon and word. The pointed wedge fills with the accent
	///     and swings round to follow the pointer.
	/// </summary>
	Pie,

	/// <summary>
	///     The upper half of the ring, opening upward from a half hub sitting on the row, for something at the bottom of
	///     the window or under a thumb.
	/// </summary>
	HalfRing,

	/// <summary>
	///     A quarter circle opening from a corner, with slightly smaller buttons. It opens toward the middle of the window
	///     from where it was opened.
	/// </summary>
	CornerFan,

	/// <summary>
	///     Small buttons close around the pointer and no hub, so it hides the least of the page. Only the pointed button
	///     shows its word, in a tag outside it.
	/// </summary>
	Compact,

	/// <summary>The ring with each button's word printed outside it. The hub keeps the name and what it is.</summary>
	Labeled,

	/// <summary>
	///     Words at the ends of lines from the centre, made for flicking. The line to the pointed word lights up, so it
	///     shows where a flick is headed.
	/// </summary>
	Marking
}
