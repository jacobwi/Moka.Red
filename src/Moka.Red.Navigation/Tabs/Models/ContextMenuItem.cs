namespace Moka.Red.Navigation.Tabs.Models;

/// <summary>
///     Represents a single item in a tab context menu.
/// </summary>
public sealed class ContextMenuItem
{
	/// <summary>
	///     Gets or sets the display text for this menu item.
	/// </summary>
	public required string Text { get; init; }

	/// <summary>
	///     Gets or sets an optional icon CSS class.
	/// </summary>
	public string? IconClass { get; set; }

	/// <summary>
	///     Gets or sets whether this item is disabled.
	/// </summary>
	public bool Disabled { get; set; }

	/// <summary>
	///     Gets or sets whether to show a separator line before this item.
	/// </summary>
	public bool DividerBefore { get; set; }

	/// <summary>
	///     Gets or sets the action to execute when this item is clicked.
	/// </summary>
	public required Func<Task> OnClick { get; init; }
}
