namespace Moka.Red.Forms.Cascader;

/// <summary>One node of a <see cref="MokaCascader" /> option tree.</summary>
public sealed class MokaCascaderOption
{
	/// <summary>The node's value; the selected path is these, root to leaf.</summary>
	public required string Value { get; init; }

	/// <summary>The node's display text. Null = <see cref="Value" />.</summary>
	public string? Label { get; init; }

	/// <summary>Whether the node can be chosen.</summary>
	public bool Disabled { get; init; }

	/// <summary>The node's children. A node with none is a leaf, and choosing it completes a path.</summary>
	public IReadOnlyList<MokaCascaderOption>? Children { get; init; }

	/// <summary>The text shown for the node.</summary>
	public string Text => Label ?? Value;
}
