using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Diagnostics.Components;
using Moka.Red.Diagnostics.Components.Panels;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Pages;

namespace Moka.Red.Diagnostics.Tests.Components;

// The tabs were buttons with aria-pressed, ten tab stops in a row, and nothing tied a tab to the panel it showed. They
// follow the WAI-ARIA tabs pattern now: a tablist of tabs, each controlling the tabpanel it labels when selected, with
// one tab stop on the selected tab and the arrow keys, run by the script, for the rest.
public sealed class TabsPatternTests : DevelopmentContext
{
	private const string ModulePath = "./_content/Moka.Red.Diagnostics/moka-diagnostics.js";

	public TabsPatternTests()
	{
		Services.AddMokaDiagnostics(options => options.StartExpanded = true);
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public async Task OverlayTabs_FollowTheTabsPattern()
	{
		IRenderedComponent<MokaDiagnosticsOverlay> cut = Render<MokaDiagnosticsOverlay>();

		AssertTabsPattern(cut.Find(".moka-diag-tabs"), cut.Find(".moka-diag-body"), "Console");

		await Tab(cut.Find(".moka-diag-tabs"), "Network").ClickAsync(new MouseEventArgs());

		AssertTabsPattern(cut.Find(".moka-diag-tabs"), cut.Find(".moka-diag-body"), "Network");
		Assert.NotEmpty(cut.FindComponents<NetworkPanel>());
	}

	[Fact]
	public async Task PageTabs_FollowTheTabsPattern()
	{
		IRenderedComponent<MokaDiagnosticsPage> page = Render<MokaDiagnosticsPage>();

		AssertTabsPattern(page.Find(".moka-diag-page-tabs"), page.Find(".moka-diag-page-content"), "Console");

		await Tab(page.Find(".moka-diag-page-tabs"), "Performance").ClickAsync(new MouseEventArgs());

		AssertTabsPattern(page.Find(".moka-diag-page-tabs"), page.Find(".moka-diag-page-content"), "Performance");
		Assert.NotEmpty(page.FindComponents<PerformancePanel>());
	}

	// The page's tab list gets the same arrow keys as the overlay's row, from the same script, once it is interactive.
	[Fact]
	public void PageTabList_IsGivenToTheScript()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
		module.Mode = JSRuntimeMode.Loose;

		IRenderedComponent<MokaDiagnosticsPage> page = Render<MokaDiagnosticsPage>();

		JSRuntimeInvocation bind = Assert.Single(module.Invocations, i => i.Identifier == "bindTabList");
		Assert.Equal(page.Find("[role=tablist]").GetAttribute("blazor:elementReference"),
			Assert.IsType<ElementReference>(bind.Arguments[0]).Id);
	}

	[Fact]
	public void TwoOverlays_UseDifferentIds()
	{
		IRenderedComponent<MokaDiagnosticsOverlay> first = Render<MokaDiagnosticsOverlay>();
		IRenderedComponent<MokaDiagnosticsOverlay> second = Render<MokaDiagnosticsOverlay>();

		Assert.NotEqual(first.Find("[role=tabpanel]").Id, second.Find("[role=tabpanel]").Id);
	}

	private static void AssertTabsPattern(IElement list, IElement panel, string selected)
	{
		Assert.Equal("tablist", list.GetAttribute("role"));
		Assert.Equal("Diagnostics", list.GetAttribute("aria-label"));

		List<IElement> tabs = list.QuerySelectorAll("[role=tab]").ToList();
		Assert.Equal(10, tabs.Count);
		Assert.Equal(tabs.Count, tabs.Select(tab => tab.Id).Distinct().Count());
		Assert.Empty(list.QuerySelectorAll("[aria-pressed]"));

		IElement current = Tab(list, selected);
		Assert.All(tabs, tab =>
		{
			bool isCurrent = ReferenceEquals(tab, current);
			Assert.Equal(isCurrent ? "true" : "false", tab.GetAttribute("aria-selected"));
			Assert.Equal(isCurrent ? "0" : "-1", tab.GetAttribute("tabindex"));
			Assert.Equal(panel.Id, tab.GetAttribute("aria-controls"));
			Assert.Equal("button", tab.GetAttribute("type"));
		});

		Assert.Equal("tabpanel", panel.GetAttribute("role"));
		Assert.Equal(current.Id, panel.GetAttribute("aria-labelledby"));
		Assert.Equal("0", panel.GetAttribute("tabindex"));
	}

	private static IElement Tab(IElement list, string name) =>
		list.QuerySelectorAll("[role=tab]").Single(tab => (tab.GetAttribute("aria-label") ?? tab.TextContent.Trim()) == name);
}
