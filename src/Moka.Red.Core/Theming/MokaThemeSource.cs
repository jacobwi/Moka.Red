namespace Moka.Red.Core.Theming;

// What the base classes read Theme through. A CascadingValue<MokaTheme> notifies every subscriber whenever its
// provider renders, because Blazor can't tell a class hasn't changed, so with Theme as a cascading parameter on the
// bases any layout render re-rendered every Moka component on the page: 500 of 500 in a measurement, for a value
// almost none of them read, since a theme reaches the page through CSS variables. This cascades as fixed, so
// reading it subscribes to nothing.
internal sealed class MokaThemeSource(MokaTheme? theme = null)
{
	public MokaTheme? Theme { get; set; } = theme;
}
