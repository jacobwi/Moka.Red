using System.Text.RegularExpressions;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// The motion wrappers ended on a transform they kept with animation-fill-mode, which made each one a stacking context
// and the containing block of every fixed descendant, so a dialog, drawer, command palette or tooltip opened inside was
// trapped in it. A filling animation does that even when its last frame is transform: none, since it keeps acting as
// will-change, so the animations let go when they end and their last frame is the element's own style. bUnit applies
// no CSS, so the stylesheets are read as text.
public partial class MotionEndStateTests
{
	[Theory]
	[InlineData("MokaFadeIn.razor.css", 4)]
	[InlineData("MokaSlideIn.razor.css", 4)]
	[InlineData("MokaScaleIn.razor.css", 1)]
	[InlineData("MokaStagger.razor.css", 3)]
	public void EveryKeyframeThatMoves_EndsOnTransformNone(string file, int moving)
	{
		IReadOnlyList<string> ends = ScopedCss.Values(file, "to", "transform");

		Assert.Equal(moving, ScopedCss.Values(file, "from", "transform").Count);
		Assert.Equal(moving, ends.Count);
		Assert.All(ends, end => Assert.Equal("none", end));
	}

	[Theory]
	[InlineData("MokaFadeIn.razor.css")]
	[InlineData("MokaScaleIn.razor.css")]
	[InlineData("MokaStagger.razor.css")]
	public void EveryFade_EndsFullyOpaque(string file)
	{
		IReadOnlyList<string> ends = ScopedCss.Values(file, "to", "opacity");

		Assert.NotEmpty(ends);
		Assert.All(ends, end => Assert.Equal("1", end));
	}

	[Theory]
	[InlineData("MokaFadeIn.razor.css", ".moka-fade-in.moka-fade-in--animated.moka-fade-in--none")]
	[InlineData("MokaFadeIn.razor.css", ".moka-fade-in.moka-fade-in--animated.moka-fade-in--up")]
	[InlineData("MokaFadeIn.razor.css", ".moka-fade-in.moka-fade-in--animated.moka-fade-in--down")]
	[InlineData("MokaFadeIn.razor.css", ".moka-fade-in.moka-fade-in--animated.moka-fade-in--left")]
	[InlineData("MokaFadeIn.razor.css", ".moka-fade-in.moka-fade-in--animated.moka-fade-in--right")]
	[InlineData("MokaSlideIn.razor.css", ".moka-slide-in.moka-slide-in--left")]
	[InlineData("MokaSlideIn.razor.css", ".moka-slide-in.moka-slide-in--right")]
	[InlineData("MokaSlideIn.razor.css", ".moka-slide-in.moka-slide-in--top")]
	[InlineData("MokaSlideIn.razor.css", ".moka-slide-in.moka-slide-in--bottom")]
	[InlineData("MokaScaleIn.razor.css", ".moka-scale-in")]
	public void TheAnimation_HoldsItsFirstFrameThroughTheDelay_AndLetsGoAtTheEnd(string file, string selector)
	{
		string animation = ScopedCss.Declarations(file, selector)["animation"];

		Assert.EndsWith(" backwards", animation, StringComparison.Ordinal);
	}

	[Fact]
	public void StaggeredItems_LetGoAtTheEnd() =>
		Assert.Equal("backwards", ScopedCss.Declarations("MokaStagger.razor.css", ".moka-stagger-item")["animation-fill-mode"]);

	[Theory]
	[InlineData(".moka-reveal--visible.moka-reveal--fade-in")]
	[InlineData(".moka-reveal--visible.moka-reveal--fade-up")]
	[InlineData(".moka-reveal--visible.moka-reveal--fade-down")]
	[InlineData(".moka-reveal--visible.moka-reveal--fade-left")]
	[InlineData(".moka-reveal--visible.moka-reveal--fade-right")]
	[InlineData(".moka-reveal--visible.moka-reveal--scale-in")]
	[InlineData(".moka-reveal--visible.moka-reveal--slide-up")]
	public void ARevealedElement_EndsOnTransformNone(string selector) =>
		Assert.Equal("none", ScopedCss.Declarations("MokaReveal.razor.css", selector)["transform"]);

	// will-change kept on the element for good makes it what a running transition only makes it while it runs.
	[Theory]
	[InlineData("MokaFadeIn.razor.css")]
	[InlineData("MokaSlideIn.razor.css")]
	[InlineData("MokaScaleIn.razor.css")]
	[InlineData("MokaStagger.razor.css")]
	[InlineData("MokaReveal.razor.css")]
	public void AMotionStylesheet_SetsNoWillChangeOrFilter(string file)
	{
		string css = Comment().Replace(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", file)), "");

		Assert.DoesNotContain("will-change", css, StringComparison.Ordinal);
		Assert.DoesNotMatch(@"(^|[;{\s])filter\s*:", css);
	}

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();
}
