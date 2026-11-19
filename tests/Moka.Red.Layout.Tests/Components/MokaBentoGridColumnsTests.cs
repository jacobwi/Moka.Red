using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Layout.BentoGrid;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// Columns of 1fr grew to fit wide content and pushed the grid past its edge, so they are minmax(0, 1fr) now. MinColumn
// fits as many columns as the width holds, and the items cap their spans at that count. StretchCards lines a board's
// cards up to their row.
public class MokaBentoGridColumnsTests : BunitContext
{
	private const string GridCss = "MokaBentoGrid.razor.css";
	private const string ItemCss = "MokaBentoItem.razor.css";

	public MokaBentoGridColumnsTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	private static RenderFragment Item(int colSpan = 1, int? colStart = null) => builder =>
	{
		builder.OpenComponent<MokaBentoItem>(0);
		builder.AddComponentParameter(1, nameof(MokaBentoItem.ColSpan), colSpan);
		builder.AddComponentParameter(2, nameof(MokaBentoItem.ColStart), colStart);
		builder.CloseComponent();
	};

	[Fact]
	public void Columns_ShareTheWidthEqually_WhateverTheContent()
	{
		IRenderedComponent<MokaBentoGrid> cut = Render<MokaBentoGrid>(p => p.Add(x => x.Columns, 3));

		Assert.StartsWith("grid-template-columns: repeat(3, minmax(0, 1fr));", cut.Find(".moka-bento-grid").GetAttribute("style"),
			StringComparison.Ordinal);
	}

	[Fact]
	public void AnItemInAPlainGrid_KeepsItsSpanInline()
	{
		IRenderedComponent<MokaBentoGrid> cut = Render<MokaBentoGrid>(p => p.Add(x => x.ChildContent, Item(2)));

		IElement item = cut.Find(".moka-bento-item");
		Assert.Equal("moka-bento-item", item.ClassName);
		Assert.Equal("grid-column: span 2", item.GetAttribute("style"));
	}

	[Theory]
	[InlineData("16rem", "16rem")]
	[InlineData("200", "200px")]
	[InlineData("var(--tile-min)", "var(--tile-min)")]
	public void MinColumn_FitsColumnsByWidth(string minColumn, string written)
	{
		IRenderedComponent<MokaBentoGrid> cut = Render<MokaBentoGrid>(p => p
			.Add(x => x.MinColumn, minColumn)
			.Add(x => x.GapValue, "1rem"));

		IElement root = cut.Find(".moka-bento-grid");
		Assert.Equal("moka-bento-grid moka-fill-width moka-bento-grid--fit", root.ClassName);
		Assert.Equal($"--_bento-min: {written}; --_bento-gap: 1rem; grid-auto-rows: minmax(120px, auto)", root.GetAttribute("style"));
	}

	[Theory]
	[InlineData("wide")]
	[InlineData("16rem; color: red")]
	[InlineData("calc(10rem + 1px)")]
	public void MinColumn_ThatIsntALengthOrAVar_IsIgnored(string minColumn)
	{
		IRenderedComponent<MokaBentoGrid> cut = Render<MokaBentoGrid>(p => p.Add(x => x.MinColumn, minColumn));

		IElement root = cut.Find(".moka-bento-grid");
		Assert.DoesNotContain("moka-bento-grid--fit", root.ClassList);
		Assert.StartsWith("grid-template-columns: repeat(4, minmax(0, 1fr));", root.GetAttribute("style"), StringComparison.Ordinal);
	}

	// The grid is a size container so its items can count the columns auto-fit makes: the most n for which n columns
	// and their gaps fit, floor((width + gap) / (column + gap)), at least one.
	[Fact]
	public void TheFittingGrid_CountsItsColumnsForItsItems()
	{
		IReadOnlyDictionary<string, string> fit = ScopedCss.Declarations(GridCss, ".moka-bento-grid--fit");

		Assert.Equal("inline-size", fit["container-type"]);
		Assert.Equal("repeat(auto-fit, minmax(min(100%, var(--_bento-min)), 1fr))", fit["grid-template-columns"]);
		Assert.Equal("var(--_bento-gap)", fit["gap"]);
		Assert.Equal("max(1, round(down, (100cqi + var(--_bento-gap)) / (var(--_bento-min) + var(--_bento-gap))))", fit["--_bento-fit"]);
	}

	[Fact]
	public void InAFittingGrid_ASpanIsCappedAtTheColumnsThatFit()
	{
		IRenderedComponent<MokaBentoGrid> cut = Render<MokaBentoGrid>(p => p
			.Add(x => x.MinColumn, "16rem")
			.Add(x => x.ChildContent, Item(3)));

		IElement item = cut.Find(".moka-bento-item");
		Assert.Equal("moka-bento-item moka-bento-item--fit", item.ClassName);
		Assert.Equal("--_bento-span: 3", item.GetAttribute("style"));
		Assert.Equal(["span min(var(--_bento-span), var(--_bento-fit))", "1 / -1"],
			ScopedCss.Values(ItemCss, ".moka-bento-item--fit", "grid-column"));
	}

	[Fact]
	public void InAFittingGrid_AStartMovesBackToTheLastColumnThatFits()
	{
		IRenderedComponent<MokaBentoGrid> cut = Render<MokaBentoGrid>(p => p
			.Add(x => x.MinColumn, "16rem")
			.Add(x => x.ChildContent, Item(2, 3)));

		IElement item = cut.Find(".moka-bento-item");
		Assert.Equal("moka-bento-item moka-bento-item--fit moka-bento-item--fit-at", item.ClassName);
		Assert.Equal("--_bento-start: 3; --_bento-span: 2", item.GetAttribute("style"));
		Assert.Equal(
			"min(var(--_bento-start), var(--_bento-fit)) / span min(var(--_bento-span), var(--_bento-fit) + 1 - min(var(--_bento-start), var(--_bento-fit)))",
			ScopedCss.Declarations(ItemCss, ".moka-bento-item--fit-at")["grid-column"]);
	}

	[Fact]
	public void InAFittingGrid_AnItemOfOneColumn_IsPlacedAsAlways()
	{
		IRenderedComponent<MokaBentoGrid> cut = Render<MokaBentoGrid>(p => p
			.Add(x => x.MinColumn, "16rem")
			.Add(x => x.ChildContent, Item()));

		IElement item = cut.Find(".moka-bento-item");
		Assert.Equal("moka-bento-item", item.ClassName);
		Assert.Null(item.GetAttribute("style"));
	}

	[Fact]
	public void InAFittingGrid_AStartBelowOne_IsLeftOut()
	{
		IRenderedComponent<MokaBentoGrid> cut = Render<MokaBentoGrid>(p => p
			.Add(x => x.MinColumn, "16rem")
			.Add(x => x.ChildContent, Item(2, -1)));

		IElement item = cut.Find(".moka-bento-item");
		Assert.Equal("moka-bento-item moka-bento-item--fit", item.ClassName);
		Assert.Equal("--_bento-span: 2", item.GetAttribute("style"));
	}

	// The items hear about MinColumn through the grid's cascade, so they follow it when it changes.
	[Fact]
	public void TheItemsFollow_WhenMinColumnComesAndGoes()
	{
		IRenderedComponent<MokaBentoGrid> cut = Render<MokaBentoGrid>(p => p.Add(x => x.ChildContent, Item(2)));
		Assert.Equal("grid-column: span 2", cut.Find(".moka-bento-item").GetAttribute("style"));

		cut.Render(p => p.Add(x => x.MinColumn, "16rem"));
		Assert.Equal("--_bento-span: 2", cut.Find(".moka-bento-item").GetAttribute("style"));

		cut.Render(p => p.Add(x => x.MinColumn, null));
		Assert.Equal("grid-column: span 2", cut.Find(".moka-bento-item").GetAttribute("style"));
	}

	[Fact]
	public void OnABoard_MinColumnIsNotUsed()
	{
		IRenderedComponent<MokaBentoGrid> cut = Render<MokaBentoGrid>(p => p
			.Add(x => x.Board, [new MokaBoardCard("a")])
			.Add(x => x.MinColumn, "16rem"));

		IElement root = cut.Find(".moka-bento-grid");
		Assert.DoesNotContain("moka-bento-grid--fit", root.ClassList);
		Assert.Null(root.GetAttribute("style"));
	}

	[Fact]
	public void StretchCards_LinesABoardsCardsUpToTheirRow()
	{
		IRenderedComponent<MokaBentoGrid> cut = Render<MokaBentoGrid>(p => p
			.Add(x => x.Board, [new MokaBoardCard("a")])
			.Add(x => x.StretchCards, true));

		Assert.Contains("moka-bento-grid--stretch", cut.Find(".moka-bento-grid").ClassList);
		Assert.Equal("start", ScopedCss.Declarations(GridCss, ".moka-bento-grid__cards")["align-items"]);
		Assert.Equal("stretch", ScopedCss.Declarations(GridCss, ".moka-bento-grid--stretch > .moka-bento-grid__cards")["align-items"]);
	}

	[Fact]
	public void StretchCards_ByDefault_CardsKeepTheirOwnHeight()
	{
		IRenderedComponent<MokaBentoGrid> board = Render<MokaBentoGrid>(p => p.Add(x => x.Board, [new MokaBoardCard("a")]));
		IRenderedComponent<MokaBentoGrid> plain = Render<MokaBentoGrid>(p => p.Add(x => x.StretchCards, true));

		Assert.DoesNotContain("moka-bento-grid--stretch", board.Find(".moka-bento-grid").ClassList);
		Assert.DoesNotContain("moka-bento-grid--stretch", plain.Find(".moka-bento-grid").ClassList);
	}
}
