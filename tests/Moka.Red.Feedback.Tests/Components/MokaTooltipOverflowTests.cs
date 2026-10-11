using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// A hidden tooltip was visibility: hidden, which still takes part in layout. Centred under the
// right-most button of a top bar it reached past the window, so the page itself became scrollable
// sideways and a focus or scrollIntoView shifted the whole layout.
public class MokaTooltipOverflowTests : BunitContext
{
	private const string Css = "MokaTooltip.razor.css";

	[Fact]
	public void AHiddenTooltipTakesNoLayout()
	{
		IReadOnlyDictionary<string, string> popup = ScopedCss.Declarations(Css, ".moka-tooltip-popup");

		Assert.Equal("none", popup["display"]);
		// visibility: hidden is what kept it in the layout, so it must not come back.
		Assert.False(popup.ContainsKey("visibility"));
	}

	[Fact]
	public void AShownTooltipIsDrawn()
	{
		const string Shown = ".moka-tooltip-trigger:hover .moka-tooltip-popup";

		Assert.Equal("block", ScopedCss.Declarations(Css, Shown)["display"]);
		// Two opacities for this selector: the rule settles on 1, and the @starting-style block gives
		// the value the fade starts from.
		Assert.Equal(["1", "0"], ScopedCss.Values(Css, Shown, "opacity"));
	}

	// display is discrete, so the fade needs allow-discrete to survive the switch.
	[Fact]
	public void TheFadeSurvivesTheDisplaySwitch() =>
		Assert.Contains(
			"allow-discrete",
			ScopedCss.Declarations(Css, ".moka-tooltip-popup")["transition"],
			StringComparison.Ordinal);

	// Edge placements, so a trigger at the window's edge can line the popup up instead of centring it.
	[Theory]
	[InlineData(MokaTooltipPosition.TopStart, "moka-tooltip-popup--top-start")]
	[InlineData(MokaTooltipPosition.TopEnd, "moka-tooltip-popup--top-end")]
	[InlineData(MokaTooltipPosition.BottomStart, "moka-tooltip-popup--bottom-start")]
	[InlineData(MokaTooltipPosition.BottomEnd, "moka-tooltip-popup--bottom-end")]
	public void AnEdgePlacementGetsItsOwnClass(MokaTooltipPosition position, string expected)
	{
		IRenderedComponent<MokaTooltip> cut = Render<MokaTooltip>(p => p
			.Add(x => x.Text, "Settings")
			.Add(x => x.Position, position));

		Assert.Contains(expected, cut.Find(".moka-tooltip-popup").ClassList);
	}

	[Theory]
	[InlineData(".moka-tooltip-popup--top-end", "right")]
	[InlineData(".moka-tooltip-popup--bottom-end", "right")]
	[InlineData(".moka-tooltip-popup--top-start", "left")]
	[InlineData(".moka-tooltip-popup--bottom-start", "left")]
	public void AnEdgePlacementLinesUpWithTheTrigger(string selector, string edge)
	{
		IReadOnlyDictionary<string, string> rules = ScopedCss.Declarations(Css, selector);

		Assert.Equal("0", rules[edge]);
		// No centring transform: that is what pushed the popup past the window.
		Assert.False(rules.ContainsKey("transform"));
	}
}
