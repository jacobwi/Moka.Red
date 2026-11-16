using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Kanban;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// The board moved cards with the browser's own drag and drop: nothing showed where a card would land, a drop always went
// to the end of the column, a card couldn't move within its column, and the keyboard and touch couldn't move one at all.
// The moving now happens in the board's script, which reports only the drop.
public sealed class MokaKanbanBoardTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Primitives/Kanban/MokaKanbanBoard.razor.js";
	private const string Css = "MokaKanbanBoard.razor.css";

	private readonly BunitJSModuleInterop _module;
	private readonly List<MokaKanbanItemMovedArgs<string>> _moves = [];

	public MokaKanbanBoardTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
	}

	private static List<MokaKanbanColumn<string>> Columns() =>
	[
		new("To do", new List<string> { "Draft", "Collect", "Plan" }),
		new("Doing", new List<string> { "Review" }, MokaColor.Info),
		new("Done", new List<string>())
	];

	private IRenderedComponent<MokaKanbanBoard<string>> Board(IReadOnlyList<MokaKanbanColumn<string>> columns, bool disabled = false) =>
		Render<MokaKanbanBoard<string>>(p => p
			.Add(x => x.Columns, columns)
			.Add(x => x.Disabled, disabled)
			.Add(x => x.OnItemMoved, (MokaKanbanItemMovedArgs<string> move) => _moves.Add(move)));

	[Fact]
	public void Cards_AreListItemsTheKeyboardCanReach_InColumnsNamedByTheirTitles()
	{
		IRenderedComponent<MokaKanbanBoard<string>> cut = Board(Columns());

		IElement body = cut.FindAll(".moka-kanban__column-body")[0];
		Assert.Equal("list", body.GetAttribute("role"));
		Assert.Equal("To do", cut.Find($"#{body.GetAttribute("aria-labelledby")}").TextContent);

		IReadOnlyList<IElement> cards = body.QuerySelectorAll(".moka-kanban__card").ToList();
		Assert.Equal(["0", "1", "2"], cards.Select(c => c.GetAttribute("data-index")));
		Assert.All(cards, card =>
		{
			Assert.Equal("listitem", card.GetAttribute("role"));
			Assert.Equal("0", card.GetAttribute("tabindex"));
			Assert.Equal(MokaKanbanText.Default.Instructions, cut.Find($"#{card.GetAttribute("aria-describedby")}").TextContent);
		});
		Assert.Equal("2", cut.FindAll(".moka-kanban__column")[2].GetAttribute("data-column"));
	}

	[Fact]
	public void BindsTheScriptOnce_WithTheWordsItAnnounces()
	{
		IRenderedComponent<MokaKanbanBoard<string>> cut = Board(Columns());
		cut.Render();

		JSRuntimeInvocation bind = Assert.Single(_module.Invocations, i => i.Identifier == "bindBoard");
		Assert.IsType<ElementReference>(bind.Arguments[0]);
		Assert.IsType<DotNetObjectReference<MokaKanbanBoard<string>>>(bind.Arguments[1]);
		Dictionary<string, object?> words = Assert.IsType<Dictionary<string, object?>>(bind.Arguments[2]);
		Assert.Equal(MokaKanbanText.Default.PickedUp, words["pickedUp"]);
		Assert.Equal(MokaKanbanText.Default.Dropped, words["dropped"]);
	}

	[Fact]
	public void ADisabledBoard_LoadsNoScript_AndTakesNoFocus()
	{
		IRenderedComponent<MokaKanbanBoard<string>> cut = Board(Columns(), disabled: true);

		Assert.Empty(_module.Invocations);
		Assert.Contains("moka-kanban--disabled", cut.Find(".moka-kanban").ClassList);
		Assert.All(cut.FindAll(".moka-kanban__card"), card =>
		{
			Assert.False(card.HasAttribute("tabindex"));
			Assert.False(card.HasAttribute("aria-describedby"));
		});
	}

	[Fact]
	public void Disabling_UnbindsTheScript()
	{
		IRenderedComponent<MokaKanbanBoard<string>> cut = Board(Columns());

		cut.Render(p => p.Add(x => x.Disabled, true));

		Assert.Single(_module.Invocations, i => i.Identifier == "unbindBoard");
	}

	[Fact]
	public async Task ADrop_InAnotherColumn_GoesWhereTheBarShowed_AndIsReported()
	{
		List<MokaKanbanColumn<string>> columns = Columns();
		IRenderedComponent<MokaKanbanBoard<string>> cut = Board(columns);

		await cut.InvokeAsync(() => cut.Instance.Move(0, 1, 1, 0, false));

		Assert.Equal(["Draft", "Plan"], columns[0].Items);
		Assert.Equal(["Collect", "Review"], columns[1].Items);
		cut.WaitForAssertion(() => Assert.Single(_moves));
		Assert.Equal(new MokaKanbanItemMovedArgs<string>("Collect", 0, 1) { FromIndex = 1, ToIndex = 0 }, _moves[0]);
		Assert.Equal(["Collect", "Review"], cut.FindAll(".moka-kanban__column-body")[1].QuerySelectorAll(".moka-kanban__card").Select(c => c.TextContent.Trim()));
	}

	[Fact]
	public async Task ADrop_InItsOwnColumn_Reorders()
	{
		List<MokaKanbanColumn<string>> columns = Columns();
		IRenderedComponent<MokaKanbanBoard<string>> cut = Board(columns);

		await cut.InvokeAsync(() => cut.Instance.Move(0, 0, 0, 2, false));

		Assert.Equal(["Collect", "Plan", "Draft"], columns[0].Items);
		cut.WaitForAssertion(() => Assert.Single(_moves));
		Assert.Equal(0, _moves[0].ToColumnIndex);
		Assert.Equal(2, _moves[0].ToIndex);
	}

	[Fact]
	public async Task ADrop_InAnEmptyColumn_TakesItsFirstPlace()
	{
		List<MokaKanbanColumn<string>> columns = Columns();
		IRenderedComponent<MokaKanbanBoard<string>> cut = Board(columns);

		await cut.InvokeAsync(() => cut.Instance.Move(1, 0, 2, 5, false));

		Assert.Equal(["Review"], columns[2].Items);
		cut.WaitForAssertion(() => Assert.Equal(0, Assert.Single(_moves).ToIndex));
	}

	[Fact]
	public async Task ADrop_WhereTheCardWas_ChangesAndReportsNothing()
	{
		List<MokaKanbanColumn<string>> columns = Columns();
		IRenderedComponent<MokaKanbanBoard<string>> cut = Board(columns);

		await cut.InvokeAsync(() => cut.Instance.Move(0, 1, 0, 1, false));

		Assert.Equal(["Draft", "Collect", "Plan"], columns[0].Items);
		Assert.Empty(_moves);
	}

	[Theory]
	[InlineData(-1, 0, 1, 0)]
	[InlineData(0, 9, 1, 0)]
	[InlineData(0, 0, 7, 0)]
	public async Task ADropThatNamesNoCardOrColumn_IsIgnored(int fromColumn, int fromIndex, int toColumn, int toIndex)
	{
		List<MokaKanbanColumn<string>> columns = Columns();
		IRenderedComponent<MokaKanbanBoard<string>> cut = Board(columns);

		await cut.InvokeAsync(() => cut.Instance.Move(fromColumn, fromIndex, toColumn, toIndex, false));

		Assert.Equal(["Draft", "Collect", "Plan"], columns[0].Items);
		Assert.Empty(_moves);
	}

	[Fact]
	public async Task AKeyboardMove_SendsFocusToTheCardsNewPlace()
	{
		IRenderedComponent<MokaKanbanBoard<string>> cut = Board(Columns());

		await cut.InvokeAsync(() => cut.Instance.Move(0, 0, 2, 0, true));

		cut.WaitForAssertion(() =>
		{
			JSRuntimeInvocation focus = Assert.Single(_module.Invocations, i => i.Identifier == "focusCard");
			Assert.Equal(2, focus.Arguments[1]);
			Assert.Equal(0, focus.Arguments[2]);
		});
	}

	[Fact]
	public async Task APointerMove_LeavesFocusAlone()
	{
		IRenderedComponent<MokaKanbanBoard<string>> cut = Board(Columns());

		await cut.InvokeAsync(() => cut.Instance.Move(0, 0, 2, 0, false));

		Assert.DoesNotContain(_module.Invocations, i => i.Identifier == "focusCard");
	}

	[Fact]
	public void ALimit_ShowsTheCountAgainstIt_AndMarksAColumnPastIt()
	{
		IRenderedComponent<MokaKanbanBoard<string>> cut = Board(
		[
			new("Doing", new List<string> { "a", "b", "c" }) { Limit = 2 },
			new("Done", new List<string> { "d" }) { Limit = 3 },
			new("Later", new List<string> { "e" })
		]);

		IReadOnlyList<IElement> counts = cut.FindAll(".moka-kanban__column-count");
		Assert.Equal(["3/2", "1/3", "1"], counts.Select(c => c.TextContent));
		Assert.Contains("moka-kanban__column-count--over", counts[0].ClassList);
		Assert.DoesNotContain("moka-kanban__column-count--over", counts[1].ClassList);
	}

	[Fact]
	public void AnItemTwiceInAColumn_RendersBothCards()
	{
		IRenderedComponent<MokaKanbanBoard<string>> cut = Board([new("Ideas", new List<string> { "same", "same", "other" })]);

		Assert.Equal(3, cut.FindAll(".moka-kanban__card").Count);
	}

	[Fact]
	public void ItemLabel_NamesTheCard()
	{
		IRenderedComponent<MokaKanbanBoard<string>> cut = Render<MokaKanbanBoard<string>>(p => p
			.Add(x => x.Columns, Columns())
			.Add(x => x.ItemLabel, item => $"Task {item}"));

		Assert.Equal("Task Draft", cut.Find(".moka-kanban__card").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheContextMenuListener_IsThereOnlyWhenSomethingListens()
	{
		IRenderedComponent<MokaKanbanBoard<string>> cut = Board(Columns());

		Assert.False(cut.Find(".moka-kanban__card").HasAttribute("blazor:oncontextmenu"));
	}

	[Theory]
	[InlineData(".moka-kanban__card[data-moka-lifted]", "opacity")]
	[InlineData(".moka-kanban__card[data-moka-ghost]", "box-shadow")]
	[InlineData(".moka-kanban__card[data-moka-picked]", "box-shadow")]
	[InlineData(".moka-kanban__column[data-moka-drop]", "box-shadow")]
	[InlineData(".moka-kanban[data-moka-dragging]", "cursor")]
	public void TheStyles_DrawEachStateTheScriptSets(string selector, string property) =>
		Assert.True(ScopedCss.Declarations(Css, selector).ContainsKey(property), $"{selector} sets no {property}.");
}
