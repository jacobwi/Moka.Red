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

	// :focus-within also matched a button that kept focus after a mouse click, so the tooltip stayed up once the mouse had
	// gone and came back whenever a menu or dialog handed focus back to its button. Only keyboard focus shows it now.
	[Fact]
	public void KeyboardFocusShowsItAndAClickDoesNot()
	{
		const string Focused = ".moka-tooltip-trigger:has(:focus-visible) .moka-tooltip-popup";

		Assert.Equal("block", ScopedCss.Declarations(Css, Focused)["display"]);
		Assert.Equal(["1", "0"], ScopedCss.Values(Css, Focused, "opacity"));
		Assert.Empty(ScopedCss.Declarations(Css, ".moka-tooltip-trigger:focus-within .moka-tooltip-popup"));
	}

	// The delay sat on the popup itself, so leaving a button waited as long as hovering it did and the tooltip
	// trailed the mouse onto the next button.
	[Fact]
	public void OnlyShowingWaits()
	{
		Assert.Equal("0s", ScopedCss.Declarations(Css, ".moka-tooltip-popup")["transition-delay"]);
		Assert.Equal(
			"var(--moka-tooltip-delay, 300ms)",
			ScopedCss.Declarations(Css, ".moka-tooltip-trigger:hover .moka-tooltip-popup")["transition-delay"]);
	}

	// Placed against the window, a panel that clips its overflow cannot cut the popup off.
	[Fact]
	public void WithAnchorPositioningThePopupLeavesItsContainer()
	{
		IReadOnlyDictionary<string, string> popup = ScopedCss.Declarations(Css, ".moka-tooltip-popup");

		Assert.Equal("fixed", popup["position"]);
		Assert.Equal("none", popup["transform"]);
	}

	// Each placement asks for its side first and names where to go when that side has no room.
	[Theory]
	[InlineData(".moka-tooltip-popup--top", "top", "bottom")]
	[InlineData(".moka-tooltip-popup--bottom", "bottom", "top")]
	[InlineData(".moka-tooltip-popup--left", "left", "flip-inline")]
	[InlineData(".moka-tooltip-popup--right", "right", "flip-inline")]
	[InlineData(".moka-tooltip-popup--top-start", "top span-right", "bottom span-right")]
	[InlineData(".moka-tooltip-popup--top-end", "top span-left", "bottom span-left")]
	[InlineData(".moka-tooltip-popup--bottom-start", "bottom span-right", "top span-right")]
	[InlineData(".moka-tooltip-popup--bottom-end", "bottom span-left", "top span-left")]
	public void APlacementFlipsWhereItHasNoRoom(string selector, string area, string fallback)
	{
		IReadOnlyDictionary<string, string> rules = ScopedCss.Declarations(Css, selector);

		Assert.Equal(area, rules["position-area"]);
		Assert.Contains(
			fallback,
			rules["position-try-fallbacks"].Split(',').Select(option => option.Trim()));
	}

	// The inverted on-surface box read as a white block in a dark title bar. It is a raised surface now, and each colour
	// is a variable an app can set.
	[Fact]
	public void ThePopupIsARaisedSurfaceAnAppCanRetune()
	{
		IReadOnlyDictionary<string, string> popup = ScopedCss.Declarations(Css, ".moka-tooltip-popup");

		Assert.Equal("var(--moka-tooltip-bg, var(--moka-color-surface-3))", popup["background"]);
		Assert.Equal("var(--moka-tooltip-color, var(--moka-color-on-surface))", popup["color"]);
		Assert.Equal("1px solid var(--moka-tooltip-border, var(--moka-color-outline))", popup["border"]);
		Assert.Equal("var(--moka-shadow-popup)", popup["box-shadow"]);
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
