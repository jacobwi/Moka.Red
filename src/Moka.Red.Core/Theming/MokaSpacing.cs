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

	/// <summary>The height of a medium button.</summary>
	public string ButtonHeight { get; init; } = "0";

	/// <summary>The height of a small button.</summary>
	public string ButtonHeightSm { get; init; } = "0";

	/// <summary>The height of a large button.</summary>
	public string ButtonHeightLg { get; init; } = "0";

	/// <summary>The side padding of a medium button.</summary>
	public string ButtonPadding { get; init; } = "var(--moka-spacing-lg)";

	/// <summary>The side padding of a small button.</summary>
	public string ButtonPaddingSm { get; init; } = "var(--moka-spacing-md)";

	/// <summary>The side padding of a text input.</summary>
	public string InputPadding { get; init; } = "var(--moka-spacing-md)";

	/// <summary>The height of a segment in a segmented control.</summary>
	public string SegmentHeight { get; init; } = "0";

	/// <summary>The padding of a table cell.</summary>
	public string CellPadding { get; init; } = "var(--moka-spacing-sm) var(--moka-spacing-md)";

	/// <summary>The padding of a tag, a chip or a pill.</summary>
	public string PillPadding { get; init; } = "var(--moka-spacing-xxs) var(--moka-spacing-md)";

	/// <summary>The height of a toolbar, a command bar or an app bar.</summary>
	public string ToolbarHeight { get; init; } = "40px";

	/// <summary>The height of a dense toolbar, command bar or app bar, and of a tab strip.</summary>
	public string ToolbarHeightDense { get; init; } = "32px";

	/// <summary>The height of a status bar.</summary>
	public string StatusBarHeight { get; init; } = "22px";

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
		RadiusPopup = "var(--moka-radius-lg)",
		ButtonPadding = "14px",
		ButtonPaddingSm = "10px",
		InputPadding = "12px",
		PillPadding = "5px 10px"
	}.WithGateLayout(false);

	/// <summary>The <see cref="MokaLook.Gate" /> look's sizes at compact density.</summary>
	public static MokaSpacing GateCompact { get; } = Gate.WithGateLayout(true);

	internal MokaSpacing WithCorners(MokaCorners corners) => corners switch
	{
		MokaCorners.Medium => this with
		{
			RadiusSm = "5px",
			RadiusMd = "8px",
			RadiusLg = "10px",
			RadiusXl = "14px",
			RadiusCard = "var(--moka-radius-xl)",
			RadiusPopup = "var(--moka-radius-lg)"
		},
		MokaCorners.Tight => this with
		{
			RadiusSm = "4px",
			RadiusMd = "8px",
			RadiusLg = "10px",
			RadiusXl = "12px",
			RadiusCard = "var(--moka-radius-lg)",
			RadiusPopup = "var(--moka-radius-xl)"
		},
		_ => this with
		{
			RadiusSm = "6px",
			RadiusMd = "10px",
			RadiusLg = "14px",
			RadiusXl = "20px",
			RadiusCard = "var(--moka-radius-xl)",
			RadiusPopup = "var(--moka-radius-lg)"
		}
	};

	/// <summary>The sizes of <see cref="MokaTheme.Crisp" />: compact density with small corners.</summary>
	public static MokaSpacing Crisp { get; } = GateCompact with
	{
		RadiusSm = "4px",
		RadiusMd = "8px",
		RadiusLg = "10px",
		RadiusXl = "12px",
		RadiusCard = "var(--moka-radius-lg)",
		RadiusPopup = "var(--moka-radius-xl)",
		PillPadding = "3px 8px"
	};

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
			MenuRow = "26px",
			ButtonHeight = "32px",
			ButtonHeightSm = "28px",
			ButtonHeightLg = "38px",
			SegmentHeight = "28px",
			CellPadding = "6px 14px",
			ToolbarHeight = "44px",
			ToolbarHeightDense = "36px",
			StatusBarHeight = "24px"
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
			MenuRow = "30px",
			ButtonHeight = "38px",
			ButtonHeightSm = "32px",
			ButtonHeightLg = "46px",
			SegmentHeight = "34px",
			CellPadding = "9px 16px",
			ToolbarHeight = "52px",
			ToolbarHeightDense = "44px",
			StatusBarHeight = "28px"
		};
}
