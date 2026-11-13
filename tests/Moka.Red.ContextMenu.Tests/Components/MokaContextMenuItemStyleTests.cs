using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.ContextMenu.Extensions;
using Moka.Red.Tests.Shared;

namespace Moka.Red.ContextMenu.Tests.Components;

// MokaArk's design audit: on the new design the menu that held keyboard focus drew the page's focus ring and its
// sub-menu didn't, and the rows were medium weight in full ink. Both answers are options, so a menu looks as it did
// until an app asks.
public class MokaContextMenuItemStyleTests : BunitContext
{
	private const string Css = "MokaContextMenu.razor.css";

	public MokaContextMenuItemStyleTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaContextMenu();
	}

	private IMokaContextMenuService Menu => Services.GetRequiredService<IMokaContextMenuService>();

	[Fact]
	public async Task AMenu_KeepsTodaysClasses_ByDefault()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();
		await OpenWithSubmenuAsync(cut);

		IReadOnlyList<IElement> menus = cut.FindAll("[role=menu]");
		Assert.Equal(2, menus.Count);
		Assert.Equal(["moka-ctx-menu"], menus[0].ClassList.Where(c => c.StartsWith("moka-ctx-menu", StringComparison.Ordinal)));
		Assert.Equal(["moka-ctx-menu", "moka-ctx-menu--submenu"],
			menus[1].ClassList.Where(c => c.StartsWith("moka-ctx-menu", StringComparison.Ordinal)));
	}

	[Fact]
	public async Task TheHostsOptions_ReachTheMenuAndItsSubmenu()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>(p => p
			.Add(x => x.ItemStyle, MokaContextMenuItemStyle.Quiet)
			.Add(x => x.FocusRing, false));
		await OpenWithSubmenuAsync(cut);

		IReadOnlyList<IElement> menus = cut.FindAll("[role=menu]");
		Assert.Equal(2, menus.Count);
		Assert.All(menus, menu =>
		{
			Assert.Contains("moka-ctx-menu--quiet", menu.ClassList);
			Assert.Contains("moka-ctx-menu--row-focus", menu.ClassList);
		});
	}

	[Fact]
	public void EachOption_StandsOnItsOwn()
	{
		IRenderedComponent<MokaContextMenu> quiet = Render<MokaContextMenu>(p => p
			.Add(x => x.Items, [new MokaContextMenuItem { Text = "Copy" }])
			.Add(x => x.Visible, true)
			.Add(x => x.ItemStyle, MokaContextMenuItemStyle.Quiet));
		Assert.Contains("moka-ctx-menu--quiet", quiet.Find("[role=menu]").ClassList);
		Assert.DoesNotContain("moka-ctx-menu--row-focus", quiet.Find("[role=menu]").ClassList);

		IRenderedComponent<MokaContextMenu> noRing = Render<MokaContextMenu>(p => p
			.Add(x => x.Items, [new MokaContextMenuItem { Text = "Copy" }])
			.Add(x => x.Visible, true)
			.Add(x => x.FocusRing, false));
		Assert.Contains("moka-ctx-menu--row-focus", noRing.Find("[role=menu]").ClassList);
		Assert.DoesNotContain("moka-ctx-menu--quiet", noRing.Find("[role=menu]").ClassList);
	}

	// The lit-row rules come later and outrank this one, so a lit row keeps full ink.
	[Fact]
	public void QuietRows_AreRegularWeightInTheSecondInk()
	{
		IReadOnlyDictionary<string, string> row = ScopedCss.Declarations(Css, ".moka-gate .moka-ctx-menu--quiet .moka-ctx-item");

		Assert.Equal("var(--moka-font-weight-normal)", row["font-weight"]);
		Assert.Equal("var(--moka-popup-color, var(--moka-color-on-surface-2))", row["color"]);
	}

	// moka.css draws a keyboard ring on anything with a tabindex on the new design, and wins over the menu's own
	// outline: none on order, at the same specificity.
	[Fact]
	public void WithoutTheRing_TheMenuDrawsNoOutline()
	{
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-gate .moka-ctx-menu--row-focus:focus-visible")["outline"]);
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-ctx-menu")["outline"]);
	}

	// The original design keeps its look whatever the options say.
	[Fact]
	public void TheOriginalDesign_HasNoRuleForEither()
	{
		Assert.Empty(ScopedCss.Declarations(Css, ".moka-ctx-menu--quiet .moka-ctx-item"));
		Assert.Empty(ScopedCss.Declarations(Css, ".moka-ctx-menu--row-focus:focus-visible"));
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
}
