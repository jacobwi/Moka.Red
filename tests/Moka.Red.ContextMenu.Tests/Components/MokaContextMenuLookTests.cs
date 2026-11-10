using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.ContextMenu.Extensions;
using Moka.Red.Core.Theming;

namespace Moka.Red.ContextMenu.Tests.Components;

// The theme's popup look reaches the menu through the provider's root class. A host with a Look of its own puts that
// class on the menu, and on its sub-menus, so the menu's variables win over the root's.
public class MokaContextMenuLookTests : BunitContext
{
	public MokaContextMenuLookTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaContextMenu();
	}

	private IMokaContextMenuService Menu => Services.GetRequiredService<IMokaContextMenuService>();

	[Theory]
	[InlineData(MokaPopupLook.Glass, "moka-popups-glass")]
	[InlineData(MokaPopupLook.AccentTint, "moka-popups-accent-tint")]
	[InlineData(MokaPopupLook.Standard, "moka-popups-standard")]
	public async Task AHostsLook_IsAClassOnTheMenuAndItsSubmenu(MokaPopupLook look, string expected)
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>(p => p.Add(x => x.Look, look));
		await OpenWithSubmenuAsync(cut);

		IReadOnlyList<IElement> menus = cut.FindAll("[role=menu]");
		Assert.Equal(2, menus.Count);
		Assert.All(menus, menu => Assert.Equal([expected], LookClasses(menu)));
	}

	[Fact]
	public async Task AHostWithoutALook_LeavesItToTheTheme()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>(p => p
			.AddCascadingValue(MokaTheme.Default.WithPopups(MokaPopupLook.Terminal)));
		await OpenWithSubmenuAsync(cut);

		Assert.All(cut.FindAll("[role=menu]"), menu => Assert.Empty(LookClasses(menu)));
	}

	private async Task OpenWithSubmenuAsync(IRenderedComponent<MokaContextMenuHost> cut)
	{
		await cut.InvokeAsync(() => Menu.Show(10, 10,
		[
			new() { Text = "Share", Children = [new MokaContextMenuItem { Text = "Email" }] }
		]));
		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
	}

	private static List<string> LookClasses(IElement element) =>
		[.. element.ClassList.Where(c => c.StartsWith("moka-popups-", StringComparison.Ordinal))];
}
