using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Utilities;

// A link to part of a page landed at the very top of the window, under a sticky app bar. What sticks to the window's
// edges publishes how much it covers, and the page's scroll padding follows it.
public class ScrollPaddingTests
{
	[Fact]
	public void TheTopPadding_IsWhatTheAppBarCovers()
	{
		Assert.Equal("var(--moka-appbar-space, auto)", ScopedCss.Declarations("moka.css", "html")["scroll-padding-top"]);
	}

	[Fact]
	public void TheBottomPadding_IsWhatTheActionBarCovers()
	{
		Assert.Equal("var(--moka-action-bar-space, auto)", ScopedCss.Declarations("moka.css", "html")["scroll-padding-bottom"]);
	}

	// On :root it would count as a theme token, which every theme then has to write.
	[Fact]
	public void ItIsNotAThemeToken()
	{
		Assert.DoesNotContain(ScopedCss.Declarations("moka.css", ":root").Keys, key => key.StartsWith("scroll-padding", StringComparison.Ordinal));
	}
}
