namespace Moka.Red.Core.Theming;

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
