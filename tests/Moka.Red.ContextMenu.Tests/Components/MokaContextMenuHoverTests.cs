using Bunit;
using Moka.Red.ContextMenu;

namespace Moka.Red.ContextMenu.Tests.Components;

// Hovering a row only opens and closes sub-menus, so a menu with none has no reason to send anything.
public class MokaContextMenuHoverTests : BunitContext
{
	public MokaContextMenuHoverTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void AFlatMenu_HasNoHoverListeners()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(new MokaContextMenuItem { Text = "Copy" }, new MokaContextMenuItem { Text = "Paste" });

		Assert.False(cut.Find("[role=menu]").HasAttribute("blazor:onmouseenter"));
		Assert.False(cut.Find("[role=menu]").HasAttribute("blazor:onmouseleave"));
		Assert.All(cut.FindAll("[role=menuitem]"), row => Assert.False(row.HasAttribute("blazor:onmouseenter")));
	}

	[Fact]
	public void AMenuWithASubmenu_ListensOnEveryRow()
	{
		// A plain row needs it too: moving onto one is what closes the open sub-menu.
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(
			new MokaContextMenuItem { Text = "Copy" },
			new MokaContextMenuItem { Text = "More", Children = [new MokaContextMenuItem { Text = "Deep" }] });

		Assert.True(cut.Find("[role=menu]").HasAttribute("blazor:onmouseleave"));
		Assert.All(cut.FindAll("[role=menuitem]"), row => Assert.True(row.HasAttribute("blazor:onmouseenter")));
	}

	private IRenderedComponent<MokaContextMenu> RenderMenu(params MokaContextMenuItem[] items) =>
		Render<MokaContextMenu>(p => p
			.Add(x => x.Items, items)
			.Add(x => x.Visible, true));
}
