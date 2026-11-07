using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;
using Moka.Red.Layout.BentoGrid;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// A board is MokaBentoGrid with Board set: the cards come from CardTemplate in the board's order, and customize
// mode adds the tools, the tray and the bar. Every change draws at once and is raised with the whole new layout.
public class MokaBentoBoardTests : BunitContext
{
	private const string BoardModule = "./_content/Moka.Red.Layout/BentoGrid/MokaBentoGrid.razor.js";
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	private static readonly Dictionary<string, string> Titles = new()
	{
		["a"] = "Internet",
		["b"] = "Devices",
		["c"] = "Plugins",
		["d"] = "Traffic"
	};

	private static readonly IReadOnlyList<MokaBoardCard> Layout =
	[
		new("a", MokaBoardSize.Wide),
		new("b", MokaBoardSize.Small),
		new("c", MokaBoardSize.Small, Hidden: true),
		new("d", MokaBoardSize.Full)
	];

	private readonly BunitJSModuleInterop _board;
	private readonly BunitJSModuleInterop _keys;
	private readonly List<IReadOnlyList<MokaBoardCard>> _raised = [];

	public MokaBentoBoardTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_board = JSInterop.SetupModule(BoardModule);
		_board.Mode = JSRuntimeMode.Loose;
		_keys = JSInterop.SetupModule(KeysModule);
		_keys.Mode = JSRuntimeMode.Loose;
	}

	private static RenderFragment<MokaBoardCard> Template => card => builder =>
	{
		builder.OpenComponent<MokaBentoItem>(0);
		builder.AddComponentParameter(1, nameof(MokaBentoItem.Card), card);
		builder.AddComponentParameter(2, nameof(MokaBentoItem.ChildContent), (RenderFragment)(body => body.AddContent(0, $"{card.Id} body")));
		builder.CloseComponent();
	};

	[Fact]
	public void Grid_WithoutBoard_RendersAsBefore()
	{
		IRenderedComponent<MokaBentoGrid> cut = Render<MokaBentoGrid>(p => p
			.AddChildContent("<span>x</span>"));

		cut.MarkupMatches(
			"""<div class="moka-bento-grid" style="grid-template-columns: repeat(4, 1fr); grid-auto-rows: minmax(120px, auto); gap: var(--moka-spacing-md)"><span>x</span></div>""");
		Assert.Empty(JSInterop.Invocations);
	}

	[Fact]
	public void Item_WithoutCard_RendersAsBefore()
	{
		IRenderedComponent<MokaBentoItem> cut = Render<MokaBentoItem>(p => p
			.Add(x => x.Title, "Title")
			.Add(x => x.Description, "About")
			.Add(x => x.ColSpan, 2)
			.AddChildContent("<span>x</span>"));

		cut.MarkupMatches(
			"""<div class="moka-bento-item" style="grid-column: span 2"><div class="moka-bento-item__title">Title</div><div class="moka-bento-item__desc">About</div><div class="moka-bento-item__content"><span>x</span></div></div>""");
		Assert.Empty(JSInterop.Invocations);
	}

	[Fact]
	public void Board_DrawsTheShownCardsInItsOrder_WithTheirSizes()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout);

		IReadOnlyList<IElement> items = cut.FindAll(".moka-bento-item");
		Assert.Equal(["a", "b", "d"], items.Select(i => i.GetAttribute("data-card")));
		Assert.Contains("moka-bento-item--wide", items[0].ClassList);
		Assert.Contains("moka-bento-item--small", items[1].ClassList);
		Assert.Contains("moka-bento-item--full", items[2].ClassList);
		Assert.Null(items[0].GetAttribute("style"));

		IElement root = cut.Find(".moka-bento-grid");
		Assert.Contains("moka-bento-grid--board", root.ClassList);
		Assert.Null(root.GetAttribute("style"));
	}

	[Fact]
	public void Card_IsAnArticleNamedByTheBoardsTitle()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout);

		IElement item = cut.Find("[data-card=\"a\"]");
		Assert.Equal("article", item.GetAttribute("role"));
		Assert.Equal("Internet", item.GetAttribute("aria-label"));
		Assert.Equal("Internet", item.QuerySelector("h2.moka-bento-item__heading")?.TextContent);
		Assert.Equal("a body", item.QuerySelector(".moka-bento-item__body")?.TextContent);
	}

	[Fact]
	public void Board_WhenNotCustomizing_HasNoToolsTrayOrScript()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout);

		Assert.Empty(cut.FindAll(".moka-bento-item__tools"));
		Assert.Empty(cut.FindAll(".moka-bento-grid__tray"));
		Assert.Empty(cut.FindAll(".moka-bento-grid__bar"));
		Assert.Empty(cut.FindAll(".moka-bento-item--customizing"));
		Assert.Empty(_board.Invocations);
		Assert.Empty(_keys.Invocations);
	}

	[Fact]
	public void Customizing_AddsToolsRingTrayAndBar()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true);

		foreach (IElement item in cut.FindAll(".moka-bento-item"))
		{
			Assert.Contains("moka-bento-item--customizing", item.ClassList);
			IReadOnlyList<IElement> tools = item.QuerySelectorAll(".moka-bento-item__tools [data-tool]").ToList();
			Assert.Equal(["grip", "earlier", "later", "size", "hide"], tools.Select(t => t.GetAttribute("data-tool")));
		}

		IElement chip = Assert.Single(cut.FindAll(".moka-bento-grid__tray [data-tray-card]"));
		Assert.Equal("c", chip.GetAttribute("data-tray-card"));
		Assert.Equal("Show Plugins", chip.GetAttribute("aria-label"));
		Assert.Equal("Plugins", chip.TextContent.Trim());
		Assert.Equal("Done", cut.Find(".moka-bento-grid__done").TextContent.Trim());
		Assert.Empty(cut.FindAll(".moka-bento-grid__reset"));

		IElement tray = cut.Find(".moka-bento-grid__tray");
		Assert.Equal("group", tray.GetAttribute("role"));
		IElement label = cut.Find($"#{tray.GetAttribute("aria-labelledby")}");
		Assert.Equal("Hidden", label.TextContent.Trim());
		Assert.Contains("moka-bento-grid--customizing", cut.Find(".moka-bento-grid").ClassList);
	}

	[Fact]
	public void ToolClicks_DoNotClickAClickableCard()
	{
		int clicks = 0;
		RenderFragment<MokaBoardCard> clickable = card => builder =>
		{
			builder.OpenComponent<MokaBentoItem>(0);
			builder.AddComponentParameter(1, nameof(MokaBentoItem.Card), card);
			builder.AddComponentParameter(2, nameof(MokaBentoItem.Clickable), true);
			builder.AddComponentParameter(3, nameof(MokaBentoItem.OnClick), EventCallback.Factory.Create(this, () => clicks++));
			builder.CloseComponent();
		};
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true, template: clickable);

		Tool(cut, "b", "size").Click();
		Assert.Equal(0, clicks);
		Assert.Equal("button", cut.Find("[data-card=\"b\"]").GetAttribute("role"));

		cut.Find("[data-card=\"b\"]").Click();
		Assert.Equal(1, clicks);
	}

	[Fact]
	public void Tools_AreNamedAndSitInTooltips()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true);

		IconTooltip.AssertNamed(Tool(cut, "b", "grip"), "Move Devices: drag, or press the arrow keys", MokaTooltipPosition.BottomStart);
		IconTooltip.AssertNamed(Tool(cut, "b", "earlier"), "Move Devices earlier", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(Tool(cut, "b", "later"), "Move Devices later", MokaTooltipPosition.Bottom);
		IconTooltip.AssertNamed(Tool(cut, "b", "hide"), "Hide Devices", MokaTooltipPosition.BottomEnd);

		IElement size = Tool(cut, "b", "size");
		Assert.Equal("Small", size.TextContent.Trim());
		Assert.Equal("Size of Devices: Small. Change it", size.GetAttribute("aria-label"));
		Assert.Equal("Arrange Devices", cut.Find("[data-card=\"b\"] .moka-bento-item__tools").GetAttribute("aria-label"));
	}

	[Fact]
	public void EarlierAndLater_AreDisabledAtTheEnds()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true);

		Assert.True(Tool(cut, "a", "earlier").HasAttribute("disabled"));
		Assert.False(Tool(cut, "a", "later").HasAttribute("disabled"));
		Assert.False(Tool(cut, "b", "earlier").HasAttribute("disabled"));
		Assert.False(Tool(cut, "b", "later").HasAttribute("disabled"));
		Assert.True(Tool(cut, "d", "later").HasAttribute("disabled"));
	}

	[Fact]
	public void Later_SwapsWithTheNextShownCard_AndFocusFollows()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true);
		string root = RootReference(cut);

		Tool(cut, "b", "later").Click();

		Assert.Equal(Cards(("a", MokaBoardSize.Wide, false), ("d", MokaBoardSize.Full, false), ("c", MokaBoardSize.Small, true), ("b", MokaBoardSize.Small, false)),
			Assert.Single(_raised));
		Assert.Equal(["a", "d", "b"], cut.FindAll(".moka-bento-item").Select(i => i.GetAttribute("data-card")));
		AssertFocused(root, "b", "later");
	}

	[Fact]
	public void Earlier_SwapsWithThePreviousShownCard()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true);

		Tool(cut, "b", "earlier").Click();

		Assert.Equal(["b", "a", "c", "d"], Assert.Single(_raised).Select(c => c.Id));
		Assert.Equal(["b", "a", "d"], cut.FindAll(".moka-bento-item").Select(i => i.GetAttribute("data-card")));
		Assert.True(Tool(cut, "b", "earlier").HasAttribute("disabled"));
	}

	[Fact]
	public void ArrowKeysOnTheGrip_StepTheCard_AndFocusTheGrip()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true);
		string root = RootReference(cut);

		Tool(cut, "b", "grip").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

		Assert.Equal(["b", "a", "c", "d"], Assert.Single(_raised).Select(c => c.Id));
		AssertFocused(root, "b", "grip");

		Tool(cut, "b", "grip").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

		Assert.Equal(["a", "b", "c", "d"], _raised[^1].Select(c => c.Id));
	}

	[Fact]
	public void OtherKeysOnTheGrip_MoveNothing()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true);

		Tool(cut, "b", "grip").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", CtrlKey = true });
		Tool(cut, "b", "grip").KeyDown(new KeyboardEventArgs { Key = "Tab" });
		Tool(cut, "a", "grip").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

		Assert.Empty(_raised);
	}

	// The browser would scroll the page on the same arrows. The rule has to be in the shape moka-keys.js reads.
	[Fact]
	public void GripArrows_AreCancelledInTheBrowser()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true);
		string root = RootReference(cut);

		JSRuntimeInvocation call = Assert.Single(_keys.Invocations, i => i.Identifier == "preventKeys");
		Assert.Equal(root, Assert.IsType<ElementReference>(call.Arguments[0]).Id);
		using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(call.Arguments[1]));
		JsonElement rule = Assert.Single(document.RootElement.EnumerateArray());
		Assert.Equal("[data-tool=\"grip\"]", rule.GetProperty("selector").GetString());
		Assert.Equal(["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown"], rule.GetProperty("keys").EnumerateArray().Select(k => k.GetString()));
		Assert.True(rule.GetProperty("unlessModified").GetBoolean());
	}

	[Fact]
	public void Customizing_BindsTheDragOnce()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true);
		string root = RootReference(cut);

		Tool(cut, "b", "size").Click();
		Tool(cut, "b", "size").Click();

		JSRuntimeInvocation bind = Assert.Single(_board.Invocations, i => i.Identifier == "bindBoard");
		Assert.Equal(root, Assert.IsType<ElementReference>(bind.Arguments[0]).Id);
		Assert.IsType<DotNetObjectReference<MokaBentoGrid>>(bind.Arguments[1]);
	}

	[Fact]
	public void BindsTheDrag_OnlyOnceCustomizingStarts()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout);
		Assert.Empty(_board.Invocations);

		cut.Render(p => p.Add(x => x.Customizing, true));

		Assert.Single(_board.Invocations, i => i.Identifier == "bindBoard");
	}

	[Fact]
	public void Size_CyclesSmallWideFull()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true);

		Tool(cut, "b", "size").Click();
		Assert.Equal(MokaBoardSize.Wide, _raised[^1][1].Size);
		Assert.Contains("moka-bento-item--wide", cut.Find("[data-card=\"b\"]").ClassList);
		Assert.Equal("Wide", Tool(cut, "b", "size").TextContent.Trim());

		Tool(cut, "b", "size").Click();
		Assert.Equal(MokaBoardSize.Full, _raised[^1][1].Size);

		Tool(cut, "b", "size").Click();
		Assert.Equal(MokaBoardSize.Small, _raised[^1][1].Size);
		Assert.Equal(["a", "b", "c", "d"], _raised[^1].Select(c => c.Id));
	}

	[Fact]
	public void Hide_SendsTheCardToTheTray()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true);
		string root = RootReference(cut);

		Tool(cut, "a", "hide").Click();

		Assert.Equal(Cards(("a", MokaBoardSize.Wide, true), ("b", MokaBoardSize.Small, false), ("c", MokaBoardSize.Small, true), ("d", MokaBoardSize.Full, false)),
			Assert.Single(_raised));
		Assert.Equal(["b", "d"], cut.FindAll(".moka-bento-item").Select(i => i.GetAttribute("data-card")));
		Assert.Equal(["Show Internet", "Show Plugins"], cut.FindAll("[data-tray-card]").Select(c => c.GetAttribute("aria-label")));
		AssertFocused(root, "a", "tray");
	}

	[Fact]
	public void TrayCard_ComesBackAfterTheLastShownCard()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true);
		string root = RootReference(cut);

		cut.Find("[data-tray-card=\"c\"]").Click();

		Assert.Equal(Cards(("a", MokaBoardSize.Wide, false), ("b", MokaBoardSize.Small, false), ("d", MokaBoardSize.Full, false), ("c", MokaBoardSize.Small, false)),
			Assert.Single(_raised));
		Assert.Equal(["a", "b", "d", "c"], cut.FindAll(".moka-bento-item").Select(i => i.GetAttribute("data-card")));
		Assert.Equal("Nothing hidden", cut.Find(".moka-bento-grid__tray-empty").TextContent);
		AssertFocused(root, "c", "grip");
	}

	[Fact]
	public void Dropped_PutsTheCardBeforeOrAfterTheTarget()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true);

		cut.InvokeAsync(() => cut.Instance.Dropped("d", "a", true));
		Assert.Equal(["d", "a", "b", "c"], _raised[^1].Select(c => c.Id));
		Assert.Equal(["d", "a", "b"], cut.FindAll(".moka-bento-item").Select(i => i.GetAttribute("data-card")));

		cut.InvokeAsync(() => cut.Instance.Dropped("d", "b", false));
		Assert.Equal(["a", "b", "d", "c"], _raised[^1].Select(c => c.Id));
		Assert.Equal(2, _raised.Count);
	}

	[Fact]
	public void Dropped_WhereNothingMoves_RaisesNothing()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true);

		cut.InvokeAsync(() => cut.Instance.Dropped("a", "a", true));
		cut.InvokeAsync(() => cut.Instance.Dropped("a", "b", true));
		cut.InvokeAsync(() => cut.Instance.Dropped("a", "zz", true));
		cut.InvokeAsync(() => cut.Instance.Dropped("zz", "a", true));

		Assert.Empty(_raised);
	}

	[Fact]
	public void Dropped_OutsideCustomizeMode_RaisesNothing()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout);

		cut.InvokeAsync(() => cut.Instance.Dropped("d", "a", true));

		Assert.Empty(_raised);
	}

	[Fact]
	public void Done_EndsCustomizeMode()
	{
		List<bool> customizing = [];
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true, configure: p => p
			.Add(x => x.CustomizingChanged, value => customizing.Add(value)));

		cut.Find(".moka-bento-grid__done").Click();

		Assert.Equal([false], customizing);
		Assert.Empty(cut.FindAll(".moka-bento-item__tools"));
		Assert.Empty(cut.FindAll(".moka-bento-grid__tray"));
	}

	[Fact]
	public void BackToTheDefault_RaisesTheDefault_AndIsDisabledThere()
	{
		IReadOnlyList<MokaBoardCard> defaults = [new("d", MokaBoardSize.Full), new("a"), new("b"), new("c")];
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true, configure: p => p
			.Add(x => x.DefaultBoard, defaults));
		string root = RootReference(cut);

		IElement reset = cut.Find(".moka-bento-grid__reset");
		Assert.Equal("Back to the default", reset.TextContent.Trim());
		Assert.False(reset.HasAttribute("disabled"));

		reset.Click();

		Assert.Equal(defaults, Assert.Single(_raised));
		Assert.NotSame(defaults, _raised[0]);
		Assert.True(cut.Find(".moka-bento-grid__reset").HasAttribute("disabled"));
		Assert.Empty(cut.FindAll("[data-tray-card]"));
		AssertFocused(root, null, "done");
	}

	[Fact]
	public void BoardParameter_ReplacesTheLayout_OnlyWhenItsCardsChange()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(Layout, customizing: true);
		Tool(cut, "b", "size").Click();
		IReadOnlyList<MokaBoardCard> changed = _raised[^1];

		cut.Render(p => p.Add(x => x.Board, Layout));
		cut.Render(p => p.Add(x => x.Board, [.. Layout]));
		Assert.Contains("moka-bento-item--wide", cut.Find("[data-card=\"b\"]").ClassList);

		// An app that binds the layout passes the change back, then puts the stored one back when saving fails.
		cut.Render(p => p.Add(x => x.Board, changed));
		cut.Render(p => p.Add(x => x.Board, Layout));
		Assert.Contains("moka-bento-item--small", cut.Find("[data-card=\"b\"]").ClassList);
	}

	[Fact]
	public void Board_DropsRepeatedAndEmptyIds()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board([new("a"), new("a", MokaBoardSize.Small), new(""), new("b")], customizing: true);

		Assert.Equal(["a", "b"], cut.FindAll(".moka-bento-item").Select(i => i.GetAttribute("data-card")));
		Assert.Contains("moka-bento-item--wide", cut.Find("[data-card=\"a\"]").ClassList);

		Tool(cut, "a", "later").Click();
		Assert.Equal(["b", "a"], _raised[^1].Select(c => c.Id));
	}

	[Fact]
	public void Item_Head_HasIconSourceAndOpenLink()
	{
		IRenderedComponent<MokaBentoItem> cut = Render<MokaBentoItem>(p => p
			.Add(x => x.Card, new MokaBoardCard("plugins", MokaBoardSize.Small))
			.Add(x => x.Title, "Plugins")
			.Add(x => x.Icon, MokaIcons.Content.Copy)
			.Add(x => x.Source, "Plugin")
			.Add(x => x.Href, "/plugins"));

		IElement head = cut.Find(".moka-bento-item__head");
		Assert.NotNull(head.QuerySelector(".moka-bento-item__icon svg"));
		Assert.Equal("Plugins", head.QuerySelector("h2")?.TextContent);
		Assert.Equal("Plugin", head.QuerySelector(".moka-bento-item__source")?.TextContent);
		IElement open = cut.Find("a.moka-bento-item__open");
		Assert.Equal("/plugins", open.GetAttribute("href"));
		Assert.Equal("Open", open.TextContent);
		Assert.Contains("moka-bento-item--small", cut.Find(".moka-bento-item").ClassList);
		Assert.Empty(cut.FindAll(".moka-bento-item__tools"));
	}

	[Fact]
	public void Item_OpenLink_DropsAScriptAddress()
	{
		IRenderedComponent<MokaBentoItem> cut = Render<MokaBentoItem>(p => p
			.Add(x => x.Card, new MokaBoardCard("a"))
			.Add(x => x.Href, "javascript:alert(1)"));

		Assert.Empty(cut.FindAll("a"));
		Assert.Equal("a", cut.Find("h2").TextContent);
	}

	[Fact]
	public void Item_WithAHeadButNoCard_KeepsItsSpansAndRole()
	{
		IRenderedComponent<MokaBentoItem> cut = Render<MokaBentoItem>(p => p
			.Add(x => x.Title, "Devices")
			.Add(x => x.Href, "/devices")
			.Add(x => x.ColSpan, 2));

		IElement item = cut.Find(".moka-bento-item");
		Assert.Contains("moka-bento-item--tile", item.ClassList);
		Assert.Equal("grid-column: span 2", item.GetAttribute("style"));
		Assert.False(item.HasAttribute("role"));
		Assert.False(item.HasAttribute("aria-label"));
		Assert.False(item.HasAttribute("data-card"));
		Assert.Empty(cut.FindAll(".moka-bento-item__title"));
	}

	private IRenderedComponent<MokaBentoGrid> Board(IReadOnlyList<MokaBoardCard> board, bool customizing = false,
		Action<ComponentParameterCollectionBuilder<MokaBentoGrid>>? configure = null, RenderFragment<MokaBoardCard>? template = null) =>
		Render<MokaBentoGrid>(p =>
		{
			p.Add(x => x.Board, board)
				.Add(x => x.Customizing, customizing)
				.Add(x => x.CardTemplate, template ?? Template)
				.Add(x => x.CardTitle, id => Titles.GetValueOrDefault(id, id))
				.Add(x => x.BoardChanged, layout => _raised.Add(layout));
			configure?.Invoke(p);
		});

	private static IElement Tool(IRenderedComponent<MokaBentoGrid> cut, string card, string tool) =>
		cut.Find($"[data-card=\"{card}\"] [data-tool=\"{tool}\"]");

	// bUnit writes an element's reference id only on the render that creates it.
	private static string RootReference(IRenderedComponent<MokaBentoGrid> cut) =>
		cut.Find(".moka-bento-grid").GetAttribute("blazor:elementReference") ?? "";

	private void AssertFocused(string root, string? card, string part)
	{
		JSRuntimeInvocation focus = _board.Invocations.Last(i => i.Identifier == "focusBoard");
		Assert.Equal(root, Assert.IsType<ElementReference>(focus.Arguments[0]).Id);
		Assert.Equal(card, focus.Arguments[1]);
		Assert.Equal(part, focus.Arguments[2]);
	}

	private static List<MokaBoardCard> Cards(params (string Id, MokaBoardSize Size, bool Hidden)[] cards) =>
		[.. cards.Select(c => new MokaBoardCard(c.Id, c.Size, c.Hidden))];
}
