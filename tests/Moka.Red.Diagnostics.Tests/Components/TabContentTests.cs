using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Diagnostics.Components.Panels;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Services;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Diagnostics.Tests.Components;

// The Network and Perf tabs both listed the JS interop calls, and the Services and Memory tabs both showed the GC
// figures, with the Services and Perf tabs both counting active and disposed components. Each figure has one tab now,
// and the tabs that lost one say where it went.
public sealed class TabContentTests : DevelopmentContext
{
	private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);
	private readonly FakeClock _clock = new();

	public TabContentTests()
	{
		Services.AddMokaDiagnostics();
		Services.AddSingleton<TimeProvider>(_clock);
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void PerfTab_ShowsTheComponentLifecycle_AndPointsToTheOtherTabs()
	{
		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		service.RecordRender("MokaButton", "button-1", TimeSpan.FromMilliseconds(4));
		service.RecordRender("MokaCard", "card-1", TimeSpan.FromMilliseconds(2));
		service.RecordDisposal("MokaCard", "card-1");
		service.RecordJsInteropCall("bindThing", TimeSpan.FromMilliseconds(2));

		IRenderedComponent<PerformancePanel> cut = Render<PerformancePanel>();

		Assert.Equal(["Active", "Disposed", "Avg Render", "Trend"],
			cut.FindAll(".moka-diag-perf-lifecycle-label").Select(label => label.TextContent).ToArray());
		Assert.Equal(["1", "1", "3.0 ms", "Stable"],
			cut.FindAll(".moka-diag-perf-lifecycle-value").Select(value => value.TextContent.Trim()).ToArray());
		Assert.DoesNotContain("bindThing", cut.Markup, StringComparison.Ordinal);
		Assert.Contains("Network tab", cut.Find(".moka-diag-perf-note").TextContent, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll("button"));
	}

	[Fact]
	public void ServicesTab_ListsTheServicesOnly_AndRendersOnce()
	{
		IRenderedComponent<ServicesPanel> cut = Render<ServicesPanel>();
		int renders = cut.RenderCount;

		Assert.Equal(["DiagnosticsService", "ToastService", "DialogService"],
			cut.FindAll(".moka-diag-svc-label").Select(label => label.TextContent).ToArray());
		Assert.DoesNotContain("GC Gen", cut.Markup, StringComparison.Ordinal);
		Assert.DoesNotContain("Allocated", cut.Markup, StringComparison.Ordinal);
		Assert.Contains("Memory tab", cut.Find(".moka-diag-svc-note").TextContent, StringComparison.Ordinal);

		// Registrations don't change while the app runs, so nothing refreshes it.
		_clock.Advance(Second * 3);
		Assert.Equal(renders, cut.RenderCount);
		Assert.Empty(_clock.Timers);
	}

	[Fact]
	public async Task NetworkTab_ClearsItsOwnCalls()
	{
		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		service.RecordJsInteropCall("bindThing", TimeSpan.FromMilliseconds(2));
		service.RecordRender("MokaButton", "button-1", TimeSpan.FromMilliseconds(4));
		service.RecordDisposal("MokaButton", "button-1");
		IRenderedComponent<NetworkPanel> cut = Render<NetworkPanel>();
		Assert.NotEmpty(cut.FindAll("[role=row] [role=cell]"));

		await cut.Find(".moka-diag-network-clear").ClickAsync(new MouseEventArgs());

		Assert.Empty(service.GetJsInteropEntries());
		Assert.Empty(cut.FindAll("[role=cell]"));
		Assert.Equal(1, service.DisposedComponentCount);
	}
}
