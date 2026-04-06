using Moka.Red.Core.Theming;

namespace Moka.Red.Primitives.ThemeSwitcher;

/// <summary>
///     Represents a selectable theme option in the <see cref="MokaThemeSwitcher" /> dropdown.
/// </summary>
/// <param name="Name">Display name for the theme.</param>
/// <param name="Theme">The theme instance to apply when selected.</param>
/// <param name="Description">Optional description shown below the theme name.</param>
public sealed record MokaThemeSwitcherItem(
	string Name,
	MokaTheme Theme,
	string? Description = null);
