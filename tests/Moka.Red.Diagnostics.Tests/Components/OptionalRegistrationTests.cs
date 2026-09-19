using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Diagnostics.Components;
using Moka.Red.Diagnostics.Components.Panels;
using Moka.Red.Diagnostics.Pages;
using Moka.Red.Diagnostics.Tests.Base;

namespace Moka.Red.Diagnostics.Tests.Components;

// Without AddMokaDiagnostics() these components threw "There is no registered service of type
// IMokaDiagnosticsService": [Inject] throws for a missing service even on a nullable property, and
// DiagnosticComponentBase promised to skip tracking silently.
public class OptionalRegistrationTests : BunitContext
{
	public OptionalRegistrationTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	public static TheoryData<Type> Panels =>
	[
		typeof(ConsolePanel), typeof(ThemeInspectorPanel), typeof(RenderTrackerPanel), typeof(ComponentTreePanel),
		typeof(NetworkPanel), typeof(PerformancePanel), typeof(ServicesPanel), typeof(EventLogPanel),
		typeof(SettingsPanel)
	];

	[Fact]
	public void Overlay_RendersNothing()
	{
		IRenderedComponent<MokaDiagnosticsOverlay> cut = Render<MokaDiagnosticsOverlay>();

		Assert.Empty(cut.Markup.Trim());
	}

	[Fact]
	public async Task TrackedComponent_RendersReRendersAndDisposes()
	{
		IRenderedComponent<DiagnosticComponentBaseTests.TestDiagComponent> cut =
			Render<DiagnosticComponentBaseTests.TestDiagComponent>(p => p.Add(x => x.Class, "first"));

		cut.Render(p => p.Add(x => x.Class, "second"));
		await cut.Instance.DisposeAsync();

		Assert.Contains("second", cut.Find("div").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Page_SaysDiagnosticsAreNotRegistered()
	{
		IRenderedComponent<MokaDiagnosticsPage> cut = Render<MokaDiagnosticsPage>();

		Assert.Contains("AddMokaDiagnostics()", cut.Find(".moka-diag-page-notice").TextContent, StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(Panels))]
	public void Panel_RendersWithoutThrowing(Type panel)
	{
		RenderFragment fragment = builder =>
		{
			builder.OpenComponent(0, panel);
			builder.CloseComponent();
		};

		Assert.Null(Record.Exception(() => Render(fragment)));
	}
}
