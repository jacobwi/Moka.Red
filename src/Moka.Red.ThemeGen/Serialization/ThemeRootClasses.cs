using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.ThemeGen.Serialization;

// The classes the outermost MokaThemeProvider puts on its root for a theme, for the CSS export. The provider works them
// out in Core where nothing else can call it; ThemeRootClassesTests renders a provider and holds the two together.
internal static class ThemeRootClasses
{
	public static IEnumerable<string> For(MokaTheme theme)
	{
		yield return "moka-root";

		if (theme.IsDark)
		{
			yield return "moka-dark";
		}

		if (theme.Look != MokaLook.Gate)
		{
			yield break;
		}

		yield return "moka-gate";
		yield return $"moka-popups-{MokaEnumHelpers.ToCssClass(theme.Popups)}";
		yield return $"moka-buttons-{MokaEnumHelpers.ToCssClass(theme.Buttons)}";

		if (theme.DisabledButtons != MokaDisabledButtonLook.Faded)
		{
			yield return $"moka-disabled-buttons-{MokaEnumHelpers.ToCssClass(theme.DisabledButtons)}";
		}

		if (theme.ControlHeights == MokaControlHeights.Matched)
		{
			yield return "moka-control-heights-matched";
		}

		if (theme.Tooltips != MokaTooltipLook.Standard)
		{
			yield return $"moka-tooltips-{MokaEnumHelpers.ToCssClass(theme.Tooltips)}";
		}

		if (theme.TooltipArrows)
		{
			yield return "moka-tooltips-arrow";
		}

		if (theme.TooltipMotion != MokaTooltipMotion.Fade)
		{
			yield return $"moka-tooltips-motion-{MokaEnumHelpers.ToCssClass(theme.TooltipMotion)}";
		}

		if (theme.TooltipSize != MokaTooltipSize.Standard)
		{
			yield return $"moka-tooltips-size-{MokaEnumHelpers.ToCssClass(theme.TooltipSize)}";
		}
	}
}
