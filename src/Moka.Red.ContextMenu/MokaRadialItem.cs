using Moka.Red.Core.Icons;

namespace Moka.Red.ContextMenu;

/// <summary>One round button in a <see cref="MokaRadialMenu" />.</summary>
/// <param name="Id">What picking the button answers, in <see cref="MokaRadialPick.Answer" />.</param>
/// <param name="Label">
///     The button's name. Screen readers read it, and the hub shows it while the button is highlighted.
/// </param>
/// <param name="Icon">The icon on the button.</param>
/// <param name="Tone">What the action does, which sets the button's color.</param>
/// <param name="Disabled">Whether the button can't be picked right now. It still shows, faded, with its name in the hub.</param>
/// <param name="Choices">
///     Choices that open in a list beside the button instead of picking it straight away. Picking one answers
///     <c>{Id}:{choice id}</c>.
/// </param>
public sealed record MokaRadialItem(
	string Id,
	string Label,
	MokaIconDefinition Icon,
	MokaRadialTone Tone = MokaRadialTone.Default,
	bool Disabled = false,
	IReadOnlyList<MokaRadialChoice>? Choices = null)
{
	/// <summary>Whether the button opens a list of choices.</summary>
	public bool HasChoices => Choices is { Count: > 0 };
}
