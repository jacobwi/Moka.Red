using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Moka.Red.Diagnostics.Services;

// Whether the diagnostics run in this app. The Console tab shows the app's logs, every user's on Blazor Server, so like
// ASP.NET Core's developer exception page they run only in the Development environment, unless the app turns them on
// elsewhere through DiagnosticsOptions.EnableOutsideDevelopment.
internal sealed class MokaDiagnosticsAvailability(bool isOn)
{
	private const string WebAssemblyEnvironmentType =
		"Microsoft.AspNetCore.Components.WebAssembly.Hosting.IWebAssemblyHostEnvironment, Microsoft.AspNetCore.Components.WebAssembly";

	public bool IsOn { get; } = isOn;

	public static MokaDiagnosticsAvailability For(IServiceProvider services, DiagnosticsOptions options) =>
		new(options.EnableOutsideDevelopment || RunsInEnvironment(services));

	// Without AddMokaDiagnostics() there are no options to opt in with, so the environment alone decides.
	public static bool IsOnIn(IServiceProvider services) =>
		services.GetService<MokaDiagnosticsAvailability>()?.IsOn ?? RunsInEnvironment(services);

	private static bool RunsInEnvironment(IServiceProvider services)
	{
		string? environment = services.GetService<IHostEnvironment>()?.EnvironmentName
		                      ?? WebAssemblyEnvironmentName(services);
		if (environment is not null)
		{
			return string.Equals(environment, Environments.Development, StringComparison.OrdinalIgnoreCase);
		}

		// No environment registered: a Blazor Hybrid app or a test, one user's own process, where the app's call to
		// AddMokaDiagnostics() is the switch, as the MAUI docs register developer tools under #if DEBUG. A WebAssembly
		// host always registers one, so failing to read it there keeps the diagnostics off.
		return !OperatingSystem.IsBrowser();
	}

	// The package that declares the WebAssembly host's environment brings the WebAssembly runtime script with it, which
	// a library that also runs on the server and in Blazor Hybrid can't take on, so the interface is found by name.
	[DynamicDependency("get_Environment", "Microsoft.AspNetCore.Components.WebAssembly.Hosting.IWebAssemblyHostEnvironment",
		"Microsoft.AspNetCore.Components.WebAssembly")]
	private static string? WebAssemblyEnvironmentName(IServiceProvider services)
	{
		Type? type = Type.GetType(WebAssemblyEnvironmentType);
		return type is not null && services.GetService(type) is { } environment
			? type.GetProperty("Environment")?.GetValue(environment) as string
			: null;
	}
}
