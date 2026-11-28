using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Extensions;
using Moka.Red.Core.Hotkeys;

namespace Moka.Red.Core.Tests.Hotkeys;

// A component with a shortcut of its own registers it with the app's hotkeys only while a host is there to fire it,
// so the registry has to say whether one is.
public class MokaHotkeyListeningTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Core/moka-hotkeys.js";

	public MokaHotkeyListeningTests()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.Mode = JSRuntimeMode.Loose;
		module.Setup<int>("bindHotkeys", _ => true).SetResult(7);
		Services.AddMokaHotkeys();
	}

	private IMokaHotkeyService Hotkeys => Services.GetRequiredService<IMokaHotkeyService>();

	[Fact]
	public void NothingListens_UntilAListenerIsAdded()
	{
		var service = new MokaHotkeyService();

		Assert.False(service.IsListening);
	}

	[Fact]
	public void TheRegistryListens_WhileAnyListenerIs()
	{
		var service = new MokaHotkeyService();
		int changes = 0;
		service.ListeningChanged += () => changes++;

		IDisposable first = service.AddListener();
		IDisposable second = service.AddListener();
		first.Dispose();

		Assert.True(service.IsListening);
		Assert.Equal(1, changes);

		second.Dispose();

		Assert.False(service.IsListening);
		Assert.Equal(2, changes);
	}

	[Fact]
	public void DisposingAListenerTwice_TakesItAwayOnce()
	{
		var service = new MokaHotkeyService();
		IDisposable first = service.AddListener();
		using IDisposable second = service.AddListener();

		first.Dispose();
		first.Dispose();

		Assert.True(service.IsListening);
	}

	[Fact]
	public void AnImplementationThatDoesntTrackHosts_NeverListens()
	{
		IMokaHotkeyService service = new Untracked();

		using IDisposable listener = service.AddListener();

		Assert.False(service.IsListening);
	}

	[Fact]
	public void TheHost_ListensOnceItsListenerIsBound()
	{
		IRenderedComponent<MokaHotkeyHost> cut = Render<MokaHotkeyHost>();

		cut.WaitForAssertion(() => Assert.True(Hotkeys.IsListening));
	}

	[Fact]
	public async Task TheHost_StopsListeningWhenItGoes()
	{
		IRenderedComponent<MokaHotkeyHost> cut = Render<MokaHotkeyHost>();
		cut.WaitForAssertion(() => Assert.True(Hotkeys.IsListening));

		await cut.Instance.DisposeAsync();

		Assert.False(Hotkeys.IsListening);
	}

	private sealed class Untracked : IMokaHotkeyService
	{
		public event Action OnChanged
		{
			add { }
			remove { }
		}

		public IReadOnlyList<MokaHotkey> Hotkeys => [];

		public IDisposable Register(MokaHotkey hotkey) => new MokaHotkeyService().Register(hotkey);

		public void Unregister(string id)
		{
		}

		public Task InvokeAsync(string id) => Task.CompletedTask;
	}
}
