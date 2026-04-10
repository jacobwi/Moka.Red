namespace Moka.Red.Core.Theming;

/// <summary>
///     Spacing and layout tokens. Dense/compact defaults — tighter than typical UI libraries.
///     All values are CSS size strings.
/// </summary>
public sealed record MokaSpacing
{
	// Spacing scale (4px base unit)
	public string Xxs { get; init; } = "0.125rem"; // 2px
	public string Xs { get; init; } = "0.25rem"; // 4px
	public string Sm { get; init; } = "0.375rem"; // 6px
	public string Md { get; init; } = "0.5rem"; // 8px
	public string Lg { get; init; } = "0.75rem"; // 12px
	public string Xl { get; init; } = "1rem"; // 16px
	public string Xxl { get; init; } = "1.5rem"; // 24px

	// Border radius — sharp corners, max 12px for modals
	public string RadiusNone { get; init; } = "0";
	public string RadiusSm { get; init; } = "4px"; // chips, badges, tiny buttons
	public string RadiusMd { get; init; } = "8px"; // default — buttons, inputs
	public string RadiusLg { get; init; } = "10px"; // cards, list rows
	public string RadiusXl { get; init; } = "12px"; // modals, popovers
	public string RadiusFull { get; init; } = "9999px";

	/// <summary>Default dense spacing scale.</summary>
	public static MokaSpacing Default => new();
}
