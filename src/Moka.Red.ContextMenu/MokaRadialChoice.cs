using Moka.Red.Core.Icons;

namespace Moka.Red.ContextMenu;

/// <summary>One choice in the list a <see cref="MokaRadialItem" /> opens beside its button.</summary>
/// <param name="Id">What the choice answers after its button's id: picking it answers <c>{item}:{choice}</c>.</param>
/// <param name="Label">The text in the list.</param>
/// <param name="Checked">Whether this is the current choice. It shows a check mark, and the keyboard starts on it.</param>
public sealed record MokaRadialChoice(string Id, string Label, bool Checked = false)
{
	/// <summary>
	///     An icon before the label. With <see cref="MokaRadialChoiceStyle.Replace" /> it is the face of the choice's
	///     round button, which otherwise shows the label.
	/// </summary>
	public MokaIconDefinition? Icon { get; init; }
}
