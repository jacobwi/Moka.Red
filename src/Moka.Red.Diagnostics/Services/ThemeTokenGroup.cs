namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Categorizes a theme token for display purposes.
/// </summary>
public enum ThemeTokenKind
{
	Color,
	FontFamily,
	FontSize,
	FontWeight,
	LineHeight,
	Spacing,
	BorderRadius
}

/// <summary>
///     A single theme token extracted from a <see cref="MokaTheme" />.
/// </summary>
/// <param name="CssVariable">The CSS custom property name, e.g. "--moka-color-primary".</param>
/// <param name="Value">The current value, e.g. "#d32f2f".</param>
/// <param name="Kind">The token category for display.</param>
public sealed record ThemeToken(string CssVariable, string Value, ThemeTokenKind Kind);

/// <summary>
///     A named group of related theme tokens (e.g. "Palette", "Typography").
/// </summary>
/// <param name="Name">Display name for the group.</param>
/// <param name="Tokens">Tokens in this group.</param>
public sealed record ThemeTokenGroup(string Name, IReadOnlyList<ThemeToken> Tokens);
