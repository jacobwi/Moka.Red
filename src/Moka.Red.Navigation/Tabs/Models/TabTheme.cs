using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.Tabs.Models;

/// <summary>
///     Provides C# theme overrides that generate CSS custom property inline styles,
///     taking precedence over the library stylesheet defaults.
/// </summary>
public sealed class TabTheme
{
	#region Tab Strip

	/// <summary>Background color for the tab strip bar.</summary>
	public string? StripBackground { get; set; }

	/// <summary>Border color below the tab strip.</summary>
	public string? StripBorderColor { get; set; }

	#endregion

	#region Tab Header (Inactive)

	/// <summary>Text color for inactive tab headers.</summary>
	public string? TabColor { get; set; }

	/// <summary>Background color for inactive tab headers.</summary>
	public string? TabBackground { get; set; }

	/// <summary>Background color for tab headers on hover.</summary>
	public string? TabHoverBackground { get; set; }

	/// <summary>Text color for tab headers on hover.</summary>
	public string? TabHoverColor { get; set; }

	#endregion

	#region Tab Header (Active)

	/// <summary>Text color for the active tab header.</summary>
	public string? ActiveTabColor { get; set; }

	/// <summary>Background color for the active tab header.</summary>
	public string? ActiveTabBackground { get; set; }

	/// <summary>Border color for the active tab indicator.</summary>
	public string? ActiveTabBorderColor { get; set; }

	/// <summary>Border width for the active tab indicator (e.g., "2px").</summary>
	public string? ActiveTabBorderWidth { get; set; }

	#endregion

	#region Pin / Close Buttons

	/// <summary>Color for the close button icon.</summary>
	public string? CloseButtonColor { get; set; }

	/// <summary>Background color for the close button on hover.</summary>
	public string? CloseButtonHoverBackground { get; set; }

	/// <summary>Color for the pin button icon.</summary>
	public string? PinButtonColor { get; set; }

	/// <summary>Background color for the pin button on hover.</summary>
	public string? PinButtonHoverBackground { get; set; }

	#endregion

	#region Badge

	/// <summary>Background color for tab badges.</summary>
	public string? BadgeBackground { get; set; }

	/// <summary>Text color for tab badges.</summary>
	public string? BadgeColor { get; set; }

	#endregion

	#region Group

	/// <summary>Default group border position when not overridden per-group.</summary>
	public BorderPosition DefaultGroupBorderPosition { get; set; } = BorderPosition.Left;

	/// <summary>Default group border width (e.g., "3px").</summary>
	public string? GroupBorderWidth { get; set; }

	/// <summary>Background color for group headers.</summary>
	public string? GroupHeaderBackground { get; set; }

	/// <summary>Text color for group titles.</summary>
	public string? GroupTitleColor { get; set; }

	#endregion

	#region Container

	/// <summary>Border color for the tab container.</summary>
	public string? ContainerBorderColor { get; set; }

	/// <summary>Background color for the tab container.</summary>
	public string? ContainerBackground { get; set; }

	/// <summary>Border radius for the tab container (e.g., "8px").</summary>
	public string? ContainerRadius { get; set; }

	#endregion

	#region Context Menu

	/// <summary>Background color for the context menu.</summary>
	public string? ContextMenuBackground { get; set; }

	/// <summary>Text color for context menu items.</summary>
	public string? ContextMenuItemColor { get; set; }

	/// <summary>Background color for context menu items on hover.</summary>
	public string? ContextMenuHoverBackground { get; set; }

	#endregion

	#region Style Generation

	/// <summary>
	///     Generates a CSS custom property override string to be applied as an inline style
	///     on the tab container element. Only set properties are included, and a value that could end its
	///     declaration or run into the next one is left out (see <see cref="CssValues.IsSelfContained" />).
	/// </summary>
	public string? ToContainerStyle() => new StyleBuilder()
		.AddStyle("--moka-tab-strip-bg", StripBackground)
		.AddStyle("--moka-tab-strip-border-color", StripBorderColor)
		.AddStyle("--moka-tab-color", TabColor)
		.AddStyle("--moka-tab-bg", TabBackground)
		.AddStyle("--moka-tab-hover-bg", TabHoverBackground)
		.AddStyle("--moka-tab-hover-color", TabHoverColor)
		.AddStyle("--moka-tab-active-color", ActiveTabColor)
		.AddStyle("--moka-tab-active-bg", ActiveTabBackground)
		.AddStyle("--moka-tab-active-border-color", ActiveTabBorderColor)
		.AddStyle("--moka-tab-active-border-width", ActiveTabBorderWidth)
		.AddStyle("--moka-tab-close-color", CloseButtonColor)
		.AddStyle("--moka-tab-close-hover-bg", CloseButtonHoverBackground)
		.AddStyle("--moka-tab-pin-color", PinButtonColor)
		.AddStyle("--moka-tab-pin-hover-bg", PinButtonHoverBackground)
		.AddStyle("--moka-tab-badge-bg", BadgeBackground)
		.AddStyle("--moka-tab-badge-color", BadgeColor)
		.AddStyle("--moka-tab-group-border-width", GroupBorderWidth)
		.AddStyle("--moka-tab-group-header-bg", GroupHeaderBackground)
		.AddStyle("--moka-tab-group-title-color", GroupTitleColor)
		.AddStyle("--moka-tab-container-border-color", ContainerBorderColor)
		.AddStyle("--moka-tab-container-bg", ContainerBackground)
		.AddStyle("--moka-tab-container-radius", ContainerRadius)
		.AddStyle("--moka-tab-ctx-bg", ContextMenuBackground)
		.AddStyle("--moka-tab-ctx-item-color", ContextMenuItemColor)
		.AddStyle("--moka-tab-ctx-item-hover-bg", ContextMenuHoverBackground)
		.Build();

	#endregion
}
