namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     Where the dialog appears on the screen.
/// </summary>
public enum MokaDialogPosition
{
	/// <summary>Centered modal dialog, the classic look. Default.</summary>
	Center,

	/// <summary>Full-height panel anchored to the left edge, sliding in from it.</summary>
	Left,

	/// <summary>Full-height panel anchored to the right edge, sliding in from it.</summary>
	Right,

	/// <summary>Full-width panel anchored to the top edge, sliding down from it.</summary>
	Top,

	/// <summary>Full-width panel anchored to the bottom edge, sliding up from it.</summary>
	Bottom
}
