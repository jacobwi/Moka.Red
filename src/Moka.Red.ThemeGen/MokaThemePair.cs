using Moka.Red.Core.Theming;

namespace Moka.Red.ThemeGen;

/// <summary>
///     One design in both color schemes, the shape <see cref="MokaThemeProvider" /> takes as <c>DarkTheme</c> and
///     <c>LightTheme</c>. <see cref="MokaThemeStudio" /> edits one: seeds and style choices change both, and a pinned color
///     holds in one scheme only, since a color that reads on dark surfaces rarely reads on light ones.
/// </summary>
/// <param name="Dark">The design in its dark scheme.</param>
/// <param name="Light">The design in its light scheme.</param>
public sealed record MokaThemePair(MokaTheme Dark, MokaTheme Light)
{
	/// <summary>
	///     The pair a theme belongs to: the theme itself in its own scheme, and <see cref="MokaTheme.WithDark" /> of it in the
	///     other, which keeps its seed or preset, accent, style choices and pins.
	/// </summary>
	/// <param name="theme">Either scheme of the design.</param>
	/// <returns>The pair.</returns>
	public static MokaThemePair From(MokaTheme theme)
	{
		ArgumentNullException.ThrowIfNull(theme);
		return theme.IsDark ? new MokaThemePair(theme, theme.WithDark(false)) : new MokaThemePair(theme.WithDark(true), theme);
	}

	/// <summary>The design in one scheme.</summary>
	/// <param name="dark">Whether to take the dark scheme.</param>
	/// <returns><see cref="Dark" /> or <see cref="Light" />.</returns>
	public MokaTheme For(bool dark) => dark ? Dark : Light;

	/// <summary>A copy with one scheme replaced.</summary>
	/// <param name="dark">Whether to replace the dark scheme.</param>
	/// <param name="theme">The new theme for that scheme.</param>
	/// <returns>The new pair.</returns>
	public MokaThemePair With(bool dark, MokaTheme theme) => dark ? this with { Dark = theme } : this with { Light = theme };

	/// <summary>A copy with the same change made to both schemes, such as <c>pair.Select(t =&gt; t.WithDensity(MokaDensity.Compact))</c>.</summary>
	/// <param name="change">The change.</param>
	/// <returns>The new pair.</returns>
	public MokaThemePair Select(Func<MokaTheme, MokaTheme> change)
	{
		ArgumentNullException.ThrowIfNull(change);
		return new MokaThemePair(change(Dark), change(Light));
	}
}
