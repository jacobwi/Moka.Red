using Moka.Red.Core.Enums;
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

	/// <summary>
	///     A label color for the item, such as each choice under "Color". An item without an <see cref="Icon" /> shows it
	///     as a swatch in the icon's place, and one with an icon draws the icon in it. It keeps its color on a hovered or
	///     lit row, and a checked item shows its check beside it. The shades follow the theme, light or dark, and
	///     <c>MokaTheme.WithLabelTone</c>. The swatch is hidden from screen readers, so <see cref="Text" /> should name the
	///     color.
	/// </summary>
	public MokaLabelColor? Label { get; init; }

	/// <summary>
	///     Any CSS color for the swatch or icon, in place of <see cref="Label" />: a hex value, a color keyword or a color
	///     function such as <c>var()</c>. It wins over <see cref="Label" />, and a value that isn't a CSS color is ignored.
	/// </summary>
	public string? ColorValue { get; init; }

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

	/// <summary>
	///     Click handler. Not called if Disabled or has Children (sub-menu parent). It runs from the menu, so only the menu
	///     redraws afterwards, unless the menu was opened with an owner (for example
	///     <see cref="IMokaContextMenuService.Show(Microsoft.AspNetCore.Components.Web.MouseEventArgs, IReadOnlyList{MokaContextMenuItem}, Microsoft.AspNetCore.Components.IHandleEvent?)" />
	///     or <see cref="MokaContextMenuTrigger.Owner" />), which then redraws as an EventCallback's receiver does.
	/// </summary>
	public Func<Task>? OnClick { get; init; }

	/// <summary>Synchronous click handler alternative. Runs through the menu's owner the same way.</summary>
	public Action? OnClickSync { get; init; }

	/// <summary>Whether this item has a sub-menu.</summary>
	public bool HasChildren => Children is not null && Children.Count > 0;
}
