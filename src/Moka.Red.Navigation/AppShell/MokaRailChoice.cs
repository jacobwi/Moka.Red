namespace Moka.Red.Navigation.AppShell;

/// <summary>How <see cref="MokaAppShell" /> draws its side menu on anything wider than a phone.</summary>
public enum MokaRailChoice
{
	/// <summary>The full menu, with names.</summary>
	Expanded,

	/// <summary>The icon rail.</summary>
	Collapsed,

	/// <summary>The icon rail below 1280 px, and the full menu from there up.</summary>
	Automatic
}
