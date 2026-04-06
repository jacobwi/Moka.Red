using Moka.Red.Core.Icons;

namespace Moka.Red.Forms.TreeSelect;

/// <summary>
///     Represents a single item in a <see cref="MokaTreeSelect{TValue}" /> hierarchy.
/// </summary>
/// <typeparam name="TValue">The type of the item value.</typeparam>
/// <param name="Value">The value associated with this item.</param>
/// <param name="Text">The display text for this item.</param>
/// <param name="Icon">Optional leading icon.</param>
/// <param name="Children">Optional child items forming a sub-tree.</param>
/// <param name="Disabled">Whether this item is disabled and cannot be selected.</param>
public record MokaTreeSelectItem<TValue>(
	TValue Value,
	string Text,
	MokaIconDefinition? Icon = null,
	IReadOnlyList<MokaTreeSelectItem<TValue>>? Children = null,
	bool Disabled = false)
{
	/// <summary>Whether this item has child items.</summary>
	public bool HasChildren => Children is { Count: > 0 };
}
