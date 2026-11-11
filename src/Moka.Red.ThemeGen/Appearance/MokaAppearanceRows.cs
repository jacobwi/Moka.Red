namespace Moka.Red.ThemeGen.Appearance;

/// <summary>Which rows <see cref="MokaAppearancePanel" /> shows. Combine them with <c>|</c>.</summary>
[Flags]
public enum MokaAppearanceRows
{
	/// <summary>No built-in rows, only the app's own.</summary>
	None = 0,

	/// <summary>Light, dark or following the device.</summary>
	ColorMode = 1 << 0,

	/// <summary>The palette. It hides itself when there's only one to pick from.</summary>
	Palette = 1 << 1,

	/// <summary>The accent. It hides itself when there are none to pick from.</summary>
	Accent = 1 << 2,

	/// <summary>How much room things take.</summary>
	Density = 1 << 3,

	/// <summary>How large text is.</summary>
	TextSize = 1 << 4,

	/// <summary>How round things are.</summary>
	Corners = 1 << 5,

	/// <summary>How a primary button is drawn.</summary>
	MainButton = 1 << 6,

	/// <summary>The letter case of buttons, chips, tabs and titles.</summary>
	LetterCase = 1 << 7,

	/// <summary>The font of page titles and big numbers.</summary>
	TitleFont = 1 << 8,

	/// <summary>How selected and focused things are marked.</summary>
	Selection = 1 << 9,

	/// <summary>How the place you are in the app is marked.</summary>
	CurrentPlace = 1 << 10,

	/// <summary>How status bars are drawn.</summary>
	StatusBar = 1 << 11,

	/// <summary>Whether text saying what a thing is for shows.</summary>
	Descriptions = 1 << 12,

	/// <summary>How toasts are drawn.</summary>
	Toasts = 1 << 13,

	/// <summary>How several toasts sit together: one under another, or in a pile.</summary>
	ToastStacking = 1 << 14,

	/// <summary>How notification lists are drawn.</summary>
	Notifications = 1 << 15,

	/// <summary>How menus, popovers, hover cards and other floating panels are drawn.</summary>
	Popups = 1 << 16,

	/// <summary>How the rows of lists that open from a field are drawn.</summary>
	Lists = 1 << 17,

	/// <summary>How dialogs are drawn.</summary>
	Dialogs = 1 << 18,

	/// <summary>How buttons are drawn.</summary>
	Buttons = 1 << 19,

	/// <summary>
	///     One row that sets toasts, notifications, popups, lists, dialogs and buttons at once, through
	///     <c>MokaTheme.WithComponentStyle</c>. The rows in <see cref="Components" /> still fine-tune each one after it.
	/// </summary>
	ComponentStyle = 1 << 20,

	/// <summary>How strong the label colors on tags, chips, colored folders and swatches are.</summary>
	LabelTone = 1 << 21,

	/// <summary>How charts are drawn: grid, ticks, legend, points and marks together.</summary>
	Charts = 1 << 22,

	/// <summary>The colors chart series take in turn.</summary>
	ChartPalette = 1 << 23,

	/// <summary>Whether chart marks are hatched, on screen or only in print, and whether print and forced colors draw in ink.</summary>
	ChartTexture = 1 << 24,

	/// <summary>The rows shown unless an app chooses: color mode, palette, accent, status bar, density and text size.</summary>
	Default = ColorMode | Palette | Accent | StatusBar | Density | TextSize,

	/// <summary>
	///     Every style row. A palette on the original design ignores them, so they hide while such a palette is picked, and
	///     the person's choices stay in the value for when another one is. <see cref="Components" />,
	///     <see cref="ComponentStyle" /> and <see cref="ChartsGroup" /> hide the same way.
	/// </summary>
	Style = Density | TextSize | Corners | MainButton | LetterCase | TitleFont | Selection | CurrentPlace | StatusBar | Descriptions | LabelTone,

	/// <summary>
	///     The seven component look rows, under their own heading after the style rows: toasts, toast stacking,
	///     notifications, popups, lists, dialogs and buttons. Not in <see cref="Default" /> or <see cref="Style" />.
	/// </summary>
	Components = Toasts | ToastStacking | Notifications | Popups | Lists | Dialogs | Buttons,

	/// <summary>
	///     The three chart rows, under their own heading after the component rows: the look, the colors, and texture and
	///     print. Not in <see cref="Default" /> or <see cref="Style" />.
	/// </summary>
	ChartsGroup = Charts | ChartPalette | ChartTexture,

	/// <summary>Every row.</summary>
	All = ColorMode | Palette | Accent | Style | ComponentStyle | Components | ChartsGroup
}
