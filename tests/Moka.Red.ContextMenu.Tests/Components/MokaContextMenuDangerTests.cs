using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.ContextMenu.Extensions;
using Moka.Red.Core.Enums;
using Moka.Red.Icons;
using static Moka.Red.ContextMenu.Tests.MenuDom;

namespace Moka.Red.ContextMenu.Tests.Components;

// The menu is drawn by the one host, so an app's own scoped CSS can't reach a row: the danger look is a class the menu
// writes from the item and styles in its own stylesheet.
public class MokaContextMenuDangerTests : BunitContext
{
	public MokaContextMenuDangerTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IElement RenderMenu(params MokaContextMenuItem[] items) =>
		Render<MokaContextMenu>(p => p
			.Add(x => x.Items, items)
			.Add(x => x.Visible, true)).Find("[role=menu]");

	[Fact]
	public void ADangerItem_CarriesTheClass_AndNoOtherRowDoes()
	{
		IElement menu = RenderMenu(
			new MokaContextMenuItem { Text = "Rename" },
			new MokaContextMenuItem { Text = "Delete", Icon = MokaIcons.Action.Delete, Danger = true, DividerBefore = true });

		Assert.Equal("moka-ctx-item", Row(menu, "Rename").GetAttribute("class"));
		Assert.Equal("moka-ctx-item moka-ctx-item--danger", Row(menu, "Delete").GetAttribute("class"));
	}

	[Fact]
	public void TheItemsOwnClass_StillComesLast() =>
		Assert.Equal("moka-ctx-item moka-ctx-item--danger app-row",
			Row(RenderMenu(new MokaContextMenuItem { Text = "Delete", Danger = true, CssClass = "app-row" }), "Delete")
				.GetAttribute("class"));

	[Fact]
	public void ADangerRow_IsStillAPlainMenuItem()
	{
		IElement row = Row(RenderMenu(new MokaContextMenuItem { Text = "Delete", Danger = true }), "Delete");

		Assert.Equal("menuitem", row.GetAttribute("role"));
		Assert.False(row.HasAttribute("aria-disabled"));
		Assert.False(row.HasAttribute("style"));
	}

	[Fact]
	public async Task TheLitDangerRow_KeepsBothClasses()
	{
		IRenderedComponent<MokaContextMenu> cut = Render<MokaContextMenu>(p => p
			.Add(x => x.Items, [new MokaContextMenuItem { Text = "Delete", Danger = true }])
			.Add(x => x.Visible, true));

		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

		IElement row = Row(cut.Find("[role=menu]"), "Delete");
		Assert.Contains("moka-ctx-item--focused", row.ClassList);
		Assert.Contains("moka-ctx-item--danger", row.ClassList);
	}

	[Fact]
	public void AColoredDangerItem_KeepsItsIconColor()
	{
		IElement row = Row(RenderMenu(new MokaContextMenuItem
		{
			Text = "Red label",
			Icon = MokaIcons.Action.Delete,
			Label = MokaLabelColor.Red,
			Danger = true
		}), "Red label");

		IElement icon = row.QuerySelector(".moka-ctx-icon--colored")!;
		Assert.EndsWith("color: var(--moka-label-red)", icon.GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task TheServicesMenu_DrawsADangerSubmenuItem()
	{
		Services.AddMokaContextMenu();
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();
		IMokaContextMenuService menu = Services.GetRequiredService<IMokaContextMenuService>();

		await cut.InvokeAsync(() => menu.Show(10, 10,
		[
			new() { Text = "Share", Children = [new MokaContextMenuItem { Text = "Revoke all links", Danger = true }] }
		]));
		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });

		IReadOnlyList<IElement> menus = Menus(cut);
		Assert.Equal(2, menus.Count);
		Assert.DoesNotContain("moka-ctx-item--danger", Row(menus[0], "Share").ClassList);
		Assert.Contains("moka-ctx-item--danger", Row(menus[1], "Revoke all links").ClassList);
	}
}
