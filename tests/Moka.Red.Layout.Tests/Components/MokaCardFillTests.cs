using AngleSharp.Dom;
using Bunit;
using Moka.Red.Layout.Card;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

public class MokaCardFillTests : BunitContext
{
	private const string Stylesheet = "MokaCard.razor.css";

	[Fact]
	public void Fill_MarksTheCard_AndGivesTheBodyAThinScrollbar()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Fill, true)
			.Add(x => x.Title, "Log")
			.Add(x => x.Footer, "Updated now")
			.AddChildContent("Lines"));

		Assert.Contains("moka-card--fill", cut.Find(".moka-card").ClassList);
		Assert.Contains("moka-thin-scrollbar", cut.Find(".moka-card-body").ClassList);
	}

	[Fact]
	public void WithoutFill_TheCardIsAsBefore()
	{
		IRenderedComponent<MokaCard> cut = Render<MokaCard>(p => p
			.Add(x => x.Title, "Log")
			.AddChildContent("Lines"));

		Assert.DoesNotContain("moka-card--fill", cut.Find(".moka-card").ClassList);
		Assert.Equal("moka-card-body", cut.Find(".moka-card-body").ClassName);
	}

	// A flex item grows and may shrink below its content. In a grid cell the card stretches to the row on its own,
	// so the rule sets no height that would fight a fixed row.
	[Fact]
	public void TheCard_FillsItsContainer_AsAFlexColumn()
	{
		IReadOnlyDictionary<string, string> card = ScopedCss.Declarations(Stylesheet, ".moka-card");
		IReadOnlyDictionary<string, string> fill = ScopedCss.Declarations(Stylesheet, ".moka-card--fill");

		Assert.Equal("flex", card["display"]);
		Assert.Equal("column", card["flex-direction"]);
		Assert.Equal("1 1 auto", fill["flex"]);
		Assert.Equal("0", fill["min-height"]);
		Assert.False(fill.ContainsKey("height"));
		Assert.False(fill.ContainsKey("align-self"));
	}

	[Fact]
	public void OnlyTheBody_Scrolls()
	{
		IReadOnlyDictionary<string, string> body = ScopedCss.Declarations(Stylesheet, ".moka-card--fill > .moka-card-body");

		Assert.Equal("1 1 auto", body["flex"]);
		Assert.Equal("0", body["min-height"]);
		Assert.Equal("auto", body["overflow-y"]);

		foreach (string part in new[] { ".moka-card--fill > .moka-card-media", ".moka-card--fill > .moka-card-header", ".moka-card--fill > .moka-card-footer" })
		{
			Assert.Equal("0", ScopedCss.Declarations(Stylesheet, part)["flex-shrink"]);
		}
	}

	// The wheel scrolls what is under the pointer and its containing blocks, which the body is not for the stretched
	// action's ::after. Checked in Chromium: under the layer the body never scrolled.
	[Fact]
	public void OnACardWithAnAction_TheScrollingBodySitsAboveTheAction()
	{
		IReadOnlyDictionary<string, string> body =
			ScopedCss.Declarations(Stylesheet, ".moka-card--clickable.moka-card--fill > .moka-card-body");

		Assert.Equal("relative", body["position"]);
		Assert.Equal("2", body["z-index"]);
	}
}
