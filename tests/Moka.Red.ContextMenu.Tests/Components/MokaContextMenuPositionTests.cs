using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using static Moka.Red.ContextMenu.Tests.MenuDom;

namespace Moka.Red.ContextMenu.Tests.Components;

// Every menu measures 200px wide in a 1000 by 600 viewport, and keeps an 8px margin from its edges.
// constrainToViewport is left to the loose fake, which returns nothing, so a menu stays where the
// component put it. The clamp itself runs in JS, so it is checked through what the component asks for.
public class MokaContextMenuPositionTests : BunitContext
{
	private const string MenuModule = "./_content/Moka.Red.ContextMenu/MokaContextMenu.razor.js";

	private static readonly MokaContextMenuItem[] Copy = [new() { Text = "Copy" }];

	private readonly BunitJSModuleInterop _module;

	public MokaContextMenuPositionTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(MenuModule);
	}

	[Fact]
	public void AMenuThatFitsBelowThePointer_OpensThere()
	{
		SetMenuHeight(150);

		IRenderedComponent<MokaContextMenu> cut = RenderMenu(100, 440, Copy);

		AssertAt(Menus(cut)[0], 100, 440);
	}

	// It used to be pushed up until it covered the pointer.
	[Fact]
	public void AMenuThatDoesNotFitBelowThePointer_OpensAboveIt()
	{
		SetMenuHeight(150);

		IRenderedComponent<MokaContextMenu> cut = RenderMenu(100, 500, Copy);

		AssertAt(Menus(cut)[0], 100, 350);
	}

	[Fact]
	public void WithRoomNeitherBelowNorAbove_TheMenuIsLeftToTheClamp()
	{
		// Below needs a top of 192 or less, above needs a pointer at 408 or more.
		SetMenuHeight(400);

		RenderMenu(100, 300, Copy);

		JSRuntimeInvocation clamp = _module.VerifyInvoke("constrainToViewport");
		Assert.Equal([100d, 300d, 200d, 400d, 8d], clamp.Arguments);
	}

	// A submenu sits beside its item, so clamping it up covers nothing and it does not flip up.
	[Fact]
	public async Task ASubmenu_FlipsToTheLeftOfItsParent_ButNotAboveItsItem()
	{
		SetMenuHeight(150);
		_module.Setup<MokaContextMenu.ItemAnchor?>("measureItemAnchor", _ => true)
			.SetResult(new MokaContextMenu.ItemAnchor { MenuLeft = 700, MenuRight = 900, ItemTop = 520 });
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(700, 380,
			[new() { Text = "Share", Children = [new MokaContextMenuItem { Text = "Email" }] }]);

		await Menus(cut)[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		await Menus(cut)[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });

		// To the right it would end at 1100. Its top is the row's top less the 6px nudge.
		IElement submenu = Menus(cut)[1];
		AssertAt(submenu, 500, 514);
	}

	private IRenderedComponent<MokaContextMenu> RenderMenu(double x, double y, IReadOnlyList<MokaContextMenuItem> items) =>
		Render<MokaContextMenu>(p => p
			.Add(m => m.Items, items)
			.Add(m => m.Visible, true)
			.Add(m => m.X, x)
			.Add(m => m.Y, y)
			.Add(m => m.OnClose, () => { }));

	private void SetMenuHeight(double height) =>
		_module.Setup<MokaContextMenu.MenuMetrics?>("measureMenu", _ => true)
			.SetResult(new MokaContextMenu.MenuMetrics
			{
				Width = 200,
				Height = height,
				ViewportWidth = 1000,
				ViewportHeight = 600
			});
}
