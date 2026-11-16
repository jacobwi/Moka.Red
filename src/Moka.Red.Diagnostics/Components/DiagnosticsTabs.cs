using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.Diagnostics.Components.Panels;

namespace Moka.Red.Diagnostics.Components;

// A tab of the overlay and the full page. The page shows Name; the overlay shows ShortName, and the Settings tab as a gear.
internal sealed record DiagnosticsTab(string Key, string Name, string ShortName);

// The ten tabs the overlay and the full page share, in order, and the panel each one shows.
internal static class DiagnosticsTabs
{
	public const string Console = "Console";
	public const string Settings = "Settings";

	private const string Log = "Log";
	private const string Memory = "Memory";
	private const string Network = "Network";
	private const string Perf = "Perf";
	private const string Render = "Render";
	private const string Services = "Services";
	private const string Theme = "Theme";
	private const string Tree = "Tree";

	public static IReadOnlyList<DiagnosticsTab> All { get; } =
	[
		new(Console, "Console", "Console"),
		new(Theme, "Theme", "Theme"),
		new(Render, "Render", "Render"),
		new(Tree, "Tree", "Tree"),
		new(Network, "Network", "Network"),
		new(Memory, "Memory", "Memory"),
		new(Perf, "Performance", "Perf"),
		new(Services, "Services", "Services"),
		new(Log, "Log", "Log"),
		new(Settings, "Settings", "Settings")
	];

	// One sequence number per panel, so switching tabs replaces the panel and staying on one keeps it.
	public static RenderFragment Panel(string key, MokaTheme? theme) => builder =>
	{
		switch (key)
		{
			case Console:
				builder.OpenComponent<ConsolePanel>(0);
				break;
			case Theme:
				builder.OpenComponent<ThemeInspectorPanel>(1);
				builder.AddComponentParameter(2, nameof(ThemeInspectorPanel.Theme), theme);
				break;
			case Render:
				builder.OpenComponent<RenderTrackerPanel>(3);
				break;
			case Tree:
				builder.OpenComponent<ComponentTreePanel>(4);
				break;
			case Network:
				builder.OpenComponent<NetworkPanel>(5);
				break;
			case Memory:
				builder.OpenComponent<MemoryPanel>(6);
				break;
			case Perf:
				builder.OpenComponent<PerformancePanel>(7);
				break;
			case Services:
				builder.OpenComponent<ServicesPanel>(8);
				break;
			case Log:
				builder.OpenComponent<EventLogPanel>(9);
				break;
			case Settings:
				builder.OpenComponent<SettingsPanel>(10);
				break;
			default:
				return;
		}

		builder.CloseComponent();
	};
}
