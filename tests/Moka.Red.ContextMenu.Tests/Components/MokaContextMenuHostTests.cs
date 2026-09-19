using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.ContextMenu.Extensions;

namespace Moka.Red.ContextMenu.Tests.Components;

public class MokaContextMenuHostTests : BunitContext
{
	private const string MenuModule = "./_content/Moka.Red.ContextMenu/MokaContextMenu.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaContextMenuHostTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(MenuModule);
		Services.AddMokaContextMenu();
	}

	private IMokaContextMenuService Menu => Services.GetRequiredService<IMokaContextMenuService>();

	[Fact]
	public void RendersNothing_UntilAMenuIsShown()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();

		Assert.Empty(cut.FindAll("[role=menu]"));
		Assert.True(Menu.HasHost);
	}

	[Fact]
	public void Show_RendersTheSharedMenuAtThePosition()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();

		Menu.Show(120, 80, [new() { Text = "Copy" }, new() { Text = "Paste" }]);

		cut.WaitForAssertion(() =>
		{
			IElement menu = cut.Find("[role=menu]");
			Assert.Equal(["Copy", "Paste"], MenuDom.Rows(menu).Select(MenuDom.Text));
			MenuDom.AssertAt(menu, 120, 80);
		});
	}

	[Fact]
	public async Task ChoosingAnItem_RunsIt_AndClosesTheSharedMenu()
	{
		string? chosen = null;
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();
		await cut.InvokeAsync(() => Menu.Show(10, 10, [new() { Text = "Copy", OnClickSync = () => chosen = "Copy" }]));

		await MenuDom.Row(cut.Find("[role=menu]"), "Copy").ClickAsync(new MouseEventArgs());

		Assert.Equal("Copy", chosen);
		Assert.False(Menu.Visible);
		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public async Task Escape_ClosesTheSharedMenu()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();
		await cut.InvokeAsync(() => Menu.Show(10, 10, [new() { Text = "Copy" }]));

		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.False(Menu.Visible);
		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public async Task AClickOutside_ClosesTheSharedMenu()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();
		await cut.InvokeAsync(() => Menu.Show(10, 10, [new() { Text = "Copy" }]));

		await cut.Find(".moka-ctx-backdrop").ClickAsync(new MouseEventArgs());

		Assert.False(Menu.Visible);
		Assert.Empty(cut.FindAll(".moka-ctx-backdrop"));
	}

	// Up to 0.1.11 the menu waited for the action, so an action awaiting a dialog left it open.
	[Fact]
	public async Task TheMenuCloses_BeforeAnItemsActionFinishes()
	{
		var pending = new TaskCompletionSource();
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();
		await cut.InvokeAsync(() => Menu.Show(10, 10, [new() { Text = "Delete", OnClick = () => pending.Task }]));

		Task click = MenuDom.Row(cut.Find("[role=menu]"), "Delete").ClickAsync(new MouseEventArgs());

		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role=menu]")));
		Assert.False(Menu.Visible);
		Assert.False(click.IsCompleted);

		pending.SetResult();
		await click;
	}

	[Fact]
	public async Task Closing_HandsFocusBackToWhatHadIt()
	{
		_module.Setup<string?>("captureFocus", _ => true).SetResult("focus-1");
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();
		await cut.InvokeAsync(() => Menu.Show(10, 10, [new() { Text = "Copy" }]));

		await cut.InvokeAsync(Menu.Close);

		Assert.Equal("focus-1", _module.VerifyInvoke("restoreFocus").Arguments[0]);
	}

	[Fact]
	public async Task Tab_ClosesTheSharedMenu_AndHandsFocusBack()
	{
		_module.Setup<string?>("captureFocus", _ => true).SetResult("focus-1");
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();
		await cut.InvokeAsync(() => Menu.Show(10, 10, [new() { Text = "Copy" }]));

		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "Tab" });

		Assert.False(Menu.Visible);
		Assert.Empty(cut.FindAll("[role=menu]"));
		Assert.Equal("focus-1", _module.VerifyInvoke("restoreFocus").Arguments[0]);
	}

	[Fact]
	public async Task TabInASubmenu_ClosesTheWholeSharedMenu_AndHandsFocusBack()
	{
		_module.Setup<string?>("captureFocus", _ => true).SetResult("focus-1");
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();
		await cut.InvokeAsync(() => Menu.Show(10, 10,
		[
			new() { Text = "Share", Children = [new MokaContextMenuItem { Text = "Email" }] }
		]));
		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });

		await cut.FindAll("[role=menu]")[1].KeyDownAsync(new KeyboardEventArgs { Key = "Tab" });

		Assert.False(Menu.Visible);
		Assert.Empty(cut.FindAll("[role=menu]"));
		Assert.Equal("focus-1", _module.VerifyInvoke("restoreFocus").Arguments[0]);
	}

	// The context-menu key and Shift+F10 fire contextmenu with no button, and Enter or Space on a
	// button clicks with no click count.
	[Theory]
	[InlineData("contextmenu", 0, 0)]
	[InlineData("click", 0, 0)]
	public async Task AnEventTheKeyboardCaused_OpensTheSharedMenuOnItsFirstEnabledItem(string type, long detail, long button)
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();

		await cut.InvokeAsync(() => Menu.Show(new MouseEventArgs { Type = type, Detail = detail, Button = button },
			[new() { Text = "Cut", Disabled = true }, new() { Text = "Copy" }, new() { Text = "Paste" }]));

		Assert.Equal("Copy", MenuDom.Highlighted(cut.Find("[role=menu]")));
	}

	// A right click in Chromium also reports Detail 0, so the button is what tells it apart.
	[Theory]
	[InlineData("contextmenu", 0, 2)]
	[InlineData("contextmenu", 1, 2)]
	[InlineData("click", 1, 0)]
	public async Task AnEventThePointerCaused_OpensTheSharedMenuWithNothingHighlighted(string type, long detail, long button)
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();

		await cut.InvokeAsync(() => Menu.Show(new MouseEventArgs { Type = type, Detail = detail, Button = button },
			[new() { Text = "Copy" }, new() { Text = "Paste" }]));

		Assert.Null(MenuDom.Highlighted(cut.Find("[role=menu]")));
	}

	// Enter on the highlighted item used to do nothing after a keyboard open.
	[Fact]
	public async Task EnterRightAfterAKeyboardOpen_ChoosesTheFirstEnabledItem()
	{
		string? chosen = null;
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();
		await cut.InvokeAsync(() => Menu.Show(new MouseEventArgs { Type = "contextmenu" },
			[new() { Text = "Copy", OnClickSync = () => chosen = "Copy" }]));

		await cut.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal("Copy", chosen);
		Assert.False(Menu.Visible);
	}

	[Fact]
	public async Task DisposingTheHost_UnregistersIt_AndClosesTheMenu()
	{
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();
		await cut.InvokeAsync(() => Menu.Show(10, 10, [new() { Text = "Copy" }]));

		await DisposeComponentsAsync();

		Assert.False(Menu.HasHost);
		Assert.False(Menu.Visible);
	}
}
