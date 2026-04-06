using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Navigation.Tabs.Models;
using Moka.Red.Navigation.Tabs.Plugins;
using Moka.Red.Navigation.Tabs.Services;

namespace Moka.Red.Navigation.Extensions;

/// <summary>
///     Extension methods for registering Moka.Red.Navigation tab services.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	///     Adds the core Moka tab system services to the DI container.
	/// </summary>
	/// <typeparam name="TValue">The type of value stored by tabs.</typeparam>
	public static IServiceCollection AddMokaTabs<TValue>(this IServiceCollection services)
	{
		services.AddScoped<MokaTabPluginRegistry>();
		services.AddScoped<IMokaTabSessionState<TValue>, MokaTabSessionState<TValue>>();
		services.AddScoped<MokaTabIconProvider>();
		return services;
	}

	/// <summary>
	///     Registers a tab plugin for discovery by the tab system.
	/// </summary>
	/// <typeparam name="TPlugin">The plugin type implementing <see cref="IMokaTabPlugin" />.</typeparam>
	public static IServiceCollection AddMokaTabPlugin<TPlugin>(this IServiceCollection services)
		where TPlugin : class, IMokaTabPlugin
	{
		services.AddScoped<IMokaTabPlugin, TPlugin>();
		return services;
	}
}
