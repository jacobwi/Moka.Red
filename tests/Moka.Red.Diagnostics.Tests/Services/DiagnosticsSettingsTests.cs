using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moka.Red.Diagnostics.Components;
using Moka.Red.Diagnostics.Components.Panels;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Tests.Services;

// MaxEventLogEntries was ignored (the service kept 500 events) and so was MinConsoleLogLevel (the
// logger kept Debug and above). Both can be changed in the Settings tab, with no effect.
public class DiagnosticsSettingsTests : DevelopmentContext
{
	[Fact]
	public void EventLog_KeepsMaxEventLogEntries()
	{
		var service = new MokaDiagnosticsService(new DiagnosticsOptions { MaxEventLogEntries = 150 });

		RecordEvents(service, 400);

		Assert.Equal(150, service.GetRecentEvents(int.MaxValue).Count);
	}

	[Fact]
	public void EventLog_FollowsALimitChangedWhileRunning()
	{
		var service = new MokaDiagnosticsService(new DiagnosticsOptions { MaxEventLogEntries = 1000 });
		RecordEvents(service, 800);

		service.Settings.MaxEventLogEntries = 100;
		Assert.Equal(100, service.GetRecentEvents(int.MaxValue).Count);

		service.Settings.MaxEventLogEntries = 1000;
		RecordEvents(service, 300);
		Assert.Equal(400, service.GetRecentEvents(int.MaxValue).Count);
	}

	// The Settings tab changed the DiagnosticsOptions singleton, so on Blazor Server one user's change applied to every
	// connected user. Each window's settings start from the options and change for that window only.
	[Fact]
	public void Settings_StartFromTheOptions_AndChangeForOneWindowOnly()
	{
		var services = new ServiceCollection();
		services.AddMokaDiagnostics(options =>
		{
			options.Position = OverlayPosition.TopLeft;
			options.PanelWidth = 500;
			options.MaxEventLogEntries = 200;
			options.RenderTrackingEnabled = false;
		});
		using ServiceProvider provider = services.BuildServiceProvider();
		using IServiceScope first = provider.CreateScope();
		using IServiceScope second = provider.CreateScope();
		DiagnosticsSettings mine = first.ServiceProvider.GetRequiredService<IMokaDiagnosticsService>().Settings;
		DiagnosticsSettings theirs = second.ServiceProvider.GetRequiredService<IMokaDiagnosticsService>().Settings;

		Assert.Equal(OverlayPosition.TopLeft, mine.Position);
		Assert.Equal(500, mine.PanelWidth);
		Assert.Equal(200, mine.MaxEventLogEntries);
		Assert.False(mine.RenderTrackingEnabled);
		Assert.True(mine.JsInteropTrackingEnabled);

		mine.Position = OverlayPosition.BottomLeft;
		mine.PanelWidth = 320;
		mine.RenderTrackingEnabled = true;

		Assert.Equal(OverlayPosition.TopLeft, theirs.Position);
		Assert.Equal(500, theirs.PanelWidth);
		Assert.False(theirs.RenderTrackingEnabled);
		DiagnosticsOptions options = provider.GetRequiredService<DiagnosticsOptions>();
		Assert.Equal(OverlayPosition.TopLeft, options.Position);
		Assert.Equal(500, options.PanelWidth);
		Assert.False(options.RenderTrackingEnabled);
	}

	[Fact]
	public void TrackingSwitches_ApplyToTheirOwnWindow()
	{
		var services = new ServiceCollection();
		services.AddMokaDiagnostics();
		using ServiceProvider provider = services.BuildServiceProvider();
		using IServiceScope first = provider.CreateScope();
		using IServiceScope second = provider.CreateScope();
		IMokaDiagnosticsService off = first.ServiceProvider.GetRequiredService<IMokaDiagnosticsService>();
		IMokaDiagnosticsService on = second.ServiceProvider.GetRequiredService<IMokaDiagnosticsService>();

		off.Settings.RenderTrackingEnabled = false;
		off.Settings.JsInteropTrackingEnabled = false;
		foreach (IMokaDiagnosticsService service in new[] { off, on })
		{
			service.RecordRender("MokaButton", "button-1", TimeSpan.FromMilliseconds(1));
			service.RecordJsInteropCall("import", TimeSpan.FromMilliseconds(1));
		}

		Assert.Empty(off.GetRenderEntries());
		Assert.Empty(off.GetJsInteropEntries());
		Assert.Single(on.GetRenderEntries());
		Assert.Single(on.GetJsInteropEntries());
	}

	[Fact]
	public void Settings_SayWhenTheyChange_AndOnlyThen()
	{
		var settings = new DiagnosticsSettings(new DiagnosticsOptions());
		int changes = 0;
		settings.Changed += (_, _) => changes++;

		settings.PanelWidth = 420;
		settings.Position = OverlayPosition.BottomRight;
		Assert.Equal(0, changes);

		settings.PanelWidth = 500;
		settings.Position = OverlayPosition.TopRight;
		settings.MaxEventLogEntries = 100;
		settings.RenderTrackingEnabled = false;
		settings.JsInteropTrackingEnabled = false;
		Assert.Equal(5, changes);
	}

	[Fact]
	public void SettingsTab_ChangesThisWindowOnly()
	{
		Services.AddMokaDiagnostics();
		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		IRenderedComponent<SettingsPanel> cut = Render<SettingsPanel>();

		cut.FindAll("select")[0].Change("TopLeft");
		cut.Find("input[type='range']").Change("800");
		ControlFor(cut, "Render Tracking").Change(false);

		Assert.Equal(OverlayPosition.TopLeft, service.Settings.Position);
		Assert.Equal(600, service.Settings.PanelWidth);
		Assert.False(service.Settings.RenderTrackingEnabled);
		DiagnosticsOptions options = Services.GetRequiredService<DiagnosticsOptions>();
		Assert.Equal(OverlayPosition.BottomRight, options.Position);
		Assert.Equal(420, options.PanelWidth);
		Assert.True(options.RenderTrackingEnabled);
	}

	// The tab says how far each group of settings reaches, and the console level is the one shared by the whole app.
	[Fact]
	public void SettingsTab_SaysWhatIsPerWindowAndWhatTheWholeAppShares()
	{
		Services.AddMokaDiagnostics();
		IRenderedComponent<SettingsPanel> cut = Render<SettingsPanel>();

		IReadOnlyList<AngleSharp.Dom.IElement> groups = cut.FindAll("fieldset");
		Assert.Equal(["Per window", "Whole app", "Set at startup"],
			groups.Select(group => group.QuerySelector("legend")!.TextContent.Trim()).ToArray());
		Assert.All(groups, group =>
			Assert.NotNull(cut.Find($"#{group.GetAttribute("aria-describedby")}")));
		Assert.True(groups[1].Contains(ControlFor(cut, "Console Log Level")));
		Assert.Contains("every user", groups[1].TextContent, StringComparison.Ordinal);
		Assert.Empty(groups[2].QuerySelectorAll("input, select"));
	}

	// The popup page changes its opener's settings from its own circuit; the opener's Settings tab and overlay follow.
	[Fact]
	public void SettingsTab_FollowsAChangeMadeElsewhere()
	{
		Services.AddMokaDiagnostics();
		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		IRenderedComponent<SettingsPanel> cut = Render<SettingsPanel>();

		service.Settings.PanelWidth = 510;
		service.IsPaused = true;

		cut.WaitForAssertion(() =>
		{
			Assert.Equal("510px", cut.Find(".moka-diag-settings-range .moka-diag-settings-value").TextContent.Trim());
			Assert.True(ControlFor(cut, "Pause Tracking").HasAttribute("checked"));
		});
	}

	[Fact]
	public void Overlay_FollowsAPositionChangedElsewhere()
	{
		Services.AddMokaDiagnostics();
		JSInterop.Mode = JSRuntimeMode.Loose;
		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		IRenderedComponent<MokaDiagnosticsOverlay> cut = Render<MokaDiagnosticsOverlay>();

		service.Settings.Position = OverlayPosition.TopLeft;

		cut.WaitForAssertion(() =>
			Assert.Equal("top: 16px; left: 16px;", cut.Find(".moka-diag-badge-corner").GetAttribute("style")));
	}

	[Fact]
	public void Console_KeepsMessagesFromMinConsoleLogLevelUp()
	{
		var services = new ServiceCollection();
		services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Trace));
		services.AddMokaDiagnostics(options => options.MinConsoleLogLevel = LogLevel.Warning);
		using ServiceProvider provider = services.BuildServiceProvider();
		ILogger logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Tests.Console");

		Write(logger, LogLevel.Information, "information message");
		Write(logger, LogLevel.Warning, "warning message");

		IReadOnlyList<ConsoleLogEntry> messages = provider.GetRequiredService<MokaDiagnosticsConsoleBuffer>().GetMessages();
		Assert.DoesNotContain(messages, m => m.Message == "information message");
		Assert.Contains(messages, m => m.Message == "warning message");
	}

	[Fact]
	public void SettingsTab_ChangesTheConsoleLevelWhileRunning()
	{
		Services.AddMokaDiagnostics();
		ILogger logger = Services.GetServices<ILoggerProvider>().OfType<MokaDiagnosticsLoggerProvider>().Single()
			.CreateLogger("Tests.Settings");
		IRenderedComponent<SettingsPanel> cut = Render<SettingsPanel>();

		// The second select is Console Log Level; the first is Overlay Position.
		cut.FindAll("select")[1].Change("Error");
		Write(logger, LogLevel.Warning, "warning after the change");
		Write(logger, LogLevel.Error, "error after the change");

		IReadOnlyList<ConsoleLogEntry> messages = Services.GetRequiredService<MokaDiagnosticsConsoleBuffer>().GetMessages();
		Assert.DoesNotContain(messages, m => m.Message == "warning after the change");
		Assert.Contains(messages, m => m.Message == "error after the change");
	}

	// The level list stopped at Debug and Error, so an app configured for Trace or Critical saw a select that showed a
	// level it wasn't on and couldn't be set back.
	[Theory]
	[InlineData(LogLevel.Trace)]
	[InlineData(LogLevel.Critical)]
	public void SettingsTab_OffersEveryLogLevel(LogLevel level)
	{
		Services.AddMokaDiagnostics(options => options.MinConsoleLogLevel = level);

		IRenderedComponent<SettingsPanel> cut = Render<SettingsPanel>();

		Assert.Equal(level.ToString(), cut.FindAll("select")[1].QuerySelector("option[selected]")?.GetAttribute("value"));
	}

	[Fact]
	public void SettingsTab_ChangesTheEventLogSizeWhileRunning()
	{
		Services.AddMokaDiagnostics();
		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		RecordEvents(service, 450);
		IRenderedComponent<SettingsPanel> cut = Render<SettingsPanel>();

		cut.Find("input[type='number']").Change("100");

		Assert.Equal(100, service.GetRecentEvents(int.MaxValue).Count);
	}

	private static AngleSharp.Dom.IElement ControlFor(IRenderedComponent<SettingsPanel> cut, string label) =>
		cut.Find($"#{cut.FindAll("label").Single(l => l.TextContent.Trim() == label).GetAttribute("for")}");

	// ILogger.Log itself rather than the LogWarning-style extensions, which CA1848 flags.
	private static void Write(ILogger logger, LogLevel level, string message) =>
		logger.Log(level, default, message, null, (state, _) => state);

	private static void RecordEvents(IMokaDiagnosticsService service, int count)
	{
		for (int i = 0; i < count; i++)
		{
			service.RecordRender("MokaButton", $"button-{i}", TimeSpan.FromMilliseconds(1));
		}
	}
}
