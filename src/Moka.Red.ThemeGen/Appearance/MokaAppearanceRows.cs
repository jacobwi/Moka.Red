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

	/// <summary>The rows shown unless an app chooses: color mode, palette, accent, status bar, density and text size.</summary>
	Default = ColorMode | Palette | Accent | StatusBar | Density | TextSize,

	/// <summary>
	///     Every row a palette on the original design ignores. They hide while such a palette is picked, and the person's
	///     choices stay in the value for when another one is.
	/// </summary>
	Style = Density | TextSize | Corners | MainButton | LetterCase | TitleFont | Selection | CurrentPlace | StatusBar | Descriptions,

	/// <summary>Every row.</summary>
	All = ColorMode | Palette | Accent | Style
}
