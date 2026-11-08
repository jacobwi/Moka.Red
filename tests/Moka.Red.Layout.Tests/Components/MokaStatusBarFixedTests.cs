using Bunit;
using Moka.Red.Layout.StatusBar;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// The bar was always fixed to the bottom of the window, so a status row under a panel (MokaArk's file explorer: item
// count, selection size, transfers) escaped to the window's edge and covered the app shell's account card.
public class MokaStatusBarFixedTests : BunitContext
{
	private const string Css = "MokaStatusBar.razor.css";

	public MokaStatusBarFixedTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ByDefault_TheBarIsFixedToTheWindow()
	{
		IRenderedComponent<MokaStatusBar> cut = Render<MokaStatusBar>();

		Assert.Equal("moka-statusbar moka-statusbar--bordered", cut.Find("footer").ClassName);
		Assert.Equal("fixed", ScopedCss.Declarations(Css, ".moka-statusbar")["position"]);
	}

	[Fact]
	public void NotFixed_LaysTheBarOutInItsContainer()
	{
		IRenderedComponent<MokaStatusBar> cut = Render<MokaStatusBar>(p => p.Add(x => x.Fixed, false));

		Assert.Contains("moka-statusbar--inline", cut.Find("footer").ClassList);
		IReadOnlyDictionary<string, string> inline = ScopedCss.Declarations(Css, ".moka-statusbar--inline");
		Assert.Equal("relative", inline["position"]);
		Assert.Equal("auto", inline["bottom"]);
		Assert.Equal("auto", inline["z-index"]);
		Assert.Equal("100%", inline["width"]);
	}
}
