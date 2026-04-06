namespace Moka.Red.Feedback.Popover;

/// <summary>
///     Position of the popover relative to its trigger element.
/// </summary>
public enum MokaPopoverPosition
{
	/// <summary>Above the trigger, centered.</summary>
	Top,

	/// <summary>Below the trigger, centered.</summary>
	Bottom,

	/// <summary>To the left of the trigger, centered.</summary>
	Left,

	/// <summary>To the right of the trigger, centered.</summary>
	Right,

	/// <summary>Above the trigger, aligned to the start (left).</summary>
	TopStart,

	/// <summary>Above the trigger, aligned to the end (right).</summary>
	TopEnd,

	/// <summary>Below the trigger, aligned to the start (left).</summary>
	BottomStart,

	/// <summary>Below the trigger, aligned to the end (right).</summary>
	BottomEnd
}
