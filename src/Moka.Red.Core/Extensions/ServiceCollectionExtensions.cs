using Microsoft.Extensions.DependencyInjection;
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
}
