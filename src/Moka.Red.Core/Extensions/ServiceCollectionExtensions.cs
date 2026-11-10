using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moka.Red.Core.Files;
using Moka.Red.Core.Hotkeys;
using Moka.Red.Core.PageTitle;
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
		MokaTheme registered = theme ?? MokaTheme.Default;
		services.AddCascadingValue(_ => registered);
		services.AddCascadingValue(_ => new MokaThemeSource(registered));
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

	/// <summary>
	///     Registers <see cref="IMokaPageTitleService" />, which <see cref="MokaPageTitle" /> and
	///     <see cref="MokaPageTitleHost" /> talk through. Put one <see cref="MokaPageTitleHost" /> in your layout as well:
	///     the host is what writes the title.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The same collection, for chaining.</returns>
	public static IServiceCollection AddMokaPageTitles(this IServiceCollection services)
	{
		services.TryAddScoped<IMokaPageTitleService, MokaPageTitleService>();
		return services;
	}

	/// <summary>
	///     Registers <typeparamref name="TPicker" /> as the app's <see cref="IMokaFilePicker" />, scoped, so every Moka
	///     component that would open the browser's file dialog asks it instead. <c>AddMokaRed()</c> doesn't call this:
	///     without a picker the components use the browser's dialog. A component's <c>FilePicker</c> parameter wins
	///     over the registered one.
	/// </summary>
	/// <typeparam name="TPicker">The picker, such as one over MAUI's <c>FilePicker</c>.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <returns>The same collection, for chaining.</returns>
	public static IServiceCollection AddMokaFilePicker<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPicker>(this IServiceCollection services)
		where TPicker : class, IMokaFilePicker
	{
		services.AddScoped<IMokaFilePicker, TPicker>();
		return services;
	}
}
