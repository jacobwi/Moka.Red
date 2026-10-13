using AngleSharp.Dom;
using Bunit;
using Moka.Red.Data.Pagination;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Data.Tests.Components;

// The arrows were named by title, so the browser's own pale, late hint showed instead of Moka's.
public class MokaPaginationTooltipTests : BunitContext
{
	// The bar sits under what it pages, so the names open above it.
	[Theory]
	[InlineData("First page")]
	[InlineData("Previous page")]
	[InlineData("Next page")]
	[InlineData("Last page")]
	public void TheArrows_AreNamedByTooltipsAboveTheBar(string name)
	{
		IRenderedComponent<MokaPagination> cut = Render<MokaPagination>(p => p
			.Add(x => x.TotalItems, 100)
			.Add(x => x.CurrentPage, 5));

		IconTooltip.AssertNamed(Arrow(cut, name), name, MokaTooltipPosition.Top);
	}

	[Theory]
	[InlineData("Previous page")]
	[InlineData("Next page")]
	public void TheCompactArrows_AreNamedByTooltipsAboveTheBar(string name)
	{
		IRenderedComponent<MokaPagination> cut = Render<MokaPagination>(p => p
			.Add(x => x.TotalItems, 100)
			.Add(x => x.CurrentPage, 5)
			.Add(x => x.Compact, true));

		IconTooltip.AssertNamed(Arrow(cut, name), name, MokaTooltipPosition.Top);
	}

	// Pinned to the top of its scroll area, the bar has the list below it.
	[Fact]
	public void ABarPinnedToTheTop_OpensItsTooltipsBelow()
	{
		IRenderedComponent<MokaPagination> cut = Render<MokaPagination>(p => p
			.Add(x => x.TotalItems, 100)
			.Add(x => x.Sticky, true)
			.Add(x => x.StickyPosition, MokaPaginationStickyPosition.Top));

		IconTooltip.AssertNamed(Arrow(cut, "Next page"), "Next page", MokaTooltipPosition.Bottom);
	}

	// A disabled button takes no pointer events in some browsers, so the hover has to land on the tooltip's own
	// element, which wraps the button and is never disabled. The tooltip stays for a greyed-out arrow.
	[Fact]
	public void ADisabledArrow_KeepsItsTooltip()
	{
		IRenderedComponent<MokaPagination> cut = Render<MokaPagination>(p => p.Add(x => x.TotalItems, 100));

		IElement first = Arrow(cut, "First page");
		Assert.True(first.HasAttribute("disabled"));
		IconTooltip.AssertNamed(first, "First page", MokaTooltipPosition.Top);
		Assert.False(first.ParentElement!.HasAttribute("disabled"));
	}

	private static IElement Arrow(IRenderedComponent<MokaPagination> cut, string name) =>
		cut.Find($"button[aria-label='{name}']");
}
