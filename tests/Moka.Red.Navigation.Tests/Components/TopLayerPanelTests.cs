using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Navigation.AppShell;
using Moka.Red.Navigation.MenuBar;
using Moka.Red.Navigation.Tabs;
using Moka.Red.Navigation.Tabs.Models;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// A menu bar's popup drawn inside its menu was cut off by a header's overflow and painted under any later element
// with a stacking context of its own, and so was the account card's menu by the side menu. The tab strip's own menu
// was placed at window coordinates that a transformed ancestor captured. They all go to the top layer through Core's
// moka-popup.js; what that does in the browser can't run here, so these tests cover what the menus render for it and
// when they call it.
public class TopLayerPanelTests : BunitContext
{
	private const string ShellModule = "./_content/Moka.Red.Navigation/AppShell/MokaAppShell.razor.js";
	private const string FocusCall = "Blazor._internal.domWrapper.focus";

	private readonly BunitJSModuleInterop _popup;

	public TopLayerPanelTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_popup = TopLayerPopup.Setup(JSInterop);
	}

	[Fact]
	public async Task MenuBarMenu_OpensItsPopupUnderItsMenu()
	{
		IRenderedComponent<MokaMenuBar> bar = Render<MokaMenuBar>(p => p.AddChildContent(Menus));
		string? fileId = bar.FindAll(".moka-menubar-menu")[0].GetAttribute("blazor:elementReference");

		await bar.FindAll(".moka-menubar-menu__trigger")[0].ClickAsync(new MouseEventArgs());

		TopLayerPopup.AssertOpened(_popup, bar.Find(".moka-menubar-menu__popup"), fileId, "bottom-start");
		Assert.Null(_popup.Invocations["open"][^1].Arguments[3]);
	}

	[Fact]
	public async Task TheLastMenu_HangsFromItsRightEdge()
	{
		IRenderedComponent<MokaMenuBar> bar = Render<MokaMenuBar>(p => p.AddChildContent(Menus));
		string? viewId = bar.FindAll(".moka-menubar-menu")[2].GetAttribute("blazor:elementReference");

		await bar.FindAll(".moka-menubar-menu__trigger")[2].ClickAsync(new MouseEventArgs());

		TopLayerPopup.AssertOpened(_popup, bar.Find(".moka-menubar-menu__popup"), viewId, "bottom-end");
	}

	[Fact]
	public async Task MovingToTheNextMenu_ClosesOnePopup_AndOpensTheOther()
	{
		IRenderedComponent<MokaMenuBar> bar = Render<MokaMenuBar>(p => p.AddChildContent(Menus));
		await bar.FindAll(".moka-menubar-menu__trigger")[0].ClickAsync(new MouseEventArgs());
		string fileKey = TopLayerPopup.OpenedKey(_popup);

		await bar.Find(".moka-menubar").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });

		Assert.Contains(_popup.Invocations["close"], call => fileKey.Equals(call.Arguments[0] as string, StringComparison.Ordinal));
		Assert.NotEqual(fileKey, TopLayerPopup.OpenedKey(_popup));
		Assert.Equal("Edit", bar.Find(".moka-menubar-menu__popup").GetAttribute("aria-label"));
	}

	[Fact]
	public async Task Escape_ClosesThePopup_AndDisposalClosesAnOpenOne()
	{
		IRenderedComponent<MokaMenuBar> bar = Render<MokaMenuBar>(p => p.AddChildContent(Menus));
		await bar.FindAll(".moka-menubar-menu__trigger")[0].ClickAsync(new MouseEventArgs());

		await bar.Find(".moka-menubar").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		TopLayerPopup.AssertClosed(_popup);
		Assert.Equal(1, TopLayerPopup.Count(_popup, "close"));

		await bar.FindAll(".moka-menubar-menu__trigger")[1].ClickAsync(new MouseEventArgs());
		await DisposeComponentsAsync();

		Assert.Equal(2, TopLayerPopup.Count(_popup, "close"));
	}

	[Fact]
	public void AnAccountMenu_OpensAboveTheCard_AsWideAsIt()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();
		string? rootId = cut.Find(".moka-account-card").GetAttribute("blazor:elementReference");

		cut.Find(".moka-account-card__card").Click();

		IReadOnlyDictionary<string, object?> options = TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-account-card__menu"), rootId, "top");
		Assert.Equal("match", options["width"]);
		IElement scrim = cut.Find(".moka-account-card__scrim");
		Assert.Equal("manual", scrim.GetAttribute("popover"));
		Assert.True(scrim.HasAttribute("data-moka-popup"));
		Assert.Equal(scrim.GetAttribute("blazor:elementReference"), Assert.IsType<ElementReference>(_popup.Invocations["open"][^1].Arguments[3]).Id);
		Assert.Single(JSInterop.Invocations[FocusCall]);
	}

	// An open call that hasn't answered yet holds the focus call back.
	[Fact]
	public void AnAccountMenu_IsShown_BeforeARowTakesFocus()
	{
		_popup.SetupVoid("open", _ => true);
		IRenderedComponent<MokaAccountCard> cut = Card();

		cut.Find(".moka-account-card__card").Click();

		Assert.Single(_popup.Invocations["open"]);
		Assert.Empty(JSInterop.Invocations[FocusCall]);
	}

	[Fact]
	public void AnAccountMenu_OnTheRail_OpensBesideIt_AtItsOwnWidth()
	{
		BunitJSModuleInterop shell = JSInterop.SetupModule(ShellModule);
		shell.Setup<bool>("showsRail", _ => true).SetResult(true);
		IRenderedComponent<MokaAppShell> cut = Render<MokaAppShell>(p => p.Add(x => x.Account, AccountCard));
		string? rootId = cut.Find(".moka-account-card").GetAttribute("blazor:elementReference");

		cut.Find(".moka-account-card__card").Click();

		IReadOnlyDictionary<string, object?> options = TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-account-card__menu"), rootId, "right-end");
		Assert.False(options.ContainsKey("width"));
	}

	[Fact]
	public void AnAccountMenu_InAShellShowingRows_OpensAboveTheCard()
	{
		BunitJSModuleInterop shell = JSInterop.SetupModule(ShellModule);
		shell.Setup<bool>("showsRail", _ => true).SetResult(false);
		IRenderedComponent<MokaAppShell> cut = Render<MokaAppShell>(p => p.Add(x => x.Account, AccountCard));
		string? rootId = cut.Find(".moka-account-card").GetAttribute("blazor:elementReference");

		cut.Find(".moka-account-card__card").Click();

		TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-account-card__menu"), rootId, "top");
	}

	[Fact]
	public async Task Escape_TakesTheAccountMenuOut_AndDisposalAnOpenOne()
	{
		IRenderedComponent<MokaAccountCard> cut = Card();
		await cut.Find(".moka-account-card__card").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-account-card").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		TopLayerPopup.AssertClosed(_popup);
		Assert.Equal(1, TopLayerPopup.Count(_popup, "close"));

		await cut.Find(".moka-account-card__card").ClickAsync(new MouseEventArgs());
		await DisposeComponentsAsync();

		Assert.Equal(2, TopLayerPopup.Count(_popup, "close"));
	}

	[Fact]
	public void AClosedAccountCard_SendsNothing()
	{
		Card();

		Assert.Equal(0, TopLayerPopup.Count(_popup, "open"));
	}

	[Fact]
	public void ATabMenu_IsLifted_WithItsOverlayUnderIt()
	{
		IRenderedComponent<MokaTabContextMenu<string>> cut = TabMenu();

		TopLayerPopup.AssertLifted(_popup, cut.Find(".moka-context-menu"), cut.Find(".moka-context-menu-overlay"));
		Assert.Equal(1, TopLayerPopup.Count(_popup, "open"));
	}

	// Measured where it would stand inside a transformed ancestor of the strip, the clamp would put it somewhere else.
	// An open call that hasn't answered yet holds the measuring back.
	[Fact]
	public void ATabMenu_IsLifted_BeforeItIsMeasured()
	{
		BunitJSModuleInterop tabs = JSInterop.SetupModule("./_content/Moka.Red.Navigation/moka-tabs.js");
		_popup.SetupVoid("open", _ => true);

		IRenderedComponent<MokaTabContextMenu<string>> cut = TabMenu();

		TopLayerPopup.AssertLifted(_popup, cut.Find(".moka-context-menu"), cut.Find(".moka-context-menu-overlay"));
		Assert.Empty(tabs.Invocations["MokaTabs.constrainContextMenu"]);
	}

	[Fact]
	public void ATabMenu_KeepsPlacingItself()
	{
		IRenderedComponent<MokaTabContextMenu<string>> cut = TabMenu();

		Assert.Contains("left: 140px; top: 30px", cut.Find(".moka-context-menu").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task Disposal_TakesAnOpenTabMenuOut()
	{
		TabMenu();

		await DisposeComponentsAsync();

		TopLayerPopup.AssertClosed(_popup);
	}

	private IRenderedComponent<MokaAccountCard> Card() => Render<MokaAccountCard>(p => p
		.Add(x => x.Name, "BC")
		.Add(x => x.Items, [new MokaAccountItem("Sign out")]));

	private static void AccountCard(RenderTreeBuilder builder)
	{
		builder.OpenComponent<MokaAccountCard>(0);
		builder.AddComponentParameter(1, nameof(MokaAccountCard.Name), "BC");
		builder.AddComponentParameter(2, nameof(MokaAccountCard.Items), (IReadOnlyList<MokaAccountItem>)[new MokaAccountItem("Sign out")]);
		builder.CloseComponent();
	}

	private IRenderedComponent<MokaTabContextMenu<string>> TabMenu() => Render<MokaTabContextMenu<string>>(p => p
		.Add(x => x.Visible, true)
		.Add(x => x.Tab, new TabInfo<string> { Id = "one", Title = "One" })
		.Add(x => x.X, 140)
		.Add(x => x.Y, 30));

	private static void Menus(RenderTreeBuilder builder)
	{
		Menu(builder, 0, "File", ["New", "Open"]);
		Menu(builder, 20, "Edit", ["Undo"]);
		Menu(builder, 40, "View", ["Zoom in"]);
	}

	private static void Menu(RenderTreeBuilder builder, int seq, string label, string[] items)
	{
		builder.OpenComponent<MokaMenuBarMenu>(seq);
		builder.AddComponentParameter(seq + 1, nameof(MokaMenuBarMenu.Label), label);
		builder.AddComponentParameter(seq + 2, nameof(MokaMenuBarMenu.ChildContent), (RenderFragment)(inner =>
		{
			int itemSeq = 0;
			foreach (string text in items)
			{
				inner.OpenComponent<MokaMenuBarItem>(itemSeq);
				inner.AddComponentParameter(itemSeq + 1, nameof(MokaMenuBarItem.Text), text);
				inner.CloseComponent();
				itemSeq += 10;
			}
		}));
		builder.CloseComponent();
	}
}
