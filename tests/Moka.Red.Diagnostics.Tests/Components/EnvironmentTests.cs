using Bunit;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moka.Red.Diagnostics.Components;
using Moka.Red.Diagnostics.Components.Panels;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Pages;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Tests.Components;

// The Console tab shows the app's logs, every user's on Blazor Server, and the overlay rendered wherever it was placed,
// so an app that left it in its layout showed them in production. Like ASP.NET Core's developer exception page, the
// diagnostics now run only in the Development environment unless the app turns them on elsewhere.
public sealed class EnvironmentTests : BunitContext
{
	public EnvironmentTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	public static TheoryData<Type> Panels =>
	[
		typeof(ConsolePanel), typeof(RenderTrackerPanel), typeof(ComponentTreePanel), typeof(NetworkPanel),
		typeof(MemoryPanel), typeof(PerformancePanel), typeof(ServicesPanel), typeof(EventLogPanel), typeof(SettingsPanel)
	];

	[Theory]
	[InlineData("Production")]
	[InlineData("Staging")]
	public void OutsideDevelopment_TheOverlayAndThePageRenderNothing(string environment)
	{
		Services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(environment));
		Services.AddMokaDiagnostics(options => options.StartExpanded = true);

		Assert.Empty(Render<MokaDiagnosticsOverlay>().Markup.Trim());
		Assert.Empty(Render<MokaDiagnosticsPage>().Markup.Trim());
		Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "import");
	}

	[Theory]
	[MemberData(nameof(Panels))]
	public void OutsideDevelopment_APanelPlacedOnItsOwnRendersNothing(Type panel)
	{
		Services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Production"));
		Services.AddMokaDiagnostics();

		Assert.Empty(Render(builder =>
		{
			builder.OpenComponent(0, panel);
			builder.CloseComponent();
		}).Markup.Trim());
	}

	// Nothing is shown, so nothing is kept: no renders, calls or events per circuit, and none of the server's logs.
	[Fact]
	public void OutsideDevelopment_NothingIsRecordedOrCaptured()
	{
		var services = new ServiceCollection();
		services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Production"));
		services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Trace));
		services.AddMokaDiagnostics();
		using ServiceProvider provider = services.BuildServiceProvider();
		using IServiceScope scope = provider.CreateScope();
		IMokaDiagnosticsService service = scope.ServiceProvider.GetRequiredService<IMokaDiagnosticsService>();

		service.RecordRender("MokaButton", "button-1", TimeSpan.FromMilliseconds(2));
		service.RecordJsInteropCall("import", TimeSpan.FromMilliseconds(2));
		service.RecordDisposal("MokaButton", "button-1");
		provider.GetRequiredService<ILoggerFactory>().CreateLogger("Production")
			.Log(LogLevel.Error, default, "a user's secret", null, (state, _) => state);

		Assert.Empty(service.GetRenderEntries());
		Assert.Empty(service.GetJsInteropEntries());
		Assert.Empty(service.GetRecentEvents(int.MaxValue));
		Assert.Equal(0, service.DisposedComponentCount);
		Assert.Empty(provider.GetRequiredService<MokaDiagnosticsConsoleBuffer>().GetMessages());
	}

	[Fact]
	public void OutsideDevelopment_TheAppCanTurnThemOn()
	{
		Services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Staging"));
		Services.AddMokaDiagnostics(options =>
		{
			options.StartExpanded = true;
			options.EnableOutsideDevelopment = true;
		});
		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();

		Assert.NotNull(Render<MokaDiagnosticsOverlay>().Find(".moka-diag-panel"));
		service.RecordRender("MokaButton", "button-1", TimeSpan.FromMilliseconds(2));
		Assert.Single(service.GetRenderEntries());
	}

	[Theory]
	[InlineData("Development")]
	[InlineData("development")]
	public void InDevelopment_TheOverlayRenders(string environment)
	{
		Services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(environment));
		Services.AddMokaDiagnostics(options => options.StartExpanded = true);

		Assert.NotNull(Render<MokaDiagnosticsOverlay>().Find(".moka-diag-panel"));
	}

	// A WebAssembly app registers no IHostEnvironment; its environment comes from IWebAssemblyHostEnvironment.
	[Theory]
	[InlineData("Development", true)]
	[InlineData("Production", false)]
	public void OnWebAssembly_TheHostsEnvironmentDecides(string environment, bool shown)
	{
		Services.AddSingleton<IWebAssemblyHostEnvironment>(new WebAssemblyEnvironment(environment));
		Services.AddMokaDiagnostics(options => options.StartExpanded = true);

		Assert.Equal(shown, Render<MokaDiagnosticsOverlay>().FindAll(".moka-diag-panel").Count == 1);
	}

	// A host with no environment (Blazor Hybrid) serves one user's own process, and the app's call to
	// AddMokaDiagnostics() is the switch, so nothing throws and the diagnostics run. bUnit always registers a WebAssembly
	// environment, so this host is built by hand.
	[Fact]
	public void WithNoEnvironment_RegisteringTheDiagnosticsTurnsThemOn()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddMokaDiagnostics();
		using ServiceProvider provider = services.BuildServiceProvider();
		using IServiceScope scope = provider.CreateScope();
		IMokaDiagnosticsService service = scope.ServiceProvider.GetRequiredService<IMokaDiagnosticsService>();

		service.RecordRender("MokaButton", "button-1", TimeSpan.FromMilliseconds(2));
		provider.GetRequiredService<ILoggerFactory>().CreateLogger("Hybrid")
			.Log(LogLevel.Warning, default, "captured", null, (state, _) => state);

		Assert.True(DiagnosticsServiceResolver.IsOn(scope.ServiceProvider));
		Assert.Same(service, DiagnosticsServiceResolver.Resolve(null, scope.ServiceProvider));
		Assert.Single(service.GetRenderEntries());
		Assert.Single(provider.GetRequiredService<MokaDiagnosticsConsoleBuffer>().GetMessages());
	}

	// Without AddMokaDiagnostics() there are no options to opt in with, so the environment alone decides whether the
	// panels that need no service, such as Memory with its Force GC, show.
	[Theory]
	[InlineData("Development", true)]
	[InlineData("Production", false)]
	public void WithoutRegistration_TheEnvironmentAloneDecides(string environment, bool shown)
	{
		Services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(environment));

		Assert.Equal(shown, Render<MemoryPanel>().FindAll(".moka-diag-memory-gc").Count == 1);
	}

	private sealed class WebAssemblyEnvironment(string name) : IWebAssemblyHostEnvironment
	{
		public string Environment { get; } = name;

		public string BaseAddress => "https://localhost/";
	}
}
