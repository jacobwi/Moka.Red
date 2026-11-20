using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Diagnostics.Components;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Services;
using Moka.Red.Primitives.Tooltip;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Diagnostics.Tests.Components;

// The badge and the panel replace each other, so a keyboard user who opened the panel from the badge, or closed it
// from its close button, was left with focus on the page body. Escape didn't close it, and the header's icon buttons
// and the gear tab had no names a screen reader would read.
public sealed class OverlayKeyboardTests : DevelopmentContext
{
	private const string FocusIdentifier = "Blazor._internal.domWrapper.focus";

	public OverlayKeyboardTests()
	{
		Services.AddMokaDiagnostics();
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public async Task OpeningFromTheBadge_FocusesThePanel_AndClosingFocusesTheBadge()
	{
		IRenderedComponent<MokaDiagnosticsOverlay> cut = Render<MokaDiagnosticsOverlay>();

		await cut.Find(".moka-diag-badge").ClickAsync(new MouseEventArgs());
		Assert.Equal(ReferenceOf(cut.Find(".moka-diag-panel")), LastFocused());

		await cut.Find(".moka-diag-close").ClickAsync(new MouseEventArgs());
		Assert.Equal(ReferenceOf(cut.Find(".moka-diag-badge")), LastFocused());
	}

	[Fact]
	public async Task CloseFromJs_ClosesThePanel_AndFocusesTheBadge()
	{
		IRenderedComponent<MokaDiagnosticsOverlay> cut = Render<MokaDiagnosticsOverlay>();
		await cut.Find(".moka-diag-badge").ClickAsync(new MouseEventArgs());

		await cut.InvokeAsync(cut.Instance.CloseFromJs);

		Assert.Empty(cut.FindAll(".moka-diag-panel"));
		Assert.Equal(ReferenceOf(cut.Find(".moka-diag-badge")), LastFocused());
		Assert.False(Services.GetRequiredService<IMokaDiagnosticsService>().IsOverlayVisible);
	}

	// The shortcut pressed outside the panel opens it without taking focus from what the user was doing.
	[Fact]
	public async Task ToggleFromJs_OpensWithoutMovingFocus()
	{
		IRenderedComponent<MokaDiagnosticsOverlay> cut = Render<MokaDiagnosticsOverlay>();

		await cut.InvokeAsync(cut.Instance.ToggleFromJs);

		Assert.NotNull(cut.Find(".moka-diag-panel"));
		Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == FocusIdentifier);
	}

	[Fact]
	public async Task IconButtons_AndTheSettingsTab_HaveNames()
	{
		IRenderedComponent<MokaDiagnosticsOverlay> cut = Render<MokaDiagnosticsOverlay>();
		Assert.Equal("Moka Diagnostics (Ctrl+Shift+D)", cut.Find(".moka-diag-badge").GetAttribute("aria-label"));
		await cut.Find(".moka-diag-badge").ClickAsync(new MouseEventArgs());

		IElement panel = cut.Find(".moka-diag-panel");
		Assert.Equal("region", panel.GetAttribute("role"));
		Assert.Equal("Moka Diagnostics", panel.GetAttribute("aria-label"));
		Assert.Equal("true", cut.Find(".moka-diag-drag-handle").GetAttribute("aria-hidden"));
		Assert.Equal(
			["Pause tracking", "Export diagnostics data", "Open in new window", "Close (or press Ctrl+Shift+D)"],
			cut.FindAll(".moka-diag-header-actions button").Select(b => b.GetAttribute("aria-label")).ToArray());

		IReadOnlyList<IElement> tabs = cut.FindAll("[role=tab]");
		Assert.Equal("Settings", tabs[^1].GetAttribute("aria-label"));
		Assert.Equal("true", tabs[0].GetAttribute("aria-selected"));
		Assert.All(tabs.Skip(1), tab => Assert.Equal("false", tab.GetAttribute("aria-selected")));

		await tabs[^1].ClickAsync(new MouseEventArgs());
		Assert.Equal("true", cut.FindAll("[role=tab]")[^1].GetAttribute("aria-selected"));
	}

	// The header's icon buttons, the gear tab, the scroll arrows and the badge carried a title, the browser's own pale
	// tooltip that turns up a second late. Each is named by aria-label and sits in a MokaTooltip showing the same name,
	// on the side its place in the row calls for.
	[Fact]
	public async Task IconOnlyControls_AreNamedByTooltips_NotTitles()
	{
		IRenderedComponent<MokaDiagnosticsOverlay> cut = Render<MokaDiagnosticsOverlay>();
		IconTooltip.AssertNamed(cut.Find(".moka-diag-badge"), "Moka Diagnostics (Ctrl+Shift+D)", MokaTooltipPosition.TopEnd);
		await cut.Find(".moka-diag-badge").ClickAsync(new MouseEventArgs());

		IReadOnlyList<IElement> actions = cut.FindAll(".moka-diag-header-actions button");
		IconTooltip.AssertNamed(actions[0], "Pause tracking", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(actions[1], "Export diagnostics data", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(actions[2], "Open in new window", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(actions[3], "Close (or press Ctrl+Shift+D)", MokaTooltipPosition.BottomEnd);
		IconTooltip.AssertNamed(cut.FindAll("[role=tab]")[^1], "Settings", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(cut.Find(".moka-diag-tabs-scroll--start"), "Scroll tabs left", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(cut.Find(".moka-diag-tabs-scroll--end"), "Scroll tabs right", MokaTooltipPosition.Bottom);
		Assert.Empty(cut.FindAll(".moka-diag-header [title]"));

		// The pause button keeps its name and tooltip in step when it turns into a resume button.
		await actions[0].ClickAsync(new MouseEventArgs());
		IconTooltip.AssertNamed(cut.FindAll(".moka-diag-header-actions button")[0], "Resume tracking", MokaTooltipPosition.Bottom);
	}

	// The badge's name opens away from the corner it sits in, whichever corner the window picked.
	[Theory]
	[InlineData(OverlayPosition.BottomRight, MokaTooltipPosition.TopEnd)]
	[InlineData(OverlayPosition.BottomLeft, MokaTooltipPosition.TopStart)]
	[InlineData(OverlayPosition.TopRight, MokaTooltipPosition.BottomEnd)]
	[InlineData(OverlayPosition.TopLeft, MokaTooltipPosition.BottomStart)]
	public void BadgeTooltip_OpensAwayFromItsCorner(OverlayPosition corner, MokaTooltipPosition side)
	{
		Services.GetRequiredService<IMokaDiagnosticsService>().Settings.Position = corner;

		IRenderedComponent<MokaDiagnosticsOverlay> cut = Render<MokaDiagnosticsOverlay>();

		IconTooltip.AssertNamed(cut.Find(".moka-diag-badge"), "Moka Diagnostics (Ctrl+Shift+D)", side);
	}

	// A transform on the fixed badge or a backdrop filter on the panel would capture the tooltips' fixed popups, placing
	// them in the badge or the panel and cutting them off at its edge. The corner is a fixed box with no transform, and
	// the panel's blur sits on a layer behind its content.
	[Fact]
	public void Stylesheet_LeavesTheTooltipsPopupsToTheWindow()
	{
		const string overlay = "MokaDiagnosticsOverlay.razor.css";
		Assert.Equal("fixed", ScopedCss.Declarations(overlay, ".moka-diag-badge-corner")["position"]);
		Assert.False(ScopedCss.Declarations(overlay, ".moka-diag-badge").ContainsKey("position"));
		Assert.False(ScopedCss.Declarations(overlay, ".moka-diag-panel").ContainsKey("backdrop-filter"));
		Assert.False(ScopedCss.Declarations(overlay, ".moka-diag-panel").ContainsKey("transform"));
		Assert.Equal("blur(8px)", ScopedCss.Declarations(overlay, ".moka-diag-panel::before")["backdrop-filter"]);
		Assert.Equal("-1", ScopedCss.Declarations(overlay, ".moka-diag-panel::before")["z-index"]);
		Assert.All([".moka-diag-panel", ".moka-diag-badge-corner"], root =>
		{
			IReadOnlyDictionary<string, string> declarations = ScopedCss.Declarations(overlay, root);
			Assert.Equal("#2a2a2a", declarations["--moka-tooltip-bg"]);
			Assert.Equal("#e0e0e0", declarations["--moka-tooltip-color"]);
			Assert.Equal("rgba(255, 255, 255, 0.15)", declarations["--moka-tooltip-border"]);
		});
	}

	// The tab row scrolled with its scrollbar hidden, so in a panel narrower than its ten tabs the last ones could only be
	// reached with the keyboard. The arrows are drawn by the overlay and run by its script: no click handler of their
	// own, so scrolling the row costs no round trip, and the script is given each new row as the panel opens.
	[Fact]
	public async Task TabRow_HasNamedScrollArrows_RunByTheScript()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Moka.Red.Diagnostics/moka-diagnostics.js");
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaDiagnosticsOverlay> cut = Render<MokaDiagnosticsOverlay>();

		await cut.Find(".moka-diag-badge").ClickAsync(new MouseEventArgs());

		IElement row = cut.Find(".moka-diag-tabs-row");
		IReadOnlyList<IElement> arrows = row.QuerySelectorAll(".moka-diag-tabs-scroll").ToList();
		Assert.Equal(["Scroll tabs left", "Scroll tabs right"], arrows.Select(a => a.GetAttribute("aria-label")).ToArray());
		Assert.Equal(["-1", "1"], arrows.Select(a => a.GetAttribute("data-moka-diag-scroll")).ToArray());
		Assert.All(arrows, arrow =>
		{
			Assert.Equal("BUTTON", arrow.TagName);
			Assert.Equal("button", arrow.GetAttribute("type"));
			Assert.False(arrow.HasAttribute("blazor:onclick"));

			// The arrow keys reach every tab and keep it in view, so the arrows are for the pointer and outside the list.
			Assert.Equal("-1", arrow.GetAttribute("tabindex"));
			Assert.Null(arrow.Closest("[role=tablist]"));
		});
		Assert.Equal(10, row.QuerySelectorAll(".moka-diag-tabs[role=tablist] [role=tab]").Length);

		JSRuntimeInvocation bind = Assert.Single(module.Invocations, i => i.Identifier == "bindTabRow");
		Assert.Equal(ReferenceOf(row), Assert.IsType<ElementReference>(bind.Arguments[0]).Id);

		await cut.Find(".moka-diag-close").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-diag-badge").ClickAsync(new MouseEventArgs());
		Assert.Equal(2, module.Invocations.Count(i => i.Identifier == "bindTabRow"));
	}

	// The arrows take their room whenever the row overflows and only turn visible per side, so revealing a tab never
	// changes the row's width under it; the panel stays inside a phone-width window; the popup page wraps its tabs.
	[Fact]
	public void Stylesheets_ScrollTheOverlayTabs_AndWrapThePageTabs()
	{
		const string overlay = "MokaDiagnosticsOverlay.razor.css";
		Assert.Equal("none", ScopedCss.Declarations(overlay, ".moka-diag-tabs-scroll")["display"]);
		Assert.Equal("hidden", ScopedCss.Declarations(overlay, ".moka-diag-tabs-scroll")["visibility"]);
		Assert.Equal("inline-flex",
			ScopedCss.Declarations(overlay, ".moka-diag-tabs-row[data-moka-overflow] .moka-diag-tabs-scroll")["display"]);
		Assert.Equal("visible",
			ScopedCss.Declarations(overlay, ".moka-diag-tabs-row[data-moka-more-start] .moka-diag-tabs-scroll--start")["visibility"]);
		Assert.Equal("visible",
			ScopedCss.Declarations(overlay, ".moka-diag-tabs-row[data-moka-more-end] .moka-diag-tabs-scroll--end")["visibility"]);
		Assert.Equal("auto", ScopedCss.Declarations(overlay, ".moka-diag-tabs")["overflow-x"]);
		Assert.Equal("calc(100vw - 32px)", ScopedCss.Declarations(overlay, ".moka-diag-panel")["max-width"]);

		Assert.Equal("wrap", ScopedCss.Declarations("MokaDiagnosticsPage.razor.css", ".moka-diag-page-tabs")["flex-wrap"]);
	}

	private string LastFocused() =>
		Assert.IsType<ElementReference>(JSInterop.Invocations.Last(i => i.Identifier == FocusIdentifier).Arguments[0]).Id;

	private static string? ReferenceOf(IElement element) => element.GetAttribute("blazor:elementReference");
}
