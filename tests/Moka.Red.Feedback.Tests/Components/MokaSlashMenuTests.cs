using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.SlashMenu;
using Moka.Red.Icons;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

public partial class MokaSlashMenuTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Feedback/SlashMenu/MokaSlashMenu.razor.js";
	private const string Css = "MokaSlashMenu.razor.css";

	private static IReadOnlyList<MokaSlashMenuItem> Items =>
	[
		new() { Title = "Heading", Keywords = "h1 title", Category = "struct" },
		new() { Title = "Bullet list", Keywords = "ul", Category = "struct" },
		new() { Title = "Code block", Keywords = "fence", Category = "code" }
	];

	// Categories interleaved, and one item without a category last, so the shown order differs from the given one.
	private static IReadOnlyList<MokaSlashMenuItem> Mixed =>
	[
		new() { Title = "Heading 1", Category = "Text", Shortcut = "#" },
		new() { Title = "Bullet list", Category = "Lists", Shortcut = "-" },
		new() { Title = "Quote", Category = "Text", Shortcut = ">" },
		new() { Title = "Emoji" }
	];

	public MokaSlashMenuTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void Closed_RendersNothing()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, false)
			.Add(x => x.Items, Items));

		Assert.Empty(cut.FindAll(".moka-slash-menu"));
	}

	[Fact]
	public void Open_RendersListboxWithOptions()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));

		Assert.Equal("listbox", cut.Find(".moka-slash-menu-list").GetAttribute("role"));
		Assert.Equal(3, cut.FindAll("[role='option']").Count);
	}

	[Fact]
	public void Query_FiltersByTitle()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.Query, "code"));

		Assert.Single(cut.FindAll("[role='option']"));
	}

	[Fact]
	public void Query_FiltersByKeyword()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.Query, "fence"));

		Assert.Single(cut.FindAll("[role='option']"));
	}

	[Fact]
	public void Header_Default_Renders()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));

		Assert.Equal("Insert", cut.Find(".moka-slash-menu-label").TextContent);
	}

	[Fact]
	public void Header_Empty_NoHeader()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.Header, ""));

		Assert.Empty(cut.FindAll(".moka-slash-menu-header"));
	}

	[Fact]
	public async Task HandleKeyAsync_ArrowDown_MovesActiveAndConsumes()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));

		bool consumed = false;
		await cut.InvokeAsync(async () =>
			consumed = await cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "ArrowDown" }));

		Assert.True(consumed);
		IReadOnlyList<IElement> options = cut.FindAll("[role='option']");
		Assert.Equal("true", options[1].GetAttribute("aria-selected"));
	}

	[Fact]
	public async Task HandleKeyAsync_Enter_SelectsActiveItem()
	{
		MokaSlashMenuItem? selected = null;
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.OnSelect, EventCallback.Factory.Create<MokaSlashMenuItem>(this, i => selected = i)));

		await cut.InvokeAsync(async () =>
			await cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "Enter" }));

		Assert.NotNull(selected);
		Assert.Equal("Heading", selected!.Title);
	}

	[Fact]
	public async Task HandleKeyAsync_Escape_ClosesViaOpenChanged()
	{
		bool open = true;
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => open = v)));

		await cut.InvokeAsync(async () =>
			await cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "Escape" }));

		Assert.False(open);
	}

	[Fact]
	public async Task HandleKeyAsync_WhenClosed_ReturnsFalse()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, false)
			.Add(x => x.Items, Items));

		bool consumed = true;
		await cut.InvokeAsync(async () =>
			consumed = await cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "ArrowDown" }));

		Assert.False(consumed);
	}

	// Focus stays in the host textarea, so it has to point aria-activedescendant at the highlighted
	// option and aria-controls at the list. Neither had an id to point at.
	[Fact]
	public void ListboxAndOptions_HaveIds()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));

		string listboxId = cut.Instance.ListboxId;
		Assert.Equal(listboxId, cut.Find("[role=listbox]").Id);
		Assert.Equal(
			new[] { $"{listboxId}-option-0", $"{listboxId}-option-1", $"{listboxId}-option-2" },
			cut.FindAll("[role=option]").Select(o => o.Id));
	}

	[Fact]
	public async Task ActiveOptionId_NamesTheHighlightedOption()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));
		Assert.Equal("true", cut.Find($"#{cut.Instance.ActiveOptionId}").GetAttribute("aria-selected"));

		await cut.InvokeAsync(() => cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "ArrowUp" }));

		IElement active = cut.Find($"#{cut.Instance.ActiveOptionId}");
		Assert.Equal("true", active.GetAttribute("aria-selected"));
		Assert.Contains("Code block", active.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void ActiveOptionId_FollowsANewQuery()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));

		cut.Render(p => p.Add(x => x.Query, "list"));

		IElement active = cut.Find($"#{cut.Instance.ActiveOptionId}");
		Assert.Contains("Bullet list", active.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ActiveOptionId_IsNull_WhileClosedOrWithoutMatches()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, false)
			.Add(x => x.Items, Items));
		Assert.Null(cut.Instance.ActiveOptionId);

		cut.Render(p => p.Add(x => x.Open, true).Add(x => x.Query, "zzzzz"));
		Assert.Null(cut.Instance.ActiveOptionId);

		cut.Render(p => p.Add(x => x.Query, ""));
		Assert.NotNull(cut.Instance.ActiveOptionId);

		await cut.InvokeAsync(() => cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "Escape" }));
		Assert.Null(cut.Instance.ActiveOptionId);
	}

	[Fact]
	public void ListboxId_StaysTheSame_AcrossRendersAndReopening()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));
		string listboxId = cut.Instance.ListboxId;

		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true).Add(x => x.Query, "code"));

		Assert.Equal(listboxId, cut.Instance.ListboxId);
		Assert.Equal(listboxId, cut.Find("[role=listbox]").Id);
	}

	// Two menus on one page must not share ids, or aria-activedescendant could point into the wrong one.
	[Fact]
	public void TwoMenus_HaveDifferentIds()
	{
		IRenderedComponent<MokaSlashMenu> first = Render<MokaSlashMenu>(p => p.Add(x => x.Items, Items));
		IRenderedComponent<MokaSlashMenu> second = Render<MokaSlashMenu>(p => p.Add(x => x.Items, Items));

		Assert.NotEqual(first.Instance.ListboxId, second.Instance.ListboxId);
	}

	// Dark and Light are the original design, and an app that names them upgrades without its look changing.
	[Fact]
	public void OnTheOriginalDesign_EachRowCarriesItsCategory_InTheGivenOrder()
	{
		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(MokaTheme.Dark);

		Assert.Equal(["Heading 1", "Bullet list", "Quote", "Emoji"], Titles(cut));
		Assert.Equal(["Text", "Lists", "Text"], cut.FindAll("[role=option] .moka-slash-menu-category").Select(c => c.TextContent));
		Assert.Empty(cut.FindAll(".moka-slash-menu-group-label"));
		Assert.Empty(cut.FindAll("[role=group]"));
	}

	[Fact]
	public void OnTheOriginalDesign_TheHintsAreInTheHeader_AndThereIsNoFooter()
	{
		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(MokaTheme.Dark);

		Assert.Equal(
			["↑↓", "⏎", "Esc"],
			cut.FindAll(".moka-slash-menu-header .moka-slash-menu-hints kbd").Select(cap => cap.TextContent.Trim()));
		Assert.Empty(cut.FindAll(".moka-slash-menu-footer"));
	}

	[Fact]
	public void OnTheOriginalDesign_IconsAreBare_AndShortcutsAreNotDrawn()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.AddCascadingValue(MokaTheme.Dark)
			.Add(x => x.Open, true)
			.Add(x => x.ShowHints, false)
			.Add(x => x.Items, [new MokaSlashMenuItem { Title = "Code block", Icon = MokaIcons.File.Code, Shortcut = "```" }]));

		IElement row = cut.Find("[role=option]");
		Assert.NotNull(row.QuerySelector(":scope > svg.moka-slash-menu-icon"));
		Assert.Null(row.QuerySelector(".moka-slash-menu-tile"));
		Assert.Empty(cut.FindAll("kbd"));
	}

	[Fact]
	public void OnTheOriginalDesign_NoMatchesSaysSo_InTheList()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.AddCascadingValue(MokaTheme.Dark)
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.Query, "tabel"));

		IElement list = cut.Find("[role=listbox]");
		Assert.False(list.HasAttribute("hidden"));
		Assert.Equal("No matches", list.QuerySelector(".moka-slash-menu-empty")!.TextContent);
		Assert.Empty(cut.FindAll(".moka-slash-menu-empty-state"));
		Assert.Empty(cut.FindAll("[role=status]"));
	}

	[Fact]
	public void WithoutATheme_TheOriginalDesignRenders()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Mixed));

		Assert.Equal(3, cut.FindAll(".moka-slash-menu-category").Count);
		Assert.Empty(cut.FindAll(".moka-slash-menu-group-label"));
		Assert.Empty(cut.FindAll(".moka-slash-menu-footer"));
	}

	// A theme switched at run time redraws the menu in the other design, in the other design's order.
	[Fact]
	public void SwitchingTheTheme_RedrawsTheMenuInTheOtherDesign()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> host = Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, MokaTheme.Dark)
			.AddChildContent<MokaSlashMenu>(menu => menu.Add(x => x.Open, true).Add(x => x.Items, Mixed)));
		MokaSlashMenu menu = host.FindComponent<MokaSlashMenu>().Instance;
		Assert.Equal("Heading 1", Titles(host.FindComponent<MokaSlashMenu>())[0]);

		host.Render(p => p
			.Add(x => x.Value, MokaTheme.Default)
			.AddChildContent<MokaSlashMenu>(m => m.Add(x => x.Open, true).Add(x => x.Items, Mixed)));

		IRenderedComponent<MokaSlashMenu> cut = host.FindComponent<MokaSlashMenu>();
		Assert.Same(menu, cut.Instance);
		Assert.Equal(["Emoji", "Heading 1", "Quote", "Bullet list"], Titles(cut));
		Assert.Single(cut.FindAll(".moka-slash-menu-footer"));
	}

	// One heading over each group replaces the category tag every row used to repeat.
	[Fact]
	public void OnTheNewDesign_CategoriesBecomeHeadings_InTheOrderTheyFirstAppear()
	{
		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(MokaTheme.Default);

		Assert.Equal(["Text", "Lists"], cut.FindAll(".moka-slash-menu-group-label").Select(l => l.TextContent));
		Assert.Equal(["Emoji", "Heading 1", "Quote", "Bullet list"], Titles(cut));
		Assert.Empty(cut.FindAll(".moka-slash-menu-category"));
		Assert.Empty(cut.FindAll(".moka-slash-menu-hints"));
	}

	[Fact]
	public void OnTheNewDesign_AGroupIsNamedByItsHeading()
	{
		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(MokaTheme.Default);

		IReadOnlyList<IElement> groups = cut.FindAll("[role=group]");
		Assert.Equal(2, groups.Count);
		foreach (IElement group in groups)
		{
			IElement label = group.QuerySelector(".moka-slash-menu-group-label")!;
			Assert.Equal(group.GetAttribute("aria-labelledby"), label.Id);
			Assert.Equal("presentation", label.GetAttribute("role"));
		}

		Assert.Equal(["Heading 1", "Quote"], groups[0].QuerySelectorAll("[role=option] .moka-slash-menu-title").Select(t => t.TextContent));
	}

	// Items without a category have no heading to sit under, so they come first rather than under another group's.
	[Fact]
	public void OnTheNewDesign_ItemsWithoutACategoryComeFirst_WithNoHeading()
	{
		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(MokaTheme.Default);

		IElement first = cut.Find(".moka-slash-menu-group");
		Assert.Equal("presentation", first.GetAttribute("role"));
		Assert.Null(first.GetAttribute("aria-labelledby"));
		Assert.Null(first.QuerySelector(".moka-slash-menu-group-label"));
		Assert.Equal("Emoji", first.QuerySelector(".moka-slash-menu-title")!.TextContent);
	}

	// The highlight counts rows in the order they are shown, or Enter would insert a row other than the lit one.
	[Fact]
	public async Task OnTheNewDesign_TheArrowsAndEnterFollowTheShownOrder()
	{
		MokaSlashMenuItem? selected = null;
		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(MokaTheme.Default, p => p
			.Add(x => x.OnSelect, EventCallback.Factory.Create<MokaSlashMenuItem>(this, i => selected = i)));

		await cut.InvokeAsync(() => cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "ArrowDown" }));
		await cut.InvokeAsync(() => cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "ArrowDown" }));

		IElement active = cut.Find($"#{cut.Instance.ActiveOptionId}");
		Assert.Equal("Quote", active.QuerySelector(".moka-slash-menu-title")!.TextContent);
		Assert.Equal(cut.Instance.ListboxId + "-option-2", active.Id);

		await cut.InvokeAsync(() => cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "Enter" }));
		Assert.Equal("Quote", selected?.Title);
	}

	[Fact]
	public void OnTheNewDesign_MaxVisibleCountsInTheShownOrder()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.AddCascadingValue(MokaTheme.Default)
			.Add(x => x.Open, true)
			.Add(x => x.MaxVisible, 2)
			.Add(x => x.Items,
			[
				new MokaSlashMenuItem { Title = "Heading 1", Category = "Text" },
				new MokaSlashMenuItem { Title = "Bullet list", Category = "Lists" },
				new MokaSlashMenuItem { Title = "Quote", Category = "Text" }
			]));

		Assert.Equal(["Heading 1", "Quote"], Titles(cut));
		Assert.Equal(["Text"], cut.FindAll(".moka-slash-menu-group-label").Select(l => l.TextContent));
	}

	[Theory]
	[InlineData("##", new[] { "##" })]
	[InlineData("[ ]", new[] { "[ ]" })]
	[InlineData("Ctrl+Alt+2", new[] { "Ctrl", "Alt", "2" })]
	[InlineData("+", new[] { "+" })]
	public void OnTheNewDesign_AShortcutIsDrawnAsKeyCaps(string shortcut, string[] caps)
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.AddCascadingValue(MokaTheme.Default)
			.Add(x => x.Open, true)
			.Add(x => x.ShowHints, false)
			.Add(x => x.Items, [new MokaSlashMenuItem { Title = "Heading 2", Shortcut = shortcut }]));

		Assert.Equal(caps, cut.FindAll("[role=option] .moka-slash-menu-shortcut kbd").Select(k => k.TextContent));
	}

	[Fact]
	public void OnTheNewDesign_NoShortcutDrawsNoCapsOnTheRow()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.AddCascadingValue(MokaTheme.Default)
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));

		Assert.Empty(cut.FindAll("[role=option] kbd"));
		Assert.Empty(cut.FindAll(".moka-slash-menu-shortcut"));
	}

	[Fact]
	public void OnTheNewDesign_AnIconSitsInATile()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.AddCascadingValue(MokaTheme.Default)
			.Add(x => x.Open, true)
			.Add(x => x.Items,
			[
				new MokaSlashMenuItem { Title = "Code block", Icon = MokaIcons.File.Code },
				new MokaSlashMenuItem { Title = "Plain" }
			]));

		IReadOnlyList<IElement> rows = cut.FindAll("[role=option]");
		Assert.NotNull(rows[0].QuerySelector(".moka-slash-menu-tile > svg"));
		Assert.Null(rows[0].QuerySelector(".moka-slash-menu-icon"));
		Assert.Null(rows[1].QuerySelector(".moka-slash-menu-tile"));
	}

	// Focus is in the textarea, so nobody typing could hover a tooltip to learn what a cap does. Words say it.
	[Fact]
	public void OnTheNewDesign_TheFooterSaysWhatEachKeyDoes()
	{
		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(MokaTheme.Default);

		IReadOnlyList<IElement> hints = cut.FindAll(".moka-slash-menu-footer .moka-slash-menu-hint");
		Assert.Equal(["Move", "Insert", "Close"], hints.Select(h => h.LastElementChild!.TextContent));
		Assert.Equal(["↑", "↓", "⏎", "Esc"], cut.FindAll(".moka-slash-menu-footer kbd").Select(k => k.TextContent));
		Assert.Empty(cut.FindAll(".moka-slash-menu-header kbd"));
	}

	[Fact]
	public void OnTheNewDesign_TheFooterShowsWithoutAHeader()
	{
		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(MokaTheme.Default, p => p.Add(x => x.Header, null));

		Assert.Single(cut.FindAll(".moka-slash-menu-footer"));
	}

	[Fact]
	public void OnTheNewDesign_ShowHintsFalseDrawsNoFooter()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.AddCascadingValue(MokaTheme.Default)
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.ShowHints, false));

		Assert.Empty(cut.FindAll(".moka-slash-menu-footer"));
		Assert.Empty(cut.FindAll("kbd"));
	}

	[Fact]
	public void OnTheNewDesign_NoMatchesSaysWhatWasTyped_AndWhatTheKeysDo()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.AddCascadingValue(MokaTheme.Default)
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.Query, " tabel "));

		Assert.Equal("Nothing called \"tabel\"", cut.Find(".moka-slash-menu-empty-title").TextContent);
		Assert.Equal("Enter adds a new line. Esc closes this and leaves the slash as text.",
			cut.Find(".moka-slash-menu-empty-hint").TextContent);
		Assert.Equal(["New line", "Close"],
			cut.FindAll(".moka-slash-menu-footer .moka-slash-menu-hint").Select(h => h.LastElementChild!.TextContent));
		Assert.True(cut.Find("[role=listbox]").HasAttribute("hidden"));
		Assert.Empty(cut.FindAll(".moka-slash-menu-empty"));
	}

	[Fact]
	public void OnTheNewDesign_EmptyTextPutsTheQueryWhereItSays()
	{
		IRenderedComponent<MokaSlashMenu> cut = RenderNoMatches(p => p.Add(x => x.EmptyText, "No block is called {query}"));

		Assert.Equal("No block is called tabel", cut.Find(".moka-slash-menu-empty-title").TextContent);
	}

	[Fact]
	public void OnTheNewDesign_EmptyTextAndHintCanBeTurnedOff()
	{
		IRenderedComponent<MokaSlashMenu> cut = RenderNoMatches(p => p
			.Add(x => x.EmptyText, null)
			.Add(x => x.EmptyHint, ""));

		Assert.Single(cut.FindAll(".moka-slash-menu-empty-state"));
		Assert.Empty(cut.FindAll(".moka-slash-menu-empty-title"));
		Assert.Empty(cut.FindAll(".moka-slash-menu-empty-hint"));
	}

	// Focus stays in the textarea, so a screen reader hears that nothing matched only through a live region.
	[Fact]
	public void OnTheNewDesign_NoMatchesIsAnnounced()
	{
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.AddCascadingValue(MokaTheme.Default)
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items));
		Assert.Equal("", cut.Find("[role=status]").TextContent);

		cut.Render(p => p.Add(x => x.Query, "tabel"));

		Assert.Equal("Nothing called \"tabel\"", cut.Find("[role=status]").TextContent);
	}

	// Hovering a row used to be a round trip to .NET and a render of the menu for every row the mouse crossed.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Rows_ListenForNoHover_AndCarryTheirIndex(bool newDesign)
	{
		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(Look(newDesign));

		IReadOnlyList<IElement> rows = cut.FindAll("[role=option]");
		Assert.Equal(4, rows.Count);
		for (int i = 0; i < rows.Count; i++)
		{
			Assert.False(rows[i].HasAttribute("blazor:onmouseenter"));
			Assert.Equal(i.ToString(CultureInfo.InvariantCulture), rows[i].GetAttribute("data-moka-index"));
		}
	}

	// Pressing the mouse anywhere in the menu, a heading or the list's padding included, must not take focus from
	// the textarea, which closes the menu when it loses focus.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void PressingTheMouse_InTheMenu_KeepsFocusInTheEditor(bool newDesign)
	{
		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(Look(newDesign));

		Assert.True(cut.Find(".moka-slash-menu").HasAttribute("blazor:onmousedown:preventDefault"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void TheList_IsTrackedForHover_OncePerOpening(bool newDesign)
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.Mode = JSRuntimeMode.Loose;

		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(Look(newDesign));
		string listId = cut.Find("[role=listbox]").GetAttribute("blazor:elementReference")!;

		cut.Render(p => p.Add(x => x.Query, "e"));
		JSRuntimeInvocation tracked = Assert.Single(Calls(module, "trackHover"));
		Assert.Equal(listId, Assert.IsType<ElementReference>(tracked.Arguments[0]).Id);

		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true));

		Assert.Equal(2, Calls(module, "trackHover").Count);
	}

	[Theory]
	[InlineData(false, "Emoji")]
	[InlineData(true, "Bullet list")]
	public async Task AHoveredRow_IsWhereTheNextKeyStartsFrom(bool newDesign, string fourthRow)
	{
		MokaSlashMenuItem? selected = null;
		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(Look(newDesign), p => p
			.Add(x => x.OnSelect, EventCallback.Factory.Create<MokaSlashMenuItem>(this, i => selected = i)));

		await cut.InvokeAsync(() => cut.Instance.OnItemHovered(3));

		Assert.Equal("true", cut.FindAll("[role=option]")[3].GetAttribute("aria-selected"));
		Assert.Equal(cut.Instance.ListboxId + "-option-3", cut.Instance.ActiveOptionId);

		await cut.InvokeAsync(() => cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "Enter" }));
		Assert.Equal(fourthRow, selected?.Title);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(4)]
	public async Task AHoverOutsideTheRows_ChangesNothing(int index)
	{
		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(MokaTheme.Default);

		await cut.InvokeAsync(() => cut.Instance.OnItemHovered(index));

		Assert.Equal(cut.Instance.ListboxId + "-option-0", cut.Instance.ActiveOptionId);
	}

	// Eight rows with descriptions are taller than the list on the new design, so the keyboard could light a row
	// out of sight.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task TheArrows_ScrollTheLitRowIntoView(bool newDesign)
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(Look(newDesign));
		Assert.Empty(Calls(module, "reveal"));

		await cut.InvokeAsync(() => cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "ArrowUp" }));

		JSRuntimeInvocation reveal = Assert.Single(Calls(module, "reveal"));
		Assert.IsType<ElementReference>(reveal.Arguments[0]);
		Assert.Equal(3, Assert.IsType<int>(reveal.Arguments[1]));
	}

	[Fact]
	public async Task ANewQuery_ScrollsBackToTheFirstRow()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(MokaTheme.Default);
		await cut.InvokeAsync(() => cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "ArrowUp" }));

		cut.Render(p => p.Add(x => x.Query, "t"));

		Assert.Equal([3, 0], Calls(module, "reveal").Select(call => call.Arguments[1]));
	}

	[Fact]
	public async Task AHover_ScrollsNothing()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaSlashMenu> cut = RenderMixed(MokaTheme.Default);

		await cut.InvokeAsync(() => cut.Instance.OnItemHovered(2));

		Assert.Empty(Calls(module, "reveal"));
	}

	// On Red the accent is nearly the error color, so lit text in it read as a destructive item. On the new design
	// the text keeps its color and only the icon tile takes the accent.
	[Fact]
	public void OnTheNewDesign_TheLitRowKeepsItsTextColor_AndItsTileTakesTheAccent()
	{
		const string gateRow = ".moka-gate .moka-slash-menu-list:not([data-moka-pointer]) .moka-slash-menu-row--active";
		const string row = ".moka-slash-menu-list:not([data-moka-pointer]) .moka-slash-menu-row--active";

		Assert.Equal("var(--moka-popup-row-hover-color, var(--moka-popup-color, var(--moka-color-on-surface)))",
			ScopedCss.Declarations(Css, $"{gateRow} .moka-slash-menu-title")["color"]);
		IReadOnlyDictionary<string, string> tile = ScopedCss.Declarations(Css, $"{row} .moka-slash-menu-tile");
		Assert.Equal("var(--moka-color-primary-light)", tile["color"]);
		Assert.Equal("var(--moka-color-primary-bg)", tile["background"]);
		IReadOnlyDictionary<string, string> lit = ScopedCss.Declarations(Css, gateRow);
		Assert.Equal("var(--moka-popup-row-hover, var(--moka-color-primary-row))", lit["background"]);
		Assert.Equal("var(--moka-popup-row-hover-shadow, var(--moka-selected-glow))", lit["box-shadow"]);
	}

	[Fact]
	public void OnTheOriginalDesign_TheLitRowIsAsItWas()
	{
		const string row = ".moka-slash-menu-list:not([data-moka-pointer]) .moka-slash-menu-row--active";

		Assert.Equal("var(--moka-color-primary)", ScopedCss.Declarations(Css, $"{row} .moka-slash-menu-title")["color"]);
		Assert.Equal("var(--moka-color-primary)", ScopedCss.Declarations(Css, $"{row} ::deep .moka-slash-menu-icon")["color"]);
		IReadOnlyDictionary<string, string> lit = ScopedCss.Declarations(Css, row);
		Assert.Equal("var(--moka-color-primary-glow)", lit["background"]);
		Assert.Equal("inset var(--moka-slash-menu-rail-width) 0 0 var(--moka-color-primary)", lit["box-shadow"]);
	}

	// A row with a description is about 46 px on the new design: 13.5 px title and 12 px description at their line
	// heights, a 1 px gap and 6 px above and below. The list used to stop at 320 px, short of the default eight.
	[Fact]
	public void OnTheNewDesign_TheListHoldsEightRowsWithDescriptions()
	{
		const double rowPx = 13.5 * 1.4 + 12 * 1.2 + 1 + 2 * 6;
		const double listPaddingPx = 2 + 6;

		string maxHeight = ScopedCss.Declarations(Css, ".moka-gate .moka-slash-menu-list")["max-height"];
		Match rem = RemValue().Match(maxHeight);

		Assert.True(rem.Success, maxHeight);
		Assert.True(double.Parse(rem.Groups[1].Value, CultureInfo.InvariantCulture) * 16 >= 8 * rowPx + listPaddingPx, maxHeight);
	}

	// --moka-item-padding is two values, so writing it after another one gave a row 4 px above, 10 px below and the
	// item's vertical padding at the sides, with the text sitting high. The dropdown item had 0 above and 10 px below.
	[Theory]
	[InlineData("MokaSlashMenu.razor.css")]
	[InlineData("MokaDropdownItem.razor.css")]
	public void ATwoValuePaddingToken_IsNeverOneSideOfAPadding(string file)
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", file));

		List<string> found =
		[
			.. Padding().Matches(css)
				.Select(m => m.Groups[1].Value.Trim())
				.Where(v => TwoValueToken().IsMatch(v) && !TwoValueToken().Match(v).Value.Equals(v, StringComparison.Ordinal))
		];

		Assert.Empty(found);
	}

	private static MokaTheme Look(bool newDesign) => newDesign ? MokaTheme.Default : MokaTheme.Dark;

	private IRenderedComponent<MokaSlashMenu> RenderMixed(
		MokaTheme theme,
		Action<ComponentParameterCollectionBuilder<MokaSlashMenu>>? more = null) =>
		Render<MokaSlashMenu>(p =>
		{
			p.AddCascadingValue(theme).Add(x => x.Open, true).Add(x => x.Items, Mixed);
			more?.Invoke(p);
		});

	private IRenderedComponent<MokaSlashMenu> RenderNoMatches(Action<ComponentParameterCollectionBuilder<MokaSlashMenu>> more) =>
		Render<MokaSlashMenu>(p =>
		{
			p.AddCascadingValue(MokaTheme.Default).Add(x => x.Open, true).Add(x => x.Items, Items).Add(x => x.Query, "tabel");
			more(p);
		});

	// Placed by the app inside a card, the menu was cut off by the card's hidden overflow.
	[Fact]
	public void WithAnAnchor_TheMenuOpensInTheTopLayer_BelowTheAnchor()
	{
		BunitJSModuleInterop popup = TopLayerPopup.Setup(JSInterop);

		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.Anchor, new ElementReference("editor")));

		TopLayerPopup.AssertOpened(popup, cut.Find(".moka-slash-menu"), "editor", "bottom-start");
	}

	[Fact]
	public void WithAnAnchor_ClosingTheMenu_ClosesThePopup()
	{
		BunitJSModuleInterop popup = TopLayerPopup.Setup(JSInterop);
		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.Anchor, new ElementReference("editor")));

		cut.Render(p => p.Add(x => x.Open, false));

		TopLayerPopup.AssertClosed(popup);
	}

	[Fact]
	public void Placement_ChoosesWhereItOpens()
	{
		BunitJSModuleInterop popup = TopLayerPopup.Setup(JSInterop);

		IRenderedComponent<MokaSlashMenu> cut = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.Anchor, new ElementReference("editor"))
			.Add(x => x.Placement, Moka.Red.Feedback.Popover.MokaPopoverPosition.TopStart));

		TopLayerPopup.AssertOpened(popup, cut.Find(".moka-slash-menu"), "editor", "top-start");
	}

	[Fact]
	public void WithoutAnAnchor_TheAppPlacesTheMenu_AsBefore()
	{
		BunitJSModuleInterop popup = TopLayerPopup.Setup(JSInterop);

		IElement menu = Render<MokaSlashMenu>(p => p.Add(x => x.Open, true).Add(x => x.Items, Items)).Find(".moka-slash-menu");

		Assert.False(menu.HasAttribute("popover"));
		Assert.False(menu.HasAttribute("data-moka-popup"));
		Assert.Empty(popup.Invocations["open"]);
	}

	// A host passes its field before its own first render, when the reference is still empty.
	[Fact]
	public void AnEmptyAnchor_CountsAsNone()
	{
		BunitJSModuleInterop popup = TopLayerPopup.Setup(JSInterop);

		IElement menu = Render<MokaSlashMenu>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Items, Items)
			.Add(x => x.Anchor, default(ElementReference))).Find(".moka-slash-menu");

		Assert.False(menu.HasAttribute("popover"));
		Assert.Empty(popup.Invocations["open"]);
	}

	private static List<string> Titles(IRenderedComponent<MokaSlashMenu> cut) =>
		[.. cut.FindAll("[role=option] .moka-slash-menu-title").Select(t => t.TextContent)];

	private static List<JSRuntimeInvocation> Calls(BunitJSModuleInterop module, string identifier) =>
		[.. module.Invocations.Where(i => i.Identifier == identifier)];

	[GeneratedRegex(@"([\d.]+)rem")]
	private static partial Regex RemValue();

	[GeneratedRegex(@"padding[\w-]*\s*:\s*([^;}]+)")]
	private static partial Regex Padding();

	[GeneratedRegex(@"var\(--moka-(item|row|panel|cell|pill)-padding\)")]
	private static partial Regex TwoValueToken();
}
