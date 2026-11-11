namespace Moka.Red.ContextMenu;

/// <summary>How a <see cref="MokaRadialMenu" /> shows the <see cref="MokaRadialItem.Choices" /> of a button.</summary>
public enum MokaRadialChoiceStyle
{
	/// <summary>A list beside the button, on the side away from the hub.</summary>
	List,

	/// <summary>
	///     The choices on a short arc outside the button, so a flick carries on outward: press, pass through the
	///     button, keep going to a choice, let go.
	/// </summary>
	Ring,

	/// <summary>
	///     Picking the button swaps the ring for its choices and the hub becomes Back. A click on the hub or Escape goes
	///     back. Big targets and room for many choices.
	/// </summary>
	Replace,

	/// <summary>
	///     A smaller list growing straight out from the button along its direction, tied to it by a short accent line.
	///     It never sits over a neighbour.
	/// </summary>
	Flyout
}
