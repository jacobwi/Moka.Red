using AngleSharp.Dom;
using Bunit;
using Moka.Red.Layout.Card;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

public class MokaCardInnerTests : BunitContext
{
	private const string Stylesheet = "MokaCard.razor.css";
	private const string SuccessEdge = "color-mix(in srgb, var(--moka-color-success) 22%, transparent)";

	private IElement Card(Action<ComponentParameterCollectionBuilder<MokaCard>> parameters) =>
		Render(parameters).Find(".moka-card");

	[Fact]
	public void Default_KeepsItsClasses()
	{
		Assert.Equal("moka-card moka-card--elevation-1", Card(_ => { }).ClassName);
	}

	[Fact]
	public void Inner_AddsItsClass_AndLeavesTheRest()
	{
		IElement card = Card(p => p.Add(x => x.Inner, true));

		Assert.Equal("moka-card moka-card--elevation-1 moka-card--inner", card.ClassName);
	}

	[Fact]
	public void Live_WithoutInner_DoesNothing()
	{
		IElement card = Card(p => p.Add(x => x.Live, true));

		Assert.Equal("moka-card moka-card--elevation-1", card.ClassName);
	}

	[Fact]
	public void Dashed_WithoutInner_DoesNothing()
	{
		IElement card = Card(p => p.Add(x => x.Dashed, true));

		Assert.Equal("moka-card moka-card--elevation-1", card.ClassName);
	}

	[Fact]
	public void Live_WithInner_AddsItsClass()
	{
		IElement card = Card(p => p.Add(x => x.Inner, true).Add(x => x.Live, true));

		Assert.Contains("moka-card--live", card.ClassList);
		Assert.DoesNotContain("moka-card--dashed", card.ClassList);
	}

	[Fact]
	public void Dashed_WithInner_AddsItsClass()
	{
		IElement card = Card(p => p.Add(x => x.Inner, true).Add(x => x.Dashed, true));

		Assert.Contains("moka-card--dashed", card.ClassList);
		Assert.DoesNotContain("moka-card--live", card.ClassList);
	}

	[Fact]
	public void Outlined_WithInner_KeepsBothClasses()
	{
		IElement card = Card(p => p.Add(x => x.Inner, true).Add(x => x.Outlined, true));

		Assert.Contains("moka-card--inner", card.ClassList);
		Assert.Contains("moka-card--outlined", card.ClassList);
	}

	[Fact]
	public void Class_StaysLast()
	{
		IElement card = Card(p => p.Add(x => x.Inner, true).Add(x => x.Class, "mine"));

		Assert.EndsWith("moka-card--inner mine", card.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void InnerRule_IsThePanelColor_WithNoLineOrShadow()
	{
		IReadOnlyDictionary<string, string> inner = ScopedCss.Declarations(Stylesheet, ".moka-card--inner");

		Assert.Equal("var(--moka-color-surface-variant)", inner["background"]);
		Assert.Equal("transparent", inner["border-color"]);
		Assert.Equal("var(--moka-radius-lg)", inner["border-radius"]);
		Assert.Equal("var(--moka-inner-padding)", inner["padding"]);
		Assert.Equal("var(--moka-spacing-lg)", inner["gap"]);
		Assert.Equal("none", inner["box-shadow"]);
	}

	[Theory]
	[InlineData(".moka-card--inner > .moka-card-header")]
	[InlineData(".moka-card--inner > .moka-card-body")]
	[InlineData(".moka-card--inner > .moka-card-footer")]
	public void InnerParts_LeaveThePaddingToTheCard(string part)
	{
		Assert.Equal("0", ScopedCss.Declarations(Stylesheet, part)["padding"]);
	}

	[Fact]
	public void InnerVariants_DrawTheirEdges()
	{
		Assert.Equal("var(--moka-color-outline)",
			ScopedCss.Declarations(Stylesheet, ".moka-card--inner.moka-card--outlined")["border-color"]);
		Assert.Equal(SuccessEdge,
			ScopedCss.Declarations(Stylesheet, ".moka-card--inner.moka-card--live")["border-color"]);
		Assert.Equal("1px dashed var(--moka-color-outline-strong)",
			ScopedCss.Declarations(Stylesheet, ".moka-card--inner.moka-card--dashed")["border"]);
	}

	// .moka-gate .moka-card sets the border color and radius with the specificity of these rules, so they restate them.
	[Fact]
	public void GateRules_RestateWhatTheCardsOwnGateRuleOverrides()
	{
		IReadOnlyDictionary<string, string> inner = ScopedCss.Declarations(Stylesheet, ".moka-gate .moka-card--inner");

		Assert.Equal("transparent", inner["border-color"]);
		Assert.Equal("var(--moka-radius-lg)", inner["border-radius"]);
		Assert.Equal("var(--moka-color-outline)",
			ScopedCss.Declarations(Stylesheet, ".moka-gate .moka-card--inner.moka-card--outlined")["border-color"]);
		Assert.Equal(SuccessEdge,
			ScopedCss.Declarations(Stylesheet, ".moka-gate .moka-card--inner.moka-card--live")["border-color"]);
		Assert.Equal("var(--moka-color-outline-strong)",
			ScopedCss.Declarations(Stylesheet, ".moka-gate .moka-card--inner.moka-card--dashed")["border-color"]);
	}

	// The body's padding-top under .moka-gate reaches every card nested in a card with a header. This selector
	// matches its specificity, and being later, wins.
	[Theory]
	[InlineData(".moka-gate .moka-card.moka-card--inner > .moka-card-header")]
	[InlineData(".moka-gate .moka-card.moka-card--inner > .moka-card-body")]
	[InlineData(".moka-gate .moka-card.moka-card--inner > .moka-card-footer")]
	public void GateRules_TakeThePaddingOffTheParts(string part)
	{
		Assert.Equal("0", ScopedCss.Declarations(Stylesheet, part)["padding"]);
	}

	// Same specificity as the clickable rules, so the order decides that hover and focus still show on an inner card.
	[Fact]
	public void InnerRules_ComeBeforeTheClickableRules()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Stylesheet));

		Assert.True(css.IndexOf(".moka-card--inner {", StringComparison.Ordinal) <
		            css.IndexOf(".moka-card--clickable:hover", StringComparison.Ordinal));
		Assert.True(css.IndexOf(".moka-gate .moka-card--inner.moka-card--dashed", StringComparison.Ordinal) <
		            css.IndexOf(".moka-gate .moka-card--clickable:hover", StringComparison.Ordinal));
	}
}
