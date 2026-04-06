namespace Moka.Red.Feedback.Popover;

/// <summary>
///     Determines how a <see cref="MokaPopover" /> is triggered to open.
/// </summary>
public enum MokaPopoverTrigger
{
	/// <summary>Opens on click of the trigger element.</summary>
	Click,

	/// <summary>Opens on hover/focus of the trigger element.</summary>
	Hover,

	/// <summary>Controlled entirely via the <see cref="MokaPopover.Open" /> parameter.</summary>
	Manual
}
