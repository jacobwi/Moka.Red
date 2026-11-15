using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.ContextMenu.Extensions;
using Moka.Red.Tests.Shared;

namespace Moka.Red.ContextMenu.Tests.Components;

// A design audit: on the new design the rows were medium weight in full ink. Quiet rows are an option, so a menu looks
// as it did until an app asks.
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
			.Add(x => x.ItemStyle, MokaContextMenuItemStyle.Quiet));
		await OpenWithSubmenuAsync(cut);

		IReadOnlyList<IElement> menus = cut.FindAll("[role=menu]");
		Assert.Equal(2, menus.Count);
		Assert.All(menus, menu => Assert.Contains("moka-ctx-menu--quiet", menu.ClassList));
	}

	[Fact]
	public void QuietRows_AddOneClassToTheMenu()
	{
		IRenderedComponent<MokaContextMenu> quiet = Render<MokaContextMenu>(p => p
			.Add(x => x.Items, [new MokaContextMenuItem { Text = "Copy" }])
			.Add(x => x.Visible, true)
			.Add(x => x.ItemStyle, MokaContextMenuItemStyle.Quiet));

		Assert.Equal(["moka-ctx-menu", "moka-ctx-menu--quiet"],
			quiet.Find("[role=menu]").ClassList.Where(c => c.StartsWith("moka-ctx-menu", StringComparison.Ordinal)));
	}

	// The lit-row rules come later and outrank this one, so a lit row keeps full ink.
	[Fact]
	public void QuietRows_AreRegularWeightInTheSecondInk()
	{
		IReadOnlyDictionary<string, string> row = ScopedCss.Declarations(Css, ".moka-gate .moka-ctx-menu--quiet .moka-ctx-item");

		Assert.Equal("var(--moka-font-weight-normal)", row["font-weight"]);
		Assert.Equal("var(--moka-popup-color, var(--moka-color-on-surface-2))", row["color"]);
	}

	// The menu keeps focus while its lit row (aria-activedescendant) shows where the keyboard is, so it turns its outline
	// off. The page's keyboard ring in moka.css weighs nothing, so this holds on the new design too; the ring used to win
	// on order and go round the menu that held focus but not round its sub-menu.
	[Fact]
	public void TheMenu_DrawsNoOutline_ItsLitRowShowsTheKeyboard() =>
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-ctx-menu")["outline"]);

	// The original design keeps its look whatever the options say.
	[Fact]
	public void TheOriginalDesign_HasNoQuietRule() =>
		Assert.Empty(ScopedCss.Declarations(Css, ".moka-ctx-menu--quiet .moka-ctx-item"));

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
