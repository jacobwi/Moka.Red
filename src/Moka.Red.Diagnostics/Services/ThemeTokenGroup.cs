using Moka.Red.Core.Theming;

namespace Moka.Red.Diagnostics.Services;

/// <summary>
///     Categorizes a theme token for display purposes.
/// </summary>
public enum ThemeTokenKind
{
	/// <summary>A color, shown with a swatch.</summary>
	Color,

	/// <summary>A font family stack.</summary>
	FontFamily,

	/// <summary>A text size.</summary>
	FontSize,

	/// <summary>A font weight.</summary>
	FontWeight,

	/// <summary>A line height.</summary>
	LineHeight,

	/// <summary>A spacing step, shown with a bar of its length.</summary>
	Spacing,

	/// <summary>A corner radius, shown with a bar of its length.</summary>
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
