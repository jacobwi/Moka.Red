using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Extensions;
using Moka.Red.Core.Hotkeys;

namespace Moka.Red.Core.Tests.Hotkeys;

// The host is the one listener. Matching happens in the browser, so what matters here is that the host hands
// the browser the right set and runs the right action when it hears back.
public class MokaHotkeyHostTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Core/moka-hotkeys.js";

	private readonly BunitJSModuleInterop _module;

	public MokaHotkeyHostTests()
	{
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
		_module.Setup<int>("bindHotkeys", _ => true).SetResult(7);
		Services.AddMokaHotkeys();
	}

	private IMokaHotkeyService Hotkeys => Services.GetRequiredService<IMokaHotkeyService>();

	[Fact]
	public void TheHostBindsOneListener()
	{
		IRenderedComponent<MokaHotkeyHost> cut = Render<MokaHotkeyHost>();

		cut.WaitForAssertion(() => Assert.Single(_module.Invocations["bindHotkeys"]));
	}

	[Fact]
	public void TheHostRendersNothing() =>
		Assert.Equal(string.Empty, Render<MokaHotkeyHost>().Markup.Trim());

	[Fact]
	public void RegisteringAHotkeyPushesItToTheBrowser()
	{
		IRenderedComponent<MokaHotkeyHost> cut = Render<MokaHotkeyHost>();
		cut.WaitForAssertion(() => Assert.NotEmpty(_module.Invocations["setHotkeys"]));
		int pushes = _module.Invocations["setHotkeys"].Count;

		cut.InvokeAsync(() => Hotkeys.Register(new MokaHotkey
		{
			Id = "save",
			Keys = "Ctrl+S",
			OnInvokeSync = () => { }
		}));

		cut.WaitForAssertion(() => Assert.True(_module.Invocations["setHotkeys"].Count > pushes));
	}

	// A hotkey with no keys, or nothing to run, is listed but never sent: the browser has nothing to do with it.
	[Fact]
	public void OnlyBindableHotkeysAreSent()
	{
		IRenderedComponent<MokaHotkeyHost> cut = Render<MokaHotkeyHost>();

		cut.InvokeAsync(() =>
		{
			Hotkeys.Register(new MokaHotkey { Id = "real", Keys = "Ctrl+S", OnInvokeSync = () => { } });
			Hotkeys.Register(new MokaHotkey { Id = "listed-only", Description = "Drag a tab" });
			Hotkeys.Register(new MokaHotkey { Id = "no-action", Keys = "Ctrl+Q" });
		});

		cut.WaitForAssertion(() =>
		{
			IReadOnlyList<JSRuntimeInvocation> pushes = _module.Invocations["setHotkeys"];
			Dictionary<string, object?>[] sent = Assert.IsType<Dictionary<string, object?>[]>(pushes[pushes.Count - 1].Arguments[1]);
			Assert.Single(sent);
		});
		Assert.Equal(3, Hotkeys.Hotkeys.Count);
	}

	// A modal is drawn inside the page, so its keys reach the one listener; the browser skips the set while one is open
	// unless a hotkey says it belongs there.
	[Fact]
	public void WhetherAHotkeyWorksInAModal_IsSentToTheBrowser()
	{
		IRenderedComponent<MokaHotkeyHost> cut = Render<MokaHotkeyHost>();

		cut.InvokeAsync(() =>
		{
			Hotkeys.Register(new MokaHotkey { Id = "page", Keys = "D", OnInvokeSync = () => { } });
			Hotkeys.Register(new MokaHotkey { Id = "dialog", Keys = "Ctrl+Enter", AllowInModal = true, OnInvokeSync = () => { } });
		});

		cut.WaitForAssertion(() =>
		{
			IReadOnlyList<JSRuntimeInvocation> pushes = _module.Invocations["setHotkeys"];
			Dictionary<string, object?>[] sent = Assert.IsType<Dictionary<string, object?>[]>(pushes[pushes.Count - 1].Arguments[1]);
			Assert.Equal(false, sent.Single(h => (string?)h["id"] == "page")["allowInModal"]);
			Assert.Equal(true, sent.Single(h => (string?)h["id"] == "dialog")["allowInModal"]);
		});
	}

	[Fact]
	public async Task AMatchFromTheBrowserRunsTheAction()
	{
		int runs = 0;
		IRenderedComponent<MokaHotkeyHost> cut = Render<MokaHotkeyHost>();
		await cut.InvokeAsync(() => Hotkeys.Register(new MokaHotkey
		{
			Id = "save",
			Keys = "Ctrl+S",
			OnInvokeSync = () => runs++
		}));

		await cut.InvokeAsync(() => cut.Instance.InvokeFromJs("save"));

		Assert.Equal(1, runs);
	}

	[Fact]
	public async Task TheListenerIsReleasedWithTheHost()
	{
		IRenderedComponent<MokaHotkeyHost> cut = Render<MokaHotkeyHost>();
		cut.WaitForAssertion(() => Assert.Single(_module.Invocations["bindHotkeys"]));

		await cut.Instance.DisposeAsync();

		Assert.Equal(7, Assert.Single(_module.Invocations["unbindHotkeys"]).Arguments[0]);
	}

	// Disposed, it must not run an action for a press that was already in flight.
	[Fact]
	public async Task AfterDisposalAMatchRunsNothing()
	{
		int runs = 0;
		IRenderedComponent<MokaHotkeyHost> cut = Render<MokaHotkeyHost>();
		await cut.InvokeAsync(() => Hotkeys.Register(new MokaHotkey
		{
			Id = "save",
			Keys = "Ctrl+S",
			OnInvokeSync = () => runs++
		}));
		await cut.Instance.DisposeAsync();

		await cut.Instance.InvokeFromJs("save");

		Assert.Equal(0, runs);
	}
}
