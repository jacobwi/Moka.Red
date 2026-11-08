using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.Drawer;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// The drawer was a dialog with no name, its close button said "Close drawer" whatever the app's language, and its
// stylesheet hard-coded its durations, its borders and a faded close button.
public class MokaDrawerNameTests : BunitContext
{
	private const string Stylesheet = "MokaDrawer.razor.css";

	public MokaDrawerNameTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<MokaDrawer> Drawer(string? title = null, string? ariaLabel = null) =>
		Render<MokaDrawer>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Title, title)
			.Add(x => x.AriaLabel, ariaLabel));

	[Fact]
	public void TheTitle_NamesTheDrawer()
	{
		IRenderedComponent<MokaDrawer> cut = Drawer("Filters");

		IElement drawer = cut.Find("[role=dialog]");
		IElement title = cut.Find(".moka-drawer-title");
		Assert.False(string.IsNullOrEmpty(title.Id));
		Assert.Equal(title.Id, drawer.GetAttribute("aria-labelledby"));
		Assert.False(drawer.HasAttribute("aria-label"));
	}

	[Fact]
	public void WithoutATitle_AriaLabelNamesTheDrawer()
	{
		IElement drawer = Drawer(ariaLabel: "Filters").Find("[role=dialog]");

		Assert.Equal("Filters", drawer.GetAttribute("aria-label"));
		Assert.False(drawer.HasAttribute("aria-labelledby"));
	}

	[Fact]
	public void WithATitle_TheTitleWins_OverAriaLabel()
	{
		IElement drawer = Drawer("Filters", "Something else").Find("[role=dialog]");

		Assert.False(drawer.HasAttribute("aria-label"));
		Assert.True(drawer.HasAttribute("aria-labelledby"));
	}

	[Fact]
	public void WithNeither_TheDrawerCarriesNoName()
	{
		IElement drawer = Drawer().Find("[role=dialog]");

		Assert.False(drawer.HasAttribute("aria-label"));
		Assert.False(drawer.HasAttribute("aria-labelledby"));
	}

	[Fact]
	public void AnAppsOwnAriaLabel_StillReachesTheDrawer()
	{
		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p
			.Add(x => x.Open, true)
			.AddUnmatched("aria-label", "Filters"));

		Assert.Equal("Filters", cut.Find("[role=dialog]").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheCloseButton_IsCalledClose_ByDefault()
	{
		IconTooltip.AssertNamed(Drawer("Filters").Find(".moka-drawer-close"), "Close", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public void CloseLabel_NamesTheCloseButton_AndItsTooltip()
	{
		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.CloseLabel, "Schliessen"));

		IconTooltip.AssertNamed(cut.Find(".moka-drawer-close"), "Schliessen", MokaTooltipPosition.BottomEnd);
	}

	[Theory]
	[InlineData(".moka-drawer--left", "moka-drawer-slide-left var(--moka-transition-slow)")]
	[InlineData(".moka-drawer--right", "moka-drawer-slide-right var(--moka-transition-slow)")]
	[InlineData(".moka-drawer--top", "moka-drawer-slide-top var(--moka-transition-slow)")]
	[InlineData(".moka-drawer--bottom", "moka-drawer-slide-bottom var(--moka-transition-slow)")]
	[InlineData(".moka-drawer-backdrop", "moka-drawer-backdrop-in var(--moka-transition-fast)")]
	public void TheAnimations_TakeTheirTimingFromTheTokens_AndStopForReducedMotion(string selector, string animation)
	{
		IReadOnlyList<string> animations = ScopedCss.Values(Stylesheet, selector, "animation");

		Assert.Equal([animation, "none"], animations);
		Assert.Contains("@media (prefers-reduced-motion: reduce)", StylesheetText(), StringComparison.Ordinal);
	}

	[Fact]
	public void TheCloseButton_IsMuted_AndLightsUpOnHover()
	{
		IReadOnlyDictionary<string, string> close = ScopedCss.Declarations(Stylesheet, ".moka-drawer-close");
		IReadOnlyDictionary<string, string> hover = ScopedCss.Declarations(Stylesheet, ".moka-drawer-close:hover");

		Assert.Equal("var(--moka-color-on-surface-variant)", close["color"]);
		Assert.Equal("var(--moka-color-on-surface)", hover["color"]);
		Assert.False(close.ContainsKey("opacity"));
		Assert.False(hover.ContainsKey("opacity"));
		Assert.False(ScopedCss.Declarations(Stylesheet, ".moka-gate .moka-drawer-close").ContainsKey("opacity"));
	}

	[Theory]
	[InlineData(".moka-drawer-header", "border-bottom", "var(--moka-border-width) solid var(--moka-color-outline-variant)")]
	[InlineData(".moka-gate .moka-drawer--left", "border-right", "var(--moka-border-width) solid var(--moka-color-outline-strong)")]
	[InlineData(".moka-gate .moka-drawer--right", "border-left", "var(--moka-border-width) solid var(--moka-color-outline-strong)")]
	[InlineData(".moka-gate .moka-drawer--top", "border-bottom", "var(--moka-border-width) solid var(--moka-color-outline-strong)")]
	[InlineData(".moka-gate .moka-drawer--bottom", "border-top", "var(--moka-border-width) solid var(--moka-color-outline-strong)")]
	public void TheBorders_ReadTheBorderWidthToken(string selector, string property, string value)
	{
		Assert.Equal(value, ScopedCss.Declarations(Stylesheet, selector)[property]);
	}

	[Fact]
	public void TheStylesheet_HardCodesNoDurationOrBorderWidth()
	{
		string css = StylesheetText();

		Assert.DoesNotContain("1px", css, StringComparison.Ordinal);
		Assert.DoesNotContain("0.25s", css, StringComparison.Ordinal);
		Assert.DoesNotContain("0.15s", css, StringComparison.Ordinal);
	}

	private static string StylesheetText() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Stylesheet));
}
