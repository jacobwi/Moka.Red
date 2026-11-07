namespace Moka.Red.Core.Theming;

/// <summary>
///     Spacing and layout tokens. Dense/compact defaults - tighter than typical UI libraries.
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

	// Border radius - sharp corners, max 12px for modals
	public string RadiusNone { get; init; } = "0";
	public string RadiusSm { get; init; } = "4px"; // chips, badges, tiny buttons
	public string RadiusMd { get; init; } = "8px"; // default - buttons, inputs
	public string RadiusLg { get; init; } = "10px"; // cards, list rows
	public string RadiusXl { get; init; } = "12px"; // modals, popovers
	public string RadiusFull { get; init; } = "9999px";

	/// <summary>The corners of a card.</summary>
	public string RadiusCard { get; init; } = "var(--moka-radius-lg)";

	/// <summary>The corners of dialogs, popovers and menus.</summary>
	public string RadiusPopup { get; init; } = "var(--moka-radius-xl)";

	/// <summary>The gap between the blocks of a page.</summary>
	public string PageGap { get; init; } = "var(--moka-spacing-xl)";

	/// <summary>The padding of a card.</summary>
	public string CardPadding { get; init; } = "var(--moka-spacing-md) var(--moka-spacing-lg)";

	/// <summary>The gap between the parts of a card.</summary>
	public string CardGap { get; init; } = "var(--moka-spacing-md)";

	/// <summary>The gap between the cells of a grid of cards.</summary>
	public string GridGap { get; init; } = "var(--moka-spacing-md)";

	/// <summary>The padding of a panel inside a card.</summary>
	public string PanelPadding { get; init; } = "var(--moka-spacing-md) var(--moka-spacing-lg)";

	/// <summary>The padding of an inner card.</summary>
	public string InnerPadding { get; init; } = "var(--moka-spacing-md)";

	/// <summary>The padding of a list row.</summary>
	public string RowPadding { get; init; } = "var(--moka-spacing-sm) var(--moka-spacing-md)";

	/// <summary>The padding of a small item in a list.</summary>
	public string ItemPadding { get; init; } = "var(--moka-spacing-xs) var(--moka-spacing-md)";

	/// <summary>The height of a medium control.</summary>
	public string ControlHeight { get; init; } = "2rem";

	/// <summary>The padding of the main area of a page.</summary>
	public string MainPadding { get; init; } = "var(--moka-spacing-xl)";

	/// <summary>The height of a row in a side menu.</summary>
	public string MenuRow { get; init; } = "1.75rem";

	/// <summary>Default dense spacing scale.</summary>
	public static MokaSpacing Default { get; } = new();

	/// <summary>The <see cref="MokaLook.Gate" /> look's sizes at comfortable density: big, soft shapes.</summary>
	public static MokaSpacing Gate { get; } = new MokaSpacing
	{
		RadiusSm = "6px",
		RadiusMd = "10px",
		RadiusLg = "14px",
		RadiusXl = "20px",
		RadiusCard = "var(--moka-radius-xl)",
		RadiusPopup = "var(--moka-radius-lg)"
	}.WithGateLayout(false);

	/// <summary>The <see cref="MokaLook.Gate" /> look's sizes at compact density.</summary>
	public static MokaSpacing GateCompact { get; } = Gate.WithGateLayout(true);

	// Density swaps these rather than scaling them: 22px of padding becomes 14px and a 40px control 34px, and
	// a padding with several values could not be multiplied anyway.
	internal MokaSpacing WithGateLayout(bool compact) => compact
		? this with
		{
			PageGap = "12px",
			CardPadding = "14px",
			CardGap = "10px",
			GridGap = "10px",
			PanelPadding = "7px 10px",
			InnerPadding = "10px",
			RowPadding = "5px 10px",
			ItemPadding = "4px 10px",
			ControlHeight = "34px",
			MainPadding = "18px 22px 110px",
			MenuRow = "26px"
		}
		: this with
		{
			PageGap = "20px",
			CardPadding = "22px",
			CardGap = "16px",
			GridGap = "16px",
			PanelPadding = "12px 14px",
			InnerPadding = "14px",
			RowPadding = "10px 12px",
			ItemPadding = "8px 10px",
			ControlHeight = "40px",
			MainPadding = "28px 32px 120px",
			MenuRow = "30px"
		};
}
