using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// The title reads the heading token, which MokaTheme.WithTitleSize sets. A narrow header used to set the xxl step
// outright, which would grow a compact title, so the last word is the smaller of the two.
public class MokaPageHeaderTitleSizeTests
{
	private const string File = "MokaPageHeader.razor.css";

	[Fact]
	public void TheTitle_ReadsTheHeadingSize()
	{
		IReadOnlyList<string> sizes = ScopedCss.Values(File, ".moka-page-header__title", "font-size");

		Assert.Equal("var(--moka-font-size-heading)", sizes[0]);
	}

	[Fact]
	public void ANarrowHeader_NeverGrowsTheTitle()
	{
		IReadOnlyList<string> sizes = ScopedCss.Values(File, ".moka-page-header__title", "font-size");

		Assert.Equal("min(var(--moka-font-size-heading), var(--moka-font-size-xxl))", sizes[^1]);
		Assert.Equal(sizes[^1], ScopedCss.Values(File, ".moka-page-header__title-row ::deep h1", "font-size")[^1]);
	}
}
