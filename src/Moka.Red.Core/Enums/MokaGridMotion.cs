namespace Moka.Red.Core.Enums;

/// <summary>
///     How a <c>MokaGridBackground</c> moves. Everything stands still when the system asks for reduced motion.
/// </summary>
public enum MokaGridMotion
{
	/// <summary>Still. Nothing moves.</summary>
	None,

	/// <summary>
	///     The pattern slides one tile down and to the right, without end, at a slow steady speed. Topographic and
	///     Radar don't repeat, so they stay still.
	/// </summary>
	Drift,

	/// <summary>The glow swells and settles every few seconds while the pattern stays put. Needs a glow.</summary>
	Pulse,

	/// <summary>Every few seconds a band of brighter pattern passes from left to right, then the pattern rests.</summary>
	Sweep
}
