using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Diagnostics.Components.Panels;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Tests.Components;

public class RenderTrackerPanelTests : BunitContext
{
	public RenderTrackerPanelTests()
	{
		Services.AddMokaDiagnostics();
	}

	[Fact]
	public void Renders_EmptyState_WhenNoData()
	{
		IRenderedComponent<RenderTrackerPanel> cut = Render<RenderTrackerPanel>();

		IElement empty = cut.Find(".moka-diag-render-empty");
		Assert.NotNull(empty);
		Assert.Contains("No render data yet", empty.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void Shows_TrackedCount_AfterRecordingRenders()
	{
		// Record renders BEFORE rendering the panel so OnInitialized sees data
		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		service.RecordRender("MokaButton", "btn-1", TimeSpan.FromMilliseconds(5));
		service.RecordRender("MokaCard", "card-1", TimeSpan.FromMilliseconds(3));

		IRenderedComponent<RenderTrackerPanel> cut = Render<RenderTrackerPanel>();

		IReadOnlyList<IElement> stats = cut.FindAll(".moka-diag-render-stat-value");
		// First stat value is the tracked count
		Assert.Contains(stats, s => s.TextContent == "2");
	}

	[Fact]
	public void Shows_ComponentName_InTable()
	{
		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		service.RecordRender("MokaButton", "btn-1", TimeSpan.FromMilliseconds(5));

		IRenderedComponent<RenderTrackerPanel> cut = Render<RenderTrackerPanel>();

		IElement typeCell =
			cut.Find(".moka-diag-render-col-type:not(.moka-diag-render-header-row .moka-diag-render-col-type)");
		// The component type names appear in the table rows
		Assert.Contains("MokaButton", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void ClearButton_ResetsData()
	{
		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		service.RecordRender("MokaButton", "btn-1", TimeSpan.FromMilliseconds(5));

		IRenderedComponent<RenderTrackerPanel> cut = Render<RenderTrackerPanel>();

		IElement clearButton = cut.Find(".moka-diag-render-clear");
		clearButton.Click();

		IElement empty = cut.Find(".moka-diag-render-empty");
		Assert.NotNull(empty);
	}
}
