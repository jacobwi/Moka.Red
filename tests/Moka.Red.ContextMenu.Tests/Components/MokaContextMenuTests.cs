using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using static Moka.Red.ContextMenu.Tests.MenuDom;

namespace Moka.Red.ContextMenu.Tests.Components;

public class MokaContextMenuTests : BunitContext
{
	private int _closes;

	public MokaContextMenuTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task DisabledItems_SayTrue_AndIgnoreClicks()
	{
		bool pasted = false;
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(
		[
			new() { Text = "Copy" },
			new() { Text = "Paste", Disabled = true, OnClickSync = () => pasted = true }
		]);
		IElement menu = cut.Find("[role=menu]");
		Assert.False(Row(menu, "Copy").HasAttribute("aria-disabled"));
		Assert.Equal("true", Row(menu, "Paste").GetAttribute("aria-disabled"));

		await Row(menu, "Paste").ClickAsync(new MouseEventArgs());

		Assert.False(pasted);
		Assert.Equal(0, _closes);
	}

	[Fact]
	public async Task ClickingAnItemWithChildren_RunsNothing_AndKeepsTheMenuOpen()
	{
		bool ran = false;
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(
		[
			new() { Text = "Share", OnClickSync = () => ran = true, Children = [new MokaContextMenuItem { Text = "Email" }] }
		]);

		await Row(cut.Find("[role=menu]"), "Share").ClickAsync(new MouseEventArgs());

		Assert.False(ran);
		Assert.Equal(0, _closes);
	}

	[Fact]
	public void Dividers_AreDroppedAtTheEnds_AndRepeatsMerged()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(
		[
			MokaContextMenuItems.Divider(),
			new() { Text = "Cut" },
			MokaContextMenuItems.Divider(),
			MokaContextMenuItems.Divider(),
			new() { Text = "Paste", DividerBefore = true },
			MokaContextMenuItems.Divider()
		]);

		IElement menu = cut.Find("[role=menu]");
		Assert.Equal(["moka-ctx-item", "moka-ctx-divider", "moka-ctx-item"],
			menu.Children.Select(child => child.ClassList[0]));
	}

	[Fact]
	public void OnlyTheRootMenu_HasABackdrop()
	{
		IRenderedComponent<MokaContextMenu> cut = Render<MokaContextMenu>(p => p
			.Add(x => x.Items, [new MokaContextMenuItem { Text = "Email" }])
			.Add(x => x.Visible, true)
			.Add(x => x.IsSubmenu, true));

		Assert.Empty(cut.FindAll(".moka-ctx-backdrop"));
		Assert.Contains("moka-ctx-menu--submenu", cut.Find("[role=menu]").ClassList);
	}

	private IRenderedComponent<MokaContextMenu> RenderMenu(IReadOnlyList<MokaContextMenuItem> items) =>
		Render<MokaContextMenu>(p => p
			.Add(x => x.Items, items)
			.Add(x => x.Visible, true)
			.Add(x => x.OnClose, () => _closes++));
}
