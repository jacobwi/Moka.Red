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
