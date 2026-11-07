using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Tests.Shared;
using static Moka.Red.ContextMenu.Tests.MenuDom;
using static Moka.Red.ContextMenu.Tests.RadialDom;

namespace Moka.Red.ContextMenu.Tests.Components;

// A menu is position: fixed at viewport coordinates, and an ancestor with a transform, a filter or containment became
// what those coordinates were measured from, so a trigger's own menu inside a card that had animated in opened in the
// wrong place. The open menus now go to the top layer through Core's moka-popup.js, with no anchor, since they place
// themselves. What that does in the browser can't run here, so these tests cover what the menus render for it and
// when they call it.
public class TopLayerMenuTests : BunitContext
{
	private const string MenuModule = "./_content/Moka.Red.ContextMenu/MokaContextMenu.razor.js";

	private static readonly MokaContextMenuItem[] Edit =
	[
		new() { Text = "Copy" },
		new() { Text = "Share", Children = [new MokaContextMenuItem { Text = "Email" }] }
	];

	private readonly BunitJSModuleInterop _menu;
	private readonly BunitJSModuleInterop _popup;

	public TopLayerMenuTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_menu = JSInterop.SetupModule(MenuModule);
		_popup = TopLayerPopup.Setup(JSInterop);
	}

	[Fact]
	public void AnOpenMenu_IsLifted_WithItsBackdropUnderIt()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu();

		TopLayerPopup.AssertLifted(_popup, Menus(cut)[0], cut.Find(".moka-ctx-backdrop"));
		Assert.Equal(1, TopLayerPopup.Count(_popup, "open"));
	}

	// Measured where it would stand inside a transformed ancestor, the clamp would put it somewhere else.
	[Fact]
	public void TheMenu_IsLifted_BeforeItIsMeasured()
	{
		_menu.Setup<MokaContextMenu.MenuMetrics?>("measureMenu", _ => true);

		IRenderedComponent<MokaContextMenu> cut = RenderMenu();

		Assert.Single(_menu.Invocations["measureMenu"]);
		TopLayerPopup.AssertLifted(_popup, Menus(cut)[0], cut.Find(".moka-ctx-backdrop"));
	}

	[Fact]
	public void TheMenu_KeepsPlacingItself()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu();

		AssertAt(Menus(cut)[0], 120, 80);
	}

	[Fact]
	public async Task RendersThatChangeNothing_SendNothing()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu();

		await Menus(cut)[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		await Menus(cut)[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

		Assert.Equal(1, TopLayerPopup.Count(_popup, "open"));
		Assert.Equal(0, TopLayerPopup.Count(_popup, "close"));
	}

	[Fact]
	public async Task ASubmenu_IsLiftedToo_WithoutABackdrop()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu();
		string rootKey = TopLayerPopup.OpenedKey(_popup);

		await Menus(cut)[0].KeyDownAsync(new KeyboardEventArgs { Key = "End" });
		await Menus(cut)[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });

		Assert.Equal(2, TopLayerPopup.Count(_popup, "open"));
		TopLayerPopup.AssertLifted(_popup, Menus(cut)[1], null);
		Assert.NotEqual(rootKey, TopLayerPopup.OpenedKey(_popup));
	}

	[Fact]
	public async Task ClosingASubmenu_TakesOnlyItOut()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu();
		string rootKey = TopLayerPopup.OpenedKey(_popup);
		await Menus(cut)[0].KeyDownAsync(new KeyboardEventArgs { Key = "End" });
		await Menus(cut)[0].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
		string submenuKey = TopLayerPopup.OpenedKey(_popup);

		await Menus(cut)[1].KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft" });

		JSRuntimeInvocation close = Assert.Single(_popup.Invocations["close"]);
		Assert.Equal(submenuKey, close.Arguments[0]);
		Assert.NotEqual(rootKey, submenuKey);
	}

	[Fact]
	public void Hiding_TakesTheMenuOut_AndShowingAgain_LiftsTheNewOne()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu();
		string key = TopLayerPopup.OpenedKey(_popup);

		cut.Render(p => p.Add(m => m.Visible, false));
		TopLayerPopup.AssertClosed(_popup);

		cut.Render(p => p.Add(m => m.Visible, true));
		Assert.Equal(2, TopLayerPopup.Count(_popup, "open"));
		Assert.Equal(key, TopLayerPopup.OpenedKey(_popup));
		TopLayerPopup.AssertLifted(_popup, Menus(cut)[0], cut.Find(".moka-ctx-backdrop"));
	}

	[Fact]
	public async Task Disposal_TakesAnOpenMenuOut()
	{
		RenderMenu();

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	[Fact]
	public async Task AClosedMenu_SendsNothing()
	{
		Render<MokaContextMenu>(p => p.Add(m => m.Items, Edit));

		await DisposeComponentsAsync();

		Assert.Equal(0, TopLayerPopup.Count(_popup, "open"));
		Assert.Equal(0, TopLayerPopup.Count(_popup, "close"));
	}

	[Fact]
	public void AnOpenRing_IsLifted_WithItsBackdropUnderIt()
	{
		JSInterop.SetupModule(Module);

		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		TopLayerPopup.AssertLifted(_popup, Ring(cut), cut.Find(".moka-radial-menu-backdrop"));
	}

	// The ring is centred with percentages of the window, which a transformed ancestor would have replaced with its own.
	[Fact]
	public void TheRing_IsLifted_BeforeItTouchesFocus()
	{
		BunitJSModuleInterop ring = JSInterop.SetupModule(Module);
		ring.Setup<string?>("captureFocus", _ => true);

		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		Assert.Single(ring.Invocations["captureFocus"]);
		TopLayerPopup.AssertLifted(_popup, Ring(cut), cut.Find(".moka-radial-menu-backdrop"));
	}

	[Fact]
	public async Task ClosingTheRing_TakesItOut_AndDisposalDoesNotAgain()
	{
		JSInterop.SetupModule(Module);
		IRenderedComponent<MokaRadialMenu> cut = RenderRing();

		await Ring(cut).KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		TopLayerPopup.AssertClosed(_popup);

		await DisposeComponentsAsync();
		Assert.Equal(1, TopLayerPopup.Count(_popup, "close"));
	}

	[Fact]
	public async Task Disposal_TakesAnOpenRingOut()
	{
		JSInterop.SetupModule(Module);
		RenderRing();

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	private IRenderedComponent<MokaContextMenu> RenderMenu() => Render<MokaContextMenu>(p => p
		.Add(m => m.Items, Edit)
		.Add(m => m.Visible, true)
		.Add(m => m.X, 120)
		.Add(m => m.Y, 80)
		.Add(m => m.OnClose, EventCallback.Factory.Create(this, () => { })));

	private IRenderedComponent<MokaRadialMenu> RenderRing() => Render<MokaRadialMenu>(p => p
		.Add(m => m.Spec, Device())
		.Add(m => m.Visible, true)
		.Add(m => m.X, 300)
		.Add(m => m.Y, 300));
}
