namespace Moka.Red.Core.Theming;

/// <summary>
///     The design a theme belongs to. It decides what no single color can: shadows, the focus ring, motion,
///     how <see cref="MokaTheme.WithAccent(string)" /> finds its shades and which palette
///     <see cref="MokaTheme.WithDark" /> picks.
/// </summary>
public enum MokaLook
{
	/// <summary>
	///     Near-black surfaces, a red accent, red-tinted borders, glow rings and uppercase controls.
	///     <see cref="MokaTheme.Dark" /> and <see cref="MokaTheme.Light" />.
	/// </summary>
	Matrix,

	/// <summary>
	///     Blue-grey surfaces, solid borders, sentence case, large soft shapes, and colors that mean something.
	///     <see cref="MokaTheme.Gate" /> and <see cref="MokaTheme.GateLight" />.
	/// </summary>
	Gate
}
