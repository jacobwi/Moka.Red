using Moka.Red.Core.Hotkeys;

namespace Moka.Red.Core.Tests.Hotkeys;

// Three features talked about shortcuts and none of them owned a binding: the palette bound exactly one key,
// the cheatsheet rendered whatever static list it was handed, and a context menu item's Shortcut was a label.
// The registry is the one place a shortcut is both bound and listed.
public class MokaHotkeyServiceTests
{
	private static MokaHotkey Save(Action? run = null) => new()
	{
		Id = "save",
		Keys = "Ctrl+S",
		Description = "Save",
		Group = "File",
		OnInvokeSync = run ?? (() => { })
	};

	[Fact]
	public void ARegisteredHotkeyIsListed()
	{
		MokaHotkeyService service = new();

		service.Register(Save());

		MokaHotkey only = Assert.Single(service.Hotkeys);
		Assert.Equal("Ctrl+S", only.Keys);
	}

	[Fact]
	public async Task InvokingRunsTheAction()
	{
		int runs = 0;
		MokaHotkeyService service = new();
		service.Register(Save(() => runs++));

		await service.InvokeAsync("save");

		Assert.Equal(1, runs);
	}

	// A menu item and the shortcut should run the same thing, so an unknown id is quiet rather than a throw.
	[Fact]
	public async Task InvokingSomethingUnregisteredDoesNothing()
	{
		MokaHotkeyService service = new();

		await service.InvokeAsync("nothing-here");
	}

	[Fact]
	public async Task AnAsyncActionIsAwaited()
	{
		bool finished = false;
		MokaHotkeyService service = new();
		service.Register(new MokaHotkey
		{
			Id = "slow",
			Keys = "Ctrl+L",
			OnInvoke = async () =>
			{
				await Task.Yield();
				finished = true;
			}
		});

		await service.InvokeAsync("slow");

		Assert.True(finished);
	}

	// OnInvoke wins, so a hotkey carrying both does not run twice.
	[Fact]
	public async Task TheAsyncActionWinsOverTheSyncOne()
	{
		int async = 0;
		int sync = 0;
		MokaHotkeyService service = new();
		service.Register(new MokaHotkey
		{
			Id = "both",
			Keys = "Ctrl+B",
			OnInvoke = () => { async++; return Task.CompletedTask; },
			OnInvokeSync = () => sync++
		});

		await service.InvokeAsync("both");

		Assert.Equal(1, async);
		Assert.Equal(0, sync);
	}

	// A component that re-registers on every parameter set would otherwise grow the list without bound.
	[Fact]
	public void RegisteringTheSameIdReplacesItInPlace()
	{
		MokaHotkeyService service = new();
		service.Register(Save());

		service.Register(Save() with { Keys = "Ctrl+Shift+S", Description = "Save as" });

		MokaHotkey only = Assert.Single(service.Hotkeys);
		Assert.Equal("Ctrl+Shift+S", only.Keys);
		Assert.Equal("Save as", only.Description);
	}

	[Fact]
	public void DisposingTheRegistrationUnregistersIt()
	{
		MokaHotkeyService service = new();
		IDisposable registration = service.Register(Save());

		registration.Dispose();

		Assert.Empty(service.Hotkeys);
	}

	// Disposing twice must not take out whatever claimed the id in the meantime.
	[Fact]
	public void DisposingTwiceLeavesALaterRegistrationAlone()
	{
		MokaHotkeyService service = new();
		IDisposable first = service.Register(Save());
		first.Dispose();
		service.Register(Save() with { Description = "Save again" });

		first.Dispose();

		Assert.Single(service.Hotkeys);
	}

	[Fact]
	public void TheListIsInRegistrationOrder()
	{
		MokaHotkeyService service = new();
		service.Register(new MokaHotkey { Id = "a", Keys = "Ctrl+A" });
		service.Register(new MokaHotkey { Id = "b", Keys = "Ctrl+B" });
		service.Register(new MokaHotkey { Id = "c", Keys = "Ctrl+C" });

		Assert.Equal(["a", "b", "c"], service.Hotkeys.Select(h => h.Id));
	}

	[Fact]
	public void EveryChangeIsAnnounced()
	{
		int changes = 0;
		MokaHotkeyService service = new();
		service.OnChanged += () => changes++;

		IDisposable registration = service.Register(Save());
		registration.Dispose();

		Assert.Equal(2, changes);
	}

	// Unregistering something that is not there changes nothing, so the host does not rebind for nothing.
	[Fact]
	public void UnregisteringNothingAnnouncesNothing()
	{
		int changes = 0;
		MokaHotkeyService service = new();
		service.OnChanged += () => changes++;

		service.Unregister("never-registered");

		Assert.Equal(0, changes);
	}

	// Without keys or without an action there is nothing for the browser side to bind.
	[Theory]
	[InlineData(null, true)]
	[InlineData("", true)]
	[InlineData("   ", true)]
	[InlineData("Ctrl+S", false)]
	public void AHotkeyWithoutKeysIsNotBindable(string? keys, bool expectedUnbindable)
	{
		MokaHotkey hotkey = new() { Keys = keys, OnInvokeSync = () => { } };

		Assert.Equal(expectedUnbindable, !hotkey.IsBindable);
	}

	[Fact]
	public void AHotkeyWithoutAnActionIsNotBindable() =>
		Assert.False(new MokaHotkey { Keys = "Ctrl+S" }.IsBindable);

	// Ids are only needed for replacing and unregistering, so one is generated for a hotkey that is
	// registered once and disposed with its component.
	[Fact]
	public void TwoHotkeysWithoutIdsDoNotCollide()
	{
		MokaHotkeyService service = new();

		service.Register(new MokaHotkey { Keys = "Ctrl+A", OnInvokeSync = () => { } });
		service.Register(new MokaHotkey { Keys = "Ctrl+B", OnInvokeSync = () => { } });

		Assert.Equal(2, service.Hotkeys.Count);
	}
}
