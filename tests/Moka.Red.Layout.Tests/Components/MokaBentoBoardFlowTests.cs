using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Layout.BentoGrid;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// Flow Rows keeps a board's cards in their order: a wrapping row in which each card starts from its size's share of the
// columns and grows into what its row leaves. Dense, the default, is the grid it has always been. The layout lives in the
// stylesheet, so its rules are read as text.
public class MokaBentoBoardFlowTests : BunitContext
{
	private const string GridCss = "MokaBentoGrid.razor.css";
	private const string Cards = ".moka-bento-grid__cards";
	private const string RowsCards = ".moka-bento-grid--rows > .moka-bento-grid__cards";
	private const string RowsCard = ".moka-bento-grid--rows > .moka-bento-grid__cards > .moka-bento-grid__slot ::deep > *";

	private static readonly IReadOnlyList<MokaBoardCard> Layout =
	[
		new("a", MokaBoardSize.Wide),
		new("b", MokaBoardSize.Small, Hidden: true),
		new("c", MokaBoardSize.Small),
		new("d", MokaBoardSize.Full)
	];

	public MokaBentoBoardFlowTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	private static RenderFragment<MokaBoardCard> Template => card => builder =>
	{
		builder.OpenComponent<MokaBentoItem>(0);
		builder.AddComponentParameter(1, nameof(MokaBentoItem.Card), card);
		builder.CloseComponent();
	};

	[Fact]
	public void ByDefault_TheBoardIsDense_AndDrawsAsBefore()
	{
		IRenderedComponent<MokaBentoGrid> cut = Render<MokaBentoGrid>(p => p
			.Add(x => x.Board, Layout)
			.Add(x => x.CardTemplate, Template));

		Assert.Equal(MokaBoardFlow.Dense, cut.Instance.Flow);
		Assert.Equal("moka-bento-grid moka-fill-width moka-bento-grid--board", cut.Find(".moka-bento-grid").ClassName);
	}

	[Fact]
	public void Rows_MarksTheBoard_AndLeavesTheCardsAsTheyAre()
	{
		IRenderedComponent<MokaBentoGrid> rows = Board(MokaBoardFlow.Rows);
		IRenderedComponent<MokaBentoGrid> dense = Board(MokaBoardFlow.Dense);

		IElement root = rows.Find(".moka-bento-grid");
		Assert.Equal("moka-bento-grid moka-fill-width moka-bento-grid--board moka-bento-grid--rows", root.ClassName);
		Assert.Null(root.GetAttribute("style"));
		rows.Find(".moka-bento-grid__cards").MarkupMatches(dense.Find(".moka-bento-grid__cards").OuterHtml);
		Assert.Equal(["a", "c", "d"], rows.FindAll(".moka-bento-item").Select(i => i.GetAttribute("data-card")));
	}

	[Fact]
	public void Rows_WorksWithStretchCardsAndCustomizeMode()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(MokaBoardFlow.Rows, p => p
			.Add(x => x.StretchCards, true)
			.Add(x => x.Customizing, true));

		Assert.Equal(
			"moka-bento-grid moka-fill-width moka-bento-grid--board moka-bento-grid--customizing moka-bento-grid--stretch moka-bento-grid--rows",
			cut.Find(".moka-bento-grid").ClassName);
		Assert.Equal(3, cut.FindAll(".moka-bento-item__tools").Count);
	}

	[Fact]
	public void Rows_WithoutABoard_IsNotUsed()
	{
		IRenderedComponent<MokaBentoGrid> cut = Render<MokaBentoGrid>(p => p.Add(x => x.Flow, MokaBoardFlow.Rows));

		IElement root = cut.Find(".moka-bento-grid");
		Assert.Equal("moka-bento-grid moka-fill-width", root.ClassName);
		Assert.StartsWith("grid-template-columns: repeat(4, minmax(0, 1fr));", root.GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void TheFlowCanChange()
	{
		IRenderedComponent<MokaBentoGrid> cut = Board(MokaBoardFlow.Dense);

		cut.Render(p => p.Add(x => x.Flow, MokaBoardFlow.Rows));
		Assert.Contains("moka-bento-grid--rows", cut.Find(".moka-bento-grid").ClassList);

		cut.Render(p => p.Add(x => x.Flow, MokaBoardFlow.Dense));
		Assert.DoesNotContain("moka-bento-grid--rows", cut.Find(".moka-bento-grid").ClassList);
	}

	// Both flows use the board's columns, 4, 2 below 1180 px and 1 on a phone, and its gap, from one place.
	[Fact]
	public void TheGrid_TakesItsColumnsAndGapFromTheBoardsVariables()
	{
		IReadOnlyDictionary<string, string> cards = ScopedCss.Declarations(GridCss, Cards);

		Assert.Equal("grid", cards["display"]);
		Assert.Equal("repeat(var(--_board-columns), minmax(0, 1fr))", cards["grid-template-columns"]);
		Assert.Equal("dense", cards["grid-auto-flow"]);
		Assert.Equal("var(--_board-gap)", cards["gap"]);
		Assert.Equal("var(--moka-grid-gap)", cards["--_board-gap"]);
		Assert.Equal(["4", "2", "1"], ScopedCss.Values(GridCss, Cards, "--_board-columns"));
	}

	// The cards keep their order in a wrapping row. Alignment is left to the rules the grid uses, so StretchCards works
	// the same on both.
	[Fact]
	public void Rows_WrapTheCardsInOrder_AndShareTheGridsAlignment()
	{
		IReadOnlyDictionary<string, string> rows = ScopedCss.Declarations(GridCss, RowsCards);

		Assert.Equal("flex", rows["display"]);
		Assert.Equal("wrap", rows["flex-wrap"]);
		Assert.False(rows.ContainsKey("align-items"));
		Assert.False(rows.ContainsKey("gap"));
		Assert.Equal("start", ScopedCss.Declarations(GridCss, Cards)["align-items"]);
		Assert.Equal("stretch", ScopedCss.Declarations(GridCss, ".moka-bento-grid--stretch > .moka-bento-grid__cards")["align-items"]);
	}

	// A card's basis is its share of the columns with the gaps between them, so shares that add up to the columns fill a
	// row exactly; it grows by that share, which the board's script reads back; and wide content is cut, not widening it.
	[Fact]
	public void Rows_ACardStartsFromItsShareAndGrowsByIt()
	{
		IReadOnlyDictionary<string, string> card = ScopedCss.Declarations(GridCss, RowsCard);

		Assert.Equal("1", card["--_board-span"]);
		Assert.Equal("min(var(--_board-span), var(--_board-columns))", card["--_board-share"]);
		Assert.Equal(
			"var(--_board-share) 1 calc((100% - (var(--_board-columns) - 1) * var(--_board-gap)) * var(--_board-share) / var(--_board-columns) + (var(--_board-share) - 1) * var(--_board-gap))",
			card["flex"]);
		Assert.Equal("0", card["min-width"]);
	}

	[Theory]
	[InlineData("wide", "2")]
	[InlineData("full", "var(--_board-columns)")]
	public void Rows_ASizeSetsTheCardsSpan(string size, string span)
	{
		IReadOnlyDictionary<string, string> card = ScopedCss.Declarations(GridCss,
			$".moka-bento-grid--rows > .moka-bento-grid__cards > .moka-bento-grid__slot ::deep > .moka-bento-item--{size}");

		Assert.Equal(span, Assert.Single(card).Value);
		Assert.Equal("--_board-span", card.Keys.Single());
	}

	// The basis written out for 4, 2 and 1 columns: every way to fill a row with spans that add up to the columns takes
	// exactly its width, and one more column's card doesn't fit.
	[Theory]
	[InlineData(4, "1 1 1 1")]
	[InlineData(4, "2 1 1")]
	[InlineData(4, "1 2 1")]
	[InlineData(4, "2 2")]
	[InlineData(4, "4")]
	[InlineData(2, "1 1")]
	[InlineData(2, "2")]
	[InlineData(1, "1")]
	public void Rows_SharesThatAddUpToTheColumns_FillARowExactly(int columns, string spans)
	{
		const double width = 997.3;
		const double gap = 16;
		double Basis(int span)
		{
			int share = Math.Min(span, columns);
			return (width - (columns - 1) * gap) * share / columns + (share - 1) * gap;
		}

		ArgumentNullException.ThrowIfNull(spans);
		int[] row = [.. spans.Split(' ').Select(int.Parse)];
		double filled = row.Sum(Basis) + (row.Length - 1) * gap;

		Assert.Equal(width, filled, 9);
		Assert.True(filled + gap + Basis(1) > width);
	}

	private IRenderedComponent<MokaBentoGrid> Board(MokaBoardFlow flow, Action<ComponentParameterCollectionBuilder<MokaBentoGrid>>? configure = null) =>
		Render<MokaBentoGrid>(p =>
		{
			p.Add(x => x.Board, Layout)
				.Add(x => x.CardTemplate, Template)
				.Add(x => x.Flow, flow);
			configure?.Invoke(p);
		});
}
