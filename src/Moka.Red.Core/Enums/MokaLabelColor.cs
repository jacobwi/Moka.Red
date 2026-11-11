namespace Moka.Red.Core.Enums;

/// <summary>
///     A named hue for things people color themselves, such as tags, folders and libraries, apart from the colors that
///     mean something (<see cref="MokaColor.Error" />, <see cref="MokaColor.Warning" /> and the rest). Store the name,
///     never a color value: the theme decides the shades, so a stored choice keeps working in light and dark and in
///     every tone. The values run in picker order, slate last. <c>MokaLabelColors</c> has the CSS for each.
/// </summary>
public enum MokaLabelColor
{
	/// <summary>Red.</summary>
	Red,

	/// <summary>Rose, between red and pink.</summary>
	Rose,

	/// <summary>Orange.</summary>
	Orange,

	/// <summary>Amber, between orange and yellow.</summary>
	Amber,

	/// <summary>Yellow.</summary>
	Yellow,

	/// <summary>Lime, a yellow green.</summary>
	Lime,

	/// <summary>Green.</summary>
	Green,

	/// <summary>Emerald, a blue green.</summary>
	Emerald,

	/// <summary>Teal.</summary>
	Teal,

	/// <summary>Cyan.</summary>
	Cyan,

	/// <summary>Sky, a light blue.</summary>
	Sky,

	/// <summary>Blue.</summary>
	Blue,

	/// <summary>Indigo.</summary>
	Indigo,

	/// <summary>Violet, between indigo and purple.</summary>
	Violet,

	/// <summary>Purple.</summary>
	Purple,

	/// <summary>Pink.</summary>
	Pink,

	/// <summary>Slate, the neutral grey.</summary>
	Slate
}
