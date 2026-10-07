using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.ContextMenu.Extensions;

namespace Moka.Red.ContextMenu.Tests.Components;

/// <summary>
///     A shortcut hint is drawn as one key cap per key rather than a run of text, so it reads the way the keyboard
///     does and matches the caps the cheatsheet uses.
/// </summary>
public class MokaContextMenuShortcutTests : BunitContext
{
	private const string MenuModule = "./_content/Moka.Red.ContextMenu/MokaContextMenu.razor.js";

	public MokaContextMenuShortcutTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule(MenuModule);
		Services.AddMokaContextMenu();
	}

	private IMokaContextMenuService Menu => Services.GetRequiredService<IMokaContextMenuService>();

	[Fact]
	public void EachKeyGetsItsOwnCap()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();

		Menu.Show(0, 0, [new() { Text = "Close", Shortcut = "Ctrl+Shift+W" }]);

		cut.WaitForAssertion(() =>
			Assert.Equal(["Ctrl", "Shift", "W"], cut.FindAll("kbd").Select(cap => cap.TextContent.Trim())));
	}

	[Fact]
	public void ASingleKeyIsASingleCap()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();

		Menu.Show(0, 0, [new() { Text = "Rename", Shortcut = "F2" }]);

		cut.WaitForAssertion(() => Assert.Equal(["F2"], cut.FindAll("kbd").Select(cap => cap.TextContent.Trim())));
	}

	[Fact]
	public void AnItemWithoutAShortcutDrawsNoCap()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();

		Menu.Show(0, 0, [new() { Text = "Properties" }]);

		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("kbd")));
	}
}
