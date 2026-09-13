using Microsoft.Extensions.DependencyInjection;

namespace Moka.Red.ContextMenu.Extensions;

/// <summary>
///     Extension methods for registering the Moka.Red context-menu service.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	///     Registers <see cref="IMokaContextMenuService" /> as a scoped service. Place one
	///     <see cref="MokaContextMenuHost" /> in your layout to render the shared menu.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddMokaContextMenu(this IServiceCollection services)
	{
		services.AddScoped<IMokaContextMenuService, MokaContextMenuService>();
		return services;
	}
}
