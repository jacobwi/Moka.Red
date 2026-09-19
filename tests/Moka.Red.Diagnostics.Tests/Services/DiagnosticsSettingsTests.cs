using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moka.Red.Diagnostics.Components.Panels;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Tests.Services;

// MaxEventLogEntries was ignored (the service kept 500 events) and so was MinConsoleLogLevel (the
// logger kept Debug and above). Both can be changed in the Settings tab, with no effect.
public class DiagnosticsSettingsTests : BunitContext
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
		var options = new DiagnosticsOptions { MaxEventLogEntries = 1000 };
		var service = new MokaDiagnosticsService(options);
		RecordEvents(service, 800);

		options.MaxEventLogEntries = 100;
		Assert.Equal(100, service.GetRecentEvents(int.MaxValue).Count);

		options.MaxEventLogEntries = 1000;
		RecordEvents(service, 300);
		Assert.Equal(400, service.GetRecentEvents(int.MaxValue).Count);
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
