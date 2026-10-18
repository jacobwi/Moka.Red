using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Hotkeys;
using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Extensions;

/// <summary>
///     Extension methods for registering Moka.Red Core services.
///     For full service registration, use <c>AddMokaRed()</c> from the <c>Moka.Red</c> meta-package.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	///     Registers the Moka.Red Core cascading theme value.
	///     Prefer <c>AddMokaRed()</c> from the <c>Moka.Red.Extensions</c> namespace
	///     when using the meta-package, which registers all services.
	/// </summary>
	public static IServiceCollection AddMokaRedCore(
		this IServiceCollection services,
		MokaTheme? theme = null)
	{
		services.AddCascadingValue(_ => theme ?? MokaTheme.Light);
		return services;
	}

	/// <summary>
	///     Registers <see cref="IMokaHotkeyService" />, the app-wide keyboard shortcut registry. Put one
	///     <see cref="MokaHotkeyHost" /> in your layout as well: the host is what listens, so without it
	///     shortcuts are registered and listed but nothing fires them.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The same collection, for chaining.</returns>
	public static IServiceCollection AddMokaHotkeys(this IServiceCollection services)
	{
		services.AddScoped<IMokaHotkeyService, MokaHotkeyService>();
		return services;
	}
}
