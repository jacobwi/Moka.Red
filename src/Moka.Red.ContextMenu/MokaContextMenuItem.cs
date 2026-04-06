using Moka.Red.Core.Icons;

namespace Moka.Red.ContextMenu;

/// <summary>
///     Defines a single item in a context menu.
/// </summary>
public sealed class MokaContextMenuItem
{
	/// <summary>Display text.</summary>
	public required string Text { get; init; }

	/// <summary>Icon shown before text.</summary>
	public MokaIconDefinition? Icon { get; init; }

	/// <summary>Whether this item is disabled.</summary>
	public bool Disabled { get; init; }

	/// <summary>Whether to show a divider line before this item.</summary>
	public bool DividerBefore { get; init; }

	/// <summary>Whether this item is checked (shows checkmark).</summary>
	public bool Checked { get; init; }

	/// <summary>Keyboard shortcut hint displayed on the right (e.g., "Ctrl+C").</summary>
	public string? Shortcut { get; init; }

	/// <summary>Nested sub-menu items. When set, this item shows a sub-menu on hover.</summary>
	public IReadOnlyList<MokaContextMenuItem>? Children { get; init; }

	/// <summary>Custom CSS class for this item.</summary>
	public string? CssClass { get; init; }

	/// <summary>Click handler. Not called if Disabled or has Children (sub-menu parent).</summary>
	public Func<Task>? OnClick { get; init; }

	/// <summary>Synchronous click handler alternative.</summary>
	public Action? OnClickSync { get; init; }

	/// <summary>Whether this item has a sub-menu.</summary>
	public bool HasChildren => Children is not null && Children.Count > 0;
}
