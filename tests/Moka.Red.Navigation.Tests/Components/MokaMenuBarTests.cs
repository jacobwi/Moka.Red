using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Navigation.MenuBar;

namespace Moka.Red.Navigation.Tests.Components;

// The one shell piece that could not be composed from what the library had: MokaMenu is sidebar navigation,
// MokaDropdown is a single button's menu, and neither has menubar semantics.
public class MokaMenuBarTests : BunitContext
{
	public MokaMenuBarTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<MokaMenuBar> Bar() => Render<MokaMenuBar>(p => p.AddChildContent(Menus));

	private static void Menus(RenderTreeBuilder builder)
	{
		Menu(builder, 0, "File", ["New", "Open", "Exit"]);
		Menu(builder, 20, "Edit", ["Undo", "Redo"]);
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

	private static Task Key(IRenderedComponent<MokaMenuBar> bar, string key) =>
		bar.Find(".moka-menubar").KeyDownAsync(new KeyboardEventArgs { Key = key });

	private static List<string> OpenItems(IRenderedComponent<MokaMenuBar> bar) =>
		bar.FindAll(".moka-menubar-item__text").Select(i => i.TextContent.Trim()).ToList();

	[Fact]
	public void TheBarIsAMenubarOfMenuitems()
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();

		IElement root = bar.Find(".moka-menubar");
		Assert.Equal("menubar", root.GetAttribute("role"));
		Assert.Equal("horizontal", root.GetAttribute("aria-orientation"));
		Assert.Equal("Main menu", root.GetAttribute("aria-label"));
		Assert.Equal(3, bar.FindAll("[role=menuitem][aria-haspopup=menu]").Count);
	}

	// One tab stop for the whole bar: that is the difference between a menubar and a row of buttons.
	[Fact]
	public void OnlyOneMenuIsATabStop()
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();

		Assert.Equal(
			["0", "-1", "-1"],
			bar.FindAll(".moka-menubar-menu__trigger").Select(t => t.GetAttribute("tabindex")));
	}

	[Fact]
	public void ClickingAMenuOpensIt()
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();

		bar.FindAll(".moka-menubar-menu__trigger")[0].Click();

		Assert.Equal(["New", "Open", "Exit"], OpenItems(bar));
		Assert.Equal("true", bar.FindAll(".moka-menubar-menu__trigger")[0].GetAttribute("aria-expanded"));
	}

	[Fact]
	public void ClickingTheOpenMenuClosesIt()
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();
		bar.FindAll(".moka-menubar-menu__trigger")[0].Click();

		bar.FindAll(".moka-menubar-menu__trigger")[0].Click();

		Assert.Empty(OpenItems(bar));
	}

	[Fact]
	public async Task TheArrowKeysMoveBetweenMenus()
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();

		await Key(bar, "ArrowRight");

		Assert.Equal(
			["-1", "0", "-1"],
			bar.FindAll(".moka-menubar-menu__trigger").Select(t => t.GetAttribute("tabindex")));
	}

	// They wrap, so the ends of the bar are not dead ends.
	[Fact]
	public async Task LeftFromTheFirstMenuWrapsToTheLast()
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();

		await Key(bar, "ArrowLeft");

		Assert.Equal("0", bar.FindAll(".moka-menubar-menu__trigger")[2].GetAttribute("tabindex"));
	}

	[Theory]
	[InlineData("ArrowDown")]
	[InlineData("Enter")]
	[InlineData(" ")]
	public async Task DownEnterAndSpaceOpenTheFocusedMenu(string key)
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();

		await Key(bar, key);

		Assert.Equal(["New", "Open", "Exit"], OpenItems(bar));
	}

	// Up opens on the last item, which is how a menu bar lets you reach "Exit" without walking the list.
	[Fact]
	public async Task UpOpensOnTheLastItem()
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();

		await Key(bar, "ArrowUp");

		Assert.Contains("moka-menubar-item--highlighted", bar.FindAll(".moka-menubar-item")[2].ClassList);
	}

	[Fact]
	public async Task InsideAMenuTheArrowsMoveBetweenItems()
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();
		await Key(bar, "ArrowDown");

		await Key(bar, "ArrowDown");

		Assert.Contains("moka-menubar-item--highlighted", bar.FindAll(".moka-menubar-item")[1].ClassList);
	}

	// With a menu open, Left and Right carry the open state along the bar rather than closing it.
	[Fact]
	public async Task WithAMenuOpenTheArrowsOpenTheNextOne()
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();
		await Key(bar, "ArrowDown");

		await Key(bar, "ArrowRight");

		Assert.Equal(["Undo", "Redo"], OpenItems(bar));
	}

	[Fact]
	public async Task EscapeClosesTheOpenMenu()
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();
		await Key(bar, "ArrowDown");

		await Key(bar, "Escape");

		Assert.Empty(OpenItems(bar));
	}

	[Fact]
	public async Task HomeAndEndJumpToTheEndsOfTheBar()
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();

		await Key(bar, "End");
		Assert.Equal("0", bar.FindAll(".moka-menubar-menu__trigger")[2].GetAttribute("tabindex"));

		await Key(bar, "Home");
		Assert.Equal("0", bar.FindAll(".moka-menubar-menu__trigger")[0].GetAttribute("tabindex"));
	}

	// Typing a letter jumps to the menu that starts with it, as a desktop bar does.
	[Fact]
	public async Task TypingJumpsToAMenu()
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();

		await Key(bar, "v");

		Assert.Equal("0", bar.FindAll(".moka-menubar-menu__trigger")[2].GetAttribute("tabindex"));
	}

	[Fact]
	public async Task TypingInsideAMenuJumpsToAnItem()
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();
		await Key(bar, "ArrowDown");

		await Key(bar, "e");

		Assert.Contains("moka-menubar-item--highlighted", bar.FindAll(".moka-menubar-item")[2].ClassList);
	}

	[Fact]
	public void ChoosingAnItemClosesTheMenuThenRuns()
	{
		List<string> order = [];
		IRenderedComponent<MokaMenuBar> bar = Render<MokaMenuBar>(p => p.AddChildContent(builder =>
		{
			builder.OpenComponent<MokaMenuBarMenu>(0);
			builder.AddComponentParameter(1, nameof(MokaMenuBarMenu.Label), "File");
			builder.AddComponentParameter(2, nameof(MokaMenuBarMenu.ChildContent), (RenderFragment)(inner =>
			{
				inner.OpenComponent<MokaMenuBarItem>(0);
				inner.AddComponentParameter(1, nameof(MokaMenuBarItem.Text), "New");
				inner.AddComponentParameter(2, nameof(MokaMenuBarItem.OnClick),
					EventCallback.Factory.Create(this, () => order.Add("ran")));
				inner.CloseComponent();
			}));
			builder.CloseComponent();
		}));

		bar.Find(".moka-menubar-menu__trigger").Click();
		bar.Find(".moka-menubar-item").Click();

		Assert.Equal(["ran"], order);
		Assert.Empty(bar.FindAll(".moka-menubar-item"));
	}

	// A shortcut on an item is a hint drawn as caps, the way the context menu draws one. The item does not
	// bind the key; the hotkey registry does.
	[Fact]
	public void AnItemsShortcutIsDrawnAsKeyCaps()
	{
		IRenderedComponent<MokaMenuBar> bar = Render<MokaMenuBar>(p => p.AddChildContent(builder =>
		{
			builder.OpenComponent<MokaMenuBarMenu>(0);
			builder.AddComponentParameter(1, nameof(MokaMenuBarMenu.Label), "File");
			builder.AddComponentParameter(2, nameof(MokaMenuBarMenu.ChildContent), (RenderFragment)(inner =>
			{
				inner.OpenComponent<MokaMenuBarItem>(0);
				inner.AddComponentParameter(1, nameof(MokaMenuBarItem.Text), "New");
				inner.AddComponentParameter(2, nameof(MokaMenuBarItem.Shortcut), "Ctrl+N");
				inner.CloseComponent();
			}));
			builder.CloseComponent();
		}));

		bar.Find(".moka-menubar-menu__trigger").Click();

		Assert.Equal(["Ctrl", "N"], bar.FindAll("kbd").Select(k => k.TextContent.Trim()));
	}

	// A disabled item is skipped by the arrows rather than being a stop that cannot be chosen.
	[Fact]
	public async Task TheArrowsStepOverADisabledItem()
	{
		IRenderedComponent<MokaMenuBar> bar = Render<MokaMenuBar>(p => p.AddChildContent(builder =>
		{
			builder.OpenComponent<MokaMenuBarMenu>(0);
			builder.AddComponentParameter(1, nameof(MokaMenuBarMenu.Label), "File");
			builder.AddComponentParameter(2, nameof(MokaMenuBarMenu.ChildContent), (RenderFragment)(inner =>
			{
				inner.OpenComponent<MokaMenuBarItem>(0);
				inner.AddComponentParameter(1, nameof(MokaMenuBarItem.Text), "New");
				inner.CloseComponent();
				inner.OpenComponent<MokaMenuBarItem>(10);
				inner.AddComponentParameter(11, nameof(MokaMenuBarItem.Text), "Save");
				inner.AddComponentParameter(12, nameof(MokaMenuBarItem.Disabled), true);
				inner.CloseComponent();
				inner.OpenComponent<MokaMenuBarItem>(20);
				inner.AddComponentParameter(21, nameof(MokaMenuBarItem.Text), "Exit");
				inner.CloseComponent();
			}));
			builder.CloseComponent();
		}));

		await Key(bar, "ArrowDown");
		await Key(bar, "ArrowDown");

		Assert.Contains("moka-menubar-item--highlighted", bar.FindAll(".moka-menubar-item")[2].ClassList);
	}

	[Fact]
	public void TheMenuPopupIsANamedMenu()
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();
		bar.FindAll(".moka-menubar-menu__trigger")[0].Click();

		IElement popup = bar.Find(".moka-menubar-menu__popup");
		Assert.Equal("menu", popup.GetAttribute("role"));
		Assert.Equal("File", popup.GetAttribute("aria-label"));
		Assert.Equal(popup.Id, bar.FindAll(".moka-menubar-menu__trigger")[0].GetAttribute("aria-controls"));
	}

	// Hovering along an open bar switches menus, and hovering a closed bar opens nothing.
	[Fact]
	public void HoverSwitchesOnlyWhileSomethingIsOpen()
	{
		IRenderedComponent<MokaMenuBar> bar = Bar();

		bar.FindAll(".moka-menubar-menu")[1].MouseEnter();
		Assert.Empty(OpenItems(bar));

		bar.FindAll(".moka-menubar-menu__trigger")[0].Click();
		bar.FindAll(".moka-menubar-menu")[1].MouseEnter();
		Assert.Equal(["Undo", "Redo"], OpenItems(bar));
	}

	[Fact]
	public void OpenOnHoverFalseLeavesTheOpenMenuAlone()
	{
		IRenderedComponent<MokaMenuBar> bar = Render<MokaMenuBar>(p => p
			.Add(x => x.OpenOnHover, false)
			.AddChildContent(Menus));
		bar.FindAll(".moka-menubar-menu__trigger")[0].Click();

		bar.FindAll(".moka-menubar-menu")[1].MouseEnter();

		Assert.Equal(["New", "Open", "Exit"], OpenItems(bar));
	}
}
