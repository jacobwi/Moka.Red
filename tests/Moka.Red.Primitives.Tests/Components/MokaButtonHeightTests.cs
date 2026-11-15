using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// MokaTheme.WithControlHeights(Matched) sets --moka-matched-height-* on the provider's root; elsewhere they are invalid,
// so a button keeps the button heights it always had.
public class MokaButtonHeightTests
{
	private const string Css = "MokaButton.razor.css";

	[Theory]
	[InlineData(".moka-gate .moka-btn--xs", "var(--moka-matched-height-xs, calc(var(--moka-button-height-sm) - var(--moka-spacing-sm)))")]
	[InlineData(".moka-gate .moka-btn--sm", "var(--moka-matched-height-sm, var(--moka-button-height-sm))")]
	[InlineData(".moka-gate .moka-btn--md", "var(--moka-matched-height, var(--moka-button-height))")]
	[InlineData(".moka-gate .moka-btn--lg", "var(--moka-matched-height-lg, var(--moka-button-height-lg))")]
	public void EachSize_TakesTheMatchedHeight_ElseItsOwn(string selector, string height) =>
		Assert.Equal(height, ScopedCss.Declarations(Css, selector)["--_btn-height"]);

	[Fact]
	public void TheHeight_IsTheMinimumHeight_AndAnIconOnlyButtonsWidth()
	{
		Assert.Equal("var(--_btn-height)",
			ScopedCss.Declarations(Css, ".moka-gate :is(.moka-btn--xs, .moka-btn--sm, .moka-btn--md, .moka-btn--lg)")["min-height"]);
		Assert.Equal("var(--_btn-height)",
			ScopedCss.Declarations(Css, ".moka-gate .moka-btn--icon-only:is(.moka-btn--xs, .moka-btn--sm, .moka-btn--md, .moka-btn--lg)")["min-width"]);
	}
}
