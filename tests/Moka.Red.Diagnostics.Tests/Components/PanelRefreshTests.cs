using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moka.Red.Diagnostics.Components.Panels;
using Moka.Red.Diagnostics.Extensions;
using Moka.Red.Diagnostics.Services;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Diagnostics.Tests.Components;

// Every panel rendered once a second whatever had happened, from a timer whose dispatch was a discarded task with
// ObjectDisposedException swallowed, so a failure while reloading vanished. On Blazor Server each of those renders of
// the Console and Log tabs sent every row again, since the rows weren't keyed and new entries shift them down.
public sealed class PanelRefreshTests : BunitContext
{
	private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);
	private readonly FakeClock _clock = new();

	public PanelRefreshTests()
	{
		Services.AddMokaDiagnostics();
		Services.AddSingleton<TimeProvider>(_clock);
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void ATick_RendersOnlyWhenTheReloadSaysSomethingChanged()
	{
		bool changed = false;
		int renders = 0;
		using PanelRefresh refresh = new(_clock, Second, RunNow, _ => Task.CompletedTask, () => changed, () => renders++);

		_clock.Advance(Second);
		Assert.Equal(0, renders);

		changed = true;
		_clock.Advance(Second);
		Assert.Equal(1, renders);
	}

	[Fact]
	public void AFailingReload_ReachesTheErrorHandler()
	{
		var failure = new InvalidOperationException("reload failed");
		Exception? reported = null;
		using PanelRefresh refresh = new(_clock, Second, RunNow, ex =>
		{
			reported = ex;
			return Task.CompletedTask;
		}, () => throw failure, () => { });

		_clock.Advance(Second);

		Assert.Same(failure, reported);
	}

	[Fact]
	public void ATickThatFiredJustBeforeDisposal_DoesNothing()
	{
		int reloads = 0;
		PanelRefresh refresh = new(_clock, Second, RunNow, _ => Task.CompletedTask, () => ++reloads > 0, () => { });
		FakeTimer timer = _clock.Timers.Single();

		refresh.Dispose();
		timer.InvokeCallback();

		Assert.Equal(0, reloads);
		Assert.True(timer.Disposed);
	}

	[Fact]
	public void ConsolePanel_RendersOnATick_OnlyWhenAMessageArrived()
	{
		IRenderedComponent<ConsolePanel> cut = Render<ConsolePanel>();
		int renders = cut.RenderCount;

		_clock.Advance(Second);
		Assert.Equal(renders, cut.RenderCount);

		Services.GetRequiredService<MokaDiagnosticsConsoleBuffer>().Add(Entry("arrived"));
		_clock.Advance(Second);

		cut.WaitForAssertion(() => Assert.Equal(renders + 1, cut.RenderCount));
		Assert.Contains("arrived", cut.Find(".moka-diag-console-msg").TextContent, StringComparison.Ordinal);
	}

	// Two equal entries (the same message in the same tick) must not become one key twice, which Blazor refuses, and
	// expanding one exception expanded every entry of the same tick and category.
	[Fact]
	public async Task ConsolePanel_ShowsEqualMessagesApart()
	{
		MokaDiagnosticsConsoleBuffer buffer = Services.GetRequiredService<MokaDiagnosticsConsoleBuffer>();
		ConsoleLogEntry entry = Entry("same") with { Exception = "System.Exception: same" };
		buffer.Add(entry);
		buffer.Add(entry with { });
		IRenderedComponent<ConsolePanel> cut = Render<ConsolePanel>();
		Assert.Equal(2, cut.FindAll(".moka-diag-console-entry").Count);

		IElement expand = cut.FindAll(".moka-diag-console-expand")[0];
		Assert.Equal("false", expand.GetAttribute("aria-expanded"));
		await expand.ClickAsync(new MouseEventArgs());

		Assert.Single(cut.FindAll(".moka-diag-console-exception"));
		Assert.Equal("true", cut.FindAll(".moka-diag-console-expand")[0].GetAttribute("aria-expanded"));
		Assert.Equal("false", cut.FindAll(".moka-diag-console-expand")[1].GetAttribute("aria-expanded"));
	}

	[Fact]
	public void EventLogPanel_RendersOnATick_OnlyWhenAnEventArrived()
	{
		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		IRenderedComponent<EventLogPanel> cut = Render<EventLogPanel>();
		int renders = cut.RenderCount;

		_clock.Advance(Second);
		Assert.Equal(renders, cut.RenderCount);

		service.RecordJsInteropCall("bindThing", TimeSpan.FromMilliseconds(2));
		_clock.Advance(Second);

		cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".moka-diag-log-entry")));
		Assert.Equal(renders + 1, cut.RenderCount);
	}

	// The type filters reached the list only on the next timer tick, so a click looked like it did nothing.
	[Fact]
	public void EventLogFilters_ApplyAtOnce()
	{
		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		service.RecordRender("MokaButton", "button-1", TimeSpan.FromMilliseconds(1));
		service.RecordJsInteropCall("bindThing", TimeSpan.FromMilliseconds(2));
		IRenderedComponent<EventLogPanel> cut = Render<EventLogPanel>();
		Assert.Equal(2, cut.FindAll(".moka-diag-log-entry").Count);

		cut.FindAll(".moka-diag-log-filter input")[0].Change(false);

		IElement shown = Assert.Single(cut.FindAll(".moka-diag-log-entry"));
		Assert.Contains("bindThing", shown.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void NetworkPanel_RendersOnATick_OnlyWhenACallWasRecorded()
	{
		IMokaDiagnosticsService service = Services.GetRequiredService<IMokaDiagnosticsService>();
		service.RecordJsInteropCall("bindThing", TimeSpan.FromMilliseconds(2));
		IRenderedComponent<NetworkPanel> cut = Render<NetworkPanel>();
		int renders = cut.RenderCount;

		_clock.Advance(Second);
		Assert.Equal(renders, cut.RenderCount);

		service.RecordJsInteropCall("bindThing", TimeSpan.FromMilliseconds(3));
		_clock.Advance(Second);

		cut.WaitForAssertion(() => Assert.Equal(renders + 1, cut.RenderCount));
		Assert.Contains(cut.FindAll("[role=cell]"), cell => cell.TextContent.Trim() == "2");
	}

	private static Task RunNow(Action work)
	{
		try
		{
			work();
			return Task.CompletedTask;
		}
		catch (InvalidOperationException ex)
		{
			return Task.FromException(ex);
		}
	}

	private static ConsoleLogEntry Entry(string message) => new()
	{
		Timestamp = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc),
		Level = LogLevel.Information,
		Category = "Tests",
		Message = message
	};
}
