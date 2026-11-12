using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Charts;

// A chart's own tooltip and pin card take the theme's tooltip look on the new design, its colors, border, corners,
// shadow, blur and font, and fall back to the popup look as before when the tooltip look is Standard (every variable
// initial). They don't take the arrow, motion or size: the chart places and sizes them itself.
public class ChartTooltipLookTests
{
	private const string Css = "MokaChartSurface.razor.css";

	[Theory]
	[InlineData("background", "var(--moka-tooltip-look-bg, var(--moka-popup-bg, var(--moka-color-surface-raised)))")]
	[InlineData("color", "var(--moka-tooltip-look-color, var(--moka-popup-color, var(--moka-color-on-surface)))")]
	[InlineData("border-color", "var(--moka-tooltip-look-border, var(--moka-popup-border, var(--moka-color-outline-strong)))")]
	[InlineData("border-radius", "var(--moka-tooltip-look-radius, var(--moka-popup-radius, var(--moka-radius-popup)))")]
	[InlineData("box-shadow", "var(--moka-tooltip-look-shadow, var(--moka-popup-shadow, var(--moka-shadow-popup)))")]
	[InlineData("backdrop-filter", "var(--moka-tooltip-look-blur, var(--moka-popup-blur, none))")]
	[InlineData("font-family", "var(--moka-tooltip-look-font, var(--moka-popup-font, var(--moka-font-family)))")]
	public void TheTip_ReadsTheTooltipLook_ThenThePopupLook(string property, string expected)
	{
		Assert.Equal(expected, ScopedCss.Declarations(Css, ".moka-gate .moka-chart-tip")[property]);
		Assert.Equal(expected, ScopedCss.Values(Css, ".moka-gate .moka-chart-pin", property)[0]);
	}

	[Fact]
	public void ThePin_KeepsItsStrongerBorder_UnderStandard() =>
		Assert.Equal(
			["var(--moka-tooltip-look-border, var(--moka-popup-border, var(--moka-color-outline-strong)))",
			 "var(--moka-tooltip-look-border, var(--moka-popup-border, var(--moka-color-outline-hover)))"],
			ScopedCss.Values(Css, ".moka-gate .moka-chart-pin", "border-color"));

	// Today's muted colors are the fallbacks, so a Standard tooltip look changes nothing.
	[Fact]
	public void MutedText_TakesTheLooksMutedColor()
	{
		Assert.Equal("var(--moka-tooltip-look-muted, var(--moka-color-on-surface-variant))", ScopedCss.Declarations(Css, ".moka-gate .moka-chart-tip__head")["color"]);
		Assert.Equal("var(--moka-tooltip-look-muted, var(--moka-color-on-surface-tertiary))", ScopedCss.Declarations(Css, ".moka-gate .moka-chart-tip__delta")["color"]);
		Assert.Equal("var(--moka-tooltip-look-color, var(--moka-color-on-surface))", ScopedCss.Declarations(Css, ".moka-gate .moka-chart-tip__row--focus .moka-chart-tip__name")["color"]);
	}

	[Fact]
	public void TheChartTooltip_ReadsNoArrowMotionOrSize()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css));

		Assert.DoesNotContain("--moka-tooltip-arrow", css, StringComparison.Ordinal);
		Assert.DoesNotContain("--moka-tooltip-motion", css, StringComparison.Ordinal);
		Assert.DoesNotContain("--moka-tooltip-size", css, StringComparison.Ordinal);
	}
}
