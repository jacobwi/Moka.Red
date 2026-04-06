namespace Moka.Red.Primitives.Callout;

/// <summary>
///     Callout type controlling the color accent and default icon.
///     Modeled after GitHub-style admonitions (Note, Tip, Important, Warning, Caution).
/// </summary>
public enum MokaCalloutType
{
	/// <summary>Informational note — blue/info accent.</summary>
	Note,

	/// <summary>Helpful tip — green/success accent.</summary>
	Tip,

	/// <summary>Important information — purple/secondary accent.</summary>
	Important,

	/// <summary>Warning — yellow/warning accent.</summary>
	Warning,

	/// <summary>Caution/danger — red/error accent.</summary>
	Caution
}
