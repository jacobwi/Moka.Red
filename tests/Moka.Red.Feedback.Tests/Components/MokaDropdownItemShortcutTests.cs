using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.Dropdown;

namespace Moka.Red.Feedback.Tests.Components;

// A dropdown item had no way to show the shortcut that runs the same action, which the context menu and menu bar show.
public class MokaDropdownItemShortcutTests : BunitContext
{
	public MokaDropdownItemShortcutTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void WithoutAShortcut_TheItemIsAsItWas()
	{
		IRenderedComponent<MokaDropdownItem> cut = Render<MokaDropdownItem>(p => p.Add(x => x.Text, "Save"));

		Assert.Empty(cut.FindAll(".moka-dropdown-item-shortcut"));
		Assert.Empty(cut.FindAll("kbd"));
	}

	[Fact]
	public void AShortcut_IsAKeyCapPerKey_AfterTheText()
	{
		IRenderedComponent<MokaDropdownItem> cut = Render<MokaDropdownItem>(p => p
			.Add(x => x.Text, "Command palette")
			.Add(x => x.Shortcut, "Ctrl+Shift+P"));

		IElement shortcut = cut.Find(".moka-dropdown-item-shortcut");
		Assert.Equal(["Ctrl", "Shift", "P"], shortcut.QuerySelectorAll("kbd").Select(k => k.TextContent));
		Assert.Equal("moka-dropdown-item-text", shortcut.PreviousElementSibling!.ClassName);
	}

	[Fact]
	public void TheModKey_ReadsCtrl_OrCmdOnAnApplePlatform()
	{
		IRenderedComponent<MokaDropdownItem> cut = Render<MokaDropdownItem>(p => p
			.Add(x => x.Text, "Save")
			.Add(x => x.Shortcut, "Mod+S"));

		IElement mod = cut.FindAll(".moka-dropdown-item-shortcut kbd")[0];
		Assert.Equal("Ctrl", mod.QuerySelector(".moka-kbd-pc")!.TextContent);
		Assert.NotNull(mod.QuerySelector(".moka-kbd-apple"));
	}

	[Fact]
	public void ALink_ShowsItsShortcutToo()
	{
		IRenderedComponent<MokaDropdownItem> cut = Render<MokaDropdownItem>(p => p
			.Add(x => x.Text, "Settings")
			.Add(x => x.Href, "/settings")
			.Add(x => x.Shortcut, "Ctrl+,"));

		Assert.Equal(["Ctrl", ","], cut.FindAll("a .moka-dropdown-item-shortcut kbd").Select(k => k.TextContent));
	}
}
