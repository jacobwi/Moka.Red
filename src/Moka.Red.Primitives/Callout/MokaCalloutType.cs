using System.Diagnostics.CodeAnalysis;

namespace Moka.Red.Primitives.Callout;

/// <summary>
///     Callout type controlling the color accent and default icon.
///     Modeled after GitHub-style admonitions (Note, Tip, Important, Warning, Caution).
/// </summary>
[SuppressMessage("Design", "CA1069:Enums values should not be duplicated",
	Justification = "Info is a deliberate alias for Note: callers reach for the name they know from other libraries.")]
public enum MokaCalloutType
{
	/// <summary>Informational note - blue/info accent.</summary>
	Note,

	/// <summary>
	///     Informational note. The same callout as <see cref="Note" />, under the name callers coming from
	///     other libraries look for first.
	/// </summary>
	Info = Note,

	/// <summary>Helpful tip - green/success accent.</summary>
	Tip,

	/// <summary>Important information - purple/secondary accent.</summary>
	Important,

	/// <summary>Warning - yellow/warning accent.</summary>
	Warning,

	/// <summary>Caution/danger - red/error accent.</summary>
	Caution
}
