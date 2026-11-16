using Bunit;
using Moka.Red.Diagnostics.Components.Panels;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Diagnostics.Tests.Components;

// The whole markup of the panels whose checkboxes, selects and number boxes bind a setting (BindingGoldens says when to
// write the files again).
public sealed class PanelBindingGoldenTests : DevelopmentContext
{
	public PanelBindingGoldenTests()
	{
		Services.AddMokaDiagnostics();
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void SettingsPanel() => Golden("settings-panel", () => Render<SettingsPanel>().Markup);

	[Fact]
	public void ConsolePanel() => Golden("console-panel", () => Render<ConsolePanel>().Markup);

	[Fact]
	public void EventLogPanel() => Golden("event-log-panel", () => Render<EventLogPanel>().Markup);

	private static void Golden(string name, Func<string> markup) =>
		BindingGoldens.Check("Moka.Red.Diagnostics.Tests", name, markup);
}
