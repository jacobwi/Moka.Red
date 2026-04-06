namespace Moka.Red.Navigation.Tabs.Models;

/// <summary>
///     Represents a named group of tabs within the tab strip.
/// </summary>
public sealed class TabGroupInfo
{
	/// <summary>
	///     Gets the unique name of this group.
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	///     Gets or sets the display title for this group.
	/// </summary>
	public string? DisplayTitle { get; set; }

	/// <summary>
	///     Gets or sets whether this group is collapsed in the tab strip.
	/// </summary>
	public bool IsCollapsed { get; set; }

	/// <summary>
	///     Gets or sets the sort order of the group within the strip.
	/// </summary>
	public int Order { get; set; }

	/// <summary>
	///     Gets or sets an optional CSS class applied to the group container.
	/// </summary>
	public string? CssClass { get; set; }

	/// <summary>
	///     Gets or sets the border color for this group. When <c>null</c>, a deterministic color
	///     is computed from the group name.
	/// </summary>
	public string? Color { get; set; }

	/// <summary>
	///     Gets or sets which edge of the group container displays the color border.
	/// </summary>
	public BorderPosition BorderPosition { get; set; } = BorderPosition.Left;
}
