using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using static Moka.Red.ContextMenu.Tests.MenuDom;

namespace Moka.Red.ContextMenu.Tests.Components;

// The row under the mouse is lit in CSS through moka-hover.js, and reaches .NET only just before a key, so a mouse
// and a keyboard never light two rows. These check the .NET side of that.
public class MokaContextMenuPointerTests : BunitContext
{
	private const string MenuModule = "./_content/Moka.Red.ContextMenu/MokaContextMenu.razor.js";

	private readonly List<string> _chosen = [];
	private readonly BunitJSModuleInterop _module;

	public MokaContextMenuPointerTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(MenuModule);
	}

	[Fact]
	public void Rows_CarryTheIndexTheKeyboardCounts_AndDividersNone()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu([Item("Cut"), MokaContextMenuItems.Divider(), Item("Copy"), Item("Paste")]);

		Assert.Equal(["0", "1", "2"], Rows(Root(cut)).Select(row => row.GetAttribute("data-moka-index")));
		Assert.False(cut.Find(".moka-ctx-divider").HasAttribute("data-moka-index"));
	}

	[Fact]
	public void Opening_TracksTheMouseOverTheMenu_Once()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu([Item("Cut"), Item("Copy")]);

		JSRuntimeInvocation track = Assert.Single(_module.Invocations, i => i.Identifier == "trackHover");
		Assert.Equal(PreventKeysTarget(0), Assert.IsType<ElementReference>(track.Arguments[0]));
		Assert.IsType<DotNetObjectReference<MokaContextMenu>>(track.Arguments[1]);

		cut.Render(p => p.Add(x => x.X, 120));
		Assert.Single(_module.Invocations, i => i.Identifier == "trackHover");
	}

	[Fact]
	public async Task EachSubmenu_IsTrackedWhenItOpens()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(ShareMenu());

		await Press(cut, "End");
		await Press(cut, "ArrowRight");

		JSRuntimeInvocation[] tracked = [.. _module.Invocations.Where(i => i.Identifier == "trackHover")];
		Assert.Equal(2, tracked.Length);
		Assert.Equal(PreventKeysTarget(1), Assert.IsType<ElementReference>(tracked[1].Arguments[0]));
	}

	[Fact]
	public async Task TheRowUnderTheMouse_IsWhereTheNextKeyStarts()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu([Item("Cut"), Item("Copy"), Item("Paste")]);
		await Press(cut, "ArrowDown");
		Assert.Equal("Cut", Highlighted(Root(cut)));

		await cut.InvokeAsync(() => cut.Instance.OnItemHovered(1));
		await Press(cut, "ArrowDown");

		Assert.Equal("Paste", Highlighted(Root(cut)));
	}

	[Fact]
	public async Task EnterAfterHovering_ChoosesTheRowUnderTheMouse()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu([Item("Cut"), Item("Copy"), Item("Paste")]);

		await cut.InvokeAsync(() => cut.Instance.OnItemHovered(2));
		await Press(cut, "Enter");

		Assert.Equal(["Paste"], _chosen);
	}

	[Fact]
	public async Task Hovering_ChangesNothingOnScreen_UntilAKey()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu([Item("Cut"), Item("Copy")]);
		string before = cut.Markup;

		await cut.InvokeAsync(() => cut.Instance.OnItemHovered(1));

		Assert.Equal(before, cut.Markup);
	}

	[Fact]
	public async Task ADisabledRowOrAnIndexOutOfRange_IsIgnored()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu([Item("Cut"), Item("Copy", disabled: true), Item("Paste")]);
		await Press(cut, "ArrowDown");

		await cut.InvokeAsync(() => cut.Instance.OnItemHovered(1));
		await cut.InvokeAsync(() => cut.Instance.OnItemHovered(7));
		await cut.InvokeAsync(() => cut.Instance.OnItemHovered(-1));
		await Press(cut, "Enter");

		Assert.Equal(["Cut"], _chosen);
	}

	// A submenu is a DOM child of its parent, so the parent's tracking reports the submenu's rows too.
	[Fact]
	public async Task ASubmenusRows_AreNumberedAfterItsParents_SoTheParentIgnoresThem()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(ShareMenu());
		await Press(cut, "End");
		await Press(cut, "ArrowRight");
		IElement submenu = Menus(cut)[1];
		Assert.Equal(["3", "4"], Rows(submenu).Select(row => row.GetAttribute("data-moka-index")));

		// What the parent's tracking reports with the mouse on "Copy link".
		int reported = int.Parse(Rows(submenu)[0].GetAttribute("data-moka-index")!, System.Globalization.CultureInfo.InvariantCulture);
		await cut.InvokeAsync(() => cut.Instance.OnItemHovered(reported));
		await Root(cut).KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });

		Assert.Equal("Rename", Highlighted(Root(cut)));
	}

	[Fact]
	public async Task ASubmenu_TakesItsOwnRowsFromThePointer()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(ShareMenu());
		await Press(cut, "End");
		await Press(cut, "ArrowRight");
		IRenderedComponent<MokaContextMenu> submenu = cut.FindComponents<MokaContextMenu>().Single(menu => menu.Instance.IsSubmenu);

		await submenu.InvokeAsync(() => submenu.Instance.OnItemHovered(4));
		await Menus(cut)[1].KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal(["Email"], _chosen);
	}

	private IRenderedComponent<MokaContextMenu> RenderMenu(IReadOnlyList<MokaContextMenuItem> items) =>
		Render<MokaContextMenu>(p => p
			.Add(x => x.Items, items)
			.Add(x => x.Visible, true)
			.Add(x => x.X, 100)
			.Add(x => x.Y, 100)
			.Add(x => x.OnClose, () => { }));

	private MokaContextMenuItem Item(string text, bool disabled = false) =>
		new() { Text = text, Disabled = disabled, OnClickSync = () => _chosen.Add(text) };

	private MokaContextMenuItem[] ShareMenu() =>
	[
		Item("Open"),
		Item("Rename"),
		new() { Text = "Share", Children = [Item("Copy link"), Item("Email")] }
	];

	private static IElement Root(IRenderedComponent<MokaContextMenu> cut) => Menus(cut)[0];

	private static Task Press(IRenderedComponent<MokaContextMenu> cut, string key) =>
		Menus(cut)[^1].KeyDownAsync(new KeyboardEventArgs { Key = key });

	private ElementReference PreventKeysTarget(int nth) =>
		Assert.IsType<ElementReference>(_module.Invocations.Where(i => i.Identifier == "preventKeys").ElementAt(nth).Arguments[0]);
}
