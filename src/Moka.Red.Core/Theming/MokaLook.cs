namespace Moka.Red.Core.Theming;

/// <summary>
///     The design a theme belongs to. It decides what no single color can: shadows, the focus ring, motion,
///     how <see cref="MokaTheme.WithAccent(string)" /> finds its shades and which palette
///     <see cref="MokaTheme.WithDark" /> picks.
/// </summary>
public enum MokaLook
{
	/// <summary>
	///     The original design: glow rings, uppercase and small, outlined controls. <see cref="MokaTheme.Dark" />
	///     and <see cref="MokaTheme.Light" />, which keep rendering as they always have.
	/// </summary>
	Matrix,

	/// <summary>
	///     The current design: sentence case, roomy controls, soft shapes, and colors that mean something. Every
	///     other built-in theme, including <see cref="MokaTheme.Default" />. This is the design the style choices
	///     such as <see cref="MokaTheme.WithCorners" /> apply to, and components draw it under <c>.moka-gate</c>.
	/// </summary>
	Gate
}
