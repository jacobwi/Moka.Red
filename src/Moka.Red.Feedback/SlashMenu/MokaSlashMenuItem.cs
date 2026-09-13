using Moka.Red.Core.Icons;

namespace Moka.Red.Feedback.SlashMenu;

/// <summary>
///     A single entry in a <see cref="MokaSlashMenu" />.
///     The filter query matches case-insensitively against <see cref="Title" />,
///     <see cref="Keywords" />, and <see cref="Category" />.
/// </summary>
public sealed record MokaSlashMenuItem
{
	/// <summary>Display title shown on the row. Matched against the filter query.</summary>
	public required string Title { get; init; }

	/// <summary>Optional one-line description shown under the title (ellipsized).</summary>
	public string? Description { get; init; }

	/// <summary>Optional category tag shown right-aligned in mono uppercase. Matched against the filter query.</summary>
	public string? Category { get; init; }

	/// <summary>Optional icon rendered before the title.</summary>
	public MokaIconDefinition? Icon { get; init; }

	/// <summary>Optional extra search terms matched against the filter query but never displayed.</summary>
	public string? Keywords { get; init; }

	/// <summary>Optional consumer payload (e.g. the snippet to insert) carried through to the selection callback.</summary>
	public object? Value { get; init; }
}
