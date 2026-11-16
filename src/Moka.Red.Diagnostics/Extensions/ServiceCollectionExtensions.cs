using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
	///     Adds the Moka.Red diagnostics overlay services. Calling it again registers nothing twice, so a log message is
	///     captured once, and its <paramref name="configure" /> changes the options the first call registered.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configure">Optional configuration for diagnostics options.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddMokaDiagnostics(
		this IServiceCollection services,
		Action<DiagnosticsOptions>? configure = null)
	{
		ArgumentNullException.ThrowIfNull(services);

		DiagnosticsOptions options = RegisteredOptions(services) ?? new DiagnosticsOptions();
		configure?.Invoke(options);

		services.TryAddSingleton(options);
		services.TryAddScoped<IMokaDiagnosticsService, MokaDiagnosticsService>();
		services.TryAddSingleton<MokaDiagnosticsConsoleBuffer>();
		services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, MokaDiagnosticsLoggerProvider>(sp =>
			new MokaDiagnosticsLoggerProvider(
				sp.GetRequiredService<MokaDiagnosticsConsoleBuffer>(),
				sp.GetRequiredService<DiagnosticsOptions>())));

		// MokaComponentBase and MokaInputBase report their JS interop calls to this when it is registered.
		services.TryAddScoped<IMokaJsInteropObserver, DiagnosticsJsInteropObserver>();
		services.TryAddSingleton<MokaDiagnosticsSessions>();

		return services;
	}

	private static DiagnosticsOptions? RegisteredOptions(IServiceCollection services) =>
		services
			.Where(descriptor => descriptor.ServiceType == typeof(DiagnosticsOptions) && !descriptor.IsKeyedService)
			.Select(descriptor => descriptor.ImplementationInstance)
			.OfType<DiagnosticsOptions>()
			.FirstOrDefault();
}
