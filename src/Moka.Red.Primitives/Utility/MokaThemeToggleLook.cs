namespace Moka.Red.Primitives.Utility;

/// <summary>How a <see cref="MokaThemeToggle" /> is drawn. Each one works with two states and with three.</summary>
public enum MokaThemeToggleLook
{
	/// <summary>A round icon button. A click moves on to the next state.</summary>
	Icon,

	/// <summary>
	///     A sliding switch with a sun and a moon. With two states it's a switch to assistive technology, with three a radio
	///     group of three stops.
	/// </summary>
	Switch,

	/// <summary>A segmented control of icons, with each state's word as its name and tooltip.</summary>
	Segmented,

	/// <summary>A segmented control with an icon and a word on each segment.</summary>
	IconsAndWords,

	/// <summary>A small pill showing the current state's icon and word. A click moves on to the next state.</summary>
	Chip
}
