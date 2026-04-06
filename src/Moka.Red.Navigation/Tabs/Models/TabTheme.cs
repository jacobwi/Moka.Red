using System.Text;

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
	///     on the tab container element. Only non-null properties are included.
	/// </summary>
	public string? ToContainerStyle()
	{
		var sb = new StringBuilder();

		Append(sb, "--moka-tab-strip-bg", StripBackground);
		Append(sb, "--moka-tab-strip-border-color", StripBorderColor);
		Append(sb, "--moka-tab-color", TabColor);
		Append(sb, "--moka-tab-bg", TabBackground);
		Append(sb, "--moka-tab-hover-bg", TabHoverBackground);
		Append(sb, "--moka-tab-hover-color", TabHoverColor);
		Append(sb, "--moka-tab-active-color", ActiveTabColor);
		Append(sb, "--moka-tab-active-bg", ActiveTabBackground);
		Append(sb, "--moka-tab-active-border-color", ActiveTabBorderColor);
		Append(sb, "--moka-tab-active-border-width", ActiveTabBorderWidth);
		Append(sb, "--moka-tab-close-color", CloseButtonColor);
		Append(sb, "--moka-tab-close-hover-bg", CloseButtonHoverBackground);
		Append(sb, "--moka-tab-pin-color", PinButtonColor);
		Append(sb, "--moka-tab-pin-hover-bg", PinButtonHoverBackground);
		Append(sb, "--moka-tab-badge-bg", BadgeBackground);
		Append(sb, "--moka-tab-badge-color", BadgeColor);
		Append(sb, "--moka-tab-group-border-width", GroupBorderWidth);
		Append(sb, "--moka-tab-group-header-bg", GroupHeaderBackground);
		Append(sb, "--moka-tab-group-title-color", GroupTitleColor);
		Append(sb, "--moka-tab-container-border-color", ContainerBorderColor);
		Append(sb, "--moka-tab-container-bg", ContainerBackground);
		Append(sb, "--moka-tab-container-radius", ContainerRadius);
		Append(sb, "--moka-tab-ctx-bg", ContextMenuBackground);
		Append(sb, "--moka-tab-ctx-item-color", ContextMenuItemColor);
		Append(sb, "--moka-tab-ctx-item-hover-bg", ContextMenuHoverBackground);

		return sb.Length > 0 ? sb.ToString() : null;
	}

	private static void Append(StringBuilder sb, string property, string? value)
	{
		if (value is null)
		{
			return;
		}

		sb.Append(property);
		sb.Append(": ");
		sb.Append(value);
		sb.Append("; ");
	}

	#endregion
}
