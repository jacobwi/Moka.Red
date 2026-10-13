namespace Moka.Red.Feedback.Tooltip;

/// <summary>
///     Position of the tooltip relative to its trigger element.
/// </summary>
public enum MokaTooltipPosition
{
	/// <summary>Above the trigger element.</summary>
	Top,

	/// <summary>Below the trigger element.</summary>
	Bottom,

	/// <summary>To the left of the trigger element.</summary>
	Left,

	/// <summary>To the right of the trigger element.</summary>
	Right,

	/// <summary>Above the trigger, its left edge lined up with the trigger's.</summary>
	TopStart,

	/// <summary>Above the trigger, its right edge lined up with the trigger's.</summary>
	TopEnd,

	/// <summary>Below the trigger, its left edge lined up with the trigger's.</summary>
	BottomStart,

	/// <summary>Below the trigger, its right edge lined up with the trigger's.</summary>
	BottomEnd
}
