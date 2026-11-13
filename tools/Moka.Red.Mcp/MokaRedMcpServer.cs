using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Base;
using ModelContextProtocol.Protocol;

namespace Moka.Red.Mcp;

internal static class MokaRedMcpServer
{
	internal static string LibraryVersion { get; } =
		typeof(MokaComponentBase).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
		?? typeof(MokaComponentBase).Assembly.GetName().Version?.ToString(3)
		?? "unknown";

	internal static string Instructions { get; } =
		$"Moka.Red {LibraryVersion} is a Blazor component library. Before writing or changing Moka.Red markup, look the "
		+ "component up here instead of guessing parameter names: search_components finds one, get_component gives its "
		+ "parameters, defaults and enum values, and get_component_docs gives the docs page with examples. get_type "
		+ "describes the records, enums, services and icon sets the components take (MokaLineSeries, IMokaToastService, "
		+ "MokaIcons.Action). get_getting_started covers setup (AddMokaRed, MokaThemeProvider, imports) and theming.";

	internal static IMcpServerBuilder AddMokaRedMcp(this IServiceCollection services)
	{
		services.AddSingleton<ComponentCatalog>();
		services.AddSingleton<DocsLibrary>();

		return services
			.AddMcpServer(options =>
			{
				options.ServerInfo = new Implementation
				{
					Name = "moka-red",
					Title = "Moka.Red",
					Version = LibraryVersion,
					WebsiteUrl = "https://github.com/jacobwi/Moka.Red"
				};
				options.ServerInstructions = Instructions;
			})
			.WithTools<MokaRedTools>();
	}
}
