using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moka.Red.Core.Extensions;
using Moka.Red.Feedback.Banner;
using Moka.Red.Feedback.CommandPalette;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Feedback.Notification;
using Moka.Red.Feedback.SystemNotification;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.Feedback.Extensions;

/// <summary>
///     Extension methods for registering Moka.Red Feedback services.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	///     Registers the Moka.Red Feedback services as scoped services: toasts, dialogs, notifications, the command
	///     palette and banners, the announcer they speak through, and the browser's storage for dismissed banners unless
	///     the app registered its own <see cref="IMokaBannerStorage" />.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddMokaFeedback(this IServiceCollection services)
	{
		services.AddOptions();
		services.AddMokaAnnouncer();
		services.AddScoped<IMokaToastService, MokaToastService>();
		services.AddScoped<IMokaDialogService, MokaDialogService>();
		services.AddScoped<IMokaNotificationService, MokaNotificationService>();
		services.AddScoped<IMokaCommandPaletteService, MokaCommandPaletteService>();
		services.AddScoped<IMokaBannerService, MokaBannerService>();
		services.TryAddScoped<IMokaBannerStorage, MokaBrowserBannerStorage>();
		return services;
	}

	/// <summary>
	///     Registers the Moka.Red Feedback services, as <see cref="AddMokaFeedback(IServiceCollection)" /> does, with
	///     app-wide settings: toast defaults and a limit on stored notifications. It adds to any other
	///     <c>services.Configure&lt;MokaFeedbackOptions&gt;</c>.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configure">Sets the <see cref="MokaFeedbackOptions" />.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddMokaFeedback(this IServiceCollection services, Action<MokaFeedbackOptions> configure)
	{
		ArgumentNullException.ThrowIfNull(configure);
		services.AddMokaFeedback();
		services.Configure(configure);
		return services;
	}

	/// <summary>
	///     Registers <see cref="IMokaSystemNotifier" />, which shows notifications outside the page through the browser's
	///     Notification API. Opt-in, since it asks the person for permission when the app first uses it. A host without
	///     the API, such as a MAUI web view, gets a notifier that reports it as unsupported.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddMokaSystemNotifier(this IServiceCollection services)
	{
		services.TryAddScoped<IMokaSystemNotifier, MokaSystemNotifier>();
		return services;
	}
}
