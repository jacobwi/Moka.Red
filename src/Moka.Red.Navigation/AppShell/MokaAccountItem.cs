using Moka.Red.Core.Icons;

namespace Moka.Red.Navigation.AppShell;

/// <summary>One row in the menu a <see cref="MokaAccountCard" /> opens.</summary>
/// <param name="Text">The row's name.</param>
/// <param name="Icon">An icon before the name.</param>
/// <param name="Href">An address, so the row is a link. Without one it only raises <c>OnItemClick</c>.</param>
/// <param name="Danger">Turns red when hovered or focused, for something like powering off.</param>
/// <param name="Active">The row names the page showing now.</param>
/// <param name="Disabled">The row can't be picked, and the arrow keys pass over it.</param>
/// <param name="Checked">A setting the row turns on or off, which leaves the menu open. Null is an ordinary row.</param>
public sealed record MokaAccountItem(
	string Text,
	MokaIconDefinition? Icon = null,
	string? Href = null,
	bool Danger = false,
	bool Active = false,
	bool Disabled = false,
	bool? Checked = null)
{
	/// <summary>A line between groups of rows.</summary>
	public static MokaAccountItem Divider { get; } = new(string.Empty);

	/// <summary>Whether this is <see cref="Divider" />.</summary>
	public bool IsDivider => ReferenceEquals(this, Divider);
}
