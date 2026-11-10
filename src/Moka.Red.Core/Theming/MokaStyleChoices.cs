namespace Moka.Red.Core.Theming;

/// <summary>How round things are. See <see cref="MokaTheme.WithCorners" />.</summary>
public enum MokaCorners
{
	/// <summary>Cards at 20 px, panels at 14 px, controls at 10 px.</summary>
	Soft,

	/// <summary>Cards at 14 px, panels at 10 px, controls at 8 px.</summary>
	Medium,

	/// <summary>Cards and panels at 10 px, controls at 8 px.</summary>
	Tight
}

/// <summary>How much room things take. See <see cref="MokaTheme.WithDensity(MokaDensity)" />.</summary>
public enum MokaDensity
{
	/// <summary>Controls 40 px tall, cards with 22 px of padding.</summary>
	Comfortable,

	/// <summary>Controls 34 px tall, cards with 14 px of padding.</summary>
	Compact
}

/// <summary>How large text is. See <see cref="MokaTheme.WithTextSize" />.</summary>
public enum MokaTextSize
{
	/// <summary>14 px body and input text, 30 px page titles.</summary>
	Normal,

	/// <summary>13 px body text, 12 px inputs, 20 px page titles.</summary>
	Small
}

/// <summary>How a primary button is drawn. See <see cref="MokaTheme.WithMainButton" />.</summary>
public enum MokaMainButton
{
	/// <summary>Filled with the accent.</summary>
	Solid,

	/// <summary>A tint of the accent, with accent text.</summary>
	Tinted,

	/// <summary>An accent outline and accent text, with nothing behind them.</summary>
	Outline
}

/// <summary>The letter case of buttons, chips, tabs and titles. See <see cref="MokaTheme.WithLetterCase" />.</summary>
public enum MokaLetterCase
{
	/// <summary>As written. Small labels over a group of things stay uppercase.</summary>
	Sentence,

	/// <summary>Uppercase, slightly spaced.</summary>
	Uppercase
}

/// <summary>The font of page titles and big numbers. See <see cref="MokaTheme.WithTitleFont" />.</summary>
public enum MokaTitleFont
{
	/// <summary>Space Grotesk.</summary>
	SpaceGrotesk,

	/// <summary>Inter, the same as the body text.</summary>
	Inter,

	/// <summary>JetBrains Mono.</summary>
	JetBrainsMono
}

/// <summary>How selected and focused things are marked. See <see cref="MokaTheme.WithSelection" />.</summary>
public enum MokaSelection
{
	/// <summary>A tint with a thin ring, and a small bar along a selected table row.</summary>
	Ring,

	/// <summary>A tint alone.</summary>
	Flat,

	/// <summary>A tint with a strong glow around it.</summary>
	Glow
}

/// <summary>
///     How the place you are in the app is marked: the current page in the rail and the side menu, the account card when
///     one of its pages is showing, and the current section of a settings list. See
///     <see cref="MokaTheme.WithCurrentPlace" />.
/// </summary>
public enum MokaCurrentPlace
{
	/// <summary>A tint, marked the way <see cref="MokaTheme.Selection" /> marks selected things.</summary>
	Ring,

	/// <summary>Filled with the accent, with a soft glow around it.</summary>
	Solid
}

/// <summary>How a status bar is drawn. See <see cref="MokaTheme.WithStatusBar" /> and <c>MokaStatusBar.Look</c>.</summary>
public enum MokaStatusBarLook
{
	/// <summary>A filled bar with a line above it, the original look.</summary>
	Standard,

	/// <summary>The whole bar in the accent. Strong, for one bar per window.</summary>
	Accent,

	/// <summary>No fill, a thin line above and muted text. The quietest.</summary>
	Hairline,

	/// <summary>Lifted off the window's edges with rounded corners and a shadow.</summary>
	Floating,

	/// <summary>No bar, each item its own small outlined chip.</summary>
	Chips,

	/// <summary>Edge to edge blocks with hard dividers, the first one tinted.</summary>
	Segments,

	/// <summary>Monospace capitals on the page color, under a line in the accent.</summary>
	Terminal
}

/// <summary>How toasts are drawn. See <see cref="MokaTheme.WithToasts" /> and <c>MokaToastHost.Look</c>.</summary>
public enum MokaToastLook
{
	/// <summary>A raised card. The severity shows in the icon and the bar counting down.</summary>
	Standard,

	/// <summary>A thick edge in the severity's color down the start side.</summary>
	ToneEdge,

	/// <summary>The whole toast in the severity's soft color.</summary>
	Tinted,

	/// <summary>The icon in a tinted tile, and the action drawn as a button.</summary>
	IconTile,

	/// <summary>One dark line with the action at its end. Shows the title only.</summary>
	Snackbar,

	/// <summary>A small capsule centered at the top or bottom. Shows the title and the action only.</summary>
	Pill,

	/// <summary>Monospace capitals with a tag such as [FAIL], on the page color with a glow in the severity's color.</summary>
	Terminal
}

/// <summary>How several toasts sit together. See <see cref="MokaTheme.WithToastStacking" /> and <c>MokaToastHost.Stacking</c>.</summary>
public enum MokaToastStacking
{
	/// <summary>Every toast in full, one under another.</summary>
	List,

	/// <summary>Newer toasts cover older ones like a deck, and the pile fans out on hover or focus.</summary>
	Pile
}

/// <summary>
///     How notification lists are drawn. See <see cref="MokaTheme.WithNotifications" /> and the <c>Look</c> parameter
///     of <c>MokaNotificationCenter</c> and <c>MokaNotificationBell</c>.
/// </summary>
public enum MokaNotificationLook
{
	/// <summary>Unread rows tinted in the accent; title, message and time on single lines.</summary>
	Standard,

	/// <summary>A dot marks unread, the icon sits in a round tile, the time beside the title, two lines of message.</summary>
	Inbox,

	/// <summary>Each notification its own small card. Unread cards get an accent outline and a dot.</summary>
	Cards,

	/// <summary>One line each: icon, title and time.</summary>
	Compact,

	/// <summary>Today and Earlier headings, square icon tiles and an unread dot.</summary>
	Grouped,

	/// <summary>A line down the side with each icon on it and the time above the title.</summary>
	Timeline,

	/// <summary>Monospace with tags such as [WARN] in place of icons, and a glow on unread rows.</summary>
	Terminal
}

/// <summary>
///     How floating panels are drawn: menus, context menus, popovers, hover cards and the box around lists that open
///     from a field. See <see cref="MokaTheme.WithPopups" />.
/// </summary>
public enum MokaPopupLook
{
	/// <summary>A border and a soft shadow.</summary>
	Standard,

	/// <summary>A border and no shadow, with tighter corners.</summary>
	Flat,

	/// <summary>No visible border and a deep shadow.</summary>
	Elevated,

	/// <summary>See-through, blurring what is behind it.</summary>
	Glass,

	/// <summary>The panel and the lit row carry a trace of the accent.</summary>
	AccentTint,

	/// <summary>A dark panel on a light page and a lighter one on a dark page.</summary>
	Contrast,

	/// <summary>The page color with a tinted border, monospace rows and a glow on the lit row.</summary>
	Terminal
}

/// <summary>
///     How the rows of a list that opens from a field are drawn, such as <c>MokaSelect</c>'s. The box around them
///     follows <see cref="MokaPopupLook" />. See <see cref="MokaTheme.WithLists" />.
/// </summary>
public enum MokaListLook
{
	/// <summary>Rows edge to edge. The chosen one is tinted.</summary>
	Standard,

	/// <summary>Inset rounded rows. The chosen one keeps its background and gets a check.</summary>
	Check,

	/// <summary>The chosen row filled with the accent.</summary>
	Solid,

	/// <summary>The list joins the field into one shape.</summary>
	Attached,

	/// <summary>Roomier rows with a radio circle.</summary>
	Radio,

	/// <summary>Short rows, with key hints along the bottom.</summary>
	Compact,

	/// <summary>Monospace, with [x] on the chosen row and a glow on the lit one.</summary>
	Terminal
}

/// <summary>How dialogs are drawn. See <see cref="MokaTheme.WithDialogs" /> and <c>MokaDialog.Look</c>.</summary>
public enum MokaDialogLook
{
	/// <summary>A title in the display font, a close button, and the buttons at the bottom end.</summary>
	Standard,

	/// <summary>The header and the buttons in tinted bands with lines between.</summary>
	Bands,

	/// <summary>The dialog's icon in a tinted tile beside the title.</summary>
	IconLead,

	/// <summary>Icon, title and text centered, over buttons stacked across the whole width.</summary>
	Centered,

	/// <summary>Monospace capitals, a tinted border with a glow, and outlined buttons.</summary>
	Terminal
}

/// <summary>
///     How buttons are drawn. The letter case still applies, the corners do except under <see cref="Pill" />, and
///     <see cref="MokaMainButton" /> still fills the main button except under <see cref="Glow" />, which outlines it. See
///     <see cref="MokaTheme.WithButtons" /> and <c>MokaButton.Look</c>.
/// </summary>
public enum MokaButtonLook
{
	/// <summary>Flat fills and outlines.</summary>
	Standard,

	/// <summary>A light top edge and a faint shadow, pressed in on click.</summary>
	Raised,

	/// <summary>Secondary buttons filled with no border, danger in its soft color.</summary>
	Soft,

	/// <summary>Fully round ends, whatever the corners.</summary>
	Pill,

	/// <summary>Outlines that glow on hover.</summary>
	Glow,

	/// <summary>A hard outline and a solid offset shadow the button presses into.</summary>
	Offset,

	/// <summary>Filled buttons run from their color to a warmer shade of it.</summary>
	Gradient,

	/// <summary>Secondary buttons show no border or fill until hovered.</summary>
	Ghost
}

/// <summary>
///     Sets toasts, notifications, popups, lists, dialogs and buttons in one step. See
///     <see cref="MokaTheme.WithComponentStyle" />.
/// </summary>
public enum MokaComponentStyle
{
	/// <summary>Every one of them <c>Standard</c>.</summary>
	Standard,

	/// <summary>Icon tile toasts, an inbox, elevated popups, check lists, icon lead dialogs and soft buttons.</summary>
	Soft,

	/// <summary>Every one of them <c>Terminal</c>, with glowing buttons.</summary>
	Terminal
}
