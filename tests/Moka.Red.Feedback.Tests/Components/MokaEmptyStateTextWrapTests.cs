using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Feedback.EmptyState;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// A description of two or three lines could end on one word. TextWrap asks the browser for better breaks; today's
// breaks stay the default.
public class MokaEmptyStateTextWrapTests : BunitContext
{
	private const string Css = "MokaEmptyState.razor.css";

	[Fact]
	public void TheBrowsersOwnBreaks_AreTheDefault()
	{
		IRenderedComponent<MokaEmptyState> cut = Render<MokaEmptyState>(p => p.Add(x => x.Description, "Nothing yet"));

		Assert.Equal(MokaTextWrap.Normal, cut.Instance.TextWrap);
		Assert.Equal("moka-empty-state", cut.Find(".moka-empty-state").ClassName);
	}

	[Theory]
	[InlineData(MokaTextWrap.Pretty, "moka-empty-state--wrap-pretty")]
	[InlineData(MokaTextWrap.Balance, "moka-empty-state--wrap-balance")]
	public void TextWrap_IsAClassOnTheRoot(MokaTextWrap wrap, string expected)
	{
		IRenderedComponent<MokaEmptyState> cut = Render<MokaEmptyState>(p => p
			.Add(x => x.Description, "Nothing yet")
			.Add(x => x.TextWrap, wrap));

		Assert.Equal($"moka-empty-state {expected}", cut.Find(".moka-empty-state").ClassName);
	}

	[Theory]
	[InlineData("pretty")]
	[InlineData("balance")]
	public void TheTitleAndDescription_TakeTheStyle(string style)
	{
		Assert.Equal(style, ScopedCss.Declarations(Css, $".moka-empty-state--wrap-{style} .moka-empty-state__title")["text-wrap"]);
		Assert.Equal(style, ScopedCss.Declarations(Css, $".moka-empty-state--wrap-{style} .moka-empty-state__description")["text-wrap"]);
	}

	// Compact puts the text in a column of its own, which the descendant selectors still reach.
	[Fact]
	public void ACompactOne_TakesItToo()
	{
		IRenderedComponent<MokaEmptyState> cut = Render<MokaEmptyState>(p => p
			.Add(x => x.Description, "Nothing yet")
			.Add(x => x.Compact, true)
			.Add(x => x.TextWrap, MokaTextWrap.Pretty));

		Assert.NotNull(cut.Find(".moka-empty-state--wrap-pretty .moka-empty-state__text > .moka-empty-state__description"));
	}
}
