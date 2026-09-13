namespace Moka.Red.Feedback.Cheatsheet;

/// <summary>
///     A single shortcut row inside a <see cref="MokaCheatsheetGroup" />:
///     an action description paired with the keys that trigger it.
/// </summary>
/// <param name="Description">Human-readable action description shown on the left of the row.</param>
/// <param name="Keys">Individual key captions rendered as key chips (e.g., "Ctrl", "K").</param>
public sealed record MokaCheatsheetItem(string Description, IReadOnlyList<string> Keys);

/// <summary>
///     A titled group of shortcut rows rendered as one section of the <see cref="MokaCheatsheet" /> grid.
/// </summary>
/// <param name="Title">Group heading rendered as an uppercase micro-label.</param>
/// <param name="Items">The shortcut rows belonging to this group.</param>
public sealed record MokaCheatsheetGroup(string Title, IReadOnlyList<MokaCheatsheetItem> Items);
