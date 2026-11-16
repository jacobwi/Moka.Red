using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
	[Fact]
	public void AddMokaDiagnostics_RegistersOptions()
	{
		var services = new ServiceCollection();
		services.AddMokaDiagnostics();

		ServiceProvider provider = services.BuildServiceProvider();
		DiagnosticsOptions? options = provider.GetService<DiagnosticsOptions>();

		Assert.NotNull(options);
	}

	[Fact]
	public void AddMokaDiagnostics_RegistersService()
	{
		var services = new ServiceCollection();
		services.AddMokaDiagnostics();

		ServiceProvider provider = services.BuildServiceProvider();
		using IServiceScope scope = provider.CreateScope();
		IMokaDiagnosticsService? service = scope.ServiceProvider.GetService<IMokaDiagnosticsService>();

		Assert.NotNull(service);
	}

	[Fact]
	public void AddMokaDiagnostics_WithConfigure_AppliesOptions()
	{
		var services = new ServiceCollection();
		services.AddMokaDiagnostics(opts =>
		{
			opts.KeyboardShortcut = "F12";
			opts.Position = OverlayPosition.TopLeft;
			opts.StartExpanded = true;
		});

		ServiceProvider provider = services.BuildServiceProvider();
		DiagnosticsOptions options = provider.GetRequiredService<DiagnosticsOptions>();

		Assert.Equal("F12", options.KeyboardShortcut);
		Assert.Equal(OverlayPosition.TopLeft, options.Position);
		Assert.True(options.StartExpanded);
	}

	[Fact]
	public void AddMokaDiagnostics_ServiceIsScoped()
	{
		var services = new ServiceCollection();
		services.AddMokaDiagnostics();

		ServiceDescriptor descriptor = services.First(d => d.ServiceType == typeof(IMokaDiagnosticsService));

		Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
	}

	// A second call added a second logger provider, so every log message showed twice in the Console tab.
	[Fact]
	public void AddMokaDiagnostics_CalledTwice_CapturesEachMessageOnce()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddMokaDiagnostics();
		services.AddMokaDiagnostics();
		using ServiceProvider provider = services.BuildServiceProvider();

		Assert.Single(provider.GetServices<ILoggerProvider>().OfType<MokaDiagnosticsLoggerProvider>());
		Assert.Single(services, d => d.ServiceType == typeof(IMokaDiagnosticsService));
		Assert.Single(services, d => d.ServiceType == typeof(DiagnosticsOptions));

		provider.GetRequiredService<ILoggerFactory>().CreateLogger("Twice")
			.Log(LogLevel.Warning, default, "once only", null, (state, _) => state);

		Assert.Single(provider.GetRequiredService<MokaDiagnosticsConsoleBuffer>().GetMessages(), m => m.Message == "once only");
	}

	[Fact]
	public void AddMokaDiagnostics_CalledTwice_AppliesBothConfigurations()
	{
		var services = new ServiceCollection();
		services.AddMokaDiagnostics(options => options.PanelWidth = 500);
		services.AddMokaDiagnostics(options => options.Position = OverlayPosition.TopLeft);
		using ServiceProvider provider = services.BuildServiceProvider();

		DiagnosticsOptions options = provider.GetRequiredService<DiagnosticsOptions>();
		Assert.Equal(500, options.PanelWidth);
		Assert.Equal(OverlayPosition.TopLeft, options.Position);
	}

	[Fact]
	public void AddMokaDiagnostics_OptionsIsSingleton()
	{
		var services = new ServiceCollection();
		services.AddMokaDiagnostics();

		ServiceDescriptor descriptor = services.First(d => d.ServiceType == typeof(DiagnosticsOptions));

		Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
	}
}
