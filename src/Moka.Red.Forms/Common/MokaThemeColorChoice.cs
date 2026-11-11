namespace Moka.Red.Forms.Common;

/// <summary>A theme color a picker offers by name.</summary>
/// <param name="Label">What the swatch says, such as "On surface".</param>
/// <param name="Name">The token's name after <c>--moka-color-</c>, such as <c>on-surface</c>.</param>
internal sealed record MokaThemeColorChoice(string Label, string Name)
{
	/// <summary>The value the picker writes, such as <c>var(--moka-color-on-surface)</c>.</summary>
	internal string Token => $"var(--moka-color-{Name})";
}
