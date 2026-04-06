using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.Extensions;

namespace Moka.Red.Extensions;

/// <summary>
///     Extension methods for registering all Moka.Red services with a single call.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	///     Registers all Moka.Red services: theming (cascading value), feedback (toast,
	///     dialog, notification, command palette). This is the recommended single entry
	///     point for consumers installing the <c>Moka.Red</c> meta-package.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configure">Optional theme configuration.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddMokaRed(
		this IServiceCollection services,
		Action<MokaRedOptions>? configure = null)
	{
		// Core: cascading theme value
		var options = new MokaRedOptions();
		configure?.Invoke(options);
		services.AddCascadingValue(_ => options.Theme);

		// Feedback: toast, dialog, notification, command palette
		services.AddMokaFeedback();

		return services;
	}
}

/// <summary>
///     Configuration options for the <c>AddMokaRed</c> meta-package registration.
/// </summary>
public sealed class MokaRedOptions
{
	/// <summary>Theme used for the cascading value. Defaults to light.</summary>
	public MokaTheme Theme { get; set; } = MokaTheme.Light;
}
