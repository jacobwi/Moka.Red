using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moka.Red.Core.Base;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Extensions;

/// <summary>
///     Extension methods for registering Moka.Red diagnostics services.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	///     Adds the Moka.Red diagnostics overlay services.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configure">Optional configuration for diagnostics options.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddMokaDiagnostics(
		this IServiceCollection services,
		Action<DiagnosticsOptions>? configure = null)
	{
		ArgumentNullException.ThrowIfNull(services);

		var options = new DiagnosticsOptions();
		configure?.Invoke(options);

		services.AddSingleton(options);
		services.AddScoped<IMokaDiagnosticsService, MokaDiagnosticsService>();
		services.AddSingleton<MokaDiagnosticsConsoleBuffer>();
		services.AddSingleton<ILoggerProvider>(sp => new MokaDiagnosticsLoggerProvider(
			sp.GetRequiredService<MokaDiagnosticsConsoleBuffer>(),
			sp.GetRequiredService<DiagnosticsOptions>()));

		// MokaComponentBase and MokaInputBase report their JS interop calls to this when it is registered.
		services.AddScoped<IMokaJsInteropObserver, DiagnosticsJsInteropObserver>();
		services.AddSingleton<MokaDiagnosticsSessions>();

		return services;
	}
}
