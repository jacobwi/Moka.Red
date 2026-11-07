using Moka.Red.Core.Theming;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Theming;

// --moka-main-padding is a shorthand ("28px 32px 120px" on the new design), so a page filling the main area couldn't
// calc() its height from it. The top and bottom are tokens of their own, read from the shorthand as CSS reads it.
public class MainPaddingTokenTests
{
	[Theory]
	[InlineData("28px 32px 120px", "28px", "120px")]
	[InlineData("var(--moka-spacing-xl)", "var(--moka-spacing-xl)", "var(--moka-spacing-xl)")]
	[InlineData("12px 16px", "12px", "12px")]
	[InlineData("1px 2px 3px 4px", "1px", "3px")]
	[InlineData("calc(1rem + 2px) 8px calc(100px - 1rem)", "calc(1rem + 2px)", "calc(100px - 1rem)")]
	public void TheTopAndBottom_FollowTheShorthand(string shorthand, string top, string bottom)
	{
		MokaTheme theme = MokaTheme.Default with { Spacing = MokaTheme.Default.Spacing with { MainPadding = shorthand } };

		string css = theme.ToCssVariables();

		Assert.Contains($"--moka-main-padding: {shorthand};", css, StringComparison.Ordinal);
		Assert.Contains($"--moka-main-padding-top: {top};", css, StringComparison.Ordinal);
		Assert.Contains($"--moka-main-padding-bottom: {bottom};", css, StringComparison.Ordinal);
	}

	[Fact]
	public void TheDefaultTheme_KeepsItsRoomAtTheBottom()
	{
		string css = MokaTheme.Default.ToCssVariables();

		Assert.Contains("--moka-main-padding-top: 28px;", css, StringComparison.Ordinal);
		Assert.Contains("--moka-main-padding-bottom: 120px;", css, StringComparison.Ordinal);
	}

	[Fact]
	public void APageThatFillsTheMainArea_GrowsAndCanScrollInside()
	{
		IReadOnlyDictionary<string, string> fill = ScopedCss.Declarations("moka.css", ".moka-fill-main");

		Assert.Equal("1 1 auto", fill["flex"]);
		Assert.Equal("0", fill["min-height"]);
	}
}
