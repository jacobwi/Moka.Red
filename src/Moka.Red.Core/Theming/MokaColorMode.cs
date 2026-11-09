// In Core since MokaThemeToggle in Primitives needs it and Primitives cannot reference ThemeGen. The namespace is the one
// it always had, with a TypeForwardedTo in ThemeGen, so code written or compiled against the older packages keeps working.
namespace Moka.Red.ThemeGen.Appearance;

/// <summary>Whether an app is light, dark, or follows the device.</summary>
public enum MokaColorMode
{
	/// <summary>Always the light form of the palette.</summary>
	Light,

	/// <summary>Always the dark form of the palette.</summary>
	Dark,

	/// <summary>Whichever the device is set to, changing when the device does.</summary>
	System
}
