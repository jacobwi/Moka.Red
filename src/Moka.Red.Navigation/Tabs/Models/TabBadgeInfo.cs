namespace Moka.Red.Navigation.Tabs.Models;

/// <summary>
///     Represents badge display information for a tab header.
/// </summary>
public sealed class TabBadgeInfo
{
	/// <summary>
	///     Gets or sets the notification count displayed in the badge. Null hides the count.
	/// </summary>
	public int? Count { get; set; }

	/// <summary>
	///     Gets or sets whether to show a status dot indicator instead of a count.
	/// </summary>
	public bool ShowDot { get; set; }

	/// <summary>
	///     Gets or sets a CSS class for the badge (e.g., color/severity).
	/// </summary>
	public string? CssClass { get; set; }

	/// <summary>
	///     Gets or sets custom content to render inside the badge.
	/// </summary>
	public string? CustomContent { get; set; }

	/// <summary>
	///     Gets whether this badge has any visible content.
	/// </summary>
	public bool IsVisible => ShowDot || Count is > 0 || !string.IsNullOrEmpty(CustomContent);
}
