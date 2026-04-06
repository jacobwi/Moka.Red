using Moka.Red.Feedback.Popover;

namespace Moka.Red.Feedback.Onboarding;

/// <summary>
///     Defines a single step in an <see cref="MokaOnboarding" /> tour.
/// </summary>
public sealed record MokaOnboardingStep
{
	/// <summary>The step heading displayed in the tooltip card.</summary>
	public required string Title { get; init; }

	/// <summary>The step body text displayed below the title.</summary>
	public required string Description { get; init; }

	/// <summary>CSS selector of the DOM element to highlight (e.g., "#my-button", ".nav-link").</summary>
	public required string TargetSelector { get; init; }

	/// <summary>Tooltip position relative to the target element. Defaults to <see cref="MokaPopoverPosition.Bottom" />.</summary>
	public MokaPopoverPosition Position { get; init; } = MokaPopoverPosition.Bottom;
}
