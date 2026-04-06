using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.CommandPalette;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Feedback.Notification;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.Feedback.Extensions;

/// <summary>
///     Extension methods for registering Moka.Red Feedback services.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	///     Registers the Moka.Red Feedback services (toast, dialog, and notification) as scoped services.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddMokaFeedback(this IServiceCollection services)
	{
		services.AddScoped<IMokaToastService, MokaToastService>();
		services.AddScoped<IMokaDialogService, MokaDialogService>();
		services.AddScoped<IMokaNotificationService, MokaNotificationService>();
		services.AddScoped<IMokaCommandPaletteService, MokaCommandPaletteService>();
		return services;
	}
}
