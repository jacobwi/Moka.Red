using Microsoft.AspNetCore.Components;

namespace Moka.Red.Navigation.Tabs.Models;

/// <summary>
///     Represents the complete state of a single tab, including its value, display properties, and metadata.
/// </summary>
/// <typeparam name="TValue">The type of value stored by this tab.</typeparam>
public sealed class TabInfo<TValue>
{
	/// <summary>
	///     Gets the unique identifier for this tab.
	/// </summary>
	public string Id { get; init; } = Guid.NewGuid().ToString("N");

	/// <summary>
	///     Gets or sets the display title of the tab.
	/// </summary>
	public string Title { get; set; } = string.Empty;

	/// <summary>
	///     Gets or sets the generic value associated with this tab.
	/// </summary>
	public TValue? Value { get; set; }

	/// <summary>
	///     Gets or sets the icon CSS class (e.g., a MudBlazor icon string).
	/// </summary>
	public string? IconClass { get; set; }

	/// <summary>
	///     Gets or sets a custom icon render fragment.
	/// </summary>
	public RenderFragment? IconContent { get; set; }

	/// <summary>
	///     Gets or sets the component type to render as tab content via DynamicComponent.
	/// </summary>
	public Type? ContentComponentType { get; set; }

	/// <summary>
	///     Gets or sets parameters to pass to the DynamicComponent content.
	/// </summary>
	public IDictionary<string, object?>? ContentParameters { get; set; }

	/// <summary>
	///     Gets or sets the badge information for this tab.
	/// </summary>
	public TabBadgeInfo? Badge { get; set; }

	/// <summary>
	///     Gets or sets the name of the group this tab belongs to.
	/// </summary>
	public string? GroupName { get; set; }

	/// <summary>
	///     Gets or sets whether this tab can be closed by the user.
	/// </summary>
	public bool IsClosable { get; set; } = true;

	/// <summary>
	///     Gets or sets whether this tab is pinned (cannot be closed or reordered past other pinned tabs).
	/// </summary>
	public bool IsPinned { get; set; }

	/// <summary>
	///     Gets or sets whether this tab can be reordered via drag and drop.
	/// </summary>
	public bool IsDraggable { get; set; } = true;

	/// <summary>
	///     Gets or sets whether this tab's content should be kept alive when deactivated.
	/// </summary>
	public bool KeepAlive { get; set; }

	/// <summary>
	///     Gets or sets the tooltip text for this tab header.
	/// </summary>
	public string? Tooltip { get; set; }

	/// <summary>
	///     Gets or sets additional CSS class(es) applied to the tab header.
	/// </summary>
	public string? CssClass { get; set; }

	/// <summary>
	///     Gets or sets a custom color for this tab's active state indicator.
	///     Overrides the theme's active color when this tab is active.
	/// </summary>
	public string? ActiveColor { get; set; }

	/// <summary>
	///     Gets or sets custom action buttons rendered in the tab header.
	/// </summary>
	public RenderFragment? ActionContent { get; set; }

	/// <summary>
	///     Gets the timestamp when this tab was created.
	/// </summary>
	public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

	/// <summary>
	///     Gets or sets the timestamp of the last activation.
	/// </summary>
	public DateTimeOffset? LastActivatedAt { get; set; }
}
