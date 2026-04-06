namespace Moka.Red.Primitives.OrgChart;

/// <summary>
///     Represents a node in an organization chart tree.
/// </summary>
/// <typeparam name="TItem">The type of data each node holds.</typeparam>
/// <param name="Data">The data payload for this node.</param>
/// <param name="Children">Optional child nodes. Null or empty means this is a leaf node.</param>
/// <param name="IsCollapsed">Whether this node's children are collapsed (hidden).</param>
public record MokaOrgNode<TItem>(
	TItem Data,
	IReadOnlyList<MokaOrgNode<TItem>>? Children = null,
	bool IsCollapsed = false);
