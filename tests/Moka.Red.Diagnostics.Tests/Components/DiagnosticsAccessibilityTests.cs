using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moka.Red.Core.Theming;
using Moka.Red.Diagnostics.Components;
using Moka.Red.Diagnostics.Components.Panels;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Pages;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Tests.Components;

public class DiagnosticsAccessibilityTests : BunitContext
{
	public DiagnosticsAccessibilityTests()
	{
		Services.AddMokaDiagnostics(options => options.StartExpanded = true);
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	// The settings were plain text beside their controls, so a screen reader announced "checkbox,
	// not checked" with nothing to say what it switched.
	[Fact]
	public void SettingsControls_AreEachLabelledByTheirSetting()
	{
		IRenderedComponent<SettingsPanel> cut = Render<SettingsPanel>();

		IReadOnlyList<IElement> controls = cut.FindAll("input, select");
		Assert.Equal(8, controls.Count);
		Assert.All(controls, control =>
		{
			Assert.False(string.IsNullOrEmpty(control.Id));
			IElement label = cut.Find($"label[for='{control.Id}']");
			Assert.False(string.IsNullOrWhiteSpace(label.TextContent));
		});
		Assert.Equal("Pause Tracking", NameOf(cut, cut.FindAll("input[type=checkbox]")[1]));
	}

	[Fact]
	public void TwoSettingsPanels_UseDifferentIds()
	{
		IRenderedComponent<SettingsPanel> first = Render<SettingsPanel>();
		IRenderedComponent<SettingsPanel> second = Render<SettingsPanel>();

		Assert.NotEqual(first.Find("select").Id, second.Find("select").Id);
	}

	// A button without a type is a submit button, so inside a form every tab switch and every
	// Clear would have submitted it.
	[Fact]
	public async Task OverlayButtons_AreAllPlainButtons()
	{
		Services.GetRequiredService<MokaDiagnosticsConsoleBuffer>().Add(new ConsoleLogEntry
		{
			Timestamp = DateTime.UtcNow,
			Level = LogLevel.Error,
			Category = "Test",
			Message = "Failed",
			Exception = "System.Exception: Failed"
		});
		IRenderedComponent<MokaDiagnosticsOverlay> cut = Render<MokaDiagnosticsOverlay>();

		// Every tab in turn, since each panel renders its own buttons.
		List<IElement> buttons = [];
		int tabs = cut.FindAll(".moka-diag-tab").Count;
		Assert.Equal(10, tabs);
		for (int i = 0; i < tabs; i++)
		{
			await cut.FindAll(".moka-diag-tab")[i].ClickAsync(new MouseEventArgs());
			buttons.AddRange(cut.FindAll("button"));
		}

		Assert.Contains(buttons, b => b.ClassList.Contains("moka-diag-console-expand"));
		Assert.Contains(buttons, b => b.ClassList.Contains("moka-diag-memory-gc"));
		Assert.All(buttons, b => Assert.Equal("button", b.GetAttribute("type")));

		await cut.Find(".moka-diag-close").ClickAsync(new MouseEventArgs());
		Assert.Equal("button", cut.Find(".moka-diag-badge").GetAttribute("type"));
	}

	// The popup page offered seven of the overlay's ten tabs: Tree, Network and Memory were missing.
	[Fact]
	public async Task PopupPage_OffersEveryPanelTheOverlayHas()
	{
		IRenderedComponent<MokaDiagnosticsPage> page = Render<MokaDiagnosticsPage>();

		IReadOnlyList<IElement> tabs = page.FindAll(".moka-diag-page-tab");
		Assert.Equal(["Console", "Theme", "Render", "Tree", "Network", "Memory", "Performance", "Services", "Log", "Settings"],
			tabs.Select(tab => tab.TextContent.Trim()).ToArray());
		Assert.All(tabs, tab => Assert.Equal("button", tab.GetAttribute("type")));

		await Open(page, "Tree");
		Assert.NotEmpty(page.FindComponents<ComponentTreePanel>());
		await Open(page, "Network");
		Assert.NotEmpty(page.FindComponents<NetworkPanel>());
		await Open(page, "Memory");
		Assert.NotEmpty(page.FindComponents<MemoryPanel>());
		Assert.Equal("button", page.Find(".moka-diag-memory-gc").GetAttribute("type"));
	}

	// The sortable headers were spans with a click handler, so only a mouse could sort, and nothing
	// told a screen reader which column the list was sorted by. They are buttons in column headers
	// now, and the sorted header carries aria-sort.
	[Fact]
	public async Task NetworkSortHeaders_AreButtons_AndTheSortedColumnSaysSo()
	{
		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		service.RecordJsInteropCall("alpha", TimeSpan.FromMilliseconds(3));
		service.RecordJsInteropCall("beta", TimeSpan.FromMilliseconds(1));
		IRenderedComponent<NetworkPanel> cut = Render<NetworkPanel>();

		AssertSortableTable(cut.FindAll("[role=table] [role=columnheader]"), 5);
		Assert.Equal(2, cut.FindAll("[role=table] [role=row]").Count(row => row.QuerySelector("[role=cell]") is not null));
		Assert.Equal("descending", Header(cut.FindAll("[role=columnheader]"), "Total ms").GetAttribute("aria-sort"));

		await SortButton(cut.FindAll("[role=columnheader]"), "Calls").ClickAsync(new MouseEventArgs());
		Assert.Equal("descending", Header(cut.FindAll("[role=columnheader]"), "Calls").GetAttribute("aria-sort"));
		Assert.False(Header(cut.FindAll("[role=columnheader]"), "Total ms").HasAttribute("aria-sort"));

		await SortButton(cut.FindAll("[role=columnheader]"), "Calls").ClickAsync(new MouseEventArgs());
		Assert.Equal("ascending", Header(cut.FindAll("[role=columnheader]"), "Calls").GetAttribute("aria-sort"));
	}

	[Fact]
	public async Task RenderSortHeaders_AreButtons_AndTheSortedColumnSaysSo()
	{
		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		service.RecordRender("MokaButton", "btn-1", TimeSpan.FromMilliseconds(5));
		service.RecordRender("MokaCard", "card-1", TimeSpan.FromMilliseconds(3));
		IRenderedComponent<RenderTrackerPanel> cut = Render<RenderTrackerPanel>();

		AssertSortableTable(cut.FindAll("[role=table] [role=columnheader]"), 5);
		Assert.Equal("descending", Header(cut.FindAll("[role=columnheader]"), "Renders").GetAttribute("aria-sort"));

		await SortButton(cut.FindAll("[role=columnheader]"), "Component").ClickAsync(new MouseEventArgs());
		Assert.Equal("descending", Header(cut.FindAll("[role=columnheader]"), "Component").GetAttribute("aria-sort"));
		Assert.False(Header(cut.FindAll("[role=columnheader]"), "Renders").HasAttribute("aria-sort"));
	}

	// The token rows were divs with a click handler, so the keyboard could not copy a token, and the
	// copied message was never announced.
	[Fact]
	public void TokenRows_AreButtons_AndTheCopyIsAnnounced()
	{
		IRenderedComponent<ThemeInspectorPanel> cut = Render<ThemeInspectorPanel>(p => p.Add(x => x.Theme, MokaTheme.Dark));
		IElement status = cut.Find("[role=status]");
		Assert.Equal(string.Empty, status.TextContent.Trim());

		IElement row = TokenRow(cut, "--moka-color-primary");
		Assert.Equal("BUTTON", row.TagName);
		Assert.Equal("button", row.GetAttribute("type"));
		Assert.Equal("Copy var(--moka-color-primary)", row.GetAttribute("title"));

		row.Click();

		Assert.Equal("var(--moka-color-primary)",
			JSInterop.VerifyInvoke("navigator.clipboard.writeText").Arguments[0]);
		Assert.Equal("Copied: var(--moka-color-primary)", cut.Find("[role=status]").TextContent.Trim());
	}

	// Both filters were named only by their placeholder, which screen readers do not always read and
	// which goes away as soon as the user types.
	[Fact]
	public void FilterInputs_HaveNames()
	{
		IRenderedComponent<ConsolePanel> console = Render<ConsolePanel>();
		IRenderedComponent<ThemeInspectorPanel> theme = Render<ThemeInspectorPanel>(p => p.Add(x => x.Theme, MokaTheme.Dark));

		Assert.Equal("Filter by category", console.Find(".moka-diag-console-search").GetAttribute("aria-label"));
		Assert.Equal("Filter tokens", theme.Find(".moka-diag-search").GetAttribute("aria-label"));
	}

	private static void AssertSortableTable(IReadOnlyList<IElement> headers, int columns)
	{
		Assert.Equal(columns, headers.Count);
		Assert.All(headers, header =>
		{
			IElement? button = header.QuerySelector("button");
			Assert.NotNull(button);
			Assert.Equal("button", button.GetAttribute("type"));
			Assert.All(button.QuerySelectorAll("span"), arrow => Assert.Equal("true", arrow.GetAttribute("aria-hidden")));
		});
	}

	private static IElement Header(IReadOnlyList<IElement> headers, string text) =>
		headers.Single(header => header.TextContent.Trim().StartsWith(text, StringComparison.Ordinal));

	private static IElement SortButton(IReadOnlyList<IElement> headers, string text) =>
		Header(headers, text).QuerySelector("button")!;

	private static IElement TokenRow(IRenderedComponent<ThemeInspectorPanel> cut, string variable) =>
		cut.FindAll(".moka-diag-token-row").Single(row =>
			row.QuerySelector(".moka-diag-token-name")!.TextContent == variable);

	private static Task Open(IRenderedComponent<MokaDiagnosticsPage> page, string tab) =>
		page.FindAll(".moka-diag-page-tab").Single(t => t.TextContent.Trim() == tab).ClickAsync(new MouseEventArgs());

	private static string NameOf(IRenderedComponent<SettingsPanel> cut, IElement control) =>
		cut.Find($"label[for='{control.Id}']").TextContent.Trim();
}
