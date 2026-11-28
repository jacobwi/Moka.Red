using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Extensions;
using Moka.Red.Core.Hotkeys;
using Moka.Red.Feedback.CommandPalette;

namespace Moka.Red.Feedback.Tests.Components.CommandPalette;

// With a host listening, the palette's shortcut lives in the app's hotkeys: one listener, and listed with the rest.
// Without one it keeps its own listener, so an app with a palette and no host still opens it.
public class MokaCommandPaletteHotkeyTests : CommandPaletteTestBase
{
	public MokaCommandPaletteHotkeyTests() => Services.AddMokaHotkeys();

	private IMokaHotkeyService Hotkeys => Services.GetRequiredService<IMokaHotkeyService>();

	private MokaHotkey PaletteHotkey => Assert.Single(Hotkeys.Hotkeys);

	[Fact]
	public void WithAHostListening_TheShortcutGoesToTheAppsHotkeys()
	{
		using IDisposable host = Hotkeys.AddListener();

		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();

		cut.WaitForAssertion(() => Assert.Single(Hotkeys.Hotkeys));
		Assert.Equal("Mod+K", PaletteHotkey.Keys);
		Assert.Equal("Open the command palette", PaletteHotkey.Description);
		Assert.True(PaletteHotkey.AllowInModal);
		Assert.Equal(MokaHotkeyFields.UnlessTyping, PaletteHotkey.InFields);
		Assert.Empty(PaletteModule.Invocations["registerShortcut"]);
	}

	[Fact]
	public async Task TheRegisteredHotkey_TogglesThePalette()
	{
		using IDisposable host = Hotkeys.AddListener();
		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();
		cut.WaitForAssertion(() => Assert.Single(Hotkeys.Hotkeys));

		await cut.InvokeAsync(() => Hotkeys.InvokeAsync(PaletteHotkey.Id));

		Assert.True(Palette.IsOpen);
	}

	[Fact]
	public void WithoutAHost_ThePaletteListensItself()
	{
		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();

		cut.WaitForAssertion(() => Assert.Single(PaletteModule.Invocations["registerShortcut"]));
		Assert.Empty(Hotkeys.Hotkeys);
	}

	[Fact]
	public void WhenAHostStarts_TheShortcutMovesToTheAppsHotkeys()
	{
		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();
		cut.WaitForAssertion(() => Assert.Single(PaletteModule.Invocations["registerShortcut"]));

		IDisposable host = Hotkeys.AddListener();

		cut.WaitForAssertion(() => Assert.Single(Hotkeys.Hotkeys));
		Assert.Single(PaletteModule.Invocations["dispose"]);
		host.Dispose();
	}

	[Fact]
	public void WhenTheHostGoes_ThePaletteListensItselfAgain()
	{
		IDisposable host = Hotkeys.AddListener();
		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();
		cut.WaitForAssertion(() => Assert.Single(Hotkeys.Hotkeys));

		host.Dispose();

		cut.WaitForAssertion(() => Assert.Single(PaletteModule.Invocations["registerShortcut"]));
		Assert.Empty(Hotkeys.Hotkeys);
	}

	[Fact]
	public void ChangingTheShortcut_ReplacesTheHotkey_AndNoneTakesItAway()
	{
		using IDisposable host = Hotkeys.AddListener();
		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();
		cut.WaitForAssertion(() => Assert.Single(Hotkeys.Hotkeys));

		cut.Render(p => p.Add(x => x.Shortcut, "Ctrl+Shift+P"));
		Assert.Equal("Ctrl+Shift+P", PaletteHotkey.Keys);

		cut.Render(p => p.Add(x => x.Shortcut, null));
		Assert.Empty(Hotkeys.Hotkeys);
	}

	[Fact]
	public void TheShortcutsDescription_ComesFromText()
	{
		using IDisposable host = Hotkeys.AddListener();

		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>(p => p
			.Add(x => x.Text, new MokaCommandPaletteText { ShortcutDescription = "Befehle" }));

		cut.WaitForAssertion(() => Assert.Equal("Befehle", PaletteHotkey.Description));
	}

	[Fact]
	public async Task ThePaletteGoing_TakesItsHotkeyAway()
	{
		using IDisposable host = Hotkeys.AddListener();
		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();
		cut.WaitForAssertion(() => Assert.Single(Hotkeys.Hotkeys));

		await cut.Instance.DisposeAsync();

		Assert.Empty(Hotkeys.Hotkeys);
	}
}
