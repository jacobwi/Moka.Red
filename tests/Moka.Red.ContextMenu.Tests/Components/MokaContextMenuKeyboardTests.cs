using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using static Moka.Red.ContextMenu.Tests.MenuDom;

namespace Moka.Red.ContextMenu.Tests.Components;

// Focus stays on a menu element and aria-activedescendant names the highlighted row, so "focus"
// here is the FocusAsync call a menu makes on itself. The menus are told apart by the element each
// one bound with preventKeys when it opened: the root first, then each submenu as it opens.
public class MokaContextMenuKeyboardTests : BunitContext
{
	private const string MenuModule = "./_content/Moka.Red.ContextMenu/MokaContextMenu.razor.js";

	private static readonly TimeSpan HoverWait = TimeSpan.FromSeconds(5);

	private readonly List<string> _chosen = [];
	private readonly BunitJSModuleInterop _module;
	private int _closes;

	public MokaContextMenuKeyboardTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(MenuModule);
	}

	[Fact]
	public async Task DownAndUp_SkipDisabledItems_AndWrap()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu([Item("Cut"), Item("Copy", disabled: true), Item("Paste")]);

		await Press(cut, "ArrowDown");
		Assert.Equal("Cut", Highlighted(Root(cut)));

		await Press(cut, "ArrowDown");
		Assert.Equal("Paste", Highlighted(Root(cut)));

		await Press(cut, "ArrowDown");
		Assert.Equal("Cut", Highlighted(Root(cut)));

		await Press(cut, "ArrowUp");
		Assert.Equal("Paste", Highlighted(Root(cut)));
	}

	[Fact]
	public async Task HomeAndEnd_HighlightTheFirstAndLastEnabledItems()
	{
		IRenderedComponent<MokaContextMenu> cut =
			RenderMenu([Item("Cut", disabled: true), Item("Copy"), Item("Paste"), Item("Delete", disabled: true)]);

		await Press(cut, "End");
		Assert.Equal("Paste", Highlighted(Root(cut)));

		await Press(cut, "Home");
		Assert.Equal("Copy", Highlighted(Root(cut)));
	}

	// End used to point aria-activedescendant at a row that does not exist.
	[Fact]
	public async Task End_WithEveryItemDisabled_HighlightsNothing()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu([Item("Cut", disabled: true), Item("Copy", disabled: true)]);

		await Press(cut, "End");
		await Press(cut, "Enter");

		Assert.False(Root(cut).HasAttribute("aria-activedescendant"));
		Assert.Empty(_chosen);
		Assert.Equal(0, _closes);
	}

	[Theory]
	[InlineData("Enter")]
	[InlineData(" ")]
	public async Task EnterOrSpace_ChoosesTheHighlightedItem_AndClosesTheMenu(string key)
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(FileMenu());

		await Press(cut, "ArrowDown");
		await Press(cut, key);

		Assert.Equal(["Open"], _chosen);
		Assert.Equal(1, _closes);
	}

	[Fact]
	public async Task EscapeInTheRootMenu_ClosesTheMenu()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(FileMenu());

		await Press(cut, "Escape");

		Assert.Equal(1, _closes);
	}

	// Tab used to leave the menu open while the browser moved focus to whatever followed it.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task TabOrShiftTab_ClosesTheMenu(bool shift)
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(FileMenu());
		await Press(cut, "ArrowDown");

		await Root(cut).KeyDownAsync(new KeyboardEventArgs { Key = "Tab", ShiftKey = shift });

		Assert.Equal(1, _closes);
		Assert.Empty(_chosen);
	}

	[Fact]
	public async Task TabInASubmenu_ClosesTheWholeMenu()
	{
		IRenderedComponent<MokaContextMenu> cut = await OpenShareFromKeyboard();

		await Submenu(cut).KeyDownAsync(Key("Tab"));

		Assert.Equal(1, _closes);
		Assert.Empty(_chosen);
	}

	// Closing puts focus back where it was, so the browser must not move it first. A menu that
	// cannot close leaves Tab alone, or focus would be stuck in it.
	[Fact]
	public void TheBrowsersTab_IsCancelledOnlyWhenTheMenuCanClose()
	{
		RenderMenu(FileMenu());
		Render<MokaContextMenu>(p => p
			.Add(x => x.Items, FileMenu())
			.Add(x => x.Visible, true));

		IReadOnlyList<JSRuntimeInvocation> bound = _module.VerifyInvoke("preventKeys", 2);
		Assert.Contains("Tab", CancelledKeys(bound[0]));
		Assert.DoesNotContain("Tab", CancelledKeys(bound[1]));
		Assert.Contains("ArrowDown", CancelledKeys(bound[1]));
	}

	// Enter on a button opens the menu, and the key can still be down when the menu takes focus.
	[Theory]
	[InlineData("Enter")]
	[InlineData(" ")]
	public async Task AHeldKey_DoesNotChooseTheHighlightedItem(string key)
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(FileMenu());
		await Press(cut, "ArrowDown");

		await Root(cut).KeyDownAsync(new KeyboardEventArgs { Key = key, Repeat = true });

		Assert.Empty(_chosen);
		Assert.Equal(0, _closes);

		await Press(cut, key);

		Assert.Equal(["Open"], _chosen);
	}

	// The context-menu key in the menu fires contextmenu on it, which showed the browser's own menu
	// over this one and could reach a trigger around it.
	[Fact]
	public void AContextMenuEventOnTheMenu_IsCancelled_AndGoesNoFurther()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(FileMenu());

		Assert.True(Root(cut).HasAttribute("blazor:oncontextmenu:preventDefault"));
		Assert.True(Root(cut).HasAttribute("blazor:oncontextmenu:stopPropagation"));
	}

	[Theory]
	[InlineData("ArrowRight")]
	[InlineData("Enter")]
	[InlineData(" ")]
	public async Task OpeningASubmenu_MovesFocusAndTheHighlightIntoIt(string key)
	{
		IRenderedComponent<MokaContextMenu> cut = await OpenShareFromKeyboard(key);

		// Email is disabled, so the first enabled item is the second row.
		Assert.Equal("Copy link", Highlighted(Submenu(cut)));
		Assert.Equal("Share", Highlighted(Root(cut)));

		ElementReference[] bound = BoundMenus(2);
		Assert.Equal([bound[0], bound[1]], FocusedMenus(2));
	}

	[Fact]
	public async Task ASubmenu_IsNamedByItsItem_WhichReportsItExpanded()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(FileMenu());
		IElement share = Row(Root(cut), "Share");
		Assert.Equal("true", share.GetAttribute("aria-haspopup"));
		Assert.Equal("false", share.GetAttribute("aria-expanded"));
		Assert.False(Row(Root(cut), "Open").HasAttribute("aria-expanded"));

		await Press(cut, "ArrowDown");
		await Press(cut, "ArrowDown");
		await Press(cut, "ArrowRight");

		share = Row(Root(cut), "Share");
		Assert.Equal("true", share.GetAttribute("aria-expanded"));
		Assert.Equal(share.Id, Submenu(cut).GetAttribute("aria-labelledby"));
		Assert.False(Root(cut).HasAttribute("aria-labelledby"));
	}

	// A submenu is a DOM child of its parent, and its keys used to reach the parent's handler too.
	[Fact]
	public async Task KeysInASubmenu_MoveOnlyItsOwnHighlight()
	{
		IRenderedComponent<MokaContextMenu> cut = await OpenShareFromKeyboard();

		await Submenu(cut).KeyDownAsync(Key("ArrowDown"));
		Assert.Equal("More", Highlighted(Submenu(cut)));
		Assert.Equal("Share", Highlighted(Root(cut)));

		await Submenu(cut).KeyDownAsync(Key("Home"));
		Assert.Equal("Copy link", Highlighted(Submenu(cut)));
		Assert.Equal("Share", Highlighted(Root(cut)));
	}

	[Fact]
	public async Task EnterInASubmenu_ChoosesItsItem_AndClosesTheWholeMenu()
	{
		IRenderedComponent<MokaContextMenu> cut = await OpenShareFromKeyboard();

		await Submenu(cut).KeyDownAsync(Key("Enter"));

		Assert.Equal(["Copy link"], _chosen);
		Assert.Equal(1, _closes);
	}

	[Theory]
	[InlineData("ArrowLeft")]
	[InlineData("Escape")]
	public async Task LeftOrEscapeInASubmenu_ClosesIt_AndFocusGoesBackToItsItem(string key)
	{
		IRenderedComponent<MokaContextMenu> cut = await OpenShareFromKeyboard();

		await Submenu(cut).KeyDownAsync(Key(key));

		Assert.Single(Menus(cut));
		Assert.Equal("Share", Highlighted(Root(cut)));
		Assert.Equal("false", Row(Root(cut), "Share").GetAttribute("aria-expanded"));
		Assert.Equal(0, _closes);

		ElementReference[] bound = BoundMenus(2);
		Assert.Equal([bound[0], bound[1], bound[0]], FocusedMenus(3));
	}

	[Fact]
	public async Task ANestedSubmenu_OpensAndClosesTheSameWay()
	{
		IRenderedComponent<MokaContextMenu> cut = await OpenShareFromKeyboard();

		await Submenu(cut).KeyDownAsync(Key("ArrowDown"));
		await Submenu(cut).KeyDownAsync(Key("ArrowRight"));

		IReadOnlyList<IElement> menus = Menus(cut);
		Assert.Equal(3, menus.Count);
		Assert.Equal("Share", Highlighted(menus[0]));
		Assert.Equal("More", Highlighted(menus[1]));
		Assert.Equal("QR code", Highlighted(menus[2]));

		await menus[2].KeyDownAsync(Key("ArrowLeft"));

		Assert.Equal(2, Menus(cut).Count);
		Assert.Equal("More", Highlighted(Submenu(cut)));
		Assert.Equal("Share", Highlighted(Root(cut)));

		ElementReference[] bound = BoundMenus(3);
		Assert.Equal([bound[0], bound[1], bound[2], bound[1]], FocusedMenus(4));
	}

	[Fact]
	public async Task Hovering_OpensASubmenu_WithoutMovingFocus()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(FileMenu());

		await HoverAsync(cut, "Share");

		Assert.Null(Highlighted(Submenu(cut)));
		_module.VerifyInvoke("preventKeys", 2);
		JSInterop.VerifyFocusAsyncInvoke(1);
	}

	[Fact]
	public async Task RightOnTheItemOfAHoverOpenedSubmenu_MovesFocusIntoIt()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(FileMenu());
		await HoverAsync(cut, "Share");

		await Press(cut, "ArrowDown");
		await Press(cut, "ArrowDown");
		await Press(cut, "ArrowRight");

		Assert.Equal(2, Menus(cut).Count);
		Assert.Equal("Copy link", Highlighted(Submenu(cut)));
		ElementReference[] bound = BoundMenus(2);
		Assert.Equal([bound[0], bound[1]], FocusedMenus(2));
	}

	// Removing the focused submenu drops focus to the body, where no key reaches a menu.
	[Fact]
	public async Task WhenAHoverClosesTheSubmenuThatHasFocus_ItsParentTakesFocusBack()
	{
		IRenderedComponent<MokaContextMenu> cut = await OpenShareFromKeyboard();

		await Row(Root(cut), "Open").MouseEnterAsync(new MouseEventArgs());

		Assert.Single(Menus(cut));
		JSRuntimeInvocation reclaim = _module.VerifyInvoke("reclaimFocus");
		Assert.Equal(BoundMenus(2)[0], Assert.IsType<ElementReference>(reclaim.Arguments[0]));
	}

	[Fact]
	public async Task LeftInTheRootMenu_ClosesAHoverOpenedSubmenu()
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(FileMenu());
		await HoverAsync(cut, "Share");

		await Press(cut, "ArrowLeft");

		Assert.Single(Menus(cut));
		Assert.Equal(0, _closes);
	}

	// A trigger's own menu inside a MokaDialog closed the dialog on the same Escape.
	[Fact]
	public async Task Escape_ClosesTheMenu_WithoutReachingItsContainer()
	{
		IRenderedComponent<KeyCounter> host = Render<KeyCounter>(p => p.Add(x => x.ChildContent, (RenderFragment)(b =>
		{
			b.OpenComponent<MokaContextMenu>(0);
			b.AddAttribute(1, nameof(MokaContextMenu.Items), (IReadOnlyList<MokaContextMenuItem>)[Item("Cut")]);
			b.AddAttribute(2, nameof(MokaContextMenu.Visible), true);
			b.AddAttribute(3, nameof(MokaContextMenu.OnClose), EventCallback.Factory.Create(this, () => _closes++));
			b.CloseComponent();
		})));

		await host.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Equal(1, _closes);
		Assert.Equal(0, host.Instance.Keys);
	}

	private IRenderedComponent<MokaContextMenu> RenderMenu(IReadOnlyList<MokaContextMenuItem> items) =>
		Render<MokaContextMenu>(p => p
			.Add(x => x.Items, items)
			.Add(x => x.Visible, true)
			.Add(x => x.X, 100)
			.Add(x => x.Y, 100)
			.Add(x => x.OnClose, () => _closes++));

	private async Task<IRenderedComponent<MokaContextMenu>> OpenShareFromKeyboard(string key = "ArrowRight")
	{
		IRenderedComponent<MokaContextMenu> cut = RenderMenu(FileMenu());
		await Press(cut, "ArrowDown");
		await Press(cut, "ArrowDown");
		await Press(cut, key);
		Assert.Equal(2, Menus(cut).Count);
		return cut;
	}

	/// <summary>Hovers a root row and waits out the hover delay until its submenu is open.</summary>
	private static async Task HoverAsync(IRenderedComponent<MokaContextMenu> cut, string text)
	{
		await Row(Root(cut), text).MouseEnterAsync(new MouseEventArgs());
		cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".moka-ctx-menu--submenu:not(.moka-ctx-menu--measuring)")),
			HoverWait);

		// The open hook runs after that render. Work queued behind it runs once it is done.
		await cut.InvokeAsync(() => { });
	}

	private static Task Press(IRenderedComponent<MokaContextMenu> cut, string key) => Root(cut).KeyDownAsync(Key(key));

	private static KeyboardEventArgs Key(string key) => new() { Key = key };

	private static IElement Root(IRenderedComponent<MokaContextMenu> cut) => Menus(cut)[0];

	private static IElement Submenu(IRenderedComponent<MokaContextMenu> cut) => Menus(cut)[1];

	/// <summary>The menu elements bound on open, in order, checking there were exactly this many.</summary>
	private ElementReference[] BoundMenus(int count) =>
		_module.VerifyInvoke("preventKeys", count).Select(call => Assert.IsType<ElementReference>(call.Arguments[0])).ToArray();

	/// <summary>The elements focused, in order, checking there were exactly this many.</summary>
	private ElementReference[] FocusedMenus(int count) =>
		JSInterop.VerifyFocusAsyncInvoke(count).Select(call => Assert.IsType<ElementReference>(call.Arguments[0])).ToArray();

	/// <summary>The keys a <c>preventKeys</c> call cancels on the menu element itself.</summary>
	private static string[] CancelledKeys(JSRuntimeInvocation call) =>
		Assert.IsAssignableFrom<IEnumerable<IDictionary<string, object?>>>(call.Arguments[1])
			.Where(rule => rule["selector"] is null)
			.SelectMany(rule => Assert.IsType<string[]>(rule["keys"]))
			.ToArray();

	private static MokaContextMenuItem Item(string text, bool disabled = false) => new() { Text = text, Disabled = disabled };

	private List<MokaContextMenuItem> FileMenu() =>
	[
		new() { Text = "Open", OnClickSync = () => _chosen.Add("Open") },
		new()
		{
			Text = "Share",
			Children =
			[
				new MokaContextMenuItem { Text = "Email", Disabled = true },
				new MokaContextMenuItem { Text = "Copy link", OnClickSync = () => _chosen.Add("Copy link") },
				new MokaContextMenuItem
				{
					Text = "More",
					Children = [new MokaContextMenuItem { Text = "QR code", OnClickSync = () => _chosen.Add("QR code") }]
				}
			]
		},
		new() { Text = "Delete", DividerBefore = true, OnClickSync = () => _chosen.Add("Delete") }
	];

	/// <summary>Counts the keys that reach it, as a MokaDialog's handler would see them.</summary>
	public sealed class KeyCounter : ComponentBase
	{
		[Parameter] public RenderFragment? ChildContent { get; set; }

		public int Keys { get; private set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenElement(0, "div");
			builder.AddAttribute(1, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, () => Keys++));
			builder.AddContent(2, ChildContent);
			builder.CloseElement();
		}
	}
}
